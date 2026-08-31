# Lists

Use hairline-divided rows, teal hover wash, and a teal tick or node for the
**current** interactive item (nav, menus, selected options). Do not use large
rounded list tiles.

## Prose lists

Prose lists in `WorkWell` and session briefing use the 7×1px teal hairline as
the unordered bullet only. On `seat="stack"` wells that bullet shares the
7px + 12px mark column with populated rows. An empty titled stack well does
not render `EmptyPlate`; it uses one prose line under the head. Ordered lists in those wells keep numerals in a
`--space-6` gutter (CSS counter, or `data-sequence` when the row is an
inspectable lineage). Row copy stacks with `Stack` inside each `li`. Section
headings in those surfaces do not share that mark. Submission version lineage
uses that same ordered list in the production assignment surface; it is not
`ItemList`.

## Item list

`ItemList` is the shared record-row primitive for named collections that are
not tables. Callers pass data plus `renderItem` so row content stays generic
(keys, inlines, readouts) while the list owns `<ul>` / `<li>` chrome.

- `items` + `itemKey` + `renderItem` — `renderItem` returns row content, not
  the `<li>`.
- Interactive rows still need a named control (link or key). Row whitespace
  is not an implicit button unless the spec says the whole row is the target.
- Optional `loadMore.onLoadMore` requests the next page. `onClick` is an alias.
  `trigger` defaults to `button`: a trailing quiet compact **Load more** key
  that stretches the row with a centered caption (`waiting` uses occupied key
  state). `trigger="end"` omits the key and observes a sentinel at the list
  foot with `IntersectionObserver`. When the sentinel intersects the nested
  scrollport (or the viewport if `scroll` is off), it calls `onLoadMore` once
  until `waiting` clears or `loadMore` is removed. Occupied end-trigger uses a
  polite centered status, not a second key.
- Rows own a single `--space-4` gutter (block and inline). Hairlines span the
  list. Seat the list in `EtchedFrame inset="flush"` so rows meet the bezel;
  do not double-pad with a default frame inset.
- Optional `scroll` names a region (`"{label}, scrollable"` by default) with
  contained overflow. Use it when the list is seated inside another scroll
  parent (Component Deck, dialog body). Datatable paging stays
  [DataTablePagination](pagination.md); do not use `ItemList` as a table
  footer.

Deck specimen: `item-list`. The Deck demos delayed `waiting` so the occupied
Load more key and end-trigger `WaitPanel` are inspectable.

- Locked items use the lock glyph plus text; they do not advance lifecycle
  (`PC-07`).
- Protected or unauthorized items use the non-disclosing unavailable pattern.
