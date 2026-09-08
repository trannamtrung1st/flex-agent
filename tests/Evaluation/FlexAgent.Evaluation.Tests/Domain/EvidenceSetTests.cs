using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceSetTests
{
    [Fact]
    public void Duplicate_evidence_ids_are_rejected()
    {
        var evaluationId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var item = EvidenceItem.TryCreate(
            evidenceId,
            "submission.direct_text",
            EvaluationFixtures.Submission(),
            EvaluationFixtures.Ownership(),
            evaluationId,
            "exact_range").Value!;

        var result = EvidenceSet.TryCreate(
            Guid.NewGuid(),
            evaluationId,
            [item, item],
            new string('d', 64));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DuplicateIdentity, result.OutcomeCode);
    }

    [Fact]
    public void Empty_evidence_id_is_rejected()
    {
        var result = EvidenceItem.TryCreate(
            Guid.Empty,
            "submission.direct_text",
            EvaluationFixtures.Submission(),
            EvaluationFixtures.Ownership(),
            Guid.NewGuid(),
            "exact_range");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
    }
}

public sealed class InvocationAttemptTests
{
    [Fact]
    public void Non_utc_start_time_is_rejected()
    {
        var result = InvocationAttempt.TryCreate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            EvaluationRequestStates.Running,
            new DateTimeOffset(2026, 9, 8, 2, 0, 0, TimeSpan.FromHours(7)),
            null);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
    }

    [Fact]
    public void Positive_attempt_ordinal_is_accepted()
    {
        var result = InvocationAttempt.TryCreate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            EvaluationRequestStates.Running,
            DateTimeOffset.UtcNow,
            null);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value!.AttemptOrdinal);
    }
}

public sealed class EvaluationLineageTests
{
    [Fact]
    public void Replacement_lineage_binds_exactly_one_predecessor()
    {
        var predecessor = Guid.NewGuid();
        var successor = Guid.NewGuid();
        var result = EvaluationLineage.TryCreate(
            Guid.NewGuid(),
            predecessor,
            Guid.NewGuid(),
            successor,
            "authorized.replace",
            EvaluationActorTypes.Service,
            "evaluation-service",
            DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        Assert.Equal(predecessor, result.Value!.PredecessorEvaluationId);
        Assert.Equal(successor, result.Value.SuccessorEvaluationId);
    }

    [Fact]
    public void Self_successor_lineage_is_rejected()
    {
        var evaluationId = Guid.NewGuid();
        var result = EvaluationLineage.TryCreate(
            Guid.NewGuid(),
            evaluationId,
            Guid.NewGuid(),
            evaluationId,
            "authorized.replace",
            EvaluationActorTypes.Human,
            "actor.reviewer.synthetic",
            DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
    }
}
