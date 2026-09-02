using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Submissions.Infrastructure;

public sealed class SubmissionsSessionAttemptTerminalSink(IAttemptTerminalMappingPort port)
    : ISessionAttemptTerminalSink
{
    public Task MapAsync(
        Guid organizationId,
        Guid attemptId,
        string attemptMapping,
        string reasonCategory,
        DateTimeOffset terminalAtUtc,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        var transaction = new AttachedPostgresEnrollmentTransaction(
            PostgresCommitTransaction.Required(commitTransaction));
        return port.MapTerminalAsync(
            organizationId,
            attemptId,
            attemptMapping,
            reasonCategory,
            terminalAtUtc.ToOffset(TimeSpan.Zero),
            transaction,
            cancellationToken);
    }
}
