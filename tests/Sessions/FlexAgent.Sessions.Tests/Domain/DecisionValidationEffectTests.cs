using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class DecisionValidationEffectTests
{
    [Fact]
    public void Accepted_no_action_terminalizes_participant_turn_without_an_agent_message()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(DecisionValidationOutcomes.Accepted, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, result.ValidationEffect.EffectOutcome);
        Assert.Equal(ResponseSlotStates.IntentionalNoAction, session.Turns[0].ResponseSlot.State);
        Assert.Equal(TurnStates.Complete, session.Turns[0].State);
        Assert.False(result.AgentMessagePublished);
        Assert.False(result.PublicationPathClaimed);
        Assert.DoesNotContain(session.VisibleTranscript, item => item.AuthorType == TranscriptAuthorTypes.Agent);
        Assert.Equal(RuntimeDecisionTypes.NoAction, result.Decision!.DecisionType);
    }

    [Fact]
    public void Validate_decision_retry_at_unchanged_session_state_does_not_mutate()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var recommendation = SessionRuntimeTestFixtures.NoAction(invocationId);
        session.RecordDecision(invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var first = session.ValidateDecision(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var version = session.SessionVersion;
        var sequence = session.SessionSequence;
        var committed = session.LastCommittedAt;

        var retry = session.ValidateDecision(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.Equal(DecisionValidationOutcomes.Accepted, first.ValidationOutcome);
        Assert.Equal(DecisionValidationOutcomes.Accepted, retry.ValidationOutcome);
        Assert.Equal(version, session.SessionVersion);
        Assert.Equal(sequence, session.SessionSequence);
        Assert.Equal(committed, session.LastCommittedAt);
        Assert.Single(session.Invocations[0].ValidationHistory);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, session.Invocations[0].ValidationEffect!.EffectOutcome);
    }

    [Fact]
    public void Revalidation_after_lifecycle_change_appends_a_revision_and_preserves_accepted_history()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var recommendation = SessionRuntimeTestFixtures.NoAction(invocationId);
        session.RecordDecision(invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        session.ValidateDecision(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(3));
        session.Pause(SessionRuntimeTestFixtures.T0.AddSeconds(4));

        var resumed = session.CompleteInvocation(
            invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(5));

        Assert.True(resumed.Succeeded, resumed.OutcomeCode);
        Assert.Equal(2, session.Invocations[0].ValidationHistory.Count);
        Assert.Equal(DecisionValidationOutcomes.Accepted, session.Invocations[0].ValidationHistory[0].ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, session.Invocations[0].ValidationHistory[0].EffectOutcome);
        Assert.Equal(DecisionValidationOutcomes.Rejected, session.Invocations[0].ValidationHistory[1].ValidationOutcome);
        Assert.Equal(RejectionReasonCategories.StateIneligible, session.Invocations[0].ValidationHistory[1].RejectionReasonCategory);
        Assert.Equal(DecisionValidationOutcomes.Rejected, resumed.ValidationEffect!.ValidationOutcome);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
        Assert.Equal(AgentInvocationStatuses.Decided, session.Invocations[0].Status);
    }

    [Fact]
    public void Reconcile_after_no_action_does_not_restart_the_invocation()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = session.Invocations[0].AgentInvocationId;
        session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        var retry = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0.AddSeconds(5));

        Assert.Equal(TriggerAdmissionOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Equal(invocationId, retry.Invocation!.AgentInvocationId);
        Assert.Equal(AgentInvocationStatuses.Decided, retry.Invocation.Status);
        Assert.Equal(ResponseSlotStates.IntentionalNoAction, session.Turns[0].ResponseSlot.State);
        Assert.Single(session.Invocations);
    }

    [Fact]
    public void No_action_is_rejected_when_frozen_policy_disallows_it()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            NoActionPermitted = false,
        };
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(values));
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(DecisionValidationOutcomes.Rejected, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(RejectionReasonCategories.PolicyProhibited, result.ValidationEffect.RejectionReasonCategory);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, result.ValidationEffect.EffectOutcome);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
        Assert.NotEqual(AgentInvocationStatuses.ExecutionFailed, result.Invocation!.Status);
        Assert.NotNull(result.Decision);
    }

    [Fact]
    public void Duplicate_apply_of_rejected_communication_does_not_install_a_second_timer_replacement()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
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
        session.RecordDecision(invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        session.ValidateDecision(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        var first = session.ApplyDecisionEffect(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var firstRevisionId = session.CurrentTimerLane!.ScheduleRevisionId;
        var duplicate = session.ApplyDecisionEffect(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(4));
        var revalidated = session.ValidateDecision(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(5));

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.True(duplicate.Succeeded, duplicate.OutcomeCode);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, first.EffectOutcome);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, duplicate.EffectOutcome);
        Assert.False(first.PublicationPathClaimed);
        Assert.False(duplicate.PublicationPathClaimed);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal(firstRevisionId, session.CurrentTimerLane.ScheduleRevisionId);
        Assert.Equal(2, session.CurrentTimerLane.ScheduleRevision);
        Assert.Equal(DecisionValidationOutcomes.Rejected, revalidated.ValidationOutcome);
        Assert.Single(session.Invocations[0].ValidationHistory);
        Assert.Equal(
            DecisionEffectOutcomes.Applied,
            Assert.Single(session.Invocations[0].ValidationEffect!.RequestedActionValidations).EffectOutcome);
    }

    [Fact]
    public void Complete_invocation_retry_after_rejected_validate_still_applies_the_accepted_timer()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
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
        session.RecordDecision(invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var validated = session.ValidateDecision(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.Equal(DecisionValidationOutcomes.Rejected, validated.ValidationOutcome);
        Assert.Equal(TimerValidationOutcomes.Accepted, validated.TimerValidationOutcome);
        Assert.Equal(AgentInvocationStatuses.DecisionRecorded, session.Invocations[0].Status);
        Assert.Equal(
            DecisionEffectOutcomes.NotAttempted,
            session.Invocations[0].ValidationEffect!.EffectOutcome);

        var retried = session.CompleteInvocation(
            invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.True(retried.Succeeded, retried.OutcomeCode);
        Assert.Equal(AgentInvocationStatuses.Decided, retried.Invocation!.Status);
        Assert.Equal(
            DecisionEffectOutcomes.Applied,
            Assert.Single(retried.ValidationEffect!.RequestedActionValidations).EffectOutcome);
        Assert.Equal(1, session.PendingTimerCount);
        Assert.Equal("PT2M", session.CurrentTimerLane!.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, session.CurrentTimerLane.RequestedByCategory);
        Assert.Single(session.Invocations[0].ValidationHistory);
    }

    [Fact]
    public void Rejected_communication_does_not_rearm_an_accepted_timer_after_cutoff()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
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
        session.RecordDecision(invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var validated = session.ValidateDecision(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(3));

        var applied = session.ApplyDecisionEffect(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.Equal(TimerValidationOutcomes.Accepted, validated.TimerValidationOutcome);
        Assert.True(applied.Succeeded, applied.OutcomeCode);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, applied.EffectOutcome);
        Assert.False(applied.PublicationPathClaimed);
        Assert.Equal(0, session.PendingTimerCount);
        Assert.Equal(TimerLaneStates.Cancelled, session.TimerSchedules[0].LaneState);
        Assert.DoesNotContain(
            session.TimerSchedules,
            revision => revision.RequestedByCategory == TimerRequestedByCategories.AgentRecommendation);
        Assert.Equal(
            DecisionEffectOutcomes.NotAttempted,
            Assert.Single(session.Invocations[0].ValidationEffect!.RequestedActionValidations).EffectOutcome);
    }

    [Fact]
    public void Well_formed_prohibited_decision_is_rejected_and_causes_no_effect()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var prohibited = new ProhibitedDecisionRecommendation(
            DecisionId: Guid.NewGuid().ToString("N"),
            InvocationId: invocationId,
            ProducedAt: SessionRuntimeTestFixtures.T0.AddSeconds(2),
            DecisionType: RuntimeDecisionTypes.RequestTool,
            NextTimer: null);

        var result = session.CompleteInvocation(
            invocationId,
            prohibited,
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(RuntimeDecisionTypes.RequestTool, result.Decision!.DecisionType);
        Assert.Equal(DecisionValidationOutcomes.Rejected, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(RejectionReasonCategories.CapabilityDisabled, result.ValidationEffect.RejectionReasonCategory);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, result.ValidationEffect.EffectOutcome);
        Assert.Null(result.ExecutionOutcome);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
        Assert.False(result.PublicationPathClaimed);
    }

    [Fact]
    public void Accepted_emit_message_claims_the_participant_response_slot()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(DecisionValidationOutcomes.Accepted, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.Applied, result.ValidationEffect.EffectOutcome);
        Assert.True(result.PublicationPathClaimed);
        Assert.False(result.AgentMessagePublished);
        Assert.Equal(ResponseSlotStates.ClaimedForPublication, session.Turns[0].ResponseSlot.State);
        Assert.Equal(invocationId, session.Turns[0].ResponseSlot.ClaimedByInvocationId);
        Assert.Equal(TurnStates.WorkQueued, session.Turns[0].State);
    }

    [Fact]
    public void Accepted_opening_emit_message_creates_an_agent_initiated_turn()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(), "idem.open", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(
                invocationId,
                communicationPurpose: "agent_opening",
                turnId: null,
                responseSlotId: null),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(DecisionEffectOutcomes.Applied, result.ValidationEffect!.EffectOutcome);
        Assert.Single(session.Turns);
        Assert.Equal(TurnKinds.AgentOpening, session.Turns[0].Kind);
        Assert.Equal(ResponseSlotStates.ClaimedForPublication, session.Turns[0].ResponseSlot.State);
        Assert.True(result.PublicationPathClaimed);
        Assert.False(result.AgentMessagePublished);
        Assert.DoesNotContain(session.Turns, turn => turn.Kind == TurnKinds.Participant);
    }

    [Fact]
    public void Accepted_timer_emit_message_creates_an_agent_initiated_turn()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.TimerTrigger(), "idem.timer", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(
                invocationId,
                communicationPurpose: "timer_check",
                turnId: null,
                responseSlotId: null),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(TurnKinds.AgentTimer, session.Turns[0].Kind);
        Assert.Equal(ResponseSlotStates.ClaimedForPublication, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public void Agent_initiated_emit_message_ignores_model_supplied_turn_and_slot_ids()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var participantInvocationId = session.Invocations[0].AgentInvocationId;
        session.CompleteInvocation(
            participantInvocationId,
            SessionRuntimeTestFixtures.NoAction(participantInvocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0.AddMinutes(6));
        var openingInvocationId = opening.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            openingInvocationId,
            SessionRuntimeTestFixtures.EmitMessage(
                openingInvocationId,
                communicationPurpose: "agent_opening",
                turnId: "turn.1",
                responseSlotId: "slot.1"),
            SessionRuntimeTestFixtures.T0.AddMinutes(6).AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(2, session.Turns.Count);
        Assert.Equal(TurnKinds.Participant, session.Turns[0].Kind);
        Assert.Equal(ResponseSlotStates.IntentionalNoAction, session.Turns[0].ResponseSlot.State);
        Assert.Equal(TurnKinds.AgentOpening, session.Turns[1].Kind);
        Assert.NotEqual("turn.1", session.Turns[1].TurnId);
        Assert.NotEqual("slot.1", session.Turns[1].ResponseSlot.ResponseSlotId);
        Assert.Equal(openingInvocationId, session.Turns[1].ResponseSlot.ClaimedByInvocationId);
    }

    [Fact]
    public void Duplicate_apply_decision_effect_reconciles_agent_initiated_emit_without_a_second_turn()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(), "idem.open", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var decision = SessionRuntimeTestFixtures.EmitMessage(
            invocationId,
            communicationPurpose: "agent_opening",
            turnId: null,
            responseSlotId: null);
        session.RecordDecision(invocationId, decision, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        session.ValidateDecision(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        var first = session.ApplyDecisionEffect(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var firstTurnId = session.Turns[0].TurnId;
        var firstSlotId = session.Turns[0].ResponseSlot.ResponseSlotId;
        var duplicate = session.ApplyDecisionEffect(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.True(duplicate.Succeeded, duplicate.OutcomeCode);
        Assert.Equal(DecisionEffectOutcomes.Applied, first.EffectOutcome);
        Assert.Equal(DecisionEffectOutcomes.Applied, duplicate.EffectOutcome);
        Assert.Single(session.Turns);
        Assert.Equal(firstTurnId, session.Turns[0].TurnId);
        Assert.Equal(firstSlotId, session.Turns[0].ResponseSlot.ResponseSlotId);
        Assert.Equal(ResponseSlotStates.ClaimedForPublication, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public void Duplicate_apply_decision_effect_does_not_rewrite_a_successful_participant_effect_to_failed()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        var duplicate = session.ApplyDecisionEffect(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.True(duplicate.Succeeded, duplicate.OutcomeCode);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, duplicate.EffectOutcome);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, session.Invocations[0].ValidationEffect!.EffectOutcome);
        Assert.Equal(ResponseSlotStates.IntentionalNoAction, session.Turns[0].ResponseSlot.State);
        Assert.Single(session.Turns);
    }

    [Fact]
    public void Revalidating_after_a_terminal_effect_does_not_reset_the_effect_outcome()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        var revalidated = session.ValidateDecision(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var duplicate = session.ApplyDecisionEffect(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.Equal(DecisionValidationOutcomes.Accepted, revalidated.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, session.Invocations[0].ValidationEffect!.EffectOutcome);
        Assert.True(duplicate.Succeeded, duplicate.OutcomeCode);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, duplicate.EffectOutcome);
        Assert.Equal(ResponseSlotStates.IntentionalNoAction, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public void Decision_for_a_different_invocation_cannot_be_attached()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction("other-invocation"),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.IdentityMismatch, result.OutcomeCode);
        Assert.Null(session.Invocations[0].Decision);
        Assert.Equal(AgentInvocationStatuses.Admitted, session.Invocations[0].Status);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public void Non_turn_no_action_creates_no_turn_and_records_explicit_no_domain_effect()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(), "idem.open", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, result.ValidationEffect!.EffectOutcome);
        Assert.Empty(session.Turns);
        Assert.False(result.PublicationPathClaimed);
        Assert.False(result.AgentMessagePublished);
        Assert.Equal(AgentInvocationStatuses.Decided, result.Invocation!.Status);
    }

    [Fact]
    public void Rejected_timer_request_does_not_reject_an_otherwise_valid_primary_decision()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var oversizedTimer = new NextTimerRecommendation("PT48H", "1");

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId, nextTimer: oversizedTimer),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(DecisionValidationOutcomes.Accepted, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, result.ValidationEffect.EffectOutcome);
        Assert.Equal(TimerValidationOutcomes.Rejected, result.ValidationEffect.TimerValidationOutcome);
        Assert.Equal(ResponseSlotStates.IntentionalNoAction, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public void Accepted_decision_whose_slot_claim_fails_is_an_effect_failure_not_an_execution_outcome()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var decision = SessionRuntimeTestFixtures.EmitMessage(invocationId);

        var recorded = session.RecordDecision(invocationId, decision, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var validated = session.ValidateDecision(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var applied = session.ApplyDecisionEffect(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.True(recorded.Succeeded, recorded.OutcomeCode);
        Assert.Equal(DecisionValidationOutcomes.Accepted, validated.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.EffectFailed, applied.EffectOutcome);
        Assert.Equal(InvocationCompletionOutcomeCodes.EffectFailed, applied.OutcomeCode);
        Assert.NotNull(session.Invocations[0].Decision);
        Assert.Null(session.Invocations[0].ExecutionOutcome);
        Assert.NotEqual(ResponseSlotStates.ClaimedForPublication, session.Turns[0].ResponseSlot.State);
        Assert.False(applied.PublicationPathClaimed);
    }

    [Fact]
    public void Malformed_control_is_an_execution_outcome_not_a_decision_rejection()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            new ExecutionFailureCompletion(ExecutionFailureReasons.MalformedControl),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.Null(result.Decision);
        Assert.Null(result.ValidationEffect);
        Assert.Equal(ExecutionFailureReasons.MalformedControl, result.ExecutionOutcome!.ReasonCategory);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
    }
}
