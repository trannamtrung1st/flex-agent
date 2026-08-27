---
version: 1
slug: "prototypes-shared-gallery"
primary_target: "prototypes/src/routes/GalleryPage.tsx"
related_targets: ["prototypes/src/styles/index.css","prototypes/src/styles/tokens.css","prototypes/src/styles/components/","prototypes/src/components/README.md"]
---

# Surface brief — Component Deck (shared-layer catalog)

## Scope and mode

Read. The living catalog of the Shipboard Terminal shared React layer at `/shared/gallery` in the official prototypes app. Specimens render the production components from `prototypes/src/components/` over the existing token/class system in `prototypes/src/styles/`. Related targets: tokens, components CSS, and `prototypes/src/components/README.md`. Not a product surface — documentation for agents and implementers.

## Topology

Command strip (brand · mode placard · ident readout) over a two-column deck: sticky index rail left, long specimen column right. Body scrolls naturally (`height: auto`); unlike the five Operate surfaces, no `100dvh` lock. Index links jump to section anchors; scrollspy marks the current section with the listbox selected grammar (teal voice + 7×1px tick). Footer carries synthetic-content note.

## Audience, job, constraints

A designer or agent implementing or extending the shared layer. Job: find every shared class, see variants and states, and copy the correct markup contract. WCAG 2.2 AA on interactive specimens (dropdown keyboard, dialog Escape, focus-visible on keys and tooltips). Synthetic specimen content only.

## Sections (index order)

Colors (token chips), Type voices, Keys (compact/standard/large quiet + hot family, TooltipHost, including `.is-waiting`), Pane cuts (including `.protocol-plate`), Etched frame, Instrument marks, Select mark (four header states), Readout rows, Datatable (persistent actions strip, filter/search toolbar, multi-sort heads, header-driven selection, expansion, pagination), Form controls (field states including helper/disabled/frozen, pair row, dropdown, ack, radio, breaker, composer), Date & time (calendar, chrono wheels, combined picker, invalid, frozen), Search select, Search multiselect, Option menu, Dialog, Toast, Tooltip, Advisory, Empty state, Wait & progress (wait-mark, wait-copy, scan track, stage bars, skeleton, wait-plate, occupied keys).

## Interactive specimens

- Dropdown: open/close, arrow keys, Enter select, outside click dismiss; the optional single-select specimen uses the opt-in nullable clear contract while required selects remain non-clearable.
- Search multiselect: `SearchableMultiSelect` open/close, inline filtering, multiple teal selections, result and selection counts, Clear and Done actions, outside click dismiss, focus return, and keyboard traversal. Matching is configured with the typed `caseSensitive` prop; the specimen uses the case-insensitive default.
- Field input: live amber validation on invalid `mm:ss` with standing `MM:SS` hint; disabled + helper (`.field-hint`); frozen etch on text and dropdown (control state — sealed campaign records stay readout); optional single select with unframed Clear text action and empty placeholder; `.form-row--pair` two-field horizon (48px gap; hint/error wrap under their field); stacked `FieldTextarea` beside `.field-textarea--resize-y`. Demo rows use the same 148px label column as ceremony `.form-row`.
- Search select: row pick commits and closes; Close dismisses; committed option stays visible when filtered.
- Date & time: `DatePicker`, `TimePicker`, and `DateTimePicker` on the temporal select-shell; empty invalid date, frozen etch, calendar + chrono popover with Clear/Done. Session mark uses HH/MM; Sync mark opts into HH/MM/SS via `withSeconds`.
- Dialog: native `<dialog>` confirm plate with readout and release key; size variants — narrow (412px) single-question confirm and wide (680px) form plate carrying `.form-row` fields, tall bodies scrolling inside the plate.
- Bulkhead drawers: leading navigation, trailing readout, and wide (420px) trailing form drawer with `.form-row` fields; Escape, scrim, and Close dismiss with focus return.
- Gangway: the production `Gangway` React component inside a miniature shell frame; the controlled toggle folds it between 232px and the 76px channel-code rail, collapsed links speak through trailing tooltip plaques, and the demo canvas beside it shows the reflow. Specimen shows stable Administrator area navigation with CAM and current ENR; narrower working context stays inside the active page.
- Toasts: system + attention slips dock bottom-right.
- Tooltip: `data-tip` on hover and keyboard focus.
- Datatable: 100 synthetic enrollments with filter, search, multi-column sort by default (each head click adds or cycles a key; rank numbers on heads; set `singleSort: true` in specimen config for one column only), teal row selection (persists across pages), expandable row detail, pagination foot (range readout + rows-per-page + page selector + prev/next). The action strip stays visible at rest: gallery-only Create stays enabled; Export, Download, and compact More disable until rows are selected (with accessible reasons). Each row uses the shared `RowActionMenu` 22px ellipsis (same as Campaign Registry), not a compact key. Selection readout and unframed Clear appear only with a selection; matching escalation is header-driven (no separate toolbar key). Table height fits the current page's rows, capped by `--datatable-max-height`.
- Wait key: Retrieve manifest occupies the quiet key for 2.2s (`.is-waiting` + wait-mark), then fires a system toast.

## Breakpoints

- ≤900px: rail becomes horizontal wrap band above main column.
- ≤720px: command strip wraps (ident full width); toast dock stretches; composer specimen gets taller min-height for wrapped placeholder.

## Implementation fidelity inventory

| Ingredient | Medium |
| --- | --- |
| Shared component specimens | Production React modules documented in `components/README.md` |
| Deck layout and section grammar | `features/gallery/GalleryDeck.tsx`, `sections/`, and local `gallery.css` |
| Index and section ordering | `gallerySections.ts` consumed by `IndexRail` and typed `GallerySection` |
| Scrollspy | Gallery-local `useGalleryScrollSpy` hook |
| Token chips | `tokens.css` values rendered by typed JSX specimens |

## States and ranges

Static specimens for colors, type, marks, readouts, option menu, advisories, empty plate, wait-mark, scan (determinate and indeterminate), stage bars, skeleton, wait-plate. Interactive states demonstrated on keys (hover/focus/disabled/waiting), form (invalid, helper, disabled, frozen, pair, open dropdown), searchable multiselect (filtered, selected, empty, keyboard focus), dialog (open), toast (fired), tooltip (focus), datatable (all control states on synthetic data).

## Anti-patterns for this surface

Do not invent components here that bypass the shared family sheets in `prototypes/src/styles/components/`. Do not add build steps. Do not treat deck-local layout classes as shared primitives. Do not add a spinner or a third status color for loading — wait is teal instrument motion.
