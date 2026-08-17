using System.Diagnostics;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record AcceptParticipantMessageCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    long ExpectedSessionVersion,
    string ParticipantMessageId,
    string TurnId,
    string ResponseSlotId,
    string TriggerId,
    string IdempotencyKey,
    Guid CorrelationId,
    string SourceChannel);

public interface IAcceptParticipantMessageHandler
{
    TriggerAdmissionResult Handle(
        AcceptParticipantMessageCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc);
}

public sealed class AcceptParticipantMessageHandler(ISessionRuntimeTelemetry? telemetry = null)
    : IAcceptParticipantMessageHandler
{
    private readonly ISessionRuntimeTelemetry _telemetry = telemetry ?? NoopSessionRuntimeTelemetry.Instance;

    public TriggerAdmissionResult Handle(
        AcceptParticipantMessageCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        var started = Stopwatch.GetTimestamp();

        TriggerAdmissionResult result;
        if (command.Actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Actor.ActorType))
        {
            result = new TriggerAdmissionResult(false, TriggerAdmissionOutcomeCodes.Denied, null, null);
        }
        else if (command.Ownership != session.Ownership)
        {
            result = new TriggerAdmissionResult(
                false,
                TriggerAdmissionOutcomeCodes.OwnershipMismatch,
                null,
                null);
        }
        else
        {
            result = session.AcceptParticipantMessage(
                command.ParticipantMessageId,
                command.TurnId,
                command.ResponseSlotId,
                command.TriggerId,
                command.IdempotencyKey,
                authoritativeUtc,
                command.ExpectedSessionVersion);
        }

        var labels = SessionRuntimeTelemetryRecording.Labels(
            (SessionRuntimeTelemetryLabelKeys.Outcome, result.OutcomeCode),
            (SessionRuntimeTelemetryLabelKeys.TriggerFamily, RuntimeTriggerIdentifiers.ParticipantInputFamily));
        _telemetry.RecordCounter(SessionRuntimeTelemetryInstruments.TriggerAdmission, labels);
        _telemetry.RecordDuration(
            SessionRuntimeTelemetryInstruments.TriggerAdmission,
            Stopwatch.GetElapsedTime(started),
            labels);
        return result;
    }
}
