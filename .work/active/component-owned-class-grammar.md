---
id: component-owned-class-grammar
status: completed
created: 2026-08-30
updated: 2026-08-31
---

# Closed (frozen 2026-08-31)

Do **not** reopen this task. Do **not** add `OperateBay` values, demote
`CeremonyArea`, rename leftover CSS, or wrap session inner hull / gallery
`spec-row`. Class leakage is closed. Live contract:
`docs/ui-ux/design-system/` (implementation-guide, cards, layouts) and the
frontend-developer skill. This file is retained history. Ignore the
Decisions inventory (`setup`, `homeBoard`, `queue`, `ledger`, wall bays,
`hug="board"` on `OperateArea`).

`TODO.md` “Scan plain html, class, css convert to component” is checked for
**page-authored Shipboard grammar**, not 1:1 wrappers for decorative internals.

# Goal

Stop feature/page authors from naming Shipboard class grammar. Promoted
components own which CSS classes they emit. CSS files stay the paint layer.
Pages compose typed component APIs. Visual appearance and scroll ownership
must not change.

This is `TODO.md` line 7 (“Scan plain html, class, css convert to component”),
scoped to **closing class leakage**, not wrapping every class 1:1.

# Governing sources

- `docs/ui-ux/design-system/README.md` — authority; visual evidence; clone production + Deck
- `docs/ui-ux/design-system/implementation-guide.md` — OperateArea owns bay strata; compose primitives, not one-off CSS
- `docs/ui-ux/design-system/components/layouts.md` — management `children` is one `OperateArea`; bay variants
- `docs/ui-ux/design-system/components/cards.md` — OperateArea, Status Bays vs plate grids, `record-plane` / setup
- `docs/ui-ux/design-system/components/layout-primitives.md` — Stack/Inline/Grid/Inset; do not invent Box/`sx`
- `docs/ui-ux/design-system/components/tables.md` — datatable grammar (classes currently documented as the contract)
- `docs/ui-ux/design-system/components/content.md` — ReadoutGrid / ReadoutList (`tone` already exists on lists)
- `docs/ui-ux/design-system/foundation/layout.md` — control / group / bay rungs
- `docs/contributing/development-harness.md` — attach `:5274` (candidate) with `:18080` healthy
- Prior discussion default: CSS remains private implementation; React is the public API; `className` is an escape hatch

# Scope

## In

Three sequenced families. Finish and verify each family before starting the next.

1. **OperateArea bay ownership** — typed `bay` (plus `hug` / `danger`) replaces required `className` bundles.
2. **Assignment / bay / readout compositions** — `AssignmentHead`, `AssignmentBays`, `StatusBays`, ReadoutGrid instrument band, StateReadout assignment mark.
3. **Datatable body contract** — table/row/cell/id primitives so production tables stop assembling `.datatable-table` / `.cell-*` / `.datatable-id` by hand. Own `.datatable-actions` without forcing bulk `TableActionBar` onto Create/Assign-only tables.

Update design-system modules, implementation-guide, and the frontend-developer
skill OperateArea bullet so they document **props**, not page-authored class
strings. Keep emitting the same CSS class names so `style-entry.test.ts`,
Bulkhead overlay queries, and scroll CSS keep working.

## Out

- CSS-in-JS, Tailwind, or moving visual values out of `web/src/styles/`
- 1:1 wrappers for decorative internals (ticks, notches, sheen, `.gangway-tick`, `.plate-foot-slot`, `.frame-node`)
- New layout primitives (`Box`, `sx`, arbitrary gap props)
- Changing product behavior, routes, copy, permissions, or scroll ownership
- Renaming CSS selectors (unless a new `data-*` is strictly required and CSS + `style-entry.test.ts` update in the same step)
- Typed `frame` / `headClassName` APIs. `frameClassName` and `headClassName` stay strings this task (`datatable-frame`, `record-frame`, `ceremony-frame`, `campaigns-frame`, `campaigns-head`, `wall-head`)
- Inferring `framed`, `composition`, `headArrangement`, or `hugMeasure` from `bay`. Those props stay independent (see Do-not-infer)
- Live-session hull CSS. Status Bays **column geometry** stays domain CSS; Family 2 may wrap the markup only
- Relocating `.board` (lives in `participant-home.css` / `not-found.css`). `bay="homeBoard"` only emits the class
- Open-ended repo-wide “scan every className”
- Impeccable polish/restyle. Ownership refactor, visual parity
- Schema-driven DataTable (columns-as-data)
- Deck datatable **expand/detail** rows (`.datatable-detail`, `.datatable-id-cell` chevron). Unique lab specimen; leftover after Family 3 unless a production table needs it

