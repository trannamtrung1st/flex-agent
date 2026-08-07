# Flex Agent

Specification-driven platform for configurable agent workflows, assessment sessions, evidence-based evaluation, and governed result release.

**Current phase:** P0 design and realization. See the
[current documentation maturity](docs/README.md#current-maturity) and
[next work](docs/product/overview.md#what-to-do-next).

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

GitHub Actions runs the same checks and Markdown lint on pull requests and pushes to `main` via [`.github/workflows/docs.yml`](.github/workflows/docs.yml).

## Development harness

Cursor and Codex rules, role skills, TDD policy, and Playwright MCP expectations: [contributing/development-harness.md](docs/contributing/development-harness.md).
