# Borders

## Width Scale

| Context | Width |
|---|---:|
| Hairline structure | 1px |
| Default controls | 1px |
| Selected/emphasis | 1px + state treatment |
| Focus-visible ring | 2px |
| High-emphasis special boundary | 2px sparingly |

## Rules

- `border-subtle` is the default structural divider.
- `border-default` is used for bounded surfaces, menus, and secondary structural boundaries.
- `border-strong` is the default boundary for inputs, checkboxes, radios, and neutral controls when the border is necessary to identify the component against an adjacent same-colored surface; it is also used for drag targets or strong separation.
- Focus-visible uses a 2px `border-focus` ring with a 2px offset. Do not rely on a border-color change alone.
- Dashed borders are reserved for drop zones, empty insertion targets, or draft placeholders.
- Do not use thick black borders as a general product motif.
