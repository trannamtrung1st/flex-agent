using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluationOwnershipTests
{
    [Fact]
    public void Complete_parent_chain_is_accepted()
    {
        var result = EvaluationOwnership.TryCreate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.True(result.Succeeded);
        Assert.NotEqual(Guid.Empty, result.Value!.SessionId);
    }

    [Fact]
    public void Empty_session_id_is_rejected()
    {
        var result = EvaluationOwnership.TryCreate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.IncompleteOwnership, result.OutcomeCode);
    }
}
