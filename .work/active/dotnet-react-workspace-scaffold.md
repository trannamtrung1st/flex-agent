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
- [>] **Remediation pass (fourth review):** close supply-chain, reproducibility,
  shutdown-evidence, license-inventory, and CI-path gaps before marking this task
  complete (see Findings / deviations).
- [ ] Reconcile delivered evidence against `GATE-STACK-RUNTIME`,
  `GATE-STACK-MODULES`, `GATE-STACK-SUPPLY`, and
  `GATE-STACK-OPERABILITY`; update contributor and maturity documentation;
  rerun Implementation CI on the remediation commit; then retire this task file.

# Remediation pass (ordered)

1. **[P1] OCI supply-chain evidence** — remove runtime `apt-get`/`apk add curl`
   in favor of a reproducibly pinned health-probe approach; generate and scan
   SBOMs for final API/worker/SPA images (not only publish output and npm graph).
2. **[P1] Immutable docs CI bootstrap** — pin `docs.yml` actions to commit SHAs,
   record them in `build/toolchain.json`, and add top-level `permissions:
   contents: read`.
3. **[P2] Fail-closed .NET SDK/language pins** — replace `rollForward:
   latestFeature` and `<LangVersion>latest</LangVersion>` with exact reviewed
   values.
4. **[P2] Graceful shutdown evidence** — describe evidence accurately; add OCI
   `docker stop` (SIGTERM) checks and API shutdown coverage where feasible;
   stop using `docker rm -f` as shutdown proof.
5. **[P2] NuGet license inventory** — produce package identity/version plus
   license expression or license reference, not only `dotnet list package` JSON.
6. **[P2] CI path filtering** — gate expensive Implementation jobs on
   implementation-relevant paths so `.work/**` and docs-only commits stay on the
   lightweight Documentation workflow; remove duplicate docs job from
   Implementation when path-gated.

# Current state

Scaffold foundation and hardening commits are on `main` (`251477b`, `de11e7d`).
Implementation workflow **#6** is green on `e2e25a8` (multi-arch OCI and
supply-chain jobs included). Local `verify-*` scripts pass.

The fourth review approves the architecture/scaffold direction but requests
changes before this task is marked complete. Remaining gaps are concentrated in
supply-chain evidence/reproducibility, verification accuracy, and CI ergonomics
—not in the fundamental workspace design.

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
- Scope Grype/SBOM to shipped publish and SPA build outputs rather than the full
  workspace tree so SDK restore caches do not fail the gate. **Remediation:**
  extend to final OCI images without removing publish/npm evidence.

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
- **Health probe without runtime package install (PROP-remediation):** prefer a
  pinned static probe binary copied into the image, or a base image that already
  includes the minimal probe dependency at a digest-pinned layer, rather than
  unpinned `apt-get`/`apk add` during build. Record the chosen approach in
  `build/toolchain.json` before closing the task.

# Findings / deviations

## Resolved (reviews 1–3)

- Invalid OCI matrix tags; mutable implementation CI bootstrap (actions now
  commit-pinned in `implementation.yml`); digest-pinned OCI bases; disabled
  production source maps; SPA favicon.
- Writable `INSTALL_DIR` for CI scanner install; pinned tool directory with
  binary SHA-256 verification; locked `@cyclonedx/cyclonedx-npm@6.0.0` (no
  `pnpm dlx`).
- ASP.NET runtime OCI base promoted to `10.0.7-noble`.
- Playwright evidence: `.playwright-mcp/page-2026-08-09T14-11-15-518Z.png`
  (desktop), `.playwright-mcp/page-2026-08-09T14-11-27-779Z.png` (narrow).
- Implementation CI confirmed green (workflow run #6 on `e2e25a8`).

## Open (fourth review — block completion)

| ID | Sev | Topic | Evidence gap |
| --- | --- | --- | --- |
| R4-1 | P1 | OCI supply-chain | Final images install floating `curl` via `apt-get`/`apk`; SBOM/Grype scan publish output and npm graph only, not final images |
| R4-2 | P1 | Docs CI immutability | `docs.yml` still uses `@v4`/`@v5`/`@v18` floating actions |
| R4-3 | P2 | SDK/language pins | `global.json` `rollForward: latestFeature`; `Directory.Build.props` `LangVersion` latest |
| R4-4 | P2 | Graceful shutdown | Worker `WorkClaimGate` tested; OCI cleanup uses `docker rm -f`; no API shutdown or SIGTERM OCI test |
| R4-5 | P2 | NuGet licenses | `dotnet list package` JSON is inventory, not license metadata (unlike `pnpm licenses`) |
| R4-6 | P2 | CI path filtering | Implementation runs on every push (e.g. `.work/**` commits); duplicates docs checks |

## Standing product boundary

- Overall scaffold acceptance still requires later schema, JCS, HTTP, PostgreSQL,
  and session/browser gates. This task reports applicable `GATE-STACK-*`
  coverage precisely and does not overstate acceptance.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Clean .NET locked restore, build, test, and publish | pass | `bash build/scripts/verify-dotnet.sh` (8/8 tests) |
| Clean pnpm frozen install, lint, typecheck, unit test, and Vite build | pass | `bash build/scripts/verify-web.sh` (2/2 Vitest tests; no `.map` in `web/dist`) |
| Architecture tests for composition-root and browser/backend boundaries | pass | `FlexAgent.Architecture.Tests` + `build/scripts/check-web-boundaries.mjs` |
| Worker shutdown gate closes during host stop | pass | `FlexAgent.Runtime.Tests` (`WorkClaimGate` / `StopAsync`) |
| API/worker graceful process shutdown (OCI) | partial | Not verified; `verify-oci.sh` uses `docker rm -f` |
| Linux `amd64`/`arm64` OCI build and runtime-content inspection | pass | Local `verify-oci.sh`; Implementation workflow #6 on `e2e25a8` |
| Publish + SPA dependency SBOM and vulnerability scan | pass | `verify-supply-chain.sh`; Grype clean on publish + SPA runtime graph |
| Final OCI image SBOM and vulnerability scan | fail | Not implemented; runtime `curl` installed from unpinned package managers |
| NuGet + npm license inventory | partial | `pnpm licenses` present; NuGet side is package list only |
| Reproducibility / immutable CI inputs | partial | `implementation.yml` pinned; `docs.yml`, SDK roll-forward, and LangVersion not fail-closed |
| Secret scan | pass | Gitleaks in `verify-supply-chain.sh` and CI |
| Playwright desktop/narrow smoke | pass | `.playwright-mcp/page-2026-08-09T14-11-15-518Z.png`, `...14-11-27-779Z.png` |
| `python3 scripts/check_docs.py` | pass | Documentation workflow |
| GitHub Actions Implementation workflow | pass | Run #6 green on `e2e25a8` |
| Governing-source and gate reconciliation | partial | Remediation pass required before task completion |

# Blockers

Remediation items **R4-1** through **R4-6** (see Open findings). GitHub Actions
confirmation is no longer a blocker.

# Completion

- [x] Planned scaffold work reconciled with delivered commits through `e2e25a8`
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass (baseline)
- [x] Governing specifications rechecked
- [x] GitHub Actions evidence confirmed (Implementation #6 on `e2e25a8`)
- [>] Remediation pass R4-1–R4-6 complete with updated evidence
- [ ] Task file retired after final reconciliation
