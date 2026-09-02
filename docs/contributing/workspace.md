# Workspace development

This document describes the executable Flex Agent workspace scaffold.
Backend feature modules follow the
[backend module architecture](../architecture/backend-module-architecture.md).
SPA Query, form, icon, and transport ownership follow the
[frontend architecture](../architecture/frontend-architecture.md) guide.
Production SPA topology is one `@flex-agent/web` production entry plus an
isolated design lab. Combined `pnpm verify:web` covers production and
design-lab graphs.

Contribution and verification constraints (SDK pins, locked restore, Grate
migrations, xUnit/Testcontainers/Playwright, OCI publish, supply-chain
locks, module import rules) are current in this document and
[`build/toolchain.json`](../../build/toolchain.json). Historical ADR-010 text
is recoverable from Git and is not the governing catalog.

## Prerequisites

Pinned toolchain versions are recorded in [`build/toolchain.json`](../../build/toolchain.json).

| Tool | Pinned line |
| --- | --- |
| .NET SDK | `10.0.100` |
| ASP.NET Core runtime (OCI) | `10.0.8` |
| Node.js | `22.18.0` |
| pnpm | `9.6.0` |
| React | `19.2.8` |
| Vite | `8.1.5` |
| TanStack Query | `5.102.4` |
| React Hook Form | `7.86.0` |
| Zod | `4.4.3` |
| Hook Form resolvers | `5.2.2` |
| Lucide React | `1.34.0` |

Use `corepack enable` so the repository `packageManager` field selects pnpm
`9.6.0`.

## Repository layout

```text
FlexAgent.slnx
global.json
src/Hosts/FlexAgent.Api/
src/Hosts/FlexAgent.Worker/
src/Modules/Sessions/
src/Modules/SyntheticBrowser/
web/
tests/Architecture/
tests/Runtime/
tests/Sessions/
tests/Integration/
tests/Browser/FlexAgent.Oidc.Playwright/
deploy/docker/
deploy/compose/
build/scripts/
database/migrations/
contracts/
scripts/
```

The workspace grows by implemented capability. Do not create placeholder
modules or empty layer projects. See the backend module guide for current
module packaging, ownership, adapter, and project-splitting rules.

## Common commands

### .NET

```bash
dotnet restore FlexAgent.slnx --locked-mode
dotnet build FlexAgent.slnx
dotnet test --solution FlexAgent.slnx
bash build/scripts/verify-dotnet.sh
```

### Web

```bash
corepack enable
pnpm install --frozen-lockfile
pnpm verify:web:production
pnpm verify:design-lab
bash build/scripts/verify-web.sh
```

Run the SPA locally (`http://localhost:5274/`). Vite uses `strictPort` on `5274`,
so a collision fails instead of moving to another port. Probe that origin before
starting; if it is already serving this SPA, attach.

```bash
cd web && pnpm dev
```

### OIDC / authenticated browser

Canonical Development/Testing origin is `http://localhost:18080` ([STACK-DEC-27](workspace.md)). Contract, case IDs, and residuals: [Keycloak OIDC contract](../operations/provider-profiles/keycloak-oidc-contract.md).

