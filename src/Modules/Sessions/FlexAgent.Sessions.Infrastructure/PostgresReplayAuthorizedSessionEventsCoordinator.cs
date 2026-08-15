using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresReplayAuthorizedSessionEventsCoordinator(
    PostgresConnectionAccessor connectionAccessor,
    PostgresSessionRuntimeRepository runtimeRepository,
    IReplayAuthorizedSessionEventsHandler replayHandler)
{
    public async Task<AuthorizedSessionEventReplayResult> ReplayAsync(
        ReplayAuthorizedSessionEventsCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(binding);

        if (command.Ownership != binding.Ownership)
        {
            return new AuthorizedSessionEventReplayResult(
                false,
                SessionEventReplayOutcomeCodes.OwnershipMismatch,
                []);
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var session = await runtimeRepository.LoadSnapshotAsync(
                command.Ownership,
                binding,
                scope.Transaction,
                cancellationToken);
            if (session is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new AuthorizedSessionEventReplayResult(
                    false,
                    SessionEventReplayOutcomeCodes.Denied,
                    []);
            }

            var result = replayHandler.Handle(command, session);
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
