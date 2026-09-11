using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class ProviderArtifactProvenanceTests
{
    [Fact]
    public void Equivalent_retry_matches_full_provenance_snapshot()
    {
        var existing = CreateSnapshot();
        var candidate = existing with { };

        Assert.True(ProviderArtifactProvenance.IsEquivalentRetry(existing, candidate));
    }

    [Fact]
    public void Changed_response_ref_is_not_an_equivalent_retry()
    {
        var existing = CreateSnapshot();
        var candidate = existing with { ProtectedResponseRef = ProviderArtifactProvenance.ProtectedResponseRef(new string('f', 64)) };

        Assert.False(ProviderArtifactProvenance.IsEquivalentRetry(existing, candidate));
    }

    [Fact]
    public void Protected_request_ref_is_derived_from_request_content_digest()
    {
        var digest = new string('a', 64);

        Assert.Equal(
            $"prot.eval.model-req.{digest}",
            ProviderArtifactProvenance.ProtectedRequestRef(digest));
    }

    [Theory]
    [InlineData("succeeded", ProviderArtifactOutcomes.Succeeded)]
    [InlineData("provider_timeout", ProviderArtifactOutcomes.TimedOut)]
    [InlineData("schema_invalid", ProviderArtifactOutcomes.InvalidOutput)]
    [InlineData("provider_unavailable", ProviderArtifactOutcomes.Failed)]
    public void Execution_outcome_category_maps_to_persistence_outcome(string category, string expected)
    {
        Assert.Equal(expected, ProviderArtifactOutcomes.FromExecutionOutcomeCategory(category));
    }

    private static ProviderArtifactProvenanceSnapshot CreateSnapshot() =>
        new(
            Guid.Parse("11111111-1111-4111-8111-111111111112"),
            "crit.judgment.quality",
            "crit.judgment.quality.v1",
            EvaluationSyntheticDevelopmentModelProfile.ProfileId,
            EvaluationSyntheticDevelopmentModelProfile.ProfileVersion,
            new string('a', 64),
            EvaluationSyntheticDevelopmentModelProfile.CredentialBindingReference,
            ProviderArtifactProvenance.ProtectedRequestRef(new string('b', 64)),
            ProviderArtifactProvenance.ProtectedResponseRef(new string('c', 64)),
            ProviderArtifactOutcomes.Succeeded,
            null);
}
