# Tables

Tables are a core workspace primitive.

## Wrapper

- background: surface-primary
- optional 1px border-subtle/default
- radius: md only when bounded as a standalone object
- no shadow by default
- horizontal overflow when required

## Header

- background: surface-secondary or sticky surface-primary
- text: fg-muted, 12–13px, 600
- bottom border: 1px border-default
- cell padding: 10–12px horizontal, 8–10px vertical

## Body

- text: 13–14px
- row divider: 1px border-subtle
- cell padding: 10–12px horizontal, 9–12px vertical
- hover: surface-hover when rows are inspectable/clickable
- selected: surface-selected

## Numeric / Technical Columns

- use tabular numerals
- use mono when values are machine identifiers or technical metrics
- right-align comparable numeric columns

## Rules

- Do not use zebra striping by default.
- Sticky first/last columns require clear divider treatment.
- Row action menus should not dominate scanning.
- Status values follow `status.md`.
- Use semantic table markup for true row/column data, including header scopes. Do not make an entire table row the only inaccessible click target; provide a keyboard-operable link/button or equivalent row interaction semantics.
