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
- [x] Refuse `0045` while live counters still store a pre-change window duration
- [x] Refuse `0045` until overlapping frozen-policy budgets end, not just the old counter expiry

# Current state

`0045` backfills `expires_at` and refuses freeze using
`window_start + max(stored window, deployed window)`, so a 10-second row that
has already expired on its own duration still blocks until the overlapping
frozen-policy budget ends. Recovery remains wait-only. After that overlap
ends, the deployed window (including a valid 20-second `0044` policy) is
frozen in place.

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
  trigger. If a mismatched counter still overlaps the frozen policy window,
  migration fails until that overlap ends (`window_start + max(stored,
  deployed)`). Editing `0045` in place is acceptable only
  because this hash has not been applied outside disposable review databases;
  a later production apply of the previous `0045` would need `0046` instead.

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
| Mutated 0044 upgrade | passed | `Upgrade_from_mutated_0044_keeps_aligned_exhausted_counters_controlling_acquisition`, `Upgrade_from_0044_refuses_live_old_window_counters_then_freezes_after_natural_expiry`, and `Upgrade_from_0044_refuses_old_window_counters_until_the_frozen_policy_window_ends` — 3 passed |
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
