# Design-system v1.0 change record

Non-normative provenance. This file does not govern product behavior, journeys,
or visual contracts. The [design-system README](README.md) and module files do.

## Source

| Field | Value |
| --- | --- |
| Visual source | Shipboard Terminal prototypes |
| Snapshot | Retired in Phase 7.5; formerly `.work/resources/impeccable-prototype-snapshot/` (Git history) |
| Experiment HEAD | `f724b68b11c2a147e59864f5789b260baaa50641` |
| Planning-review commit | `c52eeda3d8aa117bd7abd49f4ab0ab567953fe96` |
| Hashed files | 215 in `MANIFEST.json` |
| Rebuild task | `.work/active/impeccable-frontend-rebuild.md` |

The raw snapshot was temporary historical evidence and was deleted in Phase 7.5
of the rebuild, after adopted visual outcomes were durable in Git, this change
record, approved v1.0 modules, and the verified design lab. Recovery is Git
history plus this provenance record. Do not treat the deleted tree, the
external experiment checkout, or `/prototypes` as a live implementation
dependency.

## Supersession

Approved design-system v0.1 **Deep-Space Operational Futurism** is superseded
for shared visual identity, token values, typography aesthetics, component
appearance, non-semantic layout, and non-semantic motion. Git history retains
v0.1. Approved P0 interaction specifications are not superseded.

## Light-theme contrast

Light-theme phosphor teal for text/focus is `#146261` (at least 4.5:1 on
`#E8F0F2` canvas). Dark-theme `#3CC0BF` on hull ground exceeds 8:1. Implementation
must still verify every token pairing in both themes.

## Font license review

| Face | Package | License | Notes |
| --- | --- | --- | --- |
| Michroma | `@fontsource/michroma@5.3.0` | SIL OFL 1.1 | Copyright 2011 The Michroma Project Authors; imported from `web/src/styles/shared.css` |
| Sometype Mono | `@fontsource/sometype-mono@5.3.0` | SIL OFL 1.1 | Confirm OFL notice remains in the pinned package |

Exact versions are pinned in `web/package.json`. Self-host only. Include OFL
notices with the SPA license inventory.

## Item list primitive (2026-08-30)

`ItemList` is the shared record-row primitive: `renderItem` supplies custom
row content; optional `loadMore` requests the next page via a trailing
full-width key (`trigger="button"`, default) or an end-of-scroll sentinel
(`trigger="end"`) inside an optional named nested scrollport. Rows own a
`--space-4` gutter; Deck specimens seat the list in a flush etched frame.
Deck catalog section `item-list`.
WorkWell prose lists and submission version lineage stay composition, not this
component.

## Participants assignment lists (2026-08-30)

Product interaction (`UI-SUBM-DEC-13`–`16` in the Submission and Attempt
specification): cursor-paged Participants and options, stay on the registry
after assign, reserved select-all for future bulk, and Close/Revoke confirmation
with required reason codes. Not a design-system primitive change.

## Official docs refresh (2026-08-30)

Canonical modules synced to the current Shipboard implementation without
changing Approved v1.0 token meaning. Visual evidence now treats rebuilt
production pages as clone sources; the Component Deck remains the primitive
catalog; isolated lab journeys remain donors only for shells whose approved
family is not yet production-backed. Deck catalog includes `datatable-scroll`.
Layouts record current production assignment and keep approved Session/Review
families from the Activity IA as the contract target.

`Grid` `fit="fill"` and `AssignmentPlate` are the destination / assignment plate
recipes. Home omits unavailable destinations. When My work is available,
production `/` redirects to `/my-work` and Home is omitted from the gangway.
Design-lab Status Bays reuse `AssignmentPlate` inside the domain bay hull.

Consistency pass: canonical IA now lists `/activities/new` and `/results`;
unavailable locators may stay on `management` until the host contract exists.
Frontend architecture, design-lab README, and frontend-developer skill use the
same clone rule.

