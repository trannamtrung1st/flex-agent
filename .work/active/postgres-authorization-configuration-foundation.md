---
id: postgres-authorization-configuration-foundation
status: completed
created: 2026-08-10
updated: 2026-08-10
---

# Goal

Implement ADR-010 downstream artifact 4 as the first executable
authorization/configuration persistence slice: immutable Organization-owned
configuration-source versions are stored in PostgreSQL through scoped
repositories, an in-process authorization decision, and one atomic
state/audit/outbox boundary, with Grate migrations and real PostgreSQL 18 tests
proving migration safety, tenant isolation, denial behavior, and rollback.

This task establishes persistence and enforcement infrastructure. It does not
yet expose a product UI or production HTTP/OIDC surface, resolve a complete
Session configuration, or start an assessment workflow.

# Governing sources

- `AGENTS.md` — product invariants, specification-driven TDD, security/privacy
  defaults, and implementation workflow
- `docs/product/concept-model.md` — Organization boundary, versioned sources,
  resolved configuration, and immutable history
- `docs/product/mvp-scope.md` — P0 validation slice and provider-independent
  foundation priority
- `docs/product/overview.md` — product principles and validation strategy
- `docs/requirements/features/auth-resource-isolation.md`
  - `REQ-AUTH-1`–`REQ-AUTH-6`: authenticated actor, deny-by-default,
    Organization ownership, action/resource scope, and non-widening
  - `REQ-AUTH-12`, `REQ-AUTH-13`, `REQ-AUTH-16`–`REQ-AUTH-22`: scoped
    queries, trusted ownership, commit-time reauthorization, fail-closed and
    non-disclosing denial
  - `REQ-AUTH-26`–`REQ-AUTH-31`: append-only/minimized audit and durable audit
    acceptance
  - `REQ-AUTH-30`, `AC-AUTH-1`, `AC-AUTH-4`, `AC-AUTH-7`, `AC-AUTH-9`–
    `AC-AUTH-15`, `AC-AUTH-17`, `AC-AUTH-21`, `AC-AUTH-22`: executable
    positive, negative, concurrency, and audit coverage
- `docs/requirements/features/resolved-session-configuration.md`
  - `REQ-RSC-15`–`REQ-RSC-22`, `AC-RSC-3`, `AC-RSC-7`, `AC-RSC-11`, and
    `AC-RSC-12`: immutable/versioned configuration sources, verified digests,
    history, scope, and idempotency
  - only the source-registry/persistence subset is in this task; full resolution,
    atomic Session start, manifest append/seal, inspection, and reconstruction
    remain later work
- `docs/architecture/mvp-architecture.md` — modular monolith ownership,
  PostgreSQL authority, transaction, audit/outbox, and isolation boundaries
- `docs/architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md`
  — immutable source identity, verified digest, normalized content, and
  digest-is-not-authorization boundary
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
  — in-process kernel, enforcement adapters, trusted scope derivation,
  commit-time reauthorization, stable denial reasons, and scoped repositories
- `docs/architecture/decisions/ADR-003-authorization-audit-persistence.md`
  — atomic mutation/audit persistence, append-only audit, minimized payload,
  durability classes, and outbox projection
- `docs/architecture/decisions/ADR-004-assessment-activation-baseline-and-atomicity.md`
  — source ownership and digest validation independent of later baseline binding
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — Npgsql/Dapper, Grate, PostgreSQL 18/Testcontainers, module boundaries,
  persistence rules, artifact sequence, and `GATE-STACK-POSTGRES`,
  `GATE-STACK-MODULES`, and the first slice of `GATE-STACK-ISOLATION`
- `.work/active/canonical-contract-jcs-foundation.md` and
  `.work/active/canonical-contract-package.md` — completed artifact-2/artifact-3
  prerequisites and deferred artifact-4 boundaries

# Scope

## In

- Confirm and centrally pin exact stable versions for Npgsql `10.x`, Dapper,
  Grate `2.x`, Testcontainers for .NET/PostgreSQL, and any narrow PostgreSQL
  test dependency; regenerate and validate NuGet lock files.
