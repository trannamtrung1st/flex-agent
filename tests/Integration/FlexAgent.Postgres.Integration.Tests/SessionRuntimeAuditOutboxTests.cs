using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionRuntimeAuditOutboxTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Audit_failure_rolls_back_admission_and_outbox()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var correlationId = Guid.NewGuid();
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler(),
            new FaultInjectingAuditEventWriter(),
            new PostgresOutboxItemWriter());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.AdmitAsync(
                new AdmitTrustedTriggerCommand(
                    SessionPersistenceFixtures.Actor(organization.ActorId),
                    binding.Ownership,
                    0,
                    SessionPersistenceFixtures.OpeningTrigger("trig.opening.audit"),
                    "idem.opening.audit",
                    correlationId,
                    "integration.test"),
                binding,
                CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var invocationCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_invocations
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var auditCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM audit_events WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        Assert.Equal(0, invocationCount);
        Assert.Equal(0, auditCount);
        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task Outbox_failure_rolls_back_admission_and_audit()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var correlationId = Guid.NewGuid();
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler(),
            new PostgresAuditEventWriter(),
            new FaultInjectingOutboxItemWriter());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.AdmitAsync(
                new AdmitTrustedTriggerCommand(
                    SessionPersistenceFixtures.Actor(organization.ActorId),
                    binding.Ownership,
                    0,
                    SessionPersistenceFixtures.OpeningTrigger("trig.opening.outbox"),
                    "idem.opening.outbox",
                    correlationId,
                    "integration.test"),
                binding,
                CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var invocationCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_invocations
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var auditCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM audit_events WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        Assert.Equal(0, invocationCount);
        Assert.Equal(0, auditCount);
        Assert.Equal(0, outboxCount);
    }

    private sealed class FaultInjectingAuditEventWriter : IAuditEventWriter
    {
        public Task InsertAsync(
            AuditEventWriteModel auditEvent,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected audit failure.");
    }

    private sealed class FaultInjectingOutboxItemWriter : IOutboxItemWriter
    {
        public Task InsertAsync(
            OutboxItemWriteModel outboxItem,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected outbox failure.");
    }
}
