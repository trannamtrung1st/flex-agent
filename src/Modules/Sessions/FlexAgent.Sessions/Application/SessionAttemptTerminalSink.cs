namespace FlexAgent.Sessions.Application;

public interface ISessionAttemptTerminalSink
{
    Task MapAsync(
        Guid organizationId,
        Guid attemptId,
        string attemptMapping,
        string reasonCategory,
        DateTimeOffset terminalAtUtc,
        object commitTransaction,
        CancellationToken cancellationToken = default);
}
