# Layout & Spacing

## Spacing System

Base unit: **4px**. Prefer this token ladder:

| Token | Value |
| --- | ---: |
| 1 | 4px |
| 2 | 8px |
| 3 | 12px |
| 4 | 16px |
| 5 | 20px |
| 6 | 24px |
| 8 | 32px |
| 10 | 40px |
| 12 | 48px |
| 16 | 64px |
| 20 | 80px |
| 24 | 96px |

Dense command surfaces need 4px granularity; do not force an 8px-only system.

## Command Deck Application Shell

Desktop application pages generally compose from:

1. global navigation rail/sidebar
2. compact top **telemetry/header rail** when useful
3. page/workspace header
4. optional contextual navigation rail
5. primary work bay
6. optional inspector/detail pane

The composition should feel like one coherent onboard system, not a webpage filled with independent cards.

### Telemetry/Header Rail

When the current context benefits from persistent operational state, a 32–44px rail may expose concise information such as current agent, session/activity state, memory mode, harness snapshot/version, live/voice state, timer/deadline, or connection/processing state.

Use compact text/mono metadata and thin separators. Do not turn the rail into a dumping ground for every available metric.

## Standard Widths

| Surface | Width |
| --- | --- |
| Global sidebar expanded | 232–260px |
| Global sidebar compact | 56–68px |
| Context rail | 220–280px |
| Inspector/detail panel | 320–420px |
| Reading column | 680–800px |
| Standard content max | 1280–1440px |
| Full workspace | fills available width |

## Hull Panels, Not Card Stacks

Prefer `surface-primary` work planes, `surface-secondary`/`surface-inset` instrument bays, 1px dividers, and one clear current-context signal rail. Avoid wrapping every toolbar, setting, paragraph, or metadata group in a floating rounded card.

## Spatial Signature

A recognizable Flex Agent workspace often includes at least two of these where appropriate:

- dark outer canvas around slightly lighter work planes
- one active blue signal rail indicating current context
- compact telemetry aligned to edges or headers
- Agent Core and bounded AI Observation Glass anchor in Agent/Session
  interaction surfaces
- precise split-pane boundaries
- faint grid/dither only in intentionally empty or identity-bearing regions

Do not add every motif to every screen.

## Responsive Behavior

- Mobile-first behavior, desktop-optimized command-deck composition.
- Multi-pane layouts collapse into stacked views or drill-in navigation.
- Telemetry rails collapse to the most decision-relevant state.
- Important actions remain reachable without horizontal scrolling.
- Tables may scroll horizontally when their semantics require columns.
- Conversation content preserves a readable line length even on very wide screens.
