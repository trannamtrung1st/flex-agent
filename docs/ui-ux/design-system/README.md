# Flex Agent design system

Shared visual, interaction, accessibility, and product-pattern guidance for
Flex Agent.

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved v1.0 |
| **Owner** | UI/UX Lead |
| **Approvers** | Product Lead, UI/UX Lead |
| **Version** | 1.0 |
| **Effective date** | 2026-08-27 |
| **Last reviewed** | 2026-08-27 |
| **Approval reference** | Repository-owner Shipboard Terminal direction in task `impeccable-frontend-rebuild`; 2026-08-27 completeness review (product-scope, UI/UX, accessibility, architecture, security/privacy) found no escalation-threshold conflict. Light-theme teal darkened to `#146261` for 4.5:1. `DS-DEC-1`/`DS-DEC-2`/`DS-DEC-8` superseded; `DS-DEC-9`–`DS-DEC-11` and `DS-PROP-2` accepted. |
| **Related decisions** | `DS-DEC-3`–`DS-DEC-7` retained; `DS-DEC-1`, `DS-DEC-2`, `DS-DEC-8` superseded; `DS-DEC-9`–`DS-DEC-11` added; `DS-PROP-1` superseded by `DS-PROP-2`; resolved `Q-DS-1` |
| **Upstream authority** | [Concept model](../../product/concept-model.md), [MVP scope](../../product/mvp-scope.md), approved [feature specifications](../../requirements/README.md), and approved [UI/UX specifications](../README.md) |
| **Supersedes** | Approved v0.1 Deep-Space Operational Futurism (Git history and [change record](change-record.md)) |

**Approved v1.0** is authoritative for shared visual language, semantic tokens,
reusable component presentation, accessibility foundations, and recurring Flex
Agent UI patterns. Approved P0 interaction specifications still govern
journeys, states, copy meaning, permissions, and accessibility contracts. All
modules under this directory inherit this metadata unless they declare
narrower metadata.

v0.1 Deep-Space styling is superseded for visual identity. Do not use it as
the target look for the frontend rebuild.

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

Prototype surfaces are visual evidence only. They do not define routes, actor
names, action availability, disclosure, or lifecycle. Pre-resolved prototype
conflicts `PC-01`–`PC-14` in the rebuild task remain implementation
constraints.

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

The visual language is **Shipboard Terminal**.

A governed examination held on a working ship's console: smoked-glass planes,
hairline bezels, phosphor-teal systems, and rationed amber attention. The
lineage is hard sci-fi working-ship instrumentation, not consumer chat. The
system refuses rounded chat bubbles, smiling avatars, pill buttons, and
floating card stacks.

It must remain credible for long conversations, configuration, review, and
audit. Decorative instrument language may appear as secondary visual detail
when comprehension is unaffected; canonical product nouns and ordinary action
labels always come from the [concept model](../../product/concept-model.md)
and approved UI/UX specifications (`PC-10`).

### Design DNA

- **Hull ground** — near-black blue-green canvas (`#07141b`) with a deeper top
  and a faint sheen at the bottom.
- **Smoked-glass planes** — translucent instrument plates with 1px teal
  hairline bezels, inset edge-light, and bottom vignette. Not a floating card
  stack and not pervasive backdrop-blur over reading content.
- **Notched geometry** — zero authored border-radius; 10–18px clipped corners;
  circular exceptions only for node terminals, Agent Core, radio marks, and
  scrollbar thumbs.
- **Phosphor teal** — system life: context, selection, focus, wait
  instruments, sealed/ready marks, and genuine live Agent state.
- **Signal amber** — rationed attention: time, the current commitment, the
  active turn or stage, validation, and destructive confirmation emphasis.
- **Instrument marks** — node dots, hairline traces, scan tracks, and drawn
  state glyphs. State never relies on a colored blob, filled pill, or spinner.
- **Two type voices** — Michroma placards name; Sometype Mono speaks.
- **Emitters-only glow** — phosphor glow only on things that emit in the
  fiction (Agent Core, timer digits, hot commit keys, backlit placards).
- **Gangway / bulkhead** — persistent area navigation as a collapsible track
  or leading drawer, never amber.

These are design metaphors, not product terms or required microcopy.

## Approved shared decisions

