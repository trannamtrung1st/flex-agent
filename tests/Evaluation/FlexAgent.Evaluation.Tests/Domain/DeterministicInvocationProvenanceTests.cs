using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class DeterministicInvocationProvenanceTests
{
    [Fact]
    public void Equivalent_retry_matches_full_provenance_snapshot()
    {
        var existing = CreateSnapshot();
        var candidate = existing with { };

        Assert.True(DeterministicInvocationProvenance.IsEquivalentRetry(existing, candidate));
    }

    [Fact]
    public void Changed_output_digest_is_not_an_equivalent_retry()
    {
        var existing = CreateSnapshot();
        var candidate = existing with { OutputContentDigest = new string('f', 64) };

        Assert.False(DeterministicInvocationProvenance.IsEquivalentRetry(existing, candidate));
    }

    [Fact]
    public void Changed_invocation_attempt_is_not_an_equivalent_retry()
    {
        var existing = CreateSnapshot();
        var candidate = existing with { InvocationAttemptId = Guid.CreateVersion7() };

        Assert.False(DeterministicInvocationProvenance.IsEquivalentRetry(existing, candidate));
    }

    [Fact]
    public void Changed_evaluator_digest_is_not_an_equivalent_retry()
    {
        var existing = CreateSnapshot();
        var candidate = existing with { EvaluatorDigest = new string('9', 64) };

        Assert.False(DeterministicInvocationProvenance.IsEquivalentRetry(existing, candidate));
    }

    [Fact]
    public void Protected_output_ref_is_derived_from_output_content_digest()
    {
        var digest = new string('a', 64);

        Assert.Equal(
            $"prot.eval.det-out.{digest}",
            DeterministicInvocationProvenance.ProtectedOutputRef(digest));
    }

    private static DeterministicInvocationProvenanceSnapshot CreateSnapshot() =>
        new(
            Guid.Parse("11111111-1111-4111-8111-111111111112"),
            "crit.objective.word-count",
            "crit.objective.word-count.v1",
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest,
            BuiltinEvaluatorRegistry.BoundedCalcConfigurationDigest,
            BuiltinEvaluatorRegistry.BoundedCalcDependencyDigest,
            new string('d', 64),
            DeterministicInvocationProvenance.ProtectedInputRef(new string('d', 64)),
            DeterministicInvocationProvenance.ProtectedOutputRef(new string('e', 64)),
            new string('e', 64),
            DeterministicInvocationOutcomes.ToPersistenceOutcome(DeterministicInvocationOutcomes.Succeeded),
            null);
}
