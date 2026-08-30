# Cards and plates

Cards are smoked-glass **plates**, not floating SaaS cards.

Implementation: `web/src/design-system/components/plates/`. Gallery: `pane`,
`frame`, `empty`.

## Pane

Reusable surface: 1px hairline, sheen over depth fill, panel inset, per-corner
notch variables. Modifiers: dim bezel, notched, chamfered eight-cut.

## Etched frame

`EtchedFrame` is the operate-well **clip** for one seated instrument, not a
grouping box around already-sectioned content. Keep the well when the body
*is* the instrument: a filling registry table or list, an empty or wait
plate, a ceremony form with a docked foot, or an assignment-station work
well. Omit it (`OperateArea` `framed={false}`) when hierarchy already reads
from the operate title, readout rules, and section heads: destination /
assignment / Status Bay plate grids, split-ledger columns, and stacked
nested records (`ReadoutGrid` plus `WorkWell`s). Test: if removing the
hairline still leaves a readable hierarchy, the frame is a card — do not
draw it.

Default ticks are **bottom-center only**. Both ticks render when the frame
class is `frame-demo` (Component Deck section `frame`) or when `ticks="both"`
is passed. Side beads hang on fill wells; hug operate columns hide them so
short plaques do not read as a broken hairline. Full-bleed wells
(`board-frame`, `datatable-frame`, or `inset="flush"`) sit flush inside the
hairline. Inset tokens: `--frame-inset-block-start` /
`--frame-inset-block-end` / `--frame-inset-inline`.

## Operate area

Administrator and lab management walls compose `OperateHead` plus optional
`EtchedFrame` via `OperateArea`. That plate is the work-bay contract:

- page title (`h1`), optional description, optional `BackKey` on nested records
  trailing beside the copy cluster (title + description) at desktop widths; at
  compact widths the key sits on its own leading row above that cluster. Do not
  put `BackKey` on the breadcrumb trail.
- optional `advisory` / `context` / `headExtra`
- etched body, or `empty` → `EmptyPlate`
- Fill composition stacks those strata with bay gap (`gap="6"` /
  `--operate-bay-gap`). Do not add a second block-end pad on `.operate-head`.
  Hug columns apply the same gap on `.operate-column--hug` (the outer stack has
  one child). `headArrangement="plaque"` stays `gap="none"` so the split ledger
  can fill the hull.
- `composition="hug"` wraps the title and etched body in `.operate-column--hug`.
  `hugMeasure` (default `auto`) sets the column width:
  - `auto` — max-content, capped at 36rem / 100%. Empty wells size the copy
    track with `minmax(0, 48ch)` so the plate hugs the note and can shrink in
    a narrow landmark. Wait wells occupy the 36rem cap so a short status
    label does not collapse the etched frame or ellipsize the operate
    description while the landmark still has room. Compact viewports still
    wrap the wait label inside that well. Do not stretch empty ceremonies to
    the wait cap — that leaves a hollow well around short copy.
  - `sm` / `md` / `lg` — fixed 412 / 520 / 680px (same rungs as dialog plates)
  Operate-head inline pad equals the frame `--cut` so the title shares the
  visible top edge of the chamfer. Compact viewports (≤720px) still stretch
  ceremony hug columns across the main slot.
- `CeremonyArea` / `CeremonyEmpty` / `CeremonyWait` / `CeremonyUnavailable`
  are the page helpers for unavailable, denied, unknown-locator, sign-in,
  workspace-error, and protected loading planes. `danger` lights the title with
  fault phosphor (`fg-danger` + `danger-glow`). They always hug
  (`hugMeasure="auto"` unless the page pins a named size). Pages do not
  assemble this stack by hand. `CeremonyUnavailable` is the unknown /
  denied / missing-resource / unauthenticated-gate plane: inset empty well
  plus a recovery key centered in the well (not the note start edge, and not
  amber `open`). Return and Reload stay **quiet**. Auth **Continue to sign
  in** uses `recovery.variant="transmit"` (large).
- `headArrangement="plaque"` for the reviewer record head (back, centered
  title and seal, session id)
