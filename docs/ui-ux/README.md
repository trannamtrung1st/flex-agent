# UI/UX documentation

User interface, user experience, interaction design, and design-system documentation for Flex Agent.

Product meaning and scope boundaries live under [product documentation](../product/README.md). UI/UX documents implement and extend approved requirements; they do not override acceptance criteria or redefine canonical concepts.

## Status

**No approved documents yet.** Entry criteria for foundational UI/UX work are met by approved P0 #1 ([`auth-resource-isolation.md`](../requirements/features/auth-resource-isolation.md)), P0 #2 ([`resolved-session-configuration.md`](../requirements/features/resolved-session-configuration.md)), and P0 #3 ([`assessment-setup.md`](../requirements/features/assessment-setup.md)). P0 #3 defines setup, readiness, activation, failure, concurrency, confirmation, exception, and responsive-accessibility states. Author authoritative assessment-setup journeys and interaction specifications against its `AC-ACT-*` criteria.

## Purpose

This area will govern how users experience the product: journeys, information architecture, interaction states, accessibility, responsive behavior, content, and visual design.

## Entry criteria

Begin UI/UX documentation when:

- At least one P0 feature specification is in `Draft` or `Approved` status
- Actors, permissions, and primary journeys are defined in requirements
- [Concept model](../product/concept-model.md) actors and session concepts are understood
- Open questions that materially affect UI/UX are resolved, or each carries an **interim default** (and `Proposed`/`PROP-*` when consequential)

## Expected document types

| Type | Description |
| --- | --- |
| User journey | End-to-end flow for an actor through a bounded outcome |
| Interaction specification | States, transitions, feedback, errors, and edge cases for a surface |
| Content guide | Voice, tone, labels, messages, and empty/error copy |
| Accessibility guide | Keyboard, focus, screen reader, contrast, and accommodation patterns |
| Design system | Components, tokens, layout, and responsive breakpoints |

## Relationship to requirements

UI/UX documents implement and extend approved requirements; they do not override acceptance criteria. Link interaction specs to `AC-*` IDs from feature specifications.

## Related documents

- [Documentation home](../README.md)
- [Product documentation](../product/README.md)
- [Concept model](../product/concept-model.md)
- [Requirements](../requirements/README.md)
