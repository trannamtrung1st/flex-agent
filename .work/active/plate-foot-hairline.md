---
id: plate-foot-hairline
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Give every `PlateFoot` a shared internal hairline above the key rail, matching existing dialog and work-well feet, including Setup and Create.

# Governing sources

- `docs/ui-ux/design-system/components/cards.md` — Plate foot
- `docs/ui-ux/design-system/foundation/borders.md` — `hairline-dim` internal divider
- `docs/ui-ux/design-system/components/modals.md` — DialogPlateFooter uses PlateFoot

# Scope

## In

- Canonical `border-block-start` on `.plate-foot` (full-width rail)
- Remove duplicate borders on `.dialog-foot` and `.work-well__foot`
- Keep `.ceremony-foot` (not plate-foot) on the same token
- Docs: cards.md plate foot + change-record

## Out

- Mixed key sizes (unchanged)
- Local Setup-only borders

# Plan

- [x] Red: CSS contract for shared `.plate-foot` hairline
- [x] Green: plates.css + drop duplicates
- [x] Docs + live Setup/Create/dialog/assignment

# Current state

Completed. `.plate-foot` is the single hairline; dialog and work-well inherit it.

# Decisions

- One rule on `.plate-foot` (`width: 100%` + `hairline-dim` block-start); other feet inherit or match the token, no double stroke.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused Vitest | pass | EtchedFrame + DialogPlate CSS contracts |
| Playwright Create | pass | 1px hairline; `.playwright-mcp/page-2026-08-30T08-24-11-921Z.png` |
| Playwright Setup | pass | foot width = ceremony 782px, `1px solid` hairline-dim; `.playwright-mcp/page-2026-08-30T08-24-40-316Z.png` |
| Playwright Home assignment | pass | same token on `.assignment-plate-keys` |
| Dialog feet | pass | `.dialog-foot.plate-foot` computed same 1px (hidden confirm rails on Setup) |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
