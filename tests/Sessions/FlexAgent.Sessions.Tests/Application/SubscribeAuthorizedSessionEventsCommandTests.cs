using System.Reflection;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class SubscribeAuthorizedSessionEventsCommandTests
{
    [Fact]
    public void Command_requires_trusted_actor_and_untrusted_cursor_without_frozen_relationship()
    {
        var ctor = typeof(SubscribeAuthorizedSessionEventsCommand)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();
        var parameters = ctor.GetParameters();

        Assert.Contains(parameters, parameter => parameter.Name == "Actor" && parameter.ParameterType == typeof(TrustedRuntimeActor));
        Assert.Contains(parameters, parameter => parameter.Name == "UntrustedSessionId" && parameter.ParameterType == typeof(Guid));
        Assert.Contains(parameters, parameter => parameter.Name == "UntrustedLastEventId" && parameter.ParameterType == typeof(string));
        Assert.DoesNotContain(parameters, parameter => parameter.Name is "OrganizationId" or "ParticipantId" or "Relationship");
        Assert.DoesNotContain(parameters, parameter => parameter.Name is "utcNow" or "authoritativeUtc" or "timestamp" or "clock");
        Assert.DoesNotContain(parameters, parameter => parameter.Name is "LastEventId" or "ActorIdHeader" or "Cookie");
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("FlexAgent.Contracts", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Authorize_denies_reviewer_and_does_not_replay()
    {
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var binding = SessionRuntimeTestFixtures.CreateBinding(ownership: ownership);
        var access = new RecordingAccess(permit: true);
        var replay = new RecordingReplay();
        var handler = CreateHandler(
            binding,
            access,
            replay,
            Subject(ownership, SessionEventSubscriptionRelationships.Reviewer));
        var reviewer = ParticipantCommand(ownership, "12");

        var authorization = await handler.AuthorizeAsync(reviewer, CancellationToken.None);

        Assert.False(authorization.IsPermitted);
        Assert.Equal(1, access.Calls);
        Assert.Equal(0, replay.Calls);
    }

    [Fact]
    public async Task Authorize_denies_wrong_participant_ownership_without_replay()
    {
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var binding = SessionRuntimeTestFixtures.CreateBinding(ownership: ownership);
        var otherParticipant = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var handler = CreateHandler(
            binding,
            new RecordingAccess(permit: true),
            new RecordingReplay(),
            Subject(ownership, SessionEventSubscriptionRelationships.Participant, otherParticipant));

        var authorization = await handler.AuthorizeAsync(
            ParticipantCommand(ownership, null),
            CancellationToken.None);

        Assert.False(authorization.IsPermitted);
    }

    [Fact]
    public async Task Authorize_denies_guessed_session_id_without_replay()
    {
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var binding = SessionRuntimeTestFixtures.CreateBinding(ownership: ownership);
        var replay = new RecordingReplay();
        var handler = CreateHandler(
            binding,
            new RecordingAccess(permit: true),
            replay,
            Subject(ownership, SessionEventSubscriptionRelationships.Participant));

        var authorization = await handler.AuthorizeAsync(
            new SubscribeAuthorizedSessionEventsCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                "1"),
            CancellationToken.None);

        Assert.False(authorization.IsPermitted);
        Assert.Equal(0, replay.Calls);
    }

    [Fact]
    public async Task Authorize_denies_when_kernel_rejects_even_with_matching_ownership()
    {
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var binding = SessionRuntimeTestFixtures.CreateBinding(ownership: ownership);
        var handler = CreateHandler(
            binding,
            new RecordingAccess(permit: false),
            new RecordingReplay(),
            Subject(ownership, SessionEventSubscriptionRelationships.Participant));

        var authorization = await handler.AuthorizeAsync(
            ParticipantCommand(ownership, "99"),
            CancellationToken.None);

        Assert.False(authorization.IsPermitted);
    }

    [Fact]
    public async Task Authorize_denies_after_relationship_narrows_while_org_grant_remains()
    {
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var binding = SessionRuntimeTestFixtures.CreateBinding(ownership: ownership);
        var subjects = new MutableSubjectSource(
            Subject(ownership, SessionEventSubscriptionRelationships.Participant));
        var access = new RecordingAccess(permit: true);
        var replay = new RecordingReplay();
        var handler = new SubscribeAuthorizedSessionEventsHandler(
            new MemoryBindingSource(binding),
            access,
            replay,
            subjects);
        var command = ParticipantCommand(ownership, "4");

        Assert.True((await handler.AuthorizeAsync(command, CancellationToken.None)).IsPermitted);

        subjects.Current = Subject(ownership, SessionEventSubscriptionRelationships.Reviewer);
        var afterNarrow = await handler.AuthorizeAsync(command, CancellationToken.None);

        Assert.False(afterNarrow.IsPermitted);
        Assert.Equal(2, access.Calls);
        Assert.Equal(0, replay.Calls);
    }

    [Fact]
    public async Task Replay_maps_trusted_actor_and_ownership_and_passes_untrusted_cursor()
    {
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var binding = SessionRuntimeTestFixtures.CreateBinding(ownership: ownership);
        var replay = new RecordingReplay
        {
            Result = new AuthorizedSessionEventReplayResult(
                true,
                SessionEventReplayOutcomeCodes.Succeeded,
                [
                    new AuthorizedSessionProjectionEvent(
                        AuthorizedSessionEventTypes.AgentFragment,
                        ownership.SessionId.ToString("D"),
                        "4",
                        "2026-08-17T00:00:00Z",
                        "Agent response fragment published.",
                        1,
                        "msg.agent.1",
                        "Hel"),
                ]),
        };
        var handler = CreateHandler(
            binding,
            new RecordingAccess(permit: true),
            replay,
            Subject(ownership, SessionEventSubscriptionRelationships.Participant));
        var command = ParticipantCommand(ownership, "3");
        var authorization = await handler.AuthorizeAsync(command, CancellationToken.None);

        Assert.True(authorization.IsPermitted);
        var result = await handler.ReplayAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("3", replay.LastCommand?.UntrustedLastEventId);
        Assert.Equal(command.Actor, replay.LastCommand?.Actor);
        Assert.Equal(ownership, replay.LastCommand?.Ownership);
        Assert.Equal("Hel", result.Events[0].TextDelta);
        Assert.DoesNotContain("adec.", result.Events[0].Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Replay_does_not_treat_last_event_id_as_actor_or_ownership()
    {
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var binding = SessionRuntimeTestFixtures.CreateBinding(ownership: ownership);
        var replay = new RecordingReplay
        {
            Result = new AuthorizedSessionEventReplayResult(
                false,
                SessionEventReplayOutcomeCodes.Reconcile,
                []),
        };
        var handler = CreateHandler(
            binding,
            new RecordingAccess(permit: true),
            replay,
            Subject(ownership, SessionEventSubscriptionRelationships.Participant));
        var stolenCursor = Guid.NewGuid().ToString("D");
        var command = ParticipantCommand(ownership, stolenCursor);

        var authorization = await handler.AuthorizeAsync(command, CancellationToken.None);
        var result = await handler.ReplayAsync(command, CancellationToken.None);

        Assert.True(authorization.IsPermitted);
        Assert.Equal(stolenCursor, replay.LastCommand?.UntrustedLastEventId);
        Assert.NotEqual(stolenCursor, replay.LastCommand?.Actor.ActorId.ToString("D"));
        Assert.Equal(ownership.SessionId, replay.LastCommand?.Ownership.SessionId);
        Assert.Empty(result.Events);
        Assert.Equal(SessionEventReplayOutcomeCodes.Reconcile, result.OutcomeCode);
    }

    private static SubscribeAuthorizedSessionEventsHandler CreateHandler(
        TrustedSessionBinding binding,
        RecordingAccess access,
        RecordingReplay replay,
        SessionEventSubject subject) =>
        new(
            new MemoryBindingSource(binding),
            access,
            replay,
            new MutableSubjectSource(subject));

    private static SessionEventSubject Subject(
        SessionOwnership ownership,
        string relationship,
        Guid? participantId = null)
    {
        var actor = SessionRuntimeTestFixtures.CreateActor();
        return new SessionEventSubject(
            actor.ActorId,
            actor.ActorType,
            ownership.OrganizationId,
            participantId ?? ownership.ParticipantId,
            relationship);
    }

    private static SubscribeAuthorizedSessionEventsCommand ParticipantCommand(
        SessionOwnership ownership,
        string? lastEventId) =>
        new(
            SessionRuntimeTestFixtures.CreateActor(),
            ownership.SessionId,
            lastEventId);

    private sealed class MutableSubjectSource(SessionEventSubject current) : ISessionEventSubjectSource
    {
        public SessionEventSubject Current { get; set; } = current;

        public Task<SessionEventSubject?> GetCurrentAsync(
            Guid actorId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SessionEventSubject?>(Current.ActorId == actorId ? Current : null);
    }

    private sealed class MemoryBindingSource(TrustedSessionBinding binding) : ITrustedSessionBindingSource
    {
        public Task<TrustedSessionBinding?> GetAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken) =>
            Task.FromResult(ownership == binding.Ownership ? binding : null);

        public Task<TrustedSessionBinding?> GetForOrganizationSessionAsync(
            Guid organizationId,
            Guid sessionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                binding.Ownership.OrganizationId == organizationId && binding.Ownership.SessionId == sessionId
                    ? binding
                    : null);
    }

    private sealed class RecordingAccess(bool permit) : ISessionEventSubscriptionAccess
    {
        public int Calls { get; private set; }

        public Task<bool> HasCurrentSubscribePermissionAsync(
            TrustedRuntimeActor actor,
            Guid organizationId,
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(permit);
        }
    }

    private sealed class RecordingReplay : IReplayAuthorizedSessionEventsCoordinator
    {
        public int Calls { get; private set; }

        public ReplayAuthorizedSessionEventsCommand? LastCommand { get; private set; }

        public AuthorizedSessionEventReplayResult Result { get; set; } =
            new(false, SessionEventReplayOutcomeCodes.Denied, []);

        public Task<AuthorizedSessionEventReplayResult> ReplayAsync(
            ReplayAuthorizedSessionEventsCommand command,
            TrustedSessionBinding binding,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastCommand = command;
            return Task.FromResult(Result);
        }
    }
}
