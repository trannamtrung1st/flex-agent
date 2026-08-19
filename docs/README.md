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
| Structure or review a backend feature module | [Backend module architecture](architecture/backend-module-architecture.md) |
| Write a new feature specification | [Feature spec template](templates/feature-spec.md) and [Feature specs](requirements/features/README.md) |
| Understand Cursor rules, roles, and QA expectations | [Development harness](contributing/development-harness.md) |
| Run the .NET/React workspace scaffold | [Workspace development](contributing/workspace.md) |

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
- Clearly labeled proposals (`Proposed`, open questions with interim defaults) inform discussion but do not govern behavior. A `Q-*` interim default is working guidance only until decided or promoted to an approved requirement/`PROP-*`.

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
| Product | Concept model v0.5; overview and MVP scope v0.4 approved | [Product hub](product/README.md): person-like persona and honest Agent identity approved without expanding text-only assessment scope |
| Requirements | Seven P0 specifications approved at current versions | [Feature catalog](requirements/README.md#feature-catalog-overview); resolved configuration v0.4 and text Session v0.5 add the P0 output profile without an eighth P0 feature; [MVP operational defaults](requirements/mvp-operational-defaults.md) remain approved |
| UI/UX | All five P0 surfaces approved; Text Session v0.5 current | The approved [Text Session](ui-ux/text-session.md) keeps envelope, output-id, audience, and timer internals hidden while presenting existing accessible message and no-action states; the journey and shared [design system](ui-ux/design-system/README.md) remain approved |
| Architecture | Approved v0.10 baseline and sixteen approved ADRs | [MVP architecture](architecture/mvp-architecture.md) v0.10, [ADR-012](architecture/decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md), [ADR-013](architecture/decisions/ADR-013-agent-requested-next-timer-replacement.md), and [ADR-014](architecture/decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md) govern Invocation/Decision, next-timer, and the P0 output envelope while preserving ADR-011 streaming. Approved [ADR-015](architecture/decisions/ADR-015-session-timer-lane-service-delegation.md) and [ADR-016](architecture/decisions/ADR-016-worker-workload-identity-and-invocation-delegation.md) govern Worker timer delegation, workload identity, and bounded Invocation delegation. The Worker reference identity and Invocation-delegation path is implemented and independently reviewed; Production/Staging enablement still requires approved deployment-profile evidence, a live issuer, and live-provider qualification remains a separate gate. |
| Feature specifications | Catalog complete; seven P0 specifications approved | 19 specs (7 P0, 2 P1, 5 P2, 5 P3); see [features/](requirements/features/README.md) |
| Implementation | Sessions runtime foundation and host successor slices completed; feature status remains Partial | Executable API, Worker, SPA, Sessions module, PostgreSQL Session runtime migrations `0005`–`0028`, synthetic Participant Text Session, production HTTP SSE with Session-scoped reauthorization, locked restores, architecture/runtime tests, OCI build inputs, and implementation CI are present. See [workspace development](contributing/workspace.md). The Worker reference path for [ADR-016](architecture/decisions/ADR-016-worker-workload-identity-and-invocation-delegation.md) workload identity and bounded Invocation delegation is implemented and independently reviewed; Production/Staging enablement, exact-profile live-provider qualification, OIDC application-session, hosted Session creation, backup/restore, and remaining ADR-010 gates are still open. Timer polling and Invocation processing remain default-off. |
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
- [Feature specifications](requirements/features/README.md) — 19 specs; all seven P0 specifications approved at their current versions
- [MVP operational defaults](requirements/mvp-operational-defaults.md) — approved intake, authentication-session, lifecycle, and recovery-placement defaults

### UI/UX

- [UI/UX documentation](ui-ux/README.md) — Approved platform Activity IA,
  end-to-end P0 assessment Campaign journey, five approved P0 surface
  specifications including Text Session v0.5, and the approved shared design
  system

### Architecture

- [Architecture documentation](architecture/README.md) — system boundaries, data, integration, runtime, and deployment
- [Approved MVP architecture](architecture/mvp-architecture.md) — end-to-end P0 boundaries, ownership, runtime flows, trust model, quality attributes, traceability, resilience, and evolution boundaries
- [Approved text Session runtime contract](architecture/session-runtime-contract.md) — ordering, timing, durable-before-display incremental Agent-response publication, SSE/reconnect, optional-broker boundary, terminalization, and recovery
- [Approved Evidence and Evaluation execution contract](architecture/evaluation-execution-contract.md) — Evidence locators/sealing, evaluator composition, completion, lineage, and reconstruction
- [Approved Human review, Result, and Release contract](architecture/review-result-release-contract.md) — exact candidate selection, revision, decision, Result construction, visibility, Release, notifications, and correction
- [ADR-007: OSS-first self-hostable deployment](architecture/decisions/ADR-007-oss-first-self-hostable-deployment.md) — portable on-premises reference deployment and optional cloud adapters
- [ADR-008: bounded OSS component set and provider/deployment defaults](architecture/decisions/ADR-008-bounded-oss-component-set.md) — approved reference products, optional adapters, model-provider profiles, external recovery responsibility, version policy, and evidence gates
- [ADR-009: MVP detailed contracts](architecture/decisions/ADR-009-mvp-session-evaluation-review-contracts.md) — approved Session, Evaluation, Review/Release, provider-streaming, broker, and notification boundaries
- [ADR-010: .NET implementation stack](architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md) — approved .NET/React runtime, schemas, persistence, Grate migrations, testing, workspace, and dependency policy
- [ADR-011: participant-visible Agent-response streaming](architecture/decisions/ADR-011-participant-visible-agent-response-streaming.md) — approved durable fragments, exact replay, incomplete-stream recovery, cutoff, validation, and backpressure
- [ADR-012: structured Agent Invocation and Decision](architecture/decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md) — approved provider-neutral Invocation/Decision boundary
- [ADR-013: Agent-requested next-timer replacement](architecture/decisions/ADR-013-agent-requested-next-timer-replacement.md) — approved optional one-lane next-timer replacement
- [ADR-014: Agent output envelope and P0 compatibility](architecture/decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md) — approved P0 Decision-output envelope and historical v1 reconstruction
- [ADR-015: Session timer-lane service delegation](architecture/decisions/ADR-015-session-timer-lane-service-delegation.md) — approved Worker timer-lane delegation realization of ADR-002
- [ADR-016: Worker workload identity and bounded Invocation delegation](architecture/decisions/ADR-016-worker-workload-identity-and-invocation-delegation.md) — approved portable Worker authentication, actor binding, and per-Session Invocation-execution delegation
- [Architecture decisions](architecture/decisions/README.md) — approved ADR catalog, status, and proposal template

### Contributor guidance

- [Development harness](contributing/development-harness.md) — Cursor rules, role skills, TDD policy, and Playwright MCP expectations
- [Provider deployment profiles](operations/provider-profiles/README.md) — non-secret Direct OpenAI profile example and fail-closed qualification notes

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