## Follow-up (do not do unless already touching the file)

- ~~Typed `frame` prop~~ (shipped 2026-08-31: `OperateFrame` on `OperateArea`; lab wall chrome in domain wrappers)
- `.control-line` on enrollment detail / Deck `AcknowledgmentGate` (`ControlLine` already exists)
- `.frozen-line` / `.in-plate-host` lab campaign record
- `.phase-spine` internals (already inside `AssignmentSpine`)
- `.form-recipe-dialog` / `.form-recipe-dialog-well` (Deck dialog recipe, not OperateArea)
- Redundant `StateReadout className="state-cell"` on tables (root already has `.state-cell`)

# Execution rules (agents)

1. Load `implementation-workflow` + `frontend-developer`. Do not start Impeccable extract/polish unless asked.
2. One family at a time. Mark only that step `[>]`.
3. **Red before green** for each family: extend **component** tests first, run them, then implement. Do **not** break production compile as a red tactic (Family 3 especially).
4. Do not restyle. After each family, screenshots must match pre-change layout, spacing, and scroll behavior, except the documented Deck `work-plane` alignment below.
5. Keep CSS class **names** on the DOM. Page tests that query `.registry-wall--hug` / `.record-plane` stay valid.
6. `className` on promoted components stays **optional and additive** (`is-released`, `is-adjusting`, lab `manifest`). Not the primary API.
7. Clone existing production usage. Do not invent a fifth bay geometry.
8. If Family 3 balloons past the primitive set, **split** to a new `.work/active/` file. Do not skip Family 1–2 docs/tests to rush tables.
9. Attach Playwright to healthy `:5274` with `:18080` `session-endpoint:ok`. Do not `compose:up` over a live stack.
10. Use existing `web/src/lib/cx`. Export new types from the production barrel (`web/src/design-system/index.ts` via `components/index.ts` / `plates/index.ts` / `datatable/index.ts`). Lab `design-lab/components/index.ts` only if it already re-exports the touched module.
11. Update this file after each step: plan markers, Current state, Verification, Findings.

# Do not infer (lock)

`bay` only selects the **host class bundle**. Callers keep today’s other props.

| `bay` | Still pass explicitly (examples) |
| --- | --- |
| `ceremony` | `CeremonyArea` still sets `composition="hug"`, `frameClassName="ceremony-frame"`, `hugMeasure`. `bay="ceremony"` does **not** hug by itself |
| `setup` | `framed` default true, `frameClassName="record-frame"` on Setup/Create. Enrollment detail is `bay="record"` **and** `framed={false}` |
| `registry` | `frameClassName="datatable-frame registry-frame"`, `frameInset="flush"` |
| `ledger` | `framed={false}`, `headArrangement="plaque"`, additive `className` for `is-released` / `is-adjusting` |
| `workspace` plate grids | `framed={false}` on Home / populated My work |
| lab walls | do **not** add `workspace-area`. Keep `headClassName` |

# Decisions

- **CSS stays.** Components select classes; they do not re-express hairlines as inline styles.
- **Same selectors on the DOM.** `bay="setup"` still emits `workspace-area work-plane record-plane record-plane--setup`.
- **Prop name is `bay`, not `tone`.** ReadoutList already has `tone`. OperateArea `record` and a readout `tone="record"` would collide in meaning. Family 2 uses `band` / `mark` (below).
- **Drop a separate `assignment` bay.** It would emit the same classes as `workspace`. Empty My work is `bay="workspace"` + `hug="board"`.
- **Closed `bay` set:**

  | `bay` | Emitted classes |
  | --- | --- |
  | `workspace` (default) | `workspace-area work-plane` |
  | `record` | `workspace-area work-plane record-plane` |
  | `setup` | `workspace-area work-plane record-plane record-plane--setup` |
  | `registry` | `workspace-area work-plane registry-wall` |
  | `ceremony` | `workspace-area work-plane work-plane--ceremony` |

  Lab walls, home board, form recipes, and reviewer queue/ledger are **not**
  `OperateBay` values. They use domain wrappers (`hostClassName` or additive
  `form-recipe`). Follow-up: `.work/active/domain-class-grammar-demotion.md`.

