namespace FlexAgent.Sessions.Domain;

public static class HostedSessionTiming
{
    public const int SyntheticDevelopmentActiveDurationSeconds = 45 * 60;

    public static HostedTimingProjection Project(
        SessionLifecycleState lifecycle,
        DateTimeOffset startedAt,
        DateTimeOffset lastCommittedAt,
        DateTimeOffset authoritativeUtc,
        HostedFrozenTimingPolicy policy,
        int accumulatedPausedSeconds = 0,
        DateTimeOffset? openPauseStartedAt = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.Reconstruction == HostedTimingReconstruction.Unavailable)
        {
            return new HostedTimingProjection("unavailable", null, "none", null, null);
        }

        if (policy.Reconstruction == HostedTimingReconstruction.Unbounded
            || policy.BudgetSeconds is not > 0)
        {
            return new HostedTimingProjection("disabled", null, "none", null, null);
        }

        if (policy.WarningSchedule.Count == 0)
        {
            return new HostedTimingProjection("unavailable", null, "none", null, null);
        }

        return Project(
            lifecycle,
            startedAt,
            lastCommittedAt,
            authoritativeUtc,
            policy.BudgetSeconds.Value,
            accumulatedPausedSeconds,
            openPauseStartedAt,
            policy.WarningSchedule,
            policy.HardEndAtUtc);
    }

    public static HostedTimingProjection Project(
        SessionLifecycleState lifecycle,
        DateTimeOffset startedAt,
        DateTimeOffset lastCommittedAt,
        DateTimeOffset authoritativeUtc,
        int budgetSeconds,
        int accumulatedPausedSeconds = 0,
        DateTimeOffset? openPauseStartedAt = null,
        IReadOnlyList<HostedTimingWarningThreshold>? warningSchedule = null,
        DateTimeOffset? hardEndAtUtc = null)
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
        var remaining = ComputeRemainingSeconds(
            lifecycle,
            budgetSeconds,
            elapsed,
            authoritativeUtc,
            hardEndAtUtc);
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

    internal static int ComputeRemainingSeconds(
        SessionLifecycleState lifecycle,
        int budgetSeconds,
        int elapsedSeconds,
        DateTimeOffset authoritativeUtc,
        DateTimeOffset? hardEndAtUtc)
    {
        if (SessionPermittedActionsProjector.IsTerminal(lifecycle)
            || lifecycle == SessionLifecycleState.Completing)
        {
            return 0;
        }

        var budgetRemaining = budgetSeconds - elapsedSeconds;
        if (hardEndAtUtc is null)
        {
            return budgetRemaining;
        }

        var hardRemaining = (int)Math.Max(
            0,
            Math.Ceiling((hardEndAtUtc.Value - authoritativeUtc).TotalSeconds));
        return Math.Min(budgetRemaining, hardRemaining);
    }
}

public sealed record HostedTimingWarningThreshold(string Code, int RemainingSecondsThreshold);

public sealed record HostedTimingProjection(
    string Policy,
    int? RemainingSeconds,
    string WarningCode,
    string? PauseStartedAt,
    int? BudgetSeconds);
