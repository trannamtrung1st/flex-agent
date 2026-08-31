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
hairline. Fused instrument interiors add `.plate-bleed` (`SetupCeremony`,
`InPlateHost`) so the generic frame clip can bleed the foot hairline; do not
teach `:has(.setup-ceremony)` / `:has(.in-plate-host)` in `plates.css`. Inset
tokens: `--frame-inset-block-start` /
`--frame-inset-block-end` / `--frame-inset-inline`.

## Operate area

Administrator and lab management walls compose `OperateHead` plus optional
`EtchedFrame` via `OperateArea`. Select host geometry with `bay` (`workspace`
default). `record` paints `record-plane`. Closed `bay` values: `workspace`,
`record`, `registry`, `ceremony` (frozen; keep `CeremonyArea` in this system;
do not add bays). Setup and create use `SetupOperateArea` in
`web/src/components/work/` (emits `record-plane--setup` on the record bay).
Lab walls, home board, Deck form recipes, and reviewer
queue/ledger use domain wrappers (`CampaignsOperateArea`,
`EnrollmentWallOperateArea`, `SampleWallOperateArea`, `HomeBoardOperateArea`,
`FormRecipeOperateArea`, `ReviewerQueueOperateArea`,
`ReviewerLedgerOperateArea`). Replacement hosts use `OperateAreaHost`;
`FormRecipeOperateArea` adds owned `form-recipe` on `OperateArea`.
`ReviewerLedgerOperateArea` paints `record-view`. Production My work uses
`AssignmentBoardOperateArea`.
`hug` on `OperateArea` is `"registry"` only and emits `registry-wall--hug`
when `bay="registry"`. Lab wall/queue wrappers still take
`hug={registryTableHug(...)}` and add that class themselves. Empty
assignment boards pass `hug="board"` on those board wrappers, not on
`OperateArea`. Pages do not author host class strings. Select etched-frame
geometry with `frame` when the body uses a shared well:

| `frame` | Emitted classes | Default inset |
| --- | --- | --- |
| `record` | `record-frame` | default |
| `registry` | `datatable-frame registry-frame` | flush |
| `datatable` | `datatable-frame` | flush |
| `ceremony` | `ceremony-frame` | default |

`frameClassName`, `headClassName`, and `hostClassName` stay on `OperateAreaHost`
for lab/domain wrappers, not a production page API. That plate is the work-bay contract:

- page title (`h1`), optional description, optional `BackKey` on nested records
  trailing beside the copy cluster (title + description) at desktop widths; at
  compact widths the key sits on its own leading row above that cluster. Do not
  put `BackKey` on the breadcrumb trail.
- optional `advisory` / `context` / `headExtra`
- etched body, or `empty` → `EmptyPlate`
- Fill composition stacks those strata with bay gap (`gap="6"` /
  `--operate-bay-gap`). Do not add a second block-end pad on `.operate-head`.
  Hug columns apply the same gap on `.operate-column--hug` (the outer stack has
  one child). `ReviewerLedgerOperateArea` passes `gap="none"` so the split ledger
  can fill the hull.
- `composition="hug"` wraps the title and etched body in `.operate-column--hug`.
  `hugMeasure` (default `auto`) sets the column width:
  - `auto` — max-content, capped at 36rem / 100%. Empty wells size the copy
    track with `max-content` (capped at 48ch on the note) so the plate hugs
    the note and recovery key. Do not stretch empty ceremonies to the wait
    cap — that leaves a hollow well around short copy. Wait wells occupy the
    36rem cap so a short status label does not collapse the etched frame or
    ellipsize the operate description while the landmark still has room.
    Compact viewports still wrap the wait label inside that well.
  - `sm` / `md` / `lg` — fixed 412 / 520 / 680px (same rungs as dialog plates)
  Operate-head inline pad equals the frame `--cut` so the title shares the
  visible top edge of the chamfer. Compact viewports (≤720px) still stretch
  ceremony hug columns across the main slot; empty notes wrap inside that
  stretched well.
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
- `ReviewerLedgerOperateArea` for the reviewer record head (`OperateHead`
  `arrangement="plaque"`: back, centered title and seal, session id)
