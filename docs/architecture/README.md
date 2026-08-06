# Architecture documentation

Technical architecture, system boundaries, and integration design for Flex Agent.

Product concepts and invariants live under [product documentation](../product/README.md). Architecture explains how approved requirements are realized technically; canonical concept definitions remain in the [concept model](../product/concept-model.md).

## Status

**Approved MVP baseline.** The [MVP architecture](mvp-architecture.md) governs P0
boundaries, logical ownership, runtime flows, trust boundaries, SPA/API/gateway
topology, OIDC direction, resilience, quality attributes, OSS-first
self-hostability, and evolution seams.

[ADR-001](decisions/ADR-001-resolved-configuration-representation-and-integrity.md)
through [ADR-007](decisions/ADR-007-oss-first-self-hostable-deployment.md) are
approved. [ADR-008](decisions/ADR-008-bounded-oss-component-set.md) approves the
bounded component set and version policy; its compatibility, security,
recovery, license, and supply-chain evidence gates remain required before an
affected profile is certified. The self-hostable architecture is approved, but
Mistral Small 3.1 24B Instruct is the approved first vLLM benchmark candidate,
while its exact artifact, quantization, and hardware profile remain uncertified
under `Q-OSS-1`. Optional LGTM is operator-pulled local/CI infrastructure and
does not block MVP or production architecture. Approved detailed contracts cover
[text Session runtime](session-runtime-contract.md),
[Evidence and Evaluation execution](evaluation-execution-contract.md), and
[Human review, Result, and Release](review-result-release-contract.md). The three
contracts and their provider-streaming, optional-broker, and notification
boundaries are approved through
[ADR-009](decisions/ADR-009-mvp-session-evaluation-review-contracts.md). The cross-cutting
[MVP operational defaults](../requirements/mvp-operational-defaults.md) resolve
upload, application-session, lifecycle, and recovery-placement policy. Detailed
UI/UX contracts, ADR-008 component compatibility evidence, machine-readable
contract schemas/fixtures, and production evidence
remain staged work as mapped in the approved overview.

## Purpose

This area will govern how the system is structured: boundaries, data ownership, runtime flows, deployment topology, quality attributes, and integration contracts.

## MVP architecture route

| Need | Start here |
| --- | --- |
| Review the approved end-to-end P0 technical shape | [MVP architecture](mvp-architecture.md) |
| Review approved cross-cutting decisions | [Architecture decisions](decisions/README.md) |
| Review approved OSS component defaults and pending evidence gates | [ADR-008: bounded OSS component set](decisions/ADR-008-bounded-oss-component-set.md) |
| Implement the approved text Session realization | [Text Session runtime contract](session-runtime-contract.md) |
| Implement the approved Evidence/Evaluation realization | [Evidence and Evaluation execution contract](evaluation-execution-contract.md) |
| Implement the approved review-to-Release realization | [Human review, Result, and Release contract](review-result-release-contract.md) |
| Review approved intake, session, lifecycle, and recovery defaults | [MVP operational defaults](../requirements/mvp-operational-defaults.md) |
| Find remaining architecture and delivery work | [MVP architecture remaining work](mvp-architecture.md#remaining-architecture-and-delivery-work) |
| Find unresolved questions and interim defaults | [MVP architecture open questions](mvp-architecture.md#open-architecture-questions) |
| Map P0 specifications to architecture surfaces | [P0 traceability map](mvp-architecture.md#p0-traceability-map) |

## Entry criteria

Begin architecture documentation when:

- At least one P0 feature specification is in `Draft` or `Approved` status
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

Architecture documents explain how approved requirements are realized. Irreversible or cross-cutting choices are recorded as [architecture decisions](decisions/README.md) (ADRs).

## Core invariants

Architecture must preserve [Concept model — Product invariants](../product/concept-model.md#product-invariants).

## Related documents

- [Documentation home](../README.md)
- [Product documentation](../product/README.md)
- [Concept model](../product/concept-model.md)
- [Requirements](../requirements/README.md)
- [Approved MVP architecture](mvp-architecture.md)
- [Architecture decisions](decisions/README.md)