| ID | Decision | Status | Rationale |
| --- | --- | --- | --- |
| `DS-DEC-1` | Use dark-first Shipboard Terminal as the identity-defining theme while maintaining an accessible light operational theme that preserves the same semantic hierarchy. | Supersedes v0.1 Deep-Space identity; light theme retained for accessibility | Prototype is dark-only; WCAG and approved P0 specs require an operable light theme (`PC-12`). |
| `DS-DEC-2` | Use phosphor teal for interaction, current context, selection, focus, wait, sealed/ready, and genuine live Agent state; reserve signal amber for attention and commitment; keep semantic success and danger for outcomes. | Supersedes v0.1 electric-blue/cyan split | Matches Shipboard two-voice color grammar without dropping repo outcome semantics. |
| `DS-DEC-3` | Prefer coherent work planes, hairline dividers, plates, and inspectors over floating card stacks. | Retained | Supports dense operational work. |
| `DS-DEC-4` | Represent an active Agent with the abstract, stateful Agent Core rather than a generic chatbot avatar. | Retained | Honest identity; no human likeness. |
| `DS-DEC-5` | Use two density modes: interaction and workspace. | Retained | Protects sustained reading while keeping administrative surfaces efficient. |
| `DS-DEC-6` | Treat WCAG 2.2 AA and the approved journey-level accessibility contract as the baseline for every shared pattern. | Retained | Accessibility is not optional styling (`PC-12`). |
| `DS-DEC-7` | Keep protected-content and authorization state explicit and non-disclosing across loading, denial, revocation, and recovery. | Retained | Visual polish must not weaken security or privacy. |
| `DS-DEC-8` | Use smoked-glass as the shared work-plane language; concentrate Agent presence in the Agent Core and examiner/instrument plate. Do not place transcript, form, table, Evidence, or review content beneath blur, reflection, or animated fields. | Supersedes v0.1 bounded Observation Glass as a separate universal motif | Prototype smoked glass is the plate language; reading planes stay opaque and high-contrast. |
| `DS-DEC-9` | Use zero authored border-radius with notched clip-path corners. Circular geometry is allowed only for node terminals, Agent Core, radio marks, scrollbar thumbs, and equivalent instrument dots. | Added in v1.0 | Shipboard geometry; 400% zoom and overflow must not clip focus or content. |
| `DS-DEC-10` | Use named Lucide imports for ordinary controls. Reserve custom drawn glyphs for brand, Agent Core, state nodes, wait instruments, and approved domain marks. | Added in v1.0 | Resolves prototype no-library rule against ADR-019 (`PC-13`). |
| `DS-DEC-11` | Use Michroma for placards and Sometype Mono for body, data, and controls, with approved system fallbacks until self-hosted files are pinned. | Added in v1.0 | Shipboard two-voice typography; `DS-PROP-2` covers delivery. |

## Structure

```text
design-system/
├── README.md
├── implementation-guide.md
├── change-record.md
├── foundation/
├── components/
└── product/
```

The module files are the design-system contract. The
[implementation guide](implementation-guide.md) is a reading manifest and
completion checklist; it is documentation, not a repository skill. Reusable
agent instructions remain under `.agents/skills/` and `.cursor/skills/`. The
[change record](change-record.md) is non-normative provenance.

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
- Prototype CSS custom properties (`--ground`, `--teal`, `--amber`) are source
  evidence. Production maps them onto the semantic roles in
  [colors](foundation/colors.md).

## Use and verification

Before designing, implementing, or reviewing UI:

1. Read this document and the governing feature and UI/UX specifications.
2. Use the [implementation guide](implementation-guide.md) to select every
   applicable foundation, component, and product module.
3. Map each UI state to the owning `REQ-*`/`AC-*` criteria and interaction
   decision before applying visual treatment.
4. Verify semantic structure, keyboard behavior, focus, announcements,
   contrast, 400 percent zoom/reflow, reduced motion, forced colors, and
   desktop/narrow states.
5. When a runnable UI exists, complete the repository Playwright MCP screenshot
   workflow and record evidence only in `.playwright-mcp/`.

## Restraint

Use teal emission, hairline traces, notches, and amber only when they
communicate hierarchy, current context, Agent activity, attention, or live
state.

Avoid generic purple-pink AI gradients, glowing every border or icon,
full-screen animated starfields, chat bubbles, pill badges, outer card-lift
shadows, retro-terminal cosplay, gaming-HUD clutter, and motion unrelated to
actual system state. Long-form conversation, forms, tables, Evidence, and
review content must remain calm and comfortable to read.

## Resolved questions and proposals

### `Q-DS-1` — Font delivery and dependency approval

**Status:** Resolved. `DS-PROP-1` (Geist Sans, Space Grotesk, IBM Plex Mono) is
superseded by `DS-PROP-2`.

**Approved decision `DS-PROP-2` (visual; package pin at implementation):** use
self-hosted, version-pinned `@fontsource/michroma` and
`@fontsource/sometype-mono` after verifying OFL-1.1 notices and approved
delivery artifacts against the repository toolchain. Never load them from a
third-party origin in an authenticated or Participant surface. Until those
implementation checks pass, use `"Michroma", "Arial Narrow", sans-serif` and
`"Sometype Mono", ui-monospace, monospace`. Exact npm versions are recorded
when the new `web/` package adds the dependencies.

No new design-system-level open questions remain for ordinary visual conflicts
with v0.1. Escalation-threshold conflicts still follow the rebuild task
authority model.

## Versioning and change control

Use semantic design-system versions. Material changes to token
meaning, state semantics, accessibility behavior, or product primitives require
review, an updated version, and downstream impact notes. Preserve superseded
decisions through version control; do not silently rewrite approved interaction
meaning.
