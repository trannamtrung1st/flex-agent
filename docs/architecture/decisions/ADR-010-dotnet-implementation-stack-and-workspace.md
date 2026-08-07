# ADR-010: .NET implementation stack and workspace

## Status

Approved — 2026-08-07

This decision selects implementation technology only. It does not change
product meaning, observable behavior, UI/UX behavior, or the approved
architecture boundaries.

## Decision metadata

| Field | Value |
| --- | --- |
| **Owner** | Architecture Lead |
| **Decision owners** | Architecture Lead, Backend Lead, Frontend Lead |
| **Approvers** | Architecture owner, Backend owner, Frontend owner, Security/Privacy reviewer, Operations owner |
| **Consulted perspectives** | Architecture, security/privacy, documentation |
| **Approved date** | 2026-08-07 |
| **Approval reference** | Explicit owner approval in the 2026-08-07 implementation-stack and follow-up schema/canonicalization reviews |
| **Governs** | Application runtime and language, backend and SPA frameworks, contract-validation approach, PostgreSQL access and migration tooling, test stack, workspace layout, and dependency/build conventions |

## Context

[ADR-006](ADR-006-mvp-architecture-baseline-and-evolution.md) approves a
modular monolith delivered through a browser SPA, stateless API, disposable
workers, one authoritative relational primary, durable database-backed work,
and private replaceable adapters. It deliberately does not select a programming
language or web framework. [ADR-007](ADR-007-oss-first-self-hostable-deployment.md)
requires portable OCI packaging and an OSS-first self-hostable reference path.
[ADR-008](ADR-008-bounded-oss-component-set.md) selects PostgreSQL, Keycloak,
SeaweedFS, NGINX, Docker Compose, and provider/deployment families without
selecting the application stack.

The implementation owners have materially more .NET experience and value its
compile-time tooling, explicit project boundaries, mature application
architecture conventions, and focused API/worker deployment model. This
resolves the former uncertainty about whether team constraints favor .NET over
Node.js. The MVP remains a modular monolith with separate API and worker
processes; this decision preserves a later microservice extraction path but does
not authorize premature networked services.

The stack must support the approved detailed contracts for
[text Session runtime](../session-runtime-contract.md),
[Evidence and Evaluation execution](../evaluation-execution-contract.md), and
[Human review, Result, and Release](../review-result-release-contract.md). It
must also make machine-readable schemas and conformance fixtures first-class
versioned artifacts rather than incidental generated API documentation.

## Confirmed constraints

- Product and feature specifications continue to govern behavior. Framework or
  serializer defaults cannot redefine an approved requirement or acceptance
  criterion.
- Domain policy must remain independent from ASP.NET Core, React, Npgsql,
  Dapper, OIDC, artifact stores, model providers, and telemetry products.
- The API and workers share one modular-monolith domain implementation. They are
  separate composition roots, not independent services with duplicated policy.
- PostgreSQL remains authoritative for durable workflow state, immutable
  records, audit/outbox, ordering, idempotency, and approved atomic boundaries.
- The SPA, generated C# or TypeScript types, provider claims, request payloads,
  URLs, events, projections, caches, and model output are never authorization
  evidence by themselves.
- Commands, events, canonical documents, work items, Evidence locators, audit
  events, and protected artifact metadata require explicit schema and procedure
  versions.
- API and worker processes must be stateless and disposable. Long or external
  work runs through approved durable work and lease boundaries, not an open HTTP
  request or local process memory as sole state.
- The reference implementation must build into portable OCI images and run
  non-interactively in the approved Docker Compose profiles.
- Sensitive content, credentials, raw provider keys, and real Participant data
  must not enter source, generated fixtures, logs, telemetry, browser bundles,
  or test artifacts.

## Decision drivers

- Use the implementation owners' .NET experience to keep backend code compact,
  structured, and maintainable.
- Enforce modular boundaries through project references, visibility rules,
  analyzers, and executable architecture tests.
- Keep language-neutral, versioned wire and durable-record schemas despite the
  C# backend and TypeScript SPA.
- Preserve exact PostgreSQL transaction and SQL control without an
  active-record or change-tracking abstraction becoming the domain model.
- Use mature OIDC, SSE, JSON Schema, PostgreSQL, telemetry, object-storage, and
  model-provider integrations.
- Combine fast unit feedback with real PostgreSQL, provider-contract, browser,
  failure, concurrency, and negative-isolation verification.
- Keep a small deployment surface without an orchestration framework, SSR
  platform, broker, cache, or migration-on-startup behavior becoming an MVP
  dependency.
