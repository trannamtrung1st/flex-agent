# Architecture documentation

Technical architecture, system boundaries, and integration design for Flex
Agent.

Product concepts and invariants live under [product documentation](../product/README.md).
Architecture explains how requirements are realized technically; canonical
concept definitions remain in the [concept model](../product/concept-model.md).

## Status

**In review.** Current architecture documents and focused runtime contracts
own still-valid architecture and code-contract constraints extracted from
ADR-001 through ADR-021. ADR files and the [ADR catalog](decisions/README.md)
remain present and still-binding until Phase 4 cutover. This Phase 3 rewrite
is recoverable beside the previous Git version and is **not** the Phase 4
authority cutover.

Current owners:

| Concern | Current document |
| --- | --- |
| MVP system shape, isolation, activation atomicity, deployment baseline, Worker lanes | [MVP architecture](mvp-architecture.md) |
| Authorization kernel, audit, Enrollment admission | [Backend module architecture](backend-module-architecture.md) and MVP architecture |
| Resolved configuration, Attempt start, streaming, Invocation/Decision, timers, envelope | [Text Session runtime contract](session-runtime-contract.md) |
| Evidence and Evaluation realization | [Evidence and Evaluation execution contract](evaluation-execution-contract.md) |
| Review, Result, and Release realization | [Human review, Result, and Release contract](review-result-release-contract.md) |
| SPA Query/form/icon/transport and single-SPA / design-lab isolation | [Frontend architecture](frontend-architecture.md) |
| OSS component and provider-profile defaults | [ADR catalog](decisions/README.md) until Phase 3 operations applies operations-owned rows |
| Workspace, toolchain, and verification gates | ADR catalog until Phase 4 contribution/verification rows |

ADR-020 dual-build `web-legacy` topology is historical only. Do not restore
it. Design-lab isolation is restated by ADR-021 constraints in frontend
architecture.

## Purpose

This area governs how the system is structured: boundaries, data ownership,
runtime flows, deployment topology, quality attributes, and integration
contracts.

## MVP architecture route

| Need | Start here |
| --- | --- |
| Review the end-to-end P0 technical shape | [MVP architecture](mvp-architecture.md) |
| Review ADR catalog (binding until Phase 4) | [Architecture decisions](decisions/README.md) |
| Review participant-visible Agent-response streaming | [ADR-011: participant-visible Agent-response streaming](decisions/ADR-011-participant-visible-agent-response-streaming.md) |
| Review the structured Agent boundary | [ADR-012: structured Agent Invocation and Decision](decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md) |
| Review the next-timer replacement boundary | [ADR-013: Agent-requested next-timer replacement](decisions/ADR-013-agent-requested-next-timer-replacement.md) |
| Review the P0 Decision-output envelope | [ADR-014: Agent output envelope and P0 compatibility](decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md) |
| Review timer-lane service delegation | [ADR-015: Session timer-lane service delegation](decisions/ADR-015-session-timer-lane-service-delegation.md) |
| Review Worker identity and Invocation delegation | [ADR-016: Worker workload identity and bounded Invocation delegation](decisions/ADR-016-worker-workload-identity-and-invocation-delegation.md) |
| Review Enrollment request-limit ownership and realization | [ADR-018: Enrollment request-limit scope](decisions/ADR-018-enrollment-request-limit-scope.md) |
| Review the implementation stack and workspace | [ADR-010: .NET implementation stack](decisions/ADR-010-dotnet-implementation-stack-and-workspace.md) |
| Review frontend state, form, icon, and transport ownership | [ADR-019: frontend state and library boundaries](decisions/ADR-019-frontend-state-and-library-boundaries.md) |
| Review frontend rebuild directories, design-lab isolation, and cutover | [ADR-020: frontend rebuild transition](decisions/ADR-020-frontend-rebuild-transition-and-design-lab-isolation.md) (superseded for production pointer) |
| Review single-SPA reset topology and fail-closed publication | [ADR-021: production frontend reset](decisions/ADR-021-production-frontend-reset-and-single-spa-topology.md) |
| Structure or review SPA Query/form layering | [Frontend architecture](frontend-architecture.md) |
| Structure or review a backend module | [Backend module architecture](backend-module-architecture.md) |
| Review OSS component defaults and pending evidence gates | [ADR-008: bounded OSS component set](decisions/ADR-008-bounded-oss-component-set.md) |
| Implement the text Session realization | [Text Session runtime contract](session-runtime-contract.md) |
| Implement the Evidence/Evaluation realization | [Evidence and Evaluation execution contract](evaluation-execution-contract.md) |
| Implement the review-to-Release realization | [Human review, Result, and Release contract](review-result-release-contract.md) |
| Review intake, session, lifecycle, and recovery defaults | [MVP operational defaults](../requirements/mvp-operational-defaults.md) |
| Find remaining architecture and delivery work | [MVP architecture remaining work](mvp-architecture.md#remaining-architecture-and-delivery-work) |
| Find unresolved questions and interim defaults | [MVP architecture open questions](mvp-architecture.md#open-architecture-questions) |
| Map P0 specifications to architecture surfaces | [P0 traceability map](mvp-architecture.md#p0-traceability-map) |

## Entry criteria

Begin architecture documentation when:

- At least one P0 feature specification is in `Draft`, `In review`, or `Approved` status
- Cross-cutting quality attributes (isolation, audit reconstructability, explainability, security) are stated in requirements
- [Concept model — Product invariants](../product/concept-model.md#product-invariants) are understood
- Material technical options require an explicit decision record

## Expected document types

| Type | Description |
| --- | --- |
| Architecture overview | System context, major components, and trust boundaries |
| Data design | Entities, ownership, isolation, retention, and event model |
| Integration design | External services, APIs, queues, and provider contracts |
| Runtime flow | Request/session lifecycle, real-time voice path, tool orchestration |
| Deployment view | Environments, topology, secrets, and operational concerns |
| Quality attributes | Performance, reliability, security, and observability targets |

## Relationship to requirements and decisions

Architecture documents explain how requirements are realized. Until Phase 4
cutover and Phase 5 ADR removal, irreversible or cross-cutting choices also
remain inspectable as [architecture decisions](decisions/README.md). Current
architecture and code-contract documents are the live owners for
architecture-owned and code-contract extraction rows.

## Core invariants

Architecture must preserve [Concept model — Product invariants](../product/concept-model.md#product-invariants),
including Organization, activity, participant, and session isolation;
authorization; audit reconstructability; atomic activation and Attempt start;
streaming publication rules; Worker identity and delegation; and frontend
isolation.

## Related documents

- [Documentation home](../README.md)
- [Product documentation](../product/README.md)
- [Concept model](../product/concept-model.md)
- [Requirements](../requirements/README.md)
- [MVP architecture](mvp-architecture.md)
- [Frontend architecture](frontend-architecture.md)
- [Backend module architecture](backend-module-architecture.md)
- [Architecture decisions](decisions/README.md)
