using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class HostedSessionCommandAdmissionTests
{
    [Fact]
    public void Participant_cannot_pause_even_when_grant_would_permit()
    {
        var permitted = SessionPermittedActionsProjector.Project(
            HostedSessionProjectionKinds.Participant,
            SessionLifecycleState.Active);

        Assert.False(HostedSessionCommandAdmission.IsPermitted(
            "session.pause.v1",
            HostedSessionProjectionKinds.Participant,
            SessionEventSubscriptionRelationships.Participant,
            permitted));
    }

    [Fact]
    public void Administrator_pause_requires_administrator_relationship()
    {
        var permitted = SessionPermittedActionsProjector.Project(
            HostedSessionProjectionKinds.Administrator,
            SessionLifecycleState.Active);

        Assert.False(HostedSessionCommandAdmission.IsPermitted(
            "session.pause.v1",
            HostedSessionProjectionKinds.Administrator,
            SessionEventSubscriptionRelationships.Participant,
            permitted));

        Assert.True(HostedSessionCommandAdmission.IsPermitted(
            "session.pause.v1",
            HostedSessionProjectionKinds.Administrator,
            SessionEventSubscriptionRelationships.Administrator,
            permitted));
    }

    [Fact]
    public void Participant_send_requires_send_message_in_snapshot()
    {
        Assert.True(HostedSessionCommandAdmission.IsPermitted(
            "session.message.send.v1",
            HostedSessionProjectionKinds.Participant,
            SessionEventSubscriptionRelationships.Participant,
            [HostedSessionPermittedActions.SendMessage]));

        Assert.False(HostedSessionCommandAdmission.IsPermitted(
            "session.message.send.v1",
            HostedSessionProjectionKinds.Participant,
            SessionEventSubscriptionRelationships.Participant,
            [HostedSessionPermittedActions.Reconcile]));
    }
}
