# Flex Agent

Specification-driven platform for configurable agent workflows, assessment sessions, evidence-based evaluation, and governed result release.

## Documentation

Authoritative product and engineering documentation lives in [`docs/`](docs/README.md).

| Area | Status |
| --- | --- |
| Product | Approved v0.1 — [concept model](docs/product/concept-model.md), [MVP scope](docs/product/mvp-scope.md) |
| Requirements | All seven P0 specifications approved — [feature catalog](docs/requirements/README.md#feature-catalog-overview) |
| UI/UX | Scaffold — [ui-ux/](docs/ui-ux/README.md) |
| Architecture | Active — [architecture/](docs/architecture/README.md) |

**Current phase:** P0 realization. The product baseline and all seven P0 feature specifications, from [`auth-resource-isolation.md`](docs/requirements/features/auth-resource-isolation.md) through [`review-result-release.md`](docs/requirements/features/review-result-release.md), are `Approved`. Next, complete the blocking architecture and UI/UX contracts, then implement and verify the [MVP executable workflow](docs/product/mvp-scope.md#mvp-executable-workflow) with specification-driven TDD.

## Validate documentation

```bash
python scripts/check_docs.py
```

GitHub Actions runs the same checks and Markdown lint on pull requests and pushes to `main` via [`.github/workflows/docs.yml`](.github/workflows/docs.yml).

## Development harness

Cursor and Codex rules, role skills, TDD policy, and Playwright MCP expectations: [contributing/development-harness.md](docs/contributing/development-harness.md).
