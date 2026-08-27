# Shared React layer — Shipboard Terminal

This Vite app is the official Flex Agent prototype source.

## CSS import order

`src/styles/index.css` loads fonts, then:

1. `tokens.css`
2. `base.css`
3. Component family sheets in `src/styles/components/` — `keys`, `chrome`, `navigation`, `plates`, `state`, `readouts`, `fields`, `menus`, `temporal`, `searchable`, `overlays`, `datatable`, `demo`. Searchable stays after temporal so date-picker rules keep their current cascade.
4. surface sheets scoped with `html[data-surface="…"]`

Do not convert this system to Tailwind, CSS modules, or CSS-in-JS. Colors, type, spacing, and motion stay in CSS. React maps semantic props to existing BEM classes or documented custom properties (`SelectPopoverConfig` → `--select-popover-*`).

The Component Deck (`/shared/gallery`) is the visual contract: specimens must import production components, not fork markup.

## Component API contract

Pick the smallest tool that fits:

| Tool | Use when | Examples |
|---|---|---|
| Typed variant props | Closed visual family | `Key variant` / `size`, `StateIndicator variant`, `FieldInput width` / `frozen`, `DropdownSelect variant` / `clearable`, `DropdownMenu placement` |
| Slots / children | Regions that compose | `DialogPlate` Head/Body/Footer, `DataTableShell` toolbar/table/empty/footer |
| Data recipes | Repeated structure from data | `ReadoutList` rows, `TableAction[]`, nav groups |
| CSS-variable config | Runtime geometry only | `SelectPopoverConfig` |
| `className` | Surface seating / layout only | wall vs campaigns-wall, ceremony wrappers |

Do not add `color`, `padding`, or `fontSize` props. `className` is not how variants are chosen.

Class strings compose with `cx` from `src/lib/cx.ts`. Breakpoints live in `src/lib/breakpoints.ts`; JS `useMediaQuery` must use `maxWidthQuery(...)`. CSS keeps the same pixel values, annotated as `/* bp.compact */` and so on.

## Shared React ownership

