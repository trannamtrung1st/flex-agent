# Dropdown, listbox, and menus

Implementation: `web/src/design-system/components/select/` and `menu/`.
Gallery: `searchable-select`, `multiselect`, `menu`.

## Option menu (single-select)

Hairline rows, teal-glass hover/focus, Bright Text plus 7×1px teal tick on the
selected option. Selected rest is the tick, not the fill. Fully keyboard
operable (arrows, Enter, Escape, typeahead where specified).

## Command menu

Same popover surface without listbox ticks. Rows are commands. Destructive
labels stay `fg-default` until confirmation. Production omits unapproved
export/delete (`PC-09`).

## Searchable select / multiselect

Field or context plate; commit on pick for single-select. Multiselect uses teal
select-marks and explicit Done/Clear. Do not treat the widget as Campaign
authority (`PC-06`).

## Placement

Popover sheen plus overlay umbra. Do not CSS-position a new popover against
its trigger. Clone `AnchoredOverlay` / `placeFloating`: portal through
`overlayPortalRoot`, then flip, shift, and size into an 8px viewport inset.
Foot listboxes open upward when the viewport below the trigger cannot fit the
panel. After portal, match trigger width in pixels; do not reuse percentage
`--select-popover-width` tokens on the floating node (they resolve against the
viewport). `DropdownMenu` `placement` stays on the API; both `connected` and
`fixed` use that path. Gallery: `searchable-select`, `multiselect`, `menu`.

## Rules

Custom listboxes must meet accessible name, expanded state, active descendant,
and focus return. Visual Shipboard styling is not a reason to ship an
inaccessible widget (`PC-12`).
