namespace FlexAgent.Sessions.Domain;

public static class HostedSessionTiming
{
    public const int SyntheticDevelopmentActiveDurationSeconds = 45 * 60;

    public static HostedTimingProjection Project(
        SessionLifecycleState lifecycle,
        DateTimeOffset startedAt,
        DateTimeOffset lastCommittedAt,
        DateTimeOffset authoritativeUtc,
        int budgetSeconds)
    {
        if (budgetSeconds <= 0)
        {
            return new HostedTimingProjection("disabled", null, "none", null, null);
        }

        var endOfActive = lifecycle is SessionLifecycleState.Active
            ? authoritativeUtc
            : lastCommittedAt < startedAt ? startedAt : lastCommittedAt;
        if (endOfActive < startedAt)
        {
            endOfActive = startedAt;
        }

        var elapsed = (int)Math.Clamp((endOfActive - startedAt).TotalSeconds, 0, budgetSeconds);
        var remaining = SessionPermittedActionsProjector.IsTerminal(lifecycle)
            || lifecycle == SessionLifecycleState.Completing
            ? 0
            : budgetSeconds - elapsed;
        var pauseStarted = lifecycle == SessionLifecycleState.Paused
            ? HostedSessionSnapshotProjector.FormatUtc(lastCommittedAt)
            : null;
        return new HostedTimingProjection(
            "active_duration",
            remaining,
            "none",
            pauseStarted,
            budgetSeconds);
    }
}

public sealed record HostedTimingProjection(
    string Policy,
    int? RemainingSeconds,
    string WarningCode,
    string? PauseStartedAt,
    int? BudgetSeconds);
