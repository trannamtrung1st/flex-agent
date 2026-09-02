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

Cursor **Load more** for named record lists is `ItemList` `loadMore`
(`trigger="button"` or `trigger="end"`), not this footer. See [lists](lists.md).

Signed-cursor **tables** (Participants registry and Assign picker,
`UI-SUBM-DEC-13`) use this footer in cursor mode: rows-per-page plus Prev/Next
mapped to `limit` and the signed `cursor`. The range is this page only
(`01–16`), not `OF` a guessed total. Do not add **Load more** to a DataTable
footer, invent a page-jump list, or page a locally accumulated window.

- Preserve selection across sort/page when the table contract says so; clear
  it on search/filter.
- Disabled prev/next stay understandable.
- Announce range changes politely when the page of results changes.