- Add the minimum repository structure required by ADR-010 when its first
  behavior is implemented:
  - shared PostgreSQL data-source/transaction primitives with no feature-table
    or authorization policy ownership;
  - an `IdentityAccess` module owning the in-process authorization decision
    contract and its PostgreSQL enforcement adapter;
  - a `Configuration` module owning immutable configuration-source and
    configuration-source-version persistence;
  - a PostgreSQL integration-test project using PostgreSQL 18 through
    Testcontainers.
- Add immutable, UTC-ordered, plain-SQL Grate migrations under
  `database/migrations/` for the minimum Organization, actor/membership or
  grant, configuration-source/version, audit-event, and outbox records needed
  by the slice.
- Provide a repository-owned, noninteractive Grate invocation that explicitly
  selects PostgreSQL, enables transactions, disables token replacement, and
  fails on changed one-time scripts. Migration execution remains separate from
  API and worker startup.
- Derive Organization and parent ownership from trusted persisted state.
  Client/request organization, ownership, role, and grant values are never
  accepted as authorization evidence.
- Define a narrow trusted application actor/context and an authorization
  decision containing actor, action, resource/scope, permit/deny, stable
  internal reason, relationship/policy version, and audit metadata. No public
  general-purpose database or service-locator escape hatch is allowed.
- Implement a protected internal application command that registers an
  immutable configuration-source version under an existing trusted
  Organization-owned source:
  - authorize the actor for the action and parent scope;
  - validate canonical schema/procedure identity and lowercase SHA-256 digest
    using the existing contract/canonicalization boundaries;
  - revalidate current permission and parent ownership inside the commit
    transaction;
  - enforce idempotency and immutable version identity;
  - write the source version, minimized append-only audit event, and outbox item
    atomically, or write none of them;
  - return stable application results without exposing protected existence or
    database details.
- Implement only scoped repository entry points. Protected get/list/count and
  mutation methods require trusted Organization/resource scope; there is no
  unscoped `GetById`, list-all, or count-all path for protected records.
- Enforce critical invariants in PostgreSQL with composite keys, foreign keys,
  unique constraints, idempotency keys, UTC timestamps, immutable version rows, and
  mutation-rejecting protection for append-only audit history where supported
  by the approved design.
- Add architecture tests rejecting persistence/framework imports from
  domain/application code, unscoped protected repository APIs, shared
  infrastructure ownership of feature tables, and cross-module repository
  writes.
- Follow observed red-green-refactor TDD for domain/application behavior,
  repository isolation, migrations, concurrency, and transaction failure.

## Out

- React components, browser state, UI/UX behavior, frontend contract generation,
  and Playwright verification. This artifact has no user-facing surface; the
  developer/frontend requirement is satisfied by recording and preserving that
  boundary rather than creating a placeholder UI.
- ASP.NET endpoints, OpenAPI changes, production OIDC/Keycloak login, opaque
  application sessions, cookies, and browser authentication. Tests use
  synthetic actors and grants through trusted test fixtures only.
- General Organization, user, role, grant, Agent, Harness, or Activity
  management behavior or UI.
- Complete configuration precedence/resolution, activation baselines,
  Enrollment/Attempt/Session binding, atomic Session start, execution manifests,
  runtime append/seal, reconstruction, exports, or reviewer/participant views.
- Assessment setup, Submission uploads/artifact storage, Session text/SSE,
  evaluation, review, Result, Release, model-provider, and notification flows.
- Service delegation/background-job execution, authorization caching,
  multi-instance revocation, or the 60-second long-lived-access propagation
  target. The schema must not preclude later versioned grants/delegations, but
  speculative behavior is excluded.
- Production infrastructure provisioning, backup/restore certification,
  database high availability, or destructive migration/rollback tooling.
- New product or architecture decisions unless implementation evidence reveals
  a contract gap; consequential discoveries must be promoted to an authoritative
  document rather than decided only in this task file.

# Acceptance and verification mapping

