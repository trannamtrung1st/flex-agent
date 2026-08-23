# Architecture documentation

Technical architecture, system boundaries, and integration design for Flex Agent.

Product concepts and invariants live under [product documentation](../product/README.md). Architecture explains how approved requirements are realized technically; canonical concept definitions remain in the [concept model](../product/concept-model.md).

## Status

**Approved MVP version 0.10 baseline, amended 2026-08-23 for effective timing,
Accommodation ownership, and parallel v2 Enrollment projections.** The
[MVP architecture](mvp-architecture.md) governs P0
boundaries, logical ownership, runtime flows, trust boundaries, SPA/API/gateway
topology, OIDC direction, resilience, quality attributes, OSS-first
self-hostability, and evolution seams.

Approved `AR-DEC-26`–`AR-DEC-27` keep effective timing and Accommodation state
in Participation and Submission, consume frozen policy facts from Assessment
Configuration and exact current Organization policy from Configuration owner
ports, and preserve strict v1 meaning while adding parallel
`/v2/assessment` Enrollment and **My work** projections.

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
does not block MVP or production architecture. Approved baselines of the
detailed contracts cover [text Session runtime](session-runtime-contract.md),
[Evidence and Evaluation execution](evaluation-execution-contract.md), and
[Human review, Result, and Release](review-result-release-contract.md). The three
contracts and their original provider-streaming, optional-broker, and
notification boundaries were approved through
[ADR-009](decisions/ADR-009-mvp-session-evaluation-review-contracts.md); the
Session publication boundary is revised by
[ADR-011](decisions/ADR-011-participant-visible-agent-response-streaming.md). The cross-cutting
[MVP operational defaults](../requirements/mvp-operational-defaults.md) resolve
upload, application-session, lifecycle, and recovery-placement policy. The
approved [Result and Release interaction specification](../ui-ux/result-release.md)
completes the P0 surface interaction set. The shared
[design system](../ui-ux/design-system/README.md) is approved. Session runtime
machine-readable schemas and fixtures exist. ADR-008 provider qualification
and remaining production evidence remain staged work as mapped in the
approved overview.

[ADR-010](decisions/ADR-010-dotnet-implementation-stack-and-workspace.md)
approves the .NET 10/ASP.NET Core API and worker, React/Vite SPA,
JSON Schema-first contracts validated with `JsonSchema.Net`, a separate
project containing pinned RFC-reference canonicalization source,
Npgsql/Dapper PostgreSQL access, Grate plain-SQL migrations, test stack,
workspace boundaries, and application dependency policy. Its schema, RFC 8785,
database, provider, artifact, session, and operability gates remain required
implementation evidence.

The 2026-08-19 and 2026-08-20 approved amendments to
[ADR-008](decisions/ADR-008-bounded-oss-component-set.md) and
[ADR-010](decisions/ADR-010-dotnet-implementation-stack-and-workspace.md)
authorize the vendor-neutral OpenAI-compatible endpoint target and a distinct
[OpenRouter synthetic-development profile](../operations/provider-profiles/openrouter-synthetic-development.md).
It permits real external calls and natural local text chat with synthetic,
non-sensitive content while keeping random free routing out of frozen Sessions,
production qualification, and the separate OpenAI-compatible endpoint evidence
track. OpenAI-hosted service is merely one potentially qualified compatible
endpoint; Organization-hosted and on-premises runtimes are first-class targets
subject to exact-profile and private-destination gates. The deterministic
adapter migration may complete without an exact live profile, but that does
not qualify or enable the adapter; live qualification remains a separate
successor gate before real use.

The [backend module architecture](backend-module-architecture.md) is approved
as the implementation guide for applying ADR-006 and ADR-010 consistently. It
defines the domain-oriented modular-monolith identity, ports-and-adapters
boundaries, project-splitting policy, module ownership rules, and required
verification for future backend work.

[ADR-011](decisions/ADR-011-participant-visible-agent-response-streaming.md)
supersedes ADR-009's complete-message-only Session publication boundary and
approves participant-visible durable-before-display incremental Agent-response
streaming for the MVP. The primary store, transactional outbox, SSE replay,
cutoff, validation, and backpressure contract remain authoritative; no external
broker becomes mandatory.

[ADR-012](decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md)
defines the approved provider-neutral Agent Invocation/Decision,
validation/effect, and explicit no-action boundary. The related product,
resolved-configuration, Session, UI/UX, and architecture revisions are approved
and govern implementation.

[ADR-013](decisions/ADR-013-agent-requested-next-timer-replacement.md)
extends that boundary with one optional runtime-owned Session timer lane. An
Agent Decision may recommend a bounded replacement delay, but the runtime
validates, schedules, fires, pauses, and cancels the event authoritatively.

[ADR-014](decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md)
specializes the Decision as a versioned output/action envelope with a P0
message-only compatibility profile. Historical v1 Decisions remain
reconstructable; voice and additional actions remain disabled.

Approved [ADR-015](decisions/ADR-015-session-timer-lane-service-delegation.md)
records the Worker timer-lane service-delegation realization of ADR-002.
Approved
[ADR-016](decisions/ADR-016-worker-workload-identity-and-invocation-delegation.md)
defines the portable Worker workload-authentication contract, reference OAuth
2.0 client-credentials signed-JWT profile, durable service-actor binding, and
bounded Invocation-execution delegation. The Worker reference-path
implementation of that ADR is present and independently reviewed; Production
and Staging enablement still require approved deployment-profile evidence, a
live issuer, and live-provider qualification.

## Purpose

This area governs how the system is structured: boundaries, data ownership, runtime flows, deployment topology, quality attributes, and integration contracts.

## MVP architecture route

| Need | Start here |
| --- | --- |
| Review the approved end-to-end P0 technical shape | [MVP architecture](mvp-architecture.md) |
| Review approved cross-cutting decisions | [Architecture decisions](decisions/README.md) |
| Review participant-visible Agent-response streaming | [ADR-011: participant-visible Agent-response streaming](decisions/ADR-011-participant-visible-agent-response-streaming.md) |
| Review the approved structured Agent boundary | [ADR-012: structured Agent Invocation and Decision](decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md) |
| Review the approved next-timer replacement boundary | [ADR-013: Agent-requested next-timer replacement](decisions/ADR-013-agent-requested-next-timer-replacement.md) |
| Review the approved P0 Decision-output envelope | [ADR-014: Agent output envelope and P0 compatibility](decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md) |
| Review the approved timer-lane service delegation | [ADR-015: Session timer-lane service delegation](decisions/ADR-015-session-timer-lane-service-delegation.md) |
| Review the approved Worker identity and Invocation delegation | [ADR-016: Worker workload identity and bounded Invocation delegation](decisions/ADR-016-worker-workload-identity-and-invocation-delegation.md) |
| Review the approved implementation stack and workspace | [ADR-010: .NET implementation stack](decisions/ADR-010-dotnet-implementation-stack-and-workspace.md) |
| Structure or review a backend module | [Backend module architecture](backend-module-architecture.md) |
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
- [Backend module architecture](backend-module-architecture.md)
- [Architecture decisions](decisions/README.md)
