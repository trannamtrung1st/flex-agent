using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresAcceptParticipantMessageCoordinator(
    PostgresConnectionAccessor connectionAccessor,
    PostgresSessionRuntimeRepository runtimeRepository,
    IAcceptParticipantMessageHandler acceptHandler,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null)
{
    private readonly IAuditEventWriter _auditEventWriter = auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter = outboxItemWriter ?? new PostgresOutboxItemWriter();

    public async Task<TriggerAdmissionResult> AcceptAsync(
        AcceptParticipantMessageCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(binding);

        if (command.Ownership != binding.Ownership)
        {
            return new TriggerAdmissionResult(
                false,
                TriggerAdmissionOutcomeCodes.OwnershipMismatch,
                null,
                null);
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
                return new TriggerAdmissionResult(false, TriggerAdmissionOutcomeCodes.Denied, null, null);
            }

            var authoritativeUtc = await runtimeRepository.ReadAuthoritativeUtcAsync(
                scope.Transaction,
                cancellationToken);
            var result = acceptHandler.Handle(command, session, authoritativeUtc);
            if (!result.Succeeded || result.Invocation is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return result;
            }

            if (result.OutcomeCode == TriggerAdmissionOutcomeCodes.Reconciled)
            {
                await scope.CommitAsync(cancellationToken);
                return result;
            }

            var saved = await runtimeRepository.TrySaveAdmissionAsync(
                command.Ownership,
                command.ExpectedSessionVersion,
                session,
                result.Invocation,
                scope.Transaction,
                cancellationToken);
            if (!saved)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TriggerAdmissionResult(false, TriggerAdmissionOutcomeCodes.StaleVersion, null, null);
            }

            await SessionRuntimePersistenceAudit.WriteAsync(
                _auditEventWriter,
                _outboxItemWriter,
                command.Actor,
                command.Ownership,
                command.CorrelationId,
                command.SourceChannel,
                SessionRuntimeAuditActions.AcceptParticipantMessage,
                SessionRuntimeOutboxEventTypes.ParticipantMessageAccepted,
                command.ParticipantMessageId,
                authoritativeUtc,
                scope.Transaction,
                cancellationToken);

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
