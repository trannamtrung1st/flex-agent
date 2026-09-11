using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluationModelResponseDocumentReaderTests
{
    private static readonly string FixtureDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "contracts",
        "fixtures",
        "schema",
        "v1",
        "evaluation",
        "evaluation-model-response");

    [Theory]
    [InlineData("valid-agent-assisted-satisfied.json")]
    [InlineData("valid-agent-judgment-satisfied.json")]
    public async Task Valid_catalog_fixture_passes_schema_and_parser(string fileName)
    {
        var utf8Json = await File.ReadAllBytesAsync(
            Path.Combine(FixtureDirectory, fileName),
            TestContext.Current.CancellationToken);

        var result = EvaluationModelResponseDocumentReader.Read(utf8Json);

        Assert.True(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.CriterionId));
    }

    [Theory]
    [InlineData("invalid-hidden-prompt.json")]
    [InlineData("invalid-unknown-status.json")]
    [InlineData("invalid-missing-response-ref.json")]
    [InlineData("invalid-empty-evidence-ids.json")]
    [InlineData("invalid-empty-uncertainty.json")]
    [InlineData("invalid-score-above-max.json")]
    public async Task Invalid_catalog_fixture_is_rejected_before_semantic_validation(string fileName)
    {
        var utf8Json = await File.ReadAllBytesAsync(
            Path.Combine(FixtureDirectory, fileName),
            TestContext.Current.CancellationToken);

        var result = EvaluationModelResponseDocumentReader.Read(utf8Json);

        Assert.False(result.Succeeded);
        Assert.Equal("model_response", result.Field);
    }

    [Fact]
    public void Empty_document_is_rejected()
    {
        var result = EvaluationModelResponseDocumentReader.Read([]);

        Assert.False(result.Succeeded);
        Assert.Equal("model_response", result.Field);
    }

    [Fact]
    public void Handwritten_parser_accepts_fixture_that_schema_rejects()
    {
        var utf8Json = File.ReadAllBytes(Path.Combine(FixtureDirectory, "invalid-score-above-max.json"));
        Assert.True(
            EvaluationModelResponseDocumentParser.TryParse(utf8Json, out _, out _),
            "Handwritten mapping is not the schema authority.");

        var result = EvaluationModelResponseDocumentReader.Read(utf8Json);
        Assert.False(result.Succeeded);
        Assert.Equal("model_response", result.Field);
    }
}
