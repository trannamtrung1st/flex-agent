---
id: component-owned-class-grammar
status: planned
created: 2026-08-30
updated: 2026-08-30
---

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
- `docs/ui-ux/design-system/components/content.md` — ReadoutGrid
- `docs/ui-ux/design-system/foundation/layout.md` — control / group / bay rungs
- `docs/contributing/development-harness.md` — attach `:5274` (candidate) with `:18080` healthy
- Prior discussion default: CSS remains private implementation; React is the public API; `className` is an escape hatch

# Scope

## In

Three sequenced families. Finish and verify each family before starting the next.

1. **OperateArea bay ownership** — typed `tone` (and hug/danger) replaces required `className` bundles.
2. **Assignment / bay / readout compositions** — `AssignmentHead`, Status/assignment bays, ReadoutGrid instrument tone, StateReadout record tone.
3. **Datatable body contract** — row/cell/id/empty primitives so production tables stop assembling `.datatable-table` / `.cell-*` / `.datatable-id` by hand. Prefer existing `TableActionBar` over duplicated action strips.

Update the design-system modules and examples so they document **props**, not
page-authored class strings. Keep emitting the same CSS class names so
`style-entry.test.ts` and scroll CSS keep working.

## Out

- CSS-in-JS, Tailwind, or moving visual values out of `web/src/styles/`
- 1:1 wrappers for decorative internals (ticks, notches, sheen, `.gangway-tick`, `.plate-foot-slot`, `.frame-node`)
- New layout primitives (`Box`, `sx`, arbitrary gap props)
- Changing product behavior, routes, copy, permissions, or scroll ownership
- Renaming CSS selectors in this task (unless a new `data-*` is strictly required and CSS + `style-entry.test.ts` are updated in the same step)
- `frameClassName` / `headClassName` string APIs (follow-up). Ceremony/datatable/campaigns/record frames stay `frameClassName` here.
- Live-session hull CSS, Status Bays **column geometry** (four-column `.bays` CSS stays domain CSS; Family 2 may wrap the markup)
- `web/src/components/` feature shells (`AssignmentStationLayout`, `SessionChrome`) except where they currently paste class soup that Family 2 extracts
- Open-ended repo-wide “scan every className”
- Impeccable polish/restyle. This is ownership refactor, visual parity.

## Follow-up (do not do in this task unless a family is already touching the file)

- Typed `frame` prop (`datatable` | `ceremony` | `campaigns` | `record`)
- `.control-line` on `AcknowledgmentGate` / enrollment (ControlLine already exists)
- `.frozen-line` / `.in-plate-host` lab campaign record
- `.phase-spine` internals (already inside `AssignmentSpine`)
- Schema-driven DataTable (columns-as-data). Family 3 is primitives, not a table DSL.

# Execution rules (agents)

1. Load `implementation-workflow` + `frontend-developer`. Do not start Impeccable extract/polish unless asked.
2. One family at a time. Mark only that step `[>]`.
3. **Red before green** for each family: extend component tests first, run them, then implement.
4. Do not restyle. After each family, screenshots must match the pre-change layout, spacing, and scroll behavior.
5. Keep CSS class **names** on the DOM. Page tests that query `.registry-wall--hug` / `.record-plane` stay valid because the component still emits them.
6. `className` on promoted components stays **optional and additive** (Deck extras, `is-released`, `is-adjusting`). It is not the primary API.
7. Clone existing production usage. Do not invent a fifth bay geometry.
8. If Family 3 balloons past a clean primitive set, **split** it to a new `.work/active/` file rather than shipping a half-DSL. Do not skip Family 1–2 docs/tests to rush tables.
9. Attach Playwright to healthy `:5274` with `:18080` `session-endpoint:ok`. Do not `compose:up` over a live stack. Prefer candidate overlay when SPA may lag `web/` source.
10. Update this file after each step: plan markers, Current state, Verification, Findings.

# Current inventory (2026-08-30)

## Already correct (do not “extract”)