- Preserve reproducible dependency resolution, reviewable build steps, SBOM
  generation, and controlled upgrades for both NuGet and frontend packages.

## Options considered

| Option | Benefits | Costs and risks | Disposition |
| --- | --- | --- | --- |
| C# on ASP.NET Core with React/Vite | Strong owner experience; mature runtime, security, telemetry, database, testing, and worker support; compile-time project boundaries; compact API and worker images/processes; clean later extraction path | Two application languages; language-neutral contracts and generated browser types still require an explicit pipeline; schema-first tooling is less turnkey than Ajv-based Node.js integration | **Selected** |
| TypeScript on Node.js, Fastify, React/Vite | One language across application surfaces; strong JSON Schema and browser tooling; low-ceremony API and worker composition | Less aligned with owner experience; TypeScript types disappear at runtime; event-loop and dependency-supply-chain controls add work; framework conventions do not enforce module or tenant boundaries | Not selected after the owner-experience constraint was confirmed |
| Python on FastAPI with React | Strong AI/provider ecosystem and concise APIs | Two application languages remain; async and type guarantees require more discipline; current providers run behind HTTP-compatible adapters and do not require Python in the application boundary | Not selected; Python-native work may run behind a versioned worker/provider port later |
| EF Core as the default persistence and migration authority | Productive CRUD, change tracking, LINQ, and a broad ecosystem | Change tracking, navigation cascades, generated migrations, and implicit unit-of-work conventions can obscure append-only history, exact SQL, Organization scope, and audited transaction boundaries | Not selected as the default; reconsider only for a bounded read/write surface with explicit evidence and no EF migrations |
| Npgsql alone | Smallest database abstraction and complete SQL control | Repetitive mapping and command setup for ordinary queries | Supported escape hatch for critical or advanced PostgreSQL operations |
| Npgsql plus Dapper | Explicit SQL and transaction ownership with compact parameter/result mapping | Requires SQL literacy and runtime mapping tests; compile-time types do not replace database constraints or boundary validation | **Selected** |
| `node-pg-migrate` | Familiar in a Node.js stack and supports PostgreSQL migrations | Couples migration execution to the rejected backend runtime and JavaScript migration APIs | Not selected |
| Grate plain-SQL migrations | Runtime-independent SQL source, PostgreSQL support, immutable one-time scripts, dry-run/baseline/up-to-date modes, and no EF migration authority | Transaction and token-replacement behavior must be configured explicitly; changed one-time scripts must fail rather than be ignored | **Selected** |
| Next.js or another full-stack rendering framework | Integrated routing, rendering, and server features | Introduces a second server/application boundary and blurs the approved SPA/API authority split; SSR is not an MVP requirement | Not selected for MVP |

## Decision

