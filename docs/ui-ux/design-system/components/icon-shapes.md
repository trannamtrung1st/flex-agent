# Icon shapes

## Library

`lucide-react` is the approved general-purpose icon set
([ADR-019](../../../architecture/frontend-architecture.md)
`FE-DEC-9`). This module governs size, color, and when a custom glyph is
required (`DS-DEC-10`, `PC-13`).

- Import named icons directly so unused icons tree-shake.
- Size with the table below. Do not introduce a one-off pixel scale.
- Default color: `fg-muted`; teal or amber only for matching state.
- When an icon accompanies a visible text label, treat the icon as decorative
  (`aria-hidden="true"`).
- An icon-only control must have an accessible name on the control.
- Do not place every icon inside a colored shape.

## Custom glyphs (rationed)

Use authored SVG/clip-path only for brand mark, operator glyph, state nodes,
wait-mark, phase complete/lock, document version, seal ring, warning triangle,
and equivalent domain marks. Match Lucide stroke weight (~1–1.1px, square
caps) so the two sources sit in one system.

## Document icon (favicon)

The browser tab / bookmark mark is a custom glyph, not a Lucide icon.

- Serve `web/public/favicon.svg` from both the production shell (`web/index.html`)
  and the design-lab shell (`web/design-lab.html`) as `/favicon.svg`, using
  `<link rel="icon" type="image/svg+xml" href="/favicon.svg" />`.
- Keep the Shipboard Terminal mark: square 32×32 hull-ground tile (`#07141b`),
  1px phosphor-teal inner frame, phosphor-teal letterform (`#3cc0bf`). No
  `border-radius` and no v0.1 electric-blue fill.
- Do not restore the superseded Deep-Space rounded “F” tile.

## Core Specs

| Size | Hit target | Graphic |
| --- | ---: | ---: |
| XS | 24px | 11–14px |
| SM | 28px | 16px |
| MD | 36px | 18px |
| LG | 44px | 22px |
| XL | 52px | 26px |

Dense table overflow controls may use a 22px visible glyph over a ≥24px
(prefer 44px) target.
