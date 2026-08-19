using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class AgentResponsePublicationBoundsAndValidationTests
{
    [Fact]
    public void Oversized_fragment_is_rejected_without_mutation()
    {
        var session = CreateSession(new StreamingPublicationBounds(4, 40, 64, 8_192, 2));
        var invocationId = ClaimParticipantPublication(session);
        var version = session.SessionVersion;

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hello", "agen.bound.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.FragmentTooLarge, result.OutcomeCode);
        Assert.DoesNotContain("Hello", result.OutcomeCode, StringComparison.Ordinal);
        Assert.Empty(session.AgentMessages);
        Assert.Equal(version, session.SessionVersion);
    }

    [Fact]
    public void Fourth_fragment_exceeds_frozen_count_and_preserves_the_prefix()
    {
        var session = CreateSession(new StreamingPublicationBounds(512, 40, 3, 8_192, 2));
        var invocationId = ClaimParticipantPublication(session);
        Commit(session, invocationId, 1, "a");
        Commit(session, invocationId, 2, "b");
        Commit(session, invocationId, 3, "c");

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 4, "d", "agen.bound.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(6));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.FragmentCountExceeded, result.OutcomeCode);
        Assert.Equal("abc", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(AgentMessageCompletionStates.Open, session.AgentMessages[0].CompletionState);
    }

    [Fact]
    public void Assembled_size_bound_rejects_the_delta_that_would_cross_it()
    {
        var session = CreateSession(new StreamingPublicationBounds(512, 40, 64, 4, 2));
        var invocationId = ClaimParticipantPublication(session);
        Commit(session, invocationId, 1, "ab");

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "cde", "agen.bound.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.AssembledSizeExceeded, result.OutcomeCode);
        Assert.Equal("ab", session.AgentMessages[0].AssembleExactText());
    }

    [Fact]
    public void In_flight_stream_bound_rejects_a_new_publisher()
    {
        var session = CreateSession(new StreamingPublicationBounds(512, 40, 64, 8_192, 1));
        var first = ClaimParticipantPublication(session, "1");
        Commit(session, first, 1, "Hi", "agen.bound.1", SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var second = ClaimParticipantPublication(session, "2", session.LastCommittedAt.AddSeconds(6));

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(second, 1, "Yo", "agen.bound.2"),
            session.LastCommittedAt);

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.InFlightExceeded, result.OutcomeCode);
        Assert.Single(session.AgentMessages);
        Assert.Equal("Hi", session.AgentMessages[0].AssembleExactText());
    }

    [Fact]
    public void Fragment_rate_uses_a_trailing_one_second_session_window()
    {
        var session = CreateSession(new StreamingPublicationBounds(512, 2, 64, 8_192, 2));
        var invocationId = ClaimParticipantPublication(session);
        Commit(session, invocationId, 1, "a", clock: SessionRuntimeTestFixtures.T0.AddSeconds(3));
        Commit(session, invocationId, 2, "b", clock: SessionRuntimeTestFixtures.T0.AddSeconds(3).AddMilliseconds(100));

        var limited = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 3, "c", "agen.bound.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3).AddMilliseconds(200));
        Assert.False(limited.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.RateExceeded, limited.OutcomeCode);
        Assert.Equal("ab", session.AgentMessages[0].AssembleExactText());

        var afterWindow = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 3, "c", "agen.bound.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4).AddMilliseconds(101));
        Assert.True(afterWindow.Succeeded, afterWindow.OutcomeCode);
        Assert.Equal("abc", session.AgentMessages[0].AssembleExactText());
    }

    [Fact]
    public void Duplicate_reconcile_does_not_consume_rate_budget()
    {
        var session = CreateSession(new StreamingPublicationBounds(512, 1, 64, 8_192, 2));
        var invocationId = ClaimParticipantPublication(session);
        Commit(session, invocationId, 1, "a", clock: SessionRuntimeTestFixtures.T0.AddSeconds(3));

        var retry = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "a", "agen.bound.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3).AddMilliseconds(10));

        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(FragmentCommitOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Single(session.AgentMessages[0].Fragments);
    }

    [Fact]
    public void Control_characters_are_rejected_without_echo()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "ok\0secret", "agen.val.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.ValidationFailed, result.OutcomeCode);
        Assert.DoesNotContain("secret", result.OutcomeCode, StringComparison.Ordinal);
        Assert.Empty(session.AgentMessages);
    }

    [Fact]
    public void Split_script_markup_fails_the_later_delta_and_keeps_the_safe_prefix()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Commit(session, invocationId, 1, "Hello <scr", generationAttemptId: "agen.val.1");

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "ipt>alert(1)", "agen.val.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.ValidationFailed, result.OutcomeCode);
        Assert.DoesNotContain("alert", result.OutcomeCode, StringComparison.Ordinal);
        Assert.Equal("Hello <scr", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(AgentMessageCompletionStates.Open, session.AgentMessages[0].CompletionState);
    }

    [Fact]
    public void Tab_newline_and_cr_remain_recordable()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "line\r\n\tnext", "agen.val.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("line\r\n\tnext", session.AgentMessages[0].AssembleExactText());
    }

    [Fact]
    public void Unpaired_surrogate_is_rejected_without_mutation()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        var version = session.SessionVersion;

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "\uD800", "agen.val.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.ValidationFailed, result.OutcomeCode);
        Assert.Empty(session.AgentMessages);
        Assert.Equal(version, session.SessionVersion);
    }

    [Fact]
    public void Unpublished_completed_publication_cancels_the_claimed_slot_and_turn()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Assert.True(session.HasOpenAgentContentPublication(invocationId));

        var result = session.FailUnpublishedAgentResponse(
            invocationId,
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(FragmentCommitOutcomeCodes.UnpublishedFailed, result.OutcomeCode);
        Assert.Empty(session.AgentMessages);
        Assert.False(session.HasOpenAgentContentPublication(invocationId));
        Assert.Equal(TurnStates.Cancelled, session.Turns[0].State);
        Assert.Equal(ResponseSlotStates.Cancelled, session.Turns[0].ResponseSlot.State);
    }

    private static SessionRuntime CreateSession(StreamingPublicationBounds bounds) =>
        SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(
                RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
                {
                    StreamingPublicationBounds = bounds,
                }));

    private static void Commit(
        SessionRuntime session,
        string invocationId,
        int ordinal,
        string text,
        string generationAttemptId = "agen.bound.1",
        DateTimeOffset? clock = null)
    {
        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, ordinal, text, generationAttemptId),
            clock ?? SessionRuntimeTestFixtures.T0.AddSeconds(2 + ordinal));
        Assert.True(result.Succeeded, result.OutcomeCode);
    }

    private static string ClaimParticipantPublication(
        SessionRuntime session,
        string key = "1",
        DateTimeOffset? at = null)
    {
        var clock = at ?? session.LastCommittedAt;
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            $"msg.p.{key}",
            $"turn.{key}",
            $"slot.{key}",
            $"trig.participant.{key}",
            $"idem.p.{key}",
            clock);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var completed = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(
                invocationId,
                turnId: $"turn.{key}",
                responseSlotId: $"slot.{key}"),
            session.LastCommittedAt);
        Assert.True(completed.PublicationPathClaimed, completed.OutcomeCode);
        return invocationId;
    }
}