| ID | Approved decision | Rationale and boundary |
| --- | --- | --- |
| `STACK-DEC-1` | Use .NET `10.x` LTS, C#, and ASP.NET Core `10.x` for the API and worker runtimes. Pin the SDK through `global.json` and consume supported security patches. | .NET 10 is supported through November 2028 and matches owner expertise. The API and worker remain separate composition roots over shared versioned modules. |
| `STACK-DEC-2` | Use a repository solution with central NuGet package management, committed restore lock files, locked CI restore, nullable reference types, warnings-as-errors, deterministic builds, and analyzers. Do not add a separate .NET build orchestrator initially. | Native `dotnet` build/test/publish tooling is sufficient until measured task-graph or build-performance evidence says otherwise. |
| `STACK-DEC-3` | Use thin ASP.NET Core endpoint groups for HTTP commands, queries, health/readiness, OIDC callbacks, and SSE. Endpoints validate transport, establish trusted identity/context, invoke application commands/queries, and map deny-by-default responses. | ASP.NET Core 10 supplies the required OIDC, OpenAPI 3.1, JSON Schema 2020-12 representation, and typed SSE primitives without becoming the domain boundary. |
| `STACK-DEC-4` | Use React `19.2.x`, Vite `8.1.x`, and strict TypeScript for a client-rendered SPA. Keep Node.js and `pnpm` as frontend/build tooling only; do not introduce SSR, server actions, or a framework-owned backend in the MVP. | Preserves ADR-006's explicit SPA/API authority boundary and produces static assets for the approved NGINX profile. Selecting .NET does not remove the frontend Node.js toolchain. |
| `STACK-DEC-5` | Make committed JSON Schema Draft 2020-12 documents the canonical machine-readable contract source. Use the `JsonSchema.Net` package for runtime and fixture validation, explicitly select `Dialect.Draft202012`, begin with reviewed C# DTO mappings, and generate OpenAPI `3.1.x` as a transport projection. | Durable and wire contracts remain language-neutral. C# types, fluent schema builders, and generated OpenAPI improve ergonomics but are not contract authority and cannot grant authorization. Code generation is deferred until it proves useful without schema drift. |
| `STACK-DEC-6` | Use `System.Text.Json` with explicit strict options and source-generated metadata where useful. Reject unknown request-body members where the contract is closed, prohibit body type coercion, bound depth and payload size, and use deny-by-default response DTOs. | ASP.NET Core defaults are not trusted to preserve canonical input or prevent over-posting and response leakage. |
| `STACK-DEC-7` | Implement RFC 8785 JSON Canonicalization Scheme processing in a separate `FlexAgent.CanonicalJson` project behind a small application-owned boundary, followed by lowercase SHA-256 where approved procedures require it. Vendor a reviewed, exact-commit snapshot of the C# source from the RFC-listed `cyberphone/json-canonicalization` reference repository; do not consume a JCS NuGet package, Git submodule, or build/runtime network fetch. | Ordinary `System.Text.Json` serialization is not an RFC 8785 guarantee. A pinned source snapshot keeps the audit-relevant algorithm reviewable and reproducible while the separate project prevents it from becoming domain or infrastructure policy. |
| `STACK-DEC-8` | Use Npgsql `10.x` as the PostgreSQL driver and Dapper as a thin mapping helper. Use parameterized explicit SQL and Npgsql directly for advanced PostgreSQL features, transaction primitives, bulk operations, or mappings Dapper cannot express safely. Do not use EF Core or another ORM as the default persistence or migration authority. | Preserves exact SQL, scoped repository entry points, composite constraints, append-only history, and audited transaction boundaries while avoiding repetitive mapping code. |
| `STACK-DEC-9` | Use Grate `2.x` as the sole database migration runner with immutable plain-SQL one-time migrations. Run it as a separate deployment operation with PostgreSQL selected explicitly, transactions enabled, token replacement disabled, noninteractive mode enabled, and changed one-time scripts configured to fail. | Keeps schema authority in reviewed SQL and prevents API/worker startup, EF metadata, token substitution, or edited history from silently changing production state. |
| `STACK-DEC-10` | Use ASP.NET Core OpenID Connect for Keycloak Authorization Code flow with PKCE and server-side exchange. Store provider credentials only server-side. Use a PostgreSQL-backed opaque application-session identifier in the approved secure cookie and preserve the approved rotation, expiry, revocation, and SSE freshness behavior. | Avoids exposing provider tokens to the SPA and avoids making an instance-local authentication ticket or unshared Data Protection key ring the multi-instance session authority. |
| `STACK-DEC-11` | Use the official OpenAI .NET SDK behind the application-owned `ModelProvider` adapter. Direct OpenAI is the first external adapter; OpenRouter remains synthetic-development-only and vLLM remains the self-hosted OpenAI-compatible family. Provider-specific headers, feature differences, usage, cancellation, retry, normalized failure, and provenance stay inside adapters and contract tests. | The SDK supports custom endpoints, but protocol compatibility is evidence, not an assumption. No provider SDK becomes the domain contract or chooses a fallback silently. |
| `STACK-DEC-12` | Use the AWS SDK for .NET behind the application-owned artifact-store adapter, configuring the SeaweedFS endpoint and path-style addressing as required. | The SDK supports custom service URLs, but exact SeaweedFS behavior remains conditional on ADR-008's integrity, presigned-access, lifecycle, recovery, and compatibility gates. |
| `STACK-DEC-13` | Use OpenTelemetry .NET for traces, metrics, logs, and OTLP export through an allowlisted telemetry boundary. | Automatic instrumentation is useful but must not capture credentials, raw protected content, provider payloads, or unrestricted URLs/headers. |
| `STACK-DEC-14` | Use xUnit v3 for .NET domain, application, contract, adapter, and integration tests; Testcontainers for focused PostgreSQL 18 tests; approved Compose profiles for Keycloak and cross-component tests; and Playwright for browser journeys and visual verification. | Combines fast feedback with real-store, real-browser, negative-isolation, concurrency, and failure evidence. The repository's Playwright MCP verification requirements remain independent of the backend language. |
| `STACK-DEC-15` | Build API and worker with `dotnet publish`, build the SPA with Vite, and package API, worker, and static SPA/gateway artifacts in separate multi-stage OCI images. | Preserves disposable runtime roles and prevents SDKs, development dependencies, source-only secrets, or migration authority from entering runtime images unnecessarily. |
| `STACK-DEC-16` | Pin direct dependencies centrally and transitive graphs in lock files, prohibit floating container tags and prerelease packages in released profiles, produce SBOMs, scan dependencies/images, and review upgrades with focused contract, migration, and regression tests. | Extends ADR-008's supply-chain policy to NuGet and frontend packages. Package restore and build steps are code-execution boundaries. |
| `STACK-DEC-17` | Treat API and worker entry points as composition roots. Keep domain/application modules, adapters, contracts, and browser code separately importable, and enforce the dependency rules below in CI. | Prevents framework, provider, and persistence choices from becoming product semantics and keeps future module extraction reversible. |

