using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresInvocationWorkSessionGateway(
    PostgresConnectionAccessor connectionAccessor,
    PostgresSessionRuntimeRepository runtimeRepository,
    ITrustedSessionBindingSource bindingSource,
    DurableInvocationWorkSettings settings,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null) : IInvocationWorkSessionGateway
{
    private readonly IAuditEventWriter _auditEventWriter = auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter = outboxItemWriter ?? new PostgresOutboxItemWriter();

    public async Task<LoadedInvocationWorkSession?> LoadAsync(
        SessionOwnership ownership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        var binding = await bindingSource.GetAsync(ownership, cancellationToken);
        if (binding is null || binding.Ownership != ownership)
        {
            return null;
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var session = await runtimeRepository.LoadSnapshotAsync(
                ownership,
                binding,
                scope.Transaction,
                cancellationToken);
            if (session is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return null;
            }

            await scope.CommitAsync(cancellationToken);
            return new LoadedInvocationWorkSession(session, binding, session.SessionVersion);
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<DateTimeOffset> ReadAuthoritativeUtcAsync(CancellationToken cancellationToken)
    {
        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var utc = await runtimeRepository.ReadAuthoritativeUtcAsync(scope.Transaction, cancellationToken);
            await scope.CommitAsync(cancellationToken);
            return utc;
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> TrySaveCompletionAsync(
        SessionOwnership ownership,
        long expectedSessionVersion,
        SessionRuntime session,
        AgentInvocation invocation,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(invocation);

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var saved = await runtimeRepository.TrySaveCompletionAsync(
                ownership,
                expectedSessionVersion,
                session,
                invocation,
                scope.Transaction,
                cancellationToken);
            if (!saved)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var authoritativeUtc = await runtimeRepository.ReadAuthoritativeUtcAsync(
                scope.Transaction,
                cancellationToken);
            await SessionRuntimePersistenceAudit.WriteAsync(
                _auditEventWriter,
                _outboxItemWriter,
                settings.ServiceActor,
                ownership,
                correlationId,
                settings.SourceChannel,
                SessionRuntimeAuditActions.CompleteInvocation,
                SessionRuntimeOutboxEventTypes.InvocationCompleted,
                invocation.AgentInvocationId,
                authoritativeUtc,
                scope.Transaction,
                cancellationToken);

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
