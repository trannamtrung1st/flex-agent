using System.Diagnostics;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed class AdmitTrustedTriggerHandler(ISessionRuntimeTelemetry? telemetry = null)
    : IAdmitTrustedTriggerHandler
{
    private readonly ISessionRuntimeTelemetry _telemetry = telemetry ?? NoopSessionRuntimeTelemetry.Instance;

    public TriggerAdmissionResult Handle(
        AdmitTrustedTriggerCommand command,
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
            result = session.AdmitTrustedTrigger(
                command.Trigger,
                command.IdempotencyKey,
                authoritativeUtc,
                command.ExpectedSessionVersion);
        }

        var labels = SessionRuntimeTelemetryRecording.Labels(
            (SessionRuntimeTelemetryLabelKeys.Outcome, result.OutcomeCode),
            (SessionRuntimeTelemetryLabelKeys.TriggerFamily, command.Trigger.TriggerFamily));
        _telemetry.RecordCounter(SessionRuntimeTelemetryInstruments.TriggerAdmission, labels);
        _telemetry.RecordDuration(
            SessionRuntimeTelemetryInstruments.TriggerAdmission,
            Stopwatch.GetElapsedTime(started),
            labels);
        return result;
    }
}