Exact package, SDK, frontend-tool, and OCI digests are pinned by the
implementation lock files and component manifest. The supported lines above
are the review baseline, not permission to use floating ranges. Any dependency
that fails a verification gate must return to review rather than being silently
substituted.

## Approved workspace direction

```text
FlexAgent.slnx
global.json
Directory.Build.props
Directory.Packages.props
src/
  Hosts/
    FlexAgent.Api/          # ASP.NET Core composition root
    FlexAgent.Worker/       # durable-work composition root
  Modules/
    IdentityAccess/
    Configuration/
    Assessments/
    Submissions/
    Sessions/
    Evaluations/
    ReviewRelease/
  BuildingBlocks/           # bounded shared primitives, not a generic domain dump
    FlexAgent.CanonicalJson/
      Upstream/
        CyberphoneJsonCanonicalization/ # exact-commit source snapshot
      NOTICE.md             # source, commit, license, files, modifications
  Infrastructure/           # shared low-level adapter primitives only
web/                        # React/Vite SPA and browser-safe generated types
contracts/                  # canonical schemas, fixtures, compatibility profile
database/
  migrations/               # Grate one-time plain-SQL migrations
deploy/
  compose/                  # approved local/CI/evaluation profiles
tests/
  Architecture/
  CanonicalJson/
  Contract/
  Integration/
  EndToEnd/
```

Directories and projects are created only when their first governed behavior
is implemented. A module may begin as one assembly with internal domain,
application, and adapter namespaces. Split it into additional assemblies when
that creates a verified boundary; do not generate empty layer projects merely
to imitate a template.

## Dependency and ownership rules

1. `FlexAgent.Api` and `FlexAgent.Worker` may compose modules and platform
   adapters but must not own reusable domain policy.
2. `web` may consume browser-safe generated contract types and UI code. It must
   not import backend modules, persistence code, secret resolution, or provider
   SDKs.
3. A module owns its domain rules, application commands/queries, ports, and
   repository adapters. It must not write another module's tables directly.
4. Cross-module atomic work uses an explicitly named application coordinator
   and shared transaction capability approved by the owning modules. A service
   locator or exported general-purpose database handle is not acceptable.
5. Shared PostgreSQL infrastructure owns data-source, transaction, migration
   invocation support, and low-level observability primitives. It does not own
   feature tables, queries, Organization scope, or workflow decisions.
6. Contract artifacts contain schemas, fixtures, compatibility rules, and
   derived types. They contain no actor permissions, live secrets, real
   protected content, or mutable provider state.
7. Domain/application code must not reference ASP.NET Core, React, Npgsql,
   Dapper, OIDC, S3, model-provider, or telemetry SDKs. It depends on bounded
   ports and trusted application context.
8. Repository entry points require trusted Organization/resource scope and
   expose no unscoped list/count/get-by-ID convenience path for protected data.
9. Reflection-based mapping across trust boundaries, dynamic SQL identifiers,
   ignored validation errors, serializer exceptions, and dependency-rule
   exceptions require a narrow justification, review owner, and executable
   verification.
10. `FlexAgent.CanonicalJson` may depend only on the .NET base class library and
    its reviewed vendored source. It must not reference hosts, feature modules,
    databases, providers, telemetry SDKs, or web frameworks. Only code that
    builds or verifies approved digest documents may depend on its public
    boundary; domain policy and the SPA may not depend on it.

## Validation and API rules

- Validate every untrusted request body, path/query value, trusted header
  subset, provider response, stored versioned document, and event/work payload
  at its trust boundary against a supported schema/procedure version.
- Reject unknown request-body properties for closed contracts and do not coerce
  body types. Any bounded path/query parsing is explicit and contract-tested.
- Treat schemas as reviewed application artifacts. Never compile arbitrary
  user- or Organization-supplied schemas as executable validators.
- Use response DTOs as deny-by-default projections. Internal Evaluation,
  Evidence, reviewer notes, provider metadata, credentials, and audit fields do
  not flow to a response merely because a C# object contains them.
- Prove one compatibility profile across canonical Draft 2020-12 schemas,
  runtime/fixture validation, C# and browser types, and OpenAPI 3.1 projection.
  Unsupported keywords fail the build rather than being silently ignored.