`StaticHeader` is the unsorted column-head cell (`.col-head`) beside
`SortableHeader`. Naked `th` text is not a valid head.

Root `DESIGN.md` remains a generated adapter (`python3 scripts/impeccable_context.py
generate`). The adapter source list now also fingerprints lists, tables,
inputs, borders, and empty/loading. It is not a Stitch token sheet.

## Viewport-aware overlays (2026-08-30)

Portaled plaques, menus, and select popovers share `placeFloating` (flip, shift,
and size into an 8px viewport inset). CSS `[data-tip]` plaques and native dialog
centering are unchanged. Clone `AnchoredOverlay` for new overlays. `DropdownMenu`
`placement` still exists on the API; both values portal.

## Etched frame clip vs grouping (2026-08-30)

`EtchedFrame` is a clip for one seated instrument, not a grouping box.
Stacked nested records (Enrollment detail, the Component Deck management-record
specimen) omit the well and fill the main landmark. Setup/create ceremonies
keep the 52rem form column. Registries, empty/wait plates, assignment-station
wells, and a readout fused to a `PlateFoot` keep the etched clip. Do not wrap
a stacked-record `ReadoutGrid` in its own well — it is a rule band
([content](components/content.md)). Unframed `record-plane` no longer carries
a `.record-frame` grow/undo rule — stacked records are not a clipped
instrument.

## FormSection stacked siblings (2026-08-30)

`FormSection` grouping is title-owned: a 2px `--hairline` rule under the legend
words (not the bay width), then `--form-group-gap` to the fields. Sibling clusters use `Stack` bay gap.
Do not pad, rail, or top-border the fieldset — fieldset block padding sits
between legend and fields. Do not wrap clusters in plates or insert
`.form-divider`. Side-by-side FormSections on `Grid` keep Grid gap only.

## Operate / form spacing rungs (2026-08-29)

Workspace in-page rhythm is three named tokens: `--field-label-gap` (control,
10px / `space-2-5`), `--form-group-gap` (group, 16px), `--operate-bay-gap`
(bay, 24px). `OperateArea` owns bay strata except plaque ledgers.
`FormSection` owns titled fieldsets; legend group gap is margin, not flex gap.
Legend type is H2 / plate title (`0.72rem`, `--text-bright`), not a field
microlabel. `WorkWellHead` default gap is control rung; head pad uses frame
insets.

## OperateHead copy cluster (2026-08-29)

Nested-record `BackKey` trails the copy cluster (title + description), not a
title-only row, so the title–description gap stays `space-2.5` with or without
the key. Compact widths still put the key on its own leading row.

## Work-well section ticks (2026-08-29)

Section labels in `WorkWell` and session briefing are teal microlabels without a
leading 7×1px tick. That tick remains the unordered-list bullet on those
surfaces and the current-item mark on interactive chrome. Gallery `pane`
includes a WorkWell specimen.

## Documentation sync from design lab (2026-08-28)

Official modules refreshed from `web/src/design-system/` and the Component Deck
without changing Approved v1.0 token meaning. Production candidate pages are
explicitly not visual authority. Recorded: `Alert` / `WaitPanel` / `ErrorSummary`
feedback trio, `BreadcrumbNav`, `TooltipHost`, `KeyGroup` as wrapping `Inline`,
`OperateArea` work-bay contract, design-lab route-layout assignment, and the
Deck section → module catalog in the implementation guide.

Review pass (same day) corrected: `Alert` warning/success are not distinct
skins; toast linger is 4200ms; `EtchedFrame` tick/flush rules; `WorkWell`;
`select-mark` vs instrument marks; `nav-rail` grammar vs `IndexRail`;
`OperateArea` `headArrangement` vs assembling `OperateHead`; accordion has no
promoted primitive; candidate CSS is `shared.css`.

Root `DESIGN.md` remains a generated adapter (`python3 scripts/impeccable_context.py
generate`). It is not a Stitch token sheet.

