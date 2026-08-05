# Feature specifications

Home for Flex Agent feature specifications.

## Status

**P0 authoring in progress.** Create specs in the order defined in [P0 authoring order](../README.md#p0-authoring-order).

## Purpose

Each feature spec governs one bounded, observable product outcome. Specs use the [feature spec template](../../templates/feature-spec.md) and receive stable `REQ-*` and `AC-*` IDs for traceability.

## P0 spec files

Create these files in authoring order:

| Order | File | Governs |
| --- | --- | --- |
| 1 | `auth-resource-isolation.md` | Authorization and activity-scope isolation |
| 2 | `resolved-session-configuration.md` | Resolved session configuration and execution manifest |
| 3 | `assessment-setup.md` | Assessment activity setup, cohort activation, configuration freezing |
| 4 | `submission-attempts.md` | Enrollment, submission upload, and attempt limits |
| 5 | `session-text-lifecycle.md` | Text session lifecycle and examination |
| 6 | `evidence-evaluation.md` | Evidence collection and structured evaluation |
| 7 | `review-result-release.md` | Human review, revision, and result release |

## Authoring checklist

Before opening each draft spec:

1. Confirm the behavior is not already covered by an approved spec.
2. Take the next item from [P0 authoring order](../README.md#p0-authoring-order).
3. Copy [feature spec template](../../templates/feature-spec.md) into the target file path above.
4. Link governing [concept model](../../product/concept-model.md) and [MVP scope](../../product/mvp-scope.md) sections; use [product overview](../../product/overview.md) for vision context only.
5. Distinguish `MVP`, `Later`, `Out of scope`, `Open question`, and `Proposed`.
