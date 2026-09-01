# Feature specifications

Home for Flex Agent feature specifications.

## Status

**Seven P0 specifications are Approved** after the Phase 4 governance cutover.
`REQ-*` and `AC-*` identifiers are unchanged. Deferred P1–P3 names live in
[MVP scope](../../product/mvp-scope.md). They are **not** current catalog
members and do not govern behavior.

## Purpose

Each feature spec governs one bounded, observable product outcome. Specs use the [feature spec template](../../templates/feature-spec.md) and receive stable `REQ-*` and `AC-*` IDs for traceability.

## Catalog index

| Tier | Count | Index |
| --- | --- | --- |
| P0 — MVP validation slice | 7 | [P0 spec files](#p0-spec-files) |

Deferred P1–P3 names live in [MVP scope](../../product/mvp-scope.md). There are
no placeholder specification files under this directory.

Full catalog with boundaries and product sources: [Requirements hub — Feature catalog overview](../README.md#feature-catalog-overview).

## Spec boundary rules

Avoid overlap when authoring:

```text
auth-resource-isolation          → permissions and isolation only
resolved-session-configuration   → freeze + manifest + precedence at session boundary
assessment-setup                 → activity/cohort activation freeze (before/at go-live)
submission-attempts              → enrollment, attempts, uploads (pre/during session)
session-text-lifecycle           → live session execution and participant examination
evidence-evaluation              → internal evaluation artifact (pre-release)
review-result-release            → human gate + participant-visible outcome
```

**Harness-related boundaries:**

| Spec | Governs | Does not govern |
| --- | --- | --- |
| `harness-library-configuration.md` (P1) | Basic reusable harness authoring | Snapshot compare/restore, improvement proposals |
| `workflow-stage-configuration.md` (P2) | Configurable stages and transitions | Visual workflow builders (non-goal) |
| `harness-snapshots-comparison-restoration.md` (P2) | Compare, restore, and roll out snapshots | Basic harness authoring, improvement proposals |
| `harness-improvement-proposals.md` (P3) | Controlled change proposals with review | Snapshot restore, basic authoring |

**Memory-related boundaries:**

| Spec | Governs | Does not govern |
| --- | --- | --- |
| `assessment-setup.md` (P0) | Stable memory and cohort fairness freezing at activation | Dynamic mode, candidate approval |
| `memory-governance-dynamic-mode.md` (P2) | Enable Dynamic mode with policy controls | Memory candidate approval workflows |
| `memory-candidates-learning-approval.md` (P3) | Propose and approve reusable learned artifacts | Dynamic mode enablement |

## P0 spec files

Author in this order:

| Order | File | Governs |
| --- | --- | --- |
| 1 | [`auth-resource-isolation.md`](auth-resource-isolation.md) | Authorization and activity-scope isolation |
| 2 | [`resolved-session-configuration.md`](resolved-session-configuration.md) | Resolved session configuration and execution manifest |
| 3 | [`assessment-setup.md`](assessment-setup.md) | Assessment activity setup, agent/harness selection, cohort activation, configuration and memory freezing |
| 4 | [`submission-attempts.md`](submission-attempts.md) | Enrollment, submission upload, and attempt limits |
| 5 | [`session-text-lifecycle.md`](session-text-lifecycle.md) | Text session lifecycle and examination |
| 6 | [`evidence-evaluation.md`](evidence-evaluation.md) | Evidence collection and structured evaluation |
| 7 | [`review-result-release.md`](review-result-release.md) | Human review, revision, and result release |

## P1 spec files

| Order | Name | Governs |
| --- | --- | --- |
| 8 | `agent-library-configuration.md` (deferred) | Reusable Agent identity, persona, communication behavior, and revision authoring beyond assessment-required selection |
| 9 | `harness-library-configuration.md` (deferred) | Reusable harness authoring beyond assessment-required selection |

## P2 spec files

| Order | Name | Governs |
| --- | --- | --- |
| 10 | `voice-interaction-interruption.md` (deferred) | Interruptible streaming voice with playback-confirmed continuity |
| 11 | `tool-execution-permissions.md` (deferred) | Permitted tool execution with authorization and audit |
| 12 | `workflow-stage-configuration.md` (deferred) | Configurable workflow stages and transitions |
| 13 | `harness-snapshots-comparison-restoration.md` (deferred) | Harness snapshot comparison, restoration, and rollout |
| 14 | `memory-governance-dynamic-mode.md` (deferred) | Dynamic memory mode with administrative policy controls |

## P3 spec files

| Order | Name | Governs |
| --- | --- | --- |
| 15 | `memory-candidates-learning-approval.md` (deferred) | Memory candidate proposal and approval workflows |
| 16 | `harness-improvement-proposals.md` (deferred) | Controlled harness improvement proposals |
| 17 | `shared-multi-participant-sessions.md` (deferred) | Shared real-time multi-participant sessions |
| 18 | `calibration-analytics.md` (deferred) | Calibration datasets and advanced analytics |
| 19 | `activity-deployment-forms.md` (deferred) | Direct, embedded, and API activity deployment forms |

## Authoring checklist

Before opening each draft spec:

1. Confirm the behavior is not already covered by an approved spec.
2. Take the next item from the [feature catalog](../README.md#feature-catalog-overview).
3. Replace placeholder content using the [feature spec template](../../templates/feature-spec.md).
4. Link governing [concept model](../../product/concept-model.md) and [MVP scope](../../product/mvp-scope.md) sections; use [product overview](../../product/overview.md) for vision context only.
5. Distinguish `MVP`, `Later`, `Out of scope`, `Open question`, and `Proposed`. Every open question must include an **interim default** with brief rationale (working guidance only until decided).
