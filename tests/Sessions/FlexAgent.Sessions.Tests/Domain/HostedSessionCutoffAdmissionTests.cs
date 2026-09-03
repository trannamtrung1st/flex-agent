using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class HostedSessionCutoffAdmissionTests
{
    [Fact]
    public void Live_remaining_zero_expires_and_rejects_non_reconcile_commands()
    {
        Assert.True(HostedSessionCutoffAdmission.ShouldExpireLiveSession("active", 0));
        Assert.True(HostedSessionCutoffAdmission.ShouldExpireLiveSession("paused", 0));
        Assert.True(HostedSessionCutoffAdmission.ShouldRejectCommand("session.message.send.v1", "active", 0));
        Assert.True(HostedSessionCutoffAdmission.ShouldRejectCommand("session.complete.v1", "paused", 0));
        Assert.False(HostedSessionCutoffAdmission.ShouldRejectCommand("session.reconcile.v1", "active", 0));
    }

    [Fact]
    public void Completing_with_projected_remaining_zero_can_still_seal()
    {
        Assert.False(HostedSessionCutoffAdmission.ShouldExpireLiveSession("completing", 0));
        Assert.False(HostedSessionCutoffAdmission.ShouldRejectCommand("session.complete.v1", "completing", 0));
    }

    [Fact]
    public void Unavailable_or_remaining_time_does_not_trip_cutoff()
    {
        Assert.False(HostedSessionCutoffAdmission.ShouldExpireLiveSession("active", null));
        Assert.False(HostedSessionCutoffAdmission.ShouldExpireLiveSession("active", 12));
        Assert.False(HostedSessionCutoffAdmission.ShouldRejectCommand("session.message.send.v1", "active", null));
    }
}
