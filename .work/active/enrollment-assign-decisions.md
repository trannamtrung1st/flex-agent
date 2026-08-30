---
id: enrollment-assign-decisions
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Record owner decisions on Participants assignment lists, and harden Close/Revoke confirmation, pending assign copy, cursor paging honesty, and lifecycle reason codes.

# Governing sources

- `docs/ui-ux/submission-attempt.md` — `UI-SUBM-DEC-13`–`UI-SUBM-DEC-16`
- `contracts/schemas/v1/enrollment/enrollment-lifecycle-command.v1.schema.json`

# Scope

## In

- Official UI/UX (and related) decision text
- Close/Revoke confirmation with required reason codes
- Correct suspend/restore/close/revoke reason codes
- Assigning Participant pending label
- Server cursor Load more (do not client-exclude enrolled identities)
- Already assigned vs second Enrollment active

## Out

- Bulk assign commit
- Client-filter of assigned identities
- Forced navigation to Enrollment detail after assign
- Removing select-all
- Server-side exclusion of enrolled identities from participant-options

# Plan

- [x] Spec decisions
- [x] Red tests
- [x] Green
- [x] Detector + Playwright (participant session only)

# Current state

Completed. Owner decisions are in `UI-SUBM-DEC-13`–`16`.

# Decisions

- Options may include enrolled identities; paging is server cursor, not load-all-then-exclude. Registry **Load more** fetches the next cursor; the Assign picker paginates the fetched page (picker Load more deferred).
- Success stays on the registry; receipt is a toast labeled **Enrollment active**, not a required detail handoff.
- Select-all stays as reserved bulk chrome; P0 commit remains one Participant.

# Findings / deviations

- Live browser on `:5274` is signed in as Demo Participant 1, so Assign/Close/Revoke were not visually exercised. Unit tests cover those states.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Tests | pass | `vitest` 32 tests: ProductionEnrollmentPage, Detail, presentation, client |
| Detector | pass | `detect.mjs --json` on both pages → `[]` |
| Playwright | blocked | `:5274` participant session; `:18080` healthy |

# Blockers

Admin Assign/Close/Revoke screenshots need an administrator session.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass (focused vitest)
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
