using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluationRequestTests
{
    [Fact]
    public void Initial_request_cannot_bind_a_predecessor()
    {
        var frozen = EvaluationFixtures.CreateFrozenInput().Value!;
        var result = EvaluationRequest.TryCreate(
            Guid.NewGuid(),
            EvaluationRequestKinds.Initial,
            frozen,
            "idem-eval-0001",
            "deleg.eval.synthetic",
            EvaluationRequestStates.Queued,
            Guid.NewGuid(),
            "authorized.replace");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
    }

    [Fact]
    public void Replacement_requires_predecessor_and_reason()
    {
        var frozen = EvaluationFixtures.CreateFrozenInput().Value!;
        var missing = EvaluationRequest.TryCreate(
            Guid.NewGuid(),
            EvaluationRequestKinds.Replacement,
            frozen,
            "idem-eval-0002",
            "deleg.eval.synthetic",
            EvaluationRequestStates.Queued);
        var valid = EvaluationRequest.TryCreate(
            Guid.NewGuid(),
            EvaluationRequestKinds.Replacement,
            frozen,
            "idem-eval-0002",
            "deleg.eval.synthetic",
            EvaluationRequestStates.Queued,
            Guid.NewGuid(),
            "authorized.replace");

        Assert.False(missing.Succeeded);
        Assert.True(valid.Succeeded);
        Assert.Equal(EvaluationRequestKinds.Replacement, valid.Value!.RequestKind);
    }
}