## Shared layout library (2026-08-27)

Closed shell set `management` / `guided-task` / `live-session` / `reference`
is implemented in `web/src/design-system/patterns/layouts/` with CSS in
`web/src/styles/components/layouts.css`. Production pages cannot custom-compose
outer chrome. `reference` remains design-lab only.

## Typography gallery (2026-08-27)

Component Deck Foundations now include `typography` after Type voices: heading,
body, technical, and link roles from `foundation/typography.md`. Specimens use
visual roles, not extra document headings.

## Layout primitives (2026-08-27)

Inner composition `Stack`, `Inline`, `Grid`, `Container`, `Inset`, and `SplitBay`
live in `web/src/design-system/components/layout/` with CSS in
`web/src/styles/components/layout-primitives.css`. They consume the spacing
ladder (`--space-*`) and content-width tokens. They are not a fifth shell.
`SplitBay` is the named start/main/end track for a management ledger (reviewer
console). Wrapping `Inline` children keep content size (`flex-shrink: 0`); `wrap={false}`
shrinks onto one row. Component Deck Composition sections are live
specimens only. Management work-bay variants
(index, nested record with `BackKey`, empty, split ledger) are Component Deck specimens of
`OperateArea` inside that shell.

Management and reference-catalog mains wrap slot content in `Inset`
(`composition-inset--shell-main`; `--shell-main-inset-inline` / 22px,
`--shell-main-inset-block` / 16px) by default. Page bays must not add a
second edge pad. Pages pass `contain={false}` for flush bays (Status Bays,
reviewer record, live-session transcript). Guided-task and live-session default
to no wrap.

## Implementation mapping (2026-08-27)

Recorded so agents do not treat root `DESIGN.md` or unused `--fa-*` names as
the token source:

- CSS primitives: `web/src/styles/tokens.css` (`--ground`, `--notch`,
  `--gangway-w`, `--ease-out`, …).
- Semantic aliases: `web/src/styles/semantic-aliases.css`.
- Light remaps: `web/src/styles/adaptations.css`.
- Participant instrument rails are desktop hull bulkheads (assignment 260px,
  session 232px) with stacked instrument bands at ≤1080px / ≤1180px. Desktop
  shells are `100dvh` with no 620px floor so short viewports scroll inside the
  rail rather than clipping past a hidden body.
- **v1.0 accessibility clarification (2026-08-27, `bec75d5`):** Examination
  Console at ≤760px (`bp.session`) reflows with page scroll (`body {
  overflow: auto }`, `.console { height: auto; min-height: 100dvh }`) so
  transcript, composer, Transmit, and completion consequence stay reachable at
  narrow width and 400% zoom. This implements the already-approved reflow rule
  in `foundation/layout.md` and `docs/ui-ux/text-session.md`; it does not
  introduce new product behavior.
- **Number field (2026-08-27):** `FieldNumber` in
  `web/src/design-system/components/fields/FieldNumber.tsx` with CSS in
  `fields.css`. Native spin buttons remain hidden; authored chevron keys
  provide increment/decrement. Component Deck Form controls shows text and
  number specimens side by side.

## Adopted visual concepts

Smoked-glass plates, hairline bezels, notched zero-radius geometry, phosphor
teal / rationed amber, Michroma placards, Sometype Mono data, gangway/bulkhead,
command strip, keys, readout grid, wait instruments, clipped-border frames,
emitters-only glow, the square hull-ground document icon (favicon), and the
Component Deck as the design-lab catalog.

Candidate production CSS loads `web/src/styles/shared.css` (tokens, base, and
production-safe component families). Lab-only demo and surface sheets load
only through `web/src/styles/design-lab.css`.

## Deliberate deviations from the prototype

Every `PC-01`–`PC-14` / `BR-01`–`BR-14` item in the rebuild task. Material
examples:

