# Layouts (application shells)

Closed set of outer page structures. Feature pages supply typed slot content.
They do not assemble command strips, gangways, bulkheads, instrument rails,
work bays, or page footers.

Implementation: `web/src/design-system/patterns/layouts/` for management,
guided-task, and reference. The live-session React shell is
`web/src/design-lab/components/layouts/LiveSessionLayout.tsx` until a production
session route exists; the family id stays in the production layout set. Route assignment is
router-owned (`PC-10`). Destinations in a gangway exist only when permitted
(`PC-09`). Accessibility follows [accessibility](../foundation/accessibility.md)
and `PC-12`. Synthetic fixtures stay out of production (`PC-14`).

## Families

| ID | Landmarks | Required slots | Optional slots | Production |
| --- | --- | --- | --- | --- |
| `management` | skip link, banner/`header` command strip, optional `nav` gangway or bulkhead, `main`, optional `footer` | command strip, main content | navigation groups, breadcrumbs, banner, footer, overlays | yes |
| `guided-task` | skip link, complementary instrument rail, `main` work well, `footer` actions | rail brand, rail instruments, heading, work well | actions, overlays | yes |
| `live-session` | skip link, complementary instrument rail, `main` transcript, composer `footer`, complementary examiner | rail brand, instruments, transcript, composer, examiner | overlays, warned/complete modifiers | yes (shell in design-lab until a production session route) |
| `reference` | skip link, command strip, optional index rail, `main`, optional footer | command strip, catalog/deck main | index rail, footer | design lab only |

Guided-task `brandExtras` may include `RailHomeLink` and `ProfileMenu` with
`placement="rail"`. Instrument-rail phase navigation uses design-system
`PhaseSpine`; domain wrappers (for example `AssignmentSpine` in
`components/work/`) map business nodes. Assignment heading status bands use
`AssignmentStatusReadout` in `components/work/`. The live-session React shell
is `LiveSessionLayout` in `web/src/design-lab/components/layouts/`; the family
id stays in the production layout set.

Each family renders one root with `data-layout="<id>"`. Pages must not wrap
content in a second layout root. Component Deck specimens may nest a second
root inside a slot when demonstrating a family; wrap each specimen in
`LayoutAssignment` for that family and set `nested` so the specimen does not
emit a second skip link, `main`, or `#main-content`. Nested family specimens
hug content: the catalog column (`body`) is the vertical wheel target. They
must not inner-scroll. Nested `command-strip` copies are catalog artifacts,
not hull chrome: gallery CSS sets `.deck .command-strip { z-index: auto }`
so they cannot compete with Deck `page-strip` (`z-index` 70). Seated overlay recipes (in-flow `DialogPlate`, not a
native `<dialog>`) hug the same way: neutralize `.dialog-body` overflow in
gallery CSS only. Live overlay dialogs keep inner body scroll so tall
ceremonies stay usable. A filling table in a live overlay keeps that body
scroll: nested `.datatable-scroll` must not be a second vertical wheel
inside a clip-path plate. Product hulls and design-lab Operate routes keep the
scroll ownership in this module. The optional management
`breadcrumbs` slot is `BreadcrumbNav` (`.text-link` ancestors, no underline).
Omit it on ceremony, console index / empty-index, and guided-task /
live-session families. Nested management records keep the trail.

## Allowed vs forbidden

Allowed inside slots: feature headings, plates, tables, forms, Status Bays,
reviewer record grid, transcript turns, demo controls (lab only). Prefer
[layout primitives](layout-primitives.md) for inner stack, wrap, intrinsic
grid, width, and padding. Do not assemble a second `data-layout` root.

