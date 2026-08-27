# Layouts (application shells)

Closed set of outer page structures. Feature pages supply typed slot content.
They do not assemble command strips, gangways, bulkheads, instrument rails,
work bays, or page footers.

Implementation: `web/src/design-system/patterns/layouts/`. Route assignment is
router-owned (`PC-10`). Destinations in a gangway exist only when permitted
(`PC-09`). Accessibility follows [accessibility](../foundation/accessibility.md)
and `PC-12`. Synthetic fixtures stay out of production (`PC-14`).

## Families

| ID | Landmarks | Required slots | Optional slots | Production |
| --- | --- | --- | --- | --- |
| `management` | skip link, banner/`header` command strip, optional `nav` gangway or bulkhead, `main`, optional `footer` | command strip, main content | navigation groups, breadcrumbs, banner, footer, overlays | yes |
| `guided-task` | skip link, complementary instrument rail, `main` work well, `footer` actions | rail brand, rail instruments, heading, work well | actions, overlays | yes |
| `live-session` | skip link, complementary instrument rail, `main` transcript, composer `footer`, complementary examiner | rail brand, instruments, transcript, composer, examiner | overlays, warned/complete modifiers | yes |
| `reference` | skip link, command strip, optional index rail, `main`, optional footer | command strip, catalog/deck main | index rail, footer | design lab only |

Each family renders one root with `data-layout="<id>"`. Pages must not wrap
content in a second layout root. Component Deck specimens may nest a second
root inside a slot when demonstrating a family; wrap each specimen in
`LayoutAssignment` for that family and set `nested` so the specimen does not
emit a second skip link, `main`, or `#main-content`.

## Allowed vs forbidden

Allowed inside slots: feature headings, plates, tables, forms, Status Bays,
reviewer record grid, transcript turns, demo controls (lab only). Prefer
[layout primitives](layout-primitives.md) for inner stack, wrap, intrinsic
grid, width, and padding. Do not assemble a second `data-layout` root.

Management `children` is one `OperateArea` (`className="workspace-area"`).
That plate owns the page title (`h1`), optional description, optional
`BackKey`, optional advisory/context, and the etched body or empty plate.
Pages do not assemble `OperateHead` plus `EtchedFrame` by hand, and they do
not place a second heading stack above the operate area. Design-lab Home and
Reviewer consoles use the same plate. The reviewer record is the split-ledger variant: `OperateHead`
`arrangement="plaque"` is the prototype `record-head` (back, centered title and
seal, session id). `SplitBay` is only the three work columns. The decision bar
is a sibling foot of that bay, not a `SplitBay` `foot`. `framed={false}` so the
ledger is not nested in a second etched well. `contain={false}` lets the
queue/record unfold fill the main landmark. Live-session remains the
examination shell and is not assigned to the reviewer route.

Main content uses `Inset` with `inline="5.5"` and `block="4"` (`contain`,
`composition-inset--shell-main`, `--shell-main-inset-inline` / 22px,
`--shell-main-inset-block` / 16px) unless the page sets `contain={false}`.
Inline pad matches the command-strip brand inset so titles align with the
wordmark on full-width bays. That wrapper pads the work bay; it does not cap
width or add a bezel. Do not add a second page-edge pad on `OperateArea`,
`.wall`, or `.campaigns-wall` when the shell already insets the main slot.
Flush bays (`contain={false}`) use the same tokens through shared
`.workspace-area` padding. Inner form columns may still use `Container
size="form"`. Etched table plates stay inside that pad.

| Family | Default `contain` | Typical opt-out |
| --- | --- | --- |
| `management` | `true` | Status Bays, reviewer split bay, other full-bleed work bays |
| `guided-task` | `false` | Default fills the work well; set `true` for a readable form column |
| `live-session` | `false` | Transcript pane is hull geometry |
| `reference` catalog | `true` | Channel index and unknown-channel copy |
| `reference` deck | `false` | Component Deck specimens need the full deck column |

| Variant | Title | Description | Back | Body |
| --- | --- | --- | --- | --- |
| Console index / registry | required | recommended | omit | etched list, table, or destinations |
| Nested record | required | recommended | `BackKey` to the parent index | etched record |
| Split ledger | required (`arrangement="plaque"`) | recommended | `BackKey` in the plaque | `SplitBay` start/main/end plus sibling decision foot; `framed={false}` and `contain={false}` |
| Empty index | required (same as the populated index) | recommended | omit | `empty` plate inside the frame |

Forbidden: a page or route module importing `CommandStrip`, `ConsoleFoot`,
`Gangway`, `Bulkhead`, `AreaGroupList`, `RailBrand`, or `IndexRail` to compose
a shell; declaring reserved `.layout-*` structural selectors; selecting
`reference` in production.

## Responsive and a11y

- Management gangway collapses; at ≤1080px (`adminDrawer` / `pageScroll`) the
  layout owns a leading bulkhead. Short desktop keeps `100dvh` with inner
  scroll, not a min-height taller than the viewport.
- Guided task stacks the instrument band at ≤1080px. Rail brand stays outside
  the rail scroller on desktop.
- Live session stacks at ≤1180px and reflows with page scroll at ≤760px so
  transcript, composer, and completion stay reachable at 400% zoom.
- Contrast, keyboard, focus, reduced motion, and forced colors follow the
  foundation modules. Layout CSS also owns print overflow, forced-color
  borders, and reduced-motion scroll behavior for the four shells. State never
  relies on color alone.

## Route examples

- Production Home, Activities, later admin setup: `management`.
- Design-lab participant home, admin console, reviewer console: `management`.
- Assignment Station: `guided-task`.
- Examination Console: `live-session`.
- Channel index, Component Deck, unknown channel: `reference`.
