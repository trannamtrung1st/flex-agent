# Shadows, Depth & Emission

The application is flat smoked glass. Depth comes from translucent fills, inset
edge-light, and rare phosphor glow — not floating SaaS cards.

| Token | Light | Dark | Usage |
| --- | --- | --- | --- |
| shadow-none | none | none | plates, tables, rails, transcript cards |
| panel-sheen | low-opacity cool highlight | `linear-gradient(115deg, rgba(215,227,234,0.045), transparent 34%)` | plate illumination |
| panel-depth | light inset wash | `linear-gradient(200deg, rgba(2,9,14,0.58), rgba(5,14,19,0.24))` | plate fill |
| panel-inset | subtle inner edge | `inset 0 1px 0 rgba(246,252,254,0.05), inset 0 -18px 34px rgba(2,9,14,0.5)` | glass vignette |
| shadow-overlay | `0 14px 30px rgba(4,12,17,0.18)` | `0 14px 30px rgba(2,9,14,0.55)` | menus/dialogs seating over chrome — overlay umbra, not card lift |

## Emission

Teal/amber emission is allowed only for active system meaning.

- `emission-focus`: focus-visible halo; approximately 8–12px soft reach.
- `emission-selected`: very faint selected-context edge; approximately 10–16px.
- `emission-live` / `emission-agent`: Agent Core and genuine live state.
- `emission-attention`: timer digits, hot commit keys, validation bezels.

Use emission colors from `colors.md` and keep opacity low. Structural chrome —
hairlines, dividers, resting keys, readout text — never glows.

**Emitters-only glow.** Glow belongs to things that emit in the fiction: the
core, timer digits, gauge fill, a hot commit key, and backlit Michroma signage.

## Rules

- Ordinary tables, settings rows, transcript items, sidebars, and plates have
  no outer drop shadow by default.
- Hover does not automatically increase elevation or emission.
- Never glow long-form text.
- Do not stack multiple large shadows and glows.
- Only a few elements should emit at once; live/current/attention has priority.
