# Icon Shapes

## Library

`lucide-react` is the approved general-purpose icon set
([ADR-019](../../../architecture/decisions/ADR-019-frontend-state-and-library-boundaries.md)
`FE-DEC-9`). This module still governs size, container use, and semantic color.
Lucide does not change those visual rules.

- Import named icons directly (`Sun`, `Moon`, and similar) so unused icons
  tree-shake.
- Size icons with the table below (`width`/`height` or an equivalent class
  mapped to those pixel sizes). Do not introduce a one-off pixel scale.
- Use semantic foreground roles (`fg-muted` by default; semantic foregrounds
  only for matching status). Do not apply raw colors.
- When an icon accompanies a visible text label, treat the icon as decorative
  (`aria-hidden="true"`). Do not duplicate the accessible name on the SVG.
- An icon-only control must have an accessible name on the control, not only
  on the graphic.
- Do not add a shared icon wrapper until repeated sizing, semantic, or
  accessibility behavior cannot stay consistent through direct use.
- Do not place every icon inside a colored shape. Use containers only when
  grouping or semantic emphasis is useful.

## Core Specs

| Size | Container | Icon |
| --- | ---: | ---: |
| XS | 24px | 14px |
| SM | 28px | 16px |
| MD | 36px | 18px |
| LG | 44px | 22px |
| XL | 52px | 26px |

- radius: sm or full according to meaning
- default background: surface-secondary
- default icon: fg-muted
- semantic variants use corresponding soft background and foreground

Do not place every icon inside a colored shape. Use containers only when grouping or semantic emphasis is useful.
