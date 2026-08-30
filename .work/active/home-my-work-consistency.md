---
id: home-my-work-consistency
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Align recovery, operator identity, docs, and dead Home wells with the unify rule: when My work is available, `/` redirects to `/my-work` and that path is operational home.

# Governing sources

- `docs/ui-ux/activity-campaign-journey.md` (`IA-MVP-1` interim, `/` locator, `JRN-MVP-3`)
- `.work/active/unify-participant-home-my-work.md`

# Scope

## In

- `identity.home` and authenticated recovery keys follow `productionWorkspaceHome`
- Docs that still implied a second Home roster or Home on every gangway
- Dead Home `my-work` destination well

## Out

- New Home-work feed
- Changing unauthenticated `homeTo: "/"`
- Deleting `/` or `/my-work` locators

# Plan

- [x] Docs: navigation model, sidebars, result-release, implementation-guide, submission-attempt, layouts
- [x] Operator identity, guards, unknown locator, contract-unavailable recovery, Home wells, tests
- [x] Live administrator Home; participant brand href (shared-cookie limit)
- [x] `DESIGN.md` regenerate

# Current state

Completed.

# Decisions

- Denied My work still recovers to `/` (workspace home when `my-work` is closed).
- Unauthenticated chrome stays on `/`.
- Authenticated shells use `identity.home` from available destinations.

# Findings / deviations

- Playwright `:5274` tabs share cookies. Opening administrator `/` this pass overwrote the participant session for later navigations. Participant `/` redirect was already evidenced in unify; this pass confirmed brand `href=/my-work` on the remaining Participant SPA and administrator destination Home.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused tests | pass | 36 tests: operator, routes, App, Home, navigation, shell, ContractUnavailable |
| Live administrator `/` | pass | Gangway Home + Activities; Activities plate only; brand `/`. Desktop `.playwright-mcp/page-2026-08-30T08-56-46-803Z.png` |
| Live administrator unknown locator | pass | Snapshot recovery `href=/` (`.playwright-mcp/page-2026-08-30T08-58-39-503Z.yml`) |
| Live participant `/` hop this pass | skipped | Shared cookies with administrator session |
| Live participant brand | pass | `href=/my-work` on Demo Participant `/my-work` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
