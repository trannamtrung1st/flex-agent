# Flex Agent Documentation

Authoritative product and engineering documentation for the Flex Agent
platform.

The repository root [`README.md`](../README.md) is the landing page: entry
links and the documentation validation command.

## Audience routes

| If you need to… | Start here |
| --- | --- |
| Understand product vision and positioning | [Product overview](product/overview.md) |
| Define or review domain concepts, relationships, and invariants | [Concept model](product/concept-model.md) |
| Understand MVP boundaries and non-goals | [MVP scope](product/mvp-scope.md) |
| Find or author feature specifications | [Feature catalog](requirements/README.md#feature-catalog-overview) and [P0 authoring order](requirements/README.md#p0-authoring-order) |
| Design journeys, interaction states, or design-system guidance | [UI/UX](ui-ux/README.md) |
| Review system boundaries, data flows, or technical decisions | [Architecture](architecture/README.md) |
| Structure or review a backend feature module | [Backend module architecture](architecture/backend-module-architecture.md) |
| Structure or review SPA Query, form, and icon ownership | [Frontend architecture](architecture/frontend-architecture.md) |
| Write a new feature specification | [Feature spec template](templates/feature-spec.md) and [Feature specs](requirements/features/README.md) |
| Understand Cursor rules, roles, and QA expectations | [Development harness](contributing/development-harness.md) |
| Run the .NET/React workspace scaffold | [Workspace development](contributing/workspace.md) |
| Run the Keycloak/OIDC local and CI harness | [Keycloak OIDC contract](operations/provider-profiles/keycloak-oidc-contract.md) |

## Authority by concern

Documents govern different concerns. A document overrides another **only
within its area of authority**. Cross-area conflicts require review in the
governing area — an architecture document must not silently redefine product
meaning or scope, and a feature spec must not silently override a product
decision without explicit supersession.

| Concern | Authoritative source | Governs |
| --- | --- | --- |
| Product meaning and scope | [Concept model](product/concept-model.md), [MVP scope](product/mvp-scope.md), or product decision record | Domain vocabulary, relationships, scope boundaries, product invariants |
| Observable system behavior | Feature specification with stable `REQ-*` and `AC-*` IDs | What the system must do, who may do it, and how success is verified |
| User interaction | UI/UX specification | Journeys, interaction states, content, accessibility, and visual design |
| Technical realization | Architecture document | Implementation approach, data ownership, deployment, and technical trade-offs |
| Implemented behavior | Code and tests | Must trace back to the sources above |

**Conflict resolution rules:**

- A conflicting architecture decision triggers product or requirements
  review; it does not override product semantics.
- A feature spec that changes domain meaning requires an updated or
  superseding product document.
- Implementation that diverges from governing specs is a defect unless
  explicitly superseded.
- Clearly labeled proposals (`Proposed`, open questions with interim
  defaults) inform discussion but do not govern behavior. A `Q-*` interim
  default is working guidance only until decided or promoted to an approved
  requirement/`PROP-*`.

Illustrative examples in product documents are not MVP commitments unless
captured in a feature specification.

Current repository governance, catalogs, and validators remain binding until
Phase 4 cutover. Phase 3 replacement sources stay In review.

## Document status

| Status | Meaning |
| --- | --- |
| `Draft` | Work in progress; not authoritative for implementation or downstream authoring |
| `In review` | Under review; not yet the cutover authority |
| `Approved` | Authoritative for the governed concern |
| `Implemented` | Approved and reflected in the product |
| `Proposed` | Suggested default or option requiring explicit approval |
| `Superseded` | Replaced by a newer document or version; retained for history |

## Documentation areas

### Product

- [Product documentation hub](product/README.md) — boundaries between product meaning, requirements, UI/UX, and architecture
- [Product overview](product/overview.md) — vision, positioning, and principles
- [Concept model](product/concept-model.md) — canonical definitions, relationships, lifecycles, and invariants
- [MVP scope](product/mvp-scope.md) — first product experience, non-goals, and deferred capabilities

### Requirements

- [Requirements hub](requirements/README.md) — lifecycle, ID conventions, and [feature catalog](requirements/README.md#feature-catalog-overview)
- [Feature specifications](requirements/features/README.md) — catalog of P0–P3 specification files
- [MVP operational defaults](requirements/mvp-operational-defaults.md) — intake, authentication-session, lifecycle, and recovery-placement defaults

### UI/UX

- [UI/UX documentation](ui-ux/README.md) — application UX architecture, P0 journeys, and the shared design system

### Architecture

- [Architecture documentation](architecture/README.md) — system boundaries, data, integration, runtime, and deployment
- [MVP architecture](architecture/mvp-architecture.md) — end-to-end P0 boundaries, ownership, runtime flows, trust model, and quality attributes
- [Text Session runtime contract](architecture/session-runtime-contract.md)
- [Evidence and Evaluation execution contract](architecture/evaluation-execution-contract.md)
- [Human review, Result, and Release contract](architecture/review-result-release-contract.md)
- [Frontend architecture](architecture/frontend-architecture.md)
- [Architecture decisions](architecture/decisions/README.md) — current ADR catalog (binding until Phase 4 cutover)

### Contributor guidance

- [Development harness](contributing/development-harness.md) — Cursor rules, role skills, TDD policy, and Playwright MCP expectations
- [Provider deployment profiles](operations/provider-profiles/README.md) — provider profiles, Keycloak OIDC contract, and qualification boundaries

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
3. Separate confirmed facts, normative requirements, decisions, proposals, assumptions, and deferred scope. Every open question must include an **interim default** with brief rationale; that interim default does not govern behavior until decided.
4. Link to one authoritative definition instead of copying content across documents.
5. Use `must` for approved requirements, `should` for recommendations, and `may` for permitted options.
6. Preserve history; do not silently rewrite approved intent.
7. Use `docs/templates/` for reusable authoring templates; keep authoritative content in area folders (`product/`, `requirements/`, `ui-ux/`, `architecture/`).
