namespace FlexAgent.Postgres.Outbox;

public sealed record OutboxItemWriteModel(
    Guid Id,
    Guid OrganizationId,
    string EventType,
    string AggregateType,
    Guid AggregateId,
    Guid CorrelationId,
    string PayloadDigest,
    DateTimeOffset CreatedAt);

public interface IOutboxItemWriter
{
    Task InsertAsync(
        OutboxItemWriteModel outboxItem,
        Npgsql.NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default);
}
