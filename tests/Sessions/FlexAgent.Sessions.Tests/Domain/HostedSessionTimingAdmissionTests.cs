using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class HostedSessionTimingAdmissionTests
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.Parse("2026-09-03T10:00:00Z");
    private static readonly DateTimeOffset ObservedAt = DateTimeOffset.Parse("2026-09-03T10:30:00Z");

    [Fact]
    public void Unavailable_policy_fails_closed_for_admission()
    {
        var verdict = HostedSessionTimingAdmission.Evaluate(
            SessionLifecycleState.Active,
            StartedAt,
            StartedAt,
            ObservedAt,
            HostedFrozenTimingPolicy.UnavailablePolicy);

        Assert.Equal(HostedSessionTimingAdmissionVerdict.TimingUnavailable, verdict);
        Assert.False(HostedSessionTimingAdmission.IsCutoffPassed(
            SessionLifecycleState.Active,
            StartedAt,
            StartedAt,
            ObservedAt,
            HostedFrozenTimingPolicy.UnavailablePolicy));
    }

    [Fact]
    public void Timed_policy_with_remaining_time_allows_admission()
    {
        var policy = new HostedFrozenTimingPolicy(
            HostedTimingReconstruction.Timed,
            3600,
            [
                new HostedTimingWarningThreshold("approaching", 900),
                new HostedTimingWarningThreshold("imminent", 300),
            ]);

        var verdict = HostedSessionTimingAdmission.Evaluate(
            SessionLifecycleState.Active,
            StartedAt,
            StartedAt,
            ObservedAt,
            policy);

        Assert.Equal(HostedSessionTimingAdmissionVerdict.Allowed, verdict);
    }

    [Fact]
    public void Timed_policy_at_cutoff_rejects_admission()
    {
        var policy = new HostedFrozenTimingPolicy(
            HostedTimingReconstruction.Timed,
            1800,
            [
                new HostedTimingWarningThreshold("approaching", 900),
                new HostedTimingWarningThreshold("imminent", 300),
            ]);

        var verdict = HostedSessionTimingAdmission.Evaluate(
            SessionLifecycleState.Active,
            StartedAt,
            StartedAt,
            StartedAt.AddSeconds(1800),
            policy);

        Assert.Equal(HostedSessionTimingAdmissionVerdict.CutoffPassed, verdict);
    }
}
