using System.Diagnostics;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record CompleteInvocationCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    long ExpectedSessionVersion,
    string AgentInvocationId,
    DecisionRecommendation? Decision,
    ExecutionFailureCompletion? ExecutionFailure,
    Guid CorrelationId,
    string SourceChannel);

public interface ICompleteInvocationHandler
{
    InvocationCompletionResult Handle(
        CompleteInvocationCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc);
}

public sealed class CompleteInvocationHandler(ISessionRuntimeTelemetry? telemetry = null)
    : ICompleteInvocationHandler
{
    private readonly ISessionRuntimeTelemetry _telemetry = telemetry ?? NoopSessionRuntimeTelemetry.Instance;

    public InvocationCompletionResult Handle(
        CompleteInvocationCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        var started = Stopwatch.GetTimestamp();

        InvocationCompletionResult result;
        if (command.Actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Actor.ActorType))
        {
            result = new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.Denied, null);
        }
        else if (command.Ownership != session.Ownership)
        {
            result = new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.OwnershipMismatch, null);
        }
        else if (command.Decision is not null && command.ExecutionFailure is not null)
        {
            result = new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.IdentityMismatch, null);
        }
        else if (command.Decision is null && command.ExecutionFailure is null)
        {
            result = new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.IdentityMismatch, null);
        }
        else
        {
            var invocation = session.Invocations.FirstOrDefault(item =>
                string.Equals(item.AgentInvocationId, command.AgentInvocationId, StringComparison.Ordinal));
            if (invocation is null)
            {
                result = new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.AlreadyTerminal, null);
            }
            else if (!invocation.IsTerminal && command.ExpectedSessionVersion != session.SessionVersion)
            {
                result = new InvocationCompletionResult(
                    false,
                    InvocationCompletionOutcomeCodes.StaleVersion,
                    invocation);
            }
            else
            {
                result = command.Decision is not null
                    ? session.CompleteInvocation(command.AgentInvocationId, command.Decision, authoritativeUtc)
                    : session.CompleteInvocation(command.AgentInvocationId, command.ExecutionFailure!, authoritativeUtc);
            }
        }

        Record(result, Stopwatch.GetElapsedTime(started));
        return result;
    }

    private void Record(InvocationCompletionResult result, TimeSpan duration)
    {
        var completionLabels = SessionRuntimeTelemetryRecording.Labels(
            (SessionRuntimeTelemetryLabelKeys.Outcome, result.OutcomeCode),
            (SessionRuntimeTelemetryLabelKeys.DecisionType, result.Decision?.DecisionType));
        _telemetry.RecordCounter(SessionRuntimeTelemetryInstruments.InvocationCompletion, completionLabels);
        _telemetry.RecordDuration(
            SessionRuntimeTelemetryInstruments.InvocationCompletion,
            duration,
            completionLabels);
        if (result.ValidationEffect is not null)
        {
            _telemetry.RecordCounter(
                SessionRuntimeTelemetryInstruments.DecisionEffect,
                SessionRuntimeTelemetryRecording.Labels(
                    (SessionRuntimeTelemetryLabelKeys.Outcome, result.ValidationEffect.EffectOutcome)));
            _telemetry.RecordCounter(
                SessionRuntimeTelemetryInstruments.TimerRecommendation,
                SessionRuntimeTelemetryRecording.Labels(
                    (SessionRuntimeTelemetryLabelKeys.Outcome, result.ValidationEffect.TimerValidationOutcome)));
        }
    }
}