- `Stack` / `Inline` / `Grid` / `Inset` / `Container` wrap `.composition-*` internally.
- `CeremonyArea` already maps hug + danger onto `workspace-area work-plane work-plane--ceremony` / `workspace-area--danger`. After Family 1 it should pass `tone="ceremony"` (and `danger`) instead of a class string.
- `DataTableShell`, `DataTableToolbar`, `DataTablePagination`, `SortableHeader`, `StaticHeader`, `CompactId`, `TableActionBar` already own their class names. Leakage is the **table body** and duplicated action strips.
- `web/src/components/` is feature composition (shell, assignment station, SafeContent), not a second design system.

## Family 1 leakage — OperateArea `className` required

`OperateArea` currently requires `className: string` and forwards it unchanged.
`CeremonyArea` is the only helper that owns a bundle.

| Caller | Current `className` | Target `tone` (+ flags) |
| --- | --- | --- |
| `ProductionHomePage` | `workspace-area work-plane` | `workspace` (default) |
| `ProductionMyWorkPage` populated | `workspace-area work-plane` | `workspace` |
| `ProductionMyWorkPage` empty | `workspace-area work-plane assignment-board--hug` | `assignment` + `hug` |
| `ProductionEnrollmentPage` | `workspace-area work-plane registry-wall` (+ `--hug` when 1–4 rows) | `registry` + `hug?` |
| `AssessmentActivitiesPage` | same as enrollment | `registry` + `hug?` |
| `ProductionEnrollmentDetailPage` | `workspace-area work-plane record-plane` | `record` |
| `AssessmentSetupPage` | `workspace-area work-plane record-plane record-plane--setup` | `setup` |
| `AssessmentCampaignCreatePage` | same as setup | `setup` |
| `CeremonyArea` | `workspace-area work-plane work-plane--ceremony` (+ `--danger`) | `ceremony` + `danger?` |
| Deck `FormRecipeSections` | `workspace-area work-plane form-recipe` | `recipe` |
| Deck `LayoutSections` setup specimen | `workspace-area work-plane record-plane record-plane--setup` | `setup` |
| Deck `LayoutSections` other | `workspace-area` / `workspace-area record-view` | `workspace` / `ledger` |
| Lab `HomePage` | `workspace-area board` (+ `assignment-board--hug` when empty) | `homeBoard` + `hug?` |
| Lab `ReviewerPage` queue | `queue-view workspace-area` | `queue` |
| Lab `ReviewerPage` record | `record-view workspace-area` (+ `is-released` / `is-adjusting`) | `ledger` + additive `className` |
| Lab `CampaignsArea` | `campaigns-wall` | `campaignsWall` |
| Lab `SampleArea` | `campaigns-wall sample-wall` | `sampleWall` |
| Lab `EnrollmentsArea` | `wall` | `enrollmentWall` |

**Do not** add `.workspace-area` to lab `campaigns-wall` / `.wall`. Nested scroll
ownership treats those as non-workspace hosts (`nested-scroll-ownership.md`).

Tests that encode the current strings:

- `web/src/design-system/components/plates/OperateArea.test.tsx` — every case passes `className="workspace-area"`
- `ProductionEnrollmentPage.test.tsx` / `AssessmentActivitiesPage.test.tsx` — hug class on `.work-plane`
- `ProductionEnrollmentDetailPage.test.tsx` — not `record-plane--setup`
- `CeremonyArea.test.tsx` / `gallery-deck.test.tsx` — `workspace-area--danger`
- `web/src/styles/style-entry.test.ts` — CSS selector contracts (do not weaken)

Docs that teach page-authored classes:

- `docs/ui-ux/design-system/components/layouts.md` line 45 (`className="workspace-area"`)
- `docs/ui-ux/design-system/components/layout-primitives.md` example (`className="workspace-area record-view"`)
- `docs/ui-ux/design-system/components/cards.md` (`record-plane`, `record-plane--setup` as page classes)
- `docs/ui-ux/design-system/implementation-guide.md` if it still shows class bundles

## Family 2 leakage — assignment / bay / instrument markup

