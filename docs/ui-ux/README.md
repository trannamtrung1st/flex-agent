# UI/UX documentation

User interface, user experience, interaction design, and design-system documentation for Flex Agent.

Product meaning and scope boundaries live under [product documentation](../product/README.md). UI/UX documents implement and extend approved requirements; they do not override acceptance criteria or redefine canonical concepts.

## Status

**The platform Activity IA, end-to-end P0 assessment Campaign journey, and
assessment Campaign setup interaction specification are approved.** All seven
P0 feature specifications, the
[MVP operational defaults](../requirements/mvp-operational-defaults.md), and the
[MVP architecture](../architecture/mvp-architecture.md) are approved, so entry
criteria are met for the complete MVP workflow. Apply the approved
[Flex Agent activity journey and MVP campaign information architecture](activity-campaign-journey.md)
and the approved
[assessment Campaign setup interaction specification](assessment-campaign-setup.md),
then author the remaining interaction specifications for Submission intake,
text Session execution, Evaluation and human review, and Result Release. Trace
every state and interaction to the owning `AC-*` criteria and preserve the
SPA/server authority boundary defined by `AR-DEC-12`.

## Purpose

This area governs how users experience the product: journeys, information architecture, interaction states, accessibility, responsive behavior, content, and visual design.

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

## Document catalog

| Document | Type | Status | Governs |
| --- | --- | --- | --- |
| [Flex Agent activity journey and MVP campaign information architecture](activity-campaign-journey.md) | Platform IA and end-to-end P0 journey | Approved | Generic Activity navigation; assessment Campaign journey; capability-scoped navigation; state handoffs; and shared interaction principles |
| [Assessment Campaign setup interaction specification](assessment-campaign-setup.md) | P0 surface interaction specification | Approved | Activity-administrator draft, readiness, activation, recovery, immutable-baseline, accessibility, responsive, and protected-content behavior |

## Related documents

- [Documentation home](../README.md)
- [Product documentation](../product/README.md)
- [Concept model](../product/concept-model.md)
- [Requirements](../requirements/README.md)
