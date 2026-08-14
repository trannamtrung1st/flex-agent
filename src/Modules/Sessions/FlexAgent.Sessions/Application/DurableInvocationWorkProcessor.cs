using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record DurableInvocationWorkItem(
    Guid WorkId,
    SessionOwnership Ownership,
    string AgentInvocationId,
    string State,
    DateTimeOffset? ClaimLeaseUntil = null);

public sealed record LoadedInvocationWorkSession(
    SessionRuntime Session,
    TrustedSessionBinding Binding,
    long ObservedSessionVersion);

public sealed record DurableInvocationWorkSettings(
    TrustedRuntimeActor ServiceActor,
    string ProviderId,
    string SourceChannel,
    int MaxControlUtf8Bytes,
    Func<SessionOwnership, ModelDeploymentCredentialBindingRequest> CreateBindingRequest,
    TimeSpan ClaimCleanupTimeout = default)
{
    public TimeSpan EffectiveClaimCleanupTimeout =>
        ClaimCleanupTimeout > TimeSpan.Zero ? ClaimCleanupTimeout : TimeSpan.FromSeconds(2);
}

public sealed record DurableInvocationWorkProcessResult(
    string Outcome,
    string? AgentInvocationId = null,
    string? CompletionOutcomeCode = null)
{
    public static DurableInvocationWorkProcessResult Idle { get; } = new(DurableInvocationWorkOutcomes.Idle);
}

public interface IDurableInvocationWorkStore
{
    Task<DurableInvocationWorkItem?> TryClaimExecuteInvocationAsync(
        TimeSpan lease,
        CancellationToken cancellationToken);

    Task ReleaseToPendingAsync(
        DurableInvocationWorkItem work,
        CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        DurableInvocationWorkItem work,
        CancellationToken cancellationToken);
}

public interface IInvocationWorkSessionGateway
{
    Task<LoadedInvocationWorkSession?> LoadAsync(
        SessionOwnership ownership,
        CancellationToken cancellationToken);

    Task<DateTimeOffset> ReadAuthoritativeUtcAsync(CancellationToken cancellationToken);

    Task<bool> TrySaveCompletionAsync(
        SessionOwnership ownership,
        long expectedSessionVersion,
        SessionRuntime session,
        AgentInvocation invocation,
        Guid correlationId,
        CancellationToken cancellationToken);
}

public interface IDurableInvocationWorkProcessor
{
    Task<DurableInvocationWorkProcessResult> TryProcessNextAsync(CancellationToken cancellationToken);
}

public interface ITrustedSessionBindingSource
{
    Task<TrustedSessionBinding?> GetAsync(
        SessionOwnership ownership,
        CancellationToken cancellationToken);
}

public sealed class IdleDurableInvocationWorkProcessor : IDurableInvocationWorkProcessor
{
    public Task<DurableInvocationWorkProcessResult> TryProcessNextAsync(CancellationToken cancellationToken) =>
        Task.FromResult(DurableInvocationWorkProcessResult.Idle);
}

