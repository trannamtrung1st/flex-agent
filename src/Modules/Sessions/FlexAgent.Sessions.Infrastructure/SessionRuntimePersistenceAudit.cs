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
        ISessionRuntimeTelemetry? telemetry = null,
        long? relationshipVersion = null,
        string? authorizationReferenceType = null,
        Guid? authorizationReferenceId = null)
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
                    RelationshipVersion: relationshipVersion,
                    SourceChannel: sourceChannel,
                    PayloadDigest: digest,
                    AuthorizationReferenceType: authorizationReferenceType,
                    AuthorizationReferenceId: authorizationReferenceId),
                transaction,
                cancellationToken);
        }
        catch
        {
            TryRecordFault(signals, SessionRuntimeTelemetryValues.Audit, SessionRuntimeTelemetryValues.Failed);
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
            TryRecordFault(signals, SessionRuntimeTelemetryValues.Outbox, SessionRuntimeTelemetryValues.Failed);
            throw;
        }

        var faultKind = action == SessionRuntimeAuditActions.SealManifest
            ? SessionRuntimeTelemetryValues.Manifest
            : SessionRuntimeTelemetryValues.Audit;
        TryRecordFault(signals, faultKind, SessionRuntimeTelemetryValues.Succeeded);
    }

    public static async Task WriteDenialAsync(
        IAuditEventWriter auditEventWriter,
        TrustedRuntimeActor actor,
        SessionOwnership ownership,
        Guid correlationId,
        string sourceChannel,
        string action,
        string outcome,
        string reasonCode,
        DateTimeOffset occurredAt,
        Npgsql.NpgsqlTransaction transaction,
        CancellationToken cancellationToken,
        ISessionRuntimeTelemetry? telemetry = null,
        string? authorizationReferenceType = null,
        Guid? authorizationReferenceId = null)
    {
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
                    Outcome: outcome,
                    ReasonCode: reasonCode,
                    RelationshipVersion: null,
                    SourceChannel: sourceChannel,
                    PayloadDigest: null,
                    AuthorizationReferenceType: authorizationReferenceId is null
                        ? null
                        : authorizationReferenceType,
                    AuthorizationReferenceId: authorizationReferenceId),
                transaction,
                cancellationToken);
        }
        catch
        {
            TryRecordFault(signals, SessionRuntimeTelemetryValues.Audit, SessionRuntimeTelemetryValues.Failed);
            throw;
        }

        TryRecordFault(signals, SessionRuntimeTelemetryValues.Audit, SessionRuntimeTelemetryValues.Succeeded);
    }

    private static void TryRecordFault(ISessionRuntimeTelemetry telemetry, string kind, string outcome)
    {
        try
        {
            telemetry.RecordCounter(
                SessionRuntimeTelemetryInstruments.Fault,
                SessionRuntimeTelemetryRecording.Labels(
                    (SessionRuntimeTelemetryLabelKeys.FaultKind, kind),
                    (SessionRuntimeTelemetryLabelKeys.Outcome, outcome)));
        }
        catch
        {
        }
    }
}
