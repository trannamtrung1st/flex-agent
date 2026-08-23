# ADR-018: Enrollment request-limit scope for the first production slice

## Status

Proposed — 2026-08-23

This record does **not** govern implementation until Product Lead, Architecture
Lead, and Security/Privacy reviewer approve it. Until then, the interim default
in `Q-8` / `PROP-8` of
[`submission-attempts.md`](../../requirements/features/submission-attempts.md)
is working guidance only.

## Owners and approvers

- Owner: Architecture Lead
- Required approvers: Product Lead, Architecture Lead, Security/Privacy reviewer
- Proposed date: 2026-08-23
- Approved date: not approved

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

## Decision

**Proposed:** this Enrollment slice does not implement replica-independent,
shared, or gateway-enforced per-actor/Organization request limits. The
implemented contract is the replica-local API limiter. ADR-006 coarse gateway
limits remain in force and are not that per-actor contract. Shared quota work
is tracked in `.work/active/p0-enrollment-shared-request-quota.md`.

## Consequences

- Positive: Enrollment closeout cannot claim a gateway per-actor quota it did
  not build; a follow-on task owns the residual.
- Negative: until the follow-on task lands, two API replicas double the
  effective local permit budget.
- Neutral: 429/`enrollment.rate_limited`/`Retry-After` behavior of the local
  limiter is unchanged.

## Related

- Requirements: `Q-8` / `PROP-8` in
  [`submission-attempts.md`](../../requirements/features/submission-attempts.md)
- Follow-on implementation:
  [`.work/active/p0-enrollment-shared-request-quota.md`](../../../.work/active/p0-enrollment-shared-request-quota.md)
- Does not supersede ADR-006
