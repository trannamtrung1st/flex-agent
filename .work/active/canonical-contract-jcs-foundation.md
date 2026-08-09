---
id: canonical-contract-jcs-foundation
status: in_progress
created: 2026-08-09
updated: 2026-08-10
---

# Goal

Implement ADR-010 downstream artifact 2: a fail-closed JSON Schema Draft
2020-12 validation harness and a separate, dependency-isolated
`FlexAgent.CanonicalJson` project backed by a reviewed exact-commit source
snapshot of the RFC-listed `cyberphone/json-canonicalization` C# reference
implementation.

The artifact must establish reproducible package/source provenance, strict
input handling, bounded canonicalization, official RFC/upstream compatibility
evidence, and enforceable project boundaries without introducing product
schemas or domain-specific digest procedures prematurely.

# Governing sources

- `AGENTS.md` — product invariants, specification-driven TDD, security/privacy,
  and implementation-workflow requirements
- `docs/product/concept-model.md` — immutable resolved configuration and
  resolved execution manifest concepts
- `docs/product/mvp-scope.md` — P0 scope and deferred capabilities
- `docs/architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md`
  — RFC 8785 plus lowercase SHA-256 procedures and historical-version rules
- `docs/architecture/decisions/ADR-007-oss-first-self-hostable-deployment.md`
  — source/license inventory and reproducible noninteractive build requirements
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — `STACK-DEC-5`, `STACK-DEC-7`, `STACK-DEC-16`, `STACK-DEC-17`, workspace
  direction, vendoring rules, and `GATE-STACK-SCHEMA`/`GATE-STACK-JCS`
- `docs/requirements/features/resolved-session-configuration.md` —
  `REQ-RSC-15`–`REQ-RSC-20` and affected acceptance criteria, as downstream
  traceability only; this artifact does not implement session resolution
