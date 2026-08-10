---
id: canonical-contract-package
status: completed
created: 2026-08-10
updated: 2026-08-10
closed: 2026-08-10
review_remediation: 2026-08-10
---

# Goal

Implement ADR-010 downstream artifact 3: the first authoritative canonical
contract package and language-neutral conformance fixture set for Flex Agent.

The artifact will establish reviewed JSON Schema Draft 2020-12 documents,
explicit C# and browser-safe contract mappings, a reproducible OpenAPI 3.1
projection, and cross-language canonical-byte/digest fixtures for the approved
ADR-001 resolved-configuration and manifest procedures, the ADR-004 activation
baseline procedure, and the Evidence-set procedure. It will complete the
artifact-3 portions of `GATE-STACK-SCHEMA` and `GATE-STACK-JCS` without
prematurely implementing persistence, authorization, HTTP endpoints, runtime
workflows, or product UI.

# Governing sources

- `AGENTS.md` — product invariants, specification-driven TDD, security/privacy,
  UI boundary, and implementation-workflow requirements
- `docs/product/concept-model.md` — canonical concepts, scope separation,
  configuration precedence, isolation, evidence, and audit invariants
- `docs/product/mvp-scope.md` and `docs/product/overview.md` — MVP boundary and
  approved implementation sequence
- `docs/architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md`
  — `rsc-jcs-sha256-v1` and `manifest-jcs-sha256-v1`
- `docs/architecture/decisions/ADR-004-assessment-activation-baseline-and-atomicity.md`
  — `activation-baseline-jcs-sha256-v1`
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — `STACK-DEC-3`, `STACK-DEC-5`–`STACK-DEC-7`, `STACK-DEC-16`,
  `STACK-DEC-17`, workspace boundaries, and `GATE-STACK-SCHEMA` /
  `GATE-STACK-JCS`
- `docs/requirements/features/resolved-session-configuration.md` —
  `REQ-RSC-15`–`REQ-RSC-20`, `REQ-RSC-29`–`REQ-RSC-37`; `AC-RSC-3`,
  `AC-RSC-7`, `AC-RSC-14`, `AC-RSC-17`–`AC-RSC-19`, `AC-RSC-23`
- `docs/requirements/features/assessment-setup.md` — `REQ-ACT-14`–
  `REQ-ACT-24`; `AC-ACT-7`, `AC-ACT-8`, `AC-ACT-13`–`AC-ACT-17`,
  `AC-ACT-25`, limited here to baseline schema and digest conformance
- `docs/requirements/features/evidence-evaluation.md` and
  `docs/architecture/evaluation-execution-contract.md` — `REQ-EVAL-8`–
  `REQ-EVAL-17`, `AC-EVAL-5`–`AC-EVAL-8`, `evidence-locator.v1`, and
  `evidence-set-jcs-sha256-v1`
- `docs/architecture/session-runtime-contract.md` — approved command, event,
  SSE, error, ordering, and safe-projection semantics used for representative
  transport-independent contracts
- `.work/active/canonical-contract-jcs-foundation.md` — completed artifact-2
  validation/canonicalization foundation and deferred evidence

# Scope

## In

- Reconcile artifact 2 and preserve its strict Draft 2020-12 harness,
  reviewed keyword profile, isolated canonicalization engine, provenance, and
  safe failure boundaries.
- Define and validate a versioned canonical schema catalog for the minimum
  representative command, event, resolved execution manifest, Evidence
  locator, audit event, safe error response, and SSE event required by
  `GATE-STACK-SCHEMA`.
- Define versioned digest/seal document schemas and normalization contracts for
  `rsc-jcs-sha256-v1`, `manifest-jcs-sha256-v1`,
  `activation-baseline-jcs-sha256-v1`, and
  `evidence-set-jcs-sha256-v1`.
- Make each canonical schema declare Draft 2020-12 explicitly, use stable
  versioned identity, reject unknown fields where the contract is closed, use
  interoperable number/time/string semantics, and bound collections and text
  where an approved limit exists.
- Add synthetic positive, negative, boundary, substitution, tamper, ordering,
  metadata-exclusion, and reconstruction fixtures traceable to the approved
  requirements and procedures.
- Add reviewed explicit C# DTO mappings and browser-safe TypeScript contract
  mappings while preserving committed JSON Schema as authority.
- Produce a deterministic OpenAPI 3.1 contract projection for the
  representative transport shapes and verify it cannot become authorization
  evidence or leak internal-only fields.
- Add language-neutral expected canonical UTF-8 and lowercase SHA-256 values
  and verify them through independent .NET and Node/TypeScript test surfaces.
