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
(`.cell-id`, first data header) absorbs leftover width (`width: 100%`). The
review queue is the exception: participant identifiers hug, and the assignment
title column (`.col-assignment`, `colMin="title"`) absorbs leftover width
(`width: auto`) so long titles stay readable without percentage columns summing
past the scrollport. Named floors still force horizontal scroll on
`.datatable-scroll` when they no longer fit.

Pages compose `DatatableTable`, `DatatableRow`, `DatatableCell` (`kind` plus
`colMin`), `DatatableId`, `DatatableActions`, `DatatableEmpty`, `SelectHeader`,
`ActionHeader` (icon-only action column with visually hidden label), and
`DatatableStateReadout` for state cells. Wrap tables in `DataTableShell`.
Reviewer dockets use `ReviewerQueueTableShell` (emits `.queue-datatable`) and
`ReviewerQueueEmpty` (emits `.queue-empty-plate`) in
`web/src/design-lab/components/reviewer/`. Class names
are the paint layer those components emit. Do not assemble `.datatable-table` /
`.datatable-id` / `.datatable-actions` / `.datatable-id-cell` /
`.datatable-detail-body` / `.col-select` /
`.col-action` on feature pages. `StaticHeader colMin="state"` emits
`.col-state`; `DatatableCell kind="content" colMin="result"` emits
`.cell-result` — do not pass those classes by hand. Lab enrollment RESULT
cells compose `recordResultMark` (`design-lab/components/state/`), not a
generic design-system helper. Stamp a named floor
with `DatatableCell colMin` / `SortableHeader colMin` / `StaticHeader colMin`
(or `datatableColMin(...)` on remaining `th`) so headers and typical values
do not collapse below:

| Kind | Floor | Typical columns |
| --- | --- | --- |
| `id` | 12rem | Campaign title, participant name |
| `label` | 10rem | Campaign code, reviewer |
| `state` | 9rem | Activation, Record, session state |
| `instant` | 13rem | `InstantReadout` timestamps |
| `compactId` | 9rem | Compact registry identifiers |
| `stage` | 8.5rem | BRIEFING…EXAMINATION |
| `result` | 7.5rem | PENDING / IN PROGRESS |
| `count` | 5.5rem | Enrollments, attempt |
| `rev` | 4rem | Revision |
| `confidence` | 7.5rem | Score head |
| `title` | 24rem | Assignment title, other long labels |
| `action` | 3rem | Icon or overflow menu |

When the floors no longer fit, the named `.datatable-scroll` region scrolls
horizontally, including the review queue with labeled-key Action columns. The
opaque sticky header rail stays the width of that visible
scrollport, including the inline gutters; column heads travel with the rows.
Gallery: `datatable-scroll`. Vertical overflow is host-owned:
filling operate tables scroll `.datatable-scroll`; hug registries
(`registryTableHug`, visible matching count 0–4 including empty and
search-empty) size the plate to
the current page and do not stretch a hollow scrollport. If that hugged page
still exceeds the bay, `.datatable-scroll` stays the vertical wheel so the
etched `clip-path` cannot shear rows. Operate-scroll
is the leftover scroller only when the plate actually hugs shorter than the
bay. A filling table in a live
overlay scrolls `.dialog-body` or `.ceremony-body` ([modals](modals.md)).

## Behavior

- Sort on heads with `aria-sort`; optional multi-column rank.
  `SortableHeader` is the sorted cell (`button.col-key`). Unsorted heads use
  `StaticHeader` (`.col-head`, same pad and type, no chevron). Do not put
  naked text in `th` — `thead th` has zero padding and will collapse.
  `StaticHeader colMin="state"` adds `.col-state` on the `th` for alignment
  only; pad, type, nowrap, and height stay on `.col-head`. Column heads stay
  one line at `--datatable-head-height` (36px). The table is `min-width: 100%`
  so short column sets still fill the scrollport.
- Hover: teal glass wash plus tick before the identifier.
- Identifier cells use `DatatableId` (`Link` or `button`). They never
  underline; the tick is the affordance, matching Design Lab registries.
  On an index or record table the identifier opens the record. On a picker
  table it toggles row selection.
- Timestamp cells use `InstantReadout`. A readable UTC instant is a `<time>`
  mark. Missing or unreadable instants use the shared absence glyph (`—`) with
  accessible name “Not recorded”; never interpolate `undefined` or blame the
  viewer timezone.
- Compact registry identifiers use `CompactId`. They center-truncate visually
  and reveal the exact full value on hover or primary press (`tone="value"`
  plaque) when the compact form differs from the value or the glyphs are
  CSS-clipped. Dense table cells give the host the full identifier column so
  a clipped or touch target still reaches the plaque. The plaque
  uses `placeFloating` so it can sit on the viewport edge (no inset gutter) and
  hug the full identifier, centered on the compact glyphs (not the stretched
  host). External scroll hides the plaque immediately; linger still covers pointer
  travel onto the plaque. Do not lock ordinary page scroll. The text is
  selectable so the full identifier can be copied. Assistive technology already
  receives the full identifier. Dense registry tables omit per-cell tab stops;
  pass `tabbable` when a standalone or short picker surface should open the
  plaque on focus-visible. Gallery: `compact-id`.
- Selection: teal select-mark. A page-only header cycles none/partial → page →
  none and never labels current-page IDs as all matching results. A
  matching-capable header cycles none/partial → page → matching → none only
  when the host provides a stable server or complete-local matching scope.
  Matching scope is a query descriptor plus explicit exclusions, not a browser
  requirement to load every identifier. Its exact total is optional; omit the
  number from accessible and visible copy when unknown. A consuming bulk action
  must explicitly accept and reauthorize that descriptor. The P0 Assign picker
  is page-only under `UI-SUBM-DEC-15`.
- Only the object identifier and explicit keys open the record; ordinary cells
  remain selectable text.
- Expansion: named Expand/Collapse; details as a clipped-border object, not a
  nested card stack. Production registries do not expand. Lab enrollment and
  Deck specimens own the row in `design-lab/components/datatable/`
  (`DatatableIdCell`, `DatatableExpandButton`, `DatatableDetailRow`,
  `DatatableDetailBody`, gutter hook, plus `DatatableDetailReadouts` /
  `Field` / `Keys`). Do not add expand APIs to the production barrel until a
  production table needs them.
- Bulk actions are all-or-nothing; mixed eligibility disables with a reason.
- Production actions require server permission (`PC-09`).
- Pagination is `DataTablePagination` in the shell footer. Use numbered mode when
  the host knows a complete count, whether its rows are local or server-backed;
  server ownership does not create a third visual mode. Use cursor mode for signed server pages
  (`UI-SUBM-DEC-13`): rows-per-page plus Prev/Next, no page jump, no invented
  total requirement. See [pagination](pagination.md). Do not attach `ItemList` Load more to
  a table.

## Narrow

Horizontal scroll with a named region is the default when columns do not fit,
including ≤720px. Icon or overflow-menu action columns keep a visually hidden
`th` (`Actions`, `colMin="action"`). Columns of labeled keys (`Inspect` /
`Open` / `View`) use `StaticHeader` so the head uses `.col-head` like every
other unsorted column. The review queue keeps the datatable at all widths;
named `.datatable-scroll` horizontal overflow carries stamped floors and
labeled keys. Queue participant identifiers use `compactId`. Never hide status
or destructive consequence in overflow without an approved alternate pattern.

## Rules

Selection is UI state, not business truth. Frozen/sorted/filtered views do not
change server authority.
