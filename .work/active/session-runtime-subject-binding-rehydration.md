---
id: session-runtime-subject-binding-rehydration
status: completed
created: 2026-08-18
updated: 2026-08-18
predecessor: session-runtime-production-http-sse
---

# Goal

Replace the production Session SSE adapter's actor-keyed subject lookup and
fail-closed bindings with persistence-backed resolution of the actor's
**current relationship for the requested Session**, so hosted
`GET /sessions/{id}/events` can become a usable path without treating a global
`actor → organization + participant + relationship` map as authorization.

# Governing sources

- `.work/active/session-runtime-production-http-sse.md` — completed adapter
  seam; records the P2 that this task must not inherit
- `docs/requirements/features/session-text-lifecycle.md` — `REQ-SESS-29`,
  `REQ-SESS-47`, `REQ-SESS-59`; complete resource-chain authorization for
  real-time access
- `docs/requirements/features/auth-resource-isolation.md` — actor/action/resource
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
- `docs/architecture/session-runtime-contract.md` — `SESS-DEC-7`

# Scope

## In

- Resolve subject scope from trusted records for `(actor, untrustedSessionId)`:
  session → organization → activity → participant/enrollment → current
  relationship. Do not keep `ISessionEventSubjectSource.GetCurrentAsync(Guid
  actorId)` as the production contract.
- Persistence-backed `ITrustedSessionBindingSource` (frozen-policy binding
  rehydration) so hosted composition is not fail-closed for authorized
  participants
- Reauthorize that Session-scoped chain on subscribe, replay, and 60-second
  held revalidation
- Negative tests: same actor participant in one Session and reviewer/admin in
  another; guessed Session id cannot inherit the other Session's relationship

## Out

- Completing `REQ-SESS-59` without OIDC product login (OIDC remains a
  related gate; this task must not revive `X-Flex-Test-Actor-Id` as production
  authentication)
- Pointing `SessionPage` at production SSE
- Worker live-model credential rehydration (see worker-host-wiring)
- Rewriting frozen migrations `0005`–`0019` unless a new schema is required
  and approved

# Plan

- [x] Replace actor-keyed subject source with Session-scoped authoritative
      resolver; keep the hosted path fail-closed until the resolver is wired
- [x] Persist/rehydrate trusted Session bindings from frozen policy
- [x] Cover cross-Session relationship isolation and enrollment/assignment
      revoke while an org grant remains
- [x] Write the initial participant relationship atomically in the trusted
      Session insert transaction (review P1)
- [x] Make `SetCurrentAsync` apply only a strictly newer supplied version
      (review P2 stale-set)
- [x] Upsert a revoked tombstone when revoke arrives before any assignment
      row (review P2 revoke-before-first-set)
- [x] Refuse populated `0020`→`0021` upgrades that cannot backfill snapshots
      (review P2)
- [x] Reconcile spec Partial rows when the hosted path is actually usable

# Current state

Completed follow-up to `3e67e91`. `RevokeCurrentAsync` upserts a revoked
tombstone with the same relationship metadata as assignment, so a newer
revoke that arrives before the first set still wins over a delayed lower
`SetCurrentAsync`. Hosted SSE still writes the starting participant at
insert and revalidates on authorize/replay/hold. Worker bindings remain
fail-closed. `REQ-SESS-59` stays Partial.

# Decisions

- `ISessionEventSubjectSource.ResolveCurrentAsync(actor, untrustedSessionId)`
  is the production contract. Actor-keyed `GetCurrentAsync(actorId)` is gone.
- Current Session relationship is a dedicated row
  (`session_actor_relationships`), not an org grant. Revoke that row while
  the org grant remains and subscribe/replay deny.
- Frozen policy is an immutable Session snapshot written at
  `InsertActiveAsync`. API `ITrustedSessionBindingSource` is
  `PostgresTrustedSessionBindingSource`. Worker stays
  `FailClosedTrustedSessionBindingSource`.
- Configuration digest on `session_runtimes` must equal the frozen policy
  digest for rehydration (P0 insert invariant).
- Rationale: ADR-002 requires actor/action/resource decisions against current
  authoritative state; a single global relationship per actor cannot
  represent participant-in-one-Session and reviewer-in-another.

# Findings / deviations

- P2 from review of `5fc6b7f` is remediated: subject lookup is Session-scoped.
- Full product Enrollment aggregate is not introduced. The current
  participant/reviewer/administrator row plus `session_runtimes` ownership
  is the trusted chain for this slice.
