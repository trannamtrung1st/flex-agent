using System.Text;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class VerifiedDeterministicOutputConflictInterpreterTests
{
    private static readonly EvaluationProcedureCriterionV1 AssistedCriterion =
        EvaluationFixtures.LoadSyntheticProcedure().Criteria.Single(item =>
            string.Equals(item.CriterionId, "crit.assisted.structure", StringComparison.Ordinal));

    [Fact]
    public void Compact_false_valid_reports_conflict()
    {
        Assert.True(IndicatesConflict("""{"valid":false}"""));
    }

    [Fact]
    public void Spaced_false_valid_reports_conflict()
    {
        Assert.True(IndicatesConflict("""{"valid": false}"""));
    }

    [Fact]
    public void Reordered_properties_with_spaced_false_valid_reports_conflict()
    {
        Assert.True(IndicatesConflict("""{"schema":"eval.agent.assisted.input.v1","valid": false}"""));
    }

    [Fact]
    public void True_valid_does_not_report_conflict()
    {
        Assert.False(IndicatesConflict("""{"valid":true,"schema":"eval.agent.assisted.input.v1"}"""));
    }

    [Fact]
    public void Nested_valid_false_substring_does_not_manufacture_conflict()
    {
        Assert.False(IndicatesConflict(
            """
            {"valid":true,"note":"literal substring valid:false must not override schema"}
            """));
    }

    [Fact]
    public void Nested_object_valid_property_does_not_manufacture_conflict()
    {
        Assert.False(IndicatesConflict("""{"valid":true,"details":{"valid":false}}"""));
    }

    [Fact]
    public void Unknown_output_schema_does_not_infer_conflict_from_substring()
    {
        var criterion = AssistedCriterion with
        {
            DeterministicEvaluator = AssistedCriterion.DeterministicEvaluator! with
            {
                OutputSchemaId = "eval.builtin.unknown.output.v1",
            },
        };

        Assert.False(VerifiedDeterministicOutputConflictInterpreter.TryIndicatesAgentOverrideConflict(
            criterion,
            """{"valid":false}"""u8));
    }

    [Theory]
    [InlineData("""{"valid": false}""", false)]
    [InlineData("""{"valid":false}""", false)]
    [InlineData("""{"valid": true}""", true)]
    [InlineData("""{"schema":"x","valid": false}""", false)]
    public void Schema_validate_reader_returns_authoritative_top_level_valid(string json, bool? expected)
    {
        Assert.Equal(
            expected,
            VerifiedDeterministicOutputConflictInterpreter.TryReadSchemaValidateValidBoolean(Encoding.UTF8.GetBytes(json)));
    }

    private static bool IndicatesConflict(string json) =>
        VerifiedDeterministicOutputConflictInterpreter.TryIndicatesAgentOverrideConflict(
            AssistedCriterion,
            Encoding.UTF8.GetBytes(json));
}