- **`hug?: "registry" | "board"`** (not boolean). `"registry"` adds
  `registry-wall--hug` on `bay="registry"` **or** any `hostClassName`
  replacement host (lab walls, review queue). `"board"` adds
  `assignment-board--hug` on `bay="workspace"` or a replacement host (lab Home).
  Otherwise ignore (unit-test). Registry hug uses `registryTableHug(visibleCount)`
  (0–4 matching rows including empty and search-empty). Empty board hug stays
  on My work / lab Home.
  **Superseded 2026-08-31:** helper hug is production `bay="registry"` only;
  lab wrappers add `registry-wall--hug` (`.work/active/ds-domain-cleanup-leftovers.md`).
- **`danger?: boolean`** on OperateArea adds `workspace-area--danger`. CeremonyArea passes `danger={danger}` into OperateArea. Do not cx the danger class in CeremonyArea.
- **`className` optional additive** (cx last). Required `className: string` goes away.
- **Deck specimens that today pass only `workspace-area`** (management index, nested record, empty) **gain `work-plane`** via default `bay="workspace"`. That is accepted alignment with production, not a new geometry. Playwright those three Deck sections. If a specimen breaks, record it; do not add a fourteenth bay that is `workspace-area` without `work-plane` unless evidence requires it.
- **Helper** `operateAreaClass(bay, { hug, danger, className })` next to `OperateArea`. Unit-test every bay × hug × danger × extra className. CeremonyArea must not duplicate the cx.
- **Family 2 naming:** `AssignmentHead` lives in `web/src/design-system/components/chrome/` (guided-task heading slot, beside OperateHead). `ReadoutGrid` `band="instruments"` → `.assignment-instruments`. `StateReadout` `mark="assignment"` → adds `.assignment-record` and default label class `.assignment-record-label` (still on the existing `.state-cell` root). Do not use `tone="record"` on StateReadout.
- **Family 2 1-caller exception:** `AssignmentBays` and `StatusBays` each have one production/lab caller. Extract anyway: they are named CSS grammar, not a 3+ copy heuristic. Do **not** unify them (`Grid` vs four-column `.bays` hull).
- **Family 3 name: `DatatableTable`**, not `DataTable`. Lab `EnrollmentTable.tsx` already exports `DataTable` as a full feature table. Colliding names will break lab imports.
- **Family 3 primitives:** `DatatableTable` (forwardRef, `hidden`, caption, additive `className` for `manifest`), `DatatableRow` (`is-selected` via prop or className), `DatatableCell` (`kind` + `colMin` + additive className for lab `col-*` / `cell-result`), `DatatableId` (polymorphic `Link` | `button`). `SortableHeader` / `StaticHeader` stay the `th` API. Empty: wrapper or Shell defaulting `datatable-empty` on `EmptyPlate`.
- **Family 3 `kind` closed set:** `id` | `content` | `state` | `select` | `action`. Lab extras (`col-candidate`, `col-assignment`, `manifest`, `cell-result`) use additive `className`, not more kinds.
- **Production action strips:** `DatatableActions` host (`.datatable-actions` + `KeyGroup` + `justify="end"`) for Create/Assign-only tables. Lab `CampaignRegistry` keeps `TableActionBar`.

# Current inventory (2026-08-30, readiness-reviewed)

## Already correct (do not “extract”)

- `Stack` / `Inline` / `Grid` / `Inset` / `Container` wrap `.composition-*` internally
- `CeremonyArea` already maps ceremony + danger; after Family 1 it passes `bay="ceremony"` + `danger` instead of a class string
- `DataTableShell`, toolbar, pagination, `SortableHeader`, `StaticHeader`, `CompactId`, `TableActionBar` already own their classes
- `web/src/components/` is feature composition, not a second design system
- `StateReadout` root already includes `.state-cell`
- `ReadoutList` already has `tone="horizon"`

## Family 1 — every OperateArea caller

`OperateArea` requires `className: string` today.

