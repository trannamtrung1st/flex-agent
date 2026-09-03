using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class HostedSessionLifecycleSequenceTests
{
    [Fact]
    public void Complete_enters_completing_then_seals_completed()
    {
        Assert.Equal(
            [
                SessionLifecycleTransitions.BeginCompleting,
                SessionLifecycleTransitions.Complete,
            ],
            HostedSessionLifecycleSequence.Transitions("session.complete.v1"));
    }

    [Fact]
    public void Terminate_enters_completing_then_seals_terminated()
    {
        Assert.Equal(
            [
                SessionLifecycleTransitions.BeginCompleting,
                SessionLifecycleTransitions.Terminate,
            ],
            HostedSessionLifecycleSequence.Transitions("session.terminate.v1"));
    }
}
