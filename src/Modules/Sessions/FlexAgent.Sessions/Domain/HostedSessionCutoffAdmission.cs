namespace FlexAgent.Sessions.Domain;

public static class HostedSessionCutoffAdmission
{
    public static bool ShouldExpireLiveSession(string lifecycleState, int? remainingSeconds) =>
        remainingSeconds == 0
        && (lifecycleState is "active" or "paused");

    public static bool ShouldRejectCommand(string commandType, string lifecycleState, int? remainingSeconds) =>
        commandType != "session.reconcile.v1"
        && ShouldExpireLiveSession(lifecycleState, remainingSeconds);
}
