namespace FlexAgent.Sessions.Domain;

public static class HostedSessionTimingAuthority
{
    public static bool IsUnavailable(string? timingPolicy) =>
        string.Equals(timingPolicy, "unavailable", StringComparison.Ordinal);

    public static bool ShouldRejectCommand(string commandType, string lifecycleState, string? timingPolicy, int? remainingSeconds)
    {
        if (IsUnavailable(timingPolicy))
        {
            return commandType is "session.message.send.v1" or "session.resume.v1";
        }

        return HostedSessionCutoffAdmission.ShouldRejectCommand(commandType, lifecycleState, remainingSeconds);
    }
}
