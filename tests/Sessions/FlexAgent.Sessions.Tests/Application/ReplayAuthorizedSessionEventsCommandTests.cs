using System.Globalization;
using System.Reflection;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class ReplayAuthorizedSessionEventsCommandTests
{
    [Fact]
    public void Command_requires_trusted_actor_and_complete_ownership_without_client_text_or_clocks()
    {
        var ctor = typeof(ReplayAuthorizedSessionEventsCommand)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();
        var parameters = ctor.GetParameters();

        Assert.Contains(parameters, parameter => parameter.Name == "Actor" && parameter.ParameterType == typeof(TrustedRuntimeActor));
        Assert.Contains(parameters, parameter => parameter.Name == "Ownership" && parameter.ParameterType == typeof(SessionOwnership));
        Assert.Contains(parameters, parameter => parameter.Name == "UntrustedLastEventId" && parameter.ParameterType == typeof(string));
        Assert.DoesNotContain(parameters, parameter => parameter.Name is "utcNow" or "authoritativeUtc" or "timestamp" or "clock");
        Assert.DoesNotContain(parameters, parameter => parameter.Name is "TextDelta" or "AssembledText" or "FragmentOrdinal" or "FragmentSequence" or "NextFragmentPosition");
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTime));
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTimeOffset));
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("FlexAgent.Contracts", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Handler_requires_a_server_loaded_session_and_does_not_accept_client_clocks()
    {
        var method = typeof(IReplayAuthorizedSessionEventsHandler)
            .GetMethod(nameof(IReplayAuthorizedSessionEventsHandler.Handle));
        Assert.NotNull(method);
        var parameters = method!.GetParameters();

        Assert.Contains(parameters, parameter => parameter.ParameterType == typeof(ReplayAuthorizedSessionEventsCommand));
        Assert.Contains(parameters, parameter => parameter.Name == "session" && parameter.ParameterType == typeof(SessionRuntime));
        Assert.DoesNotContain(parameters, parameter => parameter.Name is "utcNow" or "authoritativeUtc" or "timestamp" or "clock");
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTimeOffset));
    }

    [Fact]
    public void Replay_from_the_start_returns_committed_fragments_then_the_seal_in_session_order()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.replay.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "lo", "agen.replay.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);
        var firstSequence = session.AgentMessages[0].Fragments[0].SessionSequence;
        var secondSequence = session.AgentMessages[0].Fragments[1].SessionSequence;
        Assert.True(session.CompleteAgentResponseMessage(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(5)).Succeeded);
        var sealSequence = session.AgentMessages[0].SealedSessionSequence;
        Assert.True(sealSequence > secondSequence);

        var result = new ReplayAuthorizedSessionEventsHandler().Handle(
            new ReplayAuthorizedSessionEventsCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                session.Ownership,
                UntrustedLastEventId: null),
            session);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(SessionEventReplayOutcomeCodes.Succeeded, result.OutcomeCode);
        Assert.Equal(3, result.Events.Count);
        Assert.Equal(AuthorizedSessionEventTypes.AgentFragment, result.Events[0].EventType);
        Assert.Equal("Hel", result.Events[0].TextDelta);
        Assert.Equal(1, result.Events[0].FragmentSequence);
        Assert.Equal(firstSequence.ToString(CultureInfo.InvariantCulture), result.Events[0].SessionSequence);
        Assert.Equal(AuthorizedSessionEventTypes.AgentFragment, result.Events[1].EventType);
        Assert.Equal("lo", result.Events[1].TextDelta);
        Assert.Equal(AuthorizedSessionEventTypes.AgentComplete, result.Events[2].EventType);
        Assert.Equal(sealSequence!.Value.ToString(CultureInfo.InvariantCulture), result.Events[2].SessionSequence);
        Assert.Equal(session.AgentMessages[0].AssembledContentDigest, result.Events[2].AssembledContentDigest);
        Assert.Equal(2, result.Events[2].FragmentCount);
        Assert.Null(result.Events[2].TextDelta);
        Assert.All(result.Events, evt => Assert.Equal(session.AgentMessages[0].MessageId, evt.AgentMessageId));
        Assert.All(result.Events, evt => Assert.EndsWith("Z", evt.OccurredAt, StringComparison.Ordinal));
        Assert.False(result.HasMore);
    }

    [Fact]
    public void Replay_after_a_trusted_session_cursor_omits_earlier_fragments_and_does_not_use_client_text()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.replay.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        var firstSequence = session.AgentMessages[0].Fragments[0].SessionSequence;
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "lo", "agen.replay.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);
        Assert.True(session.CompleteAgentResponseMessage(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(5)).Succeeded);

        var result = new ReplayAuthorizedSessionEventsHandler().Handle(
            new ReplayAuthorizedSessionEventsCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                session.Ownership,
                firstSequence.ToString(CultureInfo.InvariantCulture)),
            session);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("lo", result.Events[0].TextDelta);
        Assert.Equal(AuthorizedSessionEventTypes.AgentComplete, result.Events[1].EventType);
        Assert.DoesNotContain(result.Events, evt => evt.TextDelta == "Hel");
    }

    [Fact]
    public void Malformed_or_future_cursor_reconciles_instead_of_optimistic_replay()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.replay.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);

        var handler = new ReplayAuthorizedSessionEventsHandler();
        var actor = SessionRuntimeTestFixtures.CreateActor();
        var malformed = handler.Handle(
            new ReplayAuthorizedSessionEventsCommand(actor, session.Ownership, "not-a-sequence"),
            session);
        var future = handler.Handle(
            new ReplayAuthorizedSessionEventsCommand(
                actor,
                session.Ownership,
                (session.SessionSequence + 1).ToString(CultureInfo.InvariantCulture)),
            session);
        var negative = handler.Handle(
            new ReplayAuthorizedSessionEventsCommand(actor, session.Ownership, "-1"),
            session);
        var zero = handler.Handle(
            new ReplayAuthorizedSessionEventsCommand(actor, session.Ownership, "0"),
            session);

        Assert.False(malformed.Succeeded);
        Assert.Equal(SessionEventReplayOutcomeCodes.Reconcile, malformed.OutcomeCode);
        Assert.Empty(malformed.Events);
        Assert.Equal(SessionEventReplayOutcomeCodes.Reconcile, future.OutcomeCode);
        Assert.Empty(future.Events);
        Assert.Equal(SessionEventReplayOutcomeCodes.Reconcile, negative.OutcomeCode);
        Assert.Empty(negative.Events);
        Assert.Equal(SessionEventReplayOutcomeCodes.Reconcile, zero.OutcomeCode);
        Assert.Empty(zero.Events);
    }

    [Fact]
    public void In_range_non_stream_session_sequence_reconciles_instead_of_becoming_a_trusted_cursor()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var firstInvocationId = ClaimParticipantPublication(session, "1");
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(firstInvocationId, 1, "Hel", "agen.replay.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        Assert.True(session.CompleteAgentResponseMessage(firstInvocationId, SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);
        var firstSeal = session.AgentMessages[0].SealedSessionSequence;
        Assert.NotNull(firstSeal);

        var secondAdmitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.2",
            "turn.2",
            "slot.2",
            "trig.participant.2",
            "idem.p.2",
            SessionRuntimeTestFixtures.T0.AddSeconds(5));
        Assert.True(secondAdmitted.Succeeded, secondAdmitted.OutcomeCode);
        var nonStreamSequence = session.Invocations[^1].SessionSequence;
        Assert.True(nonStreamSequence > firstSeal);
        Assert.True(nonStreamSequence <= session.SessionSequence);
        Assert.NotEqual(firstSeal, nonStreamSequence);

        var secondCompleted = session.CompleteInvocation(
            secondAdmitted.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.EmitMessage(
                secondAdmitted.Invocation.AgentInvocationId,
                turnId: "turn.2",
                responseSlotId: "slot.2"),
            SessionRuntimeTestFixtures.T0.AddSeconds(6));
        Assert.True(secondCompleted.Succeeded, secondCompleted.OutcomeCode);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(
                secondAdmitted.Invocation.AgentInvocationId,
                1,
                "later",
                "agen.replay.2"),
            SessionRuntimeTestFixtures.T0.AddSeconds(7)).Succeeded);

        var skipped = AuthorizedSessionEventProjector.Project(session, nonStreamSequence);
        Assert.DoesNotContain(skipped.Events, evt => evt.TextDelta == "Hel");

        var result = new ReplayAuthorizedSessionEventsHandler().Handle(
            new ReplayAuthorizedSessionEventsCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                session.Ownership,
                nonStreamSequence.ToString(CultureInfo.InvariantCulture)),
            session);

        Assert.False(result.Succeeded);
        Assert.Equal(SessionEventReplayOutcomeCodes.Reconcile, result.OutcomeCode);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void Replay_rejects_missing_actor_and_ownership_mismatch_without_leaking_events()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "secret", "agen.replay.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        var handler = new ReplayAuthorizedSessionEventsHandler();

        var denied = handler.Handle(
            new ReplayAuthorizedSessionEventsCommand(
                new TrustedRuntimeActor(Guid.Empty, "synthetic.test_actor"),
                session.Ownership,
                null),
            session);
        var mismatched = handler.Handle(
            new ReplayAuthorizedSessionEventsCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                SessionRuntimeTestFixtures.CreateOwnership() with { SessionId = Guid.NewGuid() },
                null),
            session);

        Assert.Equal(SessionEventReplayOutcomeCodes.Denied, denied.OutcomeCode);
        Assert.Empty(denied.Events);
        Assert.Equal(SessionEventReplayOutcomeCodes.OwnershipMismatch, mismatched.OutcomeCode);
        Assert.Empty(mismatched.Events);
    }

    [Fact]
    public void Uncertain_terminal_state_without_a_seal_sequence_reconciles()
    {
        var fragments = new[]
        {
            new AgentResponseFragment(1, 4, "Hel", ProtectedContentRef.DigestUtf8("Hel"), SessionRuntimeTestFixtures.T0),
        };
        var message = AgentResponseMessage.Rehydrate(
            "aout.uncertain.0001",
            "agen.1",
            "ainv.1",
            "adec.1",
            "turn.1",
            "slot.1",
            AgentMessageCompletionStates.Complete,
            ProtectedContentRef.DigestUtf8("Hel"),
            fragments,
            sealedSessionSequence: null);
        var session = SessionRuntime.Rehydrate(
            SessionRuntimeTestFixtures.CreateBinding(),
            SessionLifecycleState.Active,
            sessionVersion: 3,
            sessionSequence: 4,
            cutoffSequence: null,
            SessionRuntimeTestFixtures.T0,
            agentMessages: [message]);

        var result = new ReplayAuthorizedSessionEventsHandler().Handle(
            new ReplayAuthorizedSessionEventsCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                session.Ownership,
                null),
            session);

        Assert.False(result.Succeeded);
        Assert.Equal(SessionEventReplayOutcomeCodes.Reconcile, result.OutcomeCode);
        Assert.Empty(result.Events);
    }

    private static string ClaimParticipantPublication(SessionRuntime session, string key = "1")
    {
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            $"msg.p.{key}", $"turn.{key}", $"slot.{key}", $"trig.participant.{key}", $"idem.p.{key}", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var completed = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId, turnId: $"turn.{key}", responseSlotId: $"slot.{key}"),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        Assert.True(completed.PublicationPathClaimed);
        return invocationId;
    }
}
