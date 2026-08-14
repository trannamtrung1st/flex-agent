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
    IOutboxItemWriter? outboxItemWriter = null)
{
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
                return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.Denied);
            }

            var authoritativeUtc = await runtimeRepository.ReadAuthoritativeUtcAsync(
                scope.Transaction,
                cancellationToken);
            var result = publicationHandler.Handle(command, session, authoritativeUtc);
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

            var saved = await runtimeRepository.TrySaveAgentResponsePublicationAsync(
                command.Ownership,
                command.ExpectedSessionVersion,
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
                    command.Actor,
                    command.Ownership,
                    command.CorrelationId,
                    command.SourceChannel,
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
