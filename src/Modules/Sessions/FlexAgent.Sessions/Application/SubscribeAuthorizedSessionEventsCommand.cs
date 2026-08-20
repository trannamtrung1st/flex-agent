using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public static class SessionEventSubscriptionRelationships
{
    public const string Participant = "participant";
    public const string Reviewer = "reviewer";
    public const string Administrator = "administrator";
}

public sealed record SubscribeAuthorizedSessionEventsCommand(
    TrustedRuntimeActor Actor,
    Guid UntrustedSessionId,
    string? UntrustedLastEventId);

public sealed record SessionEventSubject(
    Guid ActorId,
    string ActorType,
    Guid OrganizationId,
    Guid? ParticipantId,
    string Relationship);

public sealed record SessionEventSubscriptionAuthorization(
    bool IsPermitted,
    string? Relationship = null,
    Guid? OrganizationId = null);

/// <summary>
/// Current organization, participant, and relationship for an authenticated
/// actor on one requested Session. Resolve from trusted records
/// (session → organization → activity → participant/enrollment → current
/// relationship) per ADR-002. Do not use a global actor-to-one-relationship map.
/// </summary>
public interface ISessionEventSubjectSource
{
    Task<SessionEventSubject?> ResolveCurrentAsync(
        TrustedRuntimeActor actor,
        Guid untrustedSessionId,
        CancellationToken cancellationToken = default);
}

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
    IReplayAuthorizedSessionEventsCoordinator replay,
    ISessionEventSubjectSource subjects)
    : ISubscribeAuthorizedSessionEventsHandler
{
    public async Task<SessionEventSubscriptionAuthorization> AuthorizeAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var subject = await TryGetCurrentSubjectAsync(command, cancellationToken).ConfigureAwait(false);
        if (subject is null
            || !await access.HasCurrentSubscribePermissionAsync(
                command.Actor,
                subject.OrganizationId,
                command.UntrustedSessionId,
                cancellationToken).ConfigureAwait(false))
        {
            return new SessionEventSubscriptionAuthorization(false);
        }

        var permitted = await TryResolveParticipantBindingAsync(command, subject, cancellationToken)
            .ConfigureAwait(false) is not null;
        return new SessionEventSubscriptionAuthorization(
            permitted,
            permitted ? subject.Relationship : null,
            permitted ? subject.OrganizationId : null);
    }

    public async Task<AuthorizedSessionEventReplayResult> ReplayAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var subject = await TryGetCurrentSubjectAsync(command, cancellationToken).ConfigureAwait(false);
        if (subject is null
            || !await access.HasCurrentSubscribePermissionAsync(
                command.Actor,
                subject.OrganizationId,
                command.UntrustedSessionId,
                cancellationToken).ConfigureAwait(false))
        {
            return new AuthorizedSessionEventReplayResult(
                false,
                SessionEventReplayOutcomeCodes.Denied,
                []);
        }

        var binding = await TryResolveParticipantBindingAsync(command, subject, cancellationToken).ConfigureAwait(false);
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

    private async Task<SessionEventSubject?> TryGetCurrentSubjectAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        CancellationToken cancellationToken)
    {
        if (!HasTrustedActor(command))
        {
            return null;
        }

        var subject = await subjects.ResolveCurrentAsync(
            command.Actor,
            command.UntrustedSessionId,
            cancellationToken).ConfigureAwait(false);
        if (subject is null
            || subject.ActorId != command.Actor.ActorId
            || !string.Equals(subject.ActorType, command.Actor.ActorType, StringComparison.Ordinal)
            || subject.OrganizationId == Guid.Empty)
        {
            return null;
        }

        return subject;
    }

    private async Task<TrustedSessionBinding?> TryResolveParticipantBindingAsync(
        SubscribeAuthorizedSessionEventsCommand command,
        SessionEventSubject subject,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(
                subject.Relationship,
                SessionEventSubscriptionRelationships.Participant,
                StringComparison.Ordinal)
            || subject.ParticipantId is null
            || subject.ParticipantId == Guid.Empty
            || command.UntrustedSessionId == Guid.Empty)
        {
            return null;
        }

        var binding = await bindings.GetForOrganizationSessionAsync(
            subject.OrganizationId,
            command.UntrustedSessionId,
            cancellationToken).ConfigureAwait(false);
        if (binding is null || binding.Ownership.ParticipantId != subject.ParticipantId)
        {
            return null;
        }

        return binding;
    }

    private static bool HasTrustedActor(SubscribeAuthorizedSessionEventsCommand command) =>
        command.Actor.ActorId != Guid.Empty && !string.IsNullOrWhiteSpace(command.Actor.ActorType);
}
