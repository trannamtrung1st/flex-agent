# Cards and plates

Cards are smoked-glass **plates**, not floating SaaS cards.

Implementation: `web/src/design-system/components/plates/`. Gallery: `pane`,
`frame`, `empty`.

## Pane

Reusable surface: 1px hairline, sheen over depth fill, panel inset, per-corner
notch variables. Modifiers: dim bezel, notched, chamfered eight-cut.

## Etched frame

`EtchedFrame` is the operate-well clip. Default ticks are **bottom-center
only**. Both ticks render when the frame class is `frame-demo` (Component Deck
section `frame`) or when `ticks="both"` is passed. Full-bleed wells
(`board-frame`, `datatable-frame`, or `inset="flush"`) sit flush inside the
hairline. Inset tokens: `--frame-inset-block-start` /
`--frame-inset-block-end` / `--frame-inset-inline`.

## Operate area

Administrator and lab management walls compose `OperateHead` plus optional
`EtchedFrame` via `OperateArea`. That plate is the work-bay contract:

- page title (`h1`), optional description, optional `BackKey` on nested records
- optional `advisory` / `context` / `headExtra`
- etched body, or `empty` → `EmptyPlate`
- `headArrangement="plaque"` for the reviewer record head (back, centered
  title and seal, session id)
- `framed={false}` when the body must not nest in a second etched well

Shell `contain={false}` lets the bay fill the main landmark; that prop is on
the layout, not on `OperateArea`.

Pages do not assemble `OperateHead` plus `EtchedFrame` by hand. Ceremony plates
use larger padding (30–46px) and a native dialog root.

## Work well

Guided-task and assignment bodies use `WorkWell` (`article.work-well`): optional
`WorkWellHead` (title + ident), sectioned body, optional `PlateFoot` /
`PlateStatusMark`. It is slot content inside `guided-task`, not a fifth shell.
Live-session transcript is hull geometry, not a work well.

## Enrollment / assignment plate

Readout `<dl>` with shared horizon geometry and a reserved key foot. Record
marks follow [status](../foundation/status.md). Production actions follow
permissions, not prototype OPEN/INSPECT labels.

## Demo plate

Design-lab only. Never in the production bundle (`PC-14`).

## Rules

- No outer drop shadows.
- Avoid nested plates when a divider suffices.
- Empty plates use the empty-state instrument, not bare text.
