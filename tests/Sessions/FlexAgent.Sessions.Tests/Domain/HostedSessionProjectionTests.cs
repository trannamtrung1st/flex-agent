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
