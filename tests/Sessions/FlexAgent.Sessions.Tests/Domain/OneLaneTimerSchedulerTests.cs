using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class OneLaneTimerSchedulerTests
{
    [Fact]
    public void Create_active_with_enabled_lane_arms_one_pending_default()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();

        var pending = Assert.Single(session.TimerSchedules);
        Assert.Equal(TimerLaneStates.Pending, pending.LaneState);
        Assert.Equal(1, pending.ScheduleRevision);
        Assert.Equal("PT5M", pending.RelativeDelay);
        Assert.Equal(300, pending.RemainingActiveSeconds);
        Assert.Equal(TimerRequestedByCategories.DefaultCadence, pending.RequestedByCategory);
        Assert.Equal(SessionRuntimeTestFixtures.T0.AddMinutes(5), pending.DueAt);
        Assert.Equal(1, session.PendingTimerCount);
    }

    [Fact]
    public void Create_active_with_disabled_lane_does_not_arm()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolveDisabledTimerPolicy());

        Assert.Empty(session.TimerSchedules);
        Assert.Equal(0, session.PendingTimerCount);
    }

    [Fact]
    public void Accepted_no_action_replacement_supersedes_the_pending_default()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var commitAt = SessionRuntimeTestFixtures.T0.AddSeconds(2);

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(
                invocationId,
                nextTimer: new NextTimerRecommendation("PT2M", "1")),
            commitAt);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(TimerValidationOutcomes.Accepted, result.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, result.ValidationEffect.EffectOutcome);
        Assert.Equal(ResponseSlotStates.IntentionalNoAction, session.Turns[0].ResponseSlot.State);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal(TimerLaneStates.Superseded, session.TimerSchedules[0].LaneState);
        var pending = session.CurrentTimerLane!;
        Assert.Equal(2, pending.ScheduleRevision);
        Assert.Equal(TimerLaneStates.Pending, pending.LaneState);
        Assert.Equal("PT2M", pending.RelativeDelay);
        Assert.Equal(120, pending.RemainingActiveSeconds);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, pending.RequestedByCategory);
        Assert.Equal(result.Decision!.DecisionId, pending.DrivingDecisionId);
        Assert.Equal(commitAt.AddMinutes(2), pending.DueAt);
    }

    [Fact]
    public void Accepted_emit_message_replacement_coexists_with_publication_claim()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(
                invocationId,
                nextTimer: new NextTimerRecommendation("PT3M", "1")),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(TimerValidationOutcomes.Accepted, result.ValidationEffect!.TimerValidationOutcome);
        Assert.True(result.PublicationPathClaimed);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal("PT3M", session.CurrentTimerLane!.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, session.CurrentTimerLane.RequestedByCategory);
    }

    [Fact]
    public void Omitted_or_rejected_timer_keeps_the_pending_default()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var omitted = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0.AddMinutes(6));
        var openingId = opening.Invocation!.AgentInvocationId;
        var rejected = session.CompleteInvocation(
            openingId,
            SessionRuntimeTestFixtures.EmitMessage(
                openingId,
                communicationPurpose: "agent_opening",
                turnId: null,
                responseSlotId: null,
                nextTimer: new NextTimerRecommendation("PT48H", "1")),
            SessionRuntimeTestFixtures.T0.AddMinutes(6).AddSeconds(2));

        Assert.Equal(TimerValidationOutcomes.NotPresent, omitted.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(TimerValidationOutcomes.Rejected, rejected.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(DecisionValidationOutcomes.Accepted, rejected.ValidationEffect.ValidationOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal(1, session.CurrentTimerLane!.ScheduleRevision);
        Assert.Equal("PT5M", session.CurrentTimerLane.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.DefaultCadence, session.CurrentTimerLane.RequestedByCategory);
    }

    [Theory]
    [InlineData("PT0S")]
    [InlineData("-PT5S")]
    [InlineData("PT30S")]
    [InlineData("PT31M")]
    public void Out_of_bounds_or_malformed_delay_is_rejected_without_schedule_effect(string delay)
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(
                invocationId,
                nextTimer: new NextTimerRecommendation(delay, "1")),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.Equal(TimerValidationOutcomes.Rejected, result.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(DecisionValidationOutcomes.Accepted, result.ValidationEffect.ValidationOutcome);
        Assert.Equal(1, session.CurrentTimerLane!.ScheduleRevision);
        Assert.Equal("PT5M", session.CurrentTimerLane.RelativeDelay);
    }

    [Fact]
    public void Stale_expected_revision_is_rejected_and_does_not_erase_the_pending_event()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var first = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        session.CompleteInvocation(
            first.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.NoAction(
                first.Invocation.AgentInvocationId,
                nextTimer: new NextTimerRecommendation("PT2M", "1")),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0.AddMinutes(6));
        var openingId = opening.Invocation!.AgentInvocationId;

        var stale = session.CompleteInvocation(
            openingId,
            SessionRuntimeTestFixtures.EmitMessage(
                openingId,
                communicationPurpose: "agent_opening",
                turnId: null,
                responseSlotId: null,
                nextTimer: new NextTimerRecommendation("PT4M", "1")),
            SessionRuntimeTestFixtures.T0.AddMinutes(6).AddSeconds(2));

        Assert.Equal(TimerValidationOutcomes.Rejected, stale.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(DecisionValidationOutcomes.Accepted, stale.ValidationEffect.ValidationOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal(2, session.CurrentTimerLane!.ScheduleRevision);
        Assert.Equal("PT2M", session.CurrentTimerLane.RelativeDelay);
    }

    [Fact]
    public void Pause_suspends_remaining_delay_and_resume_recomputes_due_at()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.Pause(SessionRuntimeTestFixtures.T0.AddMinutes(1));
        var early = session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(5));
        session.Resume(SessionRuntimeTestFixtures.T0.AddMinutes(10));

        Assert.False(early.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.LifecycleIneligible, early.OutcomeCode);
        Assert.Equal(TimerLaneStates.Pending, session.CurrentTimerLane!.LaneState);
        Assert.Equal(240, session.CurrentTimerLane.RemainingActiveSeconds);
        Assert.Equal(SessionRuntimeTestFixtures.T0.AddMinutes(14), session.CurrentTimerLane.DueAt);
        Assert.Equal(SessionLifecycleState.Active, session.LifecycleState);
    }

    [Fact]
    public void Fire_due_admits_one_timer_invocation_and_retries_reconcile()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var dueAt = SessionRuntimeTestFixtures.T0.AddMinutes(5);

        var first = session.FireDueTimer(1, dueAt);
        var retry = session.FireDueTimer(1, dueAt.AddSeconds(1));

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.Equal(TimerFireOutcomeCodes.Succeeded, first.OutcomeCode);
        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(TimerFireOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Equal(first.Admission!.Invocation!.AgentInvocationId, retry.Admission!.Invocation!.AgentInvocationId);
        Assert.Single(session.Invocations);
        Assert.Equal(RuntimeTriggerIdentifiers.TimerLaneDefaultType, session.Invocations[0].Trigger.TriggerType);
        Assert.Equal(TimerLaneStates.Fired, session.CurrentTimerLane!.LaneState);
        Assert.Equal(session.Invocations[0].AgentInvocationId, session.CurrentTimerLane.FiredInvocationId);
        Assert.Equal(0, session.PendingTimerCount);
    }

    [Fact]
    public void Lost_fire_retry_reconciles_the_addressed_revision_without_touching_its_successor()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var dueAt = SessionRuntimeTestFixtures.T0.AddMinutes(5);
        var first = session.FireDueTimer(1, dueAt);
        var invocationId = first.Admission!.Invocation!.AgentInvocationId;
        var completeAt = dueAt.AddSeconds(2);
        session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            completeAt);

        var lostRetry = session.FireDueTimer(1, completeAt.AddSeconds(1));
        var successorFire = session.FireDueTimer(2, completeAt.AddMinutes(4));

        Assert.True(lostRetry.Succeeded, lostRetry.OutcomeCode);
        Assert.Equal(TimerFireOutcomeCodes.Reconciled, lostRetry.OutcomeCode);
        Assert.Equal(1, lostRetry.Revision!.ScheduleRevision);
        Assert.Equal(invocationId, lostRetry.Admission!.Invocation!.AgentInvocationId);
        Assert.Equal(TimerLaneStates.Fired, session.TimerSchedules[0].LaneState);
        Assert.Equal(TimerLaneStates.Pending, session.TimerSchedules[1].LaneState);
        Assert.Equal(2, session.CurrentTimerLane!.ScheduleRevision);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal(1, session.Invocations.Count(item =>
            item.Trigger.TriggerType == RuntimeTriggerIdentifiers.TimerLaneDefaultType));
        Assert.False(successorFire.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.NotDue, successorFire.OutcomeCode);
        Assert.Equal(2, successorFire.Revision!.ScheduleRevision);
    }

    [Fact]
    public void Recovered_claimed_revision_stays_due_even_when_stored_remaining_is_positive()
    {
        var binding = SessionRuntimeTestFixtures.CreateBinding();
        var claimed = TimerScheduleRevision.Rehydrate(
            "tsrev.claimed",
            1,
            TimerLaneStates.Claimed,
            "PT5M",
            remainingActiveSeconds: 300,
            dueAt: SessionRuntimeTestFixtures.T0.AddMinutes(5),
            remainingSince: SessionRuntimeTestFixtures.T0,
            TimerRequestedByCategories.DefaultCadence,
            drivingDecisionId: null,
            firedInvocationId: null,
            SessionRuntimeTestFixtures.T0);
        var session = SessionRuntime.Rehydrate(
            binding,
            SessionLifecycleState.Active,
            sessionVersion: 0,
            sessionSequence: 0,
            cutoffSequence: null,
            SessionRuntimeTestFixtures.T0,
            timerSchedules: [claimed]);

        var result = session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(5));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(TimerFireOutcomeCodes.Succeeded, result.OutcomeCode);
        Assert.Equal(TimerLaneStates.Fired, session.CurrentTimerLane!.LaneState);
        Assert.Single(session.Invocations);
    }

    [Fact]
    public void Fire_due_before_remaining_active_delay_elapses_does_not_mutate()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();

        var result = session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(4));

        Assert.False(result.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.NotDue, result.OutcomeCode);
        Assert.Equal(TimerLaneStates.Pending, session.CurrentTimerLane!.LaneState);
        Assert.Empty(session.Invocations);
    }

    [Fact]
    public void Timer_no_action_without_replacement_arms_the_default_successor()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var fired = session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(5));
        var invocationId = fired.Admission!.Invocation!.AgentInvocationId;
        var completeAt = SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(2);

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            completeAt);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(TimerValidationOutcomes.NotPresent, result.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        var successor = session.CurrentTimerLane!;
        Assert.Equal(2, successor.ScheduleRevision);
        Assert.Equal(TimerLaneStates.Pending, successor.LaneState);
        Assert.Equal("PT5M", successor.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.SuccessorAfterFire, successor.RequestedByCategory);
        Assert.Equal(completeAt.AddMinutes(5), successor.DueAt);
        Assert.Equal(TimerLaneStates.Fired, session.TimerSchedules[0].LaneState);
    }

    [Fact]
    public void Accepted_timer_decision_installs_the_sole_successor_instead_of_default()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var fired = session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(5));
        var invocationId = fired.Admission!.Invocation!.AgentInvocationId;
        var completeAt = SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(2);

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(
                invocationId,
                nextTimer: new NextTimerRecommendation("PT2M", "1")),
            completeAt);

        Assert.Equal(TimerValidationOutcomes.Accepted, result.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        var successor = session.CurrentTimerLane!;
        Assert.Equal(2, successor.ScheduleRevision);
        Assert.Equal("PT2M", successor.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, successor.RequestedByCategory);
        Assert.Equal(completeAt.AddMinutes(2), successor.DueAt);
        Assert.DoesNotContain(
            session.TimerSchedules,
            revision => revision.RequestedByCategory == TimerRequestedByCategories.SuccessorAfterFire);
    }

    [Fact]
    public void Empty_respond_on_a_timer_invocation_still_installs_an_accepted_successor()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var fired = session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(5));
        var invocationId = fired.Admission!.Invocation!.AgentInvocationId;
        var completeAt = SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(2);
        var envelope = SessionRuntimeTestFixtures.Envelope(
            invocationId,
            outputs: [],
            requestedActions:
            [
                new RequestedActionRecommendation(
                    AgentRequestedActionKinds.NextTimerRequest,
                    "act.timer.primary",
                    "PT2M",
                    "1"),
            ]);

        var result = session.CompleteInvocation(invocationId, envelope, completeAt);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(DecisionValidationOutcomes.Rejected, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, result.ValidationEffect.EffectOutcome);
        Assert.Equal(TimerValidationOutcomes.Accepted, result.ValidationEffect.TimerValidationOutcome);
        Assert.Equal(
            DecisionEffectOutcomes.Applied,
            Assert.Single(result.ValidationEffect.RequestedActionValidations).EffectOutcome);
        Assert.Empty(session.Turns);
        Assert.False(result.PublicationPathClaimed);
        Assert.Equal(1, session.PendingTimerCount);
        var successor = session.CurrentTimerLane!;
        Assert.Equal(2, successor.ScheduleRevision);
        Assert.Equal("PT2M", successor.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, successor.RequestedByCategory);
        Assert.Equal(completeAt.AddMinutes(2), successor.DueAt);
        Assert.DoesNotContain(
            session.TimerSchedules,
            revision => revision.RequestedByCategory == TimerRequestedByCategories.SuccessorAfterFire);
    }

    [Fact]
    public void Concurrent_non_timer_replacement_during_long_running_timer_wins_expected_revision()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            ResolveTimerPolicy(maxConcurrentReplacements: 2));
        session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(5));
        var participant = session.AcceptParticipantMessage(
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(1));
        session.CompleteInvocation(
            participant.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.NoAction(
                participant.Invocation.AgentInvocationId,
                nextTimer: new NextTimerRecommendation("PT4M", "1")),
            SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(2));
        var timerId = session.Invocations[0].AgentInvocationId;

        var lateTimer = session.CompleteInvocation(
            timerId,
            SessionRuntimeTestFixtures.NoAction(
                timerId,
                nextTimer: new NextTimerRecommendation("PT2M", "1")),
            SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(3));

        Assert.Equal(TimerValidationOutcomes.Rejected, lateTimer.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal("PT4M", session.CurrentTimerLane!.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, session.CurrentTimerLane.RequestedByCategory);
    }

    [Fact]
    public void Concurrent_replacement_budget_rejects_overlapping_eligible_invocations()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            ResolveTimerPolicy(maxConcurrentReplacements: 1));
        session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(5));
        var participant = session.AcceptParticipantMessage(
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(1));

        var overlapping = session.CompleteInvocation(
            participant.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.NoAction(
                participant.Invocation.AgentInvocationId,
                nextTimer: new NextTimerRecommendation("PT4M", "1")),
            SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(2));
        var timerId = session.Invocations[0].AgentInvocationId;
        var timerSuccessor = session.CompleteInvocation(
            timerId,
            SessionRuntimeTestFixtures.NoAction(
                timerId,
                nextTimer: new NextTimerRecommendation("PT2M", "1")),
            SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(3));

        Assert.Equal(TimerValidationOutcomes.Rejected, overlapping.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(DecisionValidationOutcomes.Accepted, overlapping.ValidationEffect.ValidationOutcome);
        Assert.Equal(TimerValidationOutcomes.Accepted, timerSuccessor.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal("PT2M", session.CurrentTimerLane!.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, session.CurrentTimerLane.RequestedByCategory);
    }

    [Fact]
    public void Concurrent_replacement_ignores_recorded_decisions_that_can_no_longer_recommend_a_timer()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            ResolveTimerPolicy(maxConcurrentReplacements: 1));
        var first = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var recorded = session.RecordDecision(
            first.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.NoAction(first.Invocation.AgentInvocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        var replacement = session.CompleteInvocation(
            opening.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.EmitMessage(
                opening.Invocation.AgentInvocationId,
                communicationPurpose: "agent_opening",
                turnId: null,
                responseSlotId: null,
                nextTimer: new NextTimerRecommendation("PT2M", "1")),
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.Equal(AgentInvocationStatuses.DecisionRecorded, recorded.Invocation!.Status);
        Assert.False(recorded.Invocation.IsTerminal);
        Assert.Null(recorded.Invocation.Decision!.NextTimer);
        Assert.Equal(TimerValidationOutcomes.Accepted, replacement.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal(2, session.CurrentTimerLane!.ScheduleRevision);
        Assert.Equal("PT2M", session.CurrentTimerLane.RelativeDelay);
    }

    [Fact]
    public void Duplicate_suppression_matches_equivalent_delay_not_every_replacement()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            ResolveTimerPolicy(duplicateSuppressionWindowSeconds: 30));
        var first = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        session.CompleteInvocation(
            first.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.NoAction(
                first.Invocation.AgentInvocationId,
                nextTimer: new NextTimerRecommendation("PT2M", "1")),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var duplicate = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var duplicateResult = session.CompleteInvocation(
            duplicate.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.EmitMessage(
                duplicate.Invocation.AgentInvocationId,
                communicationPurpose: "agent_opening",
                turnId: null,
                responseSlotId: null,
                nextTimer: new NextTimerRecommendation("PT2M", "2")),
            SessionRuntimeTestFixtures.T0.AddSeconds(4));
        var distinct = session.AcceptParticipantMessage(
            "msg.p.2",
            "turn.2",
            "slot.2",
            "trig.participant.2",
            "idem.p.2",
            SessionRuntimeTestFixtures.T0.AddSeconds(5));
        var distinctResult = session.CompleteInvocation(
            distinct.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.NoAction(
                distinct.Invocation.AgentInvocationId,
                nextTimer: new NextTimerRecommendation("PT3M", "2")),
            SessionRuntimeTestFixtures.T0.AddSeconds(6));

        Assert.Equal(TimerValidationOutcomes.Rejected, duplicateResult.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(TimerValidationOutcomes.Accepted, distinctResult.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal(3, session.CurrentTimerLane!.ScheduleRevision);
        Assert.Equal("PT3M", session.CurrentTimerLane.RelativeDelay);
    }

    [Fact]
    public void Timer_terminalization_while_paused_still_arms_a_frozen_default_successor()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var fired = session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(5));
        var invocationId = fired.Admission!.Invocation!.AgentInvocationId;
        session.Pause(SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(1));

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(2));
        session.Resume(SessionRuntimeTestFixtures.T0.AddMinutes(10));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(DecisionValidationOutcomes.Rejected, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        var successor = session.CurrentTimerLane!;
        Assert.Equal(TimerLaneStates.Pending, successor.LaneState);
        Assert.Equal("PT5M", successor.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.SuccessorAfterFire, successor.RequestedByCategory);
        Assert.Equal(SessionRuntimeTestFixtures.T0.AddMinutes(15), successor.DueAt);
        Assert.False(session.FireDueTimer(successor.ScheduleRevision, SessionRuntimeTestFixtures.T0.AddMinutes(14)).Succeeded);
        var due = session.FireDueTimer(successor.ScheduleRevision, SessionRuntimeTestFixtures.T0.AddMinutes(15));
        Assert.True(due.Succeeded, due.OutcomeCode);
        Assert.Equal(TimerFireOutcomeCodes.Succeeded, due.OutcomeCode);
    }

    [Fact]
    public void Cutoff_cancels_the_pending_event_and_prevents_fire_or_rearm()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
        var fire = session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(5));
        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0.AddMinutes(6));

        Assert.Equal(TimerLaneStates.Cancelled, session.TimerSchedules[0].LaneState);
        Assert.Equal(0, session.PendingTimerCount);
        Assert.False(fire.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.LifecycleIneligible, fire.OutcomeCode);
        Assert.False(opening.Succeeded);
        Assert.Empty(session.Invocations);
    }

    [Fact]
    public void Replacement_budget_rejects_further_accepted_replacements()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            ResolveTimerPolicy(maxAcceptedReplacements: 1));
        var first = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        session.CompleteInvocation(
            first.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.NoAction(
                first.Invocation.AgentInvocationId,
                nextTimer: new NextTimerRecommendation("PT2M", "1")),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        var second = session.CompleteInvocation(
            opening.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.EmitMessage(
                opening.Invocation.AgentInvocationId,
                communicationPurpose: "agent_opening",
                turnId: null,
                responseSlotId: null,
                nextTimer: new NextTimerRecommendation("PT3M", "2")),
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.Equal(TimerValidationOutcomes.Rejected, second.ValidationEffect!.TimerValidationOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal(2, session.CurrentTimerLane!.ScheduleRevision);
        Assert.Equal("PT2M", session.CurrentTimerLane.RelativeDelay);
    }

    [Fact]
    public void Equivalent_decision_retry_does_not_create_a_second_schedule_revision()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var recommendation = SessionRuntimeTestFixtures.NoAction(
            invocationId,
            nextTimer: new NextTimerRecommendation("PT2M", "1"));
        session.CompleteInvocation(invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        var retry = session.CompleteInvocation(
            invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal(2, session.TimerSchedules.Count);
        Assert.Equal(2, session.CurrentTimerLane!.ScheduleRevision);
    }

    [Fact]
    public void Timer_terminalization_at_invocation_budget_does_not_arm_a_successor()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            ResolveTimerPolicy(maxTimerTriggeredInvocations: 1));
        var fired = session.FireDueTimer(1, SessionRuntimeTestFixtures.T0.AddMinutes(5));
        var invocationId = fired.Admission!.Invocation!.AgentInvocationId;
        var completeAt = SessionRuntimeTestFixtures.T0.AddMinutes(5).AddSeconds(2);

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            completeAt);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(0, session.PendingTimerCount);
        Assert.Equal(TimerLaneStates.Fired, session.CurrentTimerLane!.LaneState);
        Assert.Single(session.TimerSchedules);
    }

    [Fact]
    public void Recovered_due_successor_past_invocation_budget_expires_instead_of_staying_pending()
    {
        var binding = SessionRuntimeTestFixtures.CreateBinding(
            ResolveTimerPolicy(maxTimerTriggeredInvocations: 1));
        var trigger = SessionRuntimeTestFixtures.TimerTrigger();
        var invocation = AgentInvocation.Rehydrate(
            "ainv.timer.1",
            binding.Ownership,
            trigger,
            "idem.timer.1",
            binding.Policy.PolicyDigest,
            sessionSequence: 1,
            AgentInvocationStatuses.Decided);
        var pending = TimerScheduleRevision.Rehydrate(
            "tsrev.poison",
            2,
            TimerLaneStates.Pending,
            "PT5M",
            remainingActiveSeconds: 0,
            dueAt: SessionRuntimeTestFixtures.T0.AddMinutes(10),
            remainingSince: SessionRuntimeTestFixtures.T0.AddMinutes(10),
            TimerRequestedByCategories.SuccessorAfterFire,
            drivingDecisionId: null,
            firedInvocationId: null,
            SessionRuntimeTestFixtures.T0.AddMinutes(10));
        var session = SessionRuntime.Rehydrate(
            binding,
            SessionLifecycleState.Active,
            sessionVersion: 1,
            sessionSequence: 1,
            cutoffSequence: null,
            SessionRuntimeTestFixtures.T0.AddMinutes(10),
            invocations: [invocation],
            timerSchedules: [pending]);

        var result = session.FireDueTimer(2, SessionRuntimeTestFixtures.T0.AddMinutes(10));

        Assert.False(result.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.BudgetExhausted, result.OutcomeCode);
        Assert.Equal(TimerLaneStates.Expired, session.TimerSchedules[0].LaneState);
        Assert.Equal(0, session.PendingTimerCount);
        Assert.Equal("ainv.timer.1", Assert.Single(session.Invocations).AgentInvocationId);
        Assert.Contains(
            session.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.TimerEventV1
                && record.PayloadRef.ProtectedRef == "tsrev.poison.expired");
    }

    private static FrozenTextSessionRuntimePolicy ResolveTimerPolicy(
        int maxConcurrentReplacements = 2,
        int cooldownSeconds = 0,
        int duplicateSuppressionWindowSeconds = 0,
        int maxAcceptedReplacements = 5,
        int maxTimerTriggeredInvocations = 8)
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues();
        return RuntimePolicyTestFixtures.ResolvePolicy(
            baseline with
            {
                TimerLane = baseline.TimerLane! with
                {
                    Budgets = new TimerLaneBudgets(
                        MaxAcceptedReplacementsPerSession: maxAcceptedReplacements,
                        MaxTimerTriggeredInvocationsPerSession: maxTimerTriggeredInvocations,
                        CooldownSeconds: cooldownSeconds,
                        MaxConcurrentReplacements: maxConcurrentReplacements,
                        DuplicateSuppressionWindowSeconds: duplicateSuppressionWindowSeconds),
                },
                InvocationBounds = new InvocationBounds(3, 10, 0, CooldownSeconds: 0, 30),
            });
    }
}
