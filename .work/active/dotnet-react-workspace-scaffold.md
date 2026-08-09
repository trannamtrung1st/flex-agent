---
id: dotnet-react-workspace-scaffold
status: in-progress
created: 2026-08-09
updated: 2026-08-09
---

# Goal

Create the first executable Flex Agent implementation artifact: a minimal,
reproducible .NET 10 API/worker and React/Vite SPA workspace with enforceable
module boundaries, locked dependencies, portable OCI builds, and CI evidence
for the applicable runtime, supply-chain, and operability gates.

# Governing sources

- `AGENTS.md` — repository invariants, specification-driven TDD, security,
  Playwright, and implementation-workflow rules
- `docs/product/overview.md#what-to-do-next` — start the provider-independent
  foundation and canonical-contract sequence
- `docs/product/concept-model.md` and `docs/product/mvp-scope.md` — product
  boundaries and MVP exclusions
- `docs/architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md`
  — modular-monolith SPA/API/worker topology
- `docs/architecture/decisions/ADR-007-oss-first-self-hostable-deployment.md`
  — portable OCI, noninteractive build, component/license, and SBOM expectations
- `docs/architecture/decisions/ADR-008-bounded-oss-component-set.md`
  — exact component pins, immutable digests, supply-chain evidence, and no
  floating released-profile dependencies
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — `STACK-DEC-1`–`STACK-DEC-17`, approved workspace direction, dependency
  rules, and `GATE-STACK-*` evidence
