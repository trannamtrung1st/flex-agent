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
- Add a strict-TypeScript React/Vite shell using mapped design tokens, system
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
- [>] Reconcile delivered evidence against `GATE-STACK-RUNTIME`,
  `GATE-STACK-MODULES`, `GATE-STACK-SUPPLY`, and
  `GATE-STACK-OPERABILITY`; document partial/deferred gates, update contributor
  and maturity documentation, and re-run documentation validation.

# Current state

Review findings from the post-push and follow-up supply-chain passes are fixed.
Local verification scripts pass, including checksum-verified scanner install,
locked SPA SBOM generation, and OCI checks. Changes are pushed on `main` at
`251477b` (scaffold hardening) and `de11e7d` (composite skills). GitHub Actions
execution of `.github/workflows/implementation.yml` remains to be confirmed.

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
  workspace tree so SDK restore caches do not fail the gate.

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

- Local preflight found .NET SDK `10.0.100`, Node.js `22.18.0`, Corepack
  `0.33.0`, pnpm `9.6.0`, and Docker `29.4.2`; repository pins recorded in
  `build/toolchain.json`, `.nvmrc`, `global.json`, and lock files.
- `@vitejs/plugin-react` `6.0.5` is required for Vite `8.1.x` peer support.
- Post-push review fixed: invalid OCI matrix tags (`linux/amd64` → `linux-amd64`),
  mutable CI bootstrap (now commit-pinned actions + checksum-verified
  `install-supply-chain-tools.sh`), digest-pinned OCI bases, disabled production
  source maps, and SPA favicon.
- Second review fixed: CI installer now uses writable `INSTALL_DIR` under
  `$RUNNER_TEMP` with `$GITHUB_PATH`; local/CI verification always invokes
  pinned tools from task-owned directories via `ensure-supply-chain-tools.sh`;
  SPA SBOM generated from locked runtime dependency graph with
  `@cyclonedx/cyclonedx-npm` and asserts `react`/`react-dom` at pinned versions.
- Third review fixed: `@cyclonedx/cyclonedx-npm@6.0.0` added as a locked workspace
  devDependency (no `pnpm dlx`); supply-chain tool reuse now verifies on-disk
  binary SHA-256 against `build/toolchain.json` before execution.
- ASP.NET runtime OCI base promoted from `10.0.0-noble` to `10.0.7-noble` to
  address published framework advisories in the runtime image.
- Playwright MCP screenshots captured at `.playwright-mcp/page-2026-08-09T14-11-15-518Z.png`
  (desktop) and `.playwright-mcp/page-2026-08-09T14-11-27-779Z.png` (narrow).
- The governing sequence separates the workspace scaffold from canonical
  contracts/JCS and PostgreSQL/Grate work, while also stating that overall
  scaffold acceptance requires all those gates. This task reports gate coverage
  precisely and does not overstate acceptance.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Clean .NET locked restore, build, test, and publish | pass | `bash build/scripts/verify-dotnet.sh` (8/8 tests) |
| Clean pnpm frozen install, lint, typecheck, unit test, and Vite build | pass | `bash build/scripts/verify-web.sh` (2/2 Vitest tests; no `.map` in `web/dist`) |
| Architecture tests for composition-root and browser/backend boundaries | pass | `FlexAgent.Architecture.Tests` + `build/scripts/check-web-boundaries.mjs` |
| API and worker startup, readiness, graceful shutdown, and no-work-after-stop smoke tests | pass | `FlexAgent.Runtime.Tests` |
| Linux `amd64`/`arm64` OCI build and runtime-content inspection | partial | Local `bash build/scripts/verify-oci.sh` on `linux/arm64` with digest-pinned bases and no SPA `.map` files; multi-arch CI job fixed but not run in GitHub Actions yet |
| License inventory, SBOM, vulnerability scan, and secret scan | pass | `bash build/scripts/verify-supply-chain.sh` (pinned tool versions, publish SBOM, SPA runtime SBOM with react/react-dom, Grype clean, Gitleaks clean) |
| Reproducibility checks reject lock drift, floating inputs, and unreviewed package sources | pass | `--locked-mode` / `--frozen-lockfile` enforced in verify scripts and CI; actions pinned by commit SHA in `implementation.yml` |
| Playwright MCP accessibility snapshot plus desktop/narrow screenshots | pass | `.playwright-mcp/page-2026-08-09T14-11-15-518Z.png`, `.playwright-mcp/page-2026-08-09T14-11-27-779Z.png` |
| `python3 scripts/check_docs.py` | pass | Ran after doc updates |
| Governing-source and gate reconciliation | partial | `docs/contributing/workspace.md` updated; GitHub Actions run still required |

# Blockers

GitHub Actions execution of `.github/workflows/implementation.yml` on pushed
`main` (`de11e7d`) — confirm in the Actions tab.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [>] Remaining gaps or unverified behavior are recorded (GitHub Actions confirmation pending)
