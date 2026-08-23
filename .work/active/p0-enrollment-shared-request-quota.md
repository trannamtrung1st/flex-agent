---
id: p0-enrollment-shared-request-quota
status: in-progress
created: 2026-08-23
updated: 2026-08-23
---

# Goal

Own replica-independent per-actor/Organization Enrollment request limits under
approved `PROP-8` and ADR-018. The first Enrollment slice no longer owns this
contract; this task is the implementation home.

# Governing sources

- `docs/requirements/features/submission-attempts.md` — approved `Q-8` /
  `PROP-8`, `REQ-SUBM-57`–`REQ-SUBM-58`, and
  `AC-SUBM-40`–`AC-SUBM-41`
- `docs/architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md`
  — coarse gateway limits; optional cache may later hold bounded rate counters
- `docs/architecture/decisions/ADR-018-enrollment-request-limit-scope.md`
  — approved Enrollment request-limit scope
- `.work/active/p0-enrollment-assignment-discovery.md` — replica-local limiter
  already shipped as defense in depth

# Scope

## In

- A replica-independent admission control for authenticated Enrollment reads
  and mutations, with protected identifiers excluded from telemetry labels.
- Negative tests that two API processes cannot silently double the approved
  permit budget.
- PostgreSQL-backed deployment-wide policy and fixed-window counters using
  database UTC time, atomic bounded acquisition, bounded cleanup, and no local
  fallback when shared admission is uncertain.
- Preserve the existing safe 429 contract for proven exhaustion and return the
  existing non-disclosing 503 unavailable contract for shared-store failure or
  policy mismatch.

## Out

- Changing Enrollment assignment, lifecycle, or My work product behavior
- Putting actor, Organization, Enrollment, or Participant identifiers in
  metrics or gateway labels
- Selecting Redis, another store, or an actor-aware gateway mechanism without
  a reviewed design and explicit component decision
- General-purpose quotas, unauthenticated ingress limiting, billing, customer
  quota administration, or changing Enrollment authorization semantics

# Plan

- [x] Record approval of `PROP-8` and ADR-018 and reconcile ownership with the
  first Enrollment slice.
- [x] Reconcile the production topology, exact global permit contract, abuse
  and privacy threats, and multi-replica failure modes; compare a shared store
  with an actor-aware gateway surface and record the required component
  decision before implementation.
- [ ] Red — prove two API processes cannot independently spend the same
  actor/Organization/surface permit budget and cover saturation, timeout,
  database-clock boundaries, restart, cleanup, recovery, configuration
  mismatch/change, and fail-closed behavior.
- [ ] Green/refactor — implement the minimum approved PostgreSQL admission port,
  policy/counter migration, and host wiring while
  preserving existing safe `429` / `enrollment.rate_limited` / `Retry-After`
  behavior, using `503` / `enrollment.unavailable` for shared uncertainty, and
  keeping telemetry labels bounded.
- [ ] Run focused, integration, multi-instance, performance, security/privacy,
  regression, supply-chain, and OCI verification; reconcile documentation and
  obtain independent review.

# Current state

Ready for implementation. The consistency/readiness review found no remaining
product, requirements, UI/UX, architecture, security/privacy, or testability
blocker. Approved `REQ-SUBM-57`–`REQ-SUBM-58`, `AC-SUBM-40`–`AC-SUBM-41`, and
the ADR-018 readiness amendment select a PostgreSQL-backed application
admission port. NGINX remains transport-only and Redis remains unselected.

The next implementation step is the Red phase: recheck the migration head,
add failing PostgreSQL/multi-process/runtime tests, and record the observed
failures before implementation. Migration head is `0043` at this review; the
next additive migration is expected to be `0044` if no predecessor lands first.

The first Enrollment slice is now closed against the replica-local contract.
Its broad independent review was completed and approved outside this task.

# Decisions

- Approved `PROP-8` / ADR-018: keep the first Enrollment slice replica-local
  and implement replica-independent quota here.
- Preserve the current replica-local limiter as defense in depth until the
  shared mechanism is implemented, verified, and enabled.
- Use one PostgreSQL-backed deployment-wide policy revision and fixed-window
  counter set, with database UTC and atomic bounded acquisition.
- Defaults/ceilings remain 60 reads and 20 mutations per 10-second window.
  Deployment policy may only tighten; mismatched replica policy fails closed.
- Use 429 only for proven exhaustion. Shared admission timeout, unavailability,
  or mismatch returns non-disclosing 503 and never falls back locally.
- Counter state is short-lived protected operational state, not business audit
  history. Perform bounded indexed cleanup and emit no protected telemetry
  labels.

# Findings / deviations

- Review of `2ae4cb7` required either implementing shared/gateway quota or
  an explicit spec/ADR move. This task is that move's implementation home.
- Review of `08269b6` approved the bookkeeping; the authorized 2026-08-23
  decision subsequently approved `PROP-8` and ADR-018.
- Fresh backend/architecture/security/QA readiness review selected PostgreSQL
  because it is already the approved shared primary, keeps actor scope in the
  application, and avoids an unapproved identity-aware gateway or Redis
  dependency. Frontend behavior is unchanged: existing 429 recovery remains,
  and shared-admission uncertainty uses the existing unavailable state.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `PROP-8` / ADR-018 decided | passed | Approved 2026-08-23; authoritative requirement and ADR status updated. |
| Documentation validation | passed | `python3 scripts/check_docs.py`; `git diff --check`. |
| Requirements and acceptance readiness | passed | Approved `REQ-SUBM-57`–`REQ-SUBM-58` and `AC-SUBM-40`–`AC-SUBM-41` define scope, limits, exhaustion, uncertainty, privacy, and multi-replica evidence. |
| Architecture readiness | passed | ADR-018 amendment selects PostgreSQL fixed-window admission with deployment-wide policy, database UTC, atomic acquisition, bounded cleanup, and no local fallback. |
| Cross-cutting review | passed | Backend, architecture, security/privacy, frontend-boundary, and QA review found no implementation blocker; executable verification remains pending by design. |

# Blockers

None for implementation.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [ ] Governing specifications were rechecked
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
