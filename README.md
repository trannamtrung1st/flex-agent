# Flex Agent

Specification-driven platform for configurable agent workflows, assessment sessions, evidence-based evaluation, and governed result release.

## Documentation

Authoritative product and engineering documentation lives in [`docs/`](docs/README.md).

| Area | Status |
| --- | --- |
| Product | Approved v0.1 — [concept model](docs/product/concept-model.md), [MVP scope](docs/product/mvp-scope.md) |
| Requirements | All seven P0 specifications approved — [feature catalog](docs/requirements/README.md#feature-catalog-overview) |
| UI/UX | Scaffold — [ui-ux/](docs/ui-ux/README.md) |
| Architecture | Approved OSS-first, self-hostable architecture, component/provider baseline, and detailed contracts; Mistral Small 3.1 benchmark selected, self-hosted production profile not yet certified — [architecture/](docs/architecture/README.md) |

**Current phase:** P0 detailed design and realization. The product baseline, all
seven P0 feature specifications, the
[MVP operational defaults](docs/requirements/mvp-operational-defaults.md), and
the OSS-first self-hostable
[MVP architecture](docs/architecture/mvp-architecture.md), component/provider
defaults, and detailed Session, Evaluation, and Review/Release contracts are
`Approved`. The vLLM runtime family and Mistral Small 3.1 24B Instruct benchmark
candidate are selected, but the exact self-hosted artifact, quantization, and
hardware profile remain uncertified. Complete component compatibility evidence,
the UI/UX contracts, and machine-readable architecture schemas/fixtures, then implement and verify the
[MVP executable workflow](docs/product/mvp-scope.md#mvp-executable-workflow)
with specification-driven TDD.

## Validate documentation

```bash
python scripts/check_docs.py
```

GitHub Actions runs the same checks and Markdown lint on pull requests and pushes to `main` via [`.github/workflows/docs.yml`](.github/workflows/docs.yml).

## Development harness

Cursor and Codex rules, role skills, TDD policy, and Playwright MCP expectations: [contributing/development-harness.md](docs/contributing/development-harness.md).
