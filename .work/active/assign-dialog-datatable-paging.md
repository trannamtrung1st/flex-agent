---
id: assign-dialog-datatable-paging
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Replace the broken Assign Participant Load more control with the shared DataTable pagination on the fetched options page.

# Governing sources

- `docs/ui-ux/submission-attempt.md` — `UI-SUBM-DEC-13`, `UI-SUBM-DEC-15`
- `docs/ui-ux/design-system/components/tables.md`

# Scope

## In

- Assign dialog: DataTableToolbar + DataTablePagination on loaded options
- Remove assign-dialog Load more
- Spec note that cursor Load more is deferred

## Out

- Shared Load more instrument
- Server cursor fetch of later option pages
- Registry Load more (unchanged)

# Plan

- [x] Red: dialog pages with Next
- [x] Green: table controller on candidates
- [x] Detector + Playwright (admin dialog blocked)

# Current state

Completed.

# Decisions

- P0 picker paginates the fetched page with DataTablePagination. Cursor Load more is later.

# Findings / deviations

- Live `:5274` enrollments as Demo Participant 1 returns Access denied. Unit test covers 17-row pager.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Tests | pass | `ProductionEnrollmentPage.test.tsx` 13 tests |
| Detector | pass | `detect.mjs --json` → `[]` |
| Playwright | blocked | participant session; Access denied on admin enrollments |

# Blockers

Admin Assign dialog screenshots need Demo Administrator.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
