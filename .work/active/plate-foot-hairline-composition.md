---
id: plate-foot-hairline-composition
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Stop assignment-station hull chrome from drawing a floating in-plate hairline, and keep in-plate `PlateFoot` rules full-bleed to the bezel.

# Governing sources

- `docs/ui-ux/design-system/components/cards.md` — Plate foot, assignment plate
- `docs/ui-ux/design-system/components/layouts.md` — guided-task footer is a sibling of the work well
- `docs/ui-ux/design-system/foundation/borders.md` — `hairline-dim` internal divider

# Scope

## In

- `PlateFoot` `hairline` param (default on)
- `GuidedTaskFoot` defaults hairline off (hull sibling of `.well-frame.pane`)
- Assignment-plate foot rule full-bleed; keys stay inset
- Docs: cards.md + change-record

## Out

- Docking guided-task actions inside the pane
- Changing key copy or intake behavior
- Horizon readout row hairlines (content grammar, remain inset)

# Plan

- [x] Red: PlateFoot hairline contract + GuidedTaskFoot default + assignment-plate bleed
- [x] Green: component, CSS, docs
- [x] Verify: focused tests + Playwright assignment station + home plates

# Current state

Completed. In-plate rails keep the rule; hull chrome omits it.

# Decisions

- Guided-task actions stay a bay sibling (layouts.md). They must not draw `.plate-foot`’s internal divider because that rule cannot T-junction with the pane bezels.
- `hairline` defaults true so dialog, work-well, ceremony, and assignment plates stay unchanged unless they opt out.

# Findings / deviations

- Compact station screenshot cropped the bay foot below the fold; desktop intake evidence covers Cancel intake.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused Vitest | pass | EtchedFrame, AssignmentPlate, layouts, ProductionMyWorkDetail, style-entry |
| Detect | pass | `[]` |
| Playwright gallery / home plates | pass | Foot 1px from bezel (frame stroke); `.playwright-mcp/page-2026-08-30T09-03-30-092Z.png`, `.playwright-mcp/page-2026-08-30T09-06-18-282Z.png`, `.playwright-mcp/page-2026-08-30T09-08-52-594Z.png` |
| Playwright assignment station | pass | `data-hairline=false`, 0px foot border, 18px bay gap; desktop Begin intake `.playwright-mcp/page-2026-08-30T09-09-36-237Z.png`; Cancel intake `.playwright-mcp/page-2026-08-30T09-10-23-671Z.png`; compact `.playwright-mcp/page-2026-08-30T09-11-07-134Z.png` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
