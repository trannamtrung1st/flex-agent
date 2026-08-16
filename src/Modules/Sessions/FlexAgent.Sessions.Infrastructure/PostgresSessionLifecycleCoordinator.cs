using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresSessionLifecycleCoordinator(
    PostgresConnectionAccessor connectionAccessor,
    PostgresSessionRuntimeRepository runtimeRepository,
    IChangeSessionLifecycleHandler lifecycleHandler,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null)
{
    private readonly IAuditEventWriter _auditEventWriter = auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter = outboxItemWriter ?? new PostgresOutboxItemWriter();

    public async Task<SessionLifecycleChangeResult> ChangeAsync(
        ChangeSessionLifecycleCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(binding);

        if (command.Ownership != binding.Ownership)
        {
            return new SessionLifecycleChangeResult(
                false,
                SessionLifecycleOutcomeCodes.OwnershipMismatch,
                SessionLifecycleState.Ready,
                0);
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var session = await runtimeRepository.LoadForUpdateAsync(
                command.Ownership,
                binding,
                scope.Transaction,
                cancellationToken);
            if (session is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new SessionLifecycleChangeResult(
                    false,
                    SessionLifecycleOutcomeCodes.Denied,
                    SessionLifecycleState.Ready,
                    0);
            }

            var authoritativeUtc = await runtimeRepository.ReadAuthoritativeUtcAsync(
                scope.Transaction,
                cancellationToken);
            var result = lifecycleHandler.Handle(command, session, authoritativeUtc);
            if (!result.Succeeded)
            {
                await scope.RollbackAsync(cancellationToken);
                return result;
            }

            if (result.OutcomeCode == SessionLifecycleOutcomeCodes.Reconciled)
            {
                await scope.RollbackAsync(cancellationToken);
                return result;
            }

            var pendingSeals = session.PendingPublicationWork
                .Where(message => message.SealDirty)
                .ToArray();
            var saved = await runtimeRepository.TrySaveLifecycleAsync(
                command.Ownership,
                command.ExpectedSessionVersion,
                session,
                scope.Transaction,
                cancellationToken);
            if (!saved)
            {
                await scope.RollbackAsync(cancellationToken);
                return new SessionLifecycleChangeResult(
                    false,
                    SessionLifecycleOutcomeCodes.StaleVersion,
                    session.LifecycleState,
                    session.SessionVersion);
            }

            await SessionRuntimePersistenceAudit.WriteAsync(
                _auditEventWriter,
                _outboxItemWriter,
                command.Actor,
                command.Ownership,
                command.CorrelationId,
                command.SourceChannel,
                SessionRuntimeAuditActions.ChangeLifecycle,
                SessionRuntimeOutboxEventTypes.LifecycleChanged,
                $"{command.Transition}:{session.LifecycleState}:{session.SessionVersion}",
                authoritativeUtc,
                scope.Transaction,
                cancellationToken);

            foreach (var message in pendingSeals)
            {
                await SessionRuntimePersistenceAudit.WriteAsync(
                    _auditEventWriter,
                    _outboxItemWriter,
                    command.Actor,
                    command.Ownership,
                    command.CorrelationId,
                    command.SourceChannel,
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

            await scope.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
