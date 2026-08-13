using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class SessionRuntimeRehydrateTests
{
    [Fact]
    public void Rehydrate_restores_version_sequence_and_admitted_invocation()
    {
        var binding = SessionRuntimeTestFixtures.CreateBinding();
        var trigger = SessionRuntimeTestFixtures.OpeningTrigger();
        var invocation = AgentInvocation.Rehydrate(
            "inv-1",
            binding.Ownership,
            trigger,
            "idem-1",
            binding.Policy.PolicyDigest,
            sessionSequence: 4,
            AgentInvocationStatuses.Admitted);
        var committedAt = SessionRuntimeTestFixtures.T0.AddMinutes(3);

        var session = SessionRuntime.Rehydrate(
            binding,
            SessionLifecycleState.Active,
            sessionVersion: 7,
            sessionSequence: 4,
            cutoffSequence: null,
            committedAt,
            [invocation],
            lastAdmittedAtByFamily: new Dictionary<string, DateTimeOffset>
            {
                [trigger.TriggerFamily] = committedAt,
            });

        Assert.Equal(7, session.SessionVersion);
        Assert.Equal(4, session.SessionSequence);
        Assert.Equal(committedAt, session.LastCommittedAt);
        var loaded = Assert.Single(session.Invocations);
        Assert.Equal("inv-1", loaded.AgentInvocationId);
        Assert.Equal(4, loaded.SessionSequence);
        Assert.Equal(AgentInvocationStatuses.Admitted, loaded.Status);
    }
}