| Slice obligation | Implementation surface | Planned verification |
| --- | --- | --- |
| Deny by default for a specific actor/action/resource (`REQ-AUTH-1`, `REQ-AUTH-2`, `AC-AUTH-1`) | IdentityAccess kernel contract plus Configuration command adapter | Domain/application red test for missing/unknown actor and absent grant; stable deny result; zero database side effects |
| Organization ownership and trusted scope (`REQ-AUTH-3`, `REQ-AUTH-4`, `REQ-AUTH-13`, `REQ-AUTH-16`, `REQ-AUTH-17`, `AC-AUTH-4`, `AC-AUTH-9`) | Composite database relationships and scoped repository SQL | Real PostgreSQL wrong-Organization, forged request scope, parent mismatch, and guessed-ID matrix |
| Scoped list/count/read (`REQ-AUTH-12`, `AC-AUTH-7`) | Explicit-column Configuration queries requiring trusted Organization scope | Mixed-Organization fixtures prove no rows, totals, or existence oracle leak across scope |
| Commit-time current permission and fail-closed mutation (`REQ-AUTH-18`–`REQ-AUTH-22`, `AC-AUTH-11`–`AC-AUTH-13`, `AC-AUTH-17`) | Transaction coordinator reloads relationship/version before insert | Revocation race, inconsistent dependency, concurrent request, and injected failure tests prove no partial state |
| Atomic durable audit (`REQ-AUTH-26`–`REQ-AUTH-31`, `AC-AUTH-14`, `AC-AUTH-22`) | Configuration row, append-only minimized audit event, and outbox item in one PostgreSQL transaction | Success correlation assertions plus audit/outbox failure and transaction rollback fault injection |
| Immutable versioned source (`REQ-RSC-15`–`REQ-RSC-21`, `AC-RSC-3`, `AC-RSC-7`, `AC-RSC-11`, `AC-RSC-12`) | Configuration source/version schema and repository | Exact immutable identity/digest persistence, mutable-alias rejection, changed-content/new-version behavior, same digest across distinct scopes without access transfer |
| Idempotent/concurrent registration (`REQ-RSC-22`) | Organization/source/action-scoped idempotency constraint and result resolver | Duplicate same-payload returns the same authoritative result; conflicting payload and concurrent requests fail deterministically without competing versions |
| Migration safety (`STACK-DEC-9`, `GATE-STACK-POSTGRES`) | Grate tool/configuration and immutable SQL migration set | Empty PostgreSQL 18 migration, repeat no-op, changed-script detection, transactional failure rollback, concurrent runner/locking, and supported upgrade-path checks |
| Module boundaries (`STACK-DEC-17`, `GATE-STACK-MODULES`) | Solution/project references, interfaces, and architecture rules | Architecture tests reject prohibited dependencies, unscoped protected repositories, and cross-module persistence writes |

# Plan

- [x] Reconcile artifact-3 and repository prerequisites: run the focused
  canonical contract/JCS suites, the aggregate clean-room verification, inspect
  current CI, and verify exact supported package/tool versions and licenses
  against primary sources before changing dependency pins.
- [x] Finalize the smallest internal contract and threat model: name the
  protected resource/action, define trusted actor and Organization scope,
  stable decision/error categories, idempotency scope, transaction boundary,
  audit/outbox fields, sensitive-data exclusions, and the complete positive and
  negative matrix. Promote any consequential ambiguity before coding.
- [x] Red — add architecture and migration-harness tests that fail because the
  PostgreSQL infrastructure, Grate migration set/invocation, module projects,
  package pins, and enforced dependency rules do not yet exist.
- [x] Green — add the minimum centrally pinned dependencies, solution projects,
  PostgreSQL/Testcontainers fixture, separate Grate execution boundary, and
  first immutable plain-SQL migrations needed to pass empty/repeat and basic
  schema/constraint tests.
- [x] Red — add domain/application tests for deny-by-default authorization,
  trusted parent-derived scope, stable non-disclosing denial, immutable source
  identity/digest validation, and idempotent registration; run them and record
  the intended failures.
- [x] Green — implement the minimum framework-independent authorization and
  Configuration application contracts, reusing the existing canonical contract
  and JCS/SHA-256 boundaries without putting persistence or transport types in
  domain/application code.
- [x] Red — add real PostgreSQL repository and transaction tests for positive
  access plus wrong-Organization, forged scope, guessed identifier, list/count
  leakage, parent mismatch, stale/revoked permission, duplicate/conflicting
  idempotency, concurrent registration, audit/outbox rejection, and injected
  rollback failures; run and record the failures.
