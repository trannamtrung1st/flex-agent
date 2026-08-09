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
affected profile is certified. The self-hostable architecture is approved and
model-neutral: certification applies to a concrete provider deployment profile,
not a preferred model. Deployment-managed profiles and Organization BYOK are
the MVP credential modes; the same boundary preserves a separately gated path
to Organization model endpoints without making that an MVP requirement.
Optional LGTM is operator-pulled local/CI infrastructure and
does not block MVP or production architecture. Approved detailed contracts cover
[text Session runtime](session-runtime-contract.md),
[Evidence and Evaluation execution](evaluation-execution-contract.md), and
[Human review, Result, and Release](review-result-release-contract.md). The three
contracts and their original provider-streaming, optional-broker, and
notification boundaries were approved through
[ADR-009](decisions/ADR-009-mvp-session-evaluation-review-contracts.md); the
Session publication boundary is revised by
[ADR-011](decisions/ADR-011-participant-visible-agent-response-streaming.md). The cross-cutting
[MVP operational defaults](../requirements/mvp-operational-defaults.md) resolve
upload, application-session, lifecycle, and recovery-placement policy. The
remaining Result/Release interaction contract and shared UI design-system
guidance, ADR-008 component compatibility evidence, machine-readable contract
schemas/fixtures, and production evidence
remain staged work as mapped in the approved overview.

[ADR-010](decisions/ADR-010-dotnet-implementation-stack-and-workspace.md)
approves the .NET 10/ASP.NET Core API and worker, React/Vite SPA,
JSON Schema-first contracts validated with `JsonSchema.Net`, a separate
project containing pinned RFC-reference canonicalization source,
Npgsql/Dapper PostgreSQL access, Grate plain-SQL migrations, test stack,
workspace boundaries, and application dependency policy. Its schema, RFC 8785,
database, provider, artifact, session, and operability gates remain required
implementation evidence.

[ADR-011](decisions/ADR-011-participant-visible-agent-response-streaming.md)
supersedes ADR-009's complete-message-only Session publication boundary and
approves participant-visible durable-before-display incremental Agent-response
streaming for the MVP. The primary store, transactional outbox, SSE replay,
cutoff, validation, and backpressure contract remain authoritative; no external
broker becomes mandatory.

## Purpose

This area governs how the system is structured: boundaries, data ownership, runtime flows, deployment topology, quality attributes, and integration contracts.

## MVP architecture route

| Need | Start here |
| --- | --- |
| Review the approved end-to-end P0 technical shape | [MVP architecture](mvp-architecture.md) |
| Review approved cross-cutting decisions | [Architecture decisions](decisions/README.md) |
| Review participant-visible Agent-response streaming | [ADR-011: participant-visible Agent-response streaming](decisions/ADR-011-participant-visible-agent-response-streaming.md) |
| Review the approved implementation stack and workspace | [ADR-010: .NET implementation stack](decisions/ADR-010-dotnet-implementation-stack-and-workspace.md) |
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
