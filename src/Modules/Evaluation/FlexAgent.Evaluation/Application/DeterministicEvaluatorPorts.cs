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
    IEvaluationRequestAuthorityStore authorityStore,
    IProtectedEvaluationProcedureSource procedureSource,
    IEvaluatorRegistry registry,
    IDeterministicEvaluatorRunner runner,
    IDeterministicInvocationStore store)
{
    public async Task<EvaluationDecision<DeterministicEvaluatorExecutionResult>> TryExecuteAndPersistAsync(
        DeterministicEvaluatorExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var authority = await authorityStore.TryLoadAsync(
            request.Ownership,
            request.RequestId,
            request.InvocationAttemptId,
            cancellationToken);
        if (authority is null)
        {
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(
                EvaluationFailureCodes.InvalidField,
                "request");
        }

        var procedurePayload = await procedureSource.GetCanonicalUtf8Async(
            authority.Ownership.OrganizationId,
            authority.ProcedureRef.SourceId,
            authority.ProcedureRef.SourceVersionId,
            authority.ProcedureRef.ContentDigest,
            cancellationToken);
        if (procedurePayload is null)
        {
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "procedure");
        }

        var verified = DeterministicEvaluatorAuthorityVerifier.TryVerify(
            authority,
            procedurePayload,
            request);
        if (!verified.Succeeded || verified.Value is null)
        {
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(
                verified.OutcomeCode,
                verified.Field);
        }

        var registryResult = registry.TryGetRegistry(verified.Value.Authority.EvaluatorRegistryVersion);
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
