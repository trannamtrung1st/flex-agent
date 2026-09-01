# Architecture documentation

Technical architecture, system boundaries, and integration design for Flex
Agent.

Product concepts and invariants live under [product documentation](../product/README.md).
Architecture explains how requirements are realized technically; canonical
concept definitions remain in the [concept model](../product/concept-model.md).

## Status

**Approved.** Current architecture documents and focused runtime contracts
own still-valid architecture and code-contract constraints extracted from
ADR-001 through ADR-021. Historical ADR files are recoverable from Git and
are **not** the current architecture catalog.

Current owners:

| Concern | Current document |
| --- | --- |
| MVP system shape, isolation, activation atomicity, deployment baseline, Worker lanes | [MVP architecture](mvp-architecture.md) |
| Authorization kernel, audit, Enrollment admission | [Backend module architecture](backend-module-architecture.md) and MVP architecture |
| Resolved configuration, Attempt start, streaming, Invocation/Decision, timers, envelope | [Text Session runtime contract](session-runtime-contract.md) |
| Evidence and Evaluation realization | [Evidence and Evaluation execution contract](evaluation-execution-contract.md) |
| Review, Result, and Release realization | [Human review, Result, and Release contract](review-result-release-contract.md) |
| SPA Query/form/icon/transport and single-SPA / design-lab isolation | [Frontend architecture](frontend-architecture.md) |
| OSS component and provider-profile defaults | [Operations](../operations/README.md) and [provider profiles](../operations/provider-profiles/README.md) |
| Workspace, toolchain, and verification gates | [Workspace development](../contributing/workspace.md) and [MVP architecture](mvp-architecture.md) |

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
| Review participant-visible Agent-response streaming | [Text Session runtime contract](session-runtime-contract.md) |
| Review the structured Agent boundary | [Text Session runtime contract](session-runtime-contract.md) |
| Review the next-timer replacement boundary | [Text Session runtime contract](session-runtime-contract.md) |
| Review the P0 Decision-output envelope | [Text Session runtime contract](session-runtime-contract.md) |
| Review timer-lane service delegation | [MVP architecture](mvp-architecture.md) |
| Review Worker identity and Invocation delegation | [MVP architecture](mvp-architecture.md) |
| Review Enrollment request-limit ownership and realization | [Backend module architecture](backend-module-architecture.md) |
| Review the implementation stack and workspace | [Workspace development](../contributing/workspace.md) |
| Review frontend state, form, icon, and transport ownership | [Frontend architecture](frontend-architecture.md) |
| Review single-SPA topology and design-lab isolation | [Frontend architecture](frontend-architecture.md) |
| Structure or review SPA Query/form layering | [Frontend architecture](frontend-architecture.md) |
| Structure or review a backend module | [Backend module architecture](backend-module-architecture.md) |
| Review OSS component defaults and pending evidence gates | [Operations](../operations/README.md) |
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

Architecture documents explain how requirements are realized. Current
architecture and code-contract documents are the live owners. Historical ADR
text is recoverable from Git.

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
