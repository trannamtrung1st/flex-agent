# ADR-006: MVP architecture baseline and evolution boundaries

## Status

Approved

## Owners and approvers

- Owner: Architecture Lead
- Approvers: Product Lead, Architecture Lead, Security/Privacy reviewer
- Approved date: 2026-08-06

## Context

The seven P0 specifications and ADR-001 through ADR-005 define the MVP's
observable behavior and several critical consistency, authorization, audit, and
integrity boundaries. Implementation also needs one approved system shape for
the browser client, public ingress, application runtimes, persistence,
asynchronous work, identity, artifacts, model/evaluator execution, Release,
recovery, optional caching, and future execution isolation.

Without one baseline, feature teams could select incompatible transaction,
identity, event, cache, recovery, or deployment patterns. Premature microservices,
Kubernetes, external brokers, or authoritative caches would add failure and
security boundaries before scale or approved features justify them. Conversely,
running long or external work inline in API requests or keeping authoritative
state on local worker filesystems would make later isolation and orchestration
needlessly difficult.

The approved [MVP architecture](../mvp-architecture.md) was reviewed as one
coherent baseline. This ADR formalizes its cross-cutting decisions while leaving
concrete vendor products and named detailed feature contracts to their owning
follow-up work.

## Decision drivers

- Preserve the approved Organization, Activity, Participant, and Session
  isolation and configuration/evidence/outcome invariants.
- Satisfy ADR-003 through ADR-005 with one strong MVP consistency boundary.
- Keep authentication standards-based and authorization application-owned.
- Support a responsive SPA without trusting browser state for workflow authority.
- Provide durable asynchronous work without requiring an external broker.
- Make failure, retry, reconciliation, recovery, and Release visibility explicit.
- Keep sensitive artifacts, model disclosure, deterministic execution, caches,
  and telemetry bounded and non-authoritative.
- Avoid Kubernetes and stronger Agent execution infrastructure until an approved
  feature requires workspace, code, long-task, resource, or isolation behavior.
- Preserve a simple evolution path through container-ready disposable workers
  and versioned durable work contracts.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Modular application with SPA, API, worker, relational primary, durable database work, and private artifacts | Strong transactions, simple operations, explicit boundaries, incremental scaling, and a clean future extraction path | Requires disciplined logical module ownership and database isolation tests |
| Independently deployed microservices, broker, distributed cache, and Kubernetes from the start | Independent scaling and infrastructure flexibility | Adds network, consistency, deployment, authorization-version, recovery, and operational failure modes without demonstrated MVP need |
| One synchronous web process with local files and inline external work | Lowest initial infrastructure count | Couples user requests to providers, loses durable recovery, weakens isolation, and obstructs future worker scheduling |
| Server-rendered client without a dedicated SPA | Simpler initial client state | Does not match the approved responsive interaction direction and reconnectable Session experience |
| Browser-held provider tokens with provider roles as application permissions | Direct OIDC integration | Expands token exposure and couples replaceable authentication claims to domain authorization |
| Adopt Redis, an external broker, and Kubernetes only after measured or feature evidence | Keeps MVP lean while preserving explicit extension points | Requires later migration work when evidence appears |

## Decision

### Approved architecture baseline

Adopt version 0.1 of the [MVP architecture](../mvp-architecture.md) as the
technical realization baseline. Its `AR-DEC-1` through `AR-DEC-16` are approved
and governed by this ADR unless a narrower approved ADR refines them without
conflict or a later ADR explicitly supersedes them.

### Application and ingress

- Use a modular monolith for domain behavior.
- Deliver a browser SPA backed by a stateless API runtime.
- Use one simple API gateway for TLS, routing, coarse limits, correlation,
  security headers, and SSE-compatible connection handling.
- Use request/response commands plus Server-Sent Events for reconnectable text
  Session/status updates, with bounded polling only as a deployment fallback.
- Keep authorization, workflow state, timing, ordering, acceptance, Evaluation,
  and Release authority in the API/domain boundary rather than the SPA or gateway.

### Persistence and asynchronous execution

- Use one primary relational transactional store for authoritative metadata,
  state machines, immutable records, audit/outbox, idempotency, ordering, and
  uniqueness.
- Use the transactional outbox plus a durable claimable work table for MVP
  background work. Workers use leases, bounded delegation, idempotency, expected
  versions, bounded retry, and reconciliation.
- Do not require an external broker for MVP. Add one only after measured evidence
  and an architecture update preserve the same authority and failure semantics.
- Workers are stateless, disposable, container-ready, and do not depend on local
  disk or process memory as the sole authoritative state.

### Identity and external boundaries

- Integrate external OpenID Connect identity providers through a provider-neutral
  adapter and bind internal actors by stable issuer/subject identity.
