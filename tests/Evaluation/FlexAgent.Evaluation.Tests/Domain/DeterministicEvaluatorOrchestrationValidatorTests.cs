using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class DeterministicEvaluatorOrchestrationValidatorTests
{
    private static readonly EvaluationProcedureV1 Procedure = EvaluationFixtures.LoadSyntheticProcedure();

    [Fact]
    public void Frozen_deterministic_criterion_and_binding_passes()
    {
        var criterion = Procedure.Criteria[0];
        var request = CreateRequest(criterion, criterion.DeterministicEvaluator!);

        var result = DeterministicEvaluatorOrchestrationValidator.TryValidateRequest(Procedure, request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(criterion.CriterionId, result.Value!.CriterionId);
    }

    [Fact]
    public void Agent_assisted_criterion_allows_matching_deterministic_binding()
    {
        var criterion = Procedure.Criteria[1];
        var request = CreateRequest(criterion, criterion.DeterministicEvaluator!);

        var result = DeterministicEvaluatorOrchestrationValidator.TryValidateRequest(Procedure, request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(EvaluatorModes.AgentAssisted, result.Value!.EvaluatorMode);
    }

    [Fact]
    public void Unknown_criterion_is_rejected()
    {
        var criterion = Procedure.Criteria[0];
        var request = CreateRequest(
            criterion with { CriterionId = "crit.unknown.objective" },
            criterion.DeterministicEvaluator!);

        var result = DeterministicEvaluatorOrchestrationValidator.TryValidateRequest(Procedure, request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("criterion", result.Field);
    }

    [Fact]
    public void Wrong_criterion_version_is_rejected()
    {
        var criterion = Procedure.Criteria[0];
        var request = CreateRequest(
            criterion with { CriterionVersion = "crit.objective.word-count.v9" },
            criterion.DeterministicEvaluator!);

        var result = DeterministicEvaluatorOrchestrationValidator.TryValidateRequest(Procedure, request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("criterion", result.Field);
    }

    [Fact]
    public void Agent_judgment_criterion_rejects_deterministic_invocation()
    {
        var criterion = Procedure.Criteria[2];
        var request = CreateRequest(
            criterion with
            {
                CriterionId = criterion.CriterionId,
                CriterionVersion = criterion.CriterionVersion,
            },
            new DeterministicEvaluatorBindingV1(
                "eval.builtin.bounded-calc",
                "eval.builtin.bounded-calc.v1",
                new string('a', 64),
                "bounded_calculation",
                "eval.builtin.bounded-calc.input.v1",
                "eval.builtin.bounded-calc.output.v1",
                "jcs-sha256-v1",
                new string('b', 64),
                new string('c', 64),
                "PT5S",
                "PT10S",
                16777216,
                4096,
                "prohibited",
                "prohibited"));

        var result = DeterministicEvaluatorOrchestrationValidator.TryValidateRequest(Procedure, request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("evaluator_mode", result.Field);
    }

    [Fact]
    public void Procedure_binding_drift_is_rejected()
    {
        var criterion = Procedure.Criteria[0];
        var drifted = criterion.DeterministicEvaluator! with
        {
            EvaluatorDigest = new string('9', 64),
        };
        var request = CreateRequest(criterion, drifted);

        var result = DeterministicEvaluatorOrchestrationValidator.TryValidateRequest(Procedure, request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("evaluator_digest", result.Field);
    }

    private static DeterministicEvaluatorExecutionRequest CreateRequest(
        EvaluationProcedureCriterionV1 criterion,
        DeterministicEvaluatorBindingV1 binding) =>
        new(
            EvaluationFixtures.Ownership(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            criterion.CriterionId,
            criterion.CriterionVersion,
            binding,
            new DeterministicEvaluatorCanonicalInput(
                ReadOnlyMemory<byte>.Empty,
                new string('d', 64)));
}
