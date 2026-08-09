# Workspace development

This document describes the executable Flex Agent workspace scaffold introduced by
[ADR-010](../architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md).

## Prerequisites

Pinned toolchain versions are recorded in [`build/toolchain.json`](../../build/toolchain.json).

| Tool | Pinned line |
| --- | --- |
| .NET SDK | `10.0.100` |
| ASP.NET Core runtime (OCI) | `10.0.7` |
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
and must include `react` and `react-dom` at the pinned versions.
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
- [`.github/workflows/implementation.yml`](../../.github/workflows/implementation.yml) — locked restore/build/test, web checks, supply-chain evidence, and multi-architecture OCI builds

## Gate coverage in this scaffold

| Gate | Status in this artifact |
| --- | --- |
| `GATE-STACK-RUNTIME` | Partial — local/API/worker/SPA build, health endpoints, publish, and one API OCI build verified locally; CI covers multi-architecture builds |
| `GATE-STACK-MODULES` | Partial — architecture and browser/backend boundary checks only |
| `GATE-STACK-SUPPLY` | Partial — lock files, checksum-verified scanner install, shipped-artifact SBOM/Grype/Gitleaks, and license inventory pass locally; GitHub Actions evidence still required for acceptance |
| `GATE-STACK-OPERABILITY` | Partial — liveness/readiness and graceful work-claim stop only |
| `GATE-STACK-SCHEMA`, `GATE-STACK-JCS`, `GATE-STACK-HTTP`, `GATE-STACK-POSTGRES`, `GATE-STACK-ISOLATION`, `GATE-STACK-PROVIDERS`, `GATE-STACK-ARTIFACTS`, `GATE-STACK-SESSION`, `GATE-STACK-BROWSER` | Deferred — next sequenced artifacts |

Overall scaffold acceptance in
[`mvp-architecture.md`](../architecture/mvp-architecture.md#implementation-readiness)
remains blocked until the deferred gates pass.