- Give every canonical schema the exact
  `$schema: https://json-schema.org/draft/2020-12/schema` declaration and
  configure `JsonSchema.Net` with `Dialect.Draft202012`. A package-default or
  preview dialect is not acceptable. Keep a small reviewed keyword profile;
  unexpected or unsupported keywords fail the build.
- A generated client or type cannot choose Organization, actor, Activity,
  Participant, Session, role, ownership, entitlement, Result visibility, or
  Release authority. The server resolves those facts from trusted state.

## Canonicalization project and vendoring rules

- Import only the C# source needed from the RFC-listed reference repository and
  pin the exact upstream commit. Preserve upstream copyright and license
  headers. `NOTICE.md` records the source URL, commit SHA, copied files,
  Apache-2.0 license, and every local modification.
- Keep vendored upstream files isolated under `Upstream/`; application-facing
  parsing, validation, limits, error handling, and digest procedures live in a
  small reviewed wrapper. Upgrades are explicit source-review changes with
  refreshed fixtures, license inventory, SBOM, and `GATE-STACK-JCS` evidence.
- Accept strict UTF-8 and reject duplicate object properties, invalid Unicode
  including lone surrogates, non-finite numbers, and negative zero. Apply
  explicit byte, nesting-depth, property-count, and array-length limits before
  or during canonicalization. Flex Agent digest documents are top-level
  objects, and errors or telemetry must not reproduce protected input.
- Restrict audit-relevant schemas to interoperable semantics even when the
  reference implementation supports the full JCS number domain: safe integers
  may be JSON numbers; precision-sensitive decimals, scores, or money use
  normalized strings; timestamps use schema-normalized UTC strings; and
  semantic sets are sorted by their governing procedure before JCS processing.
- Test the untouched upstream behavior with the repository's official vectors
  and add Flex Agent fixtures for ADR-001, ADR-004, and Evidence-set procedures,
  including cross-language byte and lowercase SHA-256 agreement, rejection
  cases, verified RFC errata, and resource-limit cases.

## Persistence and migration rules

- Each Grate one-time migration is immutable after merge and has an unambiguous
  UTC order. Corrections append a new migration.
- Production migration execution is a separate bounded deployment operation,
  not an automatic side effect of API or worker startup.
- Grate runs noninteractively with `postgresql` selected explicitly,
  `--transaction` and `--disabletokenreplacement` enabled, and changed one-time
  script warning/ignore modes disabled. The production target database is
  provisioned separately.
- Migrations run from an empty PostgreSQL 18 database in CI and upgrade every
  supported prior schema state. Destructive or long-lock changes require an
  explicit expand/migrate/contract or forward-recovery plan.
- Composite Organization/resource constraints, uniqueness, append-only
  history, exact version bindings, expected versions, idempotency, durable
  work, outbox ordering, and required audit atomicity are enforced and tested
  in PostgreSQL, not inferred from C# types.
- Queries select explicit columns for protected data. Dynamic identifiers or
  raw result spreading into external projections are prohibited unless a
  narrow reviewed adapter proves the resulting field set is safe.
- Do not perform model, OIDC, object-store, or other unbounded network calls
  while holding a database transaction open.

## Verification gates and staged acceptance

