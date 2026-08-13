using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresCompleteInvocationCoordinator(
    PostgresConnectionAccessor connectionAccessor,
    PostgresSessionRuntimeRepository runtimeRepository,
    ICompleteInvocationHandler completionHandler,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null)
{
    private readonly IAuditEventWriter _auditEventWriter = auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter = outboxItemWriter ?? new PostgresOutboxItemWriter();

    public async Task<InvocationCompletionResult> CompleteAsync(
        CompleteInvocationCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(binding);

        if (command.Ownership != binding.Ownership)
        {
            return new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.OwnershipMismatch, null);
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
                return new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.Denied, null);
            }

            var existing = session.Invocations.FirstOrDefault(item =>
                string.Equals(item.AgentInvocationId, command.AgentInvocationId, StringComparison.Ordinal));
            var wasTerminal = existing?.IsTerminal == true;
            var authoritativeUtc = await runtimeRepository.ReadAuthoritativeUtcAsync(
                scope.Transaction,
                cancellationToken);
            var result = completionHandler.Handle(command, session, authoritativeUtc);
            if (wasTerminal)
            {
                await scope.CommitAsync(cancellationToken);
                return result;
            }

            if (!result.Succeeded
                && result.OutcomeCode != InvocationCompletionOutcomeCodes.EffectFailed)
            {
                await scope.RollbackAsync(cancellationToken);
                return result;
            }

            if (result.Invocation is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return result;
            }

            var saved = await runtimeRepository.TrySaveCompletionAsync(
                command.Ownership,
                command.ExpectedSessionVersion,
                session,
                result.Invocation,
                scope.Transaction,
                cancellationToken);
            if (!saved)
            {
                await scope.RollbackAsync(cancellationToken);
                return new InvocationCompletionResult(
                    false,
                    InvocationCompletionOutcomeCodes.StaleVersion,
                    result.Invocation);
            }

            await SessionRuntimePersistenceAudit.WriteAsync(
                _auditEventWriter,
                _outboxItemWriter,
                command.Actor,
                command.Ownership,
                command.CorrelationId,
                command.SourceChannel,
                SessionRuntimeAuditActions.CompleteInvocation,
                SessionRuntimeOutboxEventTypes.InvocationCompleted,
                result.Invocation.AgentInvocationId,
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
