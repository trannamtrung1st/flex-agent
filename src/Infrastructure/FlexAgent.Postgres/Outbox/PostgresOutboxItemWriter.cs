using Dapper;
using Npgsql;

namespace FlexAgent.Postgres.Outbox;

public sealed class PostgresOutboxItemWriter : IOutboxItemWriter
{
    private const string InsertSql = """
        INSERT INTO outbox_items (
            id,
            organization_id,
            event_type,
            aggregate_type,
            aggregate_id,
            correlation_id,
            payload_digest,
            created_at)
        VALUES (
            @Id,
            @OrganizationId,
            @EventType,
            @AggregateType,
            @AggregateId,
            @CorrelationId,
            @PayloadDigest,
            @CreatedAt);
        """;

    public async Task InsertAsync(
        OutboxItemWriteModel outboxItem,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            InsertSql,
            outboxItem,
            transaction,
            cancellationToken: cancellationToken);

        var rows = await transaction.Connection!.ExecuteAsync(command);
        if (rows != 1)
        {
            throw new InvalidOperationException("Outbox item insert did not affect exactly one row.");
        }
    }
}
