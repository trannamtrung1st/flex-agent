---
id: setup-resolved-note-alert
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Seat Setup provenance on the shared Note (`Alert` info / `Advisory`) at the top of the ceremony form, not a floating field-hint.

# Governing sources

- `docs/ui-ux/design-system/components/alerts.md` — Note strip
- `docs/ui-ux/assessment-campaign-setup.md` — Resolved from …

# Scope

## In

- Setup: Alert Note at top of form, grouped with Campaign title; merge into activated Alert
- Deck setup specimen + Alert gallery provenance spec
- Docs: alerts.md, inputs.md, typography.md, assessment-campaign-setup.md, change-record.md

## Out

- New note primitive
- Changing field-hint size globally
- OperateArea page `advisory` (would sit above tracks)

# Plan

- [x] Red: Setup Note alert, not field-hint
- [x] Green: station, gallery, docs
- [x] Live evidence

# Current state

Completed. Frozen-cluster provenance uses `Alert` Note.

# Decisions

- Standing provenance is `Alert` (`Note` + 0.78rem advisory copy). Draft: info Alert before Campaign title, grouped with the title at `--form-group-gap`. Activated: same sentence in the existing Cohort activated Alert body so two teal Notes do not stack.
- Do not use OperateArea `advisory`: that strip belongs with the operate head, above Setup tracks.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Tests | pass | `AssessmentSetupPage.test.tsx`; `pnpm exec vitest run --config vitest.design-lab.config.ts src/design-lab/features/gallery/gallery-deck.test.tsx` (28 passed) |
| Playwright (review 2026-08-30) | pass | Deck `#alert` `.playwright-mcp/element-2026-08-30T08-59-51-493Z.png`; `#layout-management-setup` desktop `.playwright-mcp/element-2026-08-30T09-00-21-781Z.png`, narrow `.playwright-mcp/element-2026-08-30T09-01-57-209Z.png`. Production `:5274` Setup: Sign in required. |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
