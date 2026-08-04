# Requirements

Requirements hub for Flex Agent. This area governs what the product must do, who may do it, and how success is verified.

Product meaning — concepts, relationships, and scope boundaries — lives under [product documentation](../product/README.md). Requirements implement observable behavior derived from that model; they do not redefine canonical concepts.

## Status

**Scaffold only.** No feature specifications are approved yet. The catalog below identifies candidate areas derived from [MVP scope](../product/mvp-scope.md) and the [concept model](../product/concept-model.md); catalog entries are not requirements until captured in an approved spec.

## Requirements lifecycle

```mermaid
flowchart LR
  productModel[Product model] --> catalog[Feature catalog]
  catalog --> draft[Draft spec]
  draft --> review[In review]
  review --> approved[Approved spec]
  approved --> implemented[Implemented]
  approved --> trace[Traceability]
```

1. **Discover** — Identify behavior from [concept model](../product/concept-model.md), [MVP scope](../product/mvp-scope.md), stakeholder input, or gaps in existing specs.
2. **Draft** — Author a feature spec using the [feature spec template](../templates/feature-spec.md) with stable IDs.
3. **Review** — Validate scope, actors, journeys, failure states, security/privacy, and testability.
4. **Approve** — Mark the spec `Approved`; it becomes authoritative for implementation.
5. **Implement and verify** — Map requirements to implementation and automated/manual verification.
6. **Maintain** — Update status to `Implemented` or supersede when behavior changes.

## ID conventions

| ID pattern | Use |
| --- | --- |
| `REQ-<area>-<n>` | Normative business rule or requirement |
| `AC-<area>-<n>` | Testable acceptance criterion (Given/When/Then) |
| `Q-<n>` | Open question blocking or informing a decision |
| `PROP-<n>` | Proposed default requiring explicit approval |

**Area codes** (examples): `AGENT`, `HARNESS`, `CAMP`, `SESS`, `SUBM`, `CONV`, `VOICE`, `TOOL`, `EVAL`, `REV`, `MEM`, `AUDIT`.

## Approval expectations

An approved feature specification must include:

- Status, owner, and source references
- Problem and measurable outcome
- Actors, permissions, and resource scope
- In scope / out of scope
- User journeys and state transitions
- Business rules with stable `REQ-*` IDs
- Data, evidence, and audit requirements
- Quality requirements (UX, performance, security, privacy)
- Acceptance criteria with stable `AC-*` IDs
- Edge and failure cases
- Dependencies, rollout, and observability
- Open questions and labeled proposals
- Traceability matrix

Specs without testable acceptance criteria are not ready for approval.

## MVP feature-spec catalog

Candidate specifications for the first MVP (AI-assisted assessment and examination). Priority reflects suggested authoring order, not implementation sequence.

| Priority | Candidate spec | Source | Owner | Spec status |
| --- | --- | --- | --- | --- |
| P0 | Agent configuration and identity | [Concept model — Agent](../product/concept-model.md#agent), [Memory](../product/concept-model.md#memory) | TBD | Not started |
| P0 | Harness configuration and snapshots | [Concept model — Harness](../product/concept-model.md#harness) | TBD | Not started |
| P0 | Campaign setup and participant management | [Concept model — Campaign](../product/concept-model.md#campaign) | TBD | Not started |
| P0 | Session lifecycle and workflow | [Concept model — Session](../product/concept-model.md#session), [Workflow](../product/concept-model.md#workflow-model) | TBD | Not started |
| P0 | Participant submissions and attachments | [MVP scope](../product/mvp-scope.md) | TBD | Not started |
| P1 | Text conversation | [MVP scope — Platform capabilities](../product/mvp-scope.md#platform-capabilities-the-mvp-should-demonstrate) | TBD | Not started |
| P1 | Voice interaction and interruption | [Concept model — Voice](../product/concept-model.md#voice-interaction-model-product-level) | TBD | Not started |
| P1 | Tool execution and permissions | [MVP scope](../product/mvp-scope.md) | TBD | Not started |
| P1 | Evidence collection | [Concept model — Evidence](../product/concept-model.md#evidence) | TBD | Not started |
| P1 | Structured evaluation | [Concept model — Evaluation](../product/concept-model.md#evaluation) | TBD | Not started |
| P2 | Human review and result release | [MVP scope](../product/mvp-scope.md) | TBD | Not started |
| P2 | Memory governance | [Concept model — Memory](../product/concept-model.md#memory) | TBD | Not started |
| P2 | Audit and reproducibility | [Concept model — Product invariants](../product/concept-model.md#product-invariants) | TBD | Not started |

### Explicitly out of catalog scope (deferred)

The following capabilities are **not** cataloged as MVP specs unless explicitly promoted:

- Unrestricted full-duplex voice, video, multi-agent collaboration
- Public agent/tool marketplaces, visual workflow builders
- Automated proctoring, biometric verification, advanced cheating detection
- Complex billing, large-scale workforce scheduling
- Fully autonomous result release or harness self-modification

See [MVP scope — Explicit non-goals](../product/mvp-scope.md#explicit-non-goals-deferred).

## Actor capabilities (reference)

High-level capability lists inform future specs but are not approved requirements:

- [MVP scope — Actor capability themes](../product/mvp-scope.md#actor-capability-themes-reference)

## Feature specifications

Approved and draft feature specs live under [features/](features/README.md).

## Related documents

- [Documentation home](../README.md)
- [Product documentation](../product/README.md)
- [Concept model](../product/concept-model.md)
- [MVP scope](../product/mvp-scope.md)
- [Product overview](../product/overview.md)
- [Feature spec template](../templates/feature-spec.md)
