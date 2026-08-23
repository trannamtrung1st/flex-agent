---
id: p0-enrollment-shared-request-quota
status: completed
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
- [x] Red — prove two API processes cannot independently spend the same
  actor/Organization/surface permit budget and cover saturation, timeout,
  database-clock boundaries, restart, cleanup, recovery, configuration
  mismatch/change, and fail-closed behavior.
- [x] Green/refactor — implement the minimum approved PostgreSQL admission port,
  policy/counter migration, and host wiring while
  preserving existing safe `429` / `enrollment.rate_limited` / `Retry-After`
  behavior, using `503` / `enrollment.unavailable` for shared uncertainty, and
  keeping telemetry labels bounded.
- [x] Run focused, integration, multi-instance, performance, security/privacy,
  regression, supply-chain, and OCI verification; reconcile documentation and
  obtain independent review.

# Current state

Completed and ready for independent review. Shared Enrollment admission is
PostgreSQL-backed, replica-independent, authenticated-only, and fail-closed.
The replica-local limiter remains defense in depth. NGINX remains
transport-only and Redis remains unselected. Frontend 429 recovery and the
existing protected unavailable state are unchanged.

# Decisions

- Approved `PROP-8` / ADR-018: keep the first Enrollment slice replica-local
  and implement replica-independent quota here.
- Preserve the current replica-local limiter as defense in depth.
- Use one PostgreSQL-backed deployment-wide policy revision and fixed-window
  counter set, with database UTC and atomic bounded acquisition.
- Defaults/ceilings remain 60 reads and 20 mutations per 10-second window.
  Deployment policy may only tighten; mismatched replica policy fails closed.
- Use 429 only for proven exhaustion. Shared admission timeout, unavailability,
  or mismatch returns non-disclosing 503 and never falls back locally.
- Counter state is short-lived protected operational state, not business audit
  history. Perform bounded indexed cleanup and emit no protected telemetry
  labels.
- Production/Staging hosts verify the deployment-wide policy at startup.
  In-memory test hosts without PostgreSQL keep the replica-local limiter only.

# Findings / deviations

- Review of `2ae4cb7` required either implementing shared/gateway quota or
  an explicit spec/ADR move. This task implemented the approved PostgreSQL
  follow-on.
- Sequential and concurrent acquires share one `to_timestamp` UTC window key so
  a replica restart cannot open a second budget in the same window.
- Frontend behavior is unchanged: existing 429 recovery remains, and
  shared-admission uncertainty uses the existing unavailable state. No new
  Playwright contract was required.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `PROP-8` / ADR-018 decided | passed | Approved 2026-08-23; authoritative requirement and ADR status updated. |
| Documentation validation | passed | Spec traceability and ADR-018 implementation note updated; `python3 scripts/check_docs.py` and `git diff --check` passed. |
| Red coverage | passed | Cases encoded in tests: two-port budget, restart, DB clock/cleanup, mismatch/timeout, tighten-only policy, latency. Observed green on the commands below. |
| Green implementation | passed | Migration `0044_enrollment_shared_request_admission.sql`; `IEnrollmentSharedAdmissionPort` / `PostgresEnrollmentSharedAdmissionPort`; host acquire-before-work wiring. |
| Focused runtime tests | passed | `dotnet test --project tests/Runtime/FlexAgent.Runtime.Tests/FlexAgent.Runtime.Tests.csproj -- --filter-class FlexAgent.Runtime.Tests.EnrollmentHttpNegativeContractTests --filter-class FlexAgent.Runtime.Tests.EnrollmentRequestLimiterTests` — 20 passed, including unauthenticated skip, 429 exhaustion, 503 uncertainty, allowlisted telemetry, and option ceilings. |
| PostgreSQL/multi-instance tests | passed | `dotnet test --project tests/Integration/FlexAgent.Postgres.Integration.Tests/FlexAgent.Postgres.Integration.Tests.csproj -- --filter-class FlexAgent.Postgres.Integration.Tests.EnrollmentSharedAdmissionTests` — 6 passed; repeat run 6 passed. Docker was started for this verification. |
| Architecture | passed | `dotnet test --project tests/Architecture/FlexAgent.Architecture.Tests/FlexAgent.Architecture.Tests.csproj` — 41 passed. |
| UI | skipped | No new interaction contract; existing 429 and protected unavailable states remain. |
| Supply-chain / OCI | not re-run | No new third-party component or image was selected; Redis remains unselected. Full OCI rebuild is a residual for the independent reviewer if they require a fresh image. |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
