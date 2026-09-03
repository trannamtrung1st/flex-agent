namespace FlexAgent.Sessions.Domain;

public static class HostedSessionTimingAdmission
{
    public static bool IsCutoffPassed(
        SessionLifecycleState lifecycle,
        DateTimeOffset startedAt,
        DateTimeOffset lastCommittedAt,
        DateTimeOffset authoritativeUtc,
        HostedFrozenTimingPolicy policy,
        int accumulatedPausedSeconds = 0,
        DateTimeOffset? openPauseStartedAt = null)
    {
        var timing = HostedSessionTiming.Project(
            lifecycle,
            startedAt,
            lastCommittedAt,
            authoritativeUtc,
            policy,
            accumulatedPausedSeconds,
            openPauseStartedAt);
        return HostedSessionCutoffAdmission.ShouldExpireLiveSession(
            MapLifecycle(lifecycle),
            timing.RemainingSeconds);
    }

    private static string MapLifecycle(SessionLifecycleState lifecycle) =>
        lifecycle switch
        {
            SessionLifecycleState.Paused => "paused",
            SessionLifecycleState.Active => "active",
            SessionLifecycleState.Completing => "completing",
            SessionLifecycleState.Completed => "completed",
            SessionLifecycleState.Terminated => "terminated",
            SessionLifecycleState.Aborted => "aborted",
            _ => lifecycle.ToString().ToLowerInvariant(),
        };
}