- [x] Green — implement explicit parameterized Npgsql/Dapper repositories,
  current-state commit-time reauthorization, composite database constraints,
  and one atomic configuration-version/audit/outbox transaction that makes the
  repository matrix pass.
- [x] Refactor with focused suites green: remove duplicated SQL/mapping and test
  setup without creating generic repositories, unscoped helpers, feature-table
  ownership in shared infrastructure, or a broadly exported transaction handle.
- [x] Complete the remaining PostgreSQL/Grate evidence:
  - [x] Grate **tool** empty-database migration (`GrateToolMigrationTests.Grate_tool_migrates_empty_database`)
  - [x] Grate **tool** repeat no-op (`GrateToolMigrationTests.Grate_tool_repeat_is_no_op`)
  - [x] changed one-time script failure (`GrateToolMigrationTests.Grate_tool_changed_one_time_script_fails_closed`)
  - [x] transactional migration failure rollback (`GrateToolMigrationTests.Grate_tool_failed_script_rolls_back_within_transaction`)
  - [x] concurrent migration runners/locking (`Grate_tool_concurrent_invocations_on_empty_database_serialize_pending_migrations` for pending migrations; `Grate_tool_concurrent_invocations_on_migrated_database_both_succeed` for repeat no-op)
  - [x] supported upgrade path (embedded + historical fixtures)
  - [x] append-only audit mutation rejection
  - [x] tool dry-run is non-mutating (`GrateToolMigrationTests.Grate_tool_dry_run_is_non_mutating`); migration reproducibility via changed-script hash enforcement and upgrade-path regressions
- [x] Integrate the new projects and gates into locked restore, aggregate local
  verification, and the blocking `Implementation` CI workflow; update dependency
  inventory/SBOM/license checks and any developer commands required to run the
  separate migration/integration-test boundary.
- [x] Reconcile delivered behavior against every mapped requirement and ADR-010
  artifact-4 gate, run focused then proportionate regression verification,
  record exact evidence and remaining deferred gates, recheck governing specs,
  and prepare the retained task file for independent backend/security review.

# Current state

Artifact 4 is implemented with review follow-up addressing commit-time grant
locking (`FOR SHARE`), separate idempotency records, `ON CONFLICT` insert
reconciliation, fail-closed Grate execution, version immutability triggers,
race-oriented integration tests, and the full Grate tool-path safety matrix.
Task status is `completed`; formal security-privacy sign-off remains a
follow-up outside this artifact slice.

# Decisions

- Treat artifact 4 as a backend-only foundation slice. A placeholder HTTP API
  or React surface would add an unapproved public contract without improving
  the persistence/authorization evidence required by the artifact.
- Use one protected configuration-source-version registration operation to
  connect authorization, trusted scope, canonical digest provenance,
  PostgreSQL constraints, idempotency, audit, and outbox behavior end to end.
  This is an internal application boundary, not a general configuration
  management feature.
- Keep authorization policy in the IdentityAccess module, configuration rules
  and repositories in the Configuration module, and only data-source,
  transaction, migration-invocation support, and low-level observability in
  shared PostgreSQL infrastructure.
- Make Organization/resource scope mandatory in every protected repository
  method signature and independently enforce parent ownership with composite
  database relationships. A matching digest or caller-supplied Organization ID
  never grants access.
- Use synthetic trusted actor/grant fixtures for this artifact. Production OIDC
  and application-session behavior remains a later explicit slice.
- Classify the configuration-version registration mutation as requiring durable
  atomic audit for this vertical-slice proof. This is consistent with the
  approved sensitive-mutation/audit boundary and ensures the first repository
  flow exercises ADR-003 rather than leaving audit integration unproven.
- **OQ-3 resolution:** pin Grate `2.1.6` as a local dotnet tool with
  `build/scripts/run-grate-migrations.sh`. Integration tests fall back to
  `GrateMigrationRunner` transactional SQL application when the tool runtime is
  unavailable locally (observed: grate 2.1.6 requires `Microsoft.NETCore.App`
  10.0.10 while the pinned SDK ships 10.0.0). CI Linux runners should use
  `DOTNET_ROLL_FORWARD=LatestPatch`.