| Component | File | Notes |
|---|---|---|
| Command strip + profile | `src/components/chrome/` | Typed identity/actions (`chrome/operator.ts`) and responsive command-strip chrome. Canonical import is `src/components`. |
| Sectioned navigation | `src/components/navigation/SectionedNavigation.tsx`, `Gangway.tsx`, `IndexRail.tsx` | Shared typed group/item rendering with distinct product-route and reference-hash wrappers. `adminNav.ts` remains responsible for campaign-aware URLs; gallery scrollspy remains gallery-local. |
| Brand mark (`BrandMark`, `StripBrand`, `RailBrand`) | `src/components/chrome/Brand.tsx` | Canonical **FLEX AGENT** wordmark (`.brand-mark`) — same teal backlit glow on strip, rail, and examiner identity. `StripBrand` is the command-strip placard (wordmark + optional suffix via `.strip-brand--origin`). |
| Keys, icon buttons, empty plate, etched frame, stage bars, ack gate | `src/components/keys/`, `plates/`, `state/` | `IconButton` keeps a 22px visual footprint over an invisible 44px target and requires an accessible label. Amber ration is a composition rule, not a prop default. Empty plate inset (`.empty-plate--inset`) seats absence inside an etched frame without nesting a second card; add `.empty-plate--separated` only when it follows seated content and needs a dashed absence horizon. `OperateArea` is the shared operate-head + frame recipe. `.protocol-plate` is the dim protocol ident on a pane (journey + session rails; gallery Pane specimen). |
| State indicator | `src/components/state/` | Decorative `rest`, `live`, `sealed`, and `dim` marks (`StateIndicator`). Consumers own domain status mapping. `ActivationMark` is the campaign frozen/draft recipe on `StateReadout`. Import from `components` or `state/`. |
| Back control | `src/components/keys/BackKey.tsx` | Visible-label back key using the canonical chevron geometry. |
| Form field | `src/components/fields/FormField.tsx` | Control-slot composition that wires labels, optional `.field-hint`, `aria-invalid`, and error descriptions without owning form state or validation. Hint id is merged into `aria-describedby` before the error id. |
| Field input / textarea | `src/components/fields/FieldControls.tsx` | Shared slot controls. Own `is-invalid` from `aria-invalid` or `invalid`, `is-frozen` + `readOnly` from `frozen`, and `width` (`standard` / `narrow` / `wide`) on inputs. `FieldTextarea` defaults to locked height (`resize="none"`); `resize="vertical"` / `"both"` opt into grow for catalog or long-form seats. |
| Date / time pickers | `src/components/temporal/` | Custom `DatePicker`, `TimePicker`, and `DateTimePicker` on the field select-shell. Calendar is a Monday-start grid; time is 24h HH/MM wheels with optional `withSeconds` for HH/MM/SS. Never native `input type="date"`. Re-exported from the public `components` barrel. |
| Control line / radio / breaker | `src/components/fields/ControlLine.tsx` | Hidden native input + authored mark. `AcknowledgmentGate` composes the amber ack mark; `RadioGroup` and `Breaker` are the teal selection voice. |
| Dialog plate | `src/components/overlays/DialogPlate.tsx` | Native-dialog interior slots with narrow/default/wide widths. Product ceremonies may retain local visual classes while composing these slots. |
| Compact readouts | `src/components/readouts/ReadoutList.tsx` | Semantic `dl` rows for rails and compact instrument lists. |
| Readout grid | `src/components/readouts/ReadoutGrid.tsx` | Shared 2/3/4/6-column instrument grid with semantic rows and field spans; container-collapses to divided rows below 46rem |
| Admin readout band | `src/features/admin/ReadoutBand.tsx` | Campaign context bands. Activation copy is `ActivationMark` in `src/components/state/ActivationMark.tsx`: full sentence in context/readouts (`Draft — not activated` / `Frozen at activation`); `compact` for table cells and filters (`Draft` / `Frozen`). Same mark either way. |
| Campaign context | `src/features/admin/CampaignContext.tsx` | Shared connected picker + activation readout for Enrollments, Cohorts, and Sessions |
| Native dialog / ceremony / dropdown / sign-out | `src/components/overlays/`, `src/components/select/` | `CeremonyDialog` wraps `NativeDialog` only (`default` / `ceremony` / `release` class variants). It does **not** wrap `DialogPlate` — callers own Head/Body/Footer (or a local interior). Native dialog uses `HTMLDialogElement.showModal()`. Searchable selects, `DropdownMenu`, and date pickers are on the public `components` barrel. `DropdownSelect` accepts `frozen` (disabled + `.is-frozen` etch) and `variant` (`field` default, `toolbar` for datatable-style segments with growable popovers). `DisclosureMenu` defaults to `variant="toolbar"` and requires `selectedId` (option id) separate from the trigger `value` (display copy). |
| Searchable select core | `src/components/select/` | Shared filter, outside dismiss, open/close, result-count copy, `SearchableSelectPanel`, and `SearchableMultiSelect`. Public single-select trigger APIs are on the `components` barrel. |
| Dropdown menu | `src/components/menu/` | Shared action-menu shell (`role="menu"`): connected select-like popover seam, keyboard, outside dismiss. `CommandMenu`, `RowActionMenu`, and `ProfileMenu` compose it. Not a listbox. |
| Datatable composition | `src/components/datatable/` | `DataTableShell`, toolbar/readout/search, pagination, and typed sortable headers. The shell owns the sticky head rail height. `useTableController` (plus `sortAndFilterRows` / `pageRows`) owns generic filter/sort/page slicing; consumers pass `match` and `getSortValue` and own query reset, selection, and domain actions. |
| Enrollment datatable | `src/components/EnrollmentTable.tsx` | Enrollment-specific rendering and full-bleed expansion over the shared datatable composition. The gallery is the visual contract; reviewer uses `bodyOnly`. Filename avoids colliding with `datatable/` on case-insensitive volumes. |
| Table actions | `src/components/TableActions.tsx`, `tableSelection.ts`, `glyphs/ActionMenuGlyph.tsx` | `TableActionBar`, `TableSelectionBand`, `HeaderSelectionControl`, `RowActionMenu` (22px `.icon-button` ellipsis, same footprint as expand), confirmation; descriptor-driven table/bulk/row surfaces; domain eligibility stays consumer-owned. Toolbar overflow is the compact labeled More key on connected `DropdownMenu`. Row overflow uses flush fixed placement so the panel can escape the scrolling table. Re-exported from `src/components`. |
| Searchable multiselect | `src/components/select/SearchableMultiSelect.tsx` | Controlled multi-value listbox with filtering, keyboard traversal, counts, Clear/Done, outside dismissal, and focus return. |
| Toast dock | `src/components/overlays/ToastDock.tsx` | Shared React toast provider/dock used by products and gallery specimens. |
| Campaign form schema | `src/features/admin/campaignSchema.ts` | Zod + RHF on the activation ceremony |
| Component Deck | `src/features/gallery/GalleryDeck.tsx`, `gallerySections.ts`, `sections/` | Typed React catalog that renders production components. `IndexRail` and section content share one registry; `useGalleryScrollSpy` is reference-navigation behavior only. |

