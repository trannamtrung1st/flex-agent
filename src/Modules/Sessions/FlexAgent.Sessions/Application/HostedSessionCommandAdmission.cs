using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public static class HostedSessionCommandAdmission
{
    public static bool IsPermitted(
        string commandType,
        string projectionKind,
        string relationship,
        IReadOnlyList<string> permittedActions)
    {
        if (!TryGetRequirements(commandType, out var requiredAction, out var requiredProjection))
        {
            return false;
        }

        if (requiredProjection is not null
            && !string.Equals(projectionKind, requiredProjection, StringComparison.Ordinal))
        {
            return false;
        }

        if (requiredProjection == HostedSessionProjectionKinds.Administrator
            && !string.Equals(
                relationship,
                SessionEventSubscriptionRelationships.Administrator,
                StringComparison.Ordinal))
        {
            return false;
        }

        return permittedActions.Contains(requiredAction);
    }

    private static bool TryGetRequirements(
        string commandType,
        out string requiredAction,
        out string? requiredProjection)
    {
        switch (commandType)
        {
            case "session.message.send.v1":
                requiredAction = HostedSessionPermittedActions.SendMessage;
                requiredProjection = HostedSessionProjectionKinds.Participant;
                return true;
            case "session.complete.v1":
                requiredAction = HostedSessionPermittedActions.CompleteSession;
                requiredProjection = HostedSessionProjectionKinds.Participant;
                return true;
            case "session.pause.v1":
                requiredAction = HostedSessionPermittedActions.PauseSession;
                requiredProjection = HostedSessionProjectionKinds.Administrator;
                return true;
            case "session.resume.v1":
                requiredAction = HostedSessionPermittedActions.ResumeSession;
                requiredProjection = HostedSessionProjectionKinds.Administrator;
                return true;
            case "session.terminate.v1":
                requiredAction = HostedSessionPermittedActions.TerminateSession;
                requiredProjection = HostedSessionProjectionKinds.Administrator;
                return true;
            case "session.reconcile.v1":
                requiredAction = HostedSessionPermittedActions.Reconcile;
                requiredProjection = null;
                return true;
            default:
                requiredAction = string.Empty;
                requiredProjection = null;
                return false;
        }
    }
}
