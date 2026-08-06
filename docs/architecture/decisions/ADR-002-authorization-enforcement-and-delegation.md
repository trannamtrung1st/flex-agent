# ADR-002: Authorization enforcement and delegated execution

## Status

Approved

## Owners and approvers

- Owner: Architecture Lead
- Approvers: Product Lead, Architecture Lead, Security/Privacy reviewer
- Approved date: 2026-08-06

## Context

The approved [authorization and isolation specification](../../requirements/features/auth-resource-isolation.md) requires the same deny-by-default authorization contract across interactive requests, queries, mutations, file access, real-time connections, background jobs, event consumers, caches, search, and projections. It also requires trusted server-side scope, commit-time reauthorization for sensitive mutations, bounded service delegation, non-disclosing denials, and revocation propagation within 60 seconds.

The requirements intentionally do not select a policy engine, authorization library, database row-security mechanism, authentication provider, or network topology. Architecture still needs one consistent decision and enforcement boundary so each delivery path does not reinterpret roles, relationships, ownership, or workflow state independently.

## Decision drivers

- Deny-by-default organization, activity, resource-subject, and session isolation.
- One policy meaning across synchronous, real-time, and asynchronous paths.
- Current authoritative state at sensitive commit boundaries.
- No trust in client-supplied ownership, role, or scope identifiers.
- Revocation of new HTTP operations immediately and revalidation or termination of cached and real-time access within 60 seconds.
- Bounded latency, observability, testability, and evolution without requiring a distributed authorization service prematurely.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Ad hoc checks in handlers, repositories, jobs, and consumers | Low initial ceremony | Policy drift, inconsistent denial behavior, missing paths, and weak negative-test coverage |
| Dedicated remote authorization service from the start | One network policy boundary and independent scaling | Adds latency, availability, deployment, versioning, and consistency failure modes before scale requires them |
| One application-owned policy contract with distributed enforcement adapters | Consistent decisions, in-process MVP simplicity, reusable test fixtures, and a later extraction path | Requires disciplined boundary coverage and explicit versioned inputs |
| Database row security as the complete solution | Strong defense at one persistence boundary | Does not cover files, caches, search, real-time, queues, external calls, function authorization, or workflow state by itself |

## Decision

### Logical authorization kernel

Use one application-owned, versioned authorization decision contract as the logical policy decision point. For the MVP, implement it as an in-process domain module rather than a separately deployed network service. Delivery adapters call the same contract; they do not embed independent role or ownership rules.

The contract evaluates at minimum:

- authenticated human or service identity;
- trusted organization scope;
- action and resource type;
- authoritative resource and parent-ownership references;
- active capabilities, memberships, assignments, enrollments, or delegations;
- current workflow or visibility state supplied by the owning feature;
- policy, relationship, and grant versions or effective times needed for freshness; and
- correlation and source-channel context needed for audit and observability.

Role labels expand to capabilities and scoped relationships before evaluation. A role label, network location, identifier, cached claim, or client-provided scope is never sufficient authorization evidence.

### Enforcement boundaries

| Boundary | Enforcement |
| --- | --- |
| HTTP/API or server-rendered request | Authenticate, derive trusted organization context, and authorize the action/resource before returning protected data or beginning a side effect. |
| List, search, count, and aggregate | Apply organization and permitted-resource constraints in the query or index request before materialization, pagination, or totals; do not post-filter an unscoped result. |
| Mutation or workflow transition | Authorize at command admission and revalidate current policy, relationship, ownership, and workflow state inside the authoritative commit boundary. |
| Nested resource or file delivery | Resolve and validate the complete parent chain before issuing a download capability or streaming bytes; changing an identifier must trigger a new decision. |
| Real-time connection | Authorize connection establishment and each privileged subscription or command; terminate or narrow access after expiry, revocation, or scope change within the approved propagation target. |
| Background job or event consumer | Authenticate the service, load a trusted delegation reference and resource scope, and revalidate current authorization before protected work and again before a sensitive commit. |
| Cache, search projection, or derived view | Partition keys and entries by trusted organization and resource scope, include relevant policy/relationship versions, and never broaden the authoritative decision. |

### Delegated service execution

Represent service delegation as a durable, auditable record with a stable identifier, organization, allowed actions, resource scope, initiating actor or system purpose, effective/expiry times, and revocation status. Jobs and events carry the delegation reference plus resource locators in a trusted envelope; they do not carry a reusable human credential or treat payload scope as authoritative.

At execution, the worker authenticates as its own service identity, loads the delegation and resource ownership from authoritative state, and rejects missing, expired, revoked, cross-organization, or action-incompatible delegation. Retries remain bound to the same organization, resource, operation, and idempotency context.

### Freshness and revocation

Authorization data and positive-decision caches use keys that include actor/service identity, organization, action, resource, and relevant policy, assignment, grant, or relationship version. Cached data may accelerate a decision but cannot authorize a sensitive commit without current-version validation.

New interactive operations must observe the authoritative revocation before authorization succeeds. Real-time connections and other long-lived access use targeted invalidation when available plus bounded revalidation so stale access is terminated or narrowed within 60 seconds. Delayed jobs revalidate when execution begins and immediately before a sensitive mutation or disclosure.

### Decision result

The decision contract returns a permit/deny result, a stable internal reason code, the policy and relationship versions used, the grant/assignment/delegation reference when applicable, and audit metadata. Delivery adapters map inaccessible and nonexistent resources to the same approved non-disclosing external behavior while retaining distinct internal diagnostics.

An unavailable or inconsistent required authorization dependency returns deny. The authorization kernel does not select authentication-provider UX, generate invitation credentials, or define business workflow state.

## Approved decision disposition

| Question | Approved disposition |
| --- | --- |
| `Q-ADR2-1` | Keep the authorization kernel in-process for the MVP. Reconsider extraction only when multiple independently deployed trusted systems require the same boundary and measured duplication, release coupling, or scale outweighs the added network failure mode. |
| `Q-ADR2-2` | When deployment introduces multiple nodes, distribute targeted policy-version or invalidation messages and retain periodic authoritative revalidation as a fallback bounded by 60 seconds. Broker selection remains deferred until deployment topology demonstrates a need. |

## Consequences

- Authorization rules have one logical owner and reusable contract tests.
- Every transport and storage path still needs an explicit enforcement adapter and negative isolation tests.
- Sensitive mutations pay the cost of current-state revalidation; caches cannot be the sole authority.
- The design supports later extraction to a service without requiring one for the MVP.
- Policy-engine, database, cache, message-broker, authentication-provider, and invitation-token products remain undecided.

## Related

- Requirements: [`REQ-AUTH-1`–`REQ-AUTH-25`](../../requirements/features/auth-resource-isolation.md#business-rules)
- Acceptance criteria: [`AC-AUTH-1`–`AC-AUTH-13`, `AC-AUTH-16`–`AC-AUTH-20`](../../requirements/features/auth-resource-isolation.md#acceptance-criteria)
- Approved defaults: [`PROP-1`–`PROP-4`, `PROP-7`, `PROP-9`](../../requirements/features/auth-resource-isolation.md#approved-defaults)
- Audit persistence: [ADR-003](ADR-003-authorization-audit-persistence.md)