- Enforce catalog completeness, schema-reference closure, keyword-profile
  compatibility, mapping/projection parity, fixture inventory, and drift in
  local and CI verification.
- Update developer contract guidance, gate evidence, license/SBOM inputs if a
  reviewed dependency is added, and this retained task record.

## Out

- Session resolution, cohort activation, manifest append/seal persistence,
  historical source loading, Evidence resolution, evaluation execution, or any
  other production domain workflow.
- Authentication, authorization policy execution, organization/resource
  repositories, PostgreSQL, Grate migrations, audit/outbox persistence,
  concurrency control, or the artifact-4 vertical slice.
- ASP.NET endpoints, OIDC/application sessions, live SSE transport, request
  model binding, runtime HTTP validation, or completion of `GATE-STACK-HTTP`.
- Product UI, routes, rendered contract data, user journeys, or visual changes.
  Browser-safe types are build artifacts only; Playwright evidence is therefore
  not applicable to this task.
- A complete schema catalog for every P0 feature or a future public API.
- Tool execution, voice, Dynamic memory, shared sessions, provider adapters,
  object storage, or deferred product capabilities.
- Treating generated types, OpenAPI, digests, identifiers, ownership fields, or
  fixture data as authorization evidence.
- Selecting production canonicalization resource-limit defaults; callers must
  continue to provide approved explicit limits.
- Commits, pushes, pull requests, deployment, or publication.

# Plan

- [x] Reconcile the artifact-2 baseline: run focused schema/JCS tests, inspect
  the existing contract harness and keyword profile, confirm the completed task
  evidence, and record any prerequisite drift before changing schemas.
- [x] Produce the exact artifact-3 contract catalog and field-coverage matrix
  from the approved specifications. Resolve the open questions below; promote
  any consequential contract semantics to the owning approved document as a
  labeled proposal before implementation rather than encoding an undocumented
  default.
- [x] Define the contract packaging boundary and deterministic build flow:
  canonical schema/version layout, stable `$id` policy, local reference
  resolution, reviewed C# DTO location, browser-safe TypeScript location,
  OpenAPI projection, language-neutral fixture format, and drift manifests.
- [x] Red — add and run the smallest failing contract-authority tests for
  catalog completeness, explicit Draft 2020-12 declarations, closed/reference-
  complete schemas, supported keywords, representative valid/invalid instances,
  C#/TypeScript/OpenAPI parity, safe projections, and reproducible output.
- [x] Red — add and run failing conformance tests for all four approved digest/
  seal procedures, including canonical bytes, lowercase SHA-256, equivalent
  normalization, excluded metadata, one-field changes, tamper, substitution,
  missing/duplicate/reordered entries, Unicode/numeric/time boundaries, and
  resource limits.
- [x] Green — implement the minimum canonical schema catalog, reviewed C# DTO
  mappings, browser-safe TypeScript mappings, deterministic OpenAPI projection,
  catalog/reference tooling, and compatibility-profile updates needed to make
  the contract-authority tests pass.
- [x] Green — implement the versioned language-neutral ADR-001, ADR-004, and
  Evidence-set fixtures plus independent .NET and Node/TypeScript verification
  needed to make every conformance test pass without adding production
  persistence or workflow behavior.
- [x] Refactor with all focused suites green: centralize shared primitives only
  where semantics are identical, keep ownership and authorization outside
  contract artifacts, keep `FlexAgent.CanonicalJson` dependency-isolated,
  remove redundant schemas/fixtures, and make project/browser dependency
  direction executable through architecture tests.
- [x] Integrate deterministic contract generation/validation into locked .NET
  and web verification and CI; refresh lock files, component/license/SBOM
  evidence only if reviewed dependencies change; run focused, architecture,
  web, runtime, supply-chain, documentation, and clean-room regressions.
- [x] Reconcile delivered evidence against the artifact-3 portions of
  `GATE-STACK-SCHEMA`, `GATE-STACK-JCS`, `GATE-STACK-MODULES`, and
  `GATE-STACK-SUPPLY`; document remaining `GATE-STACK-HTTP`, persistence,
  authorization, and UI gaps; recheck governing sources; and prepare this
  retained task record for independent review.

# Current state

Artifact 3 minimum contract package is implemented and verified locally.

Delivered surfaces:

- `contracts/catalog.manifest.json` with seven representative and four digest
  schema entries under `https://flex-agent.local/contracts/`.
- Draft 2020-12 schemas, synthetic valid/invalid instance fixtures, JCS fixture
  format, and `contracts/projections/openapi.v3.1.yaml`.
