# Flex Agent Documentation

Authoritative product and engineering documentation for the Flex Agent platform.

The repository root [`README.md`](../README.md) is the GitHub landing page: current documentation phase, entry links, and the validation command.

## Audience routes

| If you need to… | Start here |
| --- | --- |
| Understand product vision and positioning | [Product overview](product/overview.md) |
| Define or review domain concepts, relationships, and invariants | [Concept model](product/concept-model.md) |
| Understand MVP boundaries and non-goals | [MVP scope](product/mvp-scope.md) |
| Find or author feature specifications | [Feature catalog](requirements/README.md#feature-catalog-overview) and [P0 authoring order](requirements/README.md#p0-authoring-order) |
| Design journeys, interaction states, or design-system guidance | [UI/UX](ui-ux/README.md) |
| Review system boundaries, data flows, or technical decisions | [Architecture](architecture/README.md) |
| Write a new feature specification | [Feature spec template](templates/feature-spec.md) and [Feature specs](requirements/features/README.md) |
| Understand Cursor rules, roles, and QA expectations | [Development harness](contributing/development-harness.md) |

## Authority by concern

Documents govern different concerns. A document overrides another **only within its area of authority**. Cross-area conflicts require review in the governing area — an ADR must not silently redefine product meaning or scope, and a feature spec must not silently override an approved product decision without explicit supersession.

| Concern | Authoritative source | Governs |
| --- | --- | --- |
| Product meaning and scope | Approved [concept model](product/concept-model.md), [MVP scope](product/mvp-scope.md), or product decision record | Domain vocabulary, relationships, scope boundaries, product invariants |
| Observable system behavior | Approved feature specification with stable `REQ-*` and `AC-*` IDs | What the system must do, who may do it, and how success is verified |
| User interaction | Approved UI/UX specification | Journeys, interaction states, content, accessibility, and visual design |
| Technical realization | Approved architecture decision (ADR) | Implementation approach, data ownership, deployment, and technical trade-offs |
| Implemented behavior | Code and tests | Must trace back to the sources above |

**Conflict resolution rules:**

- A conflicting ADR triggers product or requirements review; it does not override product semantics.
- A feature spec that changes domain meaning requires an updated or superseding product document.
- Implementation that diverges from approved specs is a defect unless explicitly superseded.
- Clearly labeled proposals (`Proposed`, open questions) inform discussion but do not govern behavior.

Illustrative examples in product documents are not MVP commitments unless captured in an approved spec.

## Document status

| Status | Meaning |
| --- | --- |
| `Draft` | Work in progress; not authoritative for implementation or downstream authoring |
| `In review` | Under review; not yet approved |
| `Approved` | Authoritative for the governed concern |
| `Implemented` | Approved and reflected in the product |
| `Proposed` | Suggested default or option requiring explicit approval |
| `Superseded` | Replaced by a newer document or version; retained for history |

## Current maturity

| Area | Status | Notes |
| --- | --- | --- |
| Product | Approved v0.1 | [Product hub](product/README.md): overview, [concept model](product/concept-model.md), [MVP scope](product/mvp-scope.md) |
| Requirements | P0 specification review in progress | [Feature catalog](requirements/README.md#feature-catalog-overview); 19 placeholders, no approved specs yet |
| UI/UX | Scaffold | Placeholder; journeys, interaction specs, and design system deferred |
| Architecture | Scaffold | Placeholder; technical design and ADRs deferred |
| Feature specifications | Placeholder catalog complete | 19 spec scaffolds (7 P0, 2 P1, 5 P2, 5 P3) — see [features/](requirements/features/README.md) |
| Contributing | Active | Cursor rules, role skills, TDD policy, and Playwright MCP guidance |
| Templates | Active | Reusable authoring templates (not authoritative content) |

## Documentation areas

### Product

- [Product documentation hub](product/README.md) — boundaries between product meaning, requirements, UI/UX, and architecture
- [Product overview](product/overview.md) — vision, positioning, and principles
- [Concept model](product/concept-model.md) — canonical definitions, relationships, lifecycles, and invariants
- [MVP scope](product/mvp-scope.md) — first product experience, non-goals, and deferred capabilities

### Requirements

- [Requirements hub](requirements/README.md) — lifecycle, ID conventions, and [feature catalog](requirements/README.md#feature-catalog-overview)
- [Feature specifications](requirements/features/README.md) — 19 placeholder specs; author P0 first

### UI/UX

- [UI/UX documentation](ui-ux/README.md) — journeys, interaction specifications, accessibility, content, and design system (deferred)

### Architecture

- [Architecture documentation](architecture/README.md) — system boundaries, data, integration, runtime, and deployment (deferred)
- [Architecture decisions](architecture/decisions/README.md) — ADRs (deferred)

### Contributor guidance

- [Development harness](contributing/development-harness.md) — Cursor rules, role skills, TDD policy, and Playwright MCP expectations

### Templates

- [Feature spec template](templates/feature-spec.md) — standard structure for feature specifications

## Canonical vocabulary

Use terms consistently across all documents. Authoritative definitions: [Concept model](product/concept-model.md).

## Documentation boundary test

| Question | Document in… |
| --- | --- |
| Would this remain true if the UI and technology were replaced? | `docs/product/` |
| Does it state observable behavior the system must provide? | `docs/requirements/` |
| Does it define user-facing interaction, content, or visual design? | `docs/ui-ux/` |
| Does it explain technical realization, data ownership, or deployment? | `docs/architecture/` |

## Authoring guidelines

1. Read related documents before writing; reuse canonical vocabulary and stable IDs.
2. Choose the smallest appropriate document type and location.
3. Separate confirmed facts, normative requirements, decisions, proposals, assumptions, and deferred scope.
4. Link to one authoritative definition instead of copying content across documents.
5. Use `must` for approved requirements, `should` for recommendations, and `may` for permitted options.
6. Preserve history; do not silently rewrite approved intent.
7. Use `docs/templates/` for reusable authoring templates; keep authoritative content in area folders (`product/`, `requirements/`, `ui-ux/`, `architecture/`).
