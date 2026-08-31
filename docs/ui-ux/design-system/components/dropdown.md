# Dropdown, listbox, and menus

Implementation: `web/src/design-system/components/select/`, `menu/`, and `temporal/`.
Gallery: `searchable-select`, `multiselect`, `menu`, `datetime`.

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

## Overlay plate

Every portaled select, command menu, and datetime overlay is a **closed plate**:
complete `--hairline` bezel on all four sides. Seat it with
`OVERLAY_PLATE_OFFSET` (`-1`) so the overlay hairline covers the trigger-adjacent
field bezel — below covers the bottom edge, above covers the top. Horizontal
`align` (`start` / `center` / `end` / `stretch`) does not apply a side overlap;
these plates do not open left or right. Never fuse by punching the shared
edge. Inner row and footer dividers stay `--hairline-dim`. A panel foot
(Done/Clear) is an inner separator; it does not change the outer bezel.
Datetime calendar and clock heads do not restroke the plate top.
Datetime plates also drop the 1px top inset from `--panel-inset` so that
highlight does not double the bezel.
Compose overlays with `overlayPlateClass` (`select-popover popover-surface
menu-surface`) plus the widget’s own modifiers. Do not author a local overlay
bezel. Tooltip plaques keep their own gap.

## Placement

Popover sheen plus overlay umbra. Do not CSS-position a new popover against
its trigger. Clone `AnchoredOverlay` / `placeFloating`: portal through
`overlayPortalRoot`, then flip to the opposite side only when that side fits
the full panel. If neither side fits, pin flush to the viewport edge (no
inset gutter); covering the trigger is allowed. Cap height to the viewport
and scroll inside only when the panel is taller than the viewport. Place
once on open; window
resize re-runs `placeFloating`. External page, window, visual-viewport, or
ancestor scroll dismisses the overlay; scrolling inside the panel (searchable
option lists) keeps it open. Do not lock ordinary page scroll. Escape
restores trigger focus; outside pointer, focus-leave, and external-scroll
dismissal do not steal focus. Hull chrome (`command-strip`, Deck
`page-strip`) stays above the overlay so an open trigger does not paint over
the header. Overlays portal into `#root` (not `document.body`) so that chrome
stacking context can win. Foot listboxes open upward when the full panel fits above
the trigger. After portal, resolve percentage `--select-popover-*` tokens against the
trigger box (never the viewport). Stretch and min-width lock are a floor at
the trigger, not a cap: `max(100%, 16rem)` stays at least 16rem on a hug
mark. Authored `--select-popover-max-width` only lets a plate grow past the
trigger (long option labels); it does not shrink a wider field. The viewport
is the hard cap. Do not copy raw `%` onto the floating node. Shell recipes:

- Field (`align="stretch"`): plate width matches the trigger's outer box.
  Hug overrides may raise the floor (`max(100%, 16rem)`).
- Context / toolbar (min-width lock, no stretch): at least the trigger
  shell (and the toolbar 16rem / foot 148px floor); long labels may grow up
  to the authored max. Place against the shell, not the inner key. Do not
  add ±1px / `100% + 2px` seam compensation.
- Command menu: at least the trigger; `max-width: none` is the viewport, not
  the mark.
- Datetime plate and identifier plaques: do not lock min-width to the trigger.

`DropdownMenu` `placement` stays on the API; both `connected` and `fixed` use that path. Gallery:
`searchable-select`, `multiselect`, `menu`, `datetime`.

## Rules

Custom listboxes must meet accessible name, expanded state, active descendant,
and focus return on Escape or commit (`PC-12`). Outside-pointer, focus-leave,
and external-scroll dismissal must not steal focus. Visual Shipboard styling
is not a reason to ship an inaccessible widget.
