---
id: setup-note-remaining-polish
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Finish remaining Setup Note consistency: shared copy, activated Deck specimen, readiness blockers as ErrorSummary.

# Governing sources

- `docs/ui-ux/assessment-campaign-setup.md` — Blocked: **Readiness blocked**; summary before fields
- `docs/ui-ux/design-system/components/alerts.md` — do not stack teal Notes
- `docs/ui-ux/design-system/components/error-summary.md`

# Scope

## In

- `SETUP_RESOLVED_NOTE` on fieldFormat; gallery + station import it
- Gallery activated Setup composition
- Readiness blockers: ErrorSummary at top of form, amber Error, links

## Out

- Production OIDC sign-in
- New Alert warning skin

# Plan

- [x] Red: blocker summary, shared copy, gallery activated
- [x] Green
- [x] Live Deck evidence + detector

# Current state

Completed.

# Decisions

- Blockers use ErrorSummary (**Readiness blocked**), not Alert warning, so provenance Note stays the only teal Note.
- Copy lives with `CAMPAIGN_TITLE_PLACEHOLDER` so design-lab can import it.
- PlateFoot `end` arrangement renders `children`, not `primary` (match production Assign Participants).

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Tests | pass | AssessmentSetupPage, setupStation, gallery-deck 28 |
| Playwright | pass | Deck `#layout-management-setup` desktop `.playwright-mcp/element-2026-08-30T09-15-33-725Z.png` |
| Detector | pass | `[]` |

# Blockers

None. Production `:5274` Setup still Sign in required.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