Management `children` is one `OperateArea`. Select host geometry with `bay`
(`workspace` default; `record`, `registry`, `ceremony`; frozen set). Setup/create uses
`SetupOperateArea`. Lab walls,
home board, Deck form recipes, and reviewer queue/ledger use domain wrappers
(`CampaignsOperateArea`, `EnrollmentWallOperateArea`, `SampleWallOperateArea`,
`HomeBoardOperateArea`, `FormRecipeOperateArea`, `ReviewerQueueOperateArea`,
`ReviewerLedgerOperateArea`). Production My work uses `AssignmentBoardOperateArea`.
Replacement hosts pass `OperateAreaHost` `hostClassName`; `FormRecipeOperateArea`
adds owned `form-recipe` on `OperateArea`. Production pages do not pass `frameClassName` /
`headClassName` / `hostClassName` and do not import `OperateAreaHost`. Select etched-frame
geometry with `frame` (`record`, `registry`, `datatable`, `ceremony`) when
the body uses a shared well; `registry` and `datatable` default
`frameInset="flush"`. Optional `hug` is `"registry"` only on production `bay="registry"`
(`registryTableHug`). Lab wrappers that replace the host add
`registry-wall--hug` themselves. Empty assignment boards use `AssignmentBoardOperateArea` /
`HomeBoardOperateArea` `hug="board"` (emits `assignment-board--hug`). Board-hug,
assignment head/bays/instruments, and setup-ceremony paint live in
`web/src/styles/components/work-plates.css`. Production registry hug stays on
generic `app-shell.css` (`.workspace-area.registry-wall--hug`). Lab
`.campaigns-wall` / `.wall` / `.queue-view` hug lives in those surface sheets.
`danger`
adds the matching modifier. Registry tables pass
`hug={registryTableHug(visibleCount)}` so empty and short lists
(0–4 visible matching rows, including search-empty) hug the plate and longer lists fill. Do not
pass `className="workspace-area…"`; additive `className` is only for extra
states such as `is-released`. That plate owns the page title (`h1`), optional description, optional
`BackKey`, optional advisory/context, and the body: an etched well, an empty
plate, or `framed={false}` content when the body is already a plate grid, a
stacked nested record, or a split ledger. OperateArea `advisory` is page-level
(with the operate head). Do not put mutation receipts or blocking errors in
`context`; those belong to toast and in-body `Alert`. `ToastHost` defaults to
`placement="top-center"` for now.
Frozen-cluster provenance uses in-form `Alert`; see [alerts](alerts.md).
Pages do not assemble `OperateHead` plus `EtchedFrame` by hand, and they do
not place a second heading stack above the operate area. Design-lab Home and
Reviewer consoles use the same plate. The reviewer record is the split-ledger
variant: `ReviewerLedgerOperateArea` supplies `OperateHead`
`arrangement="plaque"` as the prototype `record-head`
(back, centered title and seal, session id). `SplitBay` is only the three work
columns. The decision bar is a sibling foot of that bay, not a `SplitBay`
`foot`. `framed={false}` so the
ledger is not nested in a second etched well. Layout `contain={false}` lets the
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
| `reference` catalog | `true` | Unknown-channel copy uses the default. `/surfaces` currently passes `contain={false}` for the flush channel board. |
| `reference` deck | `false` (`contain ?? !index`) | Component Deck specimens need the full deck column |

| Variant | Title | Description | Back | Body |
| --- | --- | --- | --- | --- |
| Console index / registry | required | recommended | omit | etched list or table. Destination, assignment, and Status Bay **plate grids** use `framed={false}` so the plates are the only pane |
| Nested stacked record | required | recommended | `BackKey` to the parent index, trailing beside the copy cluster (title + description); own leading row at compact widths. Not on the breadcrumb trail | `ReadoutGrid` + `WorkWell seat="stack"` (Enrollment detail, Deck management-record): `framed={false}`. Hierarchy is title, readout rules, and well heads — not a grouping hairline |
| Nested ceremony / setup-create | required | recommended | `BackKey` to the parent index, same trailing placement | one seated instrument: ceremony form with docked `PlateFoot`. Keep the etched well (`SetupOperateArea`, 52rem form column). A readout fused to that well stays inside the clip |
| Nested one-instrument record | required | recommended | `BackKey` to the parent index, same trailing placement as stacked records | filling table, empty/wait plate, or readout fused to a `PlateFoot` (lab Campaign record): keep the etched well |
| Split ledger | required (`ReviewerLedgerOperateArea` plaque head) | recommended | `BackKey` in the plaque | `SplitBay` start/main/end plus sibling decision foot; `framed={false}` and layout `contain={false}` |
| Empty index | required (same as the populated index) | recommended | omit | `empty` plate inside the frame |
| Ceremony / unavailable | required | prefer the inset empty note | omit | `CeremonyUnavailable` hug column (default `hugMeasure="auto"`; empty well hugs the note and recovery key, not the 36rem wait cap); inset empty well + recovery key centered in the well. Default recovery is **quiet**. Auth **Continue to sign in** uses `recovery.variant="transmit"` (large commit). Title aligns to the chamfered top edge. Pin `sm`/`md`/`lg` only when the well must match a dialog rung. Full-width operate bays stay `composition="fill"` so titles keep wordmark alignment. Do not assemble `CeremonyArea` + `CeremonyEmpty` + `Key` by hand for this plane. |
| Ceremony / loading | required | optional session copy | omit | `CeremonyArea` hug column; wait wells occupy the 36rem cap so a short status label does not shrink the etched frame (empty unavailable stays hug-to-copy, not that cap). `CeremonyWait` inset wait-plate (wait-mark, label, scan-track). Do not drop an inline `WaitPanel` into the etched well. |