| Pattern | Callers | CSS | Extract to |
| --- | --- | --- | --- |
| `.assignment-head` + ident + title + optional meta | `ProductionMyWorkDetailPage` (local helper), `production-routes.tsx` denied heading, lab `JourneyPage` | `app-shell.css`, `participant-journey.css` (narrow override — keep both sheets, one component) | `AssignmentHead` |
| `.assignment-bays` / `.assignment-bay` / `.assignment-bay-head` | `ProductionMyWorkPage` only | `app-shell.css` | `AssignmentBays` (section + heading; children are `Grid` of plates) |
| `.bays` / `.bay` / `.bay-head` / `.bay-plates` / `.bay-empty` | lab `HomePage` Status Bays | `app-shell.css` (domain hull, **not** `Grid`) | `StatusBays` wrapper; do not replace with `Grid` |
| `ReadoutGrid className="assignment-instruments"` | Enrollment detail, `SetupCeremonyStation`, Deck form/layout recipes | `app-shell.css` `.assignment-instruments` | `ReadoutGrid` `tone="instruments"` (or equivalent) |
| `StateReadout className="assignment-record"` + `labelClassName="assignment-record-label"` | Enrollment detail, assignment station head, Deck foundations | `app-shell.css` | `StateReadout` `tone="record"` |

## Family 3 leakage — datatable body

Shell/toolbar/pagination/actions already exist. Pages still paste:

- `<table className="datatable-table">`
- `<tr className="datatable-row">`
- `<td className="cell-id|cell-content|cell-state|cell-select|col-action">`
- `<Link\|button className="datatable-id">`
- `<EmptyPlate className="datatable-empty">`
- Custom `<div className="datatable-actions">` on **production** Enrollment and Activities (lab `CampaignRegistry` already uses `TableActionBar`)

Production tables to migrate first: `ProductionEnrollmentPage` (index + assign picker), `AssessmentActivitiesPage`.

Then lab: `CampaignRegistry`, `EnrollmentTable`, `ReviewerPage` queue, Deck `DataSections`.

# Decisions

- **CSS stays.** Components select classes; they do not re-express hairlines as inline styles.
- **Same selectors on the DOM.** `tone="setup"` still emits `workspace-area work-plane record-plane record-plane--setup` so scroll CSS and `style-entry.test.ts` keep matching.
- **Closed `tone` set** (interim default; lock in Family 1 red tests before coding):

  | `tone` | Emitted classes |
  | --- | --- |
  | `workspace` (default) | `workspace-area work-plane` |
  | `record` | `workspace-area work-plane record-plane` |
  | `setup` | `workspace-area work-plane record-plane record-plane--setup` |
  | `registry` | `workspace-area work-plane registry-wall` |
  | `assignment` | `workspace-area work-plane` |
  | `ceremony` | `workspace-area work-plane work-plane--ceremony` |
  | `ledger` | `workspace-area record-view` |
  | `queue` | `workspace-area queue-view` |
  | `recipe` | `workspace-area work-plane form-recipe` |
  | `homeBoard` | `workspace-area board` |
  | `campaignsWall` | `campaigns-wall` |
  | `sampleWall` | `campaigns-wall sample-wall` |
  | `enrollmentWall` | `wall` |

- **`hug?: boolean`**: with `registry` → add `registry-wall--hug`; with `assignment` or `homeBoard` → add `assignment-board--hug`. Hug threshold stays in the page (`rows.length > 0 && rows.length <= 4` for registries; empty board for assignment/home). Do not hide that rule inside OperateArea.
- **`danger?: boolean`**: add `workspace-area--danger` (CeremonyArea). May live only on CeremonyArea if OperateArea `tone="ceremony"` is enough and CeremonyArea keeps `danger`.
- **`className` optional additive** after the tone bundle (cx last). Required `className: string` goes away.
- **Family 3 interim**: primitives (`DataTable`, `DataTableRow`, `DataCell` with `kind`, `DatatableId`, empty via Shell). Not a column-schema component.
- **Production action strips**: replace hand-rolled `.datatable-actions` with `TableActionBar` when the page already has table-action semantics; if Enrollment/Activities only have a single Create/Assign key, a small `DataTableActions` host that owns `.datatable-actions` + `KeyGroup` is enough — do not force the full bulk-selection `TableActionBar` onto tables that have no selection.

# Plan

