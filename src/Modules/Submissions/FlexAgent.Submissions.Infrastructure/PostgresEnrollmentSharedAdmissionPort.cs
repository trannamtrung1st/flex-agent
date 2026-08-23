using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Submissions.Application;
using Npgsql;

namespace FlexAgent.Submissions.Infrastructure;

public sealed class PostgresEnrollmentSharedAdmissionPort(
    PostgresConnectionAccessor connections,
    EnrollmentSharedAdmissionSettings settings) : IEnrollmentSharedAdmissionPort
{
    public async Task<EnrollmentSharedAdmissionResult> AcquireAsync(
        Guid organizationId,
        Guid actorId,
        string surface,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.Timeout);
        try
        {
            await using var connection = await connections.OpenConnectionAsync(timeout.Token);
            var row = await connection.QuerySingleOrDefaultAsync<AdmissionRow>(
                new CommandDefinition(
                    """
                    SELECT decision, retry_after_seconds, permit_count
                    FROM submissions_try_acquire_enrollment_request_permit(
                        @OrganizationId,
                        @ActorId,
                        @Surface,
                        @PolicyRevision,
                        @ReadPermitLimit,
                        @MutationPermitLimit,
                        @WindowSeconds,
                        @CleanupBatchSize);
                    """,
                    new
                    {
                        OrganizationId = organizationId,
                        ActorId = actorId,
                        Surface = surface,
                        settings.PolicyRevision,
                        settings.ReadPermitLimit,
                        settings.MutationPermitLimit,
                        settings.WindowSeconds,
                        settings.CleanupBatchSize,
                    },
                    cancellationToken: timeout.Token));
            return Map(row);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return EnrollmentSharedAdmissionResult.Unavailable();
        }
        catch (NpgsqlException)
        {
            return EnrollmentSharedAdmissionResult.Unavailable();
        }
        catch (TimeoutException)
        {
            return EnrollmentSharedAdmissionResult.Unavailable();
        }
    }

    public async Task<bool> PolicyMatchesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.Timeout);
        try
        {
            await using var connection = await connections.OpenConnectionAsync(timeout.Token);
            var row = await connection.QuerySingleOrDefaultAsync<PolicyRow>(
                new CommandDefinition(
                    """
                    SELECT policy_revision, read_permit_limit, mutation_permit_limit, window_seconds
                    FROM submissions_enrollment_request_policies
                    WHERE singleton_key = 1;
                    """,
                    cancellationToken: timeout.Token));
            return row is not null
                && row.PolicyRevision == settings.PolicyRevision
                && row.ReadPermitLimit == settings.ReadPermitLimit
                && row.MutationPermitLimit == settings.MutationPermitLimit
                && row.WindowSeconds == settings.WindowSeconds;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (NpgsqlException)
        {
            return false;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static EnrollmentSharedAdmissionResult Map(AdmissionRow? row) =>
        row?.Decision switch
        {
            "permitted" => EnrollmentSharedAdmissionResult.Permitted(),
            "exhausted" => EnrollmentSharedAdmissionResult.Exhausted(row.RetryAfterSeconds),
            _ => EnrollmentSharedAdmissionResult.Unavailable(),
        };

    private sealed record AdmissionRow(string Decision, int RetryAfterSeconds, int PermitCount);

    private sealed record PolicyRow(
        int PolicyRevision,
        int ReadPermitLimit,
        int MutationPermitLimit,
        int WindowSeconds);
}