| Caller | Current `className` | Target |
| --- | --- | --- |
| `ProductionHomePage` | `workspace-area work-plane` | default `workspace`, `framed={false}` |
| `ProductionMyWorkPage` populated | `workspace-area work-plane` | default `workspace`, `framed={false}` |
| `ProductionMyWorkPage` empty | `workspace-area work-plane assignment-board--hug` | `workspace` + `hug="board"` |
| `ProductionEnrollmentPage` | `workspace-area work-plane registry-wall` (+ `--hug` when 0–4 matching rows) | `bay="registry"` + `hug={registryTableHug(...)}` |
| `AssessmentActivitiesPage` | same | same |
| `ProductionEnrollmentDetailPage` | `workspace-area work-plane record-plane` | `bay="record"` `framed={false}` |
| `AssessmentSetupPage` | `… record-plane record-plane--setup` | `bay="setup"` `frameClassName="record-frame"` |
| `AssessmentCampaignCreatePage` | same | `bay="setup"` |
| `CeremonyArea` | ceremony bundle + optional danger | `bay="ceremony"` `danger={danger}` (keep hug/frame props) |
| Deck `FormRecipeSections` (3) | `workspace-area work-plane form-recipe` | `bay="recipe"` |
| Deck `SetupRecordSpec` | setup bundle | `bay="setup"` |
| Deck `LayoutSections` index / record / empty | `workspace-area` only | default `workspace` (**gains `work-plane`**) |
| Deck `LayoutSections` split | `workspace-area record-view` | `bay="ledger"` |
| Lab `HomePage` | `workspace-area board` (+ `assignment-board--hug` when empty) | `bay="homeBoard"` + `hug="board"?` |
| Lab `ReviewerPage` queue | `queue-view workspace-area` | `bay="queue"` |
| Lab `ReviewerPage` record | `record-view workspace-area` + `is-released` / `is-adjusting` | `bay="ledger"` + additive `className` |
| Lab `CampaignsArea` (3: registry, missing, record) | `campaigns-wall` | `bay="campaignsWall"`; keep `headClassName="campaigns-head"` |
| Lab `SampleArea` (2) | `campaigns-wall sample-wall` | `bay="sampleWall"` |
| Lab `EnrollmentsArea` (2) | `wall` | `bay="enrollmentWall"`; keep `headClassName="wall-head"` |
| `OperateArea.test.tsx` hug-column case | `workspace-area work-plane work-plane--ceremony` | `bay="ceremony"` (or drop that className) |

**Do not** add `.workspace-area` to lab walls.

Tests that encode strings (must stay green on **emitted** classes):

- `OperateArea.test.tsx`, `CeremonyArea.test.tsx`, `gallery-deck.test.tsx`
- `ProductionEnrollmentPage.test.tsx`, `AssessmentActivitiesPage.test.tsx` (hug)
- `ProductionEnrollmentDetailPage.test.tsx` (record, not setup)
- `AssessmentSetupPage.test.tsx`, `AssessmentCampaignCreatePage.test.tsx` (setup)
- `ProductionMyWorkPage.test.tsx` (hug vs bays)
- `App.test.tsx`, `ContractUnavailablePage.test.tsx`, `ErrorBoundary.test.tsx`, `production-routes.test.tsx` (ceremony)
- `pc-surfaces.test.tsx` (`.record-view`, `.queue-datatable`)
- `web/src/styles/style-entry.test.ts` — CSS selector contracts (**do not weaken**)

Docs that teach page-authored classes:

- `docs/ui-ux/design-system/components/layouts.md` (~line 45)
- `docs/ui-ux/design-system/components/layout-primitives.md` OperateArea example
- `docs/ui-ux/design-system/components/cards.md` (`record-plane` as a page class)
- `docs/ui-ux/design-system/implementation-guide.md` OperateArea paragraph
- `.agents/skills/frontend-developer/SKILL.md` and `.cursor/skills/frontend-developer/SKILL.md` OperateArea bullet (keep copies in sync)

## Family 2