- `framed={false}` when the body must not nest in a second etched well.
  Destination and assignment **plate grids** (production Home destination
  catalog, production My work assignment lists, and
  design-lab Status Bays when rostered) omit the well: each assignment plate
  is already a pane. Production bays use `Grid` `fit="fill"` (`auto-fill`,
  `minItemWidth="control"`): column count comes from hull width, so one plate
  keeps a slot instead of stretching. Status Bays keep their named
  four-column hull in domain CSS (not `Grid`). Compact viewports (≤720px) use a
  single full-width column. When My work is available, production `/` redirects
  to `/my-work` instead of rendering a second roster. Nested record ledgers and **stacked nested
  records** (Enrollment detail: OperateHead, ReadoutGrid, WorkWells) also
  omit it. A nested record that *is* one seated instrument — table, empty
  plate, ceremony form, or readout fused to a `PlateFoot` — keeps the well.
  Unframed body still uses `.operate-scroll` (sibling of `OperateHead`).
  Fill composition pins `OperateHead`, context, and advisory, then wraps only
  the work body in `.operate-scroll`. Nested `.frame-scroll` overflow is not a
  second vertical scrollport: the etched payload grows with content except
  where a docked instrument (setup/create ceremony, filling registry table,
  split ledger) owns the inner overflow.
  Management bays clip `main` and pin bay chrome (title, description).
  Plate grids and stacked records scroll once in `.operate-scroll`. Any filling
  table inside that pane (production registry, review queue, lab walls) clips
  `.operate-scroll` so `.datatable-scroll` owns row overflow (and
  horizontal overflow). Short hug registries restore operate-scroll. A filling
  table in a live overlay is different: `.dialog-body` or `.ceremony-body` is the vertical wheel;
  nested `.datatable-scroll` clips Y so it cannot compete ([modals](modals.md)).
  Fill-remaining instruments (split ledger, setup
  ceremony, Status Bays) clip `.operate-scroll` the same way and scroll inside
  columns or the form well.
  Stretched `record-plane` setup ceremonies fill the remaining bay, clip
  `.frame-scroll`, and inner-scroll in `.create-ceremony__scroll` so the plate
  foot stays docked on short desktops. Four-track setup readouts and other
  plate-owned `ReadoutGrid` bands sit **inside** that etched well (group gap
  under the grid), not as a full-bleed `context` stratum above a narrower
  frame. On an unframed stacked record the same readout is the first bay
  stratum in the operate body; do not wrap it in its own plaque.
  `OperateArea` `context` remains for alerts and similar pinned notes;
  on setup/create `record-plane` those notes share the 52rem form column with
  the well. Unframed stacked nested records (Enrollment detail) fill the main
  landmark: `OperateHead` and `.operate-scroll` share the shell width.
  Nested `WorkWell` bodies on that plane overflow visible; they are not inner
  scrollports. Destination and assignment plate grids inside that pane size to content
  (`flex: 0 0 auto`) so the pane scrolls. Status Bays clip `.operate-scroll` so
  `.bays` fills the pane and each `.bay-plates` column inner-scrolls. Split-ledger `record-view` keeps
  that pane `overflow: hidden` so work columns scroll internally and the
  decision bar stays docked.
  Tables, empty plates, and assignment-station work wells keep the etched clip
  because they are one dataset or scrollport, not a second card around cards.

Shell `contain={false}` lets the bay fill the main landmark; that prop is on
the layout, not on `OperateArea`.

Pages do not assemble `OperateHead` plus `EtchedFrame` by hand. Ceremony plates
use larger padding (30–46px) and a native dialog root.

## Work well

Guided-task and assignment bodies use `WorkWell` (`article.work-well`): optional
`WorkWellHead` (title + ident) defaults to `gap="2.5"` (control rung) so title
and ident match `OperateHead` copy. Head padding uses `--frame-inset-*` on
all three edges (no 18px local block-end). Sectioned body, optional `PlateFoot`
(`arrangement="start"`) / `PlateStatusMark`. It is slot content inside
`guided-task`, not a fifth shell. Live-session transcript is hull geometry,
not a work well. `.work-well__body` overflows visible except as the direct
guided-task well child, where it is the one work scroller (`.well-frame` is
clipped). Do not leave stacked wells as nested `overflow-y: auto` inside a
management `.operate-scroll`.

Section `h3` labels are teal uppercase microlabels with
`--field-label-gap` under the title. They do **not** take a
leading instrument tick. Unordered lists inside `WorkWellSection` replace
bullets with the same 7×1px teal hairline used on session `.briefing-sec`
lists. Ordered lists keep numerals in a reserved `--space-6` grid gutter
aligned to the section inset; do not draw ticks on `ol`. When a row carries
`data-sequence`, that inspectable sequence is the visible numeral. Session
briefing overlays follow the same split (`.briefing-sec h2` unlabeled by a
tick; `.briefing-sec ul li` ticks). Gallery: `pane` WorkWell specimen.

