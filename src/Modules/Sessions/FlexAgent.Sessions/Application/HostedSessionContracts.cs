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
        string? terminateReasonCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        _ = (routeSessionId, commandType, commandId, idempotencyKey, expectedSessionVersion, messageText, terminateReasonCode, cancellationToken);
        return Task.FromResult<HostedSessionCommandResult?>(
            new HostedSessionCommandResult(false, "rejected", "session.denied", "none", []));
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
        string? terminateReasonCode,
        CancellationToken cancellationToken = default);
}
