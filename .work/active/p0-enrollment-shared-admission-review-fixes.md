---
id: p0-enrollment-shared-admission-review-fixes
status: completed
created: 2026-08-23
updated: 2026-08-24
---

# Goal

Close review findings on `78c81a7`: prevent mid-window policy window changes
from resetting the shared Enrollment budget, make hot-path cleanup
index-bounded on explicit expiry, and make `0045` safe for a 0044 database
whose window was legitimately lengthened.

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

- Freeze the already-deployed `window_seconds` value (minimum 10 seconds). Do
  not rewrite a valid longer 0044 window to 10 seconds, and do not delete live
  counters as an upgrade recovery path.
- Persist and index explicit counter expiry; bound cleanup by that index;
  skip locked rows so concurrent acquires do not contend on the same stale
  rows.
- Tests that catch the mid-window reset, prove expiry-keyed cleanup, and prove
  `0044` → `0045` keeps a live longer-window budget.
- Document the freeze-in-place contract and remaining load-lab gap.

## Out

- Full revision-history policy model
- Two-OS-process lab or hosted p95 against tens of thousands of live rows
- Frontend or Playwright changes
- Redis / gateway identity quota

# Plan

- [x] Red — policy window change and expiry-index cleanup tests
- [x] Green — additive migration `0045` and matching limiter freeze
- [x] Reconcile ADR/spec notes; run focused Postgres and runtime tests
- [x] Freeze the deployed window in place instead of normalizing to 10 seconds

# Current state

Review findings now freeze the already-deployed window rather than rewriting
it to 10 seconds. Live 0044 longer-window counters survive `0045`; the policy
value cannot change afterward. Counters carry `expires_at` with indexed
`SKIP LOCKED` cleanup. Application configuration may use any window ≥ 10
seconds and must still match PostgreSQL exactly.

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
- Replica configuration may use any window ≥ 10 seconds. Startup already
  requires an exact match with the PostgreSQL policy, so replicas cannot drift.
- `0045` freezes whatever valid window is already stored. It does not delete
  live counters, does not rewrite 20 → 10, and does not bypass the policy
  trigger. Editing `0045` in place is acceptable only because this hash has
  not been applied outside disposable review databases; a later production
  apply of the previous `0045` would need `0046` instead.

# Findings / deviations

- Two-port tests remain in-process; that gap is recorded, not claimed fixed.
- Representative latency still does not populate tens of thousands of live
  counters. The cleanup test uses 80 live rows plus one expired row and
  asserts the expiry index exists.
- GitHub CI was not re-run from this agent.
- Whether `0044`/`0045` was applied outside disposable databases is unknown;
  this change edits `0045` in place under the pre-release assumption.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Limiter ceiling | passed | `EnrollmentRequestLimiterTests` — 21 runtime Enrollment tests passed, including minimum-window rejection and longer-window acceptance |
| Shared admission | passed | `EnrollmentSharedAdmissionTests` — 8 passed |
| Mutated 0044 upgrade | passed | `Upgrade_from_mutated_0044_keeps_the_deployed_window_and_live_counters` — red on drain refusal, then green with window=20 and live permit_count=20 preserved |
| HTTP 429/503 | passed | included in the 21 runtime Enrollment tests |
| Docs | passed | ADR/spec notes updated; `python3 scripts/check_docs.py` passed |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
