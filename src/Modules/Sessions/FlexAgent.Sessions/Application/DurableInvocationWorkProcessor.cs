using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed class DurableInvocationWorkItem(
    Guid workId,
    SessionOwnership ownership,
    string agentInvocationId,
    string state,
    DateTimeOffset? claimLeaseUntil = null)
{
    public Guid WorkId { get; } = workId;

    public SessionOwnership Ownership { get; } = ownership;

    public string AgentInvocationId { get; } = agentInvocationId;

    public string State { get; set; } = state;

    public DateTimeOffset? ClaimLeaseUntil { get; set; } = claimLeaseUntil;
}

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

public sealed record DurableWorkBacklogSnapshot(int ClaimableCount, int ClaimablePartitionCount)
{
    public static DurableWorkBacklogSnapshot Unknown { get; } = new(-1, -1);

    public bool IsKnown => ClaimableCount >= 0;
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

    Task<DateTimeOffset?> TryRenewClaimLeaseAsync(
        DurableInvocationWorkItem work,
        TimeSpan lease,
        CancellationToken cancellationToken) =>
        Task.FromResult<DateTimeOffset?>(work.ClaimLeaseUntil ?? DateTimeOffset.UnixEpoch);

    Task<DurableWorkBacklogSnapshot> ReadClaimableSnapshotAsync(CancellationToken cancellationToken) =>
        Task.FromResult(DurableWorkBacklogSnapshot.Unknown);
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

    Task<TrustedSessionBinding?> GetForOrganizationSessionAsync(
        Guid organizationId,
        Guid sessionId,
        CancellationToken cancellationToken);
}

public sealed record SessionActorRelationship(
    SessionOwnership Ownership,
    Guid ActorId,
    string ActorType,
    string Relationship,
    long RelationshipVersion);