| ID | Gate | Required evidence |
| --- | --- | --- |
| `GATE-STACK-RUNTIME` | Runtime compatibility | Minimal API, worker, and SPA build; clean startup, readiness, graceful shutdown, and publish on supported Linux `amd64` and `arm64` CI or documented equivalent evidence |
| `GATE-STACK-SCHEMA` | Schema compatibility | Representative command, event, canonical manifest, Evidence locator, audit event, error response, and SSE event validate through `JsonSchema.Net` from canonical schemas that declare Draft 2020-12; the runtime explicitly selects that dialect; derived C#/browser types and OpenAPI reproduce cleanly; preview-dialect, unexpected-keyword, and unsupported-keyword tests fail closed |
| `GATE-STACK-JCS` | Canonicalization | The separate project records and preserves the exact vendored source commit and Apache-2.0 notices; official upstream/RFC 8785 vectors plus ADR-001, ADR-004, and Evidence-set cross-language fixtures reproduce byte-for-byte canonical UTF-8 and lowercase SHA-256 values; malformed input, verified errata, boundary, and resource-limit tests fail closed |
| `GATE-STACK-HTTP` | HTTP and serialization | Contract tests prove body non-coercion, unknown-field rejection, bounded query/path parsing, safe error mapping, payload/connection limits, OIDC/session behavior, and no sensitive response-field leakage |
| `GATE-STACK-POSTGRES` | PostgreSQL and Grate | Empty migration, repeat invocation, changed-script failure, transaction rollback, migration concurrency/locking behavior, upgrade path, composite scope constraints, and one atomic audit/outbox boundary pass against PostgreSQL 18 |
| `GATE-STACK-MODULES` | Module boundaries | Automated architecture tests reject framework imports in domain/application code, browser-to-backend references, cross-module repository writes, and unscoped protected repository methods |
| `GATE-STACK-ISOLATION` | Authorization/isolation | Positive and complete wrong-Organization, wrong-Activity, wrong-Participant, guessed-ID, list/count, stale-delegation, and background-work scope matrices pass against real repositories |
| `GATE-STACK-PROVIDERS` | Model compatibility | Fake, direct OpenAI, synthetic OpenRouter, and vLLM contract tests cover streaming, structured outputs used by the MVP, timeouts, cancellation, retries, failure normalization, usage/provenance, and fail-closed no-fallback behavior |
| `GATE-STACK-ARTIFACTS` | S3 compatibility | Exact SeaweedFS version passes private object, integrity, conditional/version behavior, presigned delivery, metadata, lifecycle, cleanup, recovery, and wrong-scope tests required by ADR-008 |
| `GATE-STACK-SESSION` | Application session and SSE | Multi-instance opaque-session rotation/revocation and synthetic SSE reconnect/replay preserve authoritative sequence ordering; disconnect does not change Session authority |
| `GATE-STACK-BROWSER` | Browser build | Static SPA loads through NGINX and Playwright verifies an authenticated synthetic journey at desktop and narrow viewports without storing credentials in artifacts |
| `GATE-STACK-SUPPLY` | Supply chain | Locked restores/installs, license inventory, SBOM, vulnerability and secret scans, and OCI builds complete without floating dependencies or embedded credentials |
| `GATE-STACK-OPERABILITY` | Operability | API and worker expose bounded liveness/readiness, terminate gracefully, stop claiming work during shutdown, and emit allowlisted telemetry without raw protected content |

This ADR approves the technology direction separately from implementation
certification. The initial scaffold is not accepted until the runtime, schema,
JCS, HTTP, PostgreSQL/Grate, module-boundary, supply-chain, and operability gates
pass. Affected feature work cannot claim acceptance until its applicable gates
also pass. A failed gate requires an ADR update or explicitly reviewed
replacement, not silent tool substitution.

Feature implementation still follows specification-driven
red-green-refactor TDD and does not inherit acceptance from an architecture
spike.

## Security and privacy assessment

| Risk | Impact | Control | Verification |
| --- | --- | --- | --- |
| Compile-time types are trusted as runtime authorization | Cross-Organization or cross-resource disclosure/mutation | Runtime validation; server-resolved actor/resource scope; scoped repositories; authorization kernel at command/query and sensitive commit | Tampered payload, guessed ID, wrong-scope, stale-permission, and generated-client misuse tests |
| Serializer or model-binding defaults mutate or leak payloads | Validation bypass, ambiguous audit input, or sensitive response disclosure | Strict `System.Text.Json`; explicit binding/parsing; deny-by-default response DTOs; safe errors | Unknown-field, coercion, over-posting, response-extra-field, and error-leak tests |
| Instance-local cookie protection becomes session authority | Logout/revocation gaps, broken multi-instance behavior, or replay | PostgreSQL-backed opaque session, identifier rotation, server-side provider credentials, bounded freshness checks | Login fixation, rotation, expiry, revocation, logout, privilege-change, and cross-instance tests |
| Shared database helpers encourage unscoped access | Severe tenant-isolation failure | Module-owned repositories, trusted scope parameters, composite constraints, no generic database export, architecture tests | Query/list/count/export/background wrong-Organization matrix |
| Grate defaults permit non-transactional or substituted SQL | Partial migration, environment drift, or silently changed history | Explicit transaction; token replacement disabled; immutable one-time scripts; separate migration identity and deployment step | Partial-failure rollback, changed-script, repeat, concurrent-run, and wrong-identity tests |
| Provider or S3 compatibility is assumed from protocol labels | Data disclosure, corrupt evidence, lost provenance, or silent model behavior changes | Application-owned adapters, exact-version contract gates, bounded retries, no fallback, immutable provider/artifact identity | Provider matrix, SeaweedFS contract suite, cancellation, integrity, and wrong-scope tests |
| Auto-instrumentation captures protected content | Credentials or Participant data disclosed through telemetry | Attribute allowlist, redaction, bounded error mapping, no raw payload/body capture | Telemetry fixture scan and synthetic-secret leak test |
| NuGet, npm, build, or container compromise | Build-time code execution or runtime compromise | Central pins and locks, locked restore/install, SBOM, scanning, provenance where available, controlled upgrades | Clean-room restore, lock-change review, unexpected-source/script failure, image scan |
| Vendored canonicalization code drifts or permits resource exhaustion | Cross-runtime digest mismatch, denial of service, or irreproducible integrity records | Exact source commit and notices; isolated project and wrapper; bounded input/depth/cardinality; explicit upgrade review | Source/provenance diff, official and Flex fixtures, malformed/large/deep input, timeout, and allocation tests |
| Deterministic evaluator runs in-process as a supposed sandbox | Network/file/process escape or Participant-controlled execution | Separate restricted worker/container boundary, no egress by default, immutable evaluator identity, positive resource limits | Egress, executable selection, resource exhaustion, cancellation, and containment tests |

