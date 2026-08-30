---
id: viewport-aware-overlays
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Floating plaques, menus, and select popovers stay inside an 8px viewport inset by flipping and shifting (and sizing menus) without a new overlay library.

# Governing sources

- `docs/ui-ux/design-system/components/tooltips-popovers.md`
- `docs/ui-ux/design-system/components/dropdown.md`
- `docs/ui-ux/design-system/foundation/accessibility.md` (400% zoom / overflow)

# Scope

## In

- Shared `placeFloating` used by `TooltipHost`, `DropdownMenu`, select/listbox popovers, and `DateTimePicker`
- Portal via existing `overlayPortalRoot`
- Spec notes for collision behavior

## Out

- Native dialog relocation
- CSS-only `[data-tip]` auto-flip
- New npm overlay library

# Plan

- [x] Failing `placeFloating` tests, then helper
- [x] Hook + `AnchoredOverlay`; wire tooltip, menus, selects, datetime
- [x] Docs + dismiss guards for portaled panels
- [x] Focused tests and Playwright on Deck CompactId + searchable select

# Current state

Review pass 2026-08-30: docs, gallery clone path, and public exports aligned with `placeFloating`. Deck rechecked menu, datetime, table-foot listbox, CompactId.

# Decisions

# Decisions

- In-house flip + shift (+ size for menus/selects). No Floating UI.
- Plaques stay nowrap; connector tracks the trigger after a horizontal shift.
- Do not copy percentage width tokens onto portaled nodes (100% becomes the viewport).

# Findings / deviations

- Stretch panels wider than the viewport are narrowed to the 8px inset.
- Canonical Compose SPA on `:18080` was not rebuilt for this source; Deck evidence is from `:5275`.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| placeFloating + tooltip/CompactId tests | passed | 60 tests |
| Playwright CompactId plaque inset | passed | left/right inside 1280×800; `.playwright-mcp/page-2026-08-30T09-32-11-275Z.png` |
| Playwright searchable select overlay | passed | 188px panel inset; `.playwright-mcp/page-2026-08-30T09-31-05-136Z.png` |
| Impeccable detect.mjs | passed | `[]` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
