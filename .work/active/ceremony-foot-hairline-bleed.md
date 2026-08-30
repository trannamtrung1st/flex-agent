---
id: ceremony-foot-hairline-bleed
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Make in-plate Setup/Create `PlateFoot` hairlines full-bleed to the etched bezel, matching assignment plates and dialogs. Seat lab guided-task actions on `GuidedTaskFoot`.

# Governing sources

- `docs/ui-ux/design-system/components/cards.md` — Plate foot full-bleed rule
- `docs/ui-ux/design-system/foundation/borders.md` — `hairline-dim` internal divider

# Scope

## In

- Zero frame-in inline pad when it hosts `.setup-ceremony`; pad ceremony children, not the foot rule
- Keys stay on `--frame-inset-inline`
- Lab journey actions use `GuidedTaskFoot` (`hairline` off)
- Docs: cards.md, change-record

## Out

- Compact guided-task stacking / foot below the fold
- Horizon / FormSection / ReadoutGrid row hairlines (content grammar, not plate strata)

# Plan

- [x] Red: CSS contract for setup ceremony bleed + lab GuidedTaskFoot
- [x] Green: plates.css + JourneyPage + docs
- [x] Verify: focused Vitest + Playwright gallery + lab journey

# Current state

Completed. Ceremony wells use the same bleed recipe as assignment plates.

# Decisions

- Shared selector is `.frame-in:has(.setup-ceremony)`, so Create, Setup, and Deck form recipes inherit it.
- Lab journey keys use `GuidedTaskFoot`; the synthetic demo note stays a sibling, not inside the foot.

# Findings / deviations

- Production `/activities/new` on `:5274` was a participant session (Access denied). Create/Setup live product pages were not opened as an administrator. Gallery `#form-recipes` and `#layout-management-setup` are the clone specimens of those plates.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused Vitest | pass | `EtchedFrame.test.tsx`, create/setup related tests (33); design-lab `pc-surfaces` journey foot |
| Playwright Setup/Create clone | pass | `#form-recipes` and `#layout-management-setup`: foot vs `.frame-in` widthDelta 0, leftDelta 0; frame pad 0; keys 24px desktop / 16px compact. Artifacts: `.playwright-mcp/page-2026-08-30T09-22-16-090Z.png`, `.playwright-mcp/page-2026-08-30T09-24-41-273Z.png`, `.playwright-mcp/page-2026-08-30T09-26-28-922Z.png` |
| Lab GuidedTaskFoot | pass | `data-hairline=false`, 0px border, aligned to well pane. `.playwright-mcp/page-2026-08-30T09-25-48-927Z.png` |
| Detector | pass | `detect.mjs --scope layout` empty |

# Blockers

None remaining for the in-scope composition.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
