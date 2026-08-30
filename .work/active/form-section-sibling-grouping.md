---
id: form-section-sibling-grouping
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

FormSection grouping that keeps the legend attached to its fields and marks groups without plates or fieldset padding.

# Governing sources

- `docs/ui-ux/design-system/components/inputs.md` — FormSection
- `docs/ui-ux/design-system/foundation/layout.md` — control / group / bay rungs
- `docs/ui-ux/design-system/components/cards.md` — no grouping plate

# Scope

## In

- Title-owned grouping: 2px `--hairline` under the legend words, group gap to fields
- Remove fieldset start-rail and sibling fieldset/legend pad that detached Cohort
- Spec, tests, Deck screenshots

## Out

- Plate/card around field clusters
- Changing ceremony save/activate behavior

# Plan

- [x] Red: CSS contract for title rule, no sibling fieldset chrome
- [x] Green: fields.css + docs
- [x] Focused tests + detector + Playwright screenshots

# Current state

Completed. Title-owned 2px `--hairline` underline under the legend words (`width: max-content`), not the bay. Bright `--teal` is reserved for current/selected chrome (tabs, strip).

# Decisions

- Title-only mark, not a rail or well. Fieldset block padding/borders detach the legend from fields.
- Sibling clusters use bay Stack gap; do not re-pad the legend.
- Underline color is `--hairline` (phosphor teal-gray), not `--teal`, so FormSection does not mimic selected tabs.

# Findings / deviations

- Candidate `:5274` production Setup/Create still unverified (Playwright sessions are mixed; Deck specimens and production pages share `FormSection` + `fields.css`).
- Untitled stacked `FormField`s (accommodation recipe) and `RadioGroup` legends are a different pattern; they correctly omit FormSection underlines.
- Catalog Agent fieldset can shrink to the longest legend width; the underline still tracks the title, not a bay rule.
- Unused `.ceremony-body > .composition-stack > .form-divider` in `admin-console.css` does not affect live dialogs.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused Vitest | pass | `style-entry`, `FormSection` stack+grid, Setup, Create |
| Design-lab Vitest | pass | `gallery-deck`, `CampaignConfigDialog` |
| Computed styles (Deck) | pass | 21 Setup + 10 recipes: 2px `rgba(110,154,156,0.52)`, title-only, 16px to fields, gap 6, 0 dividers |
| Playwright commission | pass | `.playwright-mcp/element-2026-08-30T10-40-46-284Z.png` |
| Playwright instrument | pass | `.playwright-mcp/element-2026-08-30T10-41-11-109Z.png` |
| Playwright ledger Grid | pass | `.playwright-mcp/element-2026-08-30T10-41-11-431Z.png` |
| Playwright Setup Cohort | pass | `.playwright-mcp/element-2026-08-30T10-42-23-114Z.png` |
| Playwright config dialog | pass | `.playwright-mcp/element-2026-08-30T10-43-09-707Z.png` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
