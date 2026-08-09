# Workspace development

This document describes the executable Flex Agent workspace scaffold introduced by
[ADR-010](../architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md).

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
web/
tests/Architecture/
tests/Runtime/
deploy/docker/
build/scripts/
```

Only executable composition roots, the SPA, architecture/runtime tests, and
deployment build inputs are present in this scaffold. Feature modules,
contracts, database migrations, and provider adapters are intentionally
deferred to later sequenced artifacts.

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

## Gate coverage in this scaffold

| Gate | Status in this artifact |
| --- | --- |
| `GATE-STACK-RUNTIME` | Partial — local/API/worker/SPA build, health endpoints, publish, and OCI runtime inspection verified locally on the developer host architecture; blocking CI enforces **linux/amd64** OCI builds; **linux/arm64** is deferred to non-blocking architecture certification |
| `GATE-STACK-MODULES` | Partial — architecture and browser/backend boundary checks only |
| `GATE-STACK-SUPPLY` | Partial — lock files, checksum-verified scanner install, shipped-artifact SBOM/Grype/Gitleaks, and license inventory pass locally; GitHub Actions evidence still required for acceptance |
| `GATE-STACK-OPERABILITY` | Partial — liveness/readiness and graceful work-claim stop only |
| `GATE-STACK-SCHEMA`, `GATE-STACK-JCS`, `GATE-STACK-HTTP`, `GATE-STACK-POSTGRES`, `GATE-STACK-ISOLATION`, `GATE-STACK-PROVIDERS`, `GATE-STACK-ARTIFACTS`, `GATE-STACK-SESSION`, `GATE-STACK-BROWSER` | Deferred — next sequenced artifacts |

Overall scaffold acceptance in
[`mvp-architecture.md`](../architecture/mvp-architecture.md#implementation-readiness)
remains blocked until the deferred gates pass.
