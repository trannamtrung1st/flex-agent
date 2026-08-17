using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

internal static class SessionRuntimePersistenceAudit
{
    public static async Task WriteAsync(
        IAuditEventWriter auditEventWriter,
        IOutboxItemWriter outboxItemWriter,
        TrustedRuntimeActor actor,
        SessionOwnership ownership,
        Guid correlationId,
        string sourceChannel,
        string action,
        string eventType,
        string payloadSeed,
        DateTimeOffset occurredAt,
        Npgsql.NpgsqlTransaction transaction,
        CancellationToken cancellationToken,
        ISessionRuntimeTelemetry? telemetry = null)
    {
        var digest = ProtectedContentRef.DigestForReference(payloadSeed);
        var signals = telemetry ?? NoopSessionRuntimeTelemetry.Instance;
        try
        {
            await auditEventWriter.InsertAsync(
                new AuditEventWriteModel(
                    EventId: Guid.NewGuid(),
                    OrganizationId: ownership.OrganizationId,
                    EventSchemaVersion: "audit-event.v1",
                    OccurredAt: occurredAt,
                    CorrelationId: correlationId,
                    ActorType: actor.ActorType,
                    ActorId: actor.ActorId,
                    Action: action,
                    ResourceType: SessionRuntimeResourceTypes.Session,
                    ResourceId: ownership.SessionId,
                    Outcome: "succeeded",
                    ReasonCode: null,
                    RelationshipVersion: null,
                    SourceChannel: sourceChannel,
                    PayloadDigest: digest),
                transaction,
                cancellationToken);
        }
        catch
        {
            RecordFault(signals, SessionRuntimeTelemetryValues.Audit, SessionRuntimeTelemetryValues.Failed);
            throw;
        }

        try
        {
            await outboxItemWriter.InsertAsync(
                new OutboxItemWriteModel(
                    Id: Guid.NewGuid(),
                    OrganizationId: ownership.OrganizationId,
                    EventType: eventType,
                    AggregateType: SessionRuntimeResourceTypes.Session,
                    AggregateId: ownership.SessionId,
                    CorrelationId: correlationId,
                    PayloadDigest: digest,
                    CreatedAt: occurredAt),
                transaction,
                cancellationToken);
        }
        catch
        {
            RecordFault(signals, SessionRuntimeTelemetryValues.Outbox, SessionRuntimeTelemetryValues.Failed);
            throw;
        }

        var faultKind = action == SessionRuntimeAuditActions.SealManifest
            ? SessionRuntimeTelemetryValues.Manifest
            : SessionRuntimeTelemetryValues.Audit;
        RecordFault(signals, faultKind, SessionRuntimeTelemetryValues.Succeeded);
    }

    private static void RecordFault(ISessionRuntimeTelemetry telemetry, string kind, string outcome) =>
        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.Fault,
            SessionRuntimeTelemetryRecording.Labels(
                (SessionRuntimeTelemetryLabelKeys.FaultKind, kind),
                (SessionRuntimeTelemetryLabelKeys.Outcome, outcome)));
}