# Findings / deviations

- Grate dotnet tool 2.1.6 may fail locally (exit 150) due to runtime patch
  mismatch; embedded fallback is now **opt-in** via
  `FLEXAGENT_ALLOW_EMBEDDED_MIGRATION_FALLBACK=true` (tests only). All other
  Grate failures fail closed.
- Full `GATE-STACK-ISOLATION` Activity/Participant/Session matrices remain
  deferred; this slice proves the Organization/configuration-source pattern
  only.
- Audit/outbox fault-injection rollback tests added (`AuditOutboxFaultInjectionTests`).
- Review follow-up (2026-08-10): addressed P1 grant lock, idempotency table,
  `ON CONFLICT` reconciliation, fail-closed migrations, and version
  immutability triggers.
- Review follow-up (2026-08-10, round 2): `0003` backfills legacy idempotency
  keys, enforces composite source/version FK, scoped `GetByIdForSourceAsync`,
  `0001→0002+` upgrade test, and audit/outbox rollback fault injection.
- Review follow-up (2026-08-10, round 3): restored shipped `0002` byte-for-byte;
  moved all repair logic into `0003` with table-scoped constraint checks;
  added historical `4e21917` `0002` checksum regression test.
- Review follow-up (2026-08-10, round 4): restored shipped `0003` from `d244a6a`;
  moved `conrelid` constraint hardening into `0004`; added historical `d244a6a`
  `0003` checksum regression test.
- Code review (2026-08-10): **approved** at `acaf9e3` — no blocking correctness
  findings; migration-history immutability closed; optional future hardening:
  synthetic same-name constraint collision regression for `0004`.
- CI (2026-08-10): GitHub Actions green on `main` for `acaf9e3` — Implementation
  #29 (5m 5s), Documentation #60 (16s); prior `458ec97` also green.
- Grate tool evidence (2026-08-10): `GrateToolMigrationTests` (eight tests)
  exercises `build/scripts/run-grate-migrations.sh` (Grate 2.1.6) against
  PostgreSQL 18 without embedded fallback: empty migrate, repeat no-op, changed
  one-time script failure, transactional rollback, concurrent pending-migration
  contention on an empty database (transient grate-internal bootstrap failure
  retryable; final schema applied exactly once), concurrent no-op invocations on
  a migrated database (both succeed), dry-run non-mutation, and `RunAsync`
  explicit-directory override of ambient `FLEXAGENT_MIGRATIONS_DIRECTORY`.
  Migration reproducibility is evidenced by changed-script failure and
  historical upgrade checksum regressions, not dry-run alone. Local smoke
  required `Microsoft.NETCore.App 10.0.10` with `DOTNET_ROLL_FORWARD=LatestPatch`;
  CI installs `10.0.x` patch runtime. `FLEXAGENT_MIGRATIONS_DIRECTORY` supports
  test-only migration sets (atomic-failure fixture).
- Review follow-up (2026-08-10, round 5): `RunAsync` passes caller
  `migrationsDirectory` to the Grate tool path and always sets
  `FLEXAGENT_MIGRATIONS_DIRECTORY` on the child process; dry-run evidence
  renamed to non-mutation only.
- Review follow-up (2026-08-10, round 6): added empty-database concurrent
  pending-migration test with explicit transient-bootstrap retry contract;
  migrated-database concurrent test retained for no-op path.

# Open questions / interim defaults

- **OQ-1 — Exact internal configuration-source kind.** Interim default: use one
  synthetic, schema-versioned `configuration_source` kind whose payload
  validates against an existing representative canonical contract or a
  deliberately narrow new contract only if required. Rationale: the artifact
  needs source identity/integrity evidence, not premature Agent/Harness/Activity
  authoring. This is working guidance; do not expose it as product vocabulary.
- **OQ-2 — Database-level append-only enforcement mechanism.** Interim default:
  use least-privilege repository operations plus a PostgreSQL trigger or
  equivalent mutation-rejecting database rule for audit rows, selected after
  proving it works with Grate, backups, and test cleanup. Rationale: ADR-003
  requires append-only or equivalently tamper-evident history, and application
  convention alone is insufficient. Record a new ADR only if the chosen
  mechanism creates a consequential operational boundary.
