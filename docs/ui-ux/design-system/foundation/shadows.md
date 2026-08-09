# Shadows, Depth & Emission

The application is mostly flat. Depth comes from dark surface layering, edge contrast, and rare controlled emission rather than floating SaaS cards.

| Token | Light | Dark | Usage |
|---|---|---|---|
| shadow-xs | `0 2px 6px rgba(3,10,20,.08)` | `0 2px 8px rgba(0,0,0,.30)` | sticky rail, compact popover |
| shadow-sm | `0 8px 20px rgba(3,10,20,.12)` | `0 10px 24px rgba(0,0,0,.38)` | dropdown, floating toolbar |
| shadow-md | `0 20px 48px rgba(3,10,20,.16)` | `0 24px 56px rgba(0,0,0,.46)` | modal/dialog |
| shadow-lg | `0 32px 80px rgba(3,10,20,.20)` | `0 36px 96px rgba(0,0,0,.54)` | exceptional top-level overlay |

## Emission

Blue/cyan emission is allowed only for active system meaning.

- `emission-focus`: focus-visible scanner halo; approximately 8–12px soft reach.
- `emission-selected`: very faint selected-context edge illumination; approximately 10–16px reach.
- `emission-live`: live/listening/speaking beacon; approximately 12–20px reach.
- `emission-agent`: Agent Core identity field; bounded to the core region.

Use emission colors from `colors.md` and keep opacity low. Emission belongs mostly in dark mode; light mode should rely primarily on borders/surface state.

## Rules

- Ordinary tables, settings rows, transcript items, sidebars, and static cards have no shadow by default.
- Use surface contrast and borders before elevation.
- Hover does not automatically increase elevation or emission.
- Never glow long-form text.
- Do not stack multiple large shadows and glows.
- Only a few elements should emit at once; live/current state has priority.