- `.work/active/dotnet-react-workspace-scaffold.md` — prerequisite workspace
  evidence (completed; Implementation #10 green on `6cd7fc4`)

# Scope

## In

- Reconcile the prerequisite scaffold's latest CI status before using its
  runtime, module, and supply-chain evidence as this task's baseline.
- Select and centrally pin an exact stable `JsonSchema.Net` package version
  after license, support, advisory, and .NET 10 compatibility review; commit the
  resulting NuGet lock graph.
- Add a contract-test harness that always selects `Dialect.Draft202012`, rejects
  a missing or unexpected dialect, and fails closed on unexpected or
  unsupported keywords instead of relying on package defaults.
- Add the separate
  `src/BuildingBlocks/FlexAgent.CanonicalJson/FlexAgent.CanonicalJson.csproj`
  project with only .NET base-class-library dependencies and reviewed vendored
  source.
- Select and record an exact upstream commit, copy only the required C# source
  and official test vectors, preserve copyright/license headers, and add a
  complete `NOTICE.md` describing origin, commit, copied files, license, and
  local modifications.
- Provide a small application-owned wrapper around the isolated upstream code
  for strict UTF-8 object input, canonical UTF-8 output, and lowercase SHA-256
  output where requested by approved procedures.
- Reject duplicate properties, invalid UTF-8/Unicode including lone
  surrogates, non-finite numbers, negative zero, non-object top-level inputs,
  and inputs exceeding explicit byte, nesting-depth, property-count, or
  array-length limits.
- Return stable non-content-bearing failure categories; do not reproduce input
  documents in exceptions, logs, snapshots, or test output.
- Add architecture tests that enforce the canonicalization project's dependency
  boundary and prohibit dependencies from hosts, feature modules, browser code,
  databases, providers, or telemetry packages.
- Integrate focused tests and provenance/license checks into locked local and CI
  verification, update SBOM/license evidence, and document supported developer
  commands and partial gate status.

## Out

- Canonical product schemas for commands, events, resolved configuration,
  execution manifests, Evidence locators, audit events, error responses, SSE
  events, work items, or protected artifacts; these belong to ADR-010 artifact
  3.
- ADR-001, ADR-004, or Evidence-set domain normalization/builders and their
  cross-language conformance fixtures; artifact 2 establishes the engine and
  upstream/RFC evidence only.
- Session resolution, manifest append/seal persistence, authorization, HTTP
  request validation, OpenAPI projection, generated C#/TypeScript types, or
  feature-module integration.
- PostgreSQL, Grate, model-provider, object-store, authentication, frontend, or
  participant-facing behavior.
- Passing all of `GATE-STACK-SCHEMA` or `GATE-STACK-JCS`; representative product
  schemas and Flex Agent procedure fixtures remain required in the next
  sequenced artifact.
- Commits, pushes, pull requests, deployment, or publication.

# Plan

- [x] Reconcile the scaffold prerequisite: confirm the latest implementation CI
  result, rerun the focused local baseline, and update its retained task record
  without conflating its completion with this artifact.
- [x] Inspect the current stable `JsonSchema.Net` release and the upstream JCS
  repository; record exact package/source versions, provenance, license,
  copied-file inventory, official vectors, known errata, advisories, and the
  reviewed upgrade procedure before adding executable dependencies.
- [x] Define the smallest public boundaries for schema validation and canonical
  JSON, explicit resource-limit inputs, stable safe failure categories, and
  the allowed keyword compatibility profile; promote any new durable decision
  or production default to the governing architecture documentation before
  implementation.
- [x] Red: add focused failing tests for explicit Draft 2020-12 selection,
  dialect/keyword fail-closed behavior, project dependency isolation, official
  RFC/upstream canonical bytes, lowercase SHA-256, malformed Unicode/number
  rejection, duplicate keys, top-level shape, safe errors, and every resource
  limit.
- [x] Green: add the centrally pinned schema dependency, contract-test harness,
  isolated `FlexAgent.CanonicalJson` project, minimal reviewed upstream source,
  wrapper, provenance notice, license material, and solution/lock-file entries
  needed to satisfy the focused tests.
- [x] Refactor with the suite green: keep upstream code untouched and isolated,
  keep validation/limits/error mapping in the application-owned wrapper, remove
  unnecessary copied files, and make dependency direction executable through
  architecture tests.
- [x] Add tamper/provenance and reproducibility checks that fail when the
  upstream file inventory, recorded commit/hash, notice/license material,
  package lock, dialect selection, or supported-keyword profile drifts.
- [x] Integrate the new projects into locked .NET verification and CI; regenerate
  license/SBOM evidence and run focused, architecture, runtime, supply-chain,
  documentation, and clean-room regression checks.
- [>] Reconcile delivered evidence against the artifact-2 subset of
  `GATE-STACK-SCHEMA`, `GATE-STACK-JCS`, `GATE-STACK-MODULES`, and
  `GATE-STACK-SUPPLY`; document deferred artifact-3 evidence, recheck governing
  sources, and prepare the retained task record for independent review.

# Current state

Implementation complete locally. Pinned `JsonSchema.Net` `9.4.0` and upstream
JCS commit `19d51d7fe467d4706a3ff08adf8a748f29fc21e0`. Added
`FlexAgent.CanonicalJson`, contract/canonicalization test projects, minimal
`contracts/` fixtures, architecture boundary tests, lock files, toolchain pins,
and workspace gate documentation. All 42 .NET tests pass; supply-chain, docs,
and gitleaks checks pass locally. External code review on `790cfb8` remediated
locally (wrapper isolation, nested property limits, arrays vector upstream
test, expanded provenance manifest, per-component licenses, harness keyword
ordering). GitHub Actions confirmation for the remediation commit remains
pending.

# Decisions

- Keep artifact 2 bounded to reusable validation/canonicalization
  infrastructure and official RFC/upstream evidence. Product schemas and Flex
  Agent procedure fixtures remain artifact 3, matching ADR-010's approved
  sequence.
- Put the schema compatibility harness under the contract-test surface rather
  than creating a premature domain module. A reusable runtime validation
  package is introduced only when the first governed contract consumer needs
  it.
- Keep copied upstream implementation and vectors isolated and provenance-
  checked. Application-facing parsing, limits, safe errors, and digest helpers
  remain locally owned and reviewable.
- Treat schema/JCS inputs as untrusted and potentially sensitive even though
  this artifact uses synthetic fixtures only.
- Report gate coverage precisely as partial until artifact 3 supplies the
  representative canonical schemas and ADR-001/ADR-004/Evidence-set fixtures.

# Open questions / interim defaults

- **Resolved:** `JsonSchema.Net` `9.4.0` (.NET 10 support, Apache-2.0/MIT
  transitive graph reviewed via lock file and `verify-supply-chain.sh`).
- **Resolved:** upstream JCS commit `19d51d7fe467d4706a3ff08adf8a748f29fc21e0`
  (latest on `master` at implementation time; C# canonicalizer + ES6 number
  serializer sources only).
- Resource-limit production defaults remain unapproved. Limits are explicit
  caller inputs; tests use deliberately small values.

# Findings / deviations

- `JsonSchema.Net` `9.4.0` uses `BuildOptions.Dialect = Dialect.Draft202012`
  (not legacy `EvaluationOptions.EvaluateAs`). Package default dialect is
  preview `v1/2026`; harness always sets Draft 2020-12 explicitly.
- Keyword fail-closed enforcement is implemented by an application-owned keyword
  profile walker plus `Dialect.Draft202012.With([], allowUnknownKeywords:
  false)` at build time; package defaults alone do not reject unknown keywords.
- Official upstream `arrays.json` vector is excluded from wrapper vector tests
  because Flex Agent digest documents require top-level objects; upstream
  reference behavior is still covered separately.
- Review fix: array-length limits now use a per-array counter stack so nested
  object elements are counted correctly; negative-zero detection uses numeric
  sign instead of literal substring matching.
- Review fix (`790cfb8`): upstream canonicalizer compiled from an
  `internal`-visibility adapted copy; pristine verbatim snapshot retained under
  `Upstream/Pristine/` (excluded from compile). Architecture test asserts the
  complete assembly exported-type set.
- Review fix: `MaxObjectProperties` uses a per-object counter stack so nested
  objects cannot reset the parent property count.
- Review fix: `arrays.json` is exercised by a dedicated upstream-reference test
  (wrapper tests still skip top-level arrays per object-root policy).
- Review fix: `upstream-manifest.json` inventories pristine sources, compiled
  sources, and official vectors with SHA-256; provenance tests assert exact file
  set equality with disk.
- Review fix: `NOTICE.md` records per-component licenses (Apache-2.0, V8
  BSD-3-Clause, MPL-2.0) and documented local visibility modifications.
- Review fix: `SchemaKeywordProfile` checks `allowedKeywords` before structural
  recursion so removed keywords are not implicitly allowed.
- Review fix: `contracts/*` changes now trigger the Implementation workflow via
  `detect-implementation-changes.sh`; provenance files are copied into test
  output instead of relying on fragile relative source paths.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Prerequisite scaffold CI reconciliation | pass | Scaffold task records Implementation #10 green on `6cd7fc4`; local `verify-dotnet.sh` baseline green before implementation |
| Red-phase schema harness tests | pass | Contract harness tests added before implementation; failures observed for missing dialect, wrong dialect, unsupported keyword |
| Red-phase canonicalization and limit tests | pass | CanonicalJson tests added before implementation; failures observed for top-level array, duplicate keys, limits |
| Focused contract/JCS tests | pass | `dotnet test --solution FlexAgent.slnx -c Release` — 42/42 passed |
| Architecture dependency tests | pass | `CanonicalJsonBoundaryTests` — BCL-only references, hosts do not reference CanonicalJson, complete exported-type surface is wrapper-only |
| Official RFC/upstream vectors and provenance drift | pass | 5 object vectors byte-match via wrapper; `arrays.json` via upstream reference test; `ProvenanceTests` + expanded `upstream-manifest.json` |
| Malformed, boundary, and resource-limit cases | pass | `CanonicalJsonProcessorTests` |
| Safe failure-content scan | pass | Runtime synthetic secret markers absent from `CanonicalJsonException` and `SchemaCompatibilityException` messages |
| Locked full .NET/runtime regression | pass | `bash build/scripts/verify-dotnet.sh` |
| Supply-chain and license regression | pass | `bash build/scripts/verify-supply-chain.sh` |
| Gitleaks | pass | `gitleaks detect --source .` — no leaks found |
| Documentation validation | pass | `python3 scripts/check_docs.py` |
| Gate reconciliation | partial | `GATE-STACK-SCHEMA` and `GATE-STACK-JCS` partial (artifact 2); product schemas and ADR-001/ADR-004 fixtures deferred to artifact 3; CI rerun pending |

# Blockers

None locally. GitHub Actions confirmation for this artifact remains the
external-review gap.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [>] Remaining gaps or unverified behavior are recorded
- [>] Task state is safe and complete for external review
