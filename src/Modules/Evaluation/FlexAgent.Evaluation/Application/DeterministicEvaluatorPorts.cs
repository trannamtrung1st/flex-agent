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

public interface IProtectedDeterministicOutputStore
{
    Task<EvaluationDecision<bool>> TryPersistAsync(
        ProtectedDeterministicOutputPersistCommand command,
        CancellationToken cancellationToken);

    Task<EvaluationSafeFactProjection?> TryLoadProjectionAsync(
        Guid organizationId,
        Guid requestId,
        Guid deterministicAttemptId,
        string expectedContentDigest,
        string expectedCriterionId,
        string expectedCriterionVersion,
        CancellationToken cancellationToken);

    Task<VerifiedDeterministicOutputMaterial?> TryLoadVerifiedMaterialAsync(
        EvaluationOwnership ownership,
        Guid requestId,
        Guid deterministicAttemptId,
        string expectedContentDigest,
        string expectedCriterionId,
        string expectedCriterionVersion,
        CancellationToken cancellationToken);
}

public sealed record ProtectedDeterministicOutputPersistCommand(
    EvaluationOwnership Ownership,
    Guid RequestId,
    Guid DeterministicAttemptId,
    string ProtectedOutputRef,
    string OutputContentDigest,
    ReadOnlyMemory<byte> OutputUtf8);

public interface IProtectedDeterministicInputAuthorityStore
{
    Task<EvaluationDecision<bool>> TryEstablishAsync(
        ProtectedDeterministicInputAuthorityEstablishCommand command,
        CancellationToken cancellationToken);
}

public sealed record ProtectedDeterministicInputAuthorityEstablishCommand(
    EvaluationOwnership Ownership,
    Guid RequestId,
    Guid InvocationAttemptId,
    string CriterionId,
    string CriterionVersion,
    Guid DeterministicAttemptId,
    string CanonicalInputDigest,
    string ProtectedInputRef,
    ReadOnlyMemory<byte> InputUtf8);

public sealed class DeterministicEvaluatorExecutionService(
    IEvaluationRequestAuthorityStore authorityStore,
    IProtectedEvaluationProcedureSource procedureSource,
    IEvaluatorRegistry registry,
    IDeterministicEvaluatorRunner runner,
    IDeterministicInvocationStore store,
    IProtectedDeterministicOutputStore outputStore,
    IProtectedDeterministicInputAuthorityStore inputAuthorityStore)
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

        if (string.Equals(
                execution.Value.Outcome,
                DeterministicInvocationOutcomes.Succeeded,
                StringComparison.Ordinal)
            && execution.Value.ProtectedInputRef is not null)
        {
            var establishInput = await inputAuthorityStore.TryEstablishAsync(
                new ProtectedDeterministicInputAuthorityEstablishCommand(
                    request.Ownership,
                    request.RequestId,
                    request.InvocationAttemptId,
                    request.CriterionId,
                    request.CriterionVersion,
                    execution.Value.DeterministicAttemptId,
                    request.Input.CanonicalInputDigest,
                    execution.Value.ProtectedInputRef,
                    request.Input.CanonicalUtf8),
                cancellationToken);
            if (!establishInput.Succeeded)
            {
                return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(
                    establishInput.OutcomeCode,
                    establishInput.Field);
            }
        }

        if (string.Equals(
                execution.Value.Outcome,
                DeterministicInvocationOutcomes.Succeeded,
                StringComparison.Ordinal)
            && execution.Value.OutputUtf8 is not null
            && execution.Value.OutputContentDigest is not null
            && execution.Value.ProtectedOutputRef is not null)
        {
            var materialize = await outputStore.TryPersistAsync(
                new ProtectedDeterministicOutputPersistCommand(
                    request.Ownership,
                    request.RequestId,
                    execution.Value.DeterministicAttemptId,
                    execution.Value.ProtectedOutputRef,
                    execution.Value.OutputContentDigest,
                    execution.Value.OutputUtf8.Value),
                cancellationToken);
            if (!materialize.Succeeded)
            {
                return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(
                    materialize.OutcomeCode,
                    materialize.Field);
            }
        }

        return execution;
    }
}