- [ ] Family 1 red: `OperateArea` defaults and tones without required `className`; CeremonyArea uses tone; existing hug/danger/setup assertions still pass
- [ ] Family 1 green: implement `tone` / `hug` / additive `className`; migrate every caller in the inventory table; `CeremonyArea` stops passing class strings
- [ ] Family 1 docs: layouts.md, cards.md, layout-primitives.md, implementation-guide.md — props not class bundles
- [ ] Family 1 verify: focused Vitest + Playwright bay screenshots (list below)
- [ ] Family 2 red: `AssignmentHead`, `AssignmentBays`, `StatusBays`, ReadoutGrid/StateReadout tones
- [ ] Family 2 green: migrate assignment station, denied heading, My work bays, lab Home bays, instrument readouts, record StateReadout
- [ ] Family 2 docs: cards.md (Status Bays / assignment bays), sidebars/layouts if heading slot is specified, content.md if ReadoutGrid tone is new
- [ ] Family 2 verify: Assignment station + My work + lab Home Playwright
- [ ] Family 3 red: table/row/cell/id/empty primitives; production Enrollment/Activities fail to compile or tests fail until migrated
- [ ] Family 3 green: migrate production tables first, then lab registry/queue/Deck; delete duplicated action markup
- [ ] Family 3 docs: tables.md documents components as the public API; class names remain implementation
- [ ] Family 3 verify: registry + assign dialog + Deck datatable Playwright
- [ ] Reconcile: grep for leftover page-authored grammar (queries below); record remaining gaps; mark TODO line 7 done only if Families 1–3 are complete or explicitly split

# Family 1 implementation notes

## API sketch (lock in red tests)

```tsx
<OperateArea
  label="Participants"
  title="Participants"
  tone="registry"
  hug={rows.length > 0 && rows.length <= 4}
  frameClassName="datatable-frame registry-frame"
  frameInset="flush"
/>
```

Helper lives next to `OperateArea` (e.g. `operateAreaClass.ts`) and is unit-tested
for every tone × hug × danger × extra className combination in the inventory.
Do not duplicate cx logic in CeremonyArea.

## Red tests (minimum)

1. Default (no `className`, no `tone`) → region has `workspace-area work-plane composition-stack`.
2. Each tone in the decision table emits the listed classes and not `workspace-area` on wall tones.
3. `hug` on `registry` / `assignment` / `homeBoard` only.
4. Additive `className="is-released"` keeps tone classes.
5. Existing structure tests (frame ticks, operate-scroll, hug column) still pass **without** passing `className="workspace-area"`.
6. `CeremonyArea` danger still yields `workspace-area--danger`.

Run: `pnpm exec vitest run web/src/design-system/components/plates/OperateArea.test.tsx web/src/design-system/components/plates/CeremonyArea.test.tsx`

Then migrate callers. Keep page tests that query emitted classes.

## Playwright (Family 1)

Attach `:5274`. Cover desktop (~1280) and narrow (390) where the bay hug/scroll
changes. Snapshot, do not restyle.

- Administrator Home destination grid
- Activities registry (empty if possible, and ≤4 rows hug, and many rows)
- Participants registry + nested Enrollment detail (unframed `record`)
- Setup and Create (`setup` 52rem column, docked foot)
- My work empty hug vs populated unframed grid
- Ceremony unavailable / sign-in (already `CeremonyArea`)
- Deck layout specimens: management record, setup, ledger if reachable

Evaluate: title alignment with wordmark, operate-scroll vs hug, setup column
width, no double pad, no accidental `workspace-area` on lab walls if those
routes are opened.

# Family 2 implementation notes

## AssignmentHead

Put in `web/src/design-system/components/chrome/` or `plates/` and export from
the production barrel. Props: `title`, optional `meta`, optional `status` slot
(phase + record StateReadout). Root is `<header className="assignment-head">`.
Do not move StatusReadout CSS. Guided-task `heading` slot consumes this
component; pages do not paste the header.

Callers: `ProductionMyWorkDetailPage`, `production-routes.tsx` access-denied
guided-task heading, lab `JourneyPage`.

## AssignmentBays vs StatusBays

These are **different geometries**. Do not unify them.

- Production My work: one bay, `Grid fit="fill"` plates. Component owns
  `.assignment-bays` / `.assignment-bay` / `.assignment-bay-head`.
- Lab Home: four-column `.bays` hull. Component owns that markup; CSS stays
  domain. Spec forbids using `Grid` for Status Bays columns.

## ReadoutGrid / StateReadout tones

Prefer a closed `tone` prop over `className`. Default keeps today’s un-toned
classes. `tone="instruments"` → `.assignment-instruments`. `tone="record"` on
StateReadout → `.assignment-record` + label class.

