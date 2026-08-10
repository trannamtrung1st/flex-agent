using Dapper;
using FlexAgent.Configuration;
using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Postgres.Outbox;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class AuditOutboxFaultInjectionTests(PostgresIntegrationFixture fixture) : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Audit_failure_rolls_back_version_and_outbox()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        var correlationId = Guid.NewGuid();
        var command = CreateCommand(seeded, digest, content, correlationId: correlationId);

        var services = ConfigurationServiceCollection.Create(
            Fixture.ConnectionString,
            auditEventWriter: new FaultInjectingAuditEventWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            services.RegisterHandler.HandleAsync(command, CancellationToken));

        Assert.Equal(0, await services.VersionRepository.CountForSourceAsync(
            seeded.OrganizationId,
            seeded.ConfigurationSourceId,
            CancellationToken));

        await using var connection = await services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var auditCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM audit_events WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });

        Assert.Equal(0, auditCount);
        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task Outbox_failure_rolls_back_version_and_audit()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        var correlationId = Guid.NewGuid();
        var command = CreateCommand(seeded, digest, content, correlationId: correlationId);

        var services = ConfigurationServiceCollection.Create(
            Fixture.ConnectionString,
            outboxItemWriter: new FaultInjectingOutboxItemWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            services.RegisterHandler.HandleAsync(command, CancellationToken));

        Assert.Equal(0, await services.VersionRepository.CountForSourceAsync(
            seeded.OrganizationId,
            seeded.ConfigurationSourceId,
            CancellationToken));

        await using var connection = await services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var auditCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM audit_events WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });

        Assert.Equal(0, auditCount);
        Assert.Equal(0, outboxCount);
    }

    private static RegisterConfigurationSourceVersionCommand CreateCommand(
        SeededOrganization seeded,
        string digest,
        byte[] content,
        Guid correlationId) =>
        new(
            seeded.Actor,
            seeded.Scope,
            seeded.ConfigurationSourceId,
            ConfigurationProcedureIds.RscJcsSha256V1,
            ConfigurationSchemaVersions.V1,
            content,
            digest,
            "fault-injection-key",
            correlationId,
            "integration.test");

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
