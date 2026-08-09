# Flex Agent design system

Shared visual, interaction, accessibility, and product-pattern guidance for
Flex Agent.

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved v0.1 |
| **Owner** | UI/UX Lead |
| **Approvers** | Product Lead, UI/UX Lead |
| **Version** | 0.1 |
| **Effective date** | 2026-08-09 |
| **Last reviewed** | 2026-08-09 |
| **Approval reference** | Product and UI/UX approval confirmed in the design-system review task on 2026-08-09; `DS-DEC-1`–`DS-DEC-8` and `DS-PROP-1` approved |
| **Related decisions** | `DS-DEC-1`–`DS-DEC-8`; resolved `Q-DS-1`; approved `DS-PROP-1` |
| **Upstream authority** | [Concept model](../../product/concept-model.md), [MVP scope](../../product/mvp-scope.md), approved [feature specifications](../../requirements/README.md), and approved [UI/UX specifications](../README.md) |

**Approved v0.1** is authoritative for shared visual language, semantic tokens,
reusable component behavior, accessibility foundations, and recurring Flex
Agent UI patterns. All modules under this directory inherit this metadata unless
they declare narrower metadata.

## Authority and boundaries

This design system does not define product scope, authorize a capability,
change a lifecycle, or weaken an approved requirement.

Apply sources in this order within their concerns:

1. Approved product documents govern product meaning and release scope.
2. Approved feature specifications govern observable behavior and permissions.
3. A narrower approved UI/UX specification governs its journey and
   feature-specific interaction behavior.
4. This design system governs the shared presentation and reusable interaction
   pattern where the sources above leave that concern open.
5. Approved ADRs govern technical realization; code and tests must trace to the
   sources above.

If this design system conflicts with a narrower approved interaction
specification, follow the specification and propose a design-system correction.
Do not use a visual pattern to infer server authority, data availability, or an
MVP capability.

## Release applicability

The package deliberately includes patterns for later capabilities so their UI
can share one language. Inclusion is not a release commitment.

| Scope | Applicable modules |
| --- | --- |
| Assessment MVP | Shared foundations and components; attachments/submissions, conversation, empty/loading, Evidence, Evaluation/review, protected content, Result/Release, Session controls, technical metadata, and timeline patterns where the approved P0 specifications call for them |
| Later approved releases only | Agent-library management, general Harness management and improvement, configurable workflows, voice, tools, Dynamic memory, and stored-memory management |

The [MVP scope](../../product/mvp-scope.md) and owning approved feature
specification always decide whether a capability is available.

## Design direction

The approved visual language is **Deep-Space Operational Futurism**:

> A serious AI operating environment from the near future: deep-space dark,
> electrically precise, visibly alive when the Agent is active, calm when it is
> not, and unmistakably Flex Agent.

It should evoke a sophisticated onboard intelligence without becoming a gaming
HUD, cyberpunk theme, or fictional vocabulary layer. The experience must remain
credible for long conversations, configuration, review, and audit.

### Design DNA

- **Deep Space** — black and near-black application canvas.
- **Hull Panels** — dark navy operational surfaces with precise edges.
- **Signal Rails** — electric-blue lines identifying current context,
  selection, routing, or active computation.
- **Telemetry** — compact technical metadata, identifiers, time, versions,
  state, and provenance.
- **Agent Core** — abstract visual embodiment of the active Agent.
- **Observation Glass** — a bounded AI viewport that creates the feeling of
  looking through spacecraft glass at a present onboard intelligence.
- **Beacon** — cyan language reserved for genuine live state.
- **Scanner** — controlled blue focus and selection treatment.
- **Constellation Fields** — restrained dither/vector patterns for computation,
  memory, and Agent presence.

These are design metaphors, not product terms or required microcopy. Use the
canonical vocabulary from the [concept model](../../product/concept-model.md)
and ordinary action labels in the interface.

## Approved shared decisions

| ID | Decision | Status | Rationale |
| --- | --- | --- | --- |
| `DS-DEC-1` | Use dark-first Deep-Space Operational Futurism as the identity-defining theme while maintaining an accessible light theme. | Approved | Creates a distinct Agent-centered identity without making one color scheme the only operable experience. |
| `DS-DEC-2` | Use electric blue for interaction/current context and cyan only for genuine live/listening/speaking state; reserve semantic colors for their stated meanings. | Approved | Prevents visual state collisions and decorative misuse of status colors. |
| `DS-DEC-3` | Prefer coherent work planes, rails, dividers, and inspectors over floating card stacks. | Approved | Supports dense operational work and preserves hierarchy. |
| `DS-DEC-4` | Represent an active Agent with the abstract, stateful Agent Core rather than a generic chatbot avatar. | Approved | Makes Agent presence distinctive without implying human identity. |
| `DS-DEC-5` | Use two density modes: interaction and workspace. | Approved | Protects sustained reading while keeping administrative surfaces efficient. |
| `DS-DEC-6` | Treat WCAG 2.2 AA and the approved journey-level accessibility contract as the baseline for every shared pattern. | Approved | Aligns components with the approved P0 UI/UX specifications. |
| `DS-DEC-7` | Keep protected-content and authorization state explicit and non-disclosing across loading, denial, revocation, and recovery. | Approved | Prevents visual polish from weakening product security and privacy boundaries. |
| `DS-DEC-8` | Use bounded AI Observation Glass around the Agent Core and live interaction to evoke looking through a spacecraft viewport at an onboard intelligence; do not apply glassmorphism pervasively. | Approved | Captures the intended science-fiction presence while protecting transcript, form, table, Evidence, and review readability. |

