# Flex Agent

Specification-driven platform for configurable agent workflows, assessment sessions, evidence-based evaluation, and governed result release.

**Current phase:** P0 design and realization with an executable workspace scaffold. See the
[current documentation maturity](docs/README.md#current-maturity) and
[next work](docs/product/overview.md#what-to-do-next).

## Develop

Pinned toolchain and commands: [contributing/workspace.md](docs/contributing/workspace.md).

```bash
dotnet restore FlexAgent.slnx --locked-mode && dotnet test --solution FlexAgent.slnx
corepack enable && pnpm install --frozen-lockfile && pnpm verify:web
bash build/scripts/verify-oci.sh
python3 scripts/check_docs.py
# Docker Compose required; canonical origin http://localhost:18080
pnpm verify:oidc
```

## Start here

Authoritative product and engineering documentation lives in [`docs/`](docs/README.md).

| Need | Documentation |
| --- | --- |
| Understand the product vision and scope | [Product](docs/product/README.md) |
| Review observable behavior and acceptance criteria | [Requirements](docs/requirements/README.md) |
| Design user journeys and interaction states | [UI/UX](docs/ui-ux/README.md) |
| Review technical boundaries and decisions | [Architecture](docs/architecture/README.md) |

## Validate documentation

```bash
python3 scripts/check_docs.py
```

GitHub Actions runs documentation checks via [`.github/workflows/docs.yml`](.github/workflows/docs.yml) and implementation verification via [`.github/workflows/implementation.yml`](.github/workflows/implementation.yml).

## Development harness

Cursor and Codex rules, role skills, TDD policy, and Playwright MCP expectations: [contributing/development-harness.md](docs/contributing/development-harness.md).
