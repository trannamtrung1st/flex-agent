using System.Reflection;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class ChangeSessionLifecycleCommandTests
{
    [Fact]
    public void Command_requires_trusted_actor_and_complete_session_ownership_without_client_clocks()
    {
        var ctor = typeof(ChangeSessionLifecycleCommand).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var parameters = ctor.GetParameters();

        Assert.Contains(parameters, parameter => parameter.Name == "Actor" && parameter.ParameterType == typeof(TrustedRuntimeActor));
        Assert.Contains(parameters, parameter => parameter.Name == "Ownership" && parameter.ParameterType == typeof(SessionOwnership));
        Assert.Contains(parameters, parameter => parameter.Name == "ExpectedSessionVersion" && parameter.ParameterType == typeof(long));
        Assert.Contains(parameters, parameter => parameter.Name == "Transition" && parameter.ParameterType == typeof(string));
        Assert.Contains(parameters, parameter => parameter.Name == "ReasonCode" && parameter.ParameterType == typeof(string));
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTime));
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTimeOffset));
        Assert.DoesNotContain(parameters, parameter => parameter.Name is "utcNow" or "authoritativeUtc" or "timestamp" or "clock");
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("FlexAgent.Contracts", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Handler_requires_server_loaded_session_and_authoritative_utc_outside_the_command()
    {
        var method = typeof(IChangeSessionLifecycleHandler).GetMethod(nameof(IChangeSessionLifecycleHandler.Handle));
        Assert.NotNull(method);
        var parameters = method!.GetParameters();

        Assert.Contains(parameters, parameter => parameter.ParameterType == typeof(ChangeSessionLifecycleCommand));
        Assert.Contains(parameters, parameter => parameter.Name == "session" && parameter.ParameterType == typeof(SessionRuntime));
        Assert.Contains(parameters, parameter => parameter.Name == "authoritativeUtc" && parameter.ParameterType == typeof(DateTimeOffset));
    }

    [Fact]
    public void Handler_rejects_command_ownership_that_does_not_match_the_loaded_session()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var forgedOwnership = session.Ownership with
        {
            OrganizationId = Guid.Parse("00000000-0000-0000-0000-000000000099"),
        };

        var result = new ChangeSessionLifecycleHandler().Handle(
            CreateCommand(session, SessionLifecycleTransitions.BeginCompleting) with { Ownership = forgedOwnership },
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.False(result.Succeeded);
        Assert.Equal(SessionLifecycleOutcomeCodes.OwnershipMismatch, result.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Active, session.LifecycleState);
        Assert.Equal(TimerLaneStates.Pending, session.CurrentTimerLane!.LaneState);
    }

    [Fact]
    public void Handler_rejects_missing_actor()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var command = CreateCommand(session, SessionLifecycleTransitions.BeginCompleting) with
        {
            Actor = new TrustedRuntimeActor(Guid.Empty, "synthetic.test_actor"),
        };

        var result = new ChangeSessionLifecycleHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.False(result.Succeeded);
        Assert.Equal(SessionLifecycleOutcomeCodes.Denied, result.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Active, session.LifecycleState);
    }

    [Fact]
    public void Handler_rejects_stale_expected_version_without_mutating_lifecycle()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var sequenceBefore = session.SessionSequence;
        var command = CreateCommand(session, SessionLifecycleTransitions.BeginCompleting, session.SessionVersion + 4);

        var result = new ChangeSessionLifecycleHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.False(result.Succeeded);
        Assert.Equal(SessionLifecycleOutcomeCodes.StaleVersion, result.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Active, session.LifecycleState);
        Assert.Equal(sequenceBefore, session.SessionSequence);
        Assert.Equal(TimerLaneStates.Pending, session.CurrentTimerLane!.LaneState);
    }

    [Fact]
    public void Begin_completing_cancels_the_timer_lane_and_seals_a_visible_prefix_incomplete()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.lifecycle.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        var command = CreateCommand(session, SessionLifecycleTransitions.BeginCompleting);

        var result = new ChangeSessionLifecycleHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(SessionLifecycleOutcomeCodes.Succeeded, result.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Completing, session.LifecycleState);
        Assert.Equal(TimerLaneStates.Cancelled, session.TimerSchedules[0].LaneState);
        Assert.Equal(0, session.PendingTimerCount);
        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
        Assert.Equal("Hel", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(TurnStates.Complete, session.Turns[0].State);
        Assert.True(Assert.Single(session.PendingPublicationWork).SealDirty);
    }

    [Fact]
    public void Original_begin_completing_retry_reconciles_without_a_second_seal_or_version_bump()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.lifecycle.retry"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        var handler = new ChangeSessionLifecycleHandler();
        var command = CreateCommand(session, SessionLifecycleTransitions.BeginCompleting);

        var first = handler.Handle(command, session, SessionRuntimeTestFixtures.T0.AddSeconds(4));
        var versionAfter = session.SessionVersion;
        var second = handler.Handle(command, session, SessionRuntimeTestFixtures.T0.AddSeconds(5));

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.Equal(SessionLifecycleOutcomeCodes.Succeeded, first.OutcomeCode);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(SessionLifecycleOutcomeCodes.Reconciled, second.OutcomeCode);
        Assert.Equal(versionAfter, session.SessionVersion);
        Assert.Equal(SessionLifecycleState.Completing, session.LifecycleState);
        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
        Assert.Single(session.AgentMessages[0].Fragments);
    }

    [Fact]
    public void Resume_on_a_never_paused_active_session_is_ineligible()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();

        var result = new ChangeSessionLifecycleHandler().Handle(
            CreateCommand(session, SessionLifecycleTransitions.Resume),
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.False(result.Succeeded);
        Assert.Equal(SessionLifecycleOutcomeCodes.LifecycleIneligible, result.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Active, session.LifecycleState);
    }

    [Fact]
    public void Stale_resume_after_later_active_work_is_not_reconciled()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var handler = new ChangeSessionLifecycleHandler();
        Assert.True(handler.Handle(
            CreateCommand(session, SessionLifecycleTransitions.Pause),
            session,
            SessionRuntimeTestFixtures.T0.AddMinutes(1)).Succeeded);
        Assert.True(handler.Handle(
            CreateCommand(session, SessionLifecycleTransitions.Resume),
            session,
            SessionRuntimeTestFixtures.T0.AddMinutes(2)).Succeeded);
        var versionAfterResume = session.SessionVersion;
        Assert.True(session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open.after-resume",
            SessionRuntimeTestFixtures.T0.AddMinutes(3)).Succeeded);

        var result = handler.Handle(
            CreateCommand(session, SessionLifecycleTransitions.Resume, versionAfterResume),
            session,
            SessionRuntimeTestFixtures.T0.AddMinutes(4));

        Assert.False(result.Succeeded);
        Assert.Equal(SessionLifecycleOutcomeCodes.StaleVersion, result.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Active, session.LifecycleState);
        Assert.Single(session.Invocations);
    }

    [Fact]
    public void Abort_cancels_the_timer_lane_and_seals_a_visible_prefix_incomplete()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.lifecycle.abort"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);

        var result = new ChangeSessionLifecycleHandler().Handle(
            CreateCommand(session, SessionLifecycleTransitions.Abort),
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(SessionLifecycleOutcomeCodes.Succeeded, result.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Aborted, session.LifecycleState);
        Assert.Equal(TimerLaneStates.Cancelled, session.TimerSchedules[0].LaneState);
        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
        Assert.Equal("Hel", session.AgentMessages[0].AssembleExactText());
        Assert.True(Assert.Single(session.PendingPublicationWork).SealDirty);
    }

    [Fact]
    public void Pause_then_resume_freezes_and_recomputes_the_pending_delay()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var handler = new ChangeSessionLifecycleHandler();

        var paused = handler.Handle(
            CreateCommand(session, SessionLifecycleTransitions.Pause),
            session,
            SessionRuntimeTestFixtures.T0.AddMinutes(1));
        var remaining = session.CurrentTimerLane!.RemainingActiveSeconds;
        var resumed = handler.Handle(
            CreateCommand(session, SessionLifecycleTransitions.Resume),
            session,
            SessionRuntimeTestFixtures.T0.AddMinutes(10));

        Assert.True(paused.Succeeded, paused.OutcomeCode);
        Assert.True(resumed.Succeeded, resumed.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Active, session.LifecycleState);
        Assert.Equal(remaining, session.CurrentTimerLane.RemainingActiveSeconds);
        Assert.Equal(SessionRuntimeTestFixtures.T0.AddMinutes(10).AddSeconds(remaining), session.CurrentTimerLane.DueAt);
        Assert.Equal(9 * 60, session.AccumulatedPausedSeconds);
        Assert.Null(session.OpenPauseStartedAt);
    }

    [Fact]
    public void Complete_with_time_expiry_reason_maps_to_completed_attempt()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var handler = new ChangeSessionLifecycleHandler();
        Assert.True(handler.Handle(
            CreateCommand(session, SessionLifecycleTransitions.BeginCompleting),
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(1)).Succeeded);

        var result = handler.Handle(
            CreateCommand(session, SessionLifecycleTransitions.Complete) with
            {
                ReasonCode = TerminalReasonCategories.TimeExpiry,
            },
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Completed, session.LifecycleState);
        Assert.Equal(TerminalReasonCategories.TimeExpiry, session.TerminalRecord!.ReasonCategory);
        Assert.Equal(AttemptTerminalMappings.Completed, session.TerminalRecord.AttemptMapping);
    }

    [Fact]
    public void Terminate_reason_is_carried_on_the_command_and_lifecycle_audit_seed()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var handler = new ChangeSessionLifecycleHandler();
        Assert.True(handler.Handle(
            CreateCommand(session, SessionLifecycleTransitions.BeginCompleting),
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(1)).Succeeded);

        var command = CreateCommand(session, SessionLifecycleTransitions.Terminate) with
        {
            ReasonCode = "administrator_stop",
        };
        var result = handler.Handle(command, session, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(
            "terminate:terminated:2:administrator_stop",
            SessionRuntimeLifecycleAudit.Seed(
                command.Transition,
                session.LifecycleState,
                session.SessionVersion,
                command.ReasonCode));
    }

    private static ChangeSessionLifecycleCommand CreateCommand(
        SessionRuntime session,
        string transition,
        long? expectedVersion = null) =>
        new(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            expectedVersion ?? session.SessionVersion,
            transition,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "application.test",
            transition switch
            {
                SessionLifecycleTransitions.Pause => "administrator_pause",
                SessionLifecycleTransitions.Terminate => "administrator_stop",
                _ => null,
            });

    [Fact]
    public void Pause_without_reason_is_denied()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var result = new ChangeSessionLifecycleHandler().Handle(
            CreateCommand(session, SessionLifecycleTransitions.Pause) with { ReasonCode = null },
            session,
            SessionRuntimeTestFixtures.T0.AddMinutes(1));

        Assert.False(result.Succeeded);
        Assert.Equal(SessionLifecycleOutcomeCodes.Denied, result.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Active, session.LifecycleState);
    }

    private static string ClaimParticipantPublication(SessionRuntime session)
    {
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var completed = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        Assert.True(completed.PublicationPathClaimed);
        return invocationId;
    }
}