- `framed={false}` when the body must not nest in a second etched well.
  Destination and assignment **plate grids** (production Home destination
  catalog, production My work assignment lists, and
  design-lab Status Bays when rostered) omit the well: each assignment plate
  is already a pane. Production bays use `AssignmentBays` (`web/src/components/work/`) plus `Grid`
  `fit="fill"` (`auto-fill`, `minItemWidth="control"`): column count comes from hull width, so one plate
  keeps a slot instead of stretching. Status Bays use lab `StatusBays` / `StatusBay`
  and keep their named four-column hull in domain CSS (not `Grid`). Each `.bay`
  uses even `--form-group-gap` gutters so the column hairline sits between equal
  air; do not remap `--frame-content-pad-inline-end` (flush-table overlay-thumb
  track) onto those columns, and do not add a second inline-end pad on
  `.bay-plates`. Compact viewports (≤720px) use a
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
  horizontal overflow). Hug registries (visible matching count 0–4, including
  a true empty table and search-empty) size the etched plate to toolbar,
  empty plate or rows, and pagination — they do not stretch a hollow
  scrollport to fill the bay. If that hugged page still exceeds the bay, the
  plate caps at the bay and `.datatable-scroll` remains the wheel; do not
  clip-path-shear overflow. Count **visible matching** rows (`slice.total`),
  not the unfiltered loaded list. Use `registryTableHug` on production
  registries, lab campaign/enrollment walls, and the review queue.
  A filling table in a live overlay is different: `.dialog-body` or `.ceremony-body` is the vertical wheel;
  nested `.datatable-scroll` clips Y so it cannot compete ([modals](modals.md)).
  Overlay `.dialog-body` and setup inner scrollports reserve
  `scrollbar-gutter: stable`. Overlay `.dialog-body` that keeps overlay thumbs
  still gets `--space-3` inline-end air when it hosts a filling table.
  Campaign configuration `.ceremony-body` does not reserve a gutter: the
  overlay thumb sits on the plate’s inner inline-end.
  Fill-remaining instruments (split ledger, setup
  ceremony, Status Bays) clip `.operate-scroll` the same way and scroll inside
  columns or the form well.
  Stretched `SetupOperateArea` ceremonies fill the remaining bay, clip
  `.frame-scroll`, and inner-scroll in `.create-ceremony__scroll` so the plate
  foot stays docked on short desktops. Four-track setup readouts and other
  plate-owned `ReadoutGrid` bands sit **inside** that etched well (group gap
  under the grid), not as a full-bleed `context` stratum above a narrower
  frame. On an unframed stacked record the same readout is the first bay
  stratum in the operate body; do not wrap it in its own plaque.
  `OperateArea` `context` remains for alerts and similar pinned notes;
  on setup/create (`SetupOperateArea`) those notes share the 52rem form column with
  the well. Unframed stacked nested records (Enrollment detail) fill the main
  landmark: `OperateHead` and `.operate-scroll` share the shell width.
  Nested `WorkWell` bodies on that plane overflow visible; they are not inner
  scrollports. Destination and assignment plate grids inside that pane size to content
  (`flex: 0 0 auto`) so the pane scrolls. Status Bays clip `.operate-scroll` so
  `.bays` fills the pane and each `.bay-plates` column inner-scrolls. On a
  stretched (non-dense) plate, wrapped horizon copy overflows inside
  `.readout-stack--horizon` so `PlateFoot` keeps `--plate-foot-pad-block` and
  does not shrink; dense and ≤1080px plates hug content and do not add that
  inner wheel. Split-ledger `record-view` keeps
  that pane `overflow: hidden` so work columns scroll internally and the
  decision bar stays docked.
  Tables, empty plates, and assignment-station work wells keep the etched clip
  because they are one dataset or scrollport, not a second card around cards.

Shell `contain={false}` lets the bay fill the main landmark; that prop is on
the layout, not on `OperateArea`.

Pages do not assemble `OperateHead` plus `EtchedFrame` by hand. Overlay
ceremony plates follow [modals](modals.md): confirm plates use
`--plate-foot-pad-block` and `--frame-inset-inline`; campaign fill-grid uses
`--space-6` on head, body, and foot.

## Work well

Guided-task and assignment bodies use `WorkWell` (`article.work-well`): optional
`WorkWellHead` (title + ident) defaults to `gap="2.5"` (control rung) so title
and ident share OperateHead copy rhythm. Title size is `titleRole` on
`WorkWellHead` (`"plate"` | `"task"`), not a CSS seat selector: stack infers
`"plate"` (H2 / plate title, 0.72rem, same as `FormSection` legend); pane infers
`"task"` (1.05rem seated-task name). Pass `titleRole` only to override. The
resolved role is `data-title-role` on `WorkWellHead` so custom head children
that use `.work-well__title` still pick up the size. Choose
**seat**, not mark:

