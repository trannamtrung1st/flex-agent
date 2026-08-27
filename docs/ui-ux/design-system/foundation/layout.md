# Layout & Spacing

## Spacing System

Base unit: **2px** for instrument rhythm, expressed through this ladder. Prefer
the documented steps rather than arbitrary offsets.

| Token | Value |
| --- | ---: |
| 1 | 4px |
| 2 | 8px |
| 2.5 | 10px |
| 3 | 12px |
| 4 | 16px |
| 5 | 20px |
| 5.5 | 22px |
| 6 | 24px |
| 6.5 | 26px |
| 8 | 32px |
| 10 | 40px |
| 12 | 48px |
| 16 | 64px |
| 20 | 80px |
| 24 | 96px |

Prototype rhythm uses 8 / 10 / 16 / 22 / 26px. Map those onto this ladder.
Dense command surfaces need 4px granularity; do not force an 8px-only system.

Inner composition consumes this ladder through `Stack`, `Inline`, `Grid`,
`Container`, `Inset`, and `SplitBay` ([layout primitives](../components/layout-primitives.md)).
CSS variables `--space-1` … `--space-24` live in `web/src/styles/tokens.css`.
Do not invent off-ladder offsets in feature CSS when a primitive `gap` or
`Inset` space will do.

## Application shells

Desktop pages compose from **exactly one** of these Shipboard shells. The set
is closed. New production pages select a family; they do not invent another
outer structure. Product routes and destinations still follow the approved
Activity IA, not prototype paths (`PC-10`). Shared React implementations live
in `web/src/design-system/patterns/layouts/` and are documented in
[layouts](../components/layouts.md).

1. **Management** (`management`) — command strip, optional gangway, primary
   work bay, quiet footer.
2. **Guided task** (`guided-task`) — full-height instrument rail (hull
   bulkhead on desktop) plus inset work well; compact identity.
3. **Live session** (`live-session`) — full-height instrument rail, inset
   transcript column, inset examiner/Agent plate.
4. **Reference** (`reference`) — catalog index / Component Deck only (design
   lab). Forbidden in the production entry graph.

Inner feature composition (tables, Status Bays, reviewer record grid, gallery
specimens) lives inside named slots and is not a fifth shell. Prefer the
[layout primitives](../components/layout-primitives.md) for stack, wrap, intrinsic
grid, width, padding, and named split tracks inside those slots. Keep Status Bays
and live-session columns in domain or shell CSS. Reviewer record columns use
`SplitBay`. Management work bays use `OperateArea` for title, description, optional back, and body; see
[layouts](../components/layouts.md). Management and reference-catalog mains
wrap slot content in an even `Inset` by default (`contain`, `--shell-main-inset-inline`
/ 22px, `--shell-main-inset-block` / 16px); hull shells and the Component Deck do not. `contain={false}` is flush
to the main landmark unless the page reapplies the same hull token through
`.workspace-area`.

### Command strip

A 48px min-height top chrome: wordmark, role-home tokens, operator disclosure.
Current destination uses a 2px teal underline bar plus text, not color alone.

### Gangway and bulkhead

- Expanded gangway: `--gangway-w` 232px default; the administrator shell sets
  248px.
- Collapsed gangway: `--gangway-w-collapsed` 76px channel-code rail with
  trailing tooltips.
- Bulkhead drawer: `--bulkhead-w` 280px default; a shell may use 420px wide.
  At drawer widths (≤1080px for management/assignment, ≤1180px for the live
  session console), swap persistent side navigation for a stacked instrument
  band or leading bulkhead. Do not collapse past 76px or hide destinations
  behind an unlabeled hamburger without the bulkhead pattern.

### Participant instrument rails

On desktop, Assignment Station and Examination Console left rails are hull
bulkheads: they meet the viewport on the top, bottom, and leading edges.
The work bay keeps about 18px block inset. Frame traces are clipped to the
work-bay column, not the viewport. The session examiner plate stays an inset
work plane, not a second bulkhead.

Short desktop viewports scroll instruments inside the rail (`.phase-rail-scroll`
/ `.rail-scroll`); brand and seated rail actions stay outside that scroller. Desktop shells use
`height: 100dvh` with no min-height taller than the viewport, so a short window
cannot grow past the hull while `body` overflow stays hidden. Narrow/drawer
widths stack the instrument band in the header and do not make that band
viewport-sticky.

### Telemetry / readout

Persistent operational state uses compact readout stacks and readout grids,
not a dumping ground of every metric. Campaign timezone rules in governing
specs win over decorative date formats (`PC-11`).

## Standard Widths

| Surface | Width |
| --- | --- |
| Gangway expanded | 232px default (`--gangway-w`); 248px administrator shell |
| Gangway collapsed | 76px (`--gangway-w-collapsed`) |
| Bulkhead | 280px (`--bulkhead-w`); 420px wide |
| Participant assignment instrument rail | 260px (`--instrument-rail-width`) |
| Participant session instrument rail | 232px (`--instrument-rail-width`) |
| Context / manifest rail | 200–260px |
| Examiner / inspector plate | 280–320px (session examiner column is 320px) |
| Reading column | 68–78ch; ~680–800px |
| Dialog narrow / default / wide | 412 / 520 / 680px |
| Standard content max | fills shell; Campaign record hugs content (about 52rem min on desktop) |
| Full workspace | fills remaining width |

## Hull Panels, Not Card Stacks

Prefer smoked-glass planes, 1px hairlines, and one clear current-context tick
or node. Avoid wrapping every toolbar, setting, paragraph, or metadata group in
a floating rounded card.

**Shared horizon.** Sibling plates in a row share divider and key-foot
geometry so a lone plate does not balloon beside crowded neighbors.

## Spatial Signature

A recognizable Flex Agent workspace often includes at least two of these where
appropriate:

- near-black canvas around smoked-glass work planes
- phosphor-teal current-context tick, underline, or node
- compact telemetry aligned to rails or headers
- Agent Core on Session/examiner surfaces
- precise split-pane hairlines
- hairline traces with circular node terminals on frames or phase spines

Do not add every motif to every screen.

## Responsive Behavior

- Desktop-optimized command-deck composition with stacked narrow behavior.
- Multi-pane layouts collapse into stacked views or bulkhead navigation.
- Assignment Station stacks at ≤1080px (`bp.pageScroll`). Examination Console
  stacks at ≤1180px (`bp.wideGrid`) and reflows with page scroll at ≤760px
  (`bp.session`) so transcript, composer, and completion controls stay reachable
  at narrow width and 400% zoom.
- Important actions remain reachable without hiding under overflow.
- Tables may scroll horizontally when columns are required; surrounding
  actions stay reachable.
- Conversation content preserves a readable line length on very wide screens.
- Container queries (for example readout grids below 46rem) adapt inside
  frames independently of viewport width.
- 400% zoom must reflow; clip-path notches must not hide focus or actions.
