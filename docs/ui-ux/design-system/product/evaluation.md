# Evaluation & Review

Evaluations are structured, Evidence-backed judgments organized by rubric
criterion. The interface must preserve the relationship between **criterion →
score/outcome → summary/rationale → Evidence references → confidence →
Evaluation lineage**, while keeping Human revision and Review decision as
distinct objects.

## Evaluation Summary

A summary surface may include:

- overall score/outcome when configured
- evaluation status
- concise overall summary
- criterion coverage/progress
- confidence summary when configured
- human-review flags
- release state when results may be withheld/released

Do not visually imply that a high score is equivalent to system health/success status. Scores are evaluation data, not generic success semantics.

## Criterion Row / Panel

Each rubric criterion should expose, when configured:

- criterion name and description
- score, band, or structured outcome
- concise evaluation summary
- linked evidence/references
- criterion confidence
- reviewer flag/state
- Human revision and Review decision lineage when applicable and permitted

Use tables for comparison-heavy review and split-pane/stacked inspector patterns when evidence needs to be read alongside the criterion.

## Evidence Linkage

Evidence references follow the [Evidence pattern](evidence.md). An authorized
Reviewer must be able to trace important conclusions back to the exact
permitted turn, Submission, attachment, tool result, or other recorded Evidence.

Do not hide the only evidence trail inside a tooltip or transient popover.

## Confidence

Evaluation confidence may be expressed as configured labels or numeric values. Always include text/value; color is secondary. Low confidence is not automatically an error and should not default to danger styling.

## Human Review

Use explicit states such as `Not reviewed`, `In review`, `Review required`,
`Approved`, and `Rejected` according to
[status](../foundation/status.md). Preserve the original Evaluation. A Human
revision is an attributable, Evidence-linked object; a Review decision is the
authorized approval, rejection, or escalation. Do not present either as an
in-place overwrite of the Evaluation.

## Release State

When results can be released to participants, distinguish **evaluation/review completion** from **result release**. Do not imply that `Approved` or `Completed` automatically means the participant can already see the result.

Use explicit product wording such as `Not released` / `Released` if those states are configured.

## Rules

- Preserve rubric order and criterion identity.
- Keep score/outcome, evidence, and confidence distinguishable rather than collapsing them into one badge.
- Use tabular numerals for comparable numeric scores.
- Keep reviewer actions visually quieter than the evidence being reviewed until action is required.
- Expose permitted Human revision and Review decision history without changing
  the original Evaluation.
- Evaluation UI remains calmer than live interaction; avoid cinematic Agent Core motion inside dense review grids.