- Review and Release stay separate (`PC-01`).
- Human revision is an immutable server submit (`PC-02`).
- Unpublished Results stay at **Result not available** (`PC-03`).
- Campaign activation stays draft / readiness / server activate (`PC-05`).
- Invalid Campaign identifiers never silently substitute (`PC-06`).
- Lucide for ordinary controls (`PC-13`).
- Accessible type floors, contrast, focus, forced colors, and reduced motion
  override undersized prototype microlabels and color-only state (`PC-12`).
- Semantic success and danger tokens exist for outcomes even though the
  prototype forbade red and green as brand voices.
- An accessible light theme maps the same semantic roles.
- Production routes, copy, and permissions follow the repository (`PC-09`,
  `PC-10`).

## Exception audit (Phase 3)

Purely visual v0.1 versus prototype conflicts adopt Shipboard. Behavior, flow,
semantic, accessibility, security, and IA conflicts adopt the repository. No
new escalation-threshold product question was found.

| ID | Conflict | Resolution |
| --- | --- | --- |
| `DS-X1` | Deep-Space identity vs Shipboard Terminal | Shipboard (owner-approved visual direction) |
| `DS-X2` | Prototype dark-only vs required light theme | Dark-first identity; light operational theme with mapped tokens |
| `DS-X3` | Electric blue/cyan vs teal/amber | Teal = system/context/live; amber = attention/commitment |
| `DS-X4` | Prototype no red/green vs outcome semantics | Keep success/danger tokens; never as brand; always pair with text/marks |
| `DS-X5` | Observation Glass vs smoked-glass planes | Smoked glass is the plate language; no blur under reading content |
| `DS-X6` | Prototype no icon library vs ADR-019 Lucide | `DS-DEC-10` / `PC-13` |
| `DS-X7` | Prototype microlabel sizes vs WCAG | Raise interactive/label floors; 400% zoom/reflow required |
| `DS-X8` | Prototype amber-only validation vs danger | Amber for field validation; danger for failed/destructive outcomes |
| `DS-X9` | Geist/Space Grotesk/IBM Plex vs Michroma/Sometype | `DS-PROP-2` |
| `DS-X10` | Soft radii vs zero-radius notches | `DS-DEC-9` |

## Command-strip hull (2026-08-29)

The command strip is hull chrome, not a second umbra. Resting fill is
transparent so the body canvas shows through. Sticky Component Deck chrome
uses `--ground-deep` only. Light theme no longer paints a white wash on the
strip. Gangway, console foot, management drawer bar, and instrument bulkhead
rails follow the same hull rule; overlay bulkhead drawers stay opaque.

## Compact ID (2026-08-29)

`CompactId` is the shared center-truncated identifier readout. It uses
`TooltipHost` `tone="value"` so the full identifier stays exact-case on hover.
Dense registry tables omit per-cell tab stops; pass `tabbable` for
focus-visible plaque in standalone surfaces. The plaque also opens when the
compact form differs from the value, or when CSS clips a value that already
fits logically. `compactRegistryId` lives beside
the component. Component Deck section: `compact-id`. Production Enrollment
registry and the assignment instrument rail consume it. Activities registry
dropped its Campaign ID column in the shipboard reset (IDs remain searchable).
Native `title` is not this pattern.

## Instant readout absence (2026-08-29)

Registry timestamps use `InstantReadout`. Missing or unreadable UTC instants
show the shared absence glyph (`—`) with accessible name “Not recorded”. They
must not interpolate `undefined` or report the viewer timezone as unavailable.

## Selectable TooltipHost plaques (2026-08-29)

`TooltipHost` plaques linger 240ms after pointer leave so the pointer can enter
the plaque and select or copy its text. Selection drags that start on the
plaque keep it open until pointer up. Opening one plaque dismisses any other.
CSS `data-tip` plaques stay inspect-only.

Modal `<dialog>` is a top layer. `overlayPortalRoot` seats `TooltipHost`
plaques and fixed `DropdownMenu` panels in that dialog when the host lives
there, so CompactId, header-select tips, and row menus remain visible inside
Assign and other ceremonies.

