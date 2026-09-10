using System.Text;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class AgentEvaluatorOrchestrationValidatorTests
{
    private static readonly EvaluationProcedureV1 Procedure = EvaluationFixtures.LoadSyntheticProcedure();

    [Fact]
    public void Agent_judgment_criterion_allows_model_invocation_without_deterministic_facts()
    {
        var result = AgentEvaluatorOrchestrationValidator.TryValidateInvocation(
            Procedure,
            new EvaluationModelInvocationContext("crit.judgment.quality", "crit.judgment.quality.v1"),
            verifiedDeterministicFacts: null);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(EvaluatorModes.AgentJudgment, result.Value!.EvaluatorMode);
    }

    [Fact]
    public void Agent_assisted_criterion_requires_verified_deterministic_facts()
    {
        var result = AgentEvaluatorOrchestrationValidator.TryValidateInvocation(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            verifiedDeterministicFacts: null);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public void Agent_assisted_criterion_passes_when_verified_deterministic_facts_are_present()
    {
        var facts = new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
        {
            ["detfact.synthetic.schema"] = CreateFact("""{"valid":true}"""),
        };

        var result = AgentEvaluatorOrchestrationValidator.TryValidateInvocation(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts);

        Assert.True(result.Succeeded, result.OutcomeCode);
    }

    [Fact]
    public void Deterministic_criterion_rejects_model_invocation()
    {
        var result = AgentEvaluatorOrchestrationValidator.TryValidateInvocation(
            Procedure,
            new EvaluationModelInvocationContext("crit.objective.word-count", "crit.objective.word-count.v1"),
            verifiedDeterministicFacts: null);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("evaluator_mode", result.Field);
    }

    [Fact]
    public void Agent_judgment_rejects_non_empty_deterministic_fact_context()
    {
        var facts = new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
        {
            ["detfact.synthetic.schema"] = CreateFact("""{"valid":true}"""),
        };

        var result = AgentEvaluatorOrchestrationValidator.TryValidateInvocation(
            Procedure,
            new EvaluationModelInvocationContext("crit.judgment.quality", "crit.judgment.quality.v1"),
            facts);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    private static EvaluationSafeFactProjection CreateFact(string json) =>
        new(
            "detfact.synthetic.schema",
            "detfact.synthetic.schema.v1",
            new string('c', 64),
            Encoding.UTF8.GetBytes(json));
}
