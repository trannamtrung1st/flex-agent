using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationCompletionEquivalenceTests
{
    [Fact]
    public void Json_values_equivalent_across_serialization_formatting()
    {
        Assert.True(EvaluationCompletionEquivalence.JsonValuesEquivalent(
            """["evaluator_bound"]""",
            """
            [
              "evaluator_bound"
            ]
            """));
    }

    [Fact]
    public void Json_values_differ_for_semantically_distinct_payloads()
    {
        Assert.False(EvaluationCompletionEquivalence.JsonValuesEquivalent(
            """["evaluator_bound"]""",
            """["ambiguous_language"]"""));
    }

    [Fact]
    public void Completed_at_equivalent_after_postgres_microsecond_truncation()
    {
        var stored = EvaluationCompletionTimestampCanonicalization.ToPostgresUtc(
            DateTimeOffset.Parse("2026-09-13T12:34:56.7891234+00:00"));
        var retry = DateTimeOffset.Parse("2026-09-13T12:34:56.7891234+00:00");

        Assert.True(EvaluationCompletionTimestampCanonicalization.AreEquivalent(retry, stored));
    }
}
