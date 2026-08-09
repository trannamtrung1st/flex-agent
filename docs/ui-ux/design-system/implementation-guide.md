# Design-system implementation guide

This guide selects the design-system modules needed for Flex Agent UI work. It
is documentation, not a repository skill. Load the matching role skill from
`.agents/skills/` or `.cursor/skills/` first, then use this guide with the
governing requirements and UI/UX specifications.

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
   system; do not treat token names as framework utilities.
7. Define applicable initial, loading, empty, populated, pending, success,
   validation, error/retry, reconnecting, permission-denied, terminal, and
   responsive states before styling only the happy path.

## Foundation index

- [Accessibility](foundation/accessibility.md) — WCAG 2.2 AA, keyboard, focus,
  announcements, reflow, targets, contrast, motion, and media
- [Colors](foundation/colors.md) — surfaces, foregrounds, borders, brand, live,
  semantic, and emission tokens
- [Typography](foundation/typography.md) — interface, reading, display, and
  technical typography
- [Layout](foundation/layout.md) — spacing, shell, panes, rails, widths, and
  responsive behavior
- [Density](foundation/density.md) — interaction and workspace modes
- [Radius](foundation/radius.md) — engineered radii and optional notches
- [Borders](foundation/borders.md) — structural, control, selection, and focus
  boundaries
- [Shadows](foundation/shadows.md) — restrained depth and controlled emission
- [Motion](foundation/motion.md) — functional timing and reduced motion
- [Interaction states](foundation/interaction-states.md) — hover,
  focus-visible, active, selected, disabled, readonly, and loading
- [Dither](foundation/dither.md) — bounded constellation/vector fields
- [Status](foundation/status.md) — shared state grammar and visual mapping

## Component index

- [Buttons](components/buttons.md) and
  [button groups](components/button-group.md)
- [Inputs](components/inputs.md),
  [selection controls](components/radios-checkboxes-toggle.md), and
  [error summary](components/error-summary.md)
- [Alerts](components/alerts.md), [badges](components/badges.md),
  [cards/panels](components/cards.md), and [lists](components/lists.md)
- [Avatars](components/avatars.md) and
  [icon shapes](components/icon-shapes.md)
- [Accordion](components/accordion.md), [dropdown](components/dropdown.md),
  [modals](components/modals.md), [tabs](components/tabs.md), and
  [tooltips/popovers](components/tooltips-popovers.md)
- [Tables](components/tables.md), [pagination](components/pagination.md),
  [sidebars](components/sidebars.md), and [content/grid](components/content.md)

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

### MVP Text Session

- Foundations: accessibility, colors, typography, layout, density,
  interaction states, motion, status
- Components: buttons, inputs, alerts, error summary, modals
- Product: conversation, timeline, Agent presence, Session controls,
  empty/loading, protected content
- Governing specification: [Text Session](../text-session.md)

### Evidence, Evaluation, and Human Review

- Foundations: accessibility, colors, typography, layout, density,
  interaction states, status
- Components: buttons, lists, tables, tabs, modals, error summary, content
- Product: Evidence, Evaluation/review, technical metadata, timeline,
  protected content
- Governing specification:
  [Evidence, Evaluation, and Human Review](../evidence-evaluation-human-review.md)

### Result and Release

- Foundations: accessibility, colors, typography, layout, density,
  interaction states, status
- Components: buttons, alerts, badges, lists, modals, error summary, content
- Product: Result/Release, technical metadata, timeline, protected content
- Governing specification: [Result and Release](../result-release.md)

### Later voice Session

Read the voice and Agent-presence modules only after voice has an approved
release specification. Include conversation, timeline, Session controls,
motion, dither, status, buttons, and inputs.

## Completion checklist

- The UI traces to approved `REQ-*`/`AC-*` criteria and a governing interaction
  specification.
- Feature-specific behavior wins over a generic shared pattern.
- Every token used is declared and mapped in both supported themes.
- State remains understandable without color, animation, hover, or sound alone.
- Keyboard, focus, names, announcements, target sizes, contrast, zoom/reflow,
  reduced motion, and desktop/narrow behavior are verified.
- Protected content never appears before authorization or remains after access
  loss; inaccessible and nonexistent targets use the owning non-disclosing
  pattern.
- Live cyan, Agent Core motion, streaming markers, and voice signals reflect
  authoritative real state.
- AI Observation Glass creates one bounded Agent focal plane without placing
  transcript, form, table, Evidence, or review content beneath blur, reflection,
  texture, or motion.
- Product concepts remain distinct: Evaluation, Human revision, Review
  decision, Result, and Release are never collapsed.
- When the app is runnable, accessibility snapshots and desktop/narrow
  screenshots support the final UI/UX claim.
