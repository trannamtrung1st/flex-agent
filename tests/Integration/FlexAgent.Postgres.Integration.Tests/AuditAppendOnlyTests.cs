using Dapper;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class AuditAppendOnlyTests(PostgresIntegrationFixture fixture) : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Audit_events_reject_update_and_delete()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO audit_events (
                    event_id, organization_id, event_schema_version, occurred_at,
                    correlation_id, actor_type, actor_id, action, resource_type, resource_id,
                    outcome, source_channel)
                VALUES (
                    @EventId, @OrganizationId, 'audit-event.v1', @OccurredAt,
                    @CorrelationId, 'synthetic.test_actor', @ActorId, 'test.action',
                    'test.resource', @ResourceId, 'succeeded', 'integration.test');
                """,
                new
                {
                    EventId = eventId,
                    OrganizationId = seeded.OrganizationId,
                    OccurredAt = now,
                    CorrelationId = Guid.NewGuid(),
                    ActorId = seeded.ActorId,
                    ResourceId = Guid.NewGuid(),
                },
                cancellationToken: CancellationToken));

        var updateException = await Assert.ThrowsAsync<Npgsql.PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE audit_events SET outcome = 'tampered' WHERE event_id = @EventId;",
                    new { EventId = eventId },
                    cancellationToken: CancellationToken)));

        Assert.Contains("append-only", updateException.MessageText, StringComparison.OrdinalIgnoreCase);

        var deleteException = await Assert.ThrowsAsync<Npgsql.PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM audit_events WHERE event_id = @EventId;",
                    new { EventId = eventId },
                    cancellationToken: CancellationToken)));

        Assert.Contains("append-only", deleteException.MessageText, StringComparison.OrdinalIgnoreCase);
    }
}
