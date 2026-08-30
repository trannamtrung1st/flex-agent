---
id: participants-assign-dialog
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Replace per-candidate Assign keys on the Participants registry with a form dialog that holds an assignable-Participant table. Polish empty copy and compact operate description wrap.

# Governing sources

- `docs/ui-ux/activity-campaign-journey.md` `JRN-MVP-2`
- `docs/ui-ux/design-system/components/modals.md`
- `docs/ui-ux/design-system/components/tables.md`
- `docs/ui-ux/design-system/components/buttons.md`

# Scope

## In

- Toolbar **Assign** opens `CeremonyDialog` + `DialogPlate` with a candidate table
- Single-row select + footer **Assign Participant** (bulk/filter later)
- Do not hide candidates already on the roster
- Search-empty copy uses Participants wording
- Compact operate description wraps

## Out

- Search/filter inside the dialog
- Multi-row bulk assign API loop
- Visual redesign of the registry table

# Plan

- [x] Red: enrollment page tests for dialog, copy, roster candidates
- [x] Green: dialog + CSS wrap
- [x] Verify: Vitest + Playwright MCP

# Current state

Completed. Toolbar **Assign** opens a wide `CeremonyDialog` table of every option from `listCandidates`. Duplicate assign stays a backend concern.

# Decisions

- Toolbar label is **Assign**; dialog title and commit are **Assign Participant**.
- Selection is one row for now; header select-all stays out.
- Backend remains the authority for duplicate assignment.
- Candidate list is not filtered against the current roster.

# Findings / deviations

- Helpers `assignableEnrollmentCandidates` remain in `enrollment-presentation.ts` for other callers; this page no longer uses them.
- A concurrent edit briefly reintroduced roster filtering; the page now lists all candidates again.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Vitest enrollment page | pass | 9 tests, `ProductionEnrollmentPage.test.tsx` |
| Playwright dialog desktop | pass | `.playwright-mcp/page-2026-08-30T08-14-17-667Z.png` registry; `page-2026-08-30T08-14-32-989Z.png` dialog; `page-2026-08-30T08-17-03-445Z.png` selected enrolled row |
| Playwright search empty | pass | `.playwright-mcp/page-2026-08-30T08-17-38-049Z.png` **No matching Participants** |
| Playwright compact wrap | pass | `.playwright-mcp/page-2026-08-30T08-18-04-979Z.png` 390px description wraps |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review

# Remaining gaps

- Dialog filter and bulk assign stay out of scope.
- Did not complete a live POST assign in Playwright; selection + disabled-to-enabled commit was verified.
- `SelectMark` click targets the wrapping cell; the hidden checkbox input is intercepted by the label.
