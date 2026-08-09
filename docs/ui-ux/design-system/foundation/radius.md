# Border Radius & Panel Geometry

The system is engineered and precise rather than soft or bubbly.

| Token | Value | Usage |
| --- | ---: | --- |
| xs | 2px | tiny controls, telemetry chips, code tokens |
| sm | 4px | buttons, inputs, compact menus |
| md | 6px | cards, dropdowns, panels |
| lg | 8px | dialogs, large contextual surfaces |
| full | 9999px | avatars, status dots, toggles, intentional circular controls |

## Notched Geometry

A small clipped/notched corner is an optional Flex Agent signature for **special operational surfaces only**.

- notch-sm: approximately 6px
- notch-md: approximately 10px
- preferred corner: top-right or bottom-right, consistently within a component family
- use for Agent Core frames, live-session headers, telemetry plates, or high-salience inspector panels

Do not apply notches to ordinary text inputs, every button, table cell, or every card. Implementation may use pseudo-elements, masks, borders, or clip-path as long as focus outlines and content are not clipped.

## Rules

- `sm` and `md` are the default application radii.
- Avoid radii above 8px in core application UI unless the element is circular/pill-shaped.
- Do not make every badge a pill.
- Nested containers should usually have equal or smaller radius than their parent.
- Technical/log/timeline rows may use 0–2px radius when divider-based.
- Geometry should feel fabricated and instrument-like, not playful.
