---
id: dialog-tooltip-top-layer
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Value plaques from `TooltipHost` / `CompactId` must be visible inside a modal `<dialog>` (Assign Participant Actor column), and the same portal root must apply to other body-portaled overlays.

# Governing sources

- `docs/ui-ux/design-system/components/tooltips-popovers.md`
- `docs/ui-ux/design-system/components/tables.md` (CompactId)

# Scope

## In

- `overlayPortalRoot` for TooltipHost and fixed DropdownMenu
- Tests and Assign / registry / Deck screenshots

## Out

- Viewport-edge clamping of plaques that sit near the right of a wide dialog

# Plan

- [x] Failing test: plaque is a descendant of the open dialog
- [x] Shared `overlayPortalRoot`
- [x] Docs + Playwright

# Current state

Completed.

# Decisions

- Portal to `closest("dialog")`, not into `.dialog-plate` (clip-path would clip a fixed plaque).
- Fixed `DropdownMenu` uses the same root so row menus in a dialog would not sit under the scrim.

# Findings / deviations

- Assign picker now lists many synthetic candidates; header select-all disables commit when more than one row is selected.
- Plaques near the right of a wide dialog can clip against the viewport; not changed in this pass.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Vitest CompactId, keys, overlayPortalRoot, enrollment page | passed | 53 tests |
| `tsc -b --noEmit` | passed | |
| Deck CompactId plaque on body | passed | `.playwright-mcp/page-compact-id-plaque.png` |
| Assign Actor CompactId plaque in dialog | passed | `.playwright-mcp/page-assign-actor-plaque.png`; parent `DIALOG` |
| Assign header-select plaque in dialog | passed | `.playwright-mcp/page-assign-header-plaque.png` |
| Registry enrollment CompactId on body | passed | `.playwright-mcp/page-registry-enrollment-plaque.png` |
| Assign commit one vs many | unit passed; live header click blocked by visually-hidden input | ProductionEnrollmentPage.test; Playwright used `.select-head` overlay |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
