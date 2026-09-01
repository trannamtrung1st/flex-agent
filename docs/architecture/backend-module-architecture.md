# Backend module architecture

Implementation guidance for structuring the Flex Agent backend consistently as
the MVP modular monolith grows.

## Status and authority

**Approved — 2026-09-01.** This guide currently owns backend module identity,
ports-and-adapters rules, authorization-kernel placement, append-only audit
coupling, Assessment source/activation coordination, and replica-independent
Enrollment admission constraints. It does not introduce product behavior or replace
feature specifications. ADR files remain until Phase 5 as provenance only. If this guide
conflicts with product or requirements, stop and record the conflict.

Workspace, toolchain, and CI verification live in
[Workspace development](../contributing/workspace.md) and
[`build/toolchain.json`](../../build/toolchain.json).

## Current extracted constraints

These remain current backend architecture requirements:

- One in-process authorization kernel with enforcement adapters; trusted
  Organization, activity, participant, and session scope; client-supplied
  identifiers are never authority; commit-time reauthorization; bounded
  service delegation.
- Authoritative authorization audit is append-only in the primary
  transactional store and is coupled to protected mutations.
- Assessment activation uses configuration-owned source versions and
  fail-closed in-transaction revalidation for required source owners.
- Enrollment request admission that must be replica-independent uses a
  PostgreSQL-backed application admission port with database UTC, atomic
  bounded acquisition, and fail-closed shared-state behavior. Gateway coarse
  limits are not that actor-scoped product quota.

## Architectural identity

The Flex Agent backend is a **domain-oriented modular monolith with ports and
adapters**.

- **Modular monolith** describes the system and deployment boundary: API and
  worker processes compose one shared set of business modules over one
  authoritative relational primary. A module is not a network service.
- **Domain-oriented** describes decomposition: modules follow owned business
  capabilities and durable-state boundaries, not repository-wide technical
  layers.
- **Ports and adapters** describes module integration: application-owned ports
  isolate domain and application policy from transport, persistence, model,
  object-storage, identity-provider, telemetry, and other implementation
  details.
- **Clean Architecture dependency rules** apply inward: domain policy does not
  depend on application orchestration, and neither domain nor application
  policy depends on hosts or concrete adapters. Clean Architecture is not a
  required project-per-layer template.

This description is intentionally more precise than calling the backend only
"Clean Architecture" or "hexagonal architecture." It fixes dependency and
ownership rules while allowing module packaging to evolve when an executable
boundary justifies the added project.

## System-level structure

```text
src/
  Hosts/
    FlexAgent.Api/                 # HTTP/SSE composition root
    FlexAgent.Worker/              # durable-work composition root
  Modules/
    <Capability>/
      FlexAgent.<Capability>/      # domain, application, and owned ports
      FlexAgent.<Capability>.Infrastructure/  # concrete adapters when split
  BuildingBlocks/                  # narrowly governed shared primitives
  Infrastructure/                 # shared low-level adapter primitives only
contracts/                         # canonical language-neutral schemas
database/migrations/               # immutable one-time SQL migrations
tests/                             # architecture, domain, contract, adapter,
                                   # integration, and runtime evidence
```

Only create a directory or project when it owns implemented behavior. Empty
layers and placeholder modules are prohibited.

### Dependency direction

```text
API / Worker composition roots
              |
              v
       module application
              |
              v
         module domain

concrete adapters --implement--> application-owned ports
```

Compile-time references may point toward the core or be assembled by a
composition root. Domain and application code must not reference a concrete
adapter to make runtime wiring convenient.

## Module internal structure

Every substantive business module owns these conceptual areas even when they
initially share one assembly:

| Area | Owns | Must not own or depend on |
| --- | --- | --- |
| Domain | Invariants, value objects, entities, state transitions, domain decisions, and stable domain errors | Application handlers, HTTP, database access, queues, provider SDKs, telemetry SDKs, or another module's internals |
| Application | Commands, queries, use-case orchestration, transaction intent, trusted execution context, and outbound ports | ASP.NET endpoints, SQL/Npgsql/Dapper, external-provider implementations, or model-authored authorization facts |
| Adapters | PostgreSQL repositories, HTTP endpoint mapping, provider clients, object-store clients, event/work delivery, and telemetry integration | Reusable domain policy or independent authorization/workflow rules |
| Composition | Dependency registration, configuration binding, host lifecycle, and adapter selection | Business decisions, resource ownership, or reusable use cases |

