# Tables

Dense datatable grammar: mono about 0.75rem (`--datatable-body-font-size`) for
body rows, state labels, timestamps, and column heads. Do not drop row text
below that token. Tabular numerals, separate borders so sticky heads travel,
opaque header fill, 18px inline gutter shared with toolbar and pagination.
Body-only dockets keep that side gutter and add only 4px of floor so the last
row clears the bottom chamfer. The etched frame has no inner well — hull pad
stays outside the plate — and keeps the bottom center tick only so the first
header row is not competing with top furniture.

The table fills the scrollport (`width: 100%`). Metric, status, and action
columns hug their content (`width: 1%` with nowrap). The identifier column
(`.cell-id`, first data header) absorbs leftover width. Stamp a named floor
with `data-col-min` / `SortableHeader colMin` / `StaticHeader colMin` /
`datatableColMin(...)` so headers and typical values do not collapse below:

| Kind | Floor | Typical columns |
| --- | --- | --- |
| `id` | 12rem | Campaign title, participant name |
| `label` | 10rem | Campaign code, assignment, reviewer |
| `state` | 9rem | Activation, Record, session state |
| `instant` | 13rem | `InstantReadout` timestamps |
| `compactId` | 9rem | Compact registry identifiers |
| `stage` | 8.5rem | BRIEFING…EXAMINATION |
| `result` | 7.5rem | PENDING / IN PROGRESS |
| `count` | 5.5rem | Enrollments, attempt |
| `rev` | 4rem | Revision |
| `confidence` | 7.5rem | Score head |
| `action` | 3rem | Icon or overflow menu |

When the floors no longer fit, the named `.datatable-scroll` region scrolls
horizontally. Gallery: `datatable-scroll`. Vertical overflow is host-owned:
filling operate tables scroll `.datatable-scroll`; a filling table in a live
overlay scrolls `.dialog-body` or `.ceremony-body` ([modals](modals.md)).

## Behavior

- Sort on heads with `aria-sort`; optional multi-column rank.
  `SortableHeader` is the sorted cell (`button.col-key`). Unsorted heads use
  `StaticHeader` (`.col-head`, same pad and type, no chevron). Do not put
  naked text in `th` — `thead th` has zero padding and will collapse.
- Hover: teal glass wash plus tick before the identifier.
- Identifier cells use `.datatable-id` as a button or a `Link`. They never
  underline; the tick is the affordance, matching Design Lab registries.
  On an index or record table the identifier opens the record. On a picker
  table it toggles row selection.
- Timestamp cells use `InstantReadout`. A readable UTC instant is a `<time>`
  mark. Missing or unreadable instants use the shared absence glyph (`—`) with
  accessible name “Not recorded”; never interpolate `undefined` or blame the
  viewer timezone.
-   Compact registry identifiers use `CompactId`. They center-truncate visually
  and reveal the exact full value on hover (`tone="value"` plaque) when the
  compact form differs from the value or the glyphs are CSS-clipped. The plaque
  uses `placeFloating` so it stays in the viewport inset, and the text is
  selectable so the full identifier can be copied. Assistive technology
  already receives the full identifier. Dense registry tables omit per-cell tab
  stops; pass `tabbable` when a standalone or short picker surface should open
  the plaque on focus-visible. Gallery: `compact-id`.
- Selection: teal select-mark; header cycles none → partial → page → matching.
- Only the object identifier and explicit keys open the record; ordinary cells
  remain selectable text.
- Expansion: named Expand/Collapse; details as a clipped-border object, not a
  nested card stack.
- Bulk actions are all-or-nothing; mixed eligibility disables with a reason.
- Production actions require server permission (`PC-09`).

## Narrow

Horizontal scroll with a named region is the default when columns do not fit,
including ≤720px. A queue may restack as labeled records (`data-label`) only
when that surface's governing spec uses the responsive-record pattern (review
queue). Never hide status or destructive consequence in overflow.

## Rules

Selection is UI state, not business truth. Frozen/sorted/filtered views do not
change server authority.
