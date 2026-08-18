using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresPublishAgentResponseCoordinator(
    PostgresConnectionAccessor connectionAccessor,
    PostgresSessionRuntimeRepository runtimeRepository,
    IPublishAgentResponseFragmentHandler publicationHandler,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null,
    ICommitAuthorizationKernel? authorizationKernel = null,
    DurableInvocationWorkSettings? settings = null,
    IAuthenticatedWorkloadContextSource? workloadIdentity = null)
    : IAgentResponsePublicationPersistPort
{
    private readonly ISealAgentResponseHandler _sealHandler = new SealAgentResponseHandler();
    private readonly IAuditEventWriter _auditEventWriter = auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter = outboxItemWriter ?? new PostgresOutboxItemWriter();

    public async Task<AgentResponseFragmentCommitResult> PublishFragmentAsync(
        PublishAgentResponseFragmentCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(binding);

        if (command.Ownership != binding.Ownership)
        {
            return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.OwnershipMismatch);
        }

        if (settings is not null
            && !await AuthenticatedWorkloadGuard.IsCurrentForActorAsync(
                workloadIdentity,
                settings.ServiceActor,
                cancellationToken))
        {
            return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.Denied);
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var session = await LoadSessionOrRollbackAsync(
                command.Ownership,
                binding,
                scope,
                cancellationToken);
            if (session is null)
            {
                return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.Denied);
            }

            var authoritativeUtc = await runtimeRepository.ReadAuthoritativeUtcAsync(
                scope.Transaction,
                cancellationToken);
            var result = publicationHandler.Handle(command, session, authoritativeUtc);
            return await PersistPublicationAsync(
                command.Actor,
                command.Ownership,
                command.ExpectedSessionVersion,
                command.CorrelationId,
                command.SourceChannel,
                session,
                result,
                scope,
                authoritativeUtc,
                cancellationToken);
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AgentResponseFragmentCommitResult> SealAsync(
        SealAgentResponseCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(binding);

        if (command.Ownership != binding.Ownership)
        {
            return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.OwnershipMismatch);
        }

        if (settings is not null
            && !await AuthenticatedWorkloadGuard.IsCurrentForActorAsync(
                workloadIdentity,
                settings.ServiceActor,
                cancellationToken))
        {
            return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.Denied);
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var session = await LoadSessionOrRollbackAsync(
                command.Ownership,
                binding,
                scope,
                cancellationToken);
            if (session is null)
            {
                return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.Denied);
            }

            var authoritativeUtc = await runtimeRepository.ReadAuthoritativeUtcAsync(
                scope.Transaction,
                cancellationToken);
            var result = _sealHandler.Handle(command, session, authoritativeUtc);
            return await PersistPublicationAsync(
                command.Actor,
                command.Ownership,
                command.ExpectedSessionVersion,
                command.CorrelationId,
                command.SourceChannel,
                session,
                result,
                scope,
                authoritativeUtc,
                cancellationToken);
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<SessionRuntime?> LoadSessionOrRollbackAsync(
        SessionOwnership ownership,
        TrustedSessionBinding binding,
        PostgresTransactionScope scope,
        CancellationToken cancellationToken)
    {
        var session = await runtimeRepository.LoadForUpdateAsync(
            ownership,
            binding,
            scope.Transaction,
            cancellationToken);
        if (session is null)
        {
            await scope.RollbackAsync(cancellationToken);
        }

        return session;
    }

    private async Task<AgentResponseFragmentCommitResult> PersistPublicationAsync(
        TrustedRuntimeActor actor,
        SessionOwnership ownership,
        long expectedSessionVersion,
        Guid correlationId,
        string sourceChannel,
        SessionRuntime session,
        AgentResponseFragmentCommitResult result,
        PostgresTransactionScope scope,
        DateTimeOffset authoritativeUtc,
        CancellationToken cancellationToken)
    {
        if (!result.Succeeded)
        {
            await scope.RollbackAsync(cancellationToken);
            return result;
        }

        if (result.OutcomeCode == FragmentCommitOutcomeCodes.Reconciled)
        {
            await scope.RollbackAsync(cancellationToken);
            return result;
        }

        var pendingFragments = session.PendingPublicationWork
            .SelectMany(message => message.PendingInserts.Select(fragment => (message.MessageId, fragment)))
            .ToArray();
        var pendingSeals = session.PendingPublicationWork
            .Where(message => message.SealDirty)
            .ToArray();

        var saved = await runtimeRepository.TrySaveAgentResponsePublicationAsync(
            ownership,
            expectedSessionVersion,
            session,
            scope.Transaction,
            cancellationToken);
        if (!saved)
        {
            await scope.RollbackAsync(cancellationToken);
            return new AgentResponseFragmentCommitResult(
                false,
                FragmentCommitOutcomeCodes.StaleVersion,
                result.Message,
                result.Fragment,
                result.AgentMessagePublished);
        }

        foreach (var (messageId, fragment) in pendingFragments)
        {
            await SessionRuntimePersistenceAudit.WriteAsync(
                _auditEventWriter,
                _outboxItemWriter,
                actor,
                ownership,
                correlationId,
                sourceChannel,
                SessionRuntimeAuditActions.PublishAgentResponseFragment,
                SessionRuntimeOutboxEventTypes.AgentFragmentCommitted,
                SessionRuntimePublicationOutbox.FragmentWakeupSeed(
                    messageId,
                    fragment.FragmentOrdinal,
                    fragment.ContentDigest),
                authoritativeUtc,
                scope.Transaction,
                cancellationToken);
        }

        foreach (var message in pendingSeals)
        {
            await SessionRuntimePersistenceAudit.WriteAsync(
                _auditEventWriter,
                _outboxItemWriter,
                actor,
                ownership,
                correlationId,
                sourceChannel,
                SessionRuntimeAuditActions.SealAgentResponse,
                SessionRuntimeOutboxEventTypes.AgentMessageSealed,
                SessionRuntimePublicationOutbox.SealWakeupSeed(
                    message.MessageId,
                    message.CompletionState,
                    message.AssembledContentDigest),
                authoritativeUtc,
                scope.Transaction,
                cancellationToken);
        }

        if (authorizationKernel is not null && settings is not null)
        {
            var commitDecision = await SessionInvocationExecuteCommitAuthorization.ReauthorizeAsync(
                authorizationKernel,
                settings.ServiceActor,
                ownership,
                correlationId,
                sourceChannel,
                scope.Transaction,
                cancellationToken);
            if (!commitDecision.IsPermitted)
            {
                await scope.RollbackAsync(CancellationToken.None);
                return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.Denied);
            }
        }

        await scope.CommitAsync(cancellationToken);
        return result;
    }

    public Task<AgentResponseFragmentCommitResult> PersistFragmentAsync(
        PublishAgentResponseFragmentCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken) =>
        PublishFragmentAsync(command, binding, cancellationToken);

    public Task<AgentResponseFragmentCommitResult> PersistSealAsync(
        SealAgentResponseCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken) =>
        SealAsync(command, binding, cancellationToken);

    public async Task<bool> TryPersistUnpublishedFailureAsync(
        SessionOwnership ownership,
        TrustedSessionBinding binding,
        long expectedSessionVersion,
        SessionRuntime session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(session);
        if (ownership != binding.Ownership || ownership != session.Ownership)
        {
            return false;
        }

        if (settings is not null
            && !await AuthenticatedWorkloadGuard.IsCurrentForActorAsync(
                workloadIdentity,
                settings.ServiceActor,
                cancellationToken))
        {
            return false;
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var saved = await runtimeRepository.TrySaveLifecycleAsync(
                ownership,
                expectedSessionVersion,
                session,
                scope.Transaction,
                cancellationToken);
            if (!saved)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            if (authorizationKernel is not null && settings is not null)
            {
                var commitDecision = await SessionInvocationExecuteCommitAuthorization.ReauthorizeAsync(
                    authorizationKernel,
                    settings.ServiceActor,
                    ownership,
                    Guid.NewGuid(),
                    settings.SourceChannel,
                    scope.Transaction,
                    cancellationToken);
                if (!commitDecision.IsPermitted)
                {
                    await scope.RollbackAsync(CancellationToken.None);
                    return false;
                }
            }

            await scope.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
