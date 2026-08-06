# Product documentation

Stable product strategy, domain concepts, and MVP boundaries for Flex Agent.

## Status

**Approved v0.1.** Product documents govern product meaning and scope. They inform requirements, UI/UX, and architecture but do not replace approved feature specifications, UI/UX specifications, or ADRs within those areas of authority.

## Document metadata

| Document | Status | Version | Owner | Approvers | Last reviewed | Approval reference |
| --- | --- | --- | --- | --- | --- | --- |
| [Concept model](concept-model.md) | Approved | 0.1 | Product Lead | Product Lead, Architecture Lead | 2026-08-05 | Baseline v0.1 on `main` (2026-08-05) |
| [MVP scope](mvp-scope.md) | Approved | 0.1 | Product Lead | Product Lead, Architecture Lead | 2026-08-05 | Baseline v0.1 on `main` (2026-08-05) |
| [Product overview](overview.md) | Approved | 0.1 | Product Lead | Product Lead, Architecture Lead | 2026-08-05 | Baseline v0.1 on `main` (2026-08-05) |

## Purpose

This area governs **what the product means** — vision, positioning, principles, canonical concepts, relationships, invariants, and scope boundaries — independent of specific UI implementations or technical stacks.

Use the boundary test when choosing a document location:

| Question | If yes, document belongs in… |
| --- | --- |
| Would this remain true if the UI and technology were replaced? | **Product** (`docs/product/`) |
| Does it state observable behavior the system must provide? | **Requirements** ([`requirements/`](../requirements/README.md)) |
| Does it define user-facing interaction, content, or visual design? | **UI/UX** ([`ui-ux/`](../ui-ux/README.md)) |
| Does it explain technical realization, data ownership, or deployment? | **Architecture** ([`architecture/`](../architecture/README.md)) |

Technical component designs belong in architecture. The product layer states outcome-oriented contracts; architecture chooses implementation.

## Documents

| Document | Governs |
| --- | --- |
| [Product overview](overview.md) | Vision, positioning, validation strategy, principles, and navigation |
| [Concept model](concept-model.md) | Canonical definitions, configuration precedence, lifecycles, and product invariants |
| [MVP scope](mvp-scope.md) | Executable MVP slice, release tiers, non-goals, and deferred capabilities |

## Authority

Documents govern by concern. A document overrides another only within its area of authority. Full rules: [Documentation home — Authority by concern](../README.md#authority-by-concern).

| Concern | Authoritative source |
| --- | --- |
| Product meaning and scope | Approved [concept model](concept-model.md) and [MVP scope](mvp-scope.md) |
| Vision and validation strategy | Approved [product overview](overview.md) |
| Observable system behavior | Approved feature specifications under [`requirements/features/`](../requirements/features/README.md) |
| User interaction | Approved UI/UX specifications under [`ui-ux/`](../ui-ux/README.md) |
| Technical implementation | Approved ADRs under [`architecture/decisions/`](../architecture/decisions/README.md) |

Illustrative examples in product documents are **not** MVP commitments until captured in an approved feature spec.

## Next actions

Product documentation for v0.1 is complete. **Do not expand product docs further** until P0 feature specifications are approved.

1. Author and review P0 specs in order — see [P0 authoring order](../requirements/README.md#p0-authoring-order); P0 #1 [`auth-resource-isolation.md`](../requirements/features/auth-resource-isolation.md), P0 #2 [`resolved-session-configuration.md`](../requirements/features/resolved-session-configuration.md), and P0 #3 [`assessment-setup.md`](../requirements/features/assessment-setup.md) are `Approved`, and P0 #4–#7 remain placeholders under [`requirements/features/`](../requirements/features/README.md)
2. Replace P0 #4 [`submission-attempts.md`](../requirements/features/submission-attempts.md) using the [feature spec template](../templates/feature-spec.md)
3. Keep agent and harness **selection and assessment-required parameters** inside assessment setup — not general agent or harness management (P1)
4. Record technical choices as ADRs when specs surface implementation decisions

## Related documents

- [Documentation home](../README.md)
- [Requirements](../requirements/README.md)
- [UI/UX](../ui-ux/README.md)
- [Architecture](../architecture/README.md)
