namespace FlexAgent.Sessions.Domain;

public enum HostedSessionTimingAdmissionVerdict
{
    Allowed,
    CutoffPassed,
    TimingUnavailable,
}

public static class HostedSessionTimingAdmission
{
    public static HostedSessionTimingAdmissionVerdict Evaluate(
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
            return HostedSessionTimingAdmissionVerdict.TimingUnavailable;
        }

        var timing = HostedSessionTiming.Project(
            lifecycle,
            startedAt,
            lastCommittedAt,
            authoritativeUtc,
            policy,
            accumulatedPausedSeconds,
            openPauseStartedAt);
        if (string.Equals(timing.Policy, "unavailable", StringComparison.Ordinal))
        {
            return HostedSessionTimingAdmissionVerdict.TimingUnavailable;
        }

        return HostedSessionCutoffAdmission.ShouldExpireLiveSession(
                MapLifecycle(lifecycle),
                timing.RemainingSeconds)
            ? HostedSessionTimingAdmissionVerdict.CutoffPassed
            : HostedSessionTimingAdmissionVerdict.Allowed;
    }

    public static bool IsCutoffPassed(
        SessionLifecycleState lifecycle,
        DateTimeOffset startedAt,
        DateTimeOffset lastCommittedAt,
        DateTimeOffset authoritativeUtc,
        HostedFrozenTimingPolicy policy,
        int accumulatedPausedSeconds = 0,
        DateTimeOffset? openPauseStartedAt = null) =>
        Evaluate(
            lifecycle,
            startedAt,
            lastCommittedAt,
            authoritativeUtc,
            policy,
            accumulatedPausedSeconds,
            openPauseStartedAt) == HostedSessionTimingAdmissionVerdict.CutoffPassed;

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
