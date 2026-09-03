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

    [Fact]
    public void Active_session_after_resume_excludes_closed_pause_intervals()
    {
        var started = DateTimeOffset.Parse("2026-09-03T00:00:00Z");
        var resumedAt = started.AddMinutes(28);
        var now = started.AddMinutes(30);

        var timing = HostedSessionTiming.Project(
            SessionLifecycleState.Active,
            started,
            resumedAt,
            now,
            HostedSessionTiming.SyntheticDevelopmentActiveDurationSeconds,
            accumulatedPausedSeconds: 20 * 60);

        Assert.Equal(35 * 60, timing.RemainingSeconds);
        Assert.Null(timing.PauseStartedAt);
    }

    [Fact]
    public void Frozen_activity_duration_is_the_budget_when_present()
    {
        var started = DateTimeOffset.Parse("2026-09-03T00:00:00Z");
        var now = started.AddMinutes(10);

        var timing = HostedSessionTiming.Project(
            SessionLifecycleState.Active,
            started,
            now,
            now,
            budgetSeconds: 3600);

        Assert.Equal(3000, timing.RemainingSeconds);
        Assert.Equal(3600, timing.BudgetSeconds);
    }

    [Fact]
    public void Configured_warning_schedule_projects_the_most_severe_due_threshold()
    {
        var started = DateTimeOffset.Parse("2026-09-03T00:00:00Z");
        var now = started.AddMinutes(40);

        var timing = HostedSessionTiming.Project(
            SessionLifecycleState.Active,
            started,
            now,
            now,
            budgetSeconds: 45 * 60,
            warningSchedule:
            [
                new HostedTimingWarningThreshold("approaching", 15 * 60),
                new HostedTimingWarningThreshold("imminent", 10 * 60),
            ]);

        Assert.Equal(5 * 60, timing.RemainingSeconds);
        Assert.Equal("imminent", timing.WarningCode);
    }

    [Fact]
    public void Absent_warning_schedule_does_not_invent_universal_thresholds()
    {
        var started = DateTimeOffset.Parse("2026-09-03T00:00:00Z");
        var now = started.AddMinutes(44);

        var timing = HostedSessionTiming.Project(
            SessionLifecycleState.Active,
            started,
            now,
            now,
            budgetSeconds: 45 * 60);

        Assert.Equal(60, timing.RemainingSeconds);
        Assert.Equal("none", timing.WarningCode);
    }
}
