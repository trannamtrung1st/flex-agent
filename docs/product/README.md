# Product documentation

Stable product strategy, domain concepts, and MVP boundaries for Flex Agent.

## Status

**Draft.** Product documents inform requirements, UI/UX, and architecture but do not replace approved feature specifications or ADRs.

## Purpose

This area governs **what the product means** — vision, positioning, principles, canonical concepts, relationships, invariants, and scope boundaries — independent of specific UI implementations or technical stacks.

Use the boundary test when choosing a document location:

| Question | If yes, document belongs in… |
| --- | --- |
| Would this remain true if the UI and technology were replaced? | **Product** (`docs/product/`) |
| Does it state observable behavior the system must provide? | **Requirements** ([`requirements/`](../requirements/README.md)) |
| Does it define user-facing interaction, content, or visual design? | **UI/UX** ([`ui-ux/`](../ui-ux/README.md)) |
| Does it explain technical realization, data ownership, or deployment? | **Architecture** ([`architecture/`](../architecture/README.md)) |

## Documents

| Document | Governs |
| --- | --- |
| [Product overview](overview.md) | Vision, positioning, principles, and navigation |
| [Concept model](concept-model.md) | Canonical definitions, relationships, lifecycles, and product invariants |
| [MVP scope](mvp-scope.md) | First product experience, platform direction, non-goals, and deferred capabilities |

## Authority

Source-of-truth order and document status definitions: [Documentation home](../README.md#source-of-truth-order).

Illustrative examples and candidate capabilities in product documents are **not** MVP commitments until captured in an approved feature spec.

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