- `seat="stack"` — unframed nested records (`framed={false}`). Resolves
  `inset="flush"` and head `mark="title"`: a 2px `--hairline` under the title
  + ident cluster (`width: max-content`, capped at the well). Do not shrink
  ident to the title width — a short title must not wrap a sentence ident
  into a column. Keep idents to one short line so the cluster does not reach
  the bay edge.
- `seat="pane"` — the well fills a bezel (guided-task `.well-frame`). Resolves
  `inset="frame"` and head `mark="span"`: `--frame-inset-*` plus a full-width
  1px `--hairline-dim`. Pass `inset="flush"` only when parent `frame-in` already
  pads — flush zeros all well-owned `--frame-inset-*` (head, body, and foot).
  Remaining air is `--space-2` under a span ident and `--form-group-gap` into
  the body. Do not set `mark` unless overriding a documented exception.

Sectioned body, optional `PlateFoot` (`arrangement="start"`) / `PlateStatusMark`.
`WorkWellHead` `seal` is an optional node ahead of title copy (lab
`WorkWellReleasedSeal` for published-result specimens). Do not embed
release chrome in the generic well. The well is slot content inside
`guided-task`, not a fifth shell. Live-session
transcript is hull geometry, not a work well. `.work-well__body` overflows
visible except as the direct guided-task well child, where it is the one work
scroller (`.well-frame` is clipped). Do not leave stacked wells as nested
`overflow-y: auto` inside a management `.operate-scroll`.

Section `h3` labels are teal uppercase microlabels with
`--field-label-gap` under the title. They do **not** take a
leading instrument tick. Unordered lists inside `WorkWellSection` replace
bullets with the same 7×1px teal hairline used on session `.briefing-sec`
lists. Ordered lists keep numerals in a reserved `--space-6` grid gutter
aligned to the section inset; do not draw ticks on `ol`. When a row carries
`data-sequence`, that inspectable sequence is the visible numeral. Session
briefing overlays follow the same split (`.briefing-sec h2` unlabeled by a
tick; `.briefing-sec ul li` ticks). Gallery: `work-well` specimens.

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
`--space-6` as outer `padding` on every edge and are not `.plate-foot`.
Conditional receipt copy is the first child of `.ceremony-foot-actions` and
shares its `--space-3` stack gap to the keys. Campaign configuration fill-grid
interiors are lab `CampaignCeremonyPlate`. `.ceremony-plate` is unpadded so the overlay
thumb can sit on the cut. Wide campaign configuration
(`.dialog-plate--wide.ceremony-plate`) is 840px wide; lab
`CampaignCeremonyConfigGrid` emits `.ceremony-config-grid` and
seats session limit, time warning, max attempts, and cooldown on one row
(two columns at ≤720px). Head and body use the same `--space-6`. The standing
ceremony helper is a sibling of the form stack; its `margin-block-start` is
`--space-6` and it spans the full body width (`max-width: none`). The form
stack grows so leftover body height does not park under
the helper.

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

`AssignmentPlate` (`web/src/components/work/`) is the destination and assignment tile: `frame-cut` at
`--notch` (no ticks or nodes), horizon `ReadoutList` (`tone="horizon"`), and a
reserved `PlateFoot` (`arrangement="end"`). Horizon row emphasis is
`emphasis="title"` | `"inline"` ([content](content.md)); the plate does not
author `.readout--*` classes. Paint lives in
`web/src/styles/components/work-plates.css`, not generic `plates.css`. Compact
`.plate-foot` keys stretch; `.assignment-plate-keys` opt out in that sheet.
Record marks follow
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
- Empty plates use the empty-state instrument, not bare text. Inset plates
  (`empty-plate--inset`) reset framed padding to `0`; hosts (etched frame,
  `.datatable-empty`, `--separated`) own spacing. Pages select
  `OperateArea` `empty.separated`, `CeremonyEmpty` / `CeremonyUnavailable`, or
  `DatatableEmpty`; do not pass `empty-plate--separated` / `ceremony-empty` from
  pages. Component Deck specimens may still pass those modifiers on `EmptyPlate`
  `className` to document CSS.
