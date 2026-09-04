using System.Globalization;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class HostedSessionProjectionTests
{
    [Fact]
    public void Participant_unavailable_timing_closes_send_and_complete()
    {
        var actions = SessionPermittedActionsProjector.Project(
            HostedSessionProjectionKinds.Participant,
            SessionLifecycleState.Active,
            remainingSeconds: null,
            timingPolicy: "unavailable");

        Assert.DoesNotContain(HostedSessionPermittedActions.SendMessage, actions);
        Assert.DoesNotContain(HostedSessionPermittedActions.CompleteSession, actions);
        Assert.Contains(HostedSessionPermittedActions.Reconcile, actions);
    }

    [Fact]
    public void Administrator_paused_unavailable_timing_closes_resume()
    {
        var actions = SessionPermittedActionsProjector.Project(
            HostedSessionProjectionKinds.Administrator,
            SessionLifecycleState.Paused,
            timingPolicy: "unavailable");

        Assert.DoesNotContain(HostedSessionPermittedActions.ResumeSession, actions);
        Assert.Contains(HostedSessionPermittedActions.TerminateSession, actions);
    }

    [Fact]
    public void Participant_active_permits_send_complete_and_reconcile()
    {
        var actions = SessionPermittedActionsProjector.Project(
            HostedSessionProjectionKinds.Participant,
            SessionLifecycleState.Active);

        Assert.Contains(HostedSessionPermittedActions.SendMessage, actions);
        Assert.Contains(HostedSessionPermittedActions.CompleteSession, actions);
        Assert.DoesNotContain(HostedSessionPermittedActions.PauseSession, actions);
        Assert.DoesNotContain(HostedSessionPermittedActions.TerminateSession, actions);
    }

    [Fact]
    public void Participant_active_at_zero_remaining_closes_send_and_complete()
    {
        var actions = SessionPermittedActionsProjector.Project(
            HostedSessionProjectionKinds.Participant,
            SessionLifecycleState.Active,
            remainingSeconds: 0);

        Assert.DoesNotContain(HostedSessionPermittedActions.SendMessage, actions);
        Assert.DoesNotContain(HostedSessionPermittedActions.CompleteSession, actions);
        Assert.Contains(HostedSessionPermittedActions.Reconcile, actions);
    }

    [Fact]
    public void Participant_completing_still_permits_idempotent_complete()
    {
        var actions = SessionPermittedActionsProjector.Project(
            HostedSessionProjectionKinds.Participant,
            SessionLifecycleState.Completing);

        Assert.Contains(HostedSessionPermittedActions.CompleteSession, actions);
        Assert.DoesNotContain(HostedSessionPermittedActions.SendMessage, actions);
    }

    [Fact]
    public void Administrator_active_permits_pause_and_terminate_without_transcript_actions()
    {
        var actions = SessionPermittedActionsProjector.Project(
            HostedSessionProjectionKinds.Administrator,
            SessionLifecycleState.Active);

        Assert.Equal(
            [HostedSessionPermittedActions.PauseSession, HostedSessionPermittedActions.TerminateSession],
            actions);
    }

    [Fact]
    public void Historical_non_terminal_has_no_live_controls()
    {
        var actions = SessionPermittedActionsProjector.Project(
            HostedSessionProjectionKinds.Historical,
            SessionLifecycleState.Active);

        Assert.Empty(actions);
    }

    [Fact]
    public void Snapshot_administrator_omits_transcript_and_submission()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var snapshot = HostedSessionSnapshotProjector.Project(
            session,
            HostedSessionProjectionKinds.Administrator,
            DateTimeOffset.Parse("2026-09-03T00:00:00Z"));

        Assert.Equal(HostedSessionProjectionKinds.Administrator, snapshot.ProjectionKind);
        Assert.Empty(snapshot.Transcript);
        Assert.Equal(0, snapshot.BoundSubmissionCount);
        Assert.Null(snapshot.AgentDisplayName);
        Assert.Equal("disabled", snapshot.TimingPolicy);
        Assert.Null(snapshot.RemainingSeconds);
    }

    [Fact]
    public void Snapshot_participant_projects_active_duration_remaining()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var started = DateTimeOffset.Parse("2026-09-03T00:00:00Z");
        var observed = started.AddMinutes(15);
        var snapshot = HostedSessionSnapshotProjector.Project(
            session,
            HostedSessionProjectionKinds.Participant,
            observed,
            started,
            new HostedFrozenTimingPolicy(
                HostedTimingReconstruction.Timed,
                HostedSessionTiming.SyntheticDevelopmentActiveDurationSeconds,
                [
                    new HostedTimingWarningThreshold("approaching", 15 * 60),
                    new HostedTimingWarningThreshold("imminent", 10 * 60),
                ]));

        Assert.Equal("active_duration", snapshot.TimingPolicy);
        Assert.Equal(30 * 60, snapshot.RemainingSeconds);
        Assert.Equal(45 * 60, snapshot.TimingBudgetSeconds);
    }

    [Fact]
    public void Snapshot_maps_intentional_no_action_work_state()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);
        Assert.True(
            session.CompleteInvocation(
                admitted.Invocation!.AgentInvocationId,
                SessionRuntimeTestFixtures.NoAction(admitted.Invocation.AgentInvocationId),
                SessionRuntimeTestFixtures.T0.AddSeconds(2)).Succeeded);

        var snapshot = HostedSessionSnapshotProjector.Project(
            session,
            HostedSessionProjectionKinds.Participant,
            DateTimeOffset.Parse("2026-09-03T00:00:00Z"));

        Assert.Equal("no_action", snapshot.ActivityWorkState);
    }

    [Fact]
    public void Snapshot_maps_execution_failure_to_failed_work_state()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);
        Assert.True(
            session.CompleteInvocation(
                admitted.Invocation!.AgentInvocationId,
                new ExecutionFailureCompletion(ExecutionFailureReasons.ProviderUnavailable),
                SessionRuntimeTestFixtures.T0.AddSeconds(2)).Succeeded);

        var snapshot = HostedSessionSnapshotProjector.Project(
            session,
            HostedSessionProjectionKinds.Participant,
            DateTimeOffset.Parse("2026-09-03T00:00:00Z"));

        Assert.Equal("failed", snapshot.ActivityWorkState);
    }

    [Fact]
    public void Snapshot_participant_assembles_agent_text_when_visible_row_has_no_inline_copy()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).PublicationPathClaimed);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hello examiner", "agen.proj.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        Assert.True(session.CompleteAgentResponseMessage(
            invocationId,
            SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);

        var snapshot = HostedSessionSnapshotProjector.Project(
            session,
            HostedSessionProjectionKinds.Participant,
            DateTimeOffset.Parse("2026-09-03T00:00:00Z"));

        var agent = Assert.Single(snapshot.Transcript, item => item.Author == "agent");
        Assert.Equal("Hello examiner", agent.Content);
        Assert.Equal("complete", agent.Status);
        Assert.DoesNotContain(snapshot.Transcript, item => item.Status == "unavailable" && item.Author == "agent");
        Assert.Equal("idle", snapshot.ActivityWorkState);
    }

    [Fact]
    public void Hosted_agent_events_carry_current_session_version_and_work_state()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).PublicationPathClaimed);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hello examiner", "agen.proj.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        Assert.True(session.CompleteAgentResponseMessage(
            invocationId,
            SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);

        var replay = HostedSessionEventProjector.Project(session, afterCursor: 0);
        var fragment = Assert.Single(replay.Events, evt => evt.EventType == HostedSessionEventTypes.AgentFragment);
        var complete = Assert.Single(replay.Events, evt => evt.EventType == HostedSessionEventTypes.AgentComplete);

        Assert.Equal(session.SessionVersion, fragment.SessionVersion);
        Assert.Equal(session.SessionVersion, complete.SessionVersion);
        Assert.Equal("working", fragment.WorkState);
        Assert.Equal("idle", complete.WorkState);
    }

    [Fact]
    public void Hosted_event_projector_remaps_fragments_and_adds_terminal_cutoff()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var replay = HostedSessionEventProjector.Project(session, afterCursor: 0);

        Assert.True(replay.Succeeded);
        Assert.DoesNotContain(
            replay.Events,
            evt => evt.EventType is AuthorizedSessionEventTypes.AgentFragment
                or AuthorizedSessionEventTypes.AgentComplete);
        Assert.All(
            replay.Events.Where(evt => evt.EventType.Contains("agent", StringComparison.Ordinal)),
            evt => Assert.StartsWith("session.hosted.", evt.EventType, StringComparison.Ordinal));
    }

    [Fact]
    public void Hosted_replay_emits_message_accepted_queued_work_and_no_action()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).Succeeded);

        var replay = HostedSessionEventProjector.Project(session, afterCursor: 0);

        Assert.Contains(replay.Events, evt => evt.EventType == HostedSessionEventTypes.MessageAccepted);
        Assert.Contains(
            replay.Events,
            evt => evt.EventType == HostedSessionEventTypes.AgentWork && evt.WorkState == "queued");
        var noAction = Assert.Single(replay.Events, evt => evt.EventType == HostedSessionEventTypes.AgentNoAction);
        Assert.Equal("no_action", noAction.WorkState);
        Assert.Equal("no_action", noAction.ResolutionCategory);
        Assert.True(noAction.SessionVersion > 0);
    }

    [Fact]
    public void Hosted_replay_after_cursor_omits_resolved_no_action()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);
        Assert.True(session.CompleteInvocation(
            admitted.Invocation!.AgentInvocationId,
            SessionRuntimeTestFixtures.NoAction(admitted.Invocation.AgentInvocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).Succeeded);

        var noActionCursor = HostedStreamCursors.Parse(
            Assert.Single(
                HostedSessionEventProjector.Project(session, afterCursor: 0).Events,
                evt => evt.EventType == HostedSessionEventTypes.AgentNoAction).StreamCursor);
        var replay = HostedSessionEventProjector.Project(session, afterCursor: noActionCursor);

        Assert.DoesNotContain(replay.Events, evt => evt.EventType == HostedSessionEventTypes.AgentNoAction);
        Assert.True(HostedSessionEventProjector.IsIssuedStreamCursor(session, noActionCursor));
    }

    [Fact]
    public void Snapshot_preserves_transcript_occurred_at_after_later_session_mutation()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var t1 = SessionRuntimeTestFixtures.T0;
        var t2 = t1.AddSeconds(2);
        var t3 = t1.AddSeconds(3);
        var t4 = t1.AddSeconds(4);
        var t5 = t1.AddSeconds(30);

        var first = SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            t1);
        var invocationId = first.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            t2).PublicationPathClaimed);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hello examiner", "agen.proj.1"),
            t3).Succeeded);
        Assert.True(session.CompleteAgentResponseMessage(invocationId, t4).Succeeded);

        SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.2",
            "turn.2",
            "slot.2",
            "trig.participant.2",
            "idem.p.2",
            t5);

        var snapshot = HostedSessionSnapshotProjector.Project(
            session,
            HostedSessionProjectionKinds.Participant,
            t5);

        var participant = Assert.Single(
            snapshot.Transcript,
            item => item.Author == "participant"
                && item.TurnId == HostedSessionSnapshotProjector.ToStableId("turn.1", "turn"));
        var agent = Assert.Single(snapshot.Transcript, item => item.Author == "agent");

        Assert.Equal(HostedSessionSnapshotProjector.FormatUtc(t1), participant.OccurredAt);
        Assert.Equal(HostedSessionSnapshotProjector.FormatUtc(t3), agent.OccurredAt);
        Assert.NotEqual(participant.OccurredAt, agent.OccurredAt);
        Assert.NotEqual(HostedSessionSnapshotProjector.FormatUtc(t5), participant.OccurredAt);
    }

    [Fact]
    public void Hosted_agent_complete_carries_terminal_item_status()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).PublicationPathClaimed);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        Assert.True(session.MarkAgentResponseIncomplete(
            invocationId,
            SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);

        var replay = HostedSessionEventProjector.Project(session, afterCursor: 0);
        var terminal = Assert.Single(replay.Events, evt => evt.EventType == HostedSessionEventTypes.AgentComplete);
        Assert.Equal("incomplete", terminal.ItemStatus);

        var snapshot = HostedSessionSnapshotProjector.Project(
            session,
            HostedSessionProjectionKinds.Participant,
            SessionRuntimeTestFixtures.T0.AddSeconds(5));
        var agent = Assert.Single(snapshot.Transcript, item => item.Author == "agent");
        Assert.Equal("incomplete", agent.Status);
    }

    [Fact]
    public void Issued_stream_cursor_accepts_only_hosted_emitted_cursors()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        Assert.False(HostedSessionEventProjector.IsIssuedStreamCursor(session, 999_999));

        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).PublicationPathClaimed);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hello examiner", "agen.proj.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);

        var fragmentCursor = HostedStreamCursors.Encode(
            session.AgentMessages[0].Fragments[0].SessionSequence,
            HostedStreamCursors.SlotFragment);
        Assert.True(HostedSessionEventProjector.IsIssuedStreamCursor(session, fragmentCursor));
        Assert.False(HostedSessionEventProjector.IsIssuedStreamCursor(session, fragmentCursor + 10_000));
    }

    [Fact]
    public void Same_session_sequence_emits_distinct_hosted_stream_cursors()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);

        var replay = HostedSessionEventProjector.Project(session, afterCursor: 0);
        var queued = Assert.Single(
            replay.Events,
            evt => evt.EventType == HostedSessionEventTypes.AgentWork && evt.WorkState == "queued");
        var accepted = Assert.Single(replay.Events, evt => evt.EventType == HostedSessionEventTypes.MessageAccepted);

        Assert.Equal(queued.SessionSequence, accepted.SessionSequence);
        Assert.NotEqual(queued.StreamCursor, accepted.StreamCursor);
        Assert.True(HostedStreamCursors.Parse(queued.StreamCursor) < HostedStreamCursors.Parse(accepted.StreamCursor));

        var afterQueued = HostedSessionEventProjector.Project(
            session,
            afterCursor: HostedStreamCursors.Parse(queued.StreamCursor));
        Assert.Contains(afterQueued.Events, evt => evt.EventType == HostedSessionEventTypes.MessageAccepted);
        Assert.DoesNotContain(
            afterQueued.Events,
            evt => evt.EventType == HostedSessionEventTypes.AgentWork && evt.WorkState == "queued");
    }

    [Fact]
    public void Issued_working_cursor_stays_valid_after_first_fragment()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(
            session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).PublicationPathClaimed);

        var working = Assert.Single(
            HostedSessionEventProjector.Project(session, afterCursor: 0).Events,
            evt => evt.EventType == HostedSessionEventTypes.AgentWork && evt.WorkState == "working");
        var workingCursor = HostedStreamCursors.Parse(working.StreamCursor);
        Assert.True(HostedSessionEventProjector.IsIssuedStreamCursor(session, workingCursor));

        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hello examiner", "agen.proj.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);

        Assert.True(HostedSessionEventProjector.IsIssuedStreamCursor(session, workingCursor));
        var replay = HostedSessionEventProjector.Project(session, afterCursor: workingCursor);
        Assert.Contains(replay.Events, evt => evt.EventType == HostedSessionEventTypes.AgentFragment);
        Assert.DoesNotContain(
            replay.Events,
            evt => evt.EventType == HostedSessionEventTypes.AgentWork && evt.WorkState == "working");
    }

    [Fact]
    public void Snapshot_omits_transcript_occurred_at_when_item_metadata_is_missing()
    {
        var later = SessionRuntimeTestFixtures.T0.AddHours(1);
        var session = SessionRuntime.Rehydrate(
            SessionRuntimeTestFixtures.CreateBinding(),
            SessionLifecycleState.Active,
            sessionVersion: 4,
            sessionSequence: 20,
            cutoffSequence: null,
            lastCommittedAt: later,
            transcript:
            [
                new VisibleTranscriptItemRef(
                    "msg.orphan",
                    TranscriptAuthorTypes.Participant,
                    "turn.1",
                    new ProtectedContentRef(
                        "msg.orphan",
                        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                    "orphan participant"),
                new VisibleTranscriptItemRef(
                    "msg.agent.orphan",
                    TranscriptAuthorTypes.Agent,
                    "turn.1",
                    new ProtectedContentRef(
                        "msg.agent.orphan",
                        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                    "orphan agent"),
            ]);

        var snapshot = HostedSessionSnapshotProjector.Project(
            session,
            HostedSessionProjectionKinds.Participant,
            later);
        var mutationTime = HostedSessionSnapshotProjector.FormatUtc(later);

        Assert.All(snapshot.Transcript, item => Assert.Null(item.OccurredAt));
        Assert.DoesNotContain(snapshot.Transcript, item => item.OccurredAt == mutationTime);
    }
}
