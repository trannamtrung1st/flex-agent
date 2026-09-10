using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public interface IDeterministicEvaluatorRunner
{
    EvaluationDecision<DeterministicEvaluatorExecutionResult> TryExecute(
        EvaluatorRegistrySnapshot registry,
        DeterministicEvaluatorExecutionRequest request,
        DateTimeOffset startedAt);
}

public interface IDeterministicInvocationStore
{
    Task<EvaluationDecision<Guid>> TryAppendAsync(
        DeterministicInvocationAppendCommand command,
        CancellationToken cancellationToken);
}

public sealed class DeterministicEvaluatorExecutionService(
    IEvaluatorRegistry registry,
    IDeterministicEvaluatorRunner runner,
    IDeterministicInvocationStore store)
{
    public async Task<EvaluationDecision<DeterministicEvaluatorExecutionResult>> TryExecuteAndPersistAsync(
        string registryVersion,
        EvaluationProcedureV1 procedure,
        DeterministicEvaluatorExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var orchestration = DeterministicEvaluatorOrchestrationValidator.TryValidateRequest(procedure, request);
        if (!orchestration.Succeeded)
        {
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(
                orchestration.OutcomeCode,
                orchestration.Field);
        }

        var registryResult = registry.TryGetRegistry(registryVersion);
        if (!registryResult.Succeeded || registryResult.Value is null)
        {
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(
                registryResult.OutcomeCode,
                registryResult.Field);
        }

        var startedAt = DateTimeOffset.UtcNow;
        var execution = runner.TryExecute(registryResult.Value, request, startedAt);
        if (!execution.Succeeded || execution.Value is null)
        {
            return execution;
        }

        var append = await store.TryAppendAsync(
            new DeterministicInvocationAppendCommand(
                request.Ownership,
                request.RequestId,
                request.InvocationAttemptId,
                execution.Value.DeterministicAttemptId,
                request.CriterionId,
                request.CriterionVersion,
                request.Input.CanonicalInputDigest,
                request.Binding,
                execution.Value),
            cancellationToken);
        if (!append.Succeeded)
        {
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(
                append.OutcomeCode,
                append.Field);
        }

        return execution;
    }
}
