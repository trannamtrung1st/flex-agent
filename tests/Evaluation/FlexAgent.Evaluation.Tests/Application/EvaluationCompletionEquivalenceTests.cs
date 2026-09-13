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
}
