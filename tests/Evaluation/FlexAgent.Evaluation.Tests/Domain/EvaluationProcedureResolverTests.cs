using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluationProcedureResolverTests
{
    private static readonly string FixturesRoot = Path.Combine(
        AppContext.BaseDirectory,
        "contracts",
        "fixtures",
        "schema",
        "v1",
        "evaluation",
        "evaluation-procedure");

    [Fact]
    public void Valid_three_mode_procedure_resolves()
    {
        var utf8 = File.ReadAllBytes(Path.Combine(FixturesRoot, "valid-three-mode-synthetic.json"));
        var result = EvaluationProcedureResolver.TryResolve(utf8);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(3, result.Value!.Criteria.Count);
        Assert.Equal(EvaluatorModes.Deterministic, result.Value.Criteria[0].EvaluatorMode);
        Assert.Equal(EvaluatorModes.AgentAssisted, result.Value.Criteria[1].EvaluatorMode);
        Assert.Equal(EvaluatorModes.AgentJudgment, result.Value.Criteria[2].EvaluatorMode);
    }

    [Theory]
    [InlineData("invalid-mutable-alias.json")]
    [InlineData("invalid-unknown-evaluator-and-status.json")]
    [InlineData("invalid-executable-network-fields.json")]
    public void Invalid_procedure_documents_are_rejected(string fileName)
    {
        var utf8 = File.ReadAllBytes(Path.Combine(FixturesRoot, fileName));
        var result = EvaluationProcedureResolver.TryResolve(utf8);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidProcedure, result.OutcomeCode);
    }
}
