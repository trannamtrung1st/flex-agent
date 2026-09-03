using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class HostedSessionProjectionTests
{
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

        var replay = HostedSessionEventProjector.Project(session, afterSequence: 0);
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
        var replay = HostedSessionEventProjector.Project(session, afterSequence: 0);

        Assert.True(replay.Succeeded);
        Assert.DoesNotContain(
            replay.Events,
            evt => evt.EventType is AuthorizedSessionEventTypes.AgentFragment
                or AuthorizedSessionEventTypes.AgentComplete);
        Assert.All(
            replay.Events.Where(evt => evt.EventType.Contains("agent", StringComparison.Ordinal)),
            evt => Assert.StartsWith("session.hosted.", evt.EventType, StringComparison.Ordinal));
    }
}
