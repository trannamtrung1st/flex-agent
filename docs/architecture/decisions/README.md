# Architecture decision records

Architecture Decision Records (ADRs) for Flex Agent.

## Status

**Sixteen approved decisions.** ADR-012 through ADR-014 plus their approved upstream
product/requirement revisions govern structured Agent Invocation/Decision,
next-timer replacement, and the P0 Decision-output envelope.
[ADR-015](ADR-015-session-timer-lane-service-delegation.md) records the Worker
timer-lane service-delegation realization of ADR-002.
[ADR-016](ADR-016-worker-workload-identity-and-invocation-delegation.md)
defines portable Worker workload authentication and bounded Invocation
delegation. Both Worker authorization decisions are approved; their runtime,
deployment-profile, and verification gates remain incomplete.

| ADR | Status | Decision |
| --- | --- | --- |
| [ADR-001](ADR-001-resolved-configuration-representation-and-integrity.md) | Approved | Versioned canonical representation, digest and terminal-seal procedure, logical artifact split, and source-materialization boundary |
| [ADR-002](ADR-002-authorization-enforcement-and-delegation.md) | Approved | Shared authorization decision contract, enforcement adapters, service delegation, and freshness boundaries |
| [ADR-003](ADR-003-authorization-audit-persistence.md) | Approved | Authorization audit ownership, mutation coupling, durability classes, append-only history, and MVP persistence boundaries |
| [ADR-004](ADR-004-assessment-activation-baseline-and-atomicity.md) | Approved | Assessment activation-baseline representation, content digest, trusted binding, idempotency, and atomic audited activation |
| [ADR-005](ADR-005-atomic-attempt-start-and-submission-binding.md) | Approved | Atomic Attempt activation, exact Submission-version binding, Session readiness, entitlement consumption, and required audit acceptance |
| [ADR-006](ADR-006-mvp-architecture-baseline-and-evolution.md) | Approved | MVP application/deployment baseline, SPA/API/gateway, OIDC, persistence/work, recovery, optional caching, and deferred Kubernetes evolution boundaries |
| [ADR-007](ADR-007-oss-first-self-hostable-deployment.md) | Approved | OSS-first self-hostable reference deployment, open integration contracts, OCI packaging, agent-friendly automation, and optional cloud adapters |
| [ADR-008](ADR-008-bounded-oss-component-set.md) | Approved | Bounded OSS components, model-neutral provider profiles, scoped BYOK and Organization model endpoints, development-only operator-pulled LGTM, synthetic-evaluation versus production-pilot gates, external recovery responsibility, version policy, evidence gates, and Docker Compose reference orchestration |
| [ADR-009](ADR-009-mvp-session-evaluation-review-contracts.md) | Approved; Session publication partially superseded by ADR-011 | Detailed Session, Evidence/Evaluation, and Review/Result/Release contracts, including original provider-streaming, optional-broker, and notification boundaries |
| [ADR-010](ADR-010-dotnet-implementation-stack-and-workspace.md) | Approved | .NET 10/ASP.NET Core API and worker, React/Vite SPA, `JsonSchema.Net`, separate pinned-source JCS project, Npgsql/Dapper persistence, Grate migrations, test stack, workspace boundaries, and supply-chain conventions |
| [ADR-011](ADR-011-participant-visible-agent-response-streaming.md) | Approved | Durable-before-display participant-visible incremental Agent-response fragments, ordering, replay, incomplete-stream recovery, cutoff, validation, and backpressure |
| [ADR-012](ADR-012-structured-agent-invocation-and-decision-boundary.md) | Approved | Provider-neutral trusted Agent Invocation, structured Agent Decision, independent validation/effect, explicit no-action, ordering, streaming coexistence, and minimized provenance boundary |
| [ADR-013](ADR-013-agent-requested-next-timer-replacement.md) | Approved | Optional Agent recommendation that replaces one runtime-owned next timer under frozen delay, lifecycle, idempotency, and loop bounds |
| [ADR-014](ADR-014-agent-output-envelope-and-p0-compatibility.md) | Approved | Versioned Decision envelope with typed outputs and requested actions, P0 message-only compatibility profile, runtime-owned output identity, and historical v1 reconstruction |
| [ADR-015](ADR-015-session-timer-lane-service-delegation.md) | Approved | Per-Session `session.timer_lane.fire` service delegation, timer-schedule envelope reference, explicit Worker timer-polling capability, and commit-time reauthorization |
| [ADR-016](ADR-016-worker-workload-identity-and-invocation-delegation.md) | Approved | Provider-neutral Worker workload identity, OAuth 2.0 client-credentials signed-JWT reference profile, versioned service-actor binding, and per-Session `session.invocation.execute` delegation |

## Purpose

ADRs capture significant technical choices: context, drivers, options considered, decision, consequences, status, and supersession. They prevent silent drift and preserve rationale for future reviewers.

## When to write an ADR

Create an ADR when a decision:

- Is difficult or expensive to reverse
- Affects multiple components or teams
- Establishes a pattern others must follow
- Resolves a material open question from requirements or architecture review (each open question should already carry an **interim default** with brief rationale)

## Naming convention

```text
docs/architecture/decisions/ADR-<nnn>-<short-title>.md
```

Example: `ADR-001-session-event-store.md`

## ADR template (proposed)

```markdown
# ADR-<nnn>: <Title>

## Status

Proposed | Approved | Deprecated | Superseded by ADR-<mmm>

## Context

What forces or constraints led to this decision?

## Decision drivers

- ...

## Options considered

| Option | Pros | Cons |
| --- | --- | --- |
| ... | ... | ... |

## Decision

What was chosen and why?

## Consequences

Positive, negative, and neutral outcomes.

## Related

- Requirements: ...
- Supersedes: ...
```

## Supersession

When a decision is replaced, set the old ADR status to `Superseded by ADR-<mmm>` and link forward. Do not delete historical ADRs.

## Related documents

- [Architecture documentation](../README.md)
- [Documentation home](../../README.md)
- [Requirements](../../requirements/README.md)
