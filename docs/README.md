# Flex Agent Documentation

Authoritative product and engineering documentation for the Flex Agent platform.

## Audience routes

| If you need to… | Start here |
| --- | --- |
| Understand product vision, vocabulary, and MVP direction | [Product overview](overview-idea.md) |
| Find or author approved requirements and acceptance criteria | [Requirements](requirements/README.md) |
| Design journeys, interaction states, or design-system guidance | [UI/UX](ui-ux/README.md) |
| Review system boundaries, data flows, or technical decisions | [Architecture](architecture/README.md) |
| Write a new feature specification | [Feature spec template](templates/feature-spec.md) and [Feature specs](requirements/features/README.md) |
| Understand Cursor rules, roles, and QA expectations | [Development harness](contributing/development-harness.md) |

## Source-of-truth order

When documents conflict, resolve in this order:

1. **Approved feature specification** with stable requirement and acceptance-criterion IDs
2. **Approved architecture decision** (ADR)
3. **[Product overview](overview-idea.md)** — foundational intent and vocabulary
4. **Existing implementation and tests**
5. **Clearly labeled proposal** (`Proposed`, `Draft`, open question)

The overview informs direction but does not replace approved acceptance criteria. Illustrative examples in the overview are not MVP commitments unless captured in an approved spec.

## Document status

| Status | Meaning |
| --- | --- |
| `Draft` | Work in progress; not authoritative for implementation |
| `In review` | Under review; not yet approved |
| `Approved` | Authoritative for the governed behavior |
| `Implemented` | Approved and reflected in the product |
| `Proposed` | Suggested default or option requiring explicit approval |
| `Superseded` | Replaced by a newer document; retained for history |

## Current maturity

| Area | Status | Notes |
| --- | --- | --- |
| Product overview | Draft | Foundational vocabulary and MVP direction |
| Requirements | Scaffold | Catalog and conventions only; no approved feature specs yet |
| UI/UX | Scaffold | Placeholder; journeys, interaction specs, and design system deferred |
| Architecture | Scaffold | Placeholder; technical design and ADRs deferred |
| Contributing | Active | Cursor rules, role skills, TDD policy, and Playwright MCP guidance |
| Templates | Active | Reusable authoring templates (not authoritative content) |

## Documentation areas

### Product

- [Product overview](overview-idea.md) — vision, core concepts (Agent, Harness, Campaign, Session), MVP use case, and principles

### Requirements

- [Requirements hub](requirements/README.md) — lifecycle, ID conventions, and MVP feature-spec catalog
- [Feature specifications](requirements/features/README.md) — future home for approved feature specs

### UI/UX

- [UI/UX documentation](ui-ux/README.md) — journeys, interaction specifications, accessibility, content, and design system (deferred)

### Architecture

- [Architecture documentation](architecture/README.md) — system boundaries, data, integration, runtime, and deployment (deferred)
- [Architecture decisions](architecture/decisions/README.md) — ADRs (deferred)

### Contributor guidance

- [Development harness](contributing/development-harness.md) — Cursor rules, role skills, TDD policy, and Playwright MCP expectations

### Templates

- [Feature spec template](templates/feature-spec.md) — standard structure for feature specifications

## Canonical vocabulary

Use these terms consistently across all documents:

| Term | Definition |
| --- | --- |
| **Agent** | Reusable AI identity: knowledge, capabilities, behavior, and evaluation approach |
| **Harness** | Governed operating instructions: workflow, tools, policies, memory controls, and evaluation procedures |
| **Campaign** | Structured activity configuration: participants, limits, rules, and selected agent/harness |
| **Session** | One isolated execution between a configured agent and a participant or authorized role |
| **Participant** | Person taking part in a session |
| **Reviewer** | Authorized human who inspects evidence, evaluations, and outcomes |
| **Memory** | Administratively controlled agent learning and retention |
| **Evidence** | Material linked to evaluation and audit (submissions, transcripts, tool results) |
| **Evaluation** | Structured, evidence-backed assessment of session outcomes |

## Authoring guidelines

1. Read related documents before writing; reuse canonical vocabulary and stable IDs.
2. Choose the smallest appropriate document type and location.
3. Separate confirmed facts, normative requirements, decisions, proposals, assumptions, and deferred scope.
4. Link to one authoritative definition instead of copying content across documents.
5. Use `must` for approved requirements, `should` for recommendations, and `may` for permitted options.
6. Preserve history; do not silently rewrite approved intent.
7. Use `docs/templates/` for reusable authoring templates; keep authoritative content in the area folders (`requirements/`, `ui-ux/`, `architecture/`).
