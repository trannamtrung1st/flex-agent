using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class HostedSessionTimingTests
{
    [Fact]
    public void Active_session_subtracts_elapsed_active_time_from_the_budget()
    {
        var started = DateTimeOffset.Parse("2026-09-03T00:00:00Z");
        var now = started.AddMinutes(10);

        var timing = HostedSessionTiming.Project(
            SessionLifecycleState.Active,
            started,
            now,
            now,
            HostedSessionTiming.SyntheticDevelopmentActiveDurationSeconds);

        Assert.Equal("active_duration", timing.Policy);
        Assert.Equal(35 * 60, timing.RemainingSeconds);
        Assert.Equal("none", timing.WarningCode);
        Assert.Equal(45 * 60, timing.BudgetSeconds);
    }

    [Fact]
    public void Completing_session_reports_zero_remaining()
    {
        var started = DateTimeOffset.Parse("2026-09-03T00:00:00Z");
        var now = started.AddMinutes(3);

        var timing = HostedSessionTiming.Project(
            SessionLifecycleState.Completing,
            started,
            now,
            now.AddMinutes(1),
            HostedSessionTiming.SyntheticDevelopmentActiveDurationSeconds);

        Assert.Equal(0, timing.RemainingSeconds);
    }

    [Fact]
    public void Paused_session_freezes_elapsed_at_last_commit()
    {
        var started = DateTimeOffset.Parse("2026-09-03T00:00:00Z");
        var pausedAt = started.AddMinutes(8);
        var later = pausedAt.AddMinutes(20);

        var timing = HostedSessionTiming.Project(
            SessionLifecycleState.Paused,
            started,
            pausedAt,
            later,
            HostedSessionTiming.SyntheticDevelopmentActiveDurationSeconds);

        Assert.Equal(37 * 60, timing.RemainingSeconds);
        Assert.Equal("2026-09-03T00:08:00Z", timing.PauseStartedAt);
    }
}
