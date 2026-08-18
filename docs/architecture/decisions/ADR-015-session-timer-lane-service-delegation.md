# ADR-015: Session timer-lane service delegation realization

## Status

Proposed. This records the Worker timer-lane realization of approved
[ADR-002](ADR-002-authorization-enforcement-and-delegation.md) and
`REQ-SESS-75`. It does not replace ADR-002. It is working implementation
guidance until product, architecture, and security/privacy approve it.

## Decision metadata

| Field | Value |
| --- | --- |
| **Owner** | Architecture Lead |
| **Required approvers** | Product Lead, Architecture Lead, Security/Privacy reviewer |
| **Consulted perspectives** | Architecture, backend, security/privacy |
| **Proposed date** | 2026-08-18 |
| **Governs** | Durable per-Session `session.timer_lane.fire` service delegation, timer-schedule envelope reference, Worker timer-polling capability, and commit-time reauthorization |
| **Upstream sources** | [ADR-002](ADR-002-authorization-enforcement-and-delegation.md), [auth-resource-isolation](../../requirements/features/auth-resource-isolation.md), [text Session lifecycle](../../requirements/features/session-text-lifecycle.md) `REQ-SESS-75` |
| **Extends** | ADR-002 delegated service execution |
| **Preserves** | ADR-001 frozen configuration, ADR-003 mutation-coupled audit, ADR-013 one-lane timer replacement, applied migrations `0001`–`0023` |

## Context

ADR-002 already requires background work to authenticate a service identity,
load a durable delegation reference and resource scope, and revalidate current
authorization before protected work and again before a sensitive commit. The
Organization/action grant kernel does not by itself carry a stable per-resource
delegation, purpose, expiry, or Session scope. Activating
`PostgresFireDueTimerCoordinator` with a static Worker actor would therefore
violate the delayed-work contract.

## Decision drivers

- Deny-by-default delayed timer work (`REQ-AUTH-11`, `REQ-SESS-75`).
- One authoritative PostgreSQL clock and transaction for due state, lifecycle,
  cutoff, expected revision, delegation freshness, Invocation, audit, and outbox.
- No silent backfill of authority for historical timer rows.
- Database reachability is not authorization or provider qualification.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Organization-wide Worker grant only | Smallest schema change | Cannot prove resource/action-bound delayed work; static actor becomes permission |
| Infer delegation from Session id or process location | No new envelope field | Guessed or substituted Sessions would inherit authority |
| Per-Session durable delegation linked from the timer schedule | Matches ADR-002 envelope + resource locators | Additive table and insert-time issuance |
| Backfill historical active Sessions from identifiers | Unblocks old due rows | Fabricated authority; silently widens permission |

## Decision

1. Store one durable per-Session service-delegation record for
   `session.timer_lane.fire` with a stable identifier, Worker service principal,
   complete Organization/Activity/Participant/Attempt/Session ownership, allowed
   action, system purpose, initiating authority, effective/expiry bounds,
   revocation, and monotonic version.
2. Carry that identifier on `session_timer_schedules` as the trusted work
   envelope. Do not infer it from Session id, host identity, or a static actor
   label.
3. Issue the record in the same Session-insert transaction for new
   timer-enabled Sessions. Do not backfill historical rows; missing or invalid
   references stay fail-closed and retryable without cancelling a valid pending
   timer.
4. The Worker authenticates as its configured service principal, rehydrates the
   immutable Session policy snapshot, and authorizes the delegation on the
   timer-fire transaction before admitting one Invocation. Admission does not
   take `FOR SHARE`. After persistence, audit, and outbox writes, commit
   reauthorization is the last meaningful SQL before `COMMIT`, so wall-clock
   expiry as well as concurrent revoke still deny. Due-claim selects
   only currently valid, ownership-matched envelopes for that service principal
   so revoked, expired, mismatched, or historical-null rows stay pending without
   head-of-line blocking another Session.
5. Hosted timer polling requires an explicit `Sessions:TimerPolling:Enabled`
   capability, default `false`, in addition to a Sessions connection string,
   PostgreSQL binding rehydration, and the authorization kernel. The fail-closed
   model port remains until a later provider-qualification task.
6. Issuance and revocation of service delegations are authorized operations
   (`service_delegation.issue` / `service_delegation.revoke`) against a current
   actor-organization grant. The mutated resource is `service_delegation` /
   `delegation_id`. Audit `authorization_reference` names that grant, not the
   delegation being created or changed. Callers supply initiator, correlation,
   source, and reason; insert must not invent them. Mutations occur only inside
   the caller's database transaction together with an append-only transition
   row and required durable audit (`REQ-AUTH-31`). Commit reauthorization is
   the last meaningful SQL of the mutation (or of Session insert when issuing
   during insert). Repository methods do not autocommit security-state changes.
   Single-action delegations are not rewritten in place: replacing capability
   requires revoke plus a newly authorized issue (`REQ-AUTH-5`).
7. `session.timer_lane.fire` delegations require `expires_at`, and the lifetime
   from `effective_at` cannot exceed seven days (`PROP-WBT-5`). Renewal remains
   a later authorized command. Timer-fire audit events record
   `authorization_reference_type=service_delegation` and the exact
   `delegation_id` (`REQ-AUTH-27`). Additive `0023` refuses *active* unbounded
   or over-long timer-lane rows left by `0022` with an operator-facing error
   and does not fabricate expiry. Revoked historically unbounded rows may
   upgrade so the documented revoke-then-retry repair works. A service-
   delegation mutation that fails commit-time reauthorization aborts the
   caller transaction so a later `COMMIT` cannot persist the staged writes.

## Consequences

- Timer due-claim can no longer treat process identity as permission.
- New Sessions must receive a trustworthy Worker service principal at insert.
- Operators can distinguish invocation claiming from timer polling in readiness
  copy without protected identifiers.
- Live model providers, OIDC, and production-pilot certification remain out of
  scope.

## Related

- Requirements: `REQ-AUTH-11`, `REQ-AUTH-18`–`REQ-AUTH-20`, `REQ-AUTH-27`,
  `REQ-AUTH-31`, `REQ-SESS-75`
- Proposed defaults: `PROP-WBT-1`–`PROP-WBT-7` in
  `.work/active/session-runtime-worker-binding-timer-activation.md`
