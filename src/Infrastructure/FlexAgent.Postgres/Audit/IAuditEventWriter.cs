namespace FlexAgent.Postgres.Audit;

public sealed record AuditEventWriteModel(
    Guid EventId,
    Guid OrganizationId,
    string EventSchemaVersion,
    DateTimeOffset OccurredAt,
    Guid CorrelationId,
    string ActorType,
    Guid ActorId,
    string Action,
    string ResourceType,
    Guid ResourceId,
    string Outcome,
    string? ReasonCode,
    long? RelationshipVersion,
    string SourceChannel,
    string? PayloadDigest);

public interface IAuditEventWriter
{
    Task InsertAsync(
        AuditEventWriteModel auditEvent,
        Npgsql.NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default);
}
