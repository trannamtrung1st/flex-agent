# Content & Grid System

## Reading Widths

| Content | Max width |
| --- | ---: |
| Long conversation / narrative | 760px |
| Documentation / review text | 800px |
| Form | 640–760px unless multi-column |
| Standard application content | 1280–1440px |
| Workspace/data | full available width |

## Grid

Use responsive CSS grid. Typical gaps:

- dense metadata: 8–12px
- standard component grid: 16–24px
- wide dashboard grid: 24–32px

## Breakpoints

Implementation may align to framework breakpoints, but behavior should roughly support:

- small: 640px
- medium: 768px
- large: 1024px
- extra large: 1280px
- wide: 1536px

Do not force marketing-style centered containers onto operational workspaces.

## Responsive records

When a table row cannot preserve readable, operable meaning at a narrow width,
use a labeled stacked record rather than hiding decision-relevant columns.

- Preserve one semantic record and the same logical reading order.
- Keep identity, status, consequence, exact version/time when relevant, and the
  primary/recovery action visible.
- Use a definition list, labeled groups, or another semantic structure; do not
  emulate a table with inaccessible generic containers.
- Secondary technical detail may move into a deliberate disclosure, but status,
  permission consequence, deadline, and destructive action must not.
- A true comparison table may retain horizontal scrolling inside a named region
  when stacking would destroy the relationship between columns.
