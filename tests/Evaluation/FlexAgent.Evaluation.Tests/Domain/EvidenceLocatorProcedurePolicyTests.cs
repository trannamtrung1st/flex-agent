using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceLocatorProcedurePolicyTests
{
    private static readonly EvaluationProcedureV1 Procedure =
        EvaluationProcedureTestFixtures.LoadP0TextSynthetic();

    [Fact]
    public void Resolves_whole_item_fallback_from_matching_criterion()
    {
        var result = EvidenceLocatorProcedurePolicy.TryResolveWholeItemFallbackPermitted(
            Procedure,
            "crit.objective.word-count");

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.True(result.Value);
    }

    [Fact]
    public void Unknown_criterion_is_rejected()
    {
        var result = EvidenceLocatorProcedurePolicy.TryResolveWholeItemFallbackPermitted(
            Procedure,
            "crit.unknown");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidProcedure, result.OutcomeCode);
        Assert.Equal("criterion_id", result.Field);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_criterion_id_is_rejected(string criterionId)
    {
        var result = EvidenceLocatorProcedurePolicy.TryResolveWholeItemFallbackPermitted(
            Procedure,
            criterionId);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
        Assert.Equal("criterion_id", result.Field);
    }
}
