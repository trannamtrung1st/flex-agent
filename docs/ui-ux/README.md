# UI/UX documentation

User interface, user experience, interaction design, and design-system documentation for Flex Agent.

Product meaning and scope boundaries live under [product documentation](../product/README.md). UI/UX documents implement and extend approved requirements; they do not override acceptance criteria or redefine canonical concepts.

## Status

**The platform Activity IA, end-to-end P0 assessment Campaign journey, and all
five P0 surface interaction specifications are approved at their current
versions.** Text Session v0.5, `UI-SESS-DEC-13`, `UI-SESS-DEC-14`, and
`UI-SESS-DEC-15` govern
intentional no-action, internal next-timer presentation, and hidden
Decision-envelope internals. All seven
P0 feature specifications, the
[MVP operational defaults](../requirements/mvp-operational-defaults.md), and the
[MVP architecture](../architecture/mvp-architecture.md) are approved, so entry
criteria are met for the complete MVP workflow. Apply the approved
[Flex Agent activity journey and MVP campaign information architecture](activity-campaign-journey.md)
and the approved
[assessment Campaign setup](assessment-campaign-setup.md) and
[Submission and Attempt](submission-attempt.md) and
[Text Session](text-session.md) and
[Evidence, Evaluation, and Human Review](evidence-evaluation-human-review.md)
interaction specifications, the approved
[Result and Release](result-release.md) interaction specification, and the
approved shared [design system](design-system/README.md). Every state and
interaction must trace to the owning `AC-*` criteria
and preserve the SPA/server authority boundary defined by `AR-DEC-12`.

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
| [Submission and Attempt interaction specification](submission-attempt.md) | P0 surface interaction specification | Approved | Administrator Enrollment and fairness-exception approval interaction; Participant Submission preparation, intake, immutable accepted versions, Attempt readiness/start/recovery, accessibility, responsive, and protected-content behavior |
| [Text Session interaction specification](text-session.md) | P0 surface interaction specification | Approved v0.4 | Participant pre-start acknowledgment, committed Session entry, intentional no-action, internal next-timer behavior, durable token-by-token Agent-response streaming, Agent work state, timing, reconnect, partial-stream recovery, pause, completion, terminal transcript access, administrator control, accessibility, responsive, and protected-content behavior |
| [Evidence, Evaluation, and Human Review interaction specification](evidence-evaluation-human-review.md) | P0 surface interaction specification | Approved | Assigned Review work, Evaluation processing and candidate lineage, criterion/Evidence inspection, optional Human revision, Review decision, and Result-ready/not-released handoff |
| [Result and Release interaction specification](result-release.md) | P0 surface interaction specification | Approved | Release work, immutable Result preview, explicit Release confirmation and reconciliation, Participant pre-release/released/corrected/unavailable Results, notification handoff, accessibility, responsive behavior, and protected-content interaction |
| [Flex Agent design system](design-system/README.md) | Shared visual, interaction, accessibility, and product-pattern foundation | Approved v0.1 | Deep-Space Operational Futurism, AI Observation Glass, semantic tokens, foundations, reusable components, cross-surface product patterns, authority boundaries, later-release applicability, and implementation checklist |

## Related documents

- [Documentation home](../README.md)
- [Product documentation](../product/README.md)
- [Concept model](../product/concept-model.md)
- [Requirements](../requirements/README.md)
