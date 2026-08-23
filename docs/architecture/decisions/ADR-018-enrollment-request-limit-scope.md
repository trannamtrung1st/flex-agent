# ADR-018: Enrollment request-limit scope for the first production slice

## Status

Approved — 2026-08-23; implementation-readiness amendment approved 2026-08-23

This record was approved with `Q-8` / `PROP-8` of
[`submission-attempts.md`](../../requirements/features/submission-attempts.md).

## Owners and approvers

- Owner: Architecture Lead
- Required approvers: Product Lead, Architecture Lead, Security/Privacy reviewer
- Proposed date: 2026-08-23
- Approved date: 2026-08-23
- Amendment date: 2026-08-23

## Context

[ADR-006](ADR-006-mvp-architecture-baseline-and-evolution.md) assigns the
simple API gateway TLS, routing, coarse request/connection/rate limits,
correlation, and security headers. It keeps product authorization in the API.
Optional cache may later hold bounded rate counters after explicit selection.

The first Enrollment production slice shipped a replica-local API limiter keyed
by `(organization, actor, surface)` with frozen 60/20/≥10s ceilings. That
limiter is defense in depth: each API process has independent in-memory
partitions. A task-file sentence that “the gateway applies bounded
per-actor/Organization request limits” overstated both ADR-006 and the
implemented surface.

Closing that gap either requires a replica-independent shared store or
actor-aware gateway policy, or an explicit scope decision that those are not
part of this Enrollment slice.

## Decision drivers

- Do not invent a shared operational store without a demonstrated requirement
  and approved selection.
- Do not put actor or Organization identity into gateway labels or treat
  coarse ingress limits as product quotas.
- Keep the first Enrollment slice closable only when its stated contract
  matches approved requirements and architecture.

## Options considered

| Option | Pros | Cons |
| --- | --- | --- |
| Implement replica-independent / gateway per-actor/Organization Enrollment quota in this slice | Matches the overstated task-file contract | Selects Redis or equivalent, or actor-aware NGINX policy, without an approved component decision; delays Enrollment closeout |
| Keep replica-local limiter as this slice's implemented contract; track shared quota separately (`PROP-8`) | Aligns with ADR-006 coarse gateway limits; avoids a new store | Effective capacity still scales with replica count until the follow-on task lands |
| Treat recording the gap in `.work/` as sufficient to mark the slice completed | Fast closeout | Leaves a stated production contract unmet |

The implementation-readiness review also compared the approved reference
components for the follow-on task:

| Follow-on realization | Benefits | Costs and risks |
| --- | --- | --- |
| PostgreSQL-backed application admission port | Uses the already-required authoritative PostgreSQL deployment and database clock; preserves application-owned actor scope; needs no new component | Adds one bounded write on every authenticated Enrollment request and requires cleanup, outage, and load evidence |
| Actor-aware NGINX policy | Could reject before application work | Moves trusted actor/Organization interpretation into a transport-only gateway and requires identity propagation the approved gateway contract intentionally excludes |
| Redis or another distributed counter store | Purpose-built low-latency counters | Adds an unselected component, new secret/network/failover/backup boundaries, and a second consistency dependency without current evidence |

## Decision

This Enrollment slice does not implement replica-independent,
shared, or gateway-enforced per-actor/Organization request limits. The
implemented contract is the replica-local API limiter. ADR-006 coarse gateway
limits remain in force and are not that per-actor contract. Shared quota work
is tracked in `.work/active/p0-enrollment-shared-request-quota.md`.

### Follow-on implementation realization

The follow-on uses PostgreSQL `18.x`, already selected by ADR-008, behind an
application-owned asynchronous Enrollment admission port and a
Submissions-infrastructure adapter. NGINX remains transport-only; Redis and a
new infrastructure service remain unselected.

- Add one additive migration after the current migration head for a
  Submissions-owned deployment-wide policy record and short-lived fixed-window
  counters keyed by trusted Organization, actor, surface, and database-derived
  window start. The exact migration number is resolved at implementation time.
