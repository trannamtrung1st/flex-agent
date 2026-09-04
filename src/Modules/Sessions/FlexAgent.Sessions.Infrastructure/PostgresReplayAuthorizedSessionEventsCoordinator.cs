using System.Data;
using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using Npgsql;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresReplayAuthorizedSessionEventsCoordinator(
    PostgresConnectionAccessor connectionAccessor,
    PostgresSessionRuntimeRepository runtimeRepository,
    IReplayAuthorizedSessionEventsHandler replayHandler,
    IHostedSessionFrozenTimingSource? frozenTimingSource = null)
    : IReplayAuthorizedSessionEventsCoordinator
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

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            IsolationLevel.RepeatableRead,
            cancellationToken);
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

            var result = replayHandler.Handle(
                command.UseHostedProjection
                    ? command with
                    {
                        HostedProjectionOptions = await BuildHostedProjectionOptionsAsync(
                            command,
                            scope.Transaction,
                            cancellationToken),
                    }
                    : command,
                session);
            await scope.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<HostedSessionEventProjectionOptions?> BuildHostedProjectionOptionsAsync(
        ReplayAuthorizedSessionEventsCommand command,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (frozenTimingSource is null)
        {
            return null;
        }

        var timingPolicy = await frozenTimingSource.LoadAsync(
            command.Ownership.OrganizationId,
            command.Ownership.SessionId,
            DateTimeOffset.UtcNow,
            cancellationToken);
        var startedAt = await transaction.Connection!.QuerySingleAsync<DateTimeOffset>(
            new CommandDefinition(
                """
                SELECT created_at
                FROM session_runtimes
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.Ownership.SessionId,
                },
                transaction,
                cancellationToken: cancellationToken));
        var warningOccurrences = (await transaction.Connection!.QueryAsync<HostedSessionWarningOccurrence>(
            new CommandDefinition(
                """
                SELECT warning_threshold_id AS WarningThresholdId,
                       warning_code AS WarningCode,
                       remaining_seconds_threshold AS RemainingSecondsThreshold,
                       due_at AS DueAt,
                       committed_at AS CommittedAt,
                       session_sequence AS SessionSequence,
                       remaining_seconds_at_commit AS RemainingSecondsAtCommit,
                       delivery_status AS DeliveryStatus
                FROM session_warning_occurrences
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                ORDER BY session_sequence
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.Ownership.SessionId,
                },
                transaction,
                cancellationToken: cancellationToken))).AsList();
        return new HostedSessionEventProjectionOptions(
            startedAt,
            timingPolicy,
            DateTimeOffset.UtcNow,
            warningOccurrences);
    }
}