public interface ISessionActorRelationshipStore
{
    Task<bool> SetCurrentAsync(
        SessionActorRelationship relationship,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeCurrentAsync(
        Guid actorId,
        Guid untrustedSessionId,
        long relationshipVersion,
        CancellationToken cancellationToken = default);
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
    DurableInvocationWorkSettings settings,
    IAgentResponsePublicationPersistPort publicationPersist,
    ISessionRuntimeTelemetry? telemetry = null) : IDurableInvocationWorkProcessor
{
    private readonly ISessionRuntimeTelemetry _telemetry = telemetry ?? NoopSessionRuntimeTelemetry.Instance;
    private readonly IPublishAgentResponseFragmentHandler _publicationHandler =
        new PublishAgentResponseFragmentHandler(telemetry);
    private readonly ISealAgentResponseHandler _sealHandler = new SealAgentResponseHandler();
    private readonly IAgentResponsePublicationPersistPort _publicationPersist =
        publicationPersist ?? throw new ArgumentNullException(nameof(publicationPersist));

    public async Task<DurableInvocationWorkProcessResult> TryProcessNextAsync(
        CancellationToken cancellationToken)
    {
        var claimed = await workStore.TryClaimExecuteInvocationAsync(
            TimeSpan.FromSeconds(30),
            cancellationToken);
        if (claimed is null)
        {
            RecordClaim(SessionRuntimeTelemetryValues.Idle);
            return DurableInvocationWorkProcessResult.Idle;
        }

        RecordClaim(SessionRuntimeTelemetryValues.Claimed);

        if (cancellationToken.IsCancellationRequested)
        {
            return RecordProcess(await ReleaseForRetryAsync(claimed));
        }

        var loaded = await sessionGateway.LoadAsync(claimed.Ownership, cancellationToken);
        if (loaded is null)
        {
            return RecordProcess(await ReleaseForRetryAsync(claimed));
        }

        var invocation = loaded.Session.Invocations.FirstOrDefault(item =>
            string.Equals(item.AgentInvocationId, claimed.AgentInvocationId, StringComparison.Ordinal));
        if (invocation is null)
        {
            return RecordProcess(await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId));
        }

        if (invocation.IsTerminal)
        {
            if (loaded.Session.HasOpenAgentContentPublication(invocation.AgentInvocationId))
            {
                return RecordProcess(await PublishContentAsync(claimed, loaded, invocation, cancellationToken));
            }

            await workStore.MarkCompletedAsync(claimed, cancellationToken);
            return RecordProcess(new DurableInvocationWorkProcessResult(
                DurableInvocationWorkOutcomes.Reconciled,
                claimed.AgentInvocationId,
                InvocationCompletionOutcomeCodes.AlreadyTerminal));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return RecordProcess(await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId));
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
            return RecordProcess(await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId));
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
            return RecordProcess(new DurableInvocationWorkProcessResult(
                DurableInvocationWorkOutcomes.Reconciled,
                claimed.AgentInvocationId,
                completion.OutcomeCode));
        }

        if (!completion.Succeeded
            && completion.OutcomeCode != InvocationCompletionOutcomeCodes.EffectFailed)
        {
            return RecordProcess(await ReleaseForRetryAsync(
                claimed,
                claimed.AgentInvocationId,
                completion.OutcomeCode));
        }

        if (completion.Invocation is null || !completion.Invocation.IsTerminal)
        {
            return RecordProcess(await ReleaseForRetryAsync(
                claimed,
                claimed.AgentInvocationId,
                completion.OutcomeCode));
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
            return RecordProcess(await ReleaseForRetryAsync(
                claimed,
                claimed.AgentInvocationId,
                InvocationCompletionOutcomeCodes.StaleVersion));
        }

        if (completion.OutcomeCode == InvocationCompletionOutcomeCodes.ExecutionFailed)
        {
            await workStore.MarkCompletedAsync(claimed, cancellationToken);
            return RecordProcess(new DurableInvocationWorkProcessResult(
                DurableInvocationWorkOutcomes.ExecutionFailed,
                claimed.AgentInvocationId,
                completion.OutcomeCode));
        }

        invocation = completion.Invocation;
        loaded = loaded with { ObservedSessionVersion = loaded.Session.SessionVersion };
        if (loaded.Session.HasOpenAgentContentPublication(invocation.AgentInvocationId))
        {
            return RecordProcess(await PublishContentAsync(claimed, loaded, invocation, cancellationToken));
        }

        await workStore.MarkCompletedAsync(claimed, cancellationToken);
        return RecordProcess(new DurableInvocationWorkProcessResult(
            DurableInvocationWorkOutcomes.Decided,
            claimed.AgentInvocationId,
            completion.OutcomeCode));
    }

    private async Task<DurableInvocationWorkProcessResult> PublishContentAsync(
        DurableInvocationWorkItem claimed,
        LoadedInvocationWorkSession loaded,
        AgentInvocation invocation,
        CancellationToken cancellationToken)
    {
        var session = loaded.Session;
        var existing = session.AgentMessages.FirstOrDefault(message =>
            string.Equals(message.DrivingInvocationId, invocation.AgentInvocationId, StringComparison.Ordinal));
        if (existing is { Fragments.Count: > 0, IsTerminal: false })
        {
            return await StopContentAsync(
                claimed,
                loaded,
                invocation.AgentInvocationId,
                DurableInvocationWorkOutcomes.PublicationIncomplete,
                cancellationToken);
        }

        var generationAttemptId = existing?.GenerationAttemptId ?? $"agen.{claimed.WorkId:N}";
        var nextOrdinal = (existing?.LastFragmentOrdinal ?? 0) + 1;
        var assembled = existing?.AssembleExactText() ?? string.Empty;

        try
        {
            await foreach (var contentEvent in modelExecutionPort.StreamParticipantVisibleContentAsync(
                new ModelContentStreamRequest(
                    claimed.Ownership,
                    invocation.AgentInvocationId,
                    generationAttemptId),
                cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return await InterruptContentAsync(
                        claimed,
                        loaded,
                        invocation.AgentInvocationId);
                }

                var normalized = ProviderContentNormalizer.Normalize(contentEvent, assembled);
                switch (normalized)
                {
                    case NormalizedContentSkipped:
                        continue;
                    case NormalizedContentFailed:
                        return await StopContentAsync(
                            claimed,
                            loaded,
                            invocation.AgentInvocationId,
                            DurableInvocationWorkOutcomes.PublicationIncomplete,
                            cancellationToken);
                    case NormalizedContentCompleted:
                        return await FinishContentAsync(
                            claimed,
                            loaded,
                            invocation.AgentInvocationId,
                            cancellationToken);
                    case NormalizedContentDelta delta:
                        var published = await PublishDeltaAsync(
                            claimed,
                            loaded,
                            invocation.AgentInvocationId,
                            nextOrdinal,
                            delta.ExactUtf8Text,
                            generationAttemptId,
                            cancellationToken);
                        if (published.PersistFailed)
                        {
                            return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
                        }

                        if (!published.Result.Succeeded)
                        {
                            if (ShouldRetryPublication(published.Result.OutcomeCode, session, invocation.AgentInvocationId))
                            {
                                return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
                            }

                            return await StopContentAsync(
                                claimed,
                                loaded,
                                invocation.AgentInvocationId,
                                DurableInvocationWorkOutcomes.PublicationIncomplete,
                                cancellationToken);
                        }

                        if (published.Result.OutcomeCode != FragmentCommitOutcomeCodes.Reconciled)
                        {
                            nextOrdinal++;
                            assembled += delta.ExactUtf8Text;
                            loaded = loaded with { ObservedSessionVersion = session.SessionVersion };
                        }

                        continue;
                    default:
                        return await StopContentAsync(
                            claimed,
                            loaded,
                            invocation.AgentInvocationId,
                            DurableInvocationWorkOutcomes.PublicationFailed,
                            cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            return await InterruptContentAsync(
                claimed,
                loaded,
                invocation.AgentInvocationId);
        }

        return await StopContentAsync(
            claimed,
            loaded,
            invocation.AgentInvocationId,
            DurableInvocationWorkOutcomes.PublicationIncomplete,
            cancellationToken);
    }

    private readonly record struct PublishedDelta(
        AgentResponseFragmentCommitResult Result,
        bool PersistFailed);

    private async Task<PublishedDelta> PublishDeltaAsync(
        DurableInvocationWorkItem claimed,
        LoadedInvocationWorkSession loaded,
        string agentInvocationId,
        int fragmentOrdinal,
        string exactUtf8Text,
        string generationAttemptId,
        CancellationToken cancellationToken)
    {
        var utc = await sessionGateway.ReadAuthoritativeUtcAsync(cancellationToken);
        var command = new PublishAgentResponseFragmentCommand(
            settings.ServiceActor,
            claimed.Ownership,
            loaded.ObservedSessionVersion,
            agentInvocationId,
            fragmentOrdinal,
            exactUtf8Text,
            generationAttemptId,
            Guid.NewGuid(),
            settings.SourceChannel);
        var published = _publicationHandler.Handle(command, loaded.Session, utc);
        if (!published.Succeeded)
        {
            return new PublishedDelta(published, PersistFailed: false);
        }

        var persisted = await _publicationPersist.PersistFragmentAsync(
            command,
            loaded.Binding,
            cancellationToken);
        if (!persisted.Succeeded)
        {
            return new PublishedDelta(persisted, PersistFailed: true);
        }

        var renewed = await workStore.TryRenewClaimLeaseAsync(
            claimed,
            TimeSpan.FromSeconds(30),
            cancellationToken);
        if (renewed is null)
        {
            return new PublishedDelta(published, PersistFailed: true);
        }

        claimed.ClaimLeaseUntil = renewed;
        return new PublishedDelta(published, PersistFailed: false);
    }

    private async Task<DurableInvocationWorkProcessResult> FinishContentAsync(
        DurableInvocationWorkItem claimed,
        LoadedInvocationWorkSession loaded,
        string agentInvocationId,
        CancellationToken cancellationToken)
    {
        var message = loaded.Session.AgentMessages.FirstOrDefault(item =>
            string.Equals(item.DrivingInvocationId, agentInvocationId, StringComparison.Ordinal));
        if (message is null || message.Fragments.Count == 0)
        {
            var utc = await sessionGateway.ReadAuthoritativeUtcAsync(cancellationToken);
            var failed = loaded.Session.FailUnpublishedAgentResponse(agentInvocationId, utc);
            if (!failed.Succeeded
                && failed.OutcomeCode != FragmentCommitOutcomeCodes.Reconciled)
            {
                return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
            }

            return await PersistUnpublishedFailureAndCompleteAsync(
                claimed,
                loaded,
                DurableInvocationWorkOutcomes.PublicationFailed,
                cancellationToken);
        }

        if (!message.IsTerminal)
        {
            var utc = await sessionGateway.ReadAuthoritativeUtcAsync(cancellationToken);
            var command = new SealAgentResponseCommand(
                settings.ServiceActor,
                claimed.Ownership,
                loaded.Session.SessionVersion,
                agentInvocationId,
                AgentMessageCompletionStates.Complete,
                Guid.NewGuid(),
                settings.SourceChannel);
            var sealedResult = _sealHandler.Handle(command, loaded.Session, utc);
            if (!sealedResult.Succeeded)
            {
                return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
            }

            return await PersistSealAndCompleteAsync(
                claimed,
                loaded,
                command,
                DurableInvocationWorkOutcomes.Published,
                cancellationToken);
        }

        await workStore.MarkCompletedAsync(claimed, cancellationToken);
        return new DurableInvocationWorkProcessResult(
            DurableInvocationWorkOutcomes.Published,
            claimed.AgentInvocationId);
    }

    private async Task<DurableInvocationWorkProcessResult> StopContentAsync(
        DurableInvocationWorkItem claimed,
        LoadedInvocationWorkSession loaded,
        string agentInvocationId,
        string outcome,
        CancellationToken cancellationToken)
    {
        var message = loaded.Session.AgentMessages.FirstOrDefault(item =>
            string.Equals(item.DrivingInvocationId, agentInvocationId, StringComparison.Ordinal));
        if (message is { Fragments.Count: > 0, IsTerminal: false })
        {
            var utc = await sessionGateway.ReadAuthoritativeUtcAsync(cancellationToken);
            var command = new SealAgentResponseCommand(
                settings.ServiceActor,
                claimed.Ownership,
                loaded.Session.SessionVersion,
                agentInvocationId,
                AgentMessageCompletionStates.Incomplete,
                Guid.NewGuid(),
                settings.SourceChannel);
            var sealedResult = _sealHandler.Handle(command, loaded.Session, utc);
            if (!sealedResult.Succeeded)
            {
                return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
            }

            return await PersistSealAndCompleteAsync(
                claimed,
                loaded,
                command,
                DurableInvocationWorkOutcomes.PublicationIncomplete,
                cancellationToken);
        }

        if (message is null || message.Fragments.Count == 0)
        {
            var utc = await sessionGateway.ReadAuthoritativeUtcAsync(cancellationToken);
            var failed = loaded.Session.FailUnpublishedAgentResponse(agentInvocationId, utc);
            if (!failed.Succeeded
                && failed.OutcomeCode != FragmentCommitOutcomeCodes.Reconciled)
            {
                return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
            }

            return await PersistUnpublishedFailureAndCompleteAsync(
                claimed,
                loaded,
                DurableInvocationWorkOutcomes.PublicationFailed,
                cancellationToken);
        }

        await workStore.MarkCompletedAsync(claimed, cancellationToken);
        return new DurableInvocationWorkProcessResult(outcome, claimed.AgentInvocationId);
    }

    private async Task<DurableInvocationWorkProcessResult> PersistSealAndCompleteAsync(
        DurableInvocationWorkItem claimed,
        LoadedInvocationWorkSession loaded,
        SealAgentResponseCommand command,
        string outcome,
        CancellationToken cancellationToken)
    {
        var persisted = await _publicationPersist.PersistSealAsync(
            command,
            loaded.Binding,
            cancellationToken);
        if (!persisted.Succeeded)
        {
            return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
        }

        await workStore.MarkCompletedAsync(claimed, cancellationToken);
        return new DurableInvocationWorkProcessResult(outcome, claimed.AgentInvocationId);
    }

    private async Task<DurableInvocationWorkProcessResult> PersistUnpublishedFailureAndCompleteAsync(
        DurableInvocationWorkItem claimed,
        LoadedInvocationWorkSession loaded,
        string outcome,
        CancellationToken cancellationToken)
    {
        var persisted = await _publicationPersist.TryPersistUnpublishedFailureAsync(
            claimed.Ownership,
            loaded.Binding,
            loaded.ObservedSessionVersion,
            loaded.Session,
            cancellationToken);
        if (!persisted)
        {
            return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
        }

        await workStore.MarkCompletedAsync(claimed, cancellationToken);
        return new DurableInvocationWorkProcessResult(outcome, claimed.AgentInvocationId);
    }

    private static bool ShouldRetryPublication(
        string outcomeCode,
        SessionRuntime session,
        string agentInvocationId)
    {
        if (outcomeCode is not (
            FragmentCommitOutcomeCodes.RateExceeded
            or FragmentCommitOutcomeCodes.InFlightExceeded
            or FragmentCommitOutcomeCodes.StaleVersion
            or FragmentCommitOutcomeCodes.StaleClock))
        {
            return false;
        }

        var message = session.AgentMessages.FirstOrDefault(item =>
            string.Equals(item.DrivingInvocationId, agentInvocationId, StringComparison.Ordinal));
        return message is null || message.Fragments.Count == 0;
    }

    private async Task<DurableInvocationWorkProcessResult> InterruptContentAsync(
        DurableInvocationWorkItem claimed,
        LoadedInvocationWorkSession loaded,
        string agentInvocationId)
    {
        var message = loaded.Session.AgentMessages.FirstOrDefault(item =>
            string.Equals(item.DrivingInvocationId, agentInvocationId, StringComparison.Ordinal));
        if (message is { Fragments.Count: > 0 })
        {
            using var cleanup = new CancellationTokenSource(settings.EffectiveClaimCleanupTimeout);
            return await StopContentAsync(
                claimed,
                loaded,
                agentInvocationId,
                DurableInvocationWorkOutcomes.PublicationIncomplete,
                cleanup.Token);
        }

        return await ReleaseForRetryAsync(claimed, claimed.AgentInvocationId);
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

    private void RecordClaim(string outcome) =>
        _telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.WorkClaim,
            SessionRuntimeTelemetryRecording.Labels(
                (SessionRuntimeTelemetryLabelKeys.Outcome, outcome),
                (SessionRuntimeTelemetryLabelKeys.WorkType, DurableSessionWorkTypes.ExecuteInvocation)));

    private DurableInvocationWorkProcessResult RecordProcess(DurableInvocationWorkProcessResult result)
    {
        _telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.WorkProcess,
            SessionRuntimeTelemetryRecording.Labels(
                (SessionRuntimeTelemetryLabelKeys.Outcome, result.Outcome),
                (SessionRuntimeTelemetryLabelKeys.WorkType, DurableSessionWorkTypes.ExecuteInvocation)));
        return result;
    }
}