Use namespaces to expose the conceptual areas:

```text
FlexAgent.<Capability>.Domain
FlexAgent.<Capability>.Application
FlexAgent.<Capability>.Infrastructure
```

An adapter may live in its own assembly while retaining module ownership. The
repository-level `src/Infrastructure/` directory is only for shared low-level
adapter primitives such as connection and transaction support; it must not
become a shared business-services layer.

## Project-splitting policy

Start with the smallest packaging that preserves the dependency rules. A module
may use one core assembly with internal namespaces while it has a small,
cohesive implementation.

Create `FlexAgent.<Capability>.Infrastructure` when at least one of these
conditions applies:

- the module introduces Npgsql, Dapper, an external-provider SDK, an object
  store, a queue/broker client, or another replaceable infrastructure package;
- keeping the adapter in the core assembly would make a forbidden dependency
  available to domain or application code;
- the API and worker require different concrete adapter compositions;
- security-sensitive policy, especially authorization or protected-resource
  isolation, benefits from compile-time separation from persistence;
- architecture tests cannot reliably enforce the intended boundary within one
  assembly; or
- a second adapter implementation or focused adapter test suite makes the
  boundary independently useful.

Do not create separate `Domain` and `Application` projects by default. Split
them only when a demonstrated dependency problem, independent reuse boundary,
or executable architecture test makes the distinction valuable. Folder names
alone do not establish isolation.

Existing modules may migrate to this shape incrementally when their next
substantive change crosses a split condition. A documentation-only cleanup does
not justify moving stable code.

## Ports and adapter placement

An outbound port belongs to the application or domain area that defines the
needed capability. Its implementation belongs to an adapter assembly or
adapter namespace.

```csharp
namespace FlexAgent.Sessions.Application;

internal interface ISessionRuntimeRepository
{
    Task<SessionRuntimeState?> GetForOrganizationAsync(
        OrganizationId organizationId,
        SessionId sessionId,
        CancellationToken cancellationToken);
}
```

The example is illustrative; governing contracts determine actual types and
operations. The important properties are that the port expresses an
application need, requires trusted scope, and does not leak Npgsql, Dapper, SQL,
provider SDK, or transport types.

Inbound adapters such as HTTP endpoints and durable-work consumers establish
trusted actor and resource context, validate their transport contract, invoke
an application use case, and map the result. They must not duplicate domain
rules or turn request, event, URL, provider, or model-supplied identifiers into
authorization evidence.

## Module ownership and collaboration

Each durable record, table, workflow transition, and business invariant has one
owning module.

- A module must not read or write another module's tables through its own SQL.
- A module must not reference another module's infrastructure namespace or
  adapter assembly.
- Synchronous collaboration uses a narrow application-facing contract exposed
  by the owning module.
- Asynchronous collaboration uses an explicitly versioned event or durable-work
  contract owned by its producer and accepted by its consumer.
- Cross-module atomic work uses an explicitly named application coordinator and
  a transaction capability approved by all state-owning modules. A shared
  database handle, service locator, or ambient transaction is not a substitute.
- Query projections spanning module-owned data require an explicitly approved
  read model and authorization boundary; they must not become an unscoped path
  around module ownership.

Do not add a network boundary between modules merely to make them look
independent. Extraction to a service requires a separate architecture decision
covering authority, consistency, failure, idempotency, compatibility,
operations, and data migration.

## Persistence and isolation rules

Repository methods for protected data must accept trusted Organization and
resource scope. Avoid unscoped convenience methods such as `GetById`, `List`,
or `Count` when the record is tenant-, Activity-, Participant-, or
Session-scoped.

Concrete repositories must:

