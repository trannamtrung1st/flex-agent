# Dropdown, listbox, and menus

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

Popover sheen plus overlay umbra. Foot menus open upward inside clipped frames.
Width tokens come from the select-shell family, not ad-hoc per page.

## Rules

Custom listboxes must meet accessible name, expanded state, active descendant,
and focus return. Visual Shipboard styling is not a reason to ship an
inaccessible widget (`PC-12`).