This is engineering guidance, not a compliance certification. Documentation,
types, and framework configuration are not implementation evidence.

## Approved tooling question disposition

No implementation-stack tooling question remains open. Compatibility evidence
is still required and cannot be inferred from selection approval.

| Prior ID | Approved disposition | Remaining acceptance condition |
| --- | --- | --- |
| `Q-STACK-1` / `PROP-STACK-1` | Resolved: use `JsonSchema.Net`, explicit `Dialect.Draft202012`, committed schemas as authority, and explicit C# DTO mappings initially. | Pass `GATE-STACK-SCHEMA`; code generation requires later evidence and review before adoption. |
| `Q-STACK-2` / `PROP-STACK-2` | Resolved: create `FlexAgent.CanonicalJson` and vendor an exact-commit C# source snapshot from `cyberphone/json-canonicalization` behind the hardened project boundary. | Record the selected commit during import and pass `GATE-STACK-JCS`; neither approval nor upstream provenance substitutes for local security and compatibility evidence. |

## Consequences

### Positive

- The backend uses the owners' strongest language and a mature runtime while
  preserving the approved modular-monolith topology.
- C# projects, internal visibility, analyzers, and architecture tests make
  module and adapter direction explicit without requiring microservices.
- Npgsql, Dapper, plain SQL, and Grate keep PostgreSQL transactions, constraints,
  and migration history visible and reviewable.
- ASP.NET Core supplies the required HTTP, OIDC, OpenAPI, SSE, health, worker,
  and observability integration surfaces without replacing domain policy.
- Provider, artifact, and evaluator boundaries remain replaceable; Python-native
  work can be introduced later without changing the application core.
- JSON Schema validation and canonicalization have explicit ownership and
  dependency boundaries instead of unresolved runtime-library selection.

### Costs and risks

- The repository maintains C#/.NET for backend code and TypeScript/Node.js for
  the SPA and browser tooling.
- The repository owns review and maintenance of a small vendored
  canonicalization source snapshot, including provenance and license records.
- JSON Schema and RFC 8785 compatibility still require focused evidence gates
  before the scaffold is accepted.
- Explicit SQL and Dapper require SQL literacy, mapping tests, and disciplined
  scoped repositories.
- The official OpenAI and AWS SDKs reduce integration work but do not prove
  OpenRouter, vLLM, or SeaweedFS compatibility.
- Project/module boundaries add some solution ceremony. Empty layers and
  premature service extraction remain prohibited.

## Traceability and downstream work

| Governing source | Stack realization | Evidence still required |
| --- | --- | --- |
| Authorization/isolation P0; ADR-002 and ADR-003 | Scoped module repositories, ASP.NET identity/context adapter, PostgreSQL constraints, audit/outbox, application-session store | Positive/negative resource-action and repository isolation matrix; multi-instance rotation/revocation |
| Resolved Session configuration P0; ADR-001 | Canonical JSON Schema contracts validated by `JsonSchema.Net`, C# DTO mappings, separate vendored JCS project, conformance fixtures | Exact package/source pins, drift, substitution, append/seal, and reconstruction fixtures |
| Assessment setup and Submission/Attempt P0; ADR-004 and ADR-005 | Npgsql/Dapper repositories, Grate SQL migrations, transactions, artifact ports | Migration gate, atomic fault injection, exact-version binding, upload and object-access tests |
| Text Session lifecycle and runtime contract | ASP.NET commands/SSE, worker composition root, durable work, provider port | Ordering, reconnect/replay, timeout, cancellation, late callback, and load evidence |
| Evidence/Evaluation contract | Versioned schemas, restricted worker adapter, immutable database records | Evidence seal, injection, sandbox/egress, retry/completion, and lineage tests |
| Review/Result/Release contract | Review-release module, strict DTO projections, atomic PostgreSQL Release boundary, React surfaces | Wrong-scope, stale/concurrent decision, pre-Release denial, atomic failure, correction, and visibility tests |
| UI/UX documentation gap | React/Vite SPA platform only | Approved journeys, interaction states, content, accessibility, responsive behavior, screenshots, and Playwright evidence |
| ADR-008 component and supply-chain gates | .NET OCI images, locked NuGet/frontend graphs, SBOM/scanning, provider and S3 adapters | Exact locks, compatibility/recovery evidence, and production-profile certification |

