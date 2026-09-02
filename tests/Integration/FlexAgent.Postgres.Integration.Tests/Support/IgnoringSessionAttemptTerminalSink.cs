using FlexAgent.Sessions.Application;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal sealed class IgnoringSessionAttemptTerminalSink : ISessionAttemptTerminalSink
{
    public static IgnoringSessionAttemptTerminalSink Instance { get; } = new();

    public Task MapAsync(
        Guid organizationId,
        Guid attemptId,
        string attemptMapping,
        string reasonCategory,
        DateTimeOffset terminalAtUtc,
        object commitTransaction,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
