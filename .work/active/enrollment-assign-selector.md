---
id: enrollment-assign-selector
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Replace per-candidate Assign keys on the Participants registry with one
**Assign** entry that opens a scoped Assign Participant dialog. Hide already-rostered
identities. Keep nested operate copy readable.

# Governing sources

- `docs/ui-ux/submission-attempt.md` — Assign Participant
- Neighbor: Activities registry toolbar (quiet compact commit)

# Scope

## In

- Production `/activities/:id/cohorts/:id/enrollments`
- Filter candidates already on the roster
- Nested operate-head description wrap
- Optional campaign/task copy from setup load (non-blocking)

## Out

- Enrollment detail
- Server participant-options contract change

# Plan

- [x] Red: page tests require Assign + dialog commit; hide rostered candidates
- [x] Green: helpers, dialog filter, wrap CSS, campaign/task description
- [x] Focused tests + detector
- [x] Playwright MCP desktop and narrow

# Current state

Completed.

# Decisions

- Client-filter rostered `participant_actor_id` even if options still list them.
- Toolbar **Assign** opens a ceremony dialog with SelectMark + **Assign Participant**.
- Setup load is best-effort for Campaign/Task copy; registry does not wait on it.

# Findings / deviations

- Live Accessibility Standards Review cohort often has leftover options besides Morgan;
  filter must drop Morgan from the dialog. When leftover is empty, **Assign** hides.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused tests | pass | `vitest` ProductionEnrollmentPage, enrollment-presentation, production-enrollment (24) |
| detect.mjs | pass | `[]` on ProductionEnrollmentPage.tsx |
| Playwright | pass | `.playwright-mcp/page-2026-08-30T08-15-41-594Z.png` desktop; `.playwright-mcp/page-2026-08-30T08-13-51-394Z.png` narrow wrap |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
