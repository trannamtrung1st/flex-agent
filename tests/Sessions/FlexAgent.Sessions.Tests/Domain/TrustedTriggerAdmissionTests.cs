using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class TrustedTriggerAdmissionTests
{
    [Fact]
    public void Accepting_participant_message_on_active_session_admits_stable_invocation()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();

        var result = session.AcceptParticipantMessage(
            participantMessageId: "msg.p.1",
            turnId: "turn.1",
            responseSlotId: "slot.1",
            triggerId: "trig.participant.1",
            idempotencyKey: "idem.p.1",
            authoritativeUtc: SessionRuntimeTestFixtures.T0);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Succeeded, result.OutcomeCode);
        Assert.NotNull(result.Invocation);
        Assert.False(string.IsNullOrWhiteSpace(result.Invocation!.AgentInvocationId));
        Assert.Equal(AgentInvocationStatuses.Admitted, result.Invocation.Status);
        Assert.Equal(RuntimeTriggerIdentifiers.ParticipantMessageType, result.Invocation.Trigger.TriggerType);
        Assert.Equal("turn.1", result.Invocation.Trigger.TurnId);
        Assert.Equal("slot.1", result.Invocation.Trigger.ResponseSlotId);
        Assert.Equal(session.Binding.Policy.PolicyDigest, result.Invocation.PolicyDigest);
        Assert.Equal(1, result.SessionSequence);
        Assert.Equal(session.Ownership, result.Invocation.Ownership);
        Assert.Single(session.Turns);
        Assert.Equal(TurnKinds.Participant, session.Turns[0].Kind);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public void Duplicate_participant_trigger_reconciles_to_the_same_invocation()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var first = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);

        var second = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Reconciled, second.OutcomeCode);
        Assert.Equal(first.Invocation!.AgentInvocationId, second.Invocation!.AgentInvocationId);
        Assert.Equal(first.SessionSequence, second.SessionSequence);
        Assert.Single(session.Invocations);
        Assert.Single(session.Turns);
    }

    [Fact]
    public void Participant_admission_with_the_same_trigger_and_idempotency_but_different_bound_identities_conflicts()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var first = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);

        var conflict = session.AcceptParticipantMessage(
            "msg.p.2", "turn.2", "slot.2", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.False(conflict.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.IdempotencyConflict, conflict.OutcomeCode);
        Assert.Single(session.Invocations);
        Assert.Single(session.Turns);
        Assert.Equal("turn.1", session.Turns[0].TurnId);
        Assert.Equal("msg.p.1", session.VisibleTranscript[0].MessageId);
        Assert.Equal("turn.1", first.Invocation!.Trigger.TurnId);
    }

    [Fact]
    public void Mismatched_idempotency_reuse_conflicts_without_creating_another_invocation()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);

        var conflict = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.other", SessionRuntimeTestFixtures.T0);

        Assert.False(conflict.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.IdempotencyConflict, conflict.OutcomeCode);
        Assert.Single(session.Invocations);
    }

    [Theory]
    [InlineData("interaction_signal", "interaction_signal.voice_end")]
    [InlineData("interaction_signal", "interaction_signal.silence_detected")]
    [InlineData("tool_result", "tool_result.participant_tool")]
    [InlineData("workflow_event", "workflow_event.custom_stage_transition")]
    [InlineData("timer_event", "timer_event.parallel_lane")]
    [InlineData("unknown_family", "unknown_family.x")]
    public void Unknown_or_prohibited_triggers_fail_closed(string family, string type)
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var trigger = new TrustedTrigger(family, type, "trig.bad", "purpose.bad", null, null);

        var result = session.AdmitTrustedTrigger(trigger, "idem.bad", SessionRuntimeTestFixtures.T0);

        Assert.False(result.Succeeded);
        Assert.True(
            result.OutcomeCode is TriggerAdmissionOutcomeCodes.UnknownTrigger
                or TriggerAdmissionOutcomeCodes.ProhibitedTrigger,
            result.OutcomeCode);
        Assert.Empty(session.Invocations);
    }

    [Fact]
    public void Timer_trigger_is_rejected_when_frozen_lane_is_disabled()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolveDisabledTimerPolicy());

        var result = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.TimerTrigger(),
            "idem.timer",
            SessionRuntimeTestFixtures.T0);

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.ProhibitedTrigger, result.OutcomeCode);
        Assert.Empty(session.Invocations);
    }

    [Fact]
    public void Timer_opening_and_closing_triggers_are_admitted_when_policy_permits()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();

        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(), "idem.open", SessionRuntimeTestFixtures.T0);
        var timer = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.TimerTrigger(), "idem.timer", SessionRuntimeTestFixtures.T0.AddMinutes(6));
        var closing = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.ClosingTrigger(), "idem.close", SessionRuntimeTestFixtures.T0.AddMinutes(12));

        Assert.True(opening.Succeeded, opening.OutcomeCode);
        Assert.True(timer.Succeeded, timer.OutcomeCode);
        Assert.True(closing.Succeeded, closing.OutcomeCode);
        Assert.Equal(3, session.Invocations.Count);
        Assert.Empty(session.Turns);
        Assert.Null(opening.Invocation!.Trigger.TurnId);
        Assert.Null(timer.Invocation!.Trigger.TurnId);
    }

    [Fact]
    public void Opening_trigger_is_rejected_when_frozen_policy_disallows_agent_initiated_opening()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            AgentInitiatedOpeningPermitted = false,
        };
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(values));

        var result = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(), "idem.open", SessionRuntimeTestFixtures.T0);

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.ProhibitedTrigger, result.OutcomeCode);
    }

    [Theory]
    [InlineData(SessionLifecycleState.Paused)]
    [InlineData(SessionLifecycleState.Completing)]
    [InlineData(SessionLifecycleState.Completed)]
    [InlineData(SessionLifecycleState.Terminated)]
    [InlineData(SessionLifecycleState.Aborted)]
    public void Non_active_lifecycle_rejects_new_admission(SessionLifecycleState state)
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        MoveTo(session, state);

        var result = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(), "idem.open", SessionRuntimeTestFixtures.T0.AddMinutes(1));

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.LifecycleIneligible, result.OutcomeCode);
        Assert.Empty(session.Invocations);
    }

    [Fact]
    public void Chained_invocation_budget_rejects_additional_distinct_triggers()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            InvocationBounds = new InvocationBounds(3, 1, 0, 5, 30),
        };
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(values));

        var first = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.1"),
            "idem.1",
            SessionRuntimeTestFixtures.T0);
        var second = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.2"),
            "idem.2",
            SessionRuntimeTestFixtures.T0.AddMinutes(1));

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.False(second.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.BudgetExhausted, second.OutcomeCode);
        Assert.Single(session.Invocations);
    }

    [Fact]
    public void Failed_participant_admission_does_not_leave_an_orphaned_turn()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            InvocationBounds = new InvocationBounds(3, 1, 0, 5, 30),
        };
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(values));
        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0);

        var result = session.AcceptParticipantMessage(
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0.AddMinutes(1));

        Assert.True(opening.Succeeded, opening.OutcomeCode);
        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.BudgetExhausted, result.OutcomeCode);
        Assert.Empty(session.Turns);
        Assert.Empty(session.VisibleTranscript);
        Assert.Single(session.Invocations);
    }

    [Fact]
    public void Cooldown_rejects_a_new_distinct_trigger_in_the_same_family()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var first = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.1"),
            "idem.1",
            SessionRuntimeTestFixtures.T0);
        var second = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.2"),
            "idem.2",
            SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.False(second.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.CooldownActive, second.OutcomeCode);
        Assert.Single(session.Invocations);
    }

    [Fact]
    public void Non_utc_authoritative_clock_cannot_choose_admission_order()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var offsetTime = new DateTimeOffset(2026, 8, 13, 7, 0, 0, TimeSpan.FromHours(7));

        var result = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(), "idem.open", offsetTime);

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.NonUtcClock, result.OutcomeCode);
        Assert.Empty(session.Invocations);
    }

    [Fact]
    public void Participant_trigger_without_an_existing_turn_is_rejected()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();

        var result = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.ParticipantTrigger(),
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.MissingTurn, result.OutcomeCode);
        Assert.Empty(session.Invocations);
    }

    private static void MoveTo(SessionRuntime session, SessionLifecycleState state)
    {
        switch (state)
        {
            case SessionLifecycleState.Paused:
                session.Pause(SessionRuntimeTestFixtures.T0.AddSeconds(1));
                break;
            case SessionLifecycleState.Completing:
                session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
                break;
            case SessionLifecycleState.Completed:
                session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
                session.Complete(SessionRuntimeTestFixtures.T0.AddSeconds(2));
                break;
            case SessionLifecycleState.Terminated:
                session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
                session.Terminate(SessionRuntimeTestFixtures.T0.AddSeconds(2));
                break;
            case SessionLifecycleState.Aborted:
                session.Abort(SessionRuntimeTestFixtures.T0.AddSeconds(1));
                break;
        }
    }
}
