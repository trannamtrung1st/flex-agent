---
id: etched-frame-clip-rule
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Treat `EtchedFrame` as a clip for one seated instrument, not a grouping box. Unframe stacked nested records (OperateHead + ReadoutGrid + WorkWells) and document the same rule for every matching surface.

# Governing sources

- `docs/ui-ux/design-system/components/cards.md`
- `docs/ui-ux/design-system/components/layouts.md`
- `docs/ui-ux/design-system/components/content.md`
- `docs/ui-ux/design-system/foundation/layout.md`
- Confirmed clip-vs-grouping rule (2026-08-30)

# Scope

## In

- Design-system composition rule (clip vs grouping)
- Production Enrollment detail (`framed={false}`)
- Design-lab management-record specimen
- Tests and Playwright evidence
- Consistency pass across docs, skills, CSS, gallery, and live surfaces

## Out

- Ceremony forms (setup, create, form recipes)
- Registries, empty/wait plates, assignment-station wells
- Lab Campaign record plate with ReadoutGrid + PlateFoot

# Plan

- [x] Red: Enrollment and gallery tests require unframed stacked nested records
- [x] Green: `framed={false}`, wrap Enrollment body in bay Stack, docs
- [x] Focused tests
- [x] Playwright: Enrollment production and design-lab management record
- [x] detect.mjs on changed UI files
- [x] Unframed stacked records fill the main landmark; 52rem stays on setup/create ceremonies
- [x] Consistency review: docs, skills, dead CSS, live surfaces

# Current state

Rule is in cards, layouts, content, and foundation layout. Enrollment and the management-record specimen are unframed stacked records. Setup/create stay one etched well at 52rem. ReadoutGrid is a rule band, not its own plaque.

# Decisions

- Clip vs grouping: if removing the hairline still leaves title, readout rules, and section heads, omit the frame.
- Unframed stacked records fill the main landmark; setup/create keep the 52rem form column.
- Do not wrap a stacked-record `ReadoutGrid` in its own `EtchedFrame`. The grid is a rule band, not a plaque; the clip is only when that readout is fused into one instrument (setup/create, or readout + `PlateFoot`).

# Findings / deviations

- Full design-lab suite still has pre-existing `keys.css` digest drift and a PC-11 Home timezone failure unrelated to this change.
- Playwright MCP tabs drift to design-lab; measure the production tab after each navigation.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Enrollment + OperateArea + setup/create unit tests | passed | vitest |
| gallery-deck (design-lab config) | passed | 26 tests |
| style-entry CSS contract | passed | 17 tests |
| detect.mjs | passed | `[]` |
| Playwright gallery: stacked record 0 frames; setup fused grid in frame; index/empty framed; split unframed | passed | live `#layout-management-*` |
| Playwright Enrollment desktop | passed | 0 `.frame-cut`; grid not in frame; 1148px vs 52rem 832; 4-col; `.playwright-mcp/page-2026-08-30T01-00-35-403Z.png` |
| Playwright Setup ceremony | passed | 1 frame; grid in frame; scroll 832px; `.playwright-mcp/page-2026-08-30T01-00-09-966Z.png` |
| Playwright Create ceremony | passed | 1 frame; scroll 832px; `.playwright-mcp/page-2026-08-30T00-59-31-694Z.png` |
| Playwright Activities registry | passed | 1 frame; scroll 1148px |
| Playwright Home plate grid | passed | 0 frames |
| Playwright lab Campaign record | passed | 1 frame; readout + foot inside clip |
| Full design-lab suite | skipped | pre-existing unrelated failures |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