public sealed class DurableInvocationWorkProcessor(
    IDurableInvocationWorkStore workStore,
    IInvocationWorkSessionGateway sessionGateway,
    IModelExecutionPort modelExecutionPort,
    ICompleteInvocationHandler completionHandler,
    DurableInvocationWorkSettings settings) : IDurableInvocationWorkProcessor
{
    public async Task<DurableInvocationWorkProcessResult> TryProcessNextAsync(
        CancellationToken cancellationToken)
    {
        var claimed = await workStore.TryClaimExecuteInvocationAsync(
            TimeSpan.FromSeconds(30),
            cancellationToken);
        if (claimed is null)
        {
            return DurableInvocationWorkProcessResult.Idle;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return await ReleaseForRetryAsync(claimed);
        }

        var loaded = await sessionGateway.LoadAsync(claimed.Ownership, cancellationToken);
        if (loaded is null)
        {
            return await ReleaseForRetryAsync(claimed);
        }

        var invocation = loaded.Session.Invocations.FirstOrDefault(item =>
            string.Equals(item.AgentInvocationId, claimed.AgentInvocationId, StringComparison.Ordinal));
        if (invocation is null)
        {
            return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
        }

        if (invocation.IsTerminal)
        {
            await workStore.MarkCompletedAsync(claimed, cancellationToken);
            return new DurableInvocationWorkProcessResult(
                DurableInvocationWorkOutcomes.Reconciled,
                claimed.AgentInvocationId,
                InvocationCompletionOutcomeCodes.AlreadyTerminal);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
        }

        var bindingRequest = settings.CreateBindingRequest(claimed.Ownership);
        var resolvedBinding = ModelDeploymentCredentialBindingResolver.Resolve(bindingRequest);
        var preflight = ModelExecutionPreflight.RejectIfBindingUnavailable(resolvedBinding);
        ModelExecutionAttemptResult attemptResult;
        if (preflight is not null)
        {
            attemptResult = preflight;
        }
        else
        {
            var binding = resolvedBinding.Binding!;
            var context = InvocationContextAssembler.Assemble(loaded.Session);
            attemptResult = await modelExecutionPort.ExecuteAsync(
                new ModelExecutionAttemptRequest(
                    claimed.Ownership,
                    claimed.AgentInvocationId,
                    binding.ProviderId,
                    binding.BindingReference,
                    binding.BindingVersion,
                    context,
                    invocation.Attempts.Count + 1,
                    settings.MaxControlUtf8Bytes),
                cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
        }

        var authoritativeUtc = await sessionGateway.ReadAuthoritativeUtcAsync(cancellationToken);
        var command = attemptResult is ModelExecutionStructuredControl control
            ? new CompleteInvocationCommand(
                settings.ServiceActor,
                claimed.Ownership,
                loaded.ObservedSessionVersion,
                claimed.AgentInvocationId,
                control.Envelope,
                null,
                Guid.NewGuid(),
                settings.SourceChannel)
            : new CompleteInvocationCommand(
                settings.ServiceActor,
                claimed.Ownership,
                loaded.ObservedSessionVersion,
                claimed.AgentInvocationId,
                null,
                new ExecutionFailureCompletion(
                    AssertFailureReason(attemptResult)),
                Guid.NewGuid(),
                settings.SourceChannel);

        var completion = completionHandler.Handle(command, loaded.Session, authoritativeUtc);
        if (completion.OutcomeCode == InvocationCompletionOutcomeCodes.AlreadyTerminal)
        {
            await workStore.MarkCompletedAsync(claimed, cancellationToken);
            return new DurableInvocationWorkProcessResult(
                DurableInvocationWorkOutcomes.Reconciled,
                claimed.AgentInvocationId,
                completion.OutcomeCode);
        }

        if (!completion.Succeeded
            && completion.OutcomeCode != InvocationCompletionOutcomeCodes.EffectFailed)
        {
            return await ReleaseForRetryAsync(
                claimed,
                claimed.AgentInvocationId,
                completion.OutcomeCode);
        }

        if (completion.Invocation is null || !completion.Invocation.IsTerminal)
        {
            return await ReleaseForRetryAsync(
                claimed,
                claimed.AgentInvocationId,
                completion.OutcomeCode);
        }

        var saved = await sessionGateway.TrySaveCompletionAsync(
            claimed.Ownership,
            loaded.ObservedSessionVersion,
            loaded.Session,
            completion.Invocation,
            command.CorrelationId,
            cancellationToken);
        if (!saved)
        {
            return await ReleaseForRetryAsync(
                claimed,
                claimed.AgentInvocationId,
                InvocationCompletionOutcomeCodes.StaleVersion);
        }

        await workStore.MarkCompletedAsync(claimed, cancellationToken);
        var outcome = completion.OutcomeCode == InvocationCompletionOutcomeCodes.ExecutionFailed
            ? DurableInvocationWorkOutcomes.ExecutionFailed
            : DurableInvocationWorkOutcomes.Decided;
        return new DurableInvocationWorkProcessResult(outcome, claimed.AgentInvocationId, completion.OutcomeCode);
    }

    private async Task<DurableInvocationWorkProcessResult> ReleaseForRetryAsync(
        DurableInvocationWorkItem claimed,
        string? agentInvocationId = null,
        string? completionOutcomeCode = null)
    {
        using var cleanup = new CancellationTokenSource(settings.EffectiveClaimCleanupTimeout);
        await workStore.ReleaseToPendingAsync(claimed, cleanup.Token);
        return new DurableInvocationWorkProcessResult(
            DurableInvocationWorkOutcomes.RetryLater,
            agentInvocationId ?? claimed.AgentInvocationId,
            completionOutcomeCode);
    }

    private static string AssertFailureReason(ModelExecutionAttemptResult attemptResult) =>
        attemptResult is ModelExecutionFailed failed
            ? failed.ReasonCategory
            : ExecutionFailureReasons.MalformedControl;
}