Surface layouts (`.board`, `.station`, `.console`, `.wall`, `.shell`, `.deck`) stay local. Prefer `import { … } from "../components"` for the public React API (keys, chrome, select/temporal, menu, readouts, datatable, overlays). Do not recreate `shell.tsx` or root re-export shims.

## Shared class contracts

- **Datatable row actions:** checkbox = bulk selection only (four-state header control owns page→matching escalation); `.datatable-id` opens the canonical record; `.icon-button.command-menu-trigger--icon` with chevron toggles inline detail when a table supports expansion (same 22px footprint as `RowActionMenu`). Row overflow is always `RowActionMenu` — never a compact `.key` wrapping the ellipsis. Ordinary `.cell-content` cells and row whitespace are inert and text-selectable — never attach row-level `onClick` handlers. Selected-row wash (`.is-selected`), reviewer hot priority (`.is-hot`), and expanded summary (`.is-expanded`) stay separate from identifier/disclosure affordances. `TableActionBar` stays mounted when actions exist: table-scope keys stay enabled; bulk keys disable at zero selection with reasons. `TableSelectionBand` sits in a compact row under the filter toolbar and shows live readout + unframed `.clear-action` only when rows are selected.
- **Keys:** `Key` accepts `size="compact" | "standard" | "large"` (`.key--compact`, default standard). Quiet-key padding is `--key-quiet-padding-block` / `--key-quiet-padding-inline` (defaults match `--key-padding-*`). Participant journey and session tighten `--key-quiet-padding-inline` to `18px` on the surface root. `TooltipHost` (`.tip-host`) wraps keys and icon buttons for hover/focus plaques and disabled reasons via `aria-describedby`.
- **Dropdown menu:** `DropdownMenu` is the shared command overlay (`role="menu"`). It uses the same connected popover seam as field/toolbar selects (flush trigger, no gap) but never commits a value — items fire actions. Toolbar **More** stays connected; row `RowActionMenu` uses flush `placement="fixed"` so the panel is not clipped by `.datatable-scroll`. Selects stay listboxes.
- **Searchable select:** Field and context plates commit on row pick and close. The foot key is Close (dismiss without changing). The committed option stays listed when the filter would hide it. Single-select rows use the option-menu tick; searchable multiselect uses `.select-mark` and suppresses the tick.
- **Option menu:** `.option-menu` is the listbox row grammar (hairline, teal-glass hover/focus, 7×1px tick). Ground/sheen live on `.popover-surface`; nested `.select-popover > .option-menu` is a transparent inner list. Positioning stays on the consumer. `.command-menu` is the action cousin (same hover/hairlines, no tick).
- **Optional single select:** `DropdownSelect clearable` accepts a nullable value and nullable `onChange`. The popover keeps value rows in the listbox and puts an unframed **Clear** text action (`.clear-action`) in the foot — same grammar as the datatable selection band and searchable multiselect. Required selects remain non-clearable; toolbar filters should prefer a semantic aggregate option such as **All stages** when that state has domain meaning.
- **Searchable multiselect:** `.multiselect.select-shell--field` reuses the single dropdown’s trigger and popover styling. The panel adds a combobox search field, `.multiselect-options[aria-multiselectable="true"]`, teal `.select-mark` rows, result and selection readouts, Clear, and Done. Pass `caseSensitive` for exact-case filtering; the default matches case-insensitively.

## Routes

- `/` — redirects to `/surfaces`
- `/surfaces` — prototype channel catalog (not product navigation)
- `/participant-home`
- `/participant-journey`
- `/participant-session`
- `/admin-console` — redirects to `/admin-console/enrollments`
- `/admin-console/campaigns` — Campaign Registry (`?campaign=` opens the record). CAM is the stable Campaigns domain item and always returns here.
- `/admin-console/cohorts` — sample/empty cohort register
- `/admin-console/enrollments`
- `/admin-console/sessions` — sample/empty session monitor
- `/admin-console/users-access` — sample/empty organization access
- `/admin-console/policies` — organization policy readouts (`ReadoutGrid`, 2×2)
- `/admin-console/audit-log` — sample/empty audit log
- `/reviewer-console`
- `/shared/gallery`

Channel metadata lives in `src/data/fixtures/surfaces.ts` and must stay aligned with
`src/app/router.tsx`.

## Commands

```bash
cd prototypes
pnpm dev
pnpm test
pnpm test:e2e
pnpm build
```
