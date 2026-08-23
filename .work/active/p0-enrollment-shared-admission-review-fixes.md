---
id: p0-enrollment-shared-admission-review-fixes
status: completed
created: 2026-08-23
updated: 2026-08-23
---

# Goal

Close review findings on `78c81a7`: prevent mid-window policy window changes
from resetting the shared Enrollment budget, and make hot-path cleanup
index-bounded on explicit expiry.

# Governing sources

- `docs/requirements/features/submission-attempts.md` — `REQ-SUBM-57`–
  `REQ-SUBM-58`, `AC-SUBM-40`–`AC-SUBM-41`, approved `PROP-8`
- `docs/architecture/decisions/ADR-018-enrollment-request-limit-scope.md` —
  versioned coordinated activation that cannot create overlapping budgets;
  bounded indexed cleanup
- `.work/active/p0-enrollment-shared-request-quota.md` — original
  implementation of `78c81a7`

# Scope

## In

- Freeze `window_seconds` on the deployment-wide policy for MVP (no dynamic
  window changes).
- Persist and index explicit counter expiry; bound cleanup by that index;
  skip locked rows so concurrent acquires do not contend on the same stale
  rows.
- Tests that catch the mid-window reset and prove expiry-keyed cleanup.
- Document the MVP freeze and remaining load-lab gap.

## Out

- Full revision-history policy model
- Two-OS-process lab or hosted p95 against tens of thousands of live rows
- Frontend or Playwright changes
- Redis / gateway identity quota

# Plan

- [x] Red — policy window change and expiry-index cleanup tests
- [x] Green — additive migration `0045` and matching limiter freeze
- [x] Reconcile ADR/spec notes; run focused Postgres and runtime tests

# Current state

Review findings on `78c81a7` are implemented in additive migration `0045`.
`window_seconds` cannot change. Counters carry `expires_at` with an index and
`SKIP LOCKED` cleanup. Ready for independent re-review.

# Decisions

- MVP: forbid any `window_seconds` change in the policy trigger rather than
  model overlapping revision lifetimes. Lengthening the window remains a
  future coordinated-activation design.
- Persist `expires_at` with a BEFORE INSERT/UPDATE trigger. Generated columns
  were not used because `timestamptz + interval` is not immutable in
  PostgreSQL.
- Cleanup uses `expires_at <= now ORDER BY expires_at FOR UPDATE SKIP LOCKED
  LIMIT n`.
- Do not edit landed `0044`.
- Replica-local limiter configuration must use the same frozen 10-second
  window so it cannot drift from the shared policy.

# Findings / deviations

- Two-port tests remain in-process; that gap is recorded, not claimed fixed.
- Representative latency still does not populate tens of thousands of live
  counters. The new cleanup test uses 80 live rows plus one expired row and
  asserts the expiry index exists.
- GitHub CI was not re-run from this agent.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Limiter freeze | passed | `EnrollmentRequestLimiterTests` — 6 passed |
| Shared admission | passed | `EnrollmentSharedAdmissionTests` — 8 passed, including window-freeze and expiry-index cleanup |
| HTTP 429/503 | passed | `EnrollmentHttpNegativeContractTests` — 14 passed |
| Architecture | passed | 41 passed |
| Docs | passed | `python3 scripts/check_docs.py`; `git diff --check` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
