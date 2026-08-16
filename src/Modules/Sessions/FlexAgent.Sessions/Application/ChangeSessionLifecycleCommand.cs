using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record ChangeSessionLifecycleCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    long ExpectedSessionVersion,
    string Transition,
    Guid CorrelationId,
    string SourceChannel);

public interface IChangeSessionLifecycleHandler
{
    SessionLifecycleChangeResult Handle(
        ChangeSessionLifecycleCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc);
}

public sealed class ChangeSessionLifecycleHandler : IChangeSessionLifecycleHandler
{
    public SessionLifecycleChangeResult Handle(
        ChangeSessionLifecycleCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);

        if (command.Actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Actor.ActorType))
        {
            return Failure(SessionLifecycleOutcomeCodes.Denied, session);
        }

        if (command.Ownership != session.Ownership)
        {
            return Failure(SessionLifecycleOutcomeCodes.OwnershipMismatch, session);
        }

        if (command.ExpectedSessionVersion != session.SessionVersion)
        {
            if (command.Transition == SessionLifecycleTransitions.Resume
                || !AlreadyApplied(command.Transition, session.LifecycleState))
            {
                return Failure(SessionLifecycleOutcomeCodes.StaleVersion, session);
            }

            return Success(SessionLifecycleOutcomeCodes.Reconciled, session);
        }

        if (AlreadyApplied(command.Transition, session.LifecycleState)
            && command.Transition != SessionLifecycleTransitions.Resume)
        {
            return Success(SessionLifecycleOutcomeCodes.Reconciled, session);
        }

        switch (command.Transition)
        {
            case SessionLifecycleTransitions.Pause:
                if (session.LifecycleState != SessionLifecycleState.Active)
                {
                    return Failure(SessionLifecycleOutcomeCodes.LifecycleIneligible, session);
                }

                session.Pause(authoritativeUtc);
                break;
            case SessionLifecycleTransitions.Resume:
                if (session.LifecycleState != SessionLifecycleState.Paused)
                {
                    return Failure(SessionLifecycleOutcomeCodes.LifecycleIneligible, session);
                }

                session.Resume(authoritativeUtc);
                break;
            case SessionLifecycleTransitions.BeginCompleting:
                if (session.LifecycleState is not (SessionLifecycleState.Active or SessionLifecycleState.Paused))
                {
                    return Failure(SessionLifecycleOutcomeCodes.LifecycleIneligible, session);
                }

                session.BeginCompleting(authoritativeUtc);
                break;
            case SessionLifecycleTransitions.Complete:
                if (session.LifecycleState != SessionLifecycleState.Completing)
                {
                    return Failure(SessionLifecycleOutcomeCodes.LifecycleIneligible, session);
                }

                session.Complete(authoritativeUtc);
                break;
            case SessionLifecycleTransitions.Terminate:
                if (session.LifecycleState != SessionLifecycleState.Completing)
                {
                    return Failure(SessionLifecycleOutcomeCodes.LifecycleIneligible, session);
                }

                session.Terminate(authoritativeUtc);
                break;
            case SessionLifecycleTransitions.Abort:
                if (session.LifecycleState is SessionLifecycleState.Completed
                    or SessionLifecycleState.Terminated
                    or SessionLifecycleState.Aborted)
                {
                    return Success(SessionLifecycleOutcomeCodes.Reconciled, session);
                }

                session.Abort(authoritativeUtc);
                break;
            default:
                return Failure(SessionLifecycleOutcomeCodes.Denied, session);
        }

        return Success(SessionLifecycleOutcomeCodes.Succeeded, session);
    }

    private static bool AlreadyApplied(string transition, SessionLifecycleState state) =>
        transition switch
        {
            SessionLifecycleTransitions.Pause => state == SessionLifecycleState.Paused,
            SessionLifecycleTransitions.Resume => state == SessionLifecycleState.Active,
            SessionLifecycleTransitions.BeginCompleting => state is SessionLifecycleState.Completing
                or SessionLifecycleState.Completed
                or SessionLifecycleState.Terminated
                or SessionLifecycleState.Aborted,
            SessionLifecycleTransitions.Complete => state == SessionLifecycleState.Completed,
            SessionLifecycleTransitions.Terminate => state == SessionLifecycleState.Terminated,
            SessionLifecycleTransitions.Abort => state is SessionLifecycleState.Aborted
                or SessionLifecycleState.Completed
                or SessionLifecycleState.Terminated,
            _ => false,
        };

    private static SessionLifecycleChangeResult Success(string outcome, SessionRuntime session) =>
        new(true, outcome, session.LifecycleState, session.SessionVersion);

    private static SessionLifecycleChangeResult Failure(string outcome, SessionRuntime session) =>
        new(false, outcome, session.LifecycleState, session.SessionVersion);
}