## Plate foot

`PlateFoot` is the reserved key rail for plates. Implementation:
`PlateFoot` in `web/src/design-system/components/plates/`. It is an `Inline`
`footer` (`.plate-foot`) with a closed `arrangement`. Draw a 1px
`hairline-dim` rule on the block-start edge of an in-plate `.plate-foot`
(`hairline` defaults true) so the key rail is a distinct stratum from the
plate body (Setup, Create, assignment plates, work wells, dialog feet). The
rule is full-bleed to the plate bezel; keys stay on `--frame-inset-inline`.
Ceremony wells zero `.frame-in` inline pad when they host `.setup-ceremony`
or `.in-plate-host` and pad the host children instead, matching assignment plates.
Hull chrome that is a sibling of an already-bezeled pane omits the rule:
`PlateFoot` `hairline={false}` (`data-hairline="false"`). `GuidedTaskFoot`
defaults off because guided-task actions sit in the bay below
`.well-frame.pane`, whose bottom bezel is already the closing stroke.

The sibling immediately before a hairline foot receives
`--plate-foot-pad-block` padding on its block-end so air above the rule
matches air below it (dialog-body and work-well__body already include that
inset). Hull feet do not take that predecessor pad. Setup/Create docked feet
use the same token as `margin-block-start` on the foot so the gap sits
outside the inner scroller. Ceremony fill-grid feet (`.ceremony-foot`) use
the same token as `padding-block-start` and are not `.plate-foot`.

Dialog plates are not inside `.frame-in`. `.dialog-head`, `.dialog-body`, and
`.dialog-foot` therefore set equal `--plate-foot-pad-block` on both block
edges (inline `--frame-inset-inline`). Shared `.plate-foot` still zeros
`padding-block-end` for etched-frame floors; `.dialog-foot` overrides that
so the overlay plate has a matching floor. Work-well heads already use the
same block inset via `--frame-inset-block-start` / `--frame-inset-block-end`.
At compact viewports (≤720px) `:root` remaps `--frame-inset-block-start` and
`--frame-inset-inline` to `--space-4`; `--plate-foot-pad-block` follows the
start token, so dialog chrome stays equal on both block edges.

`PlateFoot` arrangements:

| Arrangement | Justify | Use |
| --- | --- | --- |
| `end` (default) | trailing cluster | Assignment/destination plates, dialog commit feet |
| `start` | leading cluster | Work-well continue, status lines in a plate foot |
| `center` | centered cluster | Ceremony stamp only (sign-in, empty, wait) |
| `split` | secondary leading, primary trailing | Cancel + commit pairs; guided-task chrome when both are present |

`split` takes named `secondary` and `primary` slots. Source order is secondary
then primary. A missing secondary still leaves primary on the trailing edge.
Confirm dialogs with Cancel + commit use `split`, not a trailing `KeyGroup`.
Guided-task assignment-station chrome uses `GuidedTaskFoot` (`PlateFoot` plus
`.layout-guided__actions`, `hairline` off): trailing `end` for a single key
(for example Begin intake) and `split` for Cancel intake + Submit version.
Do not add a third middle pile. Keys hug; they do not stretch on assignment
plates. Ceremony fill-grid feet (`ceremony-foot`) are a separate recipe.

Pages do not set one-off `justify-content` or `plate-foot--start` on the rail.

## Enrollment / assignment plate

`AssignmentPlate` is the destination and assignment tile: `frame-cut` at
`--notch` (no ticks or nodes), horizon `ReadoutList` (`tone="horizon"`), and a
reserved `PlateFoot` (`arrangement="end"`). Record marks follow
[status](../foundation/status.md). Production actions follow permissions, not
prototype OPEN/INSPECT labels. Design-lab Status Bays seat the same plate
inside the domain `.bays` / `.bay` / `.bay-plates` hull; they must not keep a
local `.plate` clip-path twin. Gallery: `assignment-plate`.

## Demo plate

Design-lab only. Never in the production bundle (`PC-14`).

## Rules

- No outer drop shadows.
- Avoid nested plates when a divider suffices. `FormSection` grouping is a
  2px `--hairline` underline under the legend words, not a well.
- Empty plates use the empty-state instrument, not bare text.
