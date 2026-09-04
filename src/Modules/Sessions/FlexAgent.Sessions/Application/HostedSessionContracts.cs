using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public static class HostedSessionRelationships
{
    public static string ProjectionKind(string relationship) =>
        relationship switch
        {
            SessionEventSubscriptionRelationships.Administrator => HostedSessionProjectionKinds.Administrator,
            SessionEventSubscriptionRelationships.Reviewer => HostedSessionProjectionKinds.Historical,
            _ => HostedSessionProjectionKinds.Participant,
        };
}

public sealed record HostedSessionQueryResult(
    bool Found,
    string OutcomeCode,
    HostedSessionSnapshot? Snapshot);

public sealed record HostedSessionCommandResult(
    bool Succeeded,
    string OutcomeCategory,
    string OutcomeCode,
    string PermittedRecoveryAction,
    IReadOnlyList<string> PermittedActions,
    long? SessionVersion = null,
    long? SessionSequence = null,
    string? AcceptedMessageId = null);

public interface IHostedSessionSubjectSource : ISessionEventSubjectSource;

public interface IHostedSessionAccess
{
    Task<bool> HasCurrentPermissionAsync(
        TrustedRuntimeActor actor,
        Guid organizationId,
        Guid sessionId,
        string action,
        CancellationToken cancellationToken = default);
}

public interface IHostedSessionFrozenTimingSource
{
    Task<HostedFrozenTimingPolicy> LoadAsync(
        Guid organizationId,
        Guid sessionId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);
}

public interface IHostedSessionSnapshotQuery
{
    Task<HostedSessionQueryResult> GetAsync(
        TrustedRuntimeActor actor,
        Guid untrustedSessionId,
        CancellationToken cancellationToken = default);
}

public sealed class UnhostedHostedSessionSnapshotQuery : IHostedSessionSnapshotQuery
{
    public static UnhostedHostedSessionSnapshotQuery Instance { get; } = new();

    public Task<HostedSessionQueryResult> GetAsync(
        TrustedRuntimeActor actor,
        Guid untrustedSessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        _ = (untrustedSessionId, cancellationToken);
        return Task.FromResult(new HostedSessionQueryResult(false, "session.denied", null));
    }
}

public sealed class UnhostedHostedSessionCommandCoordinator : IHostedSessionCommandCoordinator
{
    public static UnhostedHostedSessionCommandCoordinator Instance { get; } = new();

    public Task<HostedSessionCommandResult?> SubmitAsync(
        TrustedRuntimeActor actor,
        Guid routeSessionId,
        string commandType,
        string commandId,
        string idempotencyKey,
        long expectedSessionVersion,
        string? messageText,
        string? pauseReasonCode,
        string? terminateReasonCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        _ = (routeSessionId, commandType, commandId, idempotencyKey, expectedSessionVersion, messageText, pauseReasonCode, terminateReasonCode, cancellationToken);
        return Task.FromResult<HostedSessionCommandResult?>(
            new HostedSessionCommandResult(false, "rejected", "session.denied", "none", []));
    }
}

public sealed record HostedSessionExpirySettings(
    TrustedRuntimeActor ServiceActor,
    string SourceChannel);

public static class HostedSessionExpiryChannels
{
    public const string Service = "session.hosted.expiry";
}

public interface IHostedSessionExpirySweep
{
    Task<int> ExpireDueAsync(CancellationToken cancellationToken = default);
}

public sealed class IdleHostedSessionExpirySweep : IHostedSessionExpirySweep
{
    public static IdleHostedSessionExpirySweep Instance { get; } = new();

    public Task<int> ExpireDueAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(0);
    }
}

public interface IHostedSessionCommandCoordinator
{
    Task<HostedSessionCommandResult?> SubmitAsync(
        TrustedRuntimeActor actor,
        Guid routeSessionId,
        string commandType,
        string commandId,
        string idempotencyKey,
        long expectedSessionVersion,
        string? messageText,
        string? pauseReasonCode,
        string? terminateReasonCode,
        CancellationToken cancellationToken = default);
}