Forbidden: a page or route module importing `CommandStrip`, `ConsoleFoot`,
`Gangway`, `Bulkhead`, `AreaGroupList`, `RailBrand`, or `IndexRail` to compose
a shell; declaring reserved `.layout-*` structural selectors; selecting
`reference` in production.

## Responsive and a11y

- Management gangway collapses; at ≤1080px (`adminDrawer` / `pageScroll`) the
  layout owns a leading bulkhead. Short desktop keeps `100dvh` with `body`
  overflow hidden. Management `main` is clipped. Bay chrome stays seated
  (breadcrumbs, `OperateHead` title and description, context, advisory). The
  work body scrolls once: `.operate-scroll` for plate grids and stacked records.
  Framed instruments grow with content. Filling registries scroll row overflow on
  `.datatable-scroll`. Any operate pane that contains a filling table (review
  queue, lab walls, production registries) is a flex column clipped so that
  table is the wheel target; hug lists (0–4 visible matching registry rows,
  including empty and search-empty) size the plate to content. If that hugged
  page still exceeds the bay, `.datatable-scroll` remains the wheel. Lab campaign and
  enrollment walls and the review queue use the same `registryTableHug` rule
  as production. A filling table
  in a live overlay keeps `.dialog-body` or `.ceremony-body` as the wheel ([modals](modals.md)).
  Fill-remaining
  instruments (split ledger, setup ceremony, Status Bays) clip `.operate-scroll`
  and scroll inside columns or the form well. Ceremony hug may overflow on `main`
  so a centered empty or wait well is not trapped. In management `main`, ceremony
  planes fill the seated bay (`flex: 1 1 auto`) so the hug cluster centers on both
  axes at desktop; compact (≤720px) still top-seats and stretches the column.
  When `breadcrumb-nav` is present, ceremony padding stays symmetric
  (`padding-block`) so flex centering is not biased toward the top.
- Guided task stacks the instrument band at ≤1080px. Rail brand stays outside
  the rail scroller on desktop. At that stacked breakpoint the actions foot is
  `position: fixed` on the viewport floor so transmit keys stay reachable while
  the page scrolls (`IA-MVP-4`).
- Live session stacks at ≤1180px and reflows with page scroll at ≤760px so
  transcript, composer, and completion stay reachable at 400% zoom.
- Contrast, keyboard, focus, reduced motion, and forced colors follow the
  foundation modules. Layout CSS also owns print overflow, forced-color
  borders, and reduced-motion scroll behavior for the four shells. State never
  relies on color alone.

## Route examples

Family assignment is router-owned. Design-lab paths
(`web/src/design-lab/app/design-lab-route-layouts.ts`):

| Path | Family |
| --- | --- |
| `/surfaces` (channel index) | `reference` with `contain={false}` |
| `/shared/gallery` (Component Deck) | `reference` deck (`index` present → contain off) |
| `*` (unknown path) | `reference` catalog (contain defaults on) |
| `/participant-home`, `/admin-console`, `/reviewer-console` | `management` |
| `/participant-journey` | `guided-task` |
| `/participant-session` | `live-session` |

Approved target families live in the
[Activity journey](../../activity-campaign-journey.md) canonical route table.
Current production assignment (`web/src/router/production-route-layouts.ts`)
matches those families where the host contract is implemented:

| Path | Family |
| --- | --- |
| `/`, `/activities`, `/activities/new`, setup, Enrollment roster/detail, `/my-work` | `management` |
| `/my-work/:enrollmentId` | `guided-task` |
| `/sessions/:sessionId`, `/review`, `/review/:reviewId`, `/release`, `/release/:resultId`, `/results`, `/results/:resultId` | `management` today (honest ceremony / contract-unavailable). Approved targets remain `live-session` for Session and `guided-task` for Review/Release records. |
| unknown locator (`*`) | `management` |

When My work is available, production `/` redirects to `/my-work`; administrator
`/` stays this `management` destination catalog.

New production pages clone a matching existing production surface and Deck
specimen. Isolated lab journeys remain donors for shells whose approved family
is not yet production-backed (`live-session`, reviewer split ledger). Do not
copy lab fixtures or invent a second chrome language.