| Pattern | Callers | Extract |
| --- | --- | --- |
| `.assignment-head` + ident + title + optional meta + optional status `dl` | `ProductionMyWorkDetailPage` (local helper, also used mid-page), `production-routes.tsx` denied heading (title only), lab `JourneyPage` (title + meta) | `AssignmentHead` in `chrome/`. Status slot optional |
| `.assignment-bays` / `.assignment-bay` / `.assignment-bay-head` | `ProductionMyWorkPage` only | `AssignmentBays`. Children remain `Grid` of plates. Tests query `.assignment-bays` and not `--dense` |
| `.bays` / `.bay` / `.bay-head` / `.bay-plates` / `.bay-empty` | lab `HomePage` only | `StatusBays`. **Not** `Grid`. CSS stays in `participant-home.css` |
| `ReadoutGrid className="assignment-instruments"` | Enrollment detail, `SetupCeremonyStation`, Deck `SetupRecordSpec`, Deck `FormRecipeSections` | `band="instruments"` |
| `StateReadout` `assignment-record` + `assignment-record-label` | Enrollment detail, assignment station (two sites in `ProductionMyWorkDetailPage`), Deck foundations | `mark="assignment"` |

## Family 3

Production first: `AssessmentActivitiesPage`, `ProductionEnrollmentPage` (index + assign picker with `is-selected` rows).

Then lab: `CampaignRegistry`, `EnrollmentTable` (keep export name `DataTable` for the feature; internally use `DatatableTable`), `ReviewerPage` queue (`manifest` additive), Deck `DataSections` **except** expand/detail markup.

`TableActionBar` already owns `.datatable-actions` for CampaignRegistry. Production Create/Assign strips need `DatatableActions`.

# Plan

- [x] Family 1 red: `operateAreaClass` + `OperateArea` tests for default `workspace`, every `bay`, hug apply/ignore, danger, additive className; existing structure tests omit required `className`
- [x] Family 1 green: implement helper + optional `className`; migrate every caller in the Family 1 table; CeremonyArea stops building class strings
- [x] Family 1 docs + skill: layouts, cards, layout-primitives, implementation-guide, both frontend-developer SKILL copies
- [x] Family 1 verify: Vitest list below + Playwright (production bays + Deck index/record/empty for `work-plane` + one lab wall to prove no `workspace-area`)
- [x] Family 2 red: AssignmentHead / AssignmentBays / StatusBays / ReadoutGrid band / StateReadout mark tests
- [x] Family 2 green: migrate callers; export from chrome + plates/readouts/state barrels
- [x] Family 2 docs: cards.md (two bay geometries), content.md (`band="instruments"`), layouts.md heading slot if it specifies assignment-head markup
- [x] Family 2 verify: Assignment station + My work + denied heading; lab Home if origin available
- [x] Family 3 red: primitive tests for DatatableTable/Row/Cell/Id/Actions (do not uncompile pages)
- [x] Family 3 green: implement primitives; migrate production tables; then lab registry/queue; Deck body rows if straightforward; leave expand/detail as recorded gap
- [x] Family 3 docs: tables.md public API is components; class names are implementation
- [x] Family 3 verify: Activities + Participants + assign dialog; Deck table if migrated
- [x] Reconcile grep gates; list leftovers; check TODO line 7 only if Families 1–3 done or leftovers explicit

# Family 1 implementation notes

```tsx
<OperateArea
  label="Participants"
  title="Participants"
  bay="registry"
  hug={registryTableHug(slice.total)}
  frameClassName="datatable-frame registry-frame"
  frameInset="flush"
/>
```

Export `type OperateBay` from plates.

Red tests (minimum):

1. Default → `workspace-area work-plane composition-stack`
2. Each `bay` emits the decision-table classes; wall bays have no `workspace-area`
3. `hug="registry"` on `registry` only; `hug="board"` on `workspace` / `homeBoard` only
4. Additive `className="is-released"` keeps bay classes
5. `danger` → `workspace-area--danger`
6. Existing tick / operate-scroll / hug-column tests pass without passing `className="workspace-area"`
7. `CeremonyArea` danger still yields `workspace-area--danger` on the region

Vitest (Family 1, after green + page migration):

```text
pnpm exec vitest run \
  web/src/design-system/components/plates/OperateArea.test.tsx \
  web/src/design-system/components/plates/CeremonyArea.test.tsx \
  web/src/pages/ProductionEnrollmentPage.test.tsx \
  web/src/pages/ProductionEnrollmentDetailPage.test.tsx \
  web/src/pages/AssessmentActivitiesPage.test.tsx \
  web/src/pages/AssessmentSetupPage.test.tsx \
  web/src/pages/AssessmentCampaignCreatePage.test.tsx \
  web/src/pages/ProductionMyWorkPage.test.tsx \
  web/src/pages/ProductionHomePage.test.tsx \
  web/src/styles/style-entry.test.ts \
  web/src/design-lab/features/gallery/gallery-deck.test.tsx \
  web/src/design-lab/pc-surfaces.test.tsx
```

