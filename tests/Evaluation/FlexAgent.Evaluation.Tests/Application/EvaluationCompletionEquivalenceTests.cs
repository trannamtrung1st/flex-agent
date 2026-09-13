using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationCompletionEquivalenceTests
{
    [Fact]
    public void Json_values_equivalent_across_serialization_formatting()
    {
        Assert.True(EvaluationCompletionEquivalence.JsonValuesEquivalent(
            """["evaluator_bound"]""",
            """
            [
              "evaluator_bound"
            ]
            """));
    }

    [Fact]
    public void Json_values_differ_for_semantically_distinct_payloads()
    {
        Assert.False(EvaluationCompletionEquivalence.JsonValuesEquivalent(
            """["evaluator_bound"]""",
            """["ambiguous_language"]"""));
    }

    [Fact]
    public void Completed_at_equivalent_after_postgres_microsecond_truncation()
    {
        var stored = EvaluationCompletionTimestampCanonicalization.ToPostgresUtc(
            DateTimeOffset.Parse("2026-09-13T12:34:56.7891234+00:00"));
        var retry = DateTimeOffset.Parse("2026-09-13T12:34:56.7891234+00:00");

        Assert.True(EvaluationCompletionTimestampCanonicalization.AreEquivalent(retry, stored));
    }

    [Fact]
    public void Judgments_are_not_equivalent_when_evidence_ids_differ()
    {
        var judgmentId = Guid.CreateVersion7();
        var evidenceA = Guid.CreateVersion7();
        var evidenceB = Guid.CreateVersion7();
        var evaluationId = Guid.CreateVersion7();
        var deterministicAttemptId = Guid.CreateVersion7();
        var command = CreateJudgment(judgmentId, evaluationId, deterministicAttemptId, [evidenceA]);
        var stored = CreateStored(judgmentId, deterministicAttemptId, [evidenceB]);

        Assert.False(EvaluationCompletionEquivalence.JudgmentsEquivalent([command], [stored]));
    }

    [Fact]
    public void Judgments_are_equivalent_when_evidence_ids_match_in_order()
    {
        var judgmentId = Guid.CreateVersion7();
        var evaluationId = Guid.CreateVersion7();
        var deterministicAttemptId = Guid.CreateVersion7();
        var evidenceA = Guid.CreateVersion7();
        var evidenceB = Guid.CreateVersion7();
        var command = CreateJudgment(judgmentId, evaluationId, deterministicAttemptId, [evidenceA, evidenceB]);
        var stored = CreateStored(judgmentId, deterministicAttemptId, [evidenceA, evidenceB]);

        Assert.True(EvaluationCompletionEquivalence.JudgmentsEquivalent([command], [stored]));
    }

    private static CriterionJudgment CreateJudgment(
        Guid judgmentId,
        Guid evaluationId,
        Guid deterministicAttemptId,
        IReadOnlyList<Guid> evidenceIds) =>
        new(
            judgmentId,
            evaluationId,
            "crit.objective.word-count",
            "crit.objective.word-count.v1",
            EvaluatorModes.Deterministic,
            CriterionStatuses.Satisfied,
            "high",
            ["evaluator_bound"],
            "Rationale text.",
            evidenceIds,
            null,
            null,
            deterministicAttemptId);

    private static StoredJudgmentSnapshot CreateStored(
        Guid judgmentId,
        Guid deterministicAttemptId,
        IReadOnlyList<Guid> evidenceIds) =>
        new(
            judgmentId,
            "crit.objective.word-count",
            "crit.objective.word-count.v1",
            EvaluatorModes.Deterministic,
            CriterionStatuses.Satisfied,
            "high",
            """["evaluator_bound"]""",
            "Rationale text.",
            null,
            null,
            deterministicAttemptId,
            evidenceIds);
}