- The durable policy record is the one deployment-wide source for read limit,
  mutation limit, window duration, and policy revision. Production and Staging
  replicas must match it and fail startup or acquisition closed on mismatch.
  The approved defaults and ceilings remain 60 reads, 20 mutations, and a
  10-second minimum window; policy changes may only tighten them and require a
  versioned coordinated activation that cannot create overlapping budgets.
- Use PostgreSQL UTC time to derive the fixed window. Acquire one permit with a
  single atomic statement/transaction that cannot increment beyond the
  applicable limit under concurrency. A committed acquisition remains consumed
  even if the HTTP response is lost because this quota counts admitted
  requests, not successful business commands.
- Authenticate and resolve the current application session before using the
  actor-scoped quota. Acquire the shared permit before any protected Enrollment
  query or mutation. Unauthenticated abuse remains governed by ADR-006/ADR-008
  coarse ingress limits and does not create actor quota rows.
- Proven exhaustion preserves the existing
  `429`/`enrollment.rate_limited`/`Retry-After`/`no-store` contract. Shared
  database timeout, unavailability, or policy mismatch returns the existing
  non-disclosing `503`/`enrollment.unavailable`/`no-store` contract and never
  falls back to a local permit. The replica-local limiter may remain as a
  stricter defense-in-depth guard but is not authoritative for the global
  budget.
- Counter rows are protected operational state, not business history or audit
  evidence. They become cleanup-eligible immediately after their window closes;
  acquisition and maintenance paths must perform bounded indexed cleanup so
  stale identifiers do not accumulate. Logs, metrics, traces, errors, and
  gateway labels expose only allowlisted surface and outcome categories.
- The implementation must prove two-process aggregation, boundary time,
  restart, policy mismatch/change, saturation, timeout/outage, recovery,
  cleanup, isolation, telemetry redaction, and representative PostgreSQL load.
  It must preserve the approved two-second Enrollment mutation objective.

## Consequences

- Positive: Enrollment closeout cannot claim a gateway per-actor quota it did
  not build; a follow-on task owns the residual.
- Negative: until the follow-on task lands, two API replicas double the
  effective local permit budget.
- Neutral: 429/`enrollment.rate_limited`/`Retry-After` behavior of the local
  limiter is unchanged.
- Positive: the follow-on is implementable with the existing PostgreSQL and
  application trust boundaries; no gateway identity policy or new cache
  component is required.
- Negative: PostgreSQL admission becomes an availability dependency for
  authenticated Enrollment requests and intentionally fails closed when a
  permit cannot be proven.

## Implementation

The follow-on PostgreSQL admission port landed on 2026-08-23 as migration
`0044_enrollment_shared_request_admission.sql` and
`IEnrollmentSharedAdmissionPort`. Migration
`0045_enrollment_shared_admission_window_freeze_and_expiry.sql` freezes
`window_seconds` at 10 seconds so a policy change cannot expire a live
counter and issue a second budget in the same aligned window, and stores
indexed `expires_at` so hot-path cleanup is a bounded expiry-range delete
with `SKIP LOCKED`. Lengthening the window remains a future coordinated
activation design, not an MVP operator control. Replica-local limiting
remains defense in depth. NGINX remains transport-only and Redis remains
unselected.

## Related

- Requirements: `Q-8` / `PROP-8` in
  [`submission-attempts.md`](../../requirements/features/submission-attempts.md),
  including `REQ-SUBM-57`–`REQ-SUBM-58` and
  `AC-SUBM-40`–`AC-SUBM-41`
- Follow-on implementation:
  [`.work/active/p0-enrollment-shared-request-quota.md`](../../../.work/active/p0-enrollment-shared-request-quota.md)
- Review fixes:
  [`.work/active/p0-enrollment-shared-admission-review-fixes.md`](../../../.work/active/p0-enrollment-shared-admission-review-fixes.md)
- Does not supersede ADR-006