Playwright Family 1 (`:5274`, ~1280 and 390 where hug/scroll changes):

- Admin Home destination grid
- Activities registry (0 rows, ≤4 hug, many rows if seed allows)
- Participants registry + Enrollment detail
- Setup and Create (52rem column, docked foot)
- My work empty hug vs populated grid
- Ceremony unavailable / sign-in
- Deck: `#layout-management-index`, `#layout-management-record`, `#layout-management-empty` (work-plane gain), setup + split ledger
- Lab Campaigns wall once: confirm **no** `.workspace-area` on the operate region

# Family 2 implementation notes

`AssignmentHead`: `title`, optional `meta`, optional `status` (the phase/record `dl`). Root `<header className="assignment-head">`. Shared assignment paint lives in `work-plates.css`; keep the narrow override in `participant-journey.css` (do not merge sheets).

`ProductionMyWorkDetailPage` uses the head pattern in more than the layout `heading` slot — migrate every local paste, not only the helper at the top of the file.

# Family 3 implementation notes

`DatatableId` must support `to` (Link) and `onClick` (button). Optional `children` leading slot is out of scope unless migrating Deck expand; production ids are text-only.

`DatatableRow` must allow `is-selected` (assign picker).

Do not wrap `th`. Headers already take `colMin`.

# Grep gates

Family 1 — no page/lab **JSX prop** of `className="workspace-area…"`. Strings may remain in `operateAreaClass.ts`, CSS, and `toHaveClass` assertions.

```text
rg "className=\{?['\"]workspace-area" web/src --glob '*.tsx'
rg "record-plane--setup" web/src --glob '*.tsx'
rg "registry-wall--hug" web/src --glob '*.tsx'
rg "campaigns-wall" web/src --glob '*.tsx'
```

Family 2:

```text
rg "className=\"assignment-head\"" web/src --glob '*.tsx'
rg "className=\"assignment-bays\"" web/src --glob '*.tsx'
rg "assignment-instruments" web/src --glob '*.tsx'
rg "assignment-record-label" web/src --glob '*.tsx'
```

Family 3 (hits allowed inside `design-system/components/datatable` and `patterns/TableActions.tsx`):

```text
rg "className=\"datatable-table\"" web/src --glob '*.tsx'
rg "className=\"datatable-id\"" web/src --glob '*.tsx'
rg "className=\"datatable-actions\"" web/src --glob '*.tsx'
```

# Current state

Frozen closed. Families 1–3 and the later domain-demotion chain shipped.
CSS class names still emit from components and domain wrappers. Leftover
paint nouns (`.readout--record`, `create-ceremony__scroll`, lab
`registry-wall--hug` on walls) stay as CSS. `CeremonyArea` stays in the
design system. New surfaces compose existing typed APIs or new domain
wrappers; they do not extend this task.

# Findings / deviations

- Readiness review: `tone` renamed to `bay` so it does not collide with ReadoutList `tone` or StateReadout “record”.
- Readiness review: `assignment` bay removed (duplicate of `workspace`); empty My work uses `hug="board"`.
- Readiness review: hug is `"registry" | "board"`, not boolean, so Home destination grid cannot accidentally take assignment-board hug.
- Readiness review: lab `DataTable` export forbids naming the primitive `DataTable`.
- Readiness review: Family 3 must not uncompile production as a red step; Deck expand/detail is an explicit leftover.
- Deck index/record/empty OperateAreas now include `work-plane` via default `bay="workspace"` (accepted alignment).
- `layouts.md` does not specify assignment-head markup; no heading-slot doc change.
- Family 3 Playwright for production Activities/Participants/assign dialog was not live-session verified (OIDC ceremony on `:5274`). Lab queue + Campaigns wall + Vitest cover the primitives.

# Class-grammar closure (2026-08-31, fourth pass — review remediation)

Shipped to close review findings:

