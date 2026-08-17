using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class InvocationExecutionTests
{
    [Fact]
    public void Successful_completion_records_exactly_one_decision_and_no_execution_outcome()
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
        Assert.Equal(InvocationCompletionOutcomeCodes.Decided, result.OutcomeCode);
        Assert.Equal(AgentInvocationStatuses.Decided, result.Invocation!.Status);
        Assert.NotNull(result.Decision);
        Assert.Null(result.ExecutionOutcome);
        Assert.Equal(result.Decision!.DecisionId, result.Invocation.AgentDecisionId);
        Assert.Null(result.Invocation.ExecutionOutcomeId);
        Assert.Single(session.Invocations);
    }

    [Fact]
    public void Second_decision_on_the_same_invocation_is_rejected()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        var second = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(second.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.AlreadyTerminal, second.OutcomeCode);
        Assert.Equal(RuntimeDecisionTypes.NoAction, session.Invocations[0].Decision!.DecisionType);
        Assert.Null(session.Invocations[0].ExecutionOutcome);
    }

    [Fact]
    public void Complete_invocation_resumes_after_decision_recorded_instead_of_returning_already_terminal()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var recommendation = SessionRuntimeTestFixtures.NoAction(invocationId);

        var recorded = session.RecordDecision(
            invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        Assert.True(recorded.Succeeded, recorded.OutcomeCode);
        Assert.Equal(AgentInvocationStatuses.DecisionRecorded, recorded.Invocation!.Status);
        Assert.False(recorded.Invocation.IsTerminal);

        var resumed = session.CompleteInvocation(
            invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(3));
        Assert.True(resumed.Succeeded, resumed.OutcomeCode);
        Assert.Equal(InvocationCompletionOutcomeCodes.Decided, resumed.OutcomeCode);
        Assert.Equal(AgentInvocationStatuses.Decided, resumed.Invocation!.Status);
        Assert.True(resumed.Invocation.IsTerminal);
        Assert.Equal(recommendation.DecisionId, resumed.Decision!.DecisionId);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, resumed.ValidationEffect!.EffectOutcome);
        Assert.Single(session.Invocations);
        Assert.Equal(1, session.Invocations[0].Attempts.Count(attempt =>
            attempt.OutcomeCategory == ExecutionAttemptOutcomeCategories.DecisionProduced));
    }

    [Fact]
    public void Complete_invocation_retry_of_the_same_decision_reconciles_after_the_pipeline_completes()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var recommendation = SessionRuntimeTestFixtures.NoAction(invocationId);

        var first = session.CompleteInvocation(
            invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var retry = session.CompleteInvocation(
            invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(InvocationCompletionOutcomeCodes.Decided, retry.OutcomeCode);
        Assert.Equal(first.Decision!.DecisionId, retry.Decision!.DecisionId);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, retry.ValidationEffect!.EffectOutcome);
        Assert.Equal(ResponseSlotStates.IntentionalNoAction, session.Turns[0].ResponseSlot.State);
        Assert.Single(session.Invocations);
    }

    [Fact]
    public void Same_decision_ids_with_a_different_payload_are_rejected()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var noAction = SessionRuntimeTestFixtures.NoAction(invocationId);
        session.RecordDecision(invocationId, noAction, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var mismatched = SessionRuntimeTestFixtures.EmitMessage(invocationId) with
        {
            DecisionId = noAction.DecisionId,
        };

        var retry = session.CompleteInvocation(
            invocationId, mismatched, SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(retry.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.IdentityMismatch, retry.OutcomeCode);
        Assert.Equal(RuntimeDecisionTypes.NoAction, session.Invocations[0].Decision!.DecisionType);
        Assert.Equal(noAction.DecisionId, session.Invocations[0].Decision!.DecisionId);
        Assert.Equal(AgentInvocationStatuses.DecisionRecorded, session.Invocations[0].Status);
        Assert.Null(session.Invocations[0].ValidationEffect);
    }

    [Theory]
    [InlineData(ExecutionFailureReasons.MalformedControl)]
    [InlineData(ExecutionFailureReasons.IncompleteControl)]
    [InlineData(ExecutionFailureReasons.ProviderTimeout)]
    [InlineData(ExecutionFailureReasons.ProviderUnavailable)]
    public void Infrastructure_failure_records_an_execution_outcome_and_never_a_decision(string reason)
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            new ExecutionFailureCompletion(reason),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(InvocationCompletionOutcomeCodes.ExecutionFailed, result.OutcomeCode);
        Assert.Equal(AgentInvocationStatuses.ExecutionFailed, result.Invocation!.Status);
        Assert.Null(result.Decision);
        Assert.NotNull(result.ExecutionOutcome);
        Assert.Equal(reason, result.ExecutionOutcome!.ReasonCategory);
        Assert.NotEqual(RuntimeDecisionTypes.NoAction, result.ExecutionOutcome.ReasonCategory);
        Assert.Null(result.Invocation.AgentDecisionId);
        Assert.DoesNotContain(
            session.Invocations[0].Attempts,
            attempt => attempt.OutcomeCategory == ExecutionAttemptOutcomeCategories.DecisionProduced);
    }

    [Fact]
    public void Execution_failure_retry_returns_already_terminal_without_mutating()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var first = session.CompleteInvocation(
            invocationId,
            new ExecutionFailureCompletion(ExecutionFailureReasons.MalformedControl),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var version = session.SessionVersion;
        var outcomeId = first.ExecutionOutcome!.ExecutionOutcomeId;

        var retry = session.CompleteInvocation(
            invocationId,
            new ExecutionFailureCompletion(ExecutionFailureReasons.MalformedControl),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(retry.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.AlreadyTerminal, retry.OutcomeCode);
        Assert.Equal(version, session.SessionVersion);
        Assert.Equal(outcomeId, session.Invocations[0].ExecutionOutcome!.ExecutionOutcomeId);
        Assert.Single(session.Invocations);
    }

    [Fact]
    public void Bounded_failed_attempts_exhaust_without_fabricating_a_decision()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            InvocationBounds = new InvocationBounds(2, 10, 0, 5, 30),
        };
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(values));
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var first = session.RecordFailedAttempt(
            invocationId,
            ExecutionFailureReasons.ProviderTimeout,
            SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.Equal(AgentInvocationStatuses.Admitted, first.Invocation!.Status);
        Assert.Null(first.Decision);

        var second = session.RecordFailedAttempt(
            invocationId,
            ExecutionFailureReasons.ProviderTimeout,
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(InvocationCompletionOutcomeCodes.AttemptsExhausted, second.OutcomeCode);
        Assert.Equal(AgentInvocationStatuses.ExecutionFailed, second.Invocation!.Status);
        Assert.Equal(ExecutionOutcomeCategories.AttemptsExhausted, second.ExecutionOutcome!.OutcomeCategory);
        Assert.Null(second.Decision);
        Assert.Equal(2, session.Invocations[0].Attempts.Count);
        Assert.Contains(
            session.ManifestRuntimeRecords,
            record => record.PayloadRef.ProtectedRef == $"{invocationId}.outcome"
                && record.PayloadRef.ContentDigest
                    == ProtectedContentRef.DigestUtf8($"failed:{ExecutionOutcomeCategories.AttemptsExhausted}"));
    }

    [Fact]
    public void Execution_failure_after_decision_recorded_does_not_replace_the_decision()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var recommendation = SessionRuntimeTestFixtures.NoAction(invocationId);
        session.RecordDecision(invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        var failed = session.CompleteInvocation(
            invocationId,
            new ExecutionFailureCompletion(ExecutionFailureReasons.ProviderTimeout),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(failed.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.AlreadyTerminal, failed.OutcomeCode);
        Assert.Equal(AgentInvocationStatuses.DecisionRecorded, session.Invocations[0].Status);
        Assert.NotNull(session.Invocations[0].Decision);
        Assert.Null(session.Invocations[0].ExecutionOutcome);
        Assert.Equal(recommendation.DecisionId, session.Invocations[0].Decision!.DecisionId);
    }

    [Fact]
    public void Late_result_after_cutoff_is_an_execution_outcome_not_no_action()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(InvocationCompletionOutcomeCodes.LateResult, result.OutcomeCode);
        Assert.Null(result.Decision);
        Assert.NotNull(result.ExecutionOutcome);
        Assert.Equal(ExecutionOutcomeCategories.LateResult, result.ExecutionOutcome!.OutcomeCategory);
        Assert.Equal(AgentInvocationStatuses.Cancelled, result.Invocation!.Status);
        Assert.NotEqual(ResponseSlotStates.IntentionalNoAction, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public void Provider_produced_at_does_not_choose_authoritative_session_sequence()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var sequenceBefore = session.SessionSequence;
        var futureProducedAt = SessionRuntimeTestFixtures.T0.AddYears(1);

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId) with { ProducedAt = futureProducedAt },
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(sequenceBefore + 1, result.Invocation!.SessionSequence);
        Assert.True(result.Invocation.SessionSequence > admitted.SessionSequence);
        Assert.NotEqual(futureProducedAt, session.LastCommittedAt);
        Assert.Equal(SessionRuntimeTestFixtures.T0.AddSeconds(2), session.LastCommittedAt);
    }

    [Fact]
    public void Non_utc_completion_clock_cannot_mutate_authoritative_state()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var offsetTime = new DateTimeOffset(2026, 8, 13, 7, 0, 2, TimeSpan.FromHours(7));
        var sequenceBefore = session.SessionSequence;
        var committedBefore = session.LastCommittedAt;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            offsetTime);

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.NonUtcClock, result.OutcomeCode);
        Assert.Null(session.Invocations[0].Decision);
        Assert.Equal(AgentInvocationStatuses.Admitted, session.Invocations[0].Status);
        Assert.Equal(sequenceBefore, session.SessionSequence);
        Assert.Equal(committedBefore, session.LastCommittedAt);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public void Authoritative_clock_older_than_last_committed_at_cannot_mutate()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var result = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(-1));

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.StaleClock, result.OutcomeCode);
        Assert.Null(session.Invocations[0].Decision);
        Assert.Equal(AgentInvocationStatuses.Admitted, session.Invocations[0].Status);
        Assert.Equal(SessionRuntimeTestFixtures.T0, session.LastCommittedAt);
    }
}
