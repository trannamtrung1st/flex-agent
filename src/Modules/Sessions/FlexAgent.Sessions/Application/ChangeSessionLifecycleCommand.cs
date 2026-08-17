using System.Diagnostics;
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

public sealed class ChangeSessionLifecycleHandler(ISessionRuntimeTelemetry? telemetry = null)
    : IChangeSessionLifecycleHandler
{
    private readonly ISessionRuntimeTelemetry _telemetry = telemetry ?? NoopSessionRuntimeTelemetry.Instance;

    public SessionLifecycleChangeResult Handle(
        ChangeSessionLifecycleCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        var started = Stopwatch.GetTimestamp();

        SessionLifecycleChangeResult result;
        if (command.Actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Actor.ActorType))
        {
            result = Failure(SessionLifecycleOutcomeCodes.Denied, session);
        }
        else if (command.Ownership != session.Ownership)
        {
            result = Failure(SessionLifecycleOutcomeCodes.OwnershipMismatch, session);
        }
        else if (command.ExpectedSessionVersion != session.SessionVersion)
        {
            result = command.Transition == SessionLifecycleTransitions.Resume
                || !AlreadyApplied(command.Transition, session.LifecycleState)
                ? Failure(SessionLifecycleOutcomeCodes.StaleVersion, session)
                : Success(SessionLifecycleOutcomeCodes.Reconciled, session);
        }
        else if (AlreadyApplied(command.Transition, session.LifecycleState)
            && command.Transition != SessionLifecycleTransitions.Resume)
        {
            result = Success(SessionLifecycleOutcomeCodes.Reconciled, session);
        }
        else
        {
            switch (command.Transition)
            {
                case SessionLifecycleTransitions.Pause:
                    if (session.LifecycleState != SessionLifecycleState.Active)
                    {
                        result = Failure(SessionLifecycleOutcomeCodes.LifecycleIneligible, session);
                        break;
                    }

                    session.Pause(authoritativeUtc);
                    result = Success(SessionLifecycleOutcomeCodes.Succeeded, session);
                    break;
                case SessionLifecycleTransitions.Resume:
                    if (session.LifecycleState != SessionLifecycleState.Paused)
                    {
                        result = Failure(SessionLifecycleOutcomeCodes.LifecycleIneligible, session);
                        break;
                    }

                    session.Resume(authoritativeUtc);
                    result = Success(SessionLifecycleOutcomeCodes.Succeeded, session);
                    break;
                case SessionLifecycleTransitions.BeginCompleting:
                    if (session.LifecycleState is not (SessionLifecycleState.Active or SessionLifecycleState.Paused))
                    {
                        result = Failure(SessionLifecycleOutcomeCodes.LifecycleIneligible, session);
                        break;
                    }

                    session.BeginCompleting(authoritativeUtc);
                    result = Success(SessionLifecycleOutcomeCodes.Succeeded, session);
                    break;
                case SessionLifecycleTransitions.Complete:
                    if (session.LifecycleState != SessionLifecycleState.Completing)
                    {
                        result = Failure(SessionLifecycleOutcomeCodes.LifecycleIneligible, session);
                        break;
                    }

                    session.Complete(authoritativeUtc);
                    result = Success(SessionLifecycleOutcomeCodes.Succeeded, session);
                    break;
                case SessionLifecycleTransitions.Terminate:
                    if (session.LifecycleState != SessionLifecycleState.Completing)
                    {
                        result = Failure(SessionLifecycleOutcomeCodes.LifecycleIneligible, session);
                        break;
                    }

                    session.Terminate(authoritativeUtc);
                    result = Success(SessionLifecycleOutcomeCodes.Succeeded, session);
                    break;
                case SessionLifecycleTransitions.Abort:
                    if (session.LifecycleState is SessionLifecycleState.Completed
                        or SessionLifecycleState.Terminated
                        or SessionLifecycleState.Aborted)
                    {
                        result = Success(SessionLifecycleOutcomeCodes.Reconciled, session);
                        break;
                    }

                    session.Abort(authoritativeUtc);
                    result = Success(SessionLifecycleOutcomeCodes.Succeeded, session);
                    break;
                default:
                    result = Failure(SessionLifecycleOutcomeCodes.Denied, session);
                    break;
            }
        }

        var labels = SessionRuntimeTelemetryRecording.Labels(
            (SessionRuntimeTelemetryLabelKeys.Outcome, result.OutcomeCode),
            (SessionRuntimeTelemetryLabelKeys.Transition, command.Transition));
        _telemetry.RecordCounter(SessionRuntimeTelemetryInstruments.LifecycleChange, labels);
        _telemetry.RecordDuration(
            SessionRuntimeTelemetryInstruments.LifecycleChange,
            Stopwatch.GetElapsedTime(started),
            labels);
        return result;
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