## Playwright (Family 2)

- `/my-work/:id` assignment station desktop + 390 (head plaque, status readout)
- Guided-task access denied heading
- `/my-work` populated bays
- Lab Home Status Bays if `:5275` or lab route is the donor (do not copy lab
  fixtures into production)

# Family 3 implementation notes

## Primitives (closed)

| Component | Emits | Notes |
| --- | --- | --- |
| `DataTable` | `table.datatable-table` | `hidden`, caption (visually hidden by default), children thead/tbody |
| `DataTableRow` | `tr.datatable-row` | optional detail row stays lab-only unless a production table needs it |
| `DataCell` | `td` + `cell-id` / `cell-content` / `cell-state` / `cell-select` / `col-action` | `kind` + `colMin` via existing `datatableColMin` |
| `DatatableId` | `Link` or `button` with `.datatable-id` | never underline; tick is CSS |
| Shell `empty` | keep `EmptyPlate` but default/add `datatable-empty` inside Shell when `empty` is passed, **or** a tiny `DataTableEmpty` wrapper |

Do not wrap every `th`; `SortableHeader` / `StaticHeader` already own heads.
Do not put `datatableColMin` on pages once `DataCell` / headers take `colMin`.

## Production migration order

1. `AssessmentActivitiesPage` (smaller)
2. `ProductionEnrollmentPage` index table
3. `ProductionEnrollmentPage` assign-dialog table (selection cells)
4. Lab `CampaignRegistry` (already has TableActionBar)
5. Lab `EnrollmentTable` / Deck `DataSections` / `ReviewerPage` queue

## Action strips

`ProductionEnrollmentPage` and `AssessmentActivitiesPage` currently duplicate:

```tsx
<div className="datatable-actions" aria-label="Table actions">
  <KeyGroup className="datatable-actions-keys" justify="end">
```

If those keys are table-level (Create, Assign) and not bulk-selection, extract
`DataTableActions` (`aria-label`, KeyGroup, classes) rather than misusing
`TableActionBar`. CampaignRegistry keeps `TableActionBar`.

## Playwright (Family 3)

- Activities registry rows, identifier link, empty plate, toolbar
- Participants registry + assign dialog table (selection, empty, pagination)
- Deck datatable specimen if still hand-built after migration

# Grep gates (before claiming a family done)

Family 1 — no production/lab **page** should pass a workspace/work-plane/record-plane
string into OperateArea. Allowed: additive modifiers (`is-released`, `is-adjusting`),
and `frameClassName` / `headClassName`.

```text
rg "className=\{?['\"]workspace-area" web/src --glob '*.tsx'
rg "record-plane--setup" web/src --glob '*.tsx'
rg "registry-wall--hug" web/src --glob '*.tsx'
```

Expect hits only in `OperateArea` / helper / tests asserting DOM classes / CSS.

Family 2:

```text
rg "className=\"assignment-head\"" web/src --glob '*.tsx'
rg "className=\"assignment-bays\"" web/src --glob '*.tsx'
rg "assignment-instruments" web/src --glob '*.tsx'
```

Family 3:

```text
rg "className=\"datatable-table\"" web/src --glob '*.tsx'
rg "className=\"datatable-id\"" web/src --glob '*.tsx'
rg "className=\"datatable-actions\"" web/src --glob '*.tsx'
```

Expect hits only inside design-system datatable/pattern modules (and tests).

# Current state

Planned. No code changes yet. Next: Family 1 red tests for OperateArea `tone`
defaults. Do not start Family 2/3 in the same pass as Family 1 implementation
unless Family 1 is already verified.

# Findings / deviations

- None yet. Inventory above is the 2026-08-30 source snapshot.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| OperateArea tone unit tests | pending | |
| CeremonyArea still maps danger | pending | |
| Production page tests (hug, record-plane) | pending | |
| style-entry CSS contracts | pending | do not weaken |
| Family 1 Playwright bays | pending | |
| Family 2 Playwright station/bays | pending | |
| Family 3 Playwright tables | pending | |
| Grep gates per family | pending | |

# Blockers

None.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [ ] Governing specifications were rechecked
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
- [ ] `TODO.md` line 7 checked only if Families 1–3 done or leftover explicitly listed under Remaining gaps
