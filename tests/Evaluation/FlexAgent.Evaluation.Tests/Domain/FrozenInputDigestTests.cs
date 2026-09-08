using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class FrozenInputDigestTests
{
    [Fact]
    public void Equivalent_frozen_input_has_a_stable_canonical_digest()
    {
        var first = EvaluationFixtures.CreateFrozenInput().Value!;
        var second = EvaluationFixtures.CreateFrozenInput().Value!;

        var firstDigest = FrozenInputDigest.Compute(first);
        var secondDigest = FrozenInputDigest.Compute(second);

        Assert.Equal(firstDigest, secondDigest);
        Assert.Matches("^[0-9a-f]{64}$", firstDigest);
    }

    [Fact]
    public void Ownership_change_changes_the_canonical_digest()
    {
        var input = EvaluationFixtures.CreateFrozenInput().Value!;
        var foreign = input with
        {
            Ownership = input.Ownership with { SessionId = Guid.NewGuid() },
        };

        Assert.NotEqual(FrozenInputDigest.Compute(input), FrozenInputDigest.Compute(foreign));
    }
}