The next artifacts are:

1. a minimal .NET/React workspace scaffold and CI workflow implementing the
   dependency and supply-chain rules;
2. the canonical contract validation harness and separate
   `FlexAgent.CanonicalJson` project, including exact package/source pins,
   notices, and compatibility evidence;
3. the canonical contract package and ADR-001/ADR-004 conformance fixtures;
4. Grate migrations and the scoped PostgreSQL repository test harness for the
   first authorization/configuration vertical slice; and
5. approved UI/UX journeys before frontend feature implementation outruns the
   interaction specifications.

## Upstream references reviewed

- [.NET releases and support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)
- [What's new in ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0)
- [ASP.NET Core Server-Sent Events](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses#server-sent-events-sse)
- [ASP.NET Core OpenID Connect](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication)
- [System.Text.Json schema APIs](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.schema)
- [Npgsql documentation](https://www.npgsql.org/doc/)
- [Npgsql 10 release notes](https://www.npgsql.org/doc/release-notes/10.0.html)
- [Dapper repository](https://github.com/DapperLib/Dapper)
- [Grate documentation](https://grate-devs.github.io/grate/)
- [Grate configuration options](https://grate-devs.github.io/grate/configuration-options/)
- [OpenAI .NET SDK](https://github.com/openai/openai-dotnet)
- [AWS SDK for .NET S3 configuration](https://docs.aws.amazon.com/sdkfornet/v4/apidocs/items/S3/TS3Config.html)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/)
- [JSON Schema Draft 2020-12](https://json-schema.org/draft/2020-12)
- [JsonSchema.Net basics](https://docs.json-everything.net/schema/basics/)
- [JsonSchema.Net dialect selection](https://docs.json-everything.net/schema/examples/version-selection/)
- [RFC 8785: JSON Canonicalization Scheme](https://www.rfc-editor.org/rfc/rfc8785)
- [`cyberphone/json-canonicalization` reference repository](https://github.com/cyberphone/json-canonicalization)
- [Reference C# canonicalizer](https://github.com/cyberphone/json-canonicalization/tree/master/dotnet/jsoncanonicalizer)
- [Reference canonicalization test data](https://github.com/cyberphone/json-canonicalization/tree/master/testdata)
- [Reference implementation Apache-2.0 license](https://github.com/cyberphone/json-canonicalization/blob/master/LICENSE)
- [OpenAPI 3.1 specification](https://spec.openapis.org/oas/v3.1.0.html)
- [React versions](https://react.dev/versions)
- [Vite releases](https://vite.dev/releases)
- [xUnit v3 documentation](https://xunit.net/docs/getting-started/v3/getting-started)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [Playwright test documentation](https://playwright.dev/docs/writing-tests)
- [pnpm workspaces](https://pnpm.io/workspaces)

## Related

- [MVP architecture](../mvp-architecture.md)
- [ADR-001: resolved configuration representation and integrity](ADR-001-resolved-configuration-representation-and-integrity.md)
- [ADR-002: authorization enforcement and delegation](ADR-002-authorization-enforcement-and-delegation.md)
- [ADR-003: authorization audit persistence](ADR-003-authorization-audit-persistence.md)
- [ADR-004: assessment activation baseline and atomicity](ADR-004-assessment-activation-baseline-and-atomicity.md)
- [ADR-005: atomic Attempt start and Submission binding](ADR-005-atomic-attempt-start-and-submission-binding.md)
- [ADR-006: MVP architecture baseline](ADR-006-mvp-architecture-baseline-and-evolution.md)
- [ADR-007: OSS-first self-hostable deployment](ADR-007-oss-first-self-hostable-deployment.md)
- [ADR-008: bounded OSS component set](ADR-008-bounded-oss-component-set.md)
- [ADR-009: MVP detailed contracts](ADR-009-mvp-session-evaluation-review-contracts.md)
- [P0 feature specifications](../../requirements/README.md#p0-authoring-order)
- [MVP operational defaults](../../requirements/mvp-operational-defaults.md)
