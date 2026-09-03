namespace FlexAgent.Sessions.Domain;

public static class HostedSessionTiming
{
    public const int SyntheticDevelopmentActiveDurationSeconds = 45 * 60;

    public static int ResolveBudget(int? frozenPerAttemptDurationSeconds) =>
        frozenPerAttemptDurationSeconds is > 0
            ? frozenPerAttemptDurationSeconds.Value
            : SyntheticDevelopmentActiveDurationSeconds;

    public static HostedTimingProjection Project(
        SessionLifecycleState lifecycle,
        DateTimeOffset startedAt,
        DateTimeOffset lastCommittedAt,
        DateTimeOffset authoritativeUtc,
        int budgetSeconds,
        int accumulatedPausedSeconds = 0,
        DateTimeOffset? openPauseStartedAt = null,
        IReadOnlyList<HostedTimingWarningThreshold>? warningSchedule = null)
    {
        if (budgetSeconds <= 0)
        {
            return new HostedTimingProjection("disabled", null, "none", null, null);
        }

        var pauseAnchor = openPauseStartedAt ?? lastCommittedAt;
        var endOfActive = lifecycle is SessionLifecycleState.Active
            ? authoritativeUtc
            : pauseAnchor < startedAt ? startedAt : pauseAnchor;
        if (endOfActive < startedAt)
        {
            endOfActive = startedAt;
        }

        var wall = Math.Max(0, (int)(endOfActive - startedAt).TotalSeconds);
        var paused = Math.Clamp(accumulatedPausedSeconds, 0, wall);
        var elapsed = (int)Math.Clamp(wall - paused, 0, budgetSeconds);
        var remaining = SessionPermittedActionsProjector.IsTerminal(lifecycle)
            || lifecycle == SessionLifecycleState.Completing
            ? 0
            : budgetSeconds - elapsed;
        var pauseStarted = lifecycle == SessionLifecycleState.Paused
            ? HostedSessionSnapshotProjector.FormatUtc(pauseAnchor)
            : null;
        return new HostedTimingProjection(
            "active_duration",
            remaining,
            ResolveWarningCode(remaining, warningSchedule),
            pauseStarted,
            budgetSeconds);
    }

    private static string ResolveWarningCode(
        int remainingSeconds,
        IReadOnlyList<HostedTimingWarningThreshold>? warningSchedule)
    {
        if (warningSchedule is not { Count: > 0 })
        {
            return "none";
        }

        var due = warningSchedule
            .Where(item => item.RemainingSecondsThreshold > 0
                && remainingSeconds <= item.RemainingSecondsThreshold)
            .ToArray();
        if (due.Length == 0)
        {
            return "none";
        }

        if (due.Any(item => string.Equals(item.Code, "imminent", StringComparison.Ordinal)))
        {
            return "imminent";
        }

        if (due.Any(item => string.Equals(item.Code, "approaching", StringComparison.Ordinal)))
        {
            return "approaching";
        }

        return due[0].Code;
    }
}

public sealed record HostedTimingWarningThreshold(string Code, int RemainingSecondsThreshold);

public sealed record HostedTimingProjection(
    string Policy,
    int? RemainingSeconds,
    string WarningCode,
    string? PauseStartedAt,
    int? BudgetSeconds);
