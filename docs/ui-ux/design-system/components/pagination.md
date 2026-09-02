# Pagination

Hairline footer: tabular range readout, rows-per-page listbox, previous/next
keys. Numbered tables that know a complete count also expose a page selector.
Foot listboxes use the same `placeFloating` path as other
selects (no viewport inset gutter): place once, re-place on resize, dismiss
on pointer-outside, focus-leave, or external scroll, keep inner option-list
scroll open, and never lock ordinary page scroll. Flip upward only when the
full panel fits above; otherwise pin flush to the viewport (covering the
trigger is allowed). Escape restores trigger focus;
outside dismissals do not steal focus.

Numbered pagination has one visual contract whether the rows are already local
or requested one page at a time. A server-numbered host supplies the current
page rows, exact authorized total, page count, and pending state; it does not
need a third presentation mode. Search, filtering, and ordering must occur at
the same server query scope before page extraction whenever only one server
page is present in the browser.

Cursor **Load more** for named record lists is `ItemList` `loadMore`
(`trigger="button"` or `trigger="end"`), not this footer. See [lists](lists.md).

Signed-cursor **tables** (Participants registry and Assign picker,
`UI-SUBM-DEC-13`) use this footer in cursor mode: rows-per-page plus Prev/Next
mapped to `limit` and the signed `cursor`. The range is this page only
(`01–16`), not `OF` a guessed total. Do not add **Load more** to a DataTable
footer, invent a page-jump list, or page a locally accumulated window. Cursor
pagination does not require a total count. A separately authorized exact count,
when available for another purpose such as matching-scope selection, does not
turn a cursor into a numbered page or add a page jump.

- Preserve explicit selection across page changes only when the table contract
  says so. Changing search/filter invalidates a matching query scope unless the
  host deliberately creates a new scope.
- Disabled prev/next stay understandable.
- Announce range changes politely when the page of results changes.
