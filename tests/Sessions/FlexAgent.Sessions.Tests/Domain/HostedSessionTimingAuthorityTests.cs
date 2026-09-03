using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class HostedSessionTimingAuthorityTests
{
    [Fact]
    public void Unavailable_timing_blocks_send_and_resume_only()
    {
        Assert.True(HostedSessionTimingAuthority.ShouldRejectCommand(
            "session.message.send.v1",
            "active",
            "unavailable",
            null));
        Assert.True(HostedSessionTimingAuthority.ShouldRejectCommand(
            "session.resume.v1",
            "paused",
            "unavailable",
            null));
        Assert.False(HostedSessionTimingAuthority.ShouldRejectCommand(
            "session.reconcile.v1",
            "active",
            "unavailable",
            null));
        Assert.False(HostedSessionTimingAuthority.ShouldRejectCommand(
            "session.pause.v1",
            "active",
            "unavailable",
            null));
    }
}