Before `compose:up` or starting Vite, [attach to a running local origin](development-harness.md#attach-to-a-running-local-origin). `pnpm compose:status` is the `:18080` probe. The commands below start a **new** stack; `compose:up` is not idempotent.

```bash
pnpm compose:validate
pnpm compose:up
pnpm compose:down
pnpm verify:oidc
```

The `compose:*` scripts delegate to `build/scripts/authenticated-browser-profile.sh`
(secret generation, realm rendering, validation, and readiness checks).

| Command | Purpose |
| --- | --- |
| `pnpm compose:validate` | Validate the rendered Compose contract without starting services |
| `pnpm compose:up` | Start a **fresh** stack (new synthetic data and OIDC secrets) and wait for `http://localhost:18080`. Do not run this over a healthy attachable stack |
| `pnpm compose:status` | Attach probe: Compose service status and `/auth/session` (`session-endpoint:ok` when healthy) |
| `pnpm compose:down` | Tear down services and generated secret material |
| `pnpm compose:reset` | `compose:down` then `compose:up` |
| `pnpm compose:candidate` | Start a **fresh** stack with the candidate overlay (`RedirectUri` `:5274`). Same reseed warning as `compose:up` |
| `pnpm compose:api:canonical` | Recreate **only** the API with canonical `RedirectUri` (`:18080`). Does not regenerate secrets or reseed |
| `pnpm compose:api:candidate` | Recreate **only** the API with candidate `RedirectUri` (`:5274`). Does not regenerate secrets or reseed |

`pnpm compose:status` also prints `redirect-uri:` so Playwright MCP can match
the sign-in origin. Synthetic usernames and password defaults live in
`tests/Browser/FlexAgent.Oidc.Playwright/helpers/oidc.ts` (do not copy them into
docs or task files). Sign-in steps:
[development harness](development-harness.md#synthetic-sign-in-playwright-mcp).

The profile stores PostgreSQL, Keycloak, and blob data in container `tmpfs` only.
Each `compose:up` regenerates synthetic OIDC secrets and reseeds the stack; use
`compose:down` when finished. Do not rely on data surviving container restarts.

By default the profile also seeds demo Activities, Enrollment, and one
`demo.participant` My work assignment through
`deploy/compose/authenticated-browser/seed-demo-work.sql`. Set
`FLEXAGENT_SEED_DEMO_WORK=0` before `compose:up` to keep identity-only fixtures.
Production deployments do not run these compose seed services.

`pnpm verify:oidc` requires Docker Compose and Chromium. It runs OIDC-E2E-07 negatives, Keycloak logout-token compatibility, canonical Playwright against the shipped `web/` SPA image, then the named non-Production overlay plus Vite on `http://localhost:5274`, and always tears down Compose. Do not run it while you intend to keep a live `:18080` stack for interactive work.

SPA against that overlay:

```bash
pnpm compose:candidate
cp web/.env.example web/.env   # optional; sets VITE_DEV_API_PROXY to the compose gateway
pnpm --filter @flex-agent/web exec -- vite --host localhost --port 5274
```

Or pass the proxy inline instead of using `web/.env`:

```bash
VITE_DEV_API_PROXY=http://127.0.0.1:18080 pnpm --filter @flex-agent/web exec -- vite --host localhost --port 5274
```

The overlay sets `RedirectUri` to `http://localhost:5274/auth/callback`. Do not start candidate Vite on another port against this overlay.

When you finish candidate UI or OIDC work, return the API to the canonical
callback before testing or developing on `http://localhost:18080`:

```bash
pnpm compose:api:canonical
```

Use `pnpm compose:reset` only when a fresh reseed is acceptable. Leaving the
candidate `RedirectUri` active while using the canonical gateway breaks sign-in
on `:18080` (**Sign-in could not be completed**).

Synthetic administrator username `demo.admin` is in
`deploy/compose/keycloak/flex-agent-realm.json`, with numbered extras
`demo.admin1`–`demo.admin5` and `demo.participant1`–`demo.participant30`
(original `demo.admin` / `demo.participant` remain). Do not copy fixture
passwords into task records, screenshots, or Production configuration.

Isolated design lab (`http://localhost:5275/design-lab/surfaces`, or
`http://127.0.0.1:5275` when started with `--host 127.0.0.1`). Probe both
loopback forms before starting.

```bash
cd web && pnpm dev:design-lab --host 127.0.0.1
```

Preview the design-lab bundle (build first):

```bash
cd web && pnpm build:design-lab && pnpm preview:design-lab
pnpm --filter @flex-agent/web test:e2e:design-lab
```

### Supply chain

```bash
bash build/scripts/verify-supply-chain.sh
```

When Syft, Grype, or Gitleaks are not already on `PATH`, the script installs
checksum-verified binaries from pinned release artifacts recorded in
[`build/toolchain.json`](../../build/toolchain.json) via
[`build/scripts/ensure-supply-chain-tools.sh`](../../build/scripts/ensure-supply-chain-tools.sh)
into `.tools/supply-chain/bin` (or `INSTALL_DIR` when overridden). Existing
binaries are reinstalled when their on-disk SHA-256 does not match the toolchain
lock. The SPA runtime SBOM is generated from the locked npm dependency graph
with the pinned `@cyclonedx/cyclonedx-npm` workspace devDependency via
[`build/scripts/generate-spa-sbom.sh`](../../build/scripts/generate-spa-sbom.sh)
and must include `react` and `react-dom` at the pinned versions. NuGet license metadata is generated from committed `packages.lock.json` files and
restored package `.nuspec` license expressions via
[`build/scripts/generate-nuget-licenses.sh`](../../build/scripts/generate-nuget-licenses.sh).
License evidence requires `<license>`, `<licenseUrl>`, or a license file;
project and repository URLs are recorded separately as provenance. Packages
without nuspec license metadata may use reviewed entries in
`build/toolchain.json` (`nugetLicenseReview`).
Final OCI image SBOMs are generated and scanned with
[`build/scripts/scan-oci-image-sboms.sh`](../../build/scripts/scan-oci-image-sboms.sh)
after [`build/scripts/build-oci-images.sh`](../../build/scripts/build-oci-images.sh).
CI uses the same installer with `INSTALL_DIR` under `$RUNNER_TEMP` instead of
piping remote install scripts.

### Documentation

```bash
python3 scripts/check_docs.py
```

### OCI images

```bash
bash build/scripts/verify-oci.sh
```

OCI health is probed externally from the host (`curl`); runtime images do not
install probe packages or define Docker `HEALTHCHECK` instructions. The script
also verifies SIGTERM graceful shutdown with `docker stop` and scans final image
SBOMs.

Or build individually:

```bash
docker build -f deploy/docker/api.Dockerfile -t flex-agent-api:local .
docker build -f deploy/docker/worker.Dockerfile -t flex-agent-worker:local .
docker build -f deploy/docker/spa.Dockerfile -t flex-agent-spa:local .
```

Images are built for verification only. This scaffold does not publish or
deploy them.

## CI

- [`.github/workflows/docs.yml`](../../.github/workflows/docs.yml) — documentation validation
- [`.github/workflows/implementation.yml`](../../.github/workflows/implementation.yml) — locked restore/build/test, web checks, supply-chain evidence, **linux/amd64** OCI builds, and the blocking `oidc` job (`pnpm verify:oidc`). Every push to `main` runs that full job set. Pull requests still skip when the PR diff has no implementation paths (see [`build/scripts/detect-implementation-changes.sh`](../../build/scripts/detect-implementation-changes.sh)). Pull-request updates cancel superseded Implementation runs; pushes to `main` do not.
- [`.github/workflows/architecture-certification.yml`](../../.github/workflows/architecture-certification.yml) — **non-blocking** weekly/manual **linux/arm64** OCI certification; required before claiming `arm64` release support (see [ADR-010](workspace.md))

## Gate coverage

| Gate | Status |
| --- | --- |
| `GATE-STACK-RUNTIME` | Partial — local/API/worker/SPA build, health endpoints, publish, and OCI runtime inspection verified locally; blocking CI enforces **linux/amd64** OCI builds; **linux/arm64** is deferred to non-blocking architecture certification |
| `GATE-STACK-MODULES` | Partial — composition-root, browser/backend, Sessions ownership, and `FlexAgent.CanonicalJson` dependency-boundary checks |
| `GATE-STACK-SUPPLY` | Partial — lock files, checksum-verified scanner install, shipped-artifact SBOM/Grype/Gitleaks, and license inventory |
| `GATE-STACK-OPERABILITY` | Partial — liveness/readiness and graceful work-claim stop only |
| `GATE-STACK-SCHEMA` | Partial — canonical Draft 2020-12 catalog including Session runtime, Decision v1/v2, Enrollment v1 plus v2 timing/accommodation projections, digest, Evidence locator, audit, and SSE schemas, with fixtures, C#/TypeScript mappings, OpenAPI projection, and contract tests; HTTP runtime request validation remains deferred |
| `GATE-STACK-JCS` | Partial — language-neutral ADR-001/ADR-004/Evidence-set and manifest-seal fixtures with independent .NET and Node verification; production normalization builders remain deferred |
| `GATE-STACK-POSTGRES` | Partial — Grate empty/repeat/changed-script plus Session runtime migrations `0005`–`0029`, human-authentication migrations `0030`–`0033`, Assessment migrations `0034`–`0042`, Enrollment assignment `0043`, shared admission `0044`–`0045`, accommodations `0046`, and complete Enrollment parent identity `0047`, including Docker-backed upgrade execution and scoped repository isolation/concurrency/idempotency/audit tests against PostgreSQL 18; backup/restore remain later |
| `GATE-STACK-ISOLATION` | Partial — Sessions protected repositories require complete ownership tuples and have wrong-scope/guessed-id tests; Worker timer fire reauthorizes per-Session service delegation at commit; human OIDC application-session isolation and the first Enrollment assignment/discovery slice are implemented and independently reviewed; organization-wide list/count matrices remain later |
| `GATE-STACK-SESSION` | Partial — opaque application-session login/rotation/revocation/back-channel logout is implemented; `pnpm verify:oidc` proves live PKCE login, local logout, and signed provider-forced logout against PostgreSQL. Remaining multi-instance live matrix, 30-minute/12-hour elapsed bounds, and held-SSE adoption remain later |
| `GATE-STACK-BROWSER` | Partial — static SPA build plus Playwright MCP; NGINX-hosted canonical OIDC Playwright and a named candidate/non-Production Wave 8.1 transition suite are required through `pnpm verify:oidc`. Remaining production journeys after authentication remain later |
| `GATE-STACK-HTTP` | Partial — human OIDC login/callback/logout/back-channel and JWKS fail-closed behavior are implemented; Keycloak `26.7.0` signed logout-token compatibility, canonical gateway PKCE, unbound/ambiguous fail-closed, and restricted-route probes are required through `pnpm verify:oidc`. Real MFA, key rotation, clock skew, account-disablement, outage, and multi-instance callback remain later |
| `GATE-STACK-ARTIFACTS` | Deferred — SeaweedFS remains a later sequenced artifact |
| `GATE-STACK-PROVIDERS` | Partial — deterministic fake plus the vendor-neutral OpenAI-compatible adapter isolated in `FlexAgent.Sessions.OpenAiCompatible` (`openai_compatible` / `sessions.openai_compatible.v1`), fake-transport Chat Completions contract tests at digest-bound base paths, public/private destination-policy negatives, frozen per-Session profile/credential authority, and fail-closed Worker composition. Historical `direct_openai` / `sessions.openai.v1` identities remain inspectable and cannot enable execution. Deterministic migration evidence is not live qualification; the adapter stays default-off until a successor exact-profile run passes. The distinct OpenRouter synthetic-development adapter is implemented and labeled `qualified_for: synthetic_development` for the pinned `openai/gpt-oss-20b:free` / Darkbloom adapter harness only ([phase 28](../operations/provider-profiles/qualified/openrouter/synthetic-development-phase28-2026-08-21.md)). It is not enabled for real data, Production, or Staging. Interactive hosted chat, exact OpenAI-compatible profile qualification, Organization-hosted private-endpoint live evidence, and vLLM contract evidence also remain |

Focused verification for schema/JCS infrastructure:

```bash
dotnet test --project tests/Contract/FlexAgent.Contract.Tests/FlexAgent.Contract.Tests.csproj -c Release
dotnet test --project tests/CanonicalJson/FlexAgent.CanonicalJson.Tests/FlexAgent.CanonicalJson.Tests.csproj -c Release
pnpm --filter @flex-agent/contracts test
```

Overall scaffold acceptance in
[`mvp-architecture.md`](../architecture/mvp-architecture.md#implementation-readiness)
remains blocked until the deferred gates pass.