- use explicit parameterized SQL and select explicit columns;
- constrain protected access before materializing data;
- preserve module table ownership and composite scope constraints;
- participate in the approved transaction, idempotency, audit, and outbox
  boundary for the use case;
- perform commit-time authorization where the governing contract requires it;
- avoid external network calls while a database transaction is open; and
- expose bounded failures without including protected content or secrets.

A repository interface does not make an operation safe by itself. Real
PostgreSQL negative tests must prove wrong-Organization, wrong-resource,
guessed-identifier, list/count, stale-authorization, and concurrent behavior
for sensitive paths.

## Host and contract boundaries

`FlexAgent.Api` and `FlexAgent.Worker` are composition roots. They may reference
module cores and concrete adapters for wiring, but they must not own reusable
domain policy. A host-specific adapter should remain thin and move into the
owning module when it becomes reusable or contains policy.

Canonical schemas under `contracts/` define approved wire and durable
contracts. C# DTOs and generated browser types are projections of those
contracts; they must not become domain entities or authorization evidence.
`FlexAgent.Contracts` contains browser-safe contract surfaces only and must not
expose permissions, credentials, or backend ownership internals.

## Building-block admission rule

Code belongs in `BuildingBlocks` only when all of these are true:

1. at least two modules require the same stable capability;
2. the capability has one precise, non-feature-specific responsibility;
3. sharing it does not merge domain ownership or create a generic utility
   dumping ground; and
4. its dependency surface can be kept narrower than the consuming modules.

Otherwise, keep the code in the owning module. Duplication is preferable to a
premature shared abstraction that couples unrelated domain concepts.

## Required verification

Every new module or material boundary change must include proportionate
evidence for `GATE-STACK-MODULES` and, when protected data is involved,
`GATE-STACK-ISOLATION`:

- architecture tests reject domain-to-application, domain/application-to-
  adapter, host-policy, browser-to-backend, and forbidden cross-module
  dependencies;
- negative-control fixtures prove that each dependency rule can actually fail;
- tests reject cross-module table writes and unscoped protected repository
  entry points;
- domain tests exercise invariants without infrastructure;
- adapter integration tests run against the real dependency where behavior or
  isolation relies on it; and
- composition tests prove both API and Worker wire only approved adapters and
  do not silently fall back.

The implementation task must map governing requirement or acceptance IDs to
the module, use case, adapter, and verification surface. Passing architecture
tests does not replace feature, integration, authorization, concurrency, or
recovery tests.

## Review checklist

Before accepting a new module or significant backend change, verify:

- [ ] The capability and durable-state owner are explicit.
- [ ] Domain invariants are independent of application and infrastructure.
- [ ] Application use cases depend on bounded ports and trusted context.
- [ ] Adapter dependencies are outside the module core when a split condition
  applies.
- [ ] The host performs composition and transport mapping only.
- [ ] Cross-module calls use an owned contract rather than another module's
  repository, tables, or infrastructure.
- [ ] Protected repository operations require trusted Organization/resource
  scope and have negative isolation tests.
- [ ] Audit, outbox, idempotency, ordering, and transaction ownership match the
  governing specification and ADR.
- [ ] Canonical contract types remain separate from domain entities and do not
  grant authority.
- [ ] Architecture tests include a working negative control for each new rule.
- [ ] No empty layer, generic shared service, or premature network service was
  introduced.

## Current implementation note

The repository is converging incrementally toward this structure. Sessions
already separates its core from PostgreSQL adapters. Smaller modules may still
contain domain, application, and adapter namespaces in one assembly. That is
permitted until a project-splitting condition applies; it is not permission for
domain/application code to depend on infrastructure.

## Related documents

- [MVP architecture](mvp-architecture.md)
- [ADR-002: authorization enforcement and delegation](decisions/ADR-002-authorization-enforcement-and-delegation.md)
- [ADR-006: MVP architecture baseline and evolution](decisions/ADR-006-mvp-architecture-baseline-and-evolution.md)
- [ADR-010: .NET implementation stack and workspace](decisions/ADR-010-dotnet-implementation-stack-and-workspace.md)
- [Workspace development](../contributing/workspace.md)
