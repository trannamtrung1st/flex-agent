---
id: session-runtime-production-http-sse
status: completed
created: 2026-08-17
updated: 2026-08-18
predecessor: structured-agent-runtime-sync
---

# Goal

Host a production HTTP Session event **adapter seam** so authorized reconnect
and multi-device replay use `ReplayAuthorizedSessionEventsCommand` over
PostgreSQL, not the synthetic `/browser` adapter. This slice does **not**
complete `REQ-SESS-59`: OIDC, frozen-policy binding rehydration, and
participant UI remain later.

# Governing sources

- `.work/active/structured-agent-runtime-sync.md` — completed foundation;
  replay command and Repeatable Read coordinator exist and are tested
- `docs/requirements/features/session-text-lifecycle.md` — `REQ-SESS-59`,
  `AC-SESS-32` (combined row remains Partial: adapter seam, UI synthetic,
  OIDC deferred)
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
- `docs/architecture/decisions/ADR-011-participant-visible-agent-response-streaming.md`
- `docs/architecture/session-runtime-contract.md` — `SESS-DEC-7`, `SESS-DEC-10`,
  `SESS-DEC-13`
- `docs/ui-ux/text-session.md` — Participant reconnect copy; do not expose
  Decision/timer internals

# Scope

## In

- API inbound adapter: `GET /sessions/{id}/events` mapping authenticated actor
  + current server-side subject + untrusted `Last-Event-ID` to
  `ReplayAuthorizedSessionEventsCommand`
- Deny by default; never treat `Last-Event-ID` as identity or authorization
- ADR-002 kernel plus current participant relationship on every authorize and
  replay, including 60-second held revalidation
- Held connection completes on grant revoke or relationship narrowing
- Fail-closed hosted bindings until frozen-policy rehydration exists
- Test identity gated to Development/Testing + harness key; never an actor
  GUID alone
- Sessions store tagged on `/health/ready` when `ConnectionStrings:Sessions`
  is set
- Keep synthetic `/browser` as the Participant UI harness (no UI switch)

## Out

- Worker claim/content-loop wiring (see `session-runtime-worker-host-wiring`)
- Live providers, OIDC product login
- Persistence-backed actor/enrollment/binding rehydration
  (`.work/active/session-runtime-subject-binding-rehydration.md`)
- Backup/restore/export labs
- Rewriting frozen migrations `0005`–`0019`
- Treating synthetic `/browser/.../events` as `REQ-SESS-59` complete
- Pointing `SessionPage` at production SSE (Playwright reconnect evidence)

# Plan

- [x] Confirm replay command/coordinator contracts and SSE event catalog
- [x] Red: API has no production Session events route / kernel
- [x] Green: authorized subscribe + replay + 60s revalidation + revoke
- [x] Isolation and cursor tests; Playwright skipped (UI not switched)
- [x] External review remediations: adapter-seam honesty, current subject
      revalidation, test-identity gate, Sessions store readiness

# Current state

This adapter-seam slice is **frozen**. External review of `4831cab` (2026-08-18)
is **approved** (0 blocking). `GET /sessions/{id}/events` remains a production
adapter seam. `REQ-SESS-59` stays Partial. Session-scoped subject/binding
rehydration is successor work
(`.work/active/session-runtime-subject-binding-rehydration.md`). Do not iterate
this SSE-only slice.

# Decisions

- Synthetic `/browser` remains a harness. Production SSE is a distinct host
  adapter over the existing replay command.
- `SubscribeAuthorizedSessionEventsCommand` carries only actor identity plus
  untrusted session id/cursor. Organization, participant id, and relationship
  are loaded from `ISessionEventSubjectSource` on every authorize/replay.
  Enrollment is not a separate store yet; current relationship + participant
  id on that source are the interim stand-in until frozen-policy rehydration.
  Actor-keyed `GetCurrentAsync(actorId)` is **not** the production model: an
  actor may have different relationships per Session. Successor:
  `.work/active/session-runtime-subject-binding-rehydration.md`.
- Hosted `ITrustedSessionBindingSource` is `FailClosedTrustedSessionBindingSource`.
  Tests that need bindings register `MemoryTrustedSessionBindingSource`
  explicitly.
- Test identity: `SessionEvents:TestIdentity:Enabled` + harness key header
  `X-Flex-Session-Events-Test-Key`, and only in Development or Testing. An
  actor GUID header is not authentication. Production environment always uses
  `DisabledSessionEventIdentityAdapter`.
- When Sessions is configured, `/health/ready` includes `sessions-store`
  (`SELECT 1`).

# Findings / deviations

- Red was compile-fail for missing `SubscribeAuthorizedSessionEventsCommand`
  (`dotnet test` Sessions subscribe class, 2026-08-17).
- Self-review: `ReplayAsync` also calls the kernel so a poll cannot disclose
  after revoke even if the 60s timer has not fired; HTTP maps deny vs
  reconcile to `: access-revoked` vs `: reconcile`.
- Second review (2026-08-17): held-loop poll now drains `HasMore` and closes
  on reconcile/deny, matching the initial replay path.
- External review remediations (2026-08-18): current subject revalidation,
  fail-closed hosted bindings, test-identity gate, Sessions readiness.
- External approval of `5fc6b7f` (2026-08-18): P1s accepted. P2 recorded, not
  remediating in this slice: `ISessionEventSubjectSource` is actor-keyed and
  cannot represent per-Session relationships. Fix with persistence-backed
  rehydration, not another SSE-only commit.

# External review

- `0b78ba0` (2026-08-18): request changes — empty hosted bindings labeled
  complete, frozen relationship on the command, unguarded actor GUID header.
- `5fc6b7f` (2026-08-18): **approved** with P2 — actor-keyed subject lookup
  is not the production authorization model. Recorded in `4831cab`.
- `4831cab` (2026-08-18): **approved**. 0 blocking. Freeze this adapter seam.
  Successor `session-runtime-subject-binding-rehydration` owns Session-scoped
  chain resolution, persistence-backed bindings, and cross-Session
  relationship tests. GitHub had no attached status checks for this SHA at
  review time; evaluation was of the diff. Local `verify-dotnet.sh` **895/895**
  was for the prior implementation SHA `5fc6b7f`.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Production route uses replay command + PostgreSQL | passed | `SessionRuntimeProductionSubscribeTests` 2/2; API composition when `ConnectionStrings:Sessions` is set |
| Last-Event-ID is not authorization | passed | Stolen cursor without actor → 401; malformed cursor reconciles with no fragment text |
| Actor GUID is not production auth | passed | Header without harness key → 401; Production env + configured test identity → 401 |
| 60s revocation revalidation | passed | Held SSE completes after kernel deny; relationship narrowing while org grant remains denies; Postgres grant revoke then `AuthorizeAsync` denies |
| Cross-session leak tests | passed | Guessed session / reviewer / wrong participant leak no events |
| Ready reflects Sessions store | passed | `Api_ready_is_unhealthy_when_sessions_store_is_configured_but_unavailable` |
| Locked .NET regression | passed | `bash build/scripts/verify-dotnet.sh` **895/895** |
| Docs | passed | `python3 scripts/check_docs.py`; `git diff --check` clean |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked (`REQ-SESS-59` remains Partial;
      this is an adapter seam)
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