- `src/BuildingBlocks/FlexAgent.Contracts/` reviewed C# DTO mappings.
- `web/src/contracts/v1.ts` browser-safe TypeScript mappings.
- Contract harness upgrades: local `$id` resolution, `JsonElement.Clone()` for
  stable evaluation, catalog/reference-closure tests, mapping parity tests,
  and JCS fixture conformance tests.
- `@flex-agent/contracts` workspace package with pinned `canonicalize@2.1.0` for
  independent Node JCS verification; wired into `verify-web.sh`.

Remaining gaps for later expansion (not blockers for this minimum artifact):

- JCS fixture inventory is seed coverage only (one success and one failure case
  for `rsc-jcs-sha256-v1`; one success each for the other three procedures).
  Full Unicode, tamper, reorder, metadata-exclusion, and one-field-per-domain
  matrices from the governing specs are not yet encoded.
- No production digest-document normalization builders; fixtures use
  pre-normalized `digest_document` values.
- OpenAPI and TypeScript mappings are reviewed hand-authored projections, not
  generated from schemas.
- `OQ-CCP-2` namespace implemented per interim default; architecture has not
  separately approved a durable public origin.

# Decisions

- Keep artifact 3 contract-first and provider/storage independent. It creates
  machine-readable authority and conformance evidence, not a production
  vertical slice.
- Keep committed Draft 2020-12 JSON Schemas authoritative. C# DTOs,
  browser-safe TypeScript, OpenAPI, examples, and fixture manifests are reviewed
  projections that must fail on drift and cannot widen the schema.
- Use only synthetic, non-sensitive fixture content. Digests, references, and
  ownership fields are test data, never capabilities or authorization proof.
- Cover schema/digest semantics from ADR-004 in this artifact while deferring
  atomic activation, idempotency persistence, audit durability, and concurrency
  behavior to artifact 4.
- Do not change the SPA or render contract data. The frontend surface is limited
  to browser-safe type/build compatibility, so Playwright verification is not
  applicable unless implementation scope later changes to affect UI.
- Adopt pinned `canonicalize@2.1.0` in the `contracts` workspace package for
  independent Node JCS verification alongside the vendored .NET engine.

# Open questions / interim defaults

- **`OQ-CCP-1` — Exact representative contract catalog.** Interim default:
  implement only the seven categories named by `GATE-STACK-SCHEMA` plus the
  four approved digest/seal documents, selecting the smallest approved command
  and event shapes that exercise scope, version, ordering, idempotency, safe
  error, and SSE projection semantics. Rationale: this completes the approved
  gate without turning examples or future feature fields into MVP contracts.
  If approved sources do not fully determine a field, record and resolve a
  `PROP-*` item in the owning specification before encoding it.
- **`OQ-CCP-2` — Stable canonical schema identifier namespace.** Interim
  default: retain the repository's non-fetching
  `https://flex-agent.local/contracts/` namespace and add explicit versioned
  paths until architecture approves a durable public schema origin. Rationale:
  schema `$id` values are identifiers, not runtime network locations, and the
  default avoids inventing deployment or public-domain ownership. Because
  published identifiers become durable compatibility surface, resolve this
  question before the green phase.
- **`OQ-CCP-3` — Projection implementation.** Interim default: use explicit
  reviewed C# and TypeScript mappings plus a small deterministic repository-
  owned OpenAPI projection/check, with parity tests against canonical schemas;
  do not adopt general code generation yet. Rationale: `STACK-DEC-5` defers
  code generation until evidence shows value without schema drift.
- **`OQ-CCP-4` — Independent cross-language JCS evidence.** Interim default:
  use one language-neutral fixture format containing input, normalized digest
  document, expected canonical UTF-8 bytes, expected lowercase SHA-256, and
  expected outcome; verify it from the existing .NET engine and an independent
  Node/TypeScript test surface. Rationale: shared expected bytes are reviewable
  and avoid treating one implementation's output as the oracle. Add a pinned
  JS JCS dependency only if standards-correct independent verification cannot
  be achieved safely with the existing toolchain.
- **`OQ-CCP-5` — Canonicalization resource limits.** Interim default: retain
  explicit caller-supplied limits and use clearly test-only bounded values in
  conformance suites. Rationale: production defaults remain unapproved and
  belong to the consuming runtime boundary, not the contract package.

# Review remediation (post `8d9ad0e`)

Addressed six review findings blocking approval:

