using Dapper;
using Npgsql;

namespace FlexAgent.Postgres.Audit;

public sealed class PostgresAuditEventWriter : IAuditEventWriter
{
    private const string InsertSql = """
        INSERT INTO audit_events (
            event_id,
            organization_id,
            event_schema_version,
            occurred_at,
            correlation_id,
            actor_type,
            actor_id,
            action,
            resource_type,
            resource_id,
            outcome,
            reason_code,
            relationship_version,
            source_channel,
            payload_digest)
        VALUES (
            @EventId,
            @OrganizationId,
            @EventSchemaVersion,
            @OccurredAt,
            @CorrelationId,
            @ActorType,
            @ActorId,
            @Action,
            @ResourceType,
            @ResourceId,
            @Outcome,
            @ReasonCode,
            @RelationshipVersion,
            @SourceChannel,
            @PayloadDigest);
        """;

    public async Task InsertAsync(
        AuditEventWriteModel auditEvent,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            InsertSql,
            auditEvent,
            transaction,
            cancellationToken: cancellationToken);

        var rows = await transaction.Connection!.ExecuteAsync(command);
        if (rows != 1)
        {
            throw new InvalidOperationException("Audit event insert did not affect exactly one row.");
        }
    }
}