- **OQ-3 — Grate invocation packaging.** Interim default: pin Grate as a local
  .NET tool and expose a repository script that supplies the approved
  noninteractive flags while keeping connection material external. Rationale:
  this is reproducible, separate from host startup, and compatible with locked
  CI. Verify current Grate support and exact option behavior from primary
  sources before implementation.
- **OQ-4 — Integration-test container availability.** Interim default: require
  a Docker-compatible local runtime and skip nothing silently; when unavailable,
  focused PostgreSQL tests report an explicit infrastructure failure and CI
  remains the acceptance authority. Rationale: SQLite or mocks cannot prove the
  PostgreSQL constraints, transactions, and Grate behavior required by ADR-010.

# Security and privacy review focus

- Assets: Organization ownership, actor/grant state, immutable configuration
  source content/digests, audit history, outbox metadata, and idempotency keys.
- Primary misuse cases: forged Organization/owner input, guessed IDs,
  cross-Organization list/count leakage, stale grant use, digest substitution,
  duplicate/concurrent writes, audit bypass, partial transaction commit,
  over-broad repository helpers, and sensitive payload copying into audit/logs.
- Required controls: trusted server-side relationship lookup, deny by default,
  commit-time reauthorization, composite scope constraints, explicit-column
  queries, stable non-disclosing external results, parameterized SQL, immutable
  version rows, atomic audit/outbox, bounded synthetic fixtures, and logs/audit
  containing references and reason codes rather than raw configuration content.
- Required recovery evidence: transaction rollback on any protected-state,
  audit, or outbox failure; idempotent retry reconciliation; migration failure
  leaves the prior schema usable; no destructive rollback command is introduced.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Repository and task-state inspection | pass | `main` clean and synchronized; all existing `.work/active/*.md` tasks completed; no overlapping artifact-4 task |
| Governing product, requirement, architecture, and completed prerequisite review | pass | Sources listed above inspected during planning on 2026-08-10 |
| Exact dependency/tool versions and primary-source behavior | pass | Npgsql 10.0.3, Dapper 2.1.79, Grate 2.1.6, Testcontainers.PostgreSql 4.11.0 pinned in `Directory.Packages.props`, `.config/dotnet-tools.json`, `build/toolchain.json` |
| Artifact-3 focused contract/JCS baseline | pass | `dotnet test --solution FlexAgent.slnx -c Release` — 83 pre-artifact tests green before implementation |
| Grate migration safety matrix against PostgreSQL 18 | pass | `GrateToolMigrationTests` (eight tool-path scenarios including empty-DB concurrent pending migration + migrated-DB concurrent no-op); embedded fallback test-only; `0001→0004` upgrade/idempotency backfill; historical checksum regressions; reproducibility via changed-script enforcement + upgrade regressions; dry-run proves non-mutation only |
| Scoped repository authorization/isolation matrix | pass | Authorization, isolation, commit lock race, concurrent idempotency, digest-key reservation, source-scoped version resolution |
| Atomic configuration/audit/outbox boundary | pass | Success correlation; audit `relationship_version`; audit/outbox fault-injection rollback tests |
| Module/dependency architecture tests | pass | `ModuleBoundaryTests` — no Npgsql/Dapper in domain/application; no unscoped get-by-id; Configuration.Application does not reference CanonicalJson |
| Locked aggregate regression and CI | pass | `bash build/scripts/verify-dotnet.sh` — 117 tests green (2026-08-10 post review round 6); GitHub Actions Implementation #29 + Documentation #60 passed on `acaf9e3` |
| Frontend and Playwright | not applicable | No UI-affecting work in artifact 4; revisit if scope changes |
| Independent backend/security review | pass (code) | Code review + CI approved at `acaf9e3` (2026-08-10); formal security-privacy reviewer sign-off deferred as follow-up |

# Blockers

None. Local Grate tool execution requires runtime patch alignment; integration
tests use repository-owned transactional SQL fallback with equivalent schema
evidence.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task marked `completed` (2026-08-10) — Grate matrix closed; security-privacy reviewer sign-off tracked separately