- Initial participant relationship is written in the same
  `InsertActiveAsync` transaction as the runtime and frozen-policy snapshot.
- Later enrollment/reviewer assignment still uses `SetCurrentAsync`.
  `RevokeCurrentAsync` upserts a revoked tombstone with the same
  relationship metadata when no row exists yet, so a delayed lower
  assignment cannot INSERT access. A separate version-watermark table was
  not added; the existing `(organization, session, actor)` row is the
  monotonic projection.
- `SetCurrentAsync` and `RevokeCurrentAsync` both take a
  `SessionActorRelationship` and apply only a strictly newer supplied
  version. Revoke upserts a tombstone (`revoked_at` set) even when no row
  existed, and preserves `revoked_at` when advancing an existing tombstone.
- Populated `0020` databases cannot reconstruct frozen policy payloads.
  `0021` fails closed when `session_runtimes` already has rows. Empty
  `0020`→`0021` remains the supported upgrade.
- `0021` was edited after first apply on `main` (same-day pre-prod). Databases
  that already recorded the previous `0021` hash will fail closed on checksum,
  matching earlier frozen-script hardening. Populated historical upgrade tests
  now stop at `0020` because `0021` is intentionally unsupported for existing
  `session_runtimes`.

# External review

- `4831cab` (2026-08-18): **approved** the recording of that P2 and this
  task's scope (`(actor, untrustedSessionId)` chain, persistence-backed
  bindings, 60-second revalidation, participant-in-one-Session /
  reviewer-in-another negative case). GitHub had no status checks for the
  SHA at review time.
- `af152df` (2026-08-18): **changes requested**. P1: no production writer for
  `session_actor_relationships` on Session insert, so hosted SSE stays
  fail-closed. P2: `SetCurrentAsync` ignores the supplied version and can
  un-revoke from a stale write. P2: `0021` strands pre-existing
  `session_runtimes` because snapshots are not backfilled. GitHub still
  exposed no commit status checks for the SHA.

- `b94d6fc` (2026-08-18): **changes requested**. P1 insert writer, stale-set
  CAS, and populated `0021` refuse are addressed. Remaining P2: a delayed
  `RevokeCurrentAsync` with no version can still revoke a newer assignment.

- `6d01939` (2026-08-18): **changes requested**. Remaining P2: a newer revoke
  is ignored on an already-revoked row, so a delayed lower `SetCurrentAsync`
  can restore access.

- `3e67e91` (2026-08-18): **changes requested**. Remaining P2: revoke cannot
  create a tombstone when no relationship row exists yet, so a delayed
  assignment INSERT can restore access.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Session-scoped subject resolution | passed | `SubscribeAuthorizedSessionEventsCommandTests` (ResolveCurrentAsync contract); `PostgresSessionActorRelationshipStore` join to `session_runtimes` |
| Cross-Session relationship isolation | passed | Unit: participant vs reviewer/admin on another Session; HTTP: `Participant_in_one_session_does_not_inherit_reviewer_or_guessed_session_access`; Postgres: guessed Session with remaining org grant and reviewer relationship on the other Session |
| Enrollment revoke while org grant remains | passed | `Subscribe_denies_after_enrollment_revoke_while_org_grant_remains` |
| Hosted bindings no longer fail-closed for authorized participants | passed | `InsertActiveAsync` writes participant v1; subscribe happy path no longer calls `SetCurrentAsync`; API composition registers `PostgresTrustedSessionBindingSource` |
| Frozen-policy snapshot round-trip | passed | `FrozenRuntimePolicySnapshotTests`; `InsertActiveAsync` writes snapshot; empty upgrade `0020`→`0021` |
| Relationship version CAS | passed | `Stale_set_current_after_revoke_does_not_restore_access`; `Stale_revoke_after_newer_assignment_does_not_revoke`; `Newer_revoke_advances_tombstone_so_delayed_lower_set_does_not_restore_access`; `Revoke_before_first_set_keeps_delayed_lower_assignment_from_creating_access` |
| Populated `0020`→`0021` fails closed | passed | `Upgrade_from_populated_0020_runtime_fails_closed` |
| Locked .NET regression | passed | `bash build/scripts/verify-dotnet.sh` **910/910** |
| Docs | passed | `python3 scripts/check_docs.py`; `git diff --check` clean; `REQ-SESS-59` Partial rows updated |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked (`REQ-SESS-59` remains Partial:
      OIDC and Participant UI are later; production SSE is now Session-scoped
      and binding-rehydrated, with a production participant writer on insert)
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