- Use server-validated identity and an API-server-managed application session;
  do not create a custom password store.
- OIDC authenticates. ADR-002's application authorization kernel owns
  Organization, relationship, assignment, resource, and workflow authorization.
- Keep Submission payloads in private protected artifact storage behind
  quarantine, validation, integrity, immutable versioning, and authorized
  delivery.
- Keep model access provider-neutral and worker-delegated. Run allowlisted
  deterministic evaluators in a restricted no-egress worker boundary by default.
- Preserve atomic Release, exact Result binding, participant visibility, and
  required audit/outbox acceptance in one primary-store transaction.

### Optional caching

Caching is optional and non-authoritative. Redis may be selected later after
measurement. Correctness, sensitive commits, idempotency, ordering, Release
visibility, and audit must work without it. Any future cache design must use
Organization/resource-scoped keys, version-aware invalidation, fail-safe
fallback, and negative isolation/failure tests.

### Resilience and recovery

Use the [approved resilience and recovery baseline](../mvp-architecture.md#resilience-and-recovery-baseline):

- single region and one authoritative write primary;
- at least two stateless API and two worker instances across available failure
  zones for production;
- ordinary instance recovery within five minutes without committed-state loss;
- a supported relational deployment with multi-zone synchronous standby and
  automatic failover where available, whether Organization-operated or managed;
- zero acknowledged-transaction data loss target for ordinary database-node or
  availability-zone failure and service restoration within 30 minutes;
- regional-disaster RPO no more than five minutes and RTO no more than four hours,
  contingent on policy-permitted recovery copies;
- protected artifact version/integrity recovery, point-in-time database recovery,
  immutable backups, restoration drills, and measured evidence before pilot.

If residency or lifecycle policy prohibits the recovery mechanism, approve and
publish the weaker achievable regional target rather than claiming the baseline.

### Deferred Kubernetes and isolated Agent execution

Do not introduce Kubernetes, per-Agent Pods, workspace cloning, participant-code
execution, long user-delegated Agent processing, a general scheduler framework,
or stronger sandbox/microVM infrastructure for the MVP.

Preserve the evolution seam through durable versioned work records,
container-ready disposable workers, protected input/output references, and no
Kubernetes objects in domain contracts. When an approved future feature requires
repository/code execution, specialized resources, long processing, or stronger
Agent isolation, define the workload envelope and security model in its feature
specification and a new ADR. A scheduler adapter may then dispatch the same
bounded work to Kubernetes Jobs or stronger sandbox runtimes. Kubernetes never
becomes product authorization or durable workflow authority by itself.

### Explicitly not selected

This ADR does not select a programming language, web framework, cloud vendor,
OIDC product, database product, object-storage product, model provider, scanner,
parser, Redis, broker, Kubernetes distribution, or sandbox product. Selection
must conform to this baseline and complete the open deployment/security
questions in the MVP architecture. [ADR-007](ADR-007-oss-first-self-hostable-deployment.md)
adds the approved constraint that the reference deployment is OSS-first and
self-hostable without mandatory cloud services.

## Consequences

- MVP implementation has one authoritative technical shape and clear trust,
  consistency, asynchronous-work, and recovery boundaries.
- Strong cross-record invariants remain local to the approved primary transaction
  instead of becoming distributed coordination problems.
- The SPA, gateway, OIDC provider, model provider, artifact store, optional cache,
  and future scheduler remain adapters rather than domain authorities.
- The system can add Redis, a broker, Kubernetes, or stronger isolation later,
  but only with evidence and explicit compatibility/security decisions.
- Feature teams must still publish the detailed Session, Evaluation, Review,
  Result/Release, upload-policy, identity-configuration, lifecycle-value, schema,
  and operational contracts listed by the MVP architecture.
- The approved architecture does not mark implementation complete or remove the
  requirement for specification-driven TDD, negative isolation tests, failure
  injection, load evidence, UI/UX specifications, or production recovery proof.

## Related

- Approved baseline: [MVP architecture](../mvp-architecture.md)
- Deployment portability: [ADR-007](ADR-007-oss-first-self-hostable-deployment.md)
- Operational defaults: [MVP operational defaults](../../requirements/mvp-operational-defaults.md)
- Product scope: [MVP scope](../../product/mvp-scope.md)
- Requirements: [P0 authoring order](../../requirements/README.md#p0-authoring-order)
- Authorization: [ADR-002](ADR-002-authorization-enforcement-and-delegation.md)
- Audit persistence: [ADR-003](ADR-003-authorization-audit-persistence.md)
- Activation atomicity: [ADR-004](ADR-004-assessment-activation-baseline-and-atomicity.md)
- Attempt/Session start: [ADR-005](ADR-005-atomic-attempt-start-and-submission-binding.md)