## Structure

```text
design-system/
├── README.md
├── implementation-guide.md
├── foundation/
│   ├── accessibility.md
│   ├── borders.md
│   ├── colors.md
│   ├── density.md
│   ├── dither.md
│   ├── interaction-states.md
│   ├── layout.md
│   ├── motion.md
│   ├── radius.md
│   ├── shadows.md
│   ├── status.md
│   └── typography.md
├── components/
│   ├── accordion.md
│   ├── alerts.md
│   ├── avatars.md
│   ├── badges.md
│   ├── button-group.md
│   ├── buttons.md
│   ├── cards.md
│   ├── content.md
│   ├── dropdown.md
│   ├── error-summary.md
│   ├── icon-shapes.md
│   ├── inputs.md
│   ├── lists.md
│   ├── modals.md
│   ├── pagination.md
│   ├── radios-checkboxes-toggle.md
│   ├── sidebars.md
│   ├── tables.md
│   ├── tabs.md
│   └── tooltips-popovers.md
└── product/
    ├── agent-presence.md
    ├── attachments.md
    ├── conversation.md
    ├── empty-loading.md
    ├── evaluation.md
    ├── evidence.md
    ├── harness.md
    ├── memory.md
    ├── protected-content.md
    ├── result-release.md
    ├── session-controls.md
    ├── technical-metadata.md
    ├── timeline.md
    ├── voice.md
    └── workflow.md
```

The module files are the design-system contract. The
[implementation guide](implementation-guide.md) is a reading manifest and
completion checklist; it is documentation, not a repository skill. Reusable
agent instructions remain under `.agents/skills/` and `.cursor/skills/`.

## Token contract

- Foundation modules define semantic design tokens; component and product
  modules consume them.
- Token names such as `surface-primary`, `fg-muted`, and `brand-primary` are
  framework-agnostic roles, not CSS classes or utility names.
- An implementation should expose a namespaced mapping such as
  `--fa-surface-primary`; the approved technical realization may choose another
  equivalent mapping.
- Raw visual values belong only in the token implementation, visual regression
  fixtures, or documented exceptional artwork—not scattered component code.
- Both supported themes must map every required semantic token. Components must
  not swap colors manually per theme.
- A new token requires a distinct semantic role, both theme values where
  applicable, documented usage, and contrast/state verification.

## Use and verification

Before designing, implementing, or reviewing UI:

1. Read this document and the governing feature and UI/UX specifications.
2. Use the [implementation guide](implementation-guide.md) to select every
   applicable foundation, component, and product module.
3. Map each UI state to the owning `REQ-*`/`AC-*` criteria and interaction
   decision before applying visual treatment.
4. Verify semantic structure, keyboard behavior, focus, announcements,
   contrast, 400 percent zoom/reflow, reduced motion, and desktop/narrow states.
5. When a runnable UI exists, complete the repository Playwright MCP screenshot
   workflow and record evidence only in `.playwright-mcp/`.

## Restraint

Use blue/cyan emission, scans, grids, dither, notches, and signal motion only
when they communicate hierarchy, current context, Agent activity, or live state.

Avoid generic purple-pink AI gradients, glowing every border or icon,
full-screen animated starfields, pervasive glassmorphism, oversized rounded
cards, retro-terminal cosplay, gaming-HUD clutter, and motion unrelated to
actual system state. Observation Glass is bounded to Agent-presence and rare
high-salience regions; long-form conversation, forms, tables, Evidence, and
review content must remain calm and comfortable to read.

## Resolved question and approved proposal

### `Q-DS-1` — Font delivery and dependency approval

**Status:** Resolved on 2026-08-09 through approval of `DS-PROP-1`.

The prior interim default was to preserve the `--font-sans`, `--font-display`, and
`--font-mono` roles but use platform fallbacks until the frontend dependency and
self-hosting approach was reviewed.

**Approved decision `DS-PROP-1`:** use self-hosted, version-pinned copies of
Geist Sans, Space Grotesk, and IBM Plex Mono after verifying their licenses and
approved delivery artifacts. Never load them from a third-party origin in an
authenticated or Participant surface by default. Until those implementation
checks pass, the approved system fallback stacks remain active.

No design-system-level open questions remain in v0.1.

## Versioning and change control

Use semantic design-system versions. Material changes to token
meaning, state semantics, accessibility behavior, or product primitives require
review, an updated version, and downstream impact notes. Preserve superseded
decisions through version control; do not silently rewrite approved interaction
meaning.
