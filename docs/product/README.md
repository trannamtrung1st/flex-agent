# Product documentation

Stable product strategy, domain concepts, and MVP boundaries for Flex Agent.

## Status

**Accepted baseline v0.1.** Product documents govern product meaning and scope. They inform requirements, UI/UX, and architecture but do not replace approved feature specifications, UI/UX specifications, or ADRs within those areas of authority.

## Document metadata

Each canonical product document includes owner, version, effective date, last reviewed date, and related decisions. See individual documents for current values.

| Document | Status | Version |
| --- | --- | --- |
| [Product overview](overview.md) | Accepted baseline | 0.1 |
| [Concept model](concept-model.md) | Accepted baseline | 0.1 |
| [MVP scope](mvp-scope.md) | Accepted baseline | 0.1 |

Formal approver sign-off is pending. **Accepted baseline** means requirements authors may depend on these documents for feature specification authoring.

## Purpose

This area governs **what the product means** — vision, positioning, principles, canonical concepts, relationships, invariants, and scope boundaries — independent of specific UI implementations or technical stacks.

Use the boundary test when choosing a document location:

| Question | If yes, document belongs in… |
| --- | --- |
| Would this remain true if the UI and technology were replaced? | **Product** (`docs/product/`) |
| Does it state observable behavior the system must provide? | **Requirements** ([`requirements/`](../requirements/README.md)) |
| Does it define user-facing interaction, content, or visual design? | **UI/UX** ([`ui-ux/`](../ui-ux/README.md)) |
| Does it explain technical realization, data ownership, or deployment? | **Architecture** ([`architecture/`](../architecture/README.md)) |

Technical component designs (for example, real-time voice controllers, append-only storage, or UTC timestamps) belong in architecture. The product layer states outcome-oriented contracts; architecture chooses implementation.

## Documents

| Document | Governs |
| --- | --- |
| [Product overview](overview.md) | Vision, positioning, validation strategy, principles, and navigation |
| [Concept model](concept-model.md) | Canonical definitions, relationships, configuration resolution, lifecycles, and product invariants |
| [MVP scope](mvp-scope.md) | MVP validation slice, release tiers, non-goals, and deferred capabilities |

## Authority

Documents govern by concern. A document overrides another only within its area of authority. Full rules: [Documentation home — Authority by concern](../README.md#authority-by-concern).

| Concern | Authoritative in product area |
| --- | --- |
| Product meaning and scope | Accepted baseline [concept model](concept-model.md), [MVP scope](mvp-scope.md), and future product decision records |
| Vision and validation strategy | Accepted baseline [product overview](overview.md) |

Illustrative examples and candidate capabilities in product documents are **not** MVP commitments until captured in an approved feature spec.

## Recommended documentation sequence

Before authoring all candidate feature specs:

1. Use accepted baseline concept model and MVP scope (this release)
2. Author P0 specs around isolation, resolved configuration, and the end-to-end assessment slice — see [MVP feature-spec catalog](../requirements/README.md#mvp-feature-spec-catalog)
3. Decompose agent and harness configuration only to the minimum required by that slice
4. Move technical realization (voice controller, storage patterns) into architecture as ADRs emerge

## Future artifact types (not created yet)

Add these only when a real decision or evidence set needs an owner and lifecycle:

| Type | When to create |
| --- | --- |
| Product decision record | A cross-cutting product choice with alternatives, rationale, and consequences |
| Research / validation note | Interview findings, usability evidence, or assumption validation that informs scope |

Do not create empty folders for these artifact types in advance.

## Related documents

- [Documentation home](../README.md)
- [Requirements](../requirements/README.md)
- [UI/UX](../ui-ux/README.md)
- [Architecture](../architecture/README.md)
