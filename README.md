# Flex Agent

Specification-driven platform for configurable agent workflows, assessment sessions, evidence-based evaluation, and governed result release.

## Documentation

Authoritative product and engineering documentation lives in [`docs/`](docs/README.md).

| Area | Status |
| --- | --- |
| Product | Approved v0.1 — [concept model](docs/product/concept-model.md), [MVP scope](docs/product/mvp-scope.md) |
| Requirements | All seven P0 specifications approved — [feature catalog](docs/requirements/README.md#feature-catalog-overview) |
| UI/UX | Scaffold — [ui-ux/](docs/ui-ux/README.md) |
| Architecture | Approved OSS-first, self-hostable architecture, model-neutral provider boundary, component baseline, and detailed contracts — [architecture/](docs/architecture/README.md) |

**Current phase:** P0 detailed design and realization. The product baseline, all
seven P0 feature specifications, the
[MVP operational defaults](docs/requirements/mvp-operational-defaults.md), and
the OSS-first self-hostable
[MVP architecture](docs/architecture/mvp-architecture.md), component/provider
defaults, and detailed Session, Evaluation, and Review/Release contracts are
`Approved`. Provider deployment profiles are independently qualified, MVP
adapters support deployment credentials and Organization BYOK, and the boundary
preserves a gated path to Organization model endpoints; no model is part of the
product identity. Complete
component compatibility evidence, the UI/UX contracts, and machine-readable
architecture schemas/fixtures, then implement and verify the
[MVP executable workflow](docs/product/mvp-scope.md#mvp-executable-workflow)
with specification-driven TDD.

The approved application stack is .NET 10/ASP.NET Core for the API and worker,
React/Vite for the SPA, Npgsql/Dapper for explicit PostgreSQL access, and Grate
for plain-SQL migrations. See
[ADR-010](docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md)
for workspace boundaries, integration choices, and the compatibility gates that
must pass before the scaffold is accepted.

## Validate documentation

```bash
python scripts/check_docs.py
```

GitHub Actions runs the same checks and Markdown lint on pull requests and pushes to `main` via [`.github/workflows/docs.yml`](.github/workflows/docs.yml).

## Development harness

Cursor and Codex rules, role skills, TDD policy, and Playwright MCP expectations: [contributing/development-harness.md](docs/contributing/development-harness.md).
