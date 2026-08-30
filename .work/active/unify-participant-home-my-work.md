---
id: unify-participant-home-my-work
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Stop duplicating the assignment roster on Home and My work. One index: `/my-work`. Participant `/` redirects there; gangway omits Home when My work is available.

# Governing sources

- `docs/ui-ux/activity-campaign-journey.md` (`/` Home, `/my-work` JRN-MVP-3)
- `PROP-UX-8` destinations (Home remains the admin catalog and brand locator)

# Scope

## In

- Redirect `/` to `/my-work` when My work is available
- Omit Home from available gangway destinations in that case
- Command-strip brand lands on `/my-work` for those actors
- Revert Home roster rendering; keep My work as the plate list
- Docs interim

## Out

- Deleting `/my-work` or `/` locators
- Status Bays on production
- Full IA-MVP-1 Home feed

# Plan

- [x] Red: nav omit Home; Home redirects
- [x] Green: navigation, Home page, shell homeTo, docs
- [x] Tests and live Participant My work / `/` bounce

# Current state

Completed.

# Decisions

- Canonical assignment index stays `/my-work` (JRN-MVP-3, assignment-station Return/crumbs).
- `/` remains the unauthenticated gate and administrator Home.

# Findings / deviations

- Brand accessible name stays “Home”; its href is `/my-work` for My work actors.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused tests | pass | Home, My work, navigation, App, shell — 26 tests |
| Live `/` → `/my-work` | pass | URL `/my-work`; gangway My work only; brand href `/my-work`. Desktop `.playwright-mcp/page-2026-08-30T08-48-45-290Z.png`; narrow `.playwright-mcp/page-2026-08-30T08-49-15-793Z.png` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
