using System.Text;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationModelExecutionServiceTests
{
    private static readonly EvaluationProcedureV1 Procedure = EvaluationFixtures.LoadSyntheticProcedure();
    private static readonly EvaluationModelExecutionService Service = new();

    [Fact]
    public async Task Deterministic_criterion_is_rejected_before_model_execution()
    {
        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.objective.word-count", "crit.objective.word-count.v1"),
            verifiedDeterministicFacts: null,
            EvaluationFixtures.Model(),
            CreateAttempt("crit.objective.word-count", "crit.objective.word-count.v1", EvaluatorModes.Deterministic),
            new SyntheticEvaluationModelExecutionAdapter(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("evaluator_mode", result.Field);
    }

    [Fact]
    public async Task Agent_assisted_execution_requires_verified_deterministic_facts()
    {
        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            verifiedDeterministicFacts: null,
            EvaluationFixtures.Model(),
            CreateAttempt("crit.assisted.structure", "crit.assisted.structure.v1", EvaluatorModes.AgentAssisted),
            new SyntheticEvaluationModelExecutionAdapter(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public async Task Agent_assisted_execution_succeeds_with_synthetic_adapter_and_verified_facts()
    {
        var facts = new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
        {
            ["detfact.synthetic.schema"] = new(
                "detfact.synthetic.schema",
                "detfact.synthetic.schema.v1",
                new string('c', 64),
                Encoding.UTF8.GetBytes("""{"valid":true}""")),
        };

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            EvaluationFixtures.Model(),
            CreateAttempt("crit.assisted.structure", "crit.assisted.structure.v1", EvaluatorModes.AgentAssisted),
            new SyntheticEvaluationModelExecutionAdapter(),
            CancellationToken.None);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(EvaluatorModes.AgentAssisted, result.Value!.EvaluatorMode);
        Assert.Equal(CriterionStatuses.Satisfied, result.Value.Status);
    }

    [Fact]
    public async Task Fail_closed_port_denies_model_execution()
    {
        var facts = new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
        {
            ["detfact.synthetic.schema"] = new(
                "detfact.synthetic.schema",
                "detfact.synthetic.schema.v1",
                new string('c', 64),
                Encoding.UTF8.GetBytes("""{"valid":true}""")),
        };

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            EvaluationFixtures.Model(),
            CreateAttempt("crit.assisted.structure", "crit.assisted.structure.v1", EvaluatorModes.AgentAssisted),
            new FailClosedEvaluationModelExecutionPort(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProcessingDisabled, result.OutcomeCode);
    }

    [Fact]
    public async Task Synthetic_adapter_can_return_insufficient_evidence()
    {
        var facts = new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
        {
            ["detfact.synthetic.schema"] = new(
                "detfact.synthetic.schema",
                "detfact.synthetic.schema.v1",
                new string('c', 64),
                Encoding.UTF8.GetBytes("""{"valid":true}""")),
        };

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            EvaluationFixtures.Model(),
            CreateAttempt(
                "crit.assisted.structure",
                "crit.assisted.structure.v1",
                EvaluatorModes.AgentAssisted,
                syntheticScenario: "insufficient"),
            new SyntheticEvaluationModelExecutionAdapter(),
            CancellationToken.None);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(CriterionStatuses.InsufficientEvidence, result.Value!.Status);
    }

    private static EvaluationModelAttemptRequest CreateAttempt(
        string criterionId,
        string criterionVersion,
        string evaluatorMode,
        string? syntheticScenario = null) =>
        new(
            EvaluationFixtures.Ownership(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            criterionId,
            criterionVersion,
            evaluatorMode,
            "eval.agent.placeholder.input.v1",
            "eval.agent.placeholder.output.v1",
            EvaluationFixtures.Model(),
            AttemptOrdinal: 1,
            MaxResponseUtf8Bytes: 16_384,
            SyntheticScenario: syntheticScenario);
}