## Ceremony unavailable helper (2026-08-29)

`CeremonyUnavailable` is the shared unknown / denied / missing-resource plane:
hug `CeremonyArea`, inset empty well, quiet recovery key centered in the well.
Design-lab not-found and production destination pages consume it. Amber `open`
stays on Open-session and other commit actions.

## Fault phosphor danger (2026-08-29)

Dark outcome red is a hull fault lamp, not Tailwind rose. Primitives
`--danger` (`#F05C58`), `--danger-bright` / `fg-danger` (`#FF7468`), and
`--danger-glow` live in `tokens.css` beside teal and amber. Denied ceremony
titles consume `--fg-danger` plus `--danger-glow` (no leftover teal halo).
Component Deck `colors` catalogs the chips and a danger placard; Management
ceremony shows `CeremonyUnavailable danger`. Light theme keeps wine
`#A9323E` / `#C43E4B`. Validation remains amber (`DS-X8`).

## Plate foot arrangement (2026-08-29)

`PlateFoot` is the shared plate action rail: closed `arrangement` `start` |
`center` | `end` (default) | `split`. Assignment and destination plates use
`end`. Work wells use `start`. Dialog feet use the same primitive except
ceremony fill-grid feet. There is no `plate-foot--start` class hatch and no
free middle slot. Every `.plate-foot` draws a `hairline-dim` block-start
rule; dialog and work-well feet inherit it instead of a second stroke.
`.ceremony-foot` keeps the same token. Air above the rule uses
`--plate-foot-pad-block` on the preceding sibling (except dialog and
work-well bodies, which already pad). Dialog `.dialog-head`, `.dialog-body`,
and `.dialog-foot` use that same token on both block edges (inline
`--frame-inset-inline`) instead of the prototype 22/14, 18/24/20, and 14/20
offsets. Ceremony fill `.ceremony-foot` uses the token as `padding-block-start`.

## Plate foot hairline composition (2026-08-30)

In-plate `PlateFoot` keeps the `hairline-dim` block-start rule (`hairline`
default true) full-bleed to the bezel. Hull chrome that is a sibling of a
complete pane — `GuidedTaskFoot` — sets `hairline={false}` so a floating
internal divider does not sit under the well-frame. Assignment-plate and
Setup/Create `.setup-ceremony` inner padding no longer insets that rule;
keys stay on `--frame-inset-inline`. Fused lab readout+foot wells use the
same recipe via `.in-plate-host`. Lab assignment-station actions use
`GuidedTaskFoot` rather than a local `.action-keys` fragment.

## Plate bay slots (2026-08-29)

Home destination plates and My work assignment plates use `Grid` `fit="fill"`
so a lone plate occupies one hull slot instead of stretching. Compact
viewports still use one full-width column.

## Ceremony wait auto measure (2026-08-29)

Auto hug `CeremonyWait` / inset wait-plate occupies the existing 36rem
column cap so a short loading line does not collapse the etched well or
ellipsize the operate description while the landmark still has room.
Desktop keeps the status label on one line; compact wraps inside the well.
Named `sm`/`md`/`lg` rungs are unchanged.

## Management scroll ownership (2026-08-29)

Management `main` is clipped. Bay chrome stays seated (breadcrumbs, title,
description, context, advisory). Fill composition wraps only the work body in
`.operate-scroll`. `EtchedFrame` grows with content and is not a vertical
scrollport. Nested `.frame-scroll` overflow is visible. Exceptions that keep
one inner scroller: setup/create `.create-ceremony__scroll` (docked foot),
filling-registry `.datatable-scroll` (rows), split-ledger columns, assignment
`.work-well__body` (`.well-frame` is clipped), Status Bays `.bay-plates`
(`.operate-scroll` is clipped). Ceremony hug may overflow on
`main`. Stacked management wells (Enrollment detail) overflow visible inside
`.operate-scroll`.

