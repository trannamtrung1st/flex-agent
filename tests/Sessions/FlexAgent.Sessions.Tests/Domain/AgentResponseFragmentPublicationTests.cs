using System.Security.Cryptography;
using System.Text;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class AgentResponseFragmentPublicationTests
{
    [Fact]
    public void First_fragment_claims_generation_attempt_and_agent_message_for_the_slot()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Assert.All(
            session.Invocations[0].ValidationEffect!.OutputValidations,
            item => Assert.Null(item.AgentOutputId));
        Assert.Empty(session.AgentMessages);
        var clock = SessionRuntimeTestFixtures.T0.AddSeconds(3);

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            clock);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(FragmentCommitOutcomeCodes.Succeeded, result.OutcomeCode);
        var message = Assert.Single(session.AgentMessages);
        Assert.Equal("agen.test.1", message.GenerationAttemptId);
        Assert.StartsWith("aout.", message.MessageId, StringComparison.Ordinal);
        Assert.Equal(invocationId, message.DrivingInvocationId);
        Assert.Equal(session.Invocations[0].Decision!.DecisionId, message.DrivingDecisionId);
        Assert.Equal("turn.1", message.TurnId);
        Assert.Equal("slot.1", message.ResponseSlotId);
        Assert.Equal(AgentMessageCompletionStates.Open, message.CompletionState);
        var fragment = Assert.Single(message.Fragments);
        Assert.Equal(1, fragment.FragmentOrdinal);
        Assert.Equal(DigestUtf8("Hel"), fragment.ContentDigest);
        Assert.True(fragment.SessionSequence > 0);
        Assert.Contains(
            session.VisibleTranscript,
            item => item.AuthorType == TranscriptAuthorTypes.Agent && item.MessageId == message.MessageId);
        Assert.True(result.AgentMessagePublished);
        Assert.All(
            session.Invocations[0].ValidationEffect!.OutputValidations,
            item => Assert.Null(item.AgentOutputId));
    }

    [Fact]
    public void Envelope_first_fragment_uses_the_runtime_owned_output_id()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var completed = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput()]),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var outputId = completed.ValidationEffect!.OutputValidations
            .Single(item => item.ValidationOutcome == DecisionValidationOutcomes.Accepted)
            .AgentOutputId;

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.env.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(outputId, session.AgentMessages[0].MessageId);
        Assert.Equal(outputId, result.Message!.MessageId);
    }

    [Fact]
    public void Contiguous_fragments_append_and_complete_seals_assembled_digest()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);

        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "lo", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);

        var completed = session.CompleteAgentResponseMessage(
            invocationId,
            SessionRuntimeTestFixtures.T0.AddSeconds(5));

        Assert.True(completed.Succeeded, completed.OutcomeCode);
        var message = Assert.Single(session.AgentMessages);
        Assert.Equal(AgentMessageCompletionStates.Complete, message.CompletionState);
        Assert.Equal(1, message.FirstFragmentOrdinal);
        Assert.Equal(2, message.LastFragmentOrdinal);
        Assert.Equal(DigestUtf8("Hello"), message.AssembledContentDigest);
        Assert.Equal(TurnStates.Complete, session.Turns[0].State);
        Assert.Equal("Hello", message.AssembleExactText());
    }

    [Fact]
    public void Duplicate_ordinal_and_digest_reconciles_without_a_second_fragment()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        var first = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var sequence = session.SessionSequence;
        var version = session.SessionVersion;

        var retry = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(FragmentCommitOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Equal(sequence, session.SessionSequence);
        Assert.Equal(version, session.SessionVersion);
        Assert.Single(session.AgentMessages[0].Fragments);
        Assert.Equal(first.Fragment!.SessionSequence, retry.Fragment!.SessionSequence);
    }

    [Fact]
    public void Gap_digest_mismatch_and_competing_attempt_fail_without_mutation()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var sequence = session.SessionSequence;

        var gap = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 3, "lo", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4));
        var mismatch = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "HEL", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(5));
        var competing = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "lo", "agen.other.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(6));

        Assert.False(gap.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.Gap, gap.OutcomeCode);
        Assert.False(mismatch.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.DigestMismatch, mismatch.OutcomeCode);
        Assert.False(competing.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.CompetingAttempt, competing.OutcomeCode);
        Assert.Equal(sequence, session.SessionSequence);
        Assert.Single(session.AgentMessages[0].Fragments);
    }

    [Fact]
    public void Empty_delta_and_unclaimed_publication_are_rejected()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;

        var unclaimed = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var empty = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, string.Empty, "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.False(unclaimed.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.PublicationNotClaimed, unclaimed.OutcomeCode);
        Assert.False(empty.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.EmptyDelta, empty.OutcomeCode);
        Assert.Empty(session.AgentMessages);
    }

    [Fact]
    public void Pause_and_terminal_cutoff_reject_new_fragments_and_incomplete_preserves_prefix()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        session.Pause(SessionRuntimeTestFixtures.T0.AddSeconds(4));

        var paused = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "lo", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(5));
        var incomplete = session.MarkAgentResponseIncomplete(
            invocationId,
            SessionRuntimeTestFixtures.T0.AddSeconds(6));

        Assert.False(paused.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.Cutoff, paused.OutcomeCode);
        Assert.True(incomplete.Succeeded, incomplete.OutcomeCode);
        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
        Assert.Equal("Hel", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(DigestUtf8("Hel"), session.AgentMessages[0].AssembledContentDigest);
        Assert.Single(session.AgentMessages[0].Fragments);

        session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(7));
        var afterCutoff = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "lo", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(8));
        Assert.False(afterCutoff.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.AlreadyTerminal, afterCutoff.OutcomeCode);
    }

    [Fact]
    public void Cutoff_with_a_visible_prefix_seals_the_message_incomplete()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
        Assert.Equal("Hel", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(TurnStates.Complete, session.Turns[0].State);
        var late = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "lo", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(5));
        Assert.False(late.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.AlreadyTerminal, late.OutcomeCode);
    }

    [Fact]
    public void Duplicate_fragment_after_complete_reconciles_instead_of_failing_terminal()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        session.CompleteAgentResponseMessage(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(4));

        var retry = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(5));
        var extra = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "!", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(6));

        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(FragmentCommitOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Single(session.AgentMessages[0].Fragments);
        Assert.False(extra.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.AlreadyTerminal, extra.OutcomeCode);
        Assert.Equal("Hi", session.AgentMessages[0].AssembleExactText());
    }

    [Fact]
    public void Competing_attempt_after_complete_is_rejected()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        session.CompleteAgentResponseMessage(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(4));

        var competing = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.2"),
            SessionRuntimeTestFixtures.T0.AddSeconds(5));

        Assert.False(competing.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.CompetingAttempt, competing.OutcomeCode);
        Assert.Equal("agen.1", session.AgentMessages[0].GenerationAttemptId);
        Assert.Single(session.AgentMessages[0].Fragments);
    }

    [Fact]
    public void Digest_mismatch_after_complete_is_rejected_as_digest_mismatch()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        session.CompleteAgentResponseMessage(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(4));

        var mismatch = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Ho", "agen.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(5));

        Assert.False(mismatch.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.DigestMismatch, mismatch.OutcomeCode);
        Assert.Equal("Hi", session.AgentMessages[0].AssembleExactText());
    }

    [Fact]
    public void Competing_attempt_after_incomplete_is_rejected()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        session.Pause(SessionRuntimeTestFixtures.T0.AddSeconds(4));
        session.MarkAgentResponseIncomplete(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(5));

        var competing = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.2"),
            SessionRuntimeTestFixtures.T0.AddSeconds(6));

        Assert.False(competing.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.CompetingAttempt, competing.OutcomeCode);
        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
        Assert.Equal("agen.1", session.AgentMessages[0].GenerationAttemptId);
    }

    [Fact]
    public void Complete_invocation_retry_after_visibility_reports_agent_message_published()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var recommendation = SessionRuntimeTestFixtures.EmitMessage(invocationId);
        session.CompleteInvocation(invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(2));
        session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        var retry = session.CompleteInvocation(
            invocationId, recommendation, SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.True(retry.PublicationPathClaimed);
        Assert.True(retry.AgentMessagePublished);
    }

    [Fact]
    public void Opening_and_timer_streams_reuse_the_same_fragment_path()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(), "idem.open", SessionRuntimeTestFixtures.T0);
        var openingId = opening.Invocation!.AgentInvocationId;
        session.CompleteInvocation(
            openingId,
            SessionRuntimeTestFixtures.EmitMessage(
                openingId,
                communicationPurpose: "agent_opening",
                turnId: null,
                responseSlotId: null),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(openingId, 1, "Hi", "agen.open.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        Assert.True(session.CompleteAgentResponseMessage(
            openingId,
            SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);

        var timer = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.TimerTrigger(), "idem.timer", SessionRuntimeTestFixtures.T0.AddSeconds(5));
        var timerId = timer.Invocation!.AgentInvocationId;
        session.CompleteInvocation(
            timerId,
            SessionRuntimeTestFixtures.EmitMessage(
                timerId,
                communicationPurpose: "timer_check",
                turnId: null,
                responseSlotId: null),
            SessionRuntimeTestFixtures.T0.AddSeconds(6));
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(timerId, 1, "Ping", "agen.timer.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(7)).Succeeded);

        Assert.Equal(2, session.AgentMessages.Count);
        Assert.Equal(TurnKinds.AgentOpening, session.Turns[0].Kind);
        Assert.Equal(TurnStates.Complete, session.Turns[0].State);
        Assert.Equal(TurnKinds.AgentTimer, session.Turns[1].Kind);
        Assert.Equal(2, session.VisibleTranscript.Count(item => item.AuthorType == TranscriptAuthorTypes.Agent));
    }

    [Fact]
    public void No_action_cannot_publish_a_fragment()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        var result = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hi", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.PublicationNotClaimed, result.OutcomeCode);
        Assert.Empty(session.AgentMessages);
        Assert.DoesNotContain(session.VisibleTranscript, item => item.AuthorType == TranscriptAuthorTypes.Agent);
    }

    [Fact]
    public void Completed_projection_does_not_replace_fragments_with_later_text()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "lo", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4));
        session.CompleteAgentResponseMessage(invocationId, SessionRuntimeTestFixtures.T0.AddSeconds(5));

        var afterComplete = session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 3, "!", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(6));

        Assert.False(afterComplete.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.AlreadyTerminal, afterComplete.OutcomeCode);
        Assert.Equal("Hello", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(DigestUtf8("Hello"), session.AgentMessages[0].AssembledContentDigest);
    }

    [Fact]
    public void Pending_publication_work_is_the_dirty_message_and_only_new_fragments()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        Assert.Empty(session.PendingPublicationWork);

        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        var message = Assert.Single(session.PendingPublicationWork);
        Assert.Equal(1, message.PendingInsertCount);

        message.Fragments[0].MarkPersisted();
        message.MarkMessagePersisted();
        message.ClearPersistedPendingInserts();
        session.RemoveCleanPublicationWork();
        Assert.Empty(session.PendingPublicationWork);

        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 2, "lo", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);
        Assert.Same(message, Assert.Single(session.PendingPublicationWork));
        Assert.Equal(1, message.PendingInsertCount);
        Assert.Equal("lo", Assert.Single(message.PendingInserts).ExactUtf8Text);
        Assert.Equal(2, message.Fragments.Count);
    }

    private static string ClaimParticipantPublication(SessionRuntime session)
    {
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var completed = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        Assert.True(completed.PublicationPathClaimed);
        Assert.False(completed.AgentMessagePublished);
        return invocationId;
    }

    private static string DigestUtf8(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
