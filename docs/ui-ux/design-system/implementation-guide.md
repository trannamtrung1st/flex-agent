# Design-system implementation guide

This guide selects the design-system modules needed for Flex Agent UI work. It
is documentation, not a repository skill. Load the matching role skill from
`.agents/skills/` or `.cursor/skills/` first, then use this guide with the
governing requirements and UI/UX specifications.

Design-system **v1.0 is Approved**. Use it as the target visual contract.
Do not use v0.1 Deep-Space styling as the target look.

## Before implementation

1. Read the [design-system status and authority](README.md#authority-and-boundaries).
2. Identify the governing product scope, feature specification, approved UI/UX
   specification, actor, permissions, and `AC-*` criteria.
3. Read [accessibility](foundation/accessibility.md),
   [colors](foundation/colors.md), [typography](foundation/typography.md),
   [layout](foundation/layout.md), [density](foundation/density.md),
   [interaction states](foundation/interaction-states.md), and
   [status](foundation/status.md).
4. Read the modules for every rendered component and product pattern.
5. Choose `interaction` or `workspace` density for each coherent region.
6. Map every semantic design token explicitly into the implementation styling
   system; do not treat token names as framework utilities. Dark primitives
   live in `web/src/styles/tokens.css`; semantic aliases in
   `semantic-aliases.css`; light remaps in `adaptations.css`. Prototype
   `--ground` / `--teal` / `--amber` / `--danger` values remain the dark-theme source.
7. Define applicable initial, loading, empty, populated, pending, success,
   validation, error/retry, reconnecting, permission-denied, terminal, and
   responsive states before styling only the happy path.
8. Apply `PC-01`–`PC-14` so prototype visuals cannot change repo behavior.
9. Verify specimens in the isolated design-lab Component Deck
   (`/design-lab/shared/gallery`). Production routes never include
   `/design-lab/*`. Clone a matching existing production page and Deck
   specimen ([visual evidence](README.md#visual-evidence)); do not invent
   chrome or copy lab fixtures. Select one closed-set
   layout family from [layouts](components/layouts.md); do not compose outer
   chrome in a page module. Inside the chosen family, compose slot content with
   [layout primitives](components/layout-primitives.md) (`Stack`, `Inline`,
   `Grid`, `Container`, `Inset`, `SplitBay`) instead of one-off flex/grid/spacing CSS.
   Inner page/form rhythm uses control / group / bay tokens (`--field-label-gap`,
   `--form-group-gap`, `--operate-bay-gap`): `OperateArea` owns bay strata
   (head / context / advisory / optional frame); titled field clusters use
   `FormSection`. `gap="none"` is only for fused plates.
   Shells wrap main content in an even `Inset` when `contain` is true
   (management and reference catalog default on; hull shells and the Component
   Deck default off). That pad is not a max-width column.

## Foundation index

- [Accessibility](foundation/accessibility.md) — WCAG 2.2 AA, keyboard, focus,
  announcements, reflow, targets, contrast, motion, forced colors
- [Colors](foundation/colors.md) — hull, phosphor teal, rationed amber,
  semantic success/danger, emission
- [Typography](foundation/typography.md) — Michroma placards, Sometype Mono
- [Layout](foundation/layout.md) — spacing, shells, gangway/bulkhead, widths
- [Density](foundation/density.md) — interaction and workspace modes
- [Radius](foundation/radius.md) — zero radius, notches, circular exceptions
- [Borders](foundation/borders.md) — hairlines and dashed absence
- [Shadows](foundation/shadows.md) — glass inset, overlay umbra, emitters-only
  glow
- [Motion](foundation/motion.md) — functional timing and reduced motion
- [Interaction states](foundation/interaction-states.md) — hover, focus,
  selected, disabled, frozen, occupied
- [Dither](foundation/dither.md) — bounded Core fields and traces
- [Status](foundation/status.md) — shared state grammar and instrument marks

## Component index

- [Buttons (keys)](components/buttons.md) and
  [button groups](components/button-group.md)
- [Inputs](components/inputs.md),
  [selection controls](components/radios-checkboxes-toggle.md), and
  [error summary](components/error-summary.md)
- [Alerts / advisories / toasts](components/alerts.md),
  [badges/marks](components/badges.md),
  [plates](components/cards.md), and [lists](components/lists.md)
- [Operator identity](components/avatars.md) and
  [icon shapes](components/icon-shapes.md)
- [Accordion](components/accordion.md), [dropdown/listbox](components/dropdown.md),
  [modals](components/modals.md), [tabs](components/tabs.md), and
  [tooltips](components/tooltips-popovers.md)
- [Tables](components/tables.md), [pagination](components/pagination.md),
  [gangway/rails/breadcrumbs](components/sidebars.md),
  [layouts (shells)](components/layouts.md),
  [layout primitives](components/layout-primitives.md), and
  [content/readout grid](components/content.md)

## Component Deck catalog

Map Deck section ids in `gallerySections.ts` to the governing module. Specimens
are visual evidence; the module is the contract.

| Deck group | Section ids | Module |
| --- | --- | --- |
| Foundations | `colors` | [colors](foundation/colors.md) |
| Foundations | `type`, `typography` | [typography](foundation/typography.md) |
| Foundations | `keys`, `key-group` | [buttons](components/buttons.md), [button groups](components/button-group.md) |
| Foundations | `pane`, `frame` | [plates](components/cards.md) (`pane` includes the WorkWell section-label vs list-tick specimen) |
| Navigation | `nav-rail` | [sidebars](components/sidebars.md) (`.nav-rail` grammar; `IndexRail` on the reference shell) |
| Navigation | `strip`, `gangway`, `drawer`, `footer` | [layouts](components/layouts.md), [sidebars](components/sidebars.md) |
| Navigation | `breadcrumbs` | [sidebars](components/sidebars.md) |
| Navigation | `tabs` | [tabs](components/tabs.md) |
| Data | `marks` | [badges](components/badges.md) |
| Data | `select-mark` | [selection](components/radios-checkboxes-toggle.md), [tables](components/tables.md) |
| Data | `readout`, `readout-grid` | [content](components/content.md) |
| Data | `compact-id` | [tables](components/tables.md), [technical metadata](product/technical-metadata.md), [tooltips](components/tooltips-popovers.md) |
| Data | `datatable`, `datatable-scroll` | [tables](components/tables.md), [pagination](components/pagination.md) |
| Feedback | `toast`, `advisory`, `alert` | [alerts](components/alerts.md) |
| Feedback | `tooltip` | [tooltips](components/tooltips-popovers.md) |
| Feedback | `error-summary` | [error summary](components/error-summary.md) |
| Feedback | `empty`, `wait`, `wait-panel` | [empty/loading](product/empty-loading.md) |
| Shells | `layout-*` | [layouts](components/layouts.md) |
| Composition | `composition-*` | [layout primitives](components/layout-primitives.md) (`composition-recipes` are specimen recipes, not a fifth primitive) |
| Overlays & input | `form-recipes` | [inputs](components/inputs.md), [error summary](components/error-summary.md), [layout primitives](components/layout-primitives.md), [plates](components/cards.md), [modals](components/modals.md) |
| Overlays & input | `form`, `file`, `datetime` | [inputs](components/inputs.md), [attachments](product/attachments.md) |
| Overlays & input | `searchable-select`, `multiselect`, `menu` | [dropdown](components/dropdown.md) |
| Overlays & input | `dialog` | [modals](components/modals.md) |

## Product-pattern index

- [Conversation](product/conversation.md), [timeline](product/timeline.md),
  [technical metadata](product/technical-metadata.md), and
  [Agent presence](product/agent-presence.md)
- [Attachments/submissions](product/attachments.md),
  [Session controls](product/session-controls.md), and
  [empty/loading](product/empty-loading.md)
- [Evidence](product/evidence.md), [Evaluation/review](product/evaluation.md),
  [Result/Release](product/result-release.md), and
  [protected content](product/protected-content.md)
- [Memory](product/memory.md), [Harness](product/harness.md),
  [workflow](product/workflow.md), and [voice](product/voice.md)

Later-release modules are reusable design preparation only. Do not render or
enable a capability unless the current release scope and an approved feature
specification authorize it.

## Example module selections

### MVP authentication shell, Home, and Activities

- Foundations: accessibility, colors, typography, layout, density,
  interaction states, status
- Components: keys, gangway/command strip, plates, alerts, dropdown
- Product: empty/loading, protected content, technical metadata
- Governing specification: [Activity journey](../activity-campaign-journey.md)
- States: unauthenticated, loading, denied, ready, context replacement, logout
- Gallery: command strip, gangway/bulkhead, quiet keys, empty plate (Deck:
  `strip`, `gangway`, `keys`, `empty`)

### MVP Campaign setup and Enrollment

- Foundations: accessibility, colors, typography, layout, density,
  interaction states, status, motion
- Components: plates (`OperateArea` clip vs grouping: Enrollment stacked
  record unframed; setup/create ceremony framed at 52rem), keys, inputs,
  selection, error summary, tables, pagination, modals, readout grid
- Product: empty/loading, technical metadata, protected content, attachments
  where Enrollment lists require them
- Governing specification:
  [Assessment Campaign setup](../assessment-campaign-setup.md) and
  [Submission and Attempt](../submission-attempt.md) (administrator Enrollment)
- States: draft, invalid, pending, active, stale, conflict, denied, empty,
  large table, narrow
- Gallery: form controls, number field, datatable, dialog, layout (Deck:
  `form`, `datatable`, `dialog`, `layout`)
- Constraints: `PC-05`, `PC-06`, `PC-09`, `PC-11`

### MVP Participant My Work and Submission intake

- Foundations: accessibility, colors, typography, layout, density,
  interaction states, status
- Components: plates, keys, inputs, alerts, error summary, lists
- Product: attachments/submissions, empty/loading, protected content
- Governing specification: [Submission and Attempt](../submission-attempt.md)
- States: no assignment, loading, denied, versioned submission, upload
  validation, pending, cancelling, reconciling, duplicate, conflict,
  unavailable, narrow
- Constraints: `PC-03`, `PC-07` (rail cannot mutate lifecycle)

### MVP Text Session (production contract-unavailable; lab composition donor)

- Foundations: accessibility, colors, typography, layout, density,
  interaction states, motion, status
- Components: keys, inputs, alerts, error summary, modals
- Product: conversation, timeline, Agent presence, Session controls,
  empty/loading, protected content
- Governing specification: [Text Session](../text-session.md)
- Constraints: `PC-08`; no production simulator. Approved family is
  `live-session`; production locator stays `management` until the host
  contract exists.

### Evidence, Evaluation, and Human Review (production contract-unavailable; lab composition donor)

- Foundations: accessibility, colors, typography, layout, density,
  interaction states, status
- Components: keys, lists, tables, tabs, modals, error summary, content
- Product: Evidence, Evaluation/review, technical metadata, timeline,
  protected content
- Governing specification:
  [Evidence, Evaluation, and Human Review](../evidence-evaluation-human-review.md)
- Constraints: `PC-01`, `PC-02`, `PC-04`. Approved Review-case family is
  `guided-task`; production locators stay `management` until the host
  contract exists.

### Result and Release (production contract-unavailable; lab composition donor)

- Foundations: accessibility, colors, typography, layout, density,
  interaction states, status
- Components: keys, alerts, marks, lists, modals, error summary, content
- Product: Result/Release, technical metadata, timeline, protected content
- Governing specification: [Result and Release](../result-release.md)
- Constraints: `PC-01`, `PC-03`, `PC-04`. Approved Release-record family is
  `guided-task`; production locators stay `management` until the host
  contract exists.

### Later voice Session

Read the voice and Agent-presence modules only after voice has an approved
release specification. Include conversation, timeline, Session controls,
motion, dither, status, keys, and inputs.

## Completion checklist

- The UI traces to approved `REQ-*`/`AC-*` criteria and a governing interaction
  specification.
- Feature-specific behavior wins over a generic shared pattern.
- Every token used is declared and mapped in both supported themes.
- State remains understandable without color, animation, hover, or sound alone.
- Keyboard, focus, names, announcements, target sizes, contrast, zoom/reflow,
  reduced motion, forced colors, and desktop/narrow behavior are verified.
- Protected content never appears before authorization or remains after access
  loss; inaccessible and nonexistent targets use the owning non-disclosing
  pattern.
- Live teal, Agent Core motion, streaming markers, and wait instruments
  reflect authoritative real state.
- Smoked-glass plates do not place transcript, form, table, Evidence, or review
  content beneath blur, reflection, texture, or motion.
- Product concepts remain distinct: Evaluation, Human revision, Review
  decision, Result, and Release are never collapsed.
- Prototype routes, Candidate/Docket/Marginalia product nouns, and fixture
  mutations are absent from production.
- When the app is runnable, accessibility snapshots and desktop/narrow
  screenshots support the final UI/UX claim.
