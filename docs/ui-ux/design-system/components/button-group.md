# Button Groups

## Core Specs

- Display: inline-flex
- Child gap: 0 for segmented controls; 8px for action groups
- Segmented wrapper: 1px `border-strong`, `sm` radius, overflow hidden
- Segmented children: no individual outer border except separators

## Rules

- Segmented controls represent mutually exclusive display/state choices.
- Toolbars are action groups, not segmented controls; keep 4–8px gaps between actions.
- Active segmented item uses `surface-selected`, `fg-brand`, and a clear selected indicator.
- Do not use pills for every button group.
