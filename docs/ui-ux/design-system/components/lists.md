# Lists

Use hairline-divided rows, teal hover wash, and a teal tick or node for the
**current** interactive item (nav, menus, selected options). Do not use large
rounded list tiles.

Prose lists in `WorkWell` and session briefing use the 7×1px teal hairline as
the unordered bullet only. Ordered lists in those wells keep numerals in a
`--space-6` gutter (CSS counter, or `data-sequence` when the row is an
inspectable lineage). Row copy stacks with `Stack` inside each `li`. Section
headings in those surfaces do not share that mark. Submission version lineage
uses that same ordered list in the production assignment surface; it is not a
design-system list component.

- Interactive rows need a named control (link or key). Row whitespace is not
  an implicit button unless the spec says the whole row is the target.
- Locked items use the lock glyph plus text; they do not advance lifecycle
  (`PC-07`).
- Protected or unauthorized items use the non-disclosing unavailable pattern.
