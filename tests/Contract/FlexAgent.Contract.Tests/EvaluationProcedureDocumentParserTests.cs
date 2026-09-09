using FlexAgent.CanonicalJson;
using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Contract.Tests;

public sealed class EvaluationProcedureDocumentParserTests
{
    private static readonly string FixturesRoot = Path.Combine(
        AppContext.BaseDirectory,
        "contracts",
        "fixtures",
        "schema",
        "v1",
        "evaluation",
        "evaluation-procedure");

    private static readonly CanonicalJsonLimits Limits = new(65_536, 64, 4_096, 4_096);

    [Fact]
    public void Valid_three_mode_synthetic_procedure_parses()
    {
        var utf8 = File.ReadAllBytes(Path.Combine(FixturesRoot, "valid-three-mode-synthetic.json"));
        Assert.True(
            EvaluationProcedureDocumentParser.TryParse(utf8, out var procedure, out var failure),
            failure);
        Assert.Equal("evaluation-procedure.v1", procedure.ProcedureSchema);
        Assert.Equal("evalproc.p0.text.synthetic", procedure.ProcedureId);
        Assert.Equal(3, procedure.Criteria.Count);
        Assert.Equal("deterministic", procedure.Criteria[0].EvaluatorMode);
        Assert.Equal("agent_assisted", procedure.Criteria[1].EvaluatorMode);
        Assert.Equal("agent_judgment", procedure.Criteria[2].EvaluatorMode);
        Assert.Equal("eval.builtin.bounded-calc", procedure.Criteria[0].DeterministicEvaluator!.EvaluatorId);
        Assert.Null(procedure.Criteria[2].DeterministicEvaluator);
        Assert.NotNull(procedure.Criteria[2].AgentIo);
    }

    [Theory]
    [InlineData("invalid-mutable-alias.json")]
    [InlineData("invalid-duplicate-criteria.json")]
    [InlineData("invalid-missing-criteria.json")]
    [InlineData("invalid-missing-evaluator-mode.json")]
    [InlineData("invalid-unknown-evaluator-and-status.json")]
    [InlineData("invalid-non-positive-bounds.json")]
    [InlineData("invalid-incompatible-schema-version.json")]
    [InlineData("invalid-executable-network-fields.json")]
    public void Invalid_procedure_fixtures_are_rejected(string fileName)
    {
        var utf8 = File.ReadAllBytes(Path.Combine(FixturesRoot, fileName));
        Assert.False(EvaluationProcedureDocumentParser.TryParse(utf8, out _, out var failure));
        Assert.Equal(EvaluationProcedureDocumentParser.InvalidDocument, failure);
    }

    [Fact]
    public void Synthetic_procedure_canonical_digest_is_stable()
    {
        var utf8 = File.ReadAllBytes(Path.Combine(FixturesRoot, "valid-three-mode-synthetic.json"));
        var digest = CanonicalJsonProcessor.CanonicalizeSha256Hex(utf8, Limits);
        var canonical = CanonicalJsonProcessor.CanonicalizeUtf8(utf8, Limits);
        Assert.Equal("876757834902e87c2fe92cb77033af74ef17421bfb095ef82ccca77b7734c127", digest);
        Assert.True(EvaluationProcedureDocumentParser.TryParse(canonical, out _, out var failure), failure);
    }
}
