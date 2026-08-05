# Flex Agent

Specification-driven platform for configurable agent workflows, assessment sessions, evidence-based evaluation, and governed result release.

## Documentation

Authoritative product and engineering documentation lives in [`docs/`](docs/README.md).

| Area | Status |
| --- | --- |
| Product | Approved v0.1 — [concept model](docs/product/concept-model.md), [MVP scope](docs/product/mvp-scope.md) |
| Requirements | P0 specification review in progress — [feature catalog](docs/requirements/README.md#feature-catalog-overview) |
| UI/UX | Scaffold — [ui-ux/](docs/ui-ux/README.md) |
| Architecture | Scaffold — [architecture/](docs/architecture/README.md) |

**Current phase:** Document-by-document review of P0 feature specifications. [`auth-resource-isolation.md`](docs/requirements/features/auth-resource-isolation.md) is `Draft`; 18 catalog files remain placeholders. Continue in [catalog order](docs/requirements/README.md#p0-authoring-order).

## Validate documentation

```bash
python scripts/check_docs.py
```

GitHub Actions runs the same checks and Markdown lint on pull requests and pushes to `main` via [`.github/workflows/docs.yml`](.github/workflows/docs.yml).

## Development harness

Cursor rules, role skills, TDD policy, and Playwright MCP expectations: [contributing/development-harness.md](docs/contributing/development-harness.md).
