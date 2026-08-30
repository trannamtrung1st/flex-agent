---
id: create-campaign-ds-gaps
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Align Create assessment Campaign with the nested ceremony / setup-create design-system contract: seated empty well, stock status marks, no page-only CSS hook.

# Governing sources

- `docs/ui-ux/design-system/components/layouts.md` (nested ceremony / setup-create)
- `docs/ui-ux/design-system/components/cards.md` (OperateArea empty / context)
- `docs/ui-ux/design-system/product/empty-loading.md`
- `docs/ui-ux/design-system/components/inputs.md` (field hint vs status)

# Scope

## In

- Missing required source category: keep `record-plane--setup` etched well and `OperateArea` `empty`
- Development eligibility: `StateReadout` as field sibling, not `FormField` hint
- Remove `.create-eligibility-note`

## Out

- Create/submit behavior, source binding rules, Deck recipe rewrite, other pages

# Plan

- [x] Red: page tests for empty well, hint vs mark, no eligibility class
- [x] Green: OperateArea empty, sibling StateReadout, drop CSS hook
- [x] Focused tests + Playwright on candidate `:5274`
- [x] Impeccable detect.mjs once

# Current state

Completed. Missing-source empty well covered by unit tests only (seeded API always has a full source set).

# Decisions

- Missing sources uses `OperateArea` `empty` (inset `EmptyPlate`) rather than a context `Alert`.
- Mixed development stays a `StateReadout` beside the field, not helper hint copy.

# Findings / deviations

- Live seed shows the plate-level development note (`Listed revisions are development only.`). Mixed-berth marks and missing-category empty plate were not reachable on the running API.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `pnpm test -- src/pages/AssessmentCampaignCreatePage.test.tsx` | pass | 12 tests |
| `pnpm test -- src/styles/style-entry.test.ts` | pass | 17 tests |
| Playwright candidate `http://localhost:5274/activities/new` | pass | `.playwright-mcp/page-2026-08-30T08-14-54-745Z.png` (populated), `page-2026-08-30T08-16-01-605Z.png` (validation), `page-2026-08-30T08-16-26-064Z.png` (narrow) |
| `detect.mjs` | pass | `[]` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
