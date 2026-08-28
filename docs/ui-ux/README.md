# UI/UX documentation

User interface, user experience, interaction design, and design-system documentation for Flex Agent.

Product meaning and scope boundaries live under [product documentation](../product/README.md). UI/UX documents implement and extend approved requirements; they do not override acceptance criteria or redefine canonical concepts.

## Status

**Replacement P0 journey and interaction specifications are Approved v1.0**
after the Shipboard production UX reset. Former versions at Git `eb9c398` are
**retired** and are not current authority; see the
[retirement ledger](retired-authority.md).

The [design system](design-system/README.md) remains **Approved v1.0 Shipboard
Terminal** visual authority. The design lab is isolated composition evidence
and is not a production journey specification.

Voice interaction, interruption, playback, TTS, and the proposed text
Interaction Controller are **unavailable in P0** until a separate product
decision expands scope.

## Purpose

This area governs how users experience the product: journeys, information architecture, interaction states, accessibility, responsive behavior, content, and visual design.

## Authority during and after the reset

| Status | Meaning |
| --- | --- |
| Approved | Current UI/UX authority for the named concern |
| Draft | Not authoritative for implementation |
| Superseded | Replaced inside this area; retained only if still in the live tree |
| Retired | Former authority; recover full text from Git via the [retirement ledger](retired-authority.md) |

Technical topology (single SPA, fail-closed publication, design-lab isolation)
is governed by
[ADR-021](../architecture/decisions/ADR-021-production-frontend-reset-and-single-spa-topology.md),
not by this index.

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
| Retirement ledger | Provenance only; not behavioral authority |

## Relationship to requirements

UI/UX documents implement and extend approved requirements; they do not override acceptance criteria. Link interaction specs to `AC-*` IDs from feature specifications.

## Document catalog

| Document | Type | Status | Governs |
| --- | --- | --- | --- |
| [Retired UI/UX authority](retired-authority.md) | Retirement ledger | Approved | Former document identity, Git provenance, successor rule; not a journey specification |
| [Flex Agent activity journey and MVP campaign information architecture](activity-campaign-journey.md) | Platform IA and end-to-end P0 journey | Approved v1.0 | Generic Activity navigation; assessment Campaign journey; capability-scoped navigation; canonical routes; state handoffs; shared interaction principles |
| [Assessment Campaign setup interaction specification](assessment-campaign-setup.md) | P0 surface interaction specification | Approved v1.0 | Activity-administrator draft, readiness, activation, recovery, immutable-baseline, accessibility, responsive, and protected-content behavior |
| [Submission and Attempt interaction specification](submission-attempt.md) | P0 surface interaction specification | Approved v1.0 | Administrator Enrollment, bounded accommodation, distinct-actor fairness-exception approval, baseline/effective timing and timezone-fallback interaction; Participant Submission preparation, intake, immutable accepted versions, Attempt readiness/start/recovery |
| [Text Session interaction specification](text-session.md) | P0 surface interaction specification | Approved v1.0 | Participant pre-start acknowledgment, committed Session entry, intentional no-action, internal next-timer behavior, durable token-by-token Agent-response streaming, Agent work state, timing, reconnect, partial-stream recovery, pause, completion, terminal transcript access, administrator control. Voice and Interaction Controller controls are absent |
| [Evidence, Evaluation, and Human Review interaction specification](evidence-evaluation-human-review.md) | P0 surface interaction specification | Approved v1.0 | Assigned Review work, Evaluation processing and candidate lineage, criterion/Evidence inspection, optional Human revision, Review decision, and Result-ready/not-released handoff |
| [Result and Release interaction specification](result-release.md) | P0 surface interaction specification | Approved v1.0 | Release work, immutable Result preview, explicit Release confirmation and reconciliation, Participant pre-release/released/corrected/unavailable Results |
| [Flex Agent design system](design-system/README.md) | Shared visual, interaction, accessibility, and product-pattern foundation | Approved v1.0 | Shipboard Terminal visual language; semantic tokens, foundations, reusable components, cross-surface product patterns. Does not authorize production capability |

## Related documents

- [Documentation home](../README.md)
- [Product documentation](../product/README.md)
- [Concept model](../product/concept-model.md)
- [Requirements](../requirements/README.md)
