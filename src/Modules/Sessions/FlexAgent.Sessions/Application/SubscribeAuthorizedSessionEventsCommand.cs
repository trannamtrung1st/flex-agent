using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public static class SessionEventSubscriptionRelationships
{
    public const string Participant = "participant";
    public const string Reviewer = "reviewer";
}

public sealed record SubscribeAuthorizedSessionEventsCommand(
    TrustedRuntimeActor Actor,
    Guid OrganizationId,
    Guid? ParticipantId,
    string Relationship,
    Guid UntrustedSessionId,
    string? UntrustedLastEventId);

public sealed record SessionEventSubscriptionAuthorization(bool IsPermitted);

public interface ISessionEventSubscriptionAccess
{
    Task<bool> HasCurrentSubscribePermissionAsync(
        TrustedRuntimeActor actor,
        Guid organizationId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

public interface ISubscribeAuthorizedSessionEventsHandler
{
    Task<SessionEventSubscriptionAuthorization> AuthorizeAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthorizedSessionEventReplayResult> ReplayAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class UnhostedSubscribeAuthorizedSessionEventsHandler : ISubscribeAuthorizedSessionEventsHandler
{
    public static UnhostedSubscribeAuthorizedSessionEventsHandler Instance { get; } = new();

    public Task<SessionEventSubscriptionAuthorization> AuthorizeAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult(new SessionEventSubscriptionAuthorization(false));
    }

    public Task<AuthorizedSessionEventReplayResult> ReplayAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult(new AuthorizedSessionEventReplayResult(
            false,
            SessionEventReplayOutcomeCodes.Denied,
            []));
    }
}

public sealed class SubscribeAuthorizedSessionEventsHandler(
    ITrustedSessionBindingSource bindings,
    ISessionEventSubscriptionAccess access,
    IReplayAuthorizedSessionEventsCoordinator replay)
    : ISubscribeAuthorizedSessionEventsHandler
{
    public async Task<SessionEventSubscriptionAuthorization> AuthorizeAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!HasTrustedActor(command)
            || !await access.HasCurrentSubscribePermissionAsync(
                command.Actor,
                command.OrganizationId,
                command.UntrustedSessionId,
                cancellationToken).ConfigureAwait(false))
        {
            return new SessionEventSubscriptionAuthorization(false);
        }

        return new SessionEventSubscriptionAuthorization(await TryResolveParticipantBindingAsync(command, cancellationToken).ConfigureAwait(false) is not null);
    }

    public async Task<AuthorizedSessionEventReplayResult> ReplayAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!HasTrustedActor(command)
            || !await access.HasCurrentSubscribePermissionAsync(
                command.Actor,
                command.OrganizationId,
                command.UntrustedSessionId,
                cancellationToken).ConfigureAwait(false))
        {
            return new AuthorizedSessionEventReplayResult(
                false,
                SessionEventReplayOutcomeCodes.Denied,
                []);
        }

        var binding = await TryResolveParticipantBindingAsync(command, cancellationToken).ConfigureAwait(false);
        if (binding is null)
        {
            return new AuthorizedSessionEventReplayResult(
                false,
                SessionEventReplayOutcomeCodes.Denied,
                []);
        }

        return await replay.ReplayAsync(
            new ReplayAuthorizedSessionEventsCommand(
                command.Actor,
                binding.Ownership,
                command.UntrustedLastEventId),
            binding,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TrustedSessionBinding?> TryResolveParticipantBindingAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        CancellationToken cancellationToken)
    {
        if (!HasTrustedActor(command)
            || !string.Equals(
                command.Relationship,
                SessionEventSubscriptionRelationships.Participant,
                StringComparison.Ordinal)
            || command.ParticipantId is null
            || command.ParticipantId == Guid.Empty
            || command.UntrustedSessionId == Guid.Empty
            || command.OrganizationId == Guid.Empty)
        {
            return null;
        }

        var binding = await bindings.GetForOrganizationSessionAsync(
            command.OrganizationId,
            command.UntrustedSessionId,
            cancellationToken).ConfigureAwait(false);
        if (binding is null || binding.Ownership.ParticipantId != command.ParticipantId)
        {
            return null;
        }

        return binding;
    }

    private static bool HasTrustedActor(SubscribeAuthorizedSessionEventsCommand command) =>
        command.Actor.ActorId != Guid.Empty && !string.IsNullOrWhiteSpace(command.Actor.ActorType);
}
