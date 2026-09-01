# Product documentation

Stable product strategy, domain concepts, and MVP boundaries for Flex Agent.

## Status

**In review.** Concept model, product overview, and MVP scope state current
product meaning. They inform requirements, UI/UX, and architecture but do not
replace feature specifications, UI/UX specifications, or architecture
documents within those areas of authority. This Phase 3 rewrite is recoverable
beside previous Git versions and is not the Phase 4 authority cutover.

## Document metadata

| Document | Status | Version | Owner | Approvers | Last reviewed |
| --- | --- | --- | --- | --- | --- |
| [Concept model](concept-model.md) | In review | 0.5 | Product Lead | Product Lead, Architecture Lead | 2026-09-01 |
| [MVP scope](mvp-scope.md) | In review | 0.4 | Product Lead | Product Lead, Architecture Lead | 2026-09-01 |
| [Product overview](overview.md) | In review | 0.4 | Product Lead | Product Lead, Architecture Lead | 2026-09-01 |

## Purpose

This area governs **what the product means** — vision, positioning,
principles, canonical concepts, relationships, invariants, and scope
boundaries — independent of specific UI implementations or technical stacks.

Use the boundary test when choosing a document location:

| Question | If yes, document belongs in… |
| --- | --- |
| Would this remain true if the UI and technology were replaced? | **Product** (`docs/product/`) |
| Does it state observable behavior the system must provide? | **Requirements** ([`requirements/`](../requirements/README.md)) |
| Does it define user-facing interaction, content, or visual design? | **UI/UX** ([`ui-ux/`](../ui-ux/README.md)) |
| Does it explain technical realization, data ownership, or deployment? | **Architecture** ([`architecture/`](../architecture/README.md)) |

Technical component designs belong in architecture. The product layer states
outcome-oriented contracts; architecture chooses implementation.

## Documents

| Document | Governs |
| --- | --- |
| [Product overview](overview.md) | Vision, positioning, validation strategy, principles, and navigation |
| [Concept model](concept-model.md) | Canonical definitions, configuration precedence, lifecycles, and product invariants |
| [MVP scope](mvp-scope.md) | Executable MVP slice, release tiers, non-goals, and deferred capabilities |

## Authority

Documents govern by concern. A document overrides another only within its area
of authority. Full rules: [Documentation home — Authority by concern](../README.md#authority-by-concern).

| Concern | Authoritative source |
| --- | --- |
| Product meaning and scope | [Concept model](concept-model.md) and [MVP scope](mvp-scope.md) |
| Vision and validation strategy | [Product overview](overview.md) |
| Observable system behavior | Feature specifications under [`requirements/features/`](../requirements/features/README.md) |
| User interaction | UI/UX specifications under [`ui-ux/`](../ui-ux/README.md) |
| Technical implementation | Architecture documents under [`architecture/`](../architecture/README.md) |

Illustrative examples in product documents are **not** MVP commitments until
captured in a feature specification.

Current governance remains binding until Phase 4 cutover. Replacement product
sources in this rewrite stay In review.

## Related documents

- [Documentation home](../README.md)
- [Requirements](../requirements/README.md)
- [UI/UX](../ui-ux/README.md)
- [Architecture](../architecture/README.md)