## Overlay filling-table scroll (2026-08-30)

Live overlay form and confirm dialogs still inner-scroll `.dialog-body`.
When that body hosts `.datatable-scroll` (Assign Participant picker), keep
one vertical wheel on `.dialog-body` or `.ceremony-body` (the clip-path
plate’s direct child). Nested `.datatable-scroll` uses `overflow-y: clip` so
it cannot compete; horizontal overflow stays on the table. Override its
default `overscroll-behavior: contain` on the Y axis
(`overscroll-behavior-y: auto`) or the table swallows the wheel.
`.datatable-toolbar` is sticky so search stays seated; pagination stays in
flow. Short lists still hug. Future picker filters belong in `DataTableToolbar`.

## Nested scroll ownership (2026-08-30)

Status Bays keep per-column `.bay-plates` scroll; the operate wrapper is
clipped. `WorkWell` body scroll is only the guided-task well. Management
stacked wells grow in `.operate-scroll` so one operate pane receives the
wheel. Filling tables in lab walls (not only `.workspace-area`) use the same
clipped operate column so `.datatable-scroll` can own rows. Etched
`.frame-scroll` uses `overflow-x: clip` with `overflow-y: visible` so
`hidden`+`visible` does not compute as a dormant nested `overflow-y: auto`.

## Component Deck catalog scroll (2026-08-30)

The Component Deck catalog column is the vertical wheel target. Nested family
specimens (`.layout-spec`) hug instead of acting as `72dvh` mini-hulls; inner
operate, rail, well, and table regions use `overflow-x`/`overflow-y: clip`
(tables keep `overflow-x: auto` with `overflow-y: clip` so `visible` does not
compute to `auto`). Form recipes hug the same way: OperateArea
`.operate-scroll` and seated in-flow `DialogPlate` bodies use `overflow-x`/
`overflow-y: clip` and `overscroll-behavior: auto`. Overlay `max-height` is
dropped on seated plates. Overlay `overflow-y: auto` plus
`overscroll-behavior: contain` otherwise traps the wheel even when the plate
is shorter than the viewport. Native `<dialog>` DemoDialog specimens still
inner-scroll. The sticky deck index rail still inner-scrolls. Other overlay
widgets are unchanged. Production hulls and design-lab Operate routes keep
their own scroll ownership.

## Ceremony auth commit recovery (2026-08-30)

`CeremonyUnavailable` recovery defaults to a quiet Return/Reload key.
`recovery.variant="transmit"` (large) is reserved for **Continue to sign in**
on the unauthenticated gate and access-changed reauth. Amber `open` stays off
this plane. Production auth no longer assembles `CeremonyArea` + `CeremonyEmpty`
+ `Key` by hand.

## Setup provenance Note (2026-08-30)

Frozen Setup cluster provenance uses the shared workspace **Note**
(`Alert` info, advisory copy at 0.78rem) at the top of the ceremony form.
It is not a floating field-hint. Activated Setup folds the same sentence
into the existing Cohort activated Alert body. Gallery: `alert` plus
Management setup. Advisory vs Alert placement, Setup shared-control copy, and
the implementation-guide Feedback index were aligned the same day. Readiness
blockers use ErrorSummary (**Readiness blocked**), not a warning Alert. The
resolved-note copy constant is `SETUP_RESOLVED_NOTE` beside campaign field
placeholders. Gallery Setup includes draft, blocked, and activated compositions.
Save and check failures on a blocked revision stay in the same ErrorSummary.
Save-only Setup failures use **Correct the following**, matching Create.

## Assignment Station guided-task foot (2026-08-30)

At ≤1080px the `guided-task` actions foot is `position: fixed` on the
viewport floor (`--ground-deep`, hairline) so Cancel / Submit stay reachable
while the stacked well scrolls. Assignment Station Submit version stays visible
during intake and uses `disabledReason` when empty or not permitted.