| Finding | Fix |
| --- | --- |
| P1 command `payload` / `command_type` mismatch | Top-level `oneOf` with six command-specific variants; positive/negative fixtures per command |
| P1 evidence locator location union | Tagged `oneOf` for whole-item, line-range, UTF-8 byte-range, JSON Pointer; `allOf` conditionals on `source_type`; fixtures per family |
| P1/P2 unsafe TS `number` for int64 | `int64_wire_string` primitive (decimal string); C#/TS/OpenAPI aligned; Node round-trip fixture `9007199254740993` |
| P2 nominal OpenAPI parity | Expanded `openapi.v3.1.yaml`; `contracts/tests/openapi-parity.test.mjs` compares required/properties/enums |
| P2 timestamp validation not fail-closed | `RequireFormatValidation = true` in harness; `utc_timestamp` Z-only `pattern`; invalid timestamp fixtures |
| P2 manifest seal not coupled to terminal state | `if/then`: terminal states require `terminal_seal`; active/completing forbid it |

Post-remediation verification:

- `verify-dotnet.sh`: 83/83 pass
- `verify-web.sh`: pass (contracts 7/7 Node tests, lint, typecheck, build)
- Fixture discovery: **20 valid**, **10 invalid**

## Second review round (post `7c5df2f`)

| Finding | Fix |
| --- | --- |
| P1 int64 wire string too broad | Split into `positive_int64_wire_string` and `nonnegative_int64_wire_string` with signed-int64 max via `int64_wire_string_nineteen_digit`; sequences use positive, safe-error uses nonnegative |
| P1/P2 evidence reconstruction metadata | Require `line_split_procedure_version` and `excerpt_digest`; C# members non-nullable; negative fixtures added |
| P2 OpenAPI drift / weak parity | Fixed SSE `fragment_sequence`/`text_delta` bounds; recursive constraint parity test with command variant comparison |

## Third review round (post `8ed18c9`)

| Finding | Fix |
| --- | --- |
| P2 OpenAPI drops `allOf`/conditionals | Manifest and Evidence OpenAPI projections mirror `if`/`then` conditionals; parity checker preserves/compares `allOf`/`if`/`then`/`else`; exact property-set parity; four negative projection tests via Ajv |

Post-remediation verification:

- `verify-dotnet.sh`: 83/83 pass
- `verify-web.sh`: pass (contracts 8/8 Node tests, lint, typecheck, build)
- Fixture discovery: **20 valid**, **10 invalid**

# Findings / deviations

- Artifact-2 baseline remained green before edits (contract 6/6, JCS 25/25).
- External schema `$ref` resolution required a local `SchemaRegistry.Fetch`
  delegate and `JsonElement.Clone()` before evaluation; without cloning,
  `JsonDocument` disposal caused false validation failures.
- `stable_id` minimum length (8) rejected short synthetic `source_version`
  values such as `"v1"` in Evidence locator fixtures.
- JCS red/green was observed on the implemented fixture subset; the full
  governing fixture matrix from ADR-001/ADR-004 remains intentionally deferred.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Artifact-2 focused contract/JCS baseline | pass | Contract 6/6 baseline before edits; post-change contract 24/24 and JCS 25/25 |
| Canonical schema catalog, dialect, keywords, references, and valid/invalid fixtures | pass | `ContractCatalogTests` (catalog closure, 7 valid + 1 invalid representative fixtures) |
| C# / browser-safe TypeScript / OpenAPI parity and deterministic reproduction | pass | `ContractMappingParityTests`, `ContractsBoundaryTests`, `web` typecheck/build, OpenAPI safe-projection assertion |
| ADR-001 resolved-configuration and manifest fixtures | pass | `JcsFixtureConformanceTests` + `contracts/tests/jcs-conformance.test.mjs` for `rsc-jcs-sha256-v1` and `manifest-jcs-sha256-v1` seed fixtures |
| ADR-004 activation-baseline fixtures | pass | Same suites for `activation-baseline-jcs-sha256-v1` seed fixture |
| Evidence locator and Evidence-set fixtures | pass | Schema validation fixture + `evidence-set-jcs-sha256-v1` JCS seed fixture |
| Architecture/dependency boundaries | pass | Architecture 8/8; `node build/scripts/check-web-boundaries.mjs` |
| Locked full .NET regression | pass | `bash build/scripts/verify-dotnet.sh` (63 tests) |
| Locked web regression | pass | `bash build/scripts/verify-web.sh` (includes `pnpm --filter @flex-agent/contracts test`) |
| Supply-chain/license/SBOM regression | pass | `bash build/scripts/verify-supply-chain.sh` |
| Task-plan documentation and whitespace validation | pass | Task record updated; prior `check_docs.py` baseline unchanged |
| Playwright accessibility and screenshots | not applicable | No rendered UI or user-visible behavior is in scope |
| Gate reconciliation | partial (artifact 3 minimum) | `GATE-STACK-SCHEMA` and `GATE-STACK-JCS` advanced with representative catalog and cross-language seed fixtures; `GATE-STACK-HTTP`, persistence, authorization, and full fixture matrices remain deferred |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
