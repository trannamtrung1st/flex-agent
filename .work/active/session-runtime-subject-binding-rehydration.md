---
id: session-runtime-subject-binding-rehydration
status: planned
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

- [ ] Replace actor-keyed subject source with Session-scoped authoritative
      resolver; keep the hosted path fail-closed until the resolver is wired
- [ ] Persist/rehydrate trusted Session bindings from frozen policy
- [ ] Cover cross-Session relationship isolation and enrollment/assignment
      revoke while an org grant remains
- [ ] Reconcile spec Partial rows when the hosted path is actually usable

# Current state

Planned successor after approval of `5fc6b7f`. Production SSE remains an
adapter seam. Do not implement until this task is started.

# Decisions

- Interim default while this task is unstarted: actor-keyed memory subject
  lookup plus fail-closed bindings. That default is adapter-seam only and
  does not govern production authorization once rehydration is in scope.
- Rationale: ADR-002 already requires actor/action/resource decisions against
  current authoritative state; a single global relationship per actor cannot
  represent participant-in-one-Session and reviewer-in-another.

# Findings / deviations

- P2 from review of `5fc6b7f`: `GetCurrentAsync(actorId)` returns one
  organization, participant id, and relationship for the actor, then the
  handler applies that to whatever `UntrustedSessionId` was requested.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Session-scoped subject resolution | pending | |
| Cross-Session relationship isolation | pending | |
| Hosted bindings no longer fail-closed for authorized participants | pending | |

# Blockers

None. Not started.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [ ] Governing specifications were rechecked
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
