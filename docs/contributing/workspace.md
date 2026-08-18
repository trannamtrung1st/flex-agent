# Workspace development

This document describes the executable Flex Agent workspace scaffold introduced by
[ADR-010](../architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md).
Backend feature modules follow the
[backend module architecture](../architecture/backend-module-architecture.md):
a domain-oriented modular monolith with ports and adapters and inward dependency
rules.

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
deploy/docker/
build/scripts/
database/migrations/
contracts/
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
pnpm lint
pnpm typecheck
pnpm test
pnpm build
bash build/scripts/verify-web.sh
```

Run the SPA locally:

```bash
cd web && pnpm dev
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
- [`.github/workflows/implementation.yml`](../../.github/workflows/implementation.yml) — locked restore/build/test, web checks, supply-chain evidence, and **linux/amd64** OCI builds on every implementation-relevant change (see [`build/scripts/detect-implementation-changes.sh`](../../build/scripts/detect-implementation-changes.sh))
- [`.github/workflows/architecture-certification.yml`](../../.github/workflows/architecture-certification.yml) — **non-blocking** weekly/manual **linux/arm64** OCI certification; required before claiming `arm64` release support (see [ADR-010](../architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md#oci-platform-certification))

## Gate coverage

| Gate | Status |
| --- | --- |
| `GATE-STACK-RUNTIME` | Partial — local/API/worker/SPA build, health endpoints, publish, and OCI runtime inspection verified locally; blocking CI enforces **linux/amd64** OCI builds; **linux/arm64** is deferred to non-blocking architecture certification |
| `GATE-STACK-MODULES` | Partial — composition-root, browser/backend, Sessions ownership, and `FlexAgent.CanonicalJson` dependency-boundary checks |
| `GATE-STACK-SUPPLY` | Partial — lock files, checksum-verified scanner install, shipped-artifact SBOM/Grype/Gitleaks, and license inventory |
| `GATE-STACK-OPERABILITY` | Partial — liveness/readiness and graceful work-claim stop only |
| `GATE-STACK-SCHEMA` | Partial — canonical Draft 2020-12 catalog including Session runtime, Decision v1/v2, digest, Evidence locator, audit, and SSE schemas, with fixtures, C#/TypeScript mappings, OpenAPI projection, and contract tests; HTTP runtime request validation remains deferred |
| `GATE-STACK-JCS` | Partial — language-neutral ADR-001/ADR-004/Evidence-set and manifest-seal fixtures with independent .NET and Node verification; production normalization builders remain deferred |
| `GATE-STACK-POSTGRES` | Partial — Grate empty/repeat/changed-script plus Session runtime migrations `0005`–`0024` and scoped repository isolation/concurrency tests against PostgreSQL 18; backup/restore and full-module isolation remain later |
| `GATE-STACK-ISOLATION` | Partial — Sessions protected repositories require complete ownership tuples and have wrong-scope/guessed-id tests; Worker timer fire reauthorizes per-Session service delegation at commit; organization-wide list/count matrices and OIDC remain later |
| `GATE-STACK-SESSION` | Partial — synthetic SSE reconnect/replay preserves Session sequence; production opaque application-session rotation/revocation and HTTP SSE host wiring remain later |
| `GATE-STACK-BROWSER` | Partial — static SPA build plus Playwright MCP on the synthetic Participant Text Session; authenticated production journey and NGINX-hosted e2e in CI remain later |
| `GATE-STACK-HTTP`, `GATE-STACK-PROVIDERS`, `GATE-STACK-ARTIFACTS` | Deferred — OIDC/HTTP contract suite, live provider adapters, and SeaweedFS remain later sequenced artifacts |

Focused verification for schema/JCS infrastructure:

```bash
dotnet test --project tests/Contract/FlexAgent.Contract.Tests/FlexAgent.Contract.Tests.csproj -c Release
dotnet test --project tests/CanonicalJson/FlexAgent.CanonicalJson.Tests/FlexAgent.CanonicalJson.Tests.csproj -c Release
pnpm --filter @flex-agent/contracts test
```

Overall scaffold acceptance in
[`mvp-architecture.md`](../architecture/mvp-architecture.md#implementation-readiness)
remains blocked until the deferred gates pass.