- `docs/architecture/mvp-architecture.md#implementation-readiness` — scaffold
  acceptance remains gated by later schema, JCS, HTTP, and PostgreSQL/Grate
  artifacts as well as this foundation
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md` — authority, tokens, and
  accessibility requirements for the minimal runnable SPA surface

# Scope

## In

- Pin supported .NET 10, Node.js, pnpm, React 19.2.x, Vite 8.1.x, TypeScript,
  test, analyzer, build, and base-image inputs to exact reviewed versions.
- Add the repository solution, central .NET build/package policy, committed
  NuGet lock files, pnpm workspace metadata, and committed pnpm lock file.
- Add only code-bearing workspace paths: minimal API and worker composition
  roots, the SPA, architecture/runtime tests, and deployment build inputs.
- Provide bounded liveness/readiness and graceful-shutdown behavior for the API
  and worker without introducing feature/domain policy or durable work claims.
- Add a strict-TypeScript React/Vite SPA using mapped design tokens, system
  font fallbacks, semantic structure, dark/light support, and no product
  capability beyond a clearly labeled development smoke surface.
- Add separate multi-stage OCI builds for API, worker, and static SPA/gateway;
  keep SDKs, development dependencies, migration authority, and credentials out
  of runtime images.
- Add CI for locked restore/install, lint/build/test/publish, architecture
  checks, OCI multi-architecture builds, license inventory, SBOM generation,
  vulnerability/secret scanning, and documentation validation.
- Add concise contributor commands and update implementation-maturity wording
  once executable evidence supports it.

## Out

- Feature modules, P0 API behavior, authentication/OIDC, authorization policy,
  application sessions, SSE, persistence, migrations, durable work, providers,
  artifact storage, and telemetry adapters.
- Canonical schemas, `JsonSchema.Net` validation, the
  `FlexAgent.CanonicalJson` vendored project, and ADR-001/ADR-004 conformance
  fixtures; these are the next sequenced artifact.
- Completing `GATE-STACK-SCHEMA`, `GATE-STACK-JCS`, `GATE-STACK-HTTP`,
  `GATE-STACK-POSTGRES`, `GATE-STACK-ISOLATION`, `GATE-STACK-PROVIDERS`,
  `GATE-STACK-ARTIFACTS`, `GATE-STACK-SESSION`, or the authenticated
  `GATE-STACK-BROWSER` journey.
- Product navigation, Campaign/Session/review journeys, reusable UI component
  implementation, self-hosted font artifacts, real credentials, and real
  Participant data.
- Deployment, release publication, commits, pushes, or pull requests.

# Plan

- [x] Resolve and record exact supported patch versions, package sources,
  action/image digests, licenses, and CI scanner/SBOM tooling; fail closed on an
  unsupported, prerelease, floating, or unreviewed input.
- [x] Add the smallest executable checks first where meaningful: architecture
  dependency rules, API/worker startup-readiness-shutdown behavior, and SPA
  accessibility/build smoke checks; record the expected initial failures.
  Treat pure bootstrap manifests as a test-first exception and validate them
  through clean locked restores and builds.
- [x] Create `FlexAgent.slnx`, `global.json`, central .NET build/package policy,
  minimal API and worker composition roots, and focused test projects without
  generating empty feature/layer projects.
- [x] Create the root pnpm workspace and strict React/Vite SPA with a minimal
  semantic smoke surface, design-token mappings for both themes, lint/type/unit
  checks, and no backend or product-authority leakage into browser code.
- [x] Add separate pinned multi-stage OCI definitions for API, worker, and the
  static SPA/gateway; prove non-root/minimal runtime contents, health behavior,
  graceful termination, and absence of embedded credentials or source-only
  assets.
- [x] Add a dedicated implementation CI workflow that preserves the existing
  documentation workflow and enforces locked clean builds, tests, architecture
  rules, multi-architecture publish/build, license inventory, SBOM,
  vulnerability/secret scans, and reproducibility/drift failures.
- [x] Run focused checks and then the clean-room aggregate verification; use the
  project Playwright MCP server for accessibility snapshots and desktop/narrow
  screenshots of the runnable SPA smoke surface, without claiming the later
  authenticated browser gate.
- [x] **Remediation pass (fourth review):** close supply-chain, reproducibility,
  shutdown-evidence, license-inventory, and CI-path gaps (code on `1d16fb1`).
- [ ] Reconcile delivered evidence against `GATE-STACK-RUNTIME`,
  `GATE-STACK-MODULES`, `GATE-STACK-SUPPLY`, and
  `GATE-STACK-OPERABILITY`; confirm Implementation CI green on remediation
  commit; then mark this task completed and retain it as implementation history.

# Remediation pass (ordered)

1. **[P1] OCI supply-chain evidence** — remove runtime `apt-get`/`apk add curl`
   and Docker `HEALTHCHECK`; probe endpoints externally from the host/orchestrator;
   generate and scan SBOMs for final API/worker/SPA images (not only publish
   output and npm graph). ASP.NET runtime OCI base promoted to `10.0.8-noble`.
2. **[P1] Immutable docs CI bootstrap** — pin `docs.yml` actions to commit SHAs,
   record them in `build/toolchain.json`, and add `permissions: contents: read`
   to both `docs.yml` and `implementation.yml`.
3. **[P2] Fail-closed .NET SDK/language pins** — `global.json`
   `rollForward: disable`; `Directory.Build.props` `LangVersion` `14.0`.
4. **[P2] Graceful shutdown evidence** — API host disposal test; OCI
   `docker stop` (SIGTERM) checks; no `docker rm -f` as shutdown proof.
5. **[P2] NuGet license inventory** — `packages.lock.json` + restored `.nuspec`
   `<license>`/`<licenseUrl>`/license-file metadata via `generate-nuget-licenses.sh`;
   project/repository URLs recorded as provenance only.
6. **[P2] CI path filtering** — `changes` detection job gates expensive
   Implementation jobs; duplicate `docs` job removed from Implementation.

# Current state

Remediation implementation is on `main` at `1d16fb1` (R4-1–R4-6 substantively
addressed). Fifth-review cleanup tightens NuGet license validation and OCI
failure cleanup; task remains **`in-progress`** until Implementation workflow
**#7** is green on the remediation commit.

Local verification (2026-08-09): `verify-dotnet.sh` (9/9), `verify-web.sh`,
`verify-supply-chain.sh`, `verify-oci.sh`, and `check_docs.py` pass before this
cleanup commit.

# Decisions

- Deliver ADR-010 downstream artifact 1 as a bounded task. Completion means the
  workspace/CI foundation and its applicable evidence exist; it does not mean
  the architecture's overall "scaffold acceptance" gate has passed.
- Create directories/projects only for executable code or verification in this
  task. Do not pre-generate empty `Modules`, `contracts`, or `database` layers.
- Keep the existing documentation workflow independent and add a separate
  implementation workflow so failures remain attributable and the docs-only
  path stays fast.
- Keep the SPA smoke surface intentionally non-product. Use approved semantic
  tokens and system font fallbacks; defer approved self-hosted fonts until their
  artifacts and licenses are verified with product UI realization.
- CI may build and inspect artifacts but must not publish images, deploy an
  environment, or use real credentials in this task.
- Use `NetArchTest.Rules` plus a browser import-boundary script for the initial
  `GATE-STACK-MODULES` evidence in the absence of feature modules.
- Use Syft, Grype, and Gitleaks in implementation CI per the interim default for
  supply-chain tooling.
- Scope Grype/SBOM to shipped publish and SPA build outputs plus final OCI images.
  OCI image Grype gates at **critical** severity while recording all findings in
  `*.grype.txt` artifacts (base OS highs remain tracked, not blocking at scaffold).
- **Health probing (PROP-remediation):** external host/orchestrator HTTP probes
  only; no in-image probe packages or Docker `HEALTHCHECK` (`oci.healthProbe:
  external` in `build/toolchain.json`).

# Open questions / interim defaults

- Exact SBOM, license-inventory, vulnerability, secret-scan, and architecture-
  test packages are not selected by ADR-010. **Interim default:** choose the
  smallest maintained, license-compatible tools that produce machine-readable
  evidence, pin every tool/action to an exact version or immutable commit, and
  isolate invocation behind repository scripts/targets. **Rationale:** this is
  reversible implementation detail and avoids making a scanner vendor part of
  product architecture. Any tool that changes deployment topology, sends
  protected source externally, or weakens a required gate requires architecture
  and security/privacy review before adoption.

# Findings / deviations

## Resolved (reviews 1–3)

- Invalid OCI matrix tags; mutable implementation CI bootstrap (actions now
  commit-pinned in `implementation.yml`); digest-pinned OCI bases; disabled
  production source maps; SPA favicon.
- Writable `INSTALL_DIR` for CI scanner install; pinned tool directory with
  binary SHA-256 verification; locked `@cyclonedx/cyclonedx-npm@6.0.0` (no
  `pnpm dlx`).
- Playwright evidence: `.playwright-mcp/page-2026-08-09T14-11-15-518Z.png`
  (desktop), `.playwright-mcp/page-2026-08-09T14-11-27-779Z.png` (narrow).
- Implementation CI confirmed green (workflow run #6 on `e2e25a8`).

## Resolved (fourth review — R4-1–R4-6)

| ID | Resolution |
| --- | --- |
| R4-1 | Removed runtime `curl`/`HEALTHCHECK`; external probes; OCI SBOM + Grype via `scan-oci-image-sboms.sh`; ASP.NET runtime `10.0.8-noble` |
| R4-2 | `docs.yml` actions SHA-pinned; `permissions: contents: read` on both workflows |
| R4-3 | `rollForward: disable`; `LangVersion` `14.0` |
| R4-4 | API `Api_host_stops_cleanly_on_disposal`; OCI `docker stop` SIGTERM checks |
| R4-5 | `generate-nuget-licenses.sh` from lock files + nuspec license metadata (provenance separate) |
| R4-6 | `detect-implementation-changes.sh` + conditional jobs; docs job removed from Implementation |

## Fifth review (closure gate)

| ID | Sev | Topic | Status |
| --- | --- | --- | --- |
| R5-1 | P1 | Task closure | **Open** — remain `in-progress` until Implementation #7 green on remediation commit; then state-only completion commit |
| R5-2 | P2 | NuGet license validator | **Resolved** — require `<license>`/`<licenseUrl>`/license file; provenance URLs separate; reviewed override for `NetArchTest.Rules@1.3.2` |
| R5-3 | P2 | OCI failure cleanup | **Resolved** — `EXIT` trap uses `docker rm -f` only for failure cleanup |

## Standing product boundary

- Overall scaffold acceptance still requires later schema, JCS, HTTP, PostgreSQL,
  and session/browser gates. This task reports applicable `GATE-STACK-*`
  coverage precisely and does not overstate acceptance.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Clean .NET locked restore, build, test, and publish | pass | `bash build/scripts/verify-dotnet.sh` (9/9 tests) |
| Clean pnpm frozen install, lint, typecheck, unit test, and Vite build | pass | `bash build/scripts/verify-web.sh` (2/2 Vitest tests; no `.map` in `web/dist`) |
| Architecture tests for composition-root and browser/backend boundaries | pass | `FlexAgent.Architecture.Tests` + `build/scripts/check-web-boundaries.mjs` |
| Worker shutdown gate closes during host stop | pass | `FlexAgent.Runtime.Tests` (`WorkClaimGate` / `StopAsync`) |
| API host stops cleanly on disposal | pass | `FlexAgent.Runtime.Tests` (`Api_host_stops_cleanly_on_disposal`) |
| API/worker graceful process shutdown (OCI) | pass | `verify-oci.sh` uses `docker stop` (SIGTERM) with exit-code check |
| Linux amd64/arm64 OCI builds | pass | Implementation #6 builds both architectures; local `build-oci-images.sh` |
| OCI runtime-content inspection | partial | Local `verify-oci.sh` on tested host architecture only |
| Publish + SPA dependency SBOM and vulnerability scan | pass | `verify-supply-chain.sh`; Grype clean on publish + SPA runtime graph (`--fail-on high`) |
| Final OCI image SBOM and vulnerability scan | pass | `scan-oci-image-sboms.sh`; Grype critical gate with highs recorded in `*.grype.txt` |
| NuGet + npm license inventory | pass | `generate-nuget-licenses.sh` (48 packages; license vs provenance fields) + `pnpm licenses` |
| Reproducibility / immutable CI inputs | pass | Both workflows SHA-pinned; SDK `rollForward: disable`; `LangVersion` `14.0` |
| Secret scan | pass | Gitleaks in `verify-supply-chain.sh` and CI |
| Playwright desktop/narrow smoke | pass | `.playwright-mcp/page-2026-08-09T14-11-15-518Z.png`, `...14-11-27-779Z.png` |
| `python3 scripts/check_docs.py` | pass | Documentation workflow |
| GitHub Actions Implementation workflow | pending | Awaiting run #7 on remediation commit `1d16fb1` (prior green: #6 on `e2e25a8`) |
| Governing-source and gate reconciliation | pass | Applicable `GATE-STACK-*` evidence reconciled for this artifact |

# Blockers

**R5-1:** GitHub Actions Implementation workflow #7 must pass on `1d16fb1` (or
later remediation commit including R5-2/R5-3) before marking this task completed.

# Completion

- [x] Planned scaffold work reconciled with delivered commits through `e2e25a8`
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications rechecked
- [x] Remediation pass R4-1–R4-6 implemented (`1d16fb1`)
- [ ] GitHub Actions evidence confirmed on remediation commit (Implementation #7)
- [ ] Task marked completed and retained for implementation history
