# Tables

Dense datatable grammar: mono about 0.72rem, tabular numerals, separate
borders so sticky heads travel, opaque header fill, 18px inline gutter shared
with toolbar and pagination.

## Behavior

- Sort on heads with `aria-sort`; optional multi-column rank.
- Hover: teal glass wash plus tick before the identifier.
- Selection: teal select-mark; header cycles none → partial → page → matching.
- Only the object identifier and explicit keys open the record; ordinary cells
  remain selectable text.
- Expansion: named Expand/Collapse; details as a clipped-border object, not a
  nested card stack.
- Bulk actions are all-or-nothing; mixed eligibility disables with a reason.
- Production actions require server permission (`PC-09`).

## Narrow

At workspace widths, horizontal scroll with a named region is allowed. At
≤720px, some queues may restack as labeled records (`data-label`) when the
governing spec uses the responsive-record pattern. Never hide status or
destructive consequence in overflow.

## Rules

Selection is UI state, not business truth. Frozen/sorted/filtered views do not
change server authority.
