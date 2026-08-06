# Architecture documentation

Technical architecture, system boundaries, and integration design for Flex Agent.

Product concepts and invariants live under [product documentation](../product/README.md). Architecture explains how approved requirements are realized technically; canonical concept definitions remain in the [concept model](../product/concept-model.md).

## Status

**Active.** [ADR-001](decisions/ADR-001-resolved-configuration-representation-and-integrity.md) governs resolved-configuration representation and integrity. [ADR-002](decisions/ADR-002-authorization-enforcement-and-delegation.md) governs authorization enforcement and delegated execution. [ADR-003](decisions/ADR-003-authorization-audit-persistence.md) governs authorization audit ownership and MVP persistence. [ADR-004](decisions/ADR-004-assessment-activation-baseline-and-atomicity.md) governs assessment activation-baseline representation and atomic activation. [ADR-005](decisions/ADR-005-atomic-attempt-start-and-submission-binding.md) governs atomic Attempt activation, exact Submission binding, Session readiness, and entitlement consumption. Authentication, product-specific storage beyond the approved MVP consistency boundaries, and remaining topology choices stay deferred.

## Purpose

This area will govern how the system is structured: boundaries, data ownership, runtime flows, deployment topology, quality attributes, and integration contracts.

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
- [Architecture decisions](decisions/README.md)
