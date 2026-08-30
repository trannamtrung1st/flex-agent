# Button groups

Labeled sibling-key clusters use `KeyGroup`: wrapping `Inline` with gap `2.5`
(`10px`), `role="group"`, class `.key-group`. Height comes from each `Key`
`size`, not a shared min-height on the cluster. Wrapping children keep content
size; do not stretch grouped keys to fill the row.

Plate and dialog action rails use [`PlateFoot` arrangement](cards.md#plate-foot),
not a second grouping primitive. `KeyGroup` may sit inside a foot slot;
`justify` on `KeyGroup` is for in-slot clustering (for example table toolbars),
not for choosing the plate’s leading/trailing/center rail.

Gallery: `key-group`. Dialog, ceremony, and decision feet may constrain the
group; they do not invent a second grouping primitive.

- One commit key maximum in the group unless the governing spec requires two
  equal-weight choices (then both stay quiet until confirmation).
- Destructive actions stay visually quiet until confirmation.
- Keyboard: Tab moves among keys unless the governing spec uses a toolbar
  pattern with arrow keys.
- Disabled members remain in tab order only when their unavailable reason must
  be reachable; otherwise omit them. Pair disabled keys with `TooltipHost` /
  `disabledReason` so the reason is available without hover-only.