- `ActionHeader` on Deck enrollment datatable specimen (`DataSections`)
- `WorkWellHead` `seal` + `WorkWellReleasedSeal`, `WorkWellHint`
- `DialogPlate` `presentation="ceremony"` with `DialogPlateNote`, `DialogPlateFootActions`, `DialogPlateFootRow`; `frozen` on plate
  (later removed from generic `DialogPlate`; see
  `.work/active/domain-ds-demotion-followup.md`)
- `FormPair` for paired field clusters (Deck form-recipes + inputs specimen)
- `ProtocolPlate` for dim protocol ident on journey/session rails + Deck pane specimen

Callers migrated: `CampaignConfigDialog`, `JourneyPage`, `SessionPage`, `FoundationsSections`, `FormRecipeSections`, `InputSections`, `DataSections`.

Vitest: `FormPair.test.tsx`, `ProtocolPlate.test.tsx`, `WorkWell.test.tsx`, `DialogPlate.test.tsx`, `CampaignConfigDialog.test.tsx`, `gallery-deck.test.tsx`, `pc-surfaces.test.tsx` (pass).

Remaining intentional page/lab class strings: session hull (`instrument-plate`,
`composer`, `ledger`/`turn`), reviewer record, gallery catalog chrome
(`spec-row`, `form-demo-*`), additive state (`is-released`, `is-hot`, `is-live`),
expand-row `rowClass` on lab enrollment table. Later:
`.ceremony-config-grid` is owned by `CampaignCeremonyConfigGrid`
(`.work/active/ds-domain-demotion-pass.md`).

# Doc drift (2026-08-31)

Synced `empty-loading.md`, `cards.md`, `modals.md`, and `change-record.md` with
component-owned empty-state APIs (`OperateArea` `empty.separated`,
`CeremonyEmpty` / `CeremonyUnavailable`, `DatatableEmpty`). Deck specimens may
still pass layout modifiers on `EmptyPlate` `className` for CSS documentation.

# Remaining gaps

None. Frozen. Do not start another class-grammar or demotion pass from this
file. Decorative internals, session inner hull, and gallery `spec-row` stay
unwrapped. CSS leftover names stay. `CeremonyArea` stays generic.

# Follow-up ergonomics (2026-08-31)

Design-system scope stays **generic** primitives only. Domain/business composition
(assignment phase/record status band, ceremony shells, intake lists) lives in
`web/src/components/`, `web/src/features/`, or pages.

Shipped in this follow-up:

- `DatatableStateReadout`, `SelectHeader` (datatable module)
- `DatatableIdCell`, `DatatableExpandButton`, `DatatableDetailRow` (expand/detail shell)
- `Key` `destructive` prop (replaces page-authored `key--danger`; transmit danger stroke in CSS)
- `AcknowledgmentGate` `presentation="plate" | "inline"` (replaces `className="control-line"` override)
- `ActivationMark` `compact` defaults `labelClassName="state-label"`
- `AssignmentStatusReadout` in `web/src/components/work/` (not design-system)
- Lab `EnrollmentTable` + Deck `DataSections` datatable specimens migrated to primitives

Production/lab callers migrated off redundant `StateReadout` table props and raw
`col-select` headers where `SelectHeader` / `DatatableStateReadout` apply.

# Class-grammar closure (2026-08-31, second pass)

- `SetupCeremony`, `SetupCeremonyScroll`, `SetupCeremonyFoot`
- `StateReadout` `emphasis="now"`
- `FrozenLine`, `InPlateHost`
- `ActivationMark` `placement="grid"`
- `FormRecipeDialog`, `FormRecipeDialogWell`

# Layering consistency (2026-08-31, review follow-up)

- `AssignmentStatusReadout` → `web/src/components/work/` (domain chrome, not design-system)
- `RailHomeLink`, `ProfileMenu` `placement="rail"`, generic `PhaseSpine` in design-system navigation
- `DatatableDetailBody` / `Readouts` / `Field` / `Keys`; `StaticHeader` `col-state`, `DatatableCell` `cell-result` via `colMin`

# Frame and readout ergonomics (2026-08-31, third pass)

- `OperateArea` `frame`: `record` | `registry` | `datatable` | `ceremony` via `operateFrameClass`
- Lab domain wrappers: `CampaignsOperateArea`, `EnrollmentWallOperateArea`, `SampleWallOperateArea`
- `ActionHeader`, `ReadoutList` `emphasis`, `IntakeItemList` (`components/work/`)
- Production pages: zero `frameClassName` / `headClassName`; docs + both frontend-developer skills updated

