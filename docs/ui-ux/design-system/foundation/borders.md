# Borders

## Width Scale

| Context | Width |
| --- | ---: |
| Hairline structure | 1px |
| Default controls | 1px |
| Selected/emphasis | 1px + teal or amber state treatment |
| Focus-visible ring | 1px phosphor teal with 3px offset (global key/control contract) |
| High-emphasis special boundary | second inset 1px stroke on ceremony commit keys only |

## Rules

- `border-subtle` (`hairline-dim`) is the default internal divider.
- `border-default` (`hairline`) is the bezel for plates, menus, and fields.
- Focus-visible uses `border-focus` plus offset; do not rely on a border-color
  change alone.
- **Dashed absence.** A dashed hairline marks what is not the current record:
  empty bay notes, superseded Agent originals, skeleton lines in transit. Solid
  hairlines frame what is seated. Dashed strokes never frame a live control.
- Do not use thick black borders as a general product motif.
- Navigation bezels never use amber.
