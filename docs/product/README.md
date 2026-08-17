# Product documentation

Stable product strategy, domain concepts, and MVP boundaries for Flex Agent.

## Status

**Concept model v0.5 approved; product overview and MVP scope v0.4 remain
approved.** Concept model v0.5 adds the person-like persona and honest Agent
identity boundary without expanding text-only MVP examination, enabling general
Agent authoring, or enabling voice. Product documents govern product meaning
and scope. They inform
requirements, UI/UX, and architecture but do not replace approved feature
specifications, UI/UX specifications, or ADRs within those areas of authority.

## Document metadata

| Document | Status | Version | Owner | Approvers | Last reviewed | Approval reference |
| --- | --- | --- | --- | --- | --- | --- |
| [Concept model](concept-model.md) | Approved | 0.5 | Product Lead | Product Lead, Architecture Lead | 2026-08-16 | Person-like persona and honest Agent identity boundary; v0.4 execution semantics preserved |
| [MVP scope](mvp-scope.md) | Approved | 0.4 | Product Lead | Product Lead, Architecture Lead | 2026-08-16 | Text-only P0 preserved; compatible with Concept model v0.5 |
| [Product overview](overview.md) | Approved | 0.4 | Product Lead | Product Lead, Architecture Lead | 2026-08-16 | Envelope and P0 profile summary; compatible with Concept model v0.5 |

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

Concept model v0.5, product overview and MVP scope v0.4, and all seven P0
feature specifications are the approved baseline. The baseline includes
structured Agent Invocation/Decision, optional next-timer replacement, the
P0-compatible Decision-output envelope, and the honest Agent identity boundary.

1. Preserve all seven approved P0 contracts — see [P0 authoring order](../requirements/README.md#p0-authoring-order) and [`requirements/features/`](../requirements/features/README.md)
2. Keep structured Agent Invocation/Decision, next-timer, and P0 output-envelope behavior aligned with approved Concept model v0.5, MVP scope v0.4, current requirement specifications, [ADR-012](../architecture/decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md), [ADR-013](../architecture/decisions/ADR-013-agent-requested-next-timer-replacement.md), [ADR-014](../architecture/decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md), MVP architecture v0.10, and Session runtime contract v0.5; complete remaining host, provider, and verification gates before enabling live execution
3. Apply the approved [P0 Activity journey](../ui-ux/activity-campaign-journey.md), [assessment Campaign setup interaction specification](../ui-ux/assessment-campaign-setup.md), [Submission and Attempt interaction specification](../ui-ux/submission-attempt.md), [Text Session interaction specification](../ui-ux/text-session.md), [Evidence, Evaluation, and Human Review interaction specification](../ui-ux/evidence-evaluation-human-review.md), [Result and Release interaction specification](../ui-ux/result-release.md), and shared [design system](../ui-ux/design-system/README.md); the synthetic Participant Text Session is implemented and Playwright-verified, and remaining P0 surfaces plus production hosting still block overall frontend completion
4. Keep agent and harness **selection and assessment-required parameters** inside assessment setup — not general agent or harness management (P1)
5. Carry approved `PROP-AGENT-1` into the P1 Agent-library specification and any later voice or human-likeness specification; do not infer those authoring or presentation capabilities for P0
6. Record new or changed technical choices as ADRs when implementation evidence surfaces a consequential decision

## Related documents

- [Documentation home](../README.md)
- [Requirements](../requirements/README.md)
- [UI/UX](../ui-ux/README.md)
- [Architecture](../architecture/README.md)
