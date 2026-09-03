using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public static class SessionRuntimeLifecycleAudit
{
    public static string Seed(
        string transition,
        SessionLifecycleState lifecycle,
        long sessionVersion,
        string? reasonCode)
    {
        var reason = string.IsNullOrWhiteSpace(reasonCode) ? "none" : reasonCode.Trim();
        return $"{transition}:{lifecycle.ToString().ToLowerInvariant()}:{sessionVersion}:{reason}";
    }
}