Page/lab JSX grep gates clean for `setup-ceremony`, `setup-track-now`, `frozen-line`, `in-plate-host`, `readout-grid-state`, and raw datatable expand classes.

Vitest: `SetupCeremony.test.tsx`, `InPlateHost.test.tsx`, `FrozenLine.test.tsx`, `StateReadout.test.tsx`, `ActivationMark.test.tsx`, `AssessmentSetupPage.test.tsx`, `AssessmentCampaignCreatePage.test.tsx`, `pc-surfaces.test.tsx`, `style-entry.test.ts` (56 tests, pass).

Docs/skills synced 2026-08-31: `tables.md`, `layouts.md`, both `frontend-developer` SKILL copies (`DatatableDetail*`, guided-task rail chrome, `AssignmentStatusReadout` layering).

# Layering follow-up (2026-08-31, review remediation)

- `AssignmentStatusReadout` relocated to `web/src/components/work/` (removed from design-system barrel)
- `StateReadout` `mark="sealed"` owns `.sealed-mark` (reviewer record head)
- `DataTableShell` `layout="queue"` owns `.queue-datatable`; `DatatableEmpty` `layout="queue"` owns `.queue-empty-plate`
- Lab `CampaignsUnavailableWell` owns `.campaigns-unavailable`

Vitest: `AssignmentStatusReadout.test.tsx`, `StateReadout.test.tsx`, `DataTableShell.test.tsx`, `pc-surfaces.test.tsx` (pass).

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| operateAreaClass / OperateArea bay tests | pass | `pnpm exec vitest run src/design-system/components/plates/operateAreaClass.test.ts src/design-system/components/plates/OperateArea.test.tsx` |
| CeremonyArea danger via OperateArea `danger` | pass | `CeremonyArea.test.tsx` (existing danger assertion) |
| Production page tests (hug, setup, record-plane, my-work) | pass | Family 1 Vitest list in this file |
| style-entry CSS contracts | pass | `src/styles/style-entry.test.ts` |
| Deck gallery + pc-surfaces | pass | `vitest.design-lab.config.ts` gallery-deck + pc-surfaces (51 tests) |
| Family 1 Playwright (incl. Deck work-plane + lab wall) | pass (partial production) | Deck evaluate: index/record/empty have `work-plane`; setup has `record-plane--setup`; split has `record-view` without `work-plane`. Lab Campaigns: `campaigns-wall` only. Ceremony sign-in desktop + 390. Artifacts: `.playwright-mcp/page-2026-08-30T12-20-15-271Z.png`, `page-2026-08-30T12-20-47-369Z.png`, `page-2026-08-30T12-21-20-328Z.png`, `page-2026-08-30T12-22-08-608Z.png` |
| Family 2 Playwright | pass (lab) | Lab Home four `.bay` columns, not Grid (`.playwright-mcp/page-2026-08-30T12-34-23-400Z.png`). Journey `.assignment-head` (`.playwright-mcp/page-2026-08-30T12-34-49-402Z.png`). Production My work / denied heading covered by page tests |
| Family 3 Playwright | pass (production + lab) | Production Activities `DatatableStateReadout` (`.playwright-mcp/page-2026-08-30T18-22-55-842Z.png` desktop, `.playwright-mcp/page-2026-08-30T18-24-05-476Z.png` 390). Participants registry + Assign dialog `SelectHeader` (`.playwright-mcp/page-2026-08-30T18-23-17-606Z.png`, `.playwright-mcp/page-2026-08-30T18-23-44-059Z.png`). Campaign create `SetupCeremony` (`.playwright-mcp/page-2026-08-30T18-24-37-125Z.png`). Lab queue prior artifact retained. |
| Production build | pass | `pnpm build` after `AssessmentCampaignCreatePage` `Stack` import + `FormEvent` typing |
| Grep gates per family | pass | Family 1–3 page JSX gates clean; expand/detail uses `DatatableIdCell` / `DatatableDetailRow`; only allowed additive `cell-result` on `DatatableCell` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
- [x] `TODO.md` line 7 checked only if Families 1–3 done or leftover explicitly listed under Remaining gaps
