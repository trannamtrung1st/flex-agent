using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class DeterministicEvaluatorExecutionServiceTests
{
    [Fact]
    public async Task Orchestration_rejects_agent_judgment_before_runner_or_store()
    {
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();
        var criterion = procedure.Criteria[2];
        var registry = new CountingRegistry();
        var runner = new CountingRunner();
        var store = new CountingStore();
        var service = new DeterministicEvaluatorExecutionService(registry, runner, store);
        var request = new DeterministicEvaluatorExecutionRequest(
            EvaluationFixtures.Ownership(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            criterion.CriterionId,
            criterion.CriterionVersion,
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
                "prohibited"),
            new DeterministicEvaluatorCanonicalInput(ReadOnlyMemory<byte>.Empty, new string('d', 64)));

        var result = await service.TryExecuteAndPersistAsync(
            EvaluatorRegistryVersions.P0,
            procedure,
            request,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("evaluator_mode", result.Field);
        Assert.Equal(0, registry.LookupCount);
        Assert.Equal(0, runner.ExecuteCount);
        Assert.Equal(0, store.AppendCount);
    }

    private sealed class CountingRegistry : IEvaluatorRegistry
    {
        public int LookupCount { get; private set; }

        public EvaluationDecision<EvaluatorRegistrySnapshot> TryGetRegistry(string registryVersion)
        {
            LookupCount++;
            return EvaluationDecision<EvaluatorRegistrySnapshot>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator);
        }
    }

    private sealed class CountingRunner : IDeterministicEvaluatorRunner
    {
        public int ExecuteCount { get; private set; }

        public EvaluationDecision<DeterministicEvaluatorExecutionResult> TryExecute(
            EvaluatorRegistrySnapshot registry,
            DeterministicEvaluatorExecutionRequest request,
            DateTimeOffset startedAt)
        {
            ExecuteCount++;
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator);
        }
    }

    private sealed class CountingStore : IDeterministicInvocationStore
    {
        public int AppendCount { get; private set; }

        public Task<EvaluationDecision<Guid>> TryAppendAsync(
            DeterministicInvocationAppendCommand command,
            CancellationToken cancellationToken)
        {
            AppendCount++;
            return Task.FromResult(EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.DeterministicConflict));
        }
    }
}
