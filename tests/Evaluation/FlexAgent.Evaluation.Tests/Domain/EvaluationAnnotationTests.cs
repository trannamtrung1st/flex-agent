using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluationAnnotationTests
{
    [Fact]
    public void Integrity_annotation_is_append_only_and_does_not_mutate_completion()
    {
        var result = EvaluationAnnotation.TryCreate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            EvaluationAnnotationKinds.SourceIntegrityChanged,
            EvaluationDispositions.AttentionRequired,
            "source.digest.drifted",
            EvaluationActorTypes.Service,
            "evaluation-service",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        Assert.True(result.Succeeded);
        Assert.Equal(EvaluationAnnotationKinds.SourceIntegrityChanged, result.Value!.Kind);
    }

    [Fact]
    public void Non_utc_annotation_time_is_rejected()
    {
        var result = EvaluationAnnotation.TryCreate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            EvaluationAnnotationKinds.LifecycleHold,
            EvaluationDispositions.Held,
            "legal.hold.active",
            EvaluationActorTypes.System,
            "lifecycle-service",
            new DateTimeOffset(2026, 9, 8, 0, 0, 0, TimeSpan.FromHours(7)));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
    }
}

public sealed class DisabledEvaluationAdmissionTests
{
    [Fact]
    public void Admission_stays_fail_closed_until_persistence_exists()
    {
        var frozen = EvaluationFixtures.CreateFrozenInput().Value!;
        var result = new DisabledEvaluationAdmission().Admit(frozen, "idem-eval-0001", "deleg.eval.synthetic");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProcessingDisabled, result.OutcomeCode);
        Assert.False(EvaluationInfrastructure.ProcessingEnabled);
    }
}
