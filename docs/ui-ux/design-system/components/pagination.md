# Pagination

Hairline footer: tabular range readout, rows-per-page listbox, page selector,
previous/next keys. Foot listboxes use the same `placeFloating` path as other
selects and open upward when the viewport below the trigger cannot fit.

Cursor **Load more** for named record lists is `ItemList` `loadMore`
(`trigger="button"` or `trigger="end"`), not this footer. See [lists](lists.md).

- Preserve selection across sort/page when the table contract says so; clear
  it on search/filter.
- Disabled prev/next stay understandable.
- Announce range changes politely when the page of results changes.
