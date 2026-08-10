using System.Security.Cryptography;
using System.Text;
using Dapper;
using FlexAgent.Configuration;
using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Postgres.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class MigrationUpgradeTests
{
    [Fact]
    public async Task Upgrade_from_0001_backfills_idempotency_and_rejects_conflicting_retry()
    {
        await using var container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("flexagent_upgrade_test")
            .WithUsername("flexagent")
            .WithPassword("flexagent_upgrade_password")
            .Build();

        await container.StartAsync(TestContext.Current.CancellationToken);
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: "0001_initial_authorization_configuration_schema.sql");

        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        const string idempotencyKey = "upgrade-key-1";
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        var now = DateTimeOffset.UtcNow;

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await connection.ExecuteAsync(
                """
                INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
                INSERT INTO actors (id, created_at) VALUES (@ActorId, @CreatedAt);
                INSERT INTO actor_organization_grants (
                    organization_id, actor_id, relationship_version, granted_action, created_at)
                VALUES (
                    @OrganizationId, @ActorId, 1, @GrantedAction, @CreatedAt);
                INSERT INTO configuration_sources (id, organization_id, source_kind, created_at)
                VALUES (@SourceId, @OrganizationId, @SourceKind, @CreatedAt);
                INSERT INTO configuration_source_versions (
                    id,
                    organization_id,
                    configuration_source_id,
                    schema_version,
                    procedure_id,
                    content_digest,
                    idempotency_key,
                    created_at)
                VALUES (
                    @VersionId,
                    @OrganizationId,
                    @SourceId,
                    @SchemaVersion,
                    @ProcedureId,
                    @ContentDigest,
                    @IdempotencyKey,
                    @CreatedAt);
                """,
                new
                {
                    OrganizationId = organizationId,
                    ActorId = actorId,
                    SourceId = sourceId,
                    VersionId = versionId,
                    GrantedAction = AuthorizationActions.RegisterConfigurationSourceVersion,
                    SourceKind = ConfigurationSourceKinds.SyntheticV1,
                    SchemaVersion = ConfigurationSchemaVersions.V1,
                    ProcedureId = ConfigurationProcedureIds.RscJcsSha256V1,
                    ContentDigest = digest,
                    IdempotencyKey = idempotencyKey,
                    CreatedAt = now,
                });
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var expectedFingerprint = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        $"{ConfigurationProcedureIds.RscJcsSha256V1}|{ConfigurationSchemaVersions.V1}|{digest}")))
            .ToLowerInvariant();

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var backfilledFingerprint = await connection.ExecuteScalarAsync<string>(
                """
                SELECT payload_fingerprint
                FROM configuration_source_version_idempotency
                WHERE organization_id = @OrganizationId
                  AND configuration_source_id = @ConfigurationSourceId
                  AND idempotency_key = @IdempotencyKey;
                """,
                new
                {
                    OrganizationId = organizationId,
                    ConfigurationSourceId = sourceId,
                    IdempotencyKey = idempotencyKey,
                });

            Assert.Equal(expectedFingerprint, backfilledFingerprint);
        }

        var services = ConfigurationServiceCollection.Create(connectionString);
        var seeded = new SeededOrganization(
            organizationId,
            actorId,
            sourceId,
            new TrustedActor(actorId, "synthetic.test_actor"),
            new OrganizationScope(organizationId));

        var alternateContent = Encoding.UTF8.GetBytes(
            """
            {"canonicalization_version":"rfc8785","effective_configuration":{"domains":[{"domain_key":"memory_mode","effective_value":{"mode":"strict"},"provenance_classification":"inherited"}]},"procedure_id":"rsc-jcs-sha256-v1","resolution_decisions":[{"decision_key":"memory_mode","outcome":"stable_required"}],"schema_version":"v1","source_references":[{"content_digest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","source_id":"agent.synth.02","source_key":"agent","source_version":"rev.0002"}]}
            """);
        var alternateDigest = FlexAgent.CanonicalJson.CanonicalJsonProcessor.CanonicalizeSha256Hex(
            alternateContent,
            new FlexAgent.CanonicalJson.CanonicalJsonLimits(65_536, 64, 4_096, 4_096));

        var conflict = await services.RegisterHandler.HandleAsync(
            new RegisterConfigurationSourceVersionCommand(
                seeded.Actor,
                seeded.Scope,
                seeded.ConfigurationSourceId,
                ConfigurationProcedureIds.RscJcsSha256V1,
                ConfigurationSchemaVersions.V1,
                alternateContent,
                alternateDigest,
                idempotencyKey,
                Guid.NewGuid(),
                "integration.test"),
            TestContext.Current.CancellationToken);

        Assert.False(conflict.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.IdempotencyConflict, conflict.OutcomeCode);

        var idempotentRetry = await services.RegisterHandler.HandleAsync(
            new RegisterConfigurationSourceVersionCommand(
                seeded.Actor,
                seeded.Scope,
                seeded.ConfigurationSourceId,
                ConfigurationProcedureIds.RscJcsSha256V1,
                ConfigurationSchemaVersions.V1,
                content,
                digest,
                idempotencyKey,
                Guid.NewGuid(),
                "integration.test"),
            TestContext.Current.CancellationToken);

        Assert.True(idempotentRetry.Succeeded);
        Assert.Equal(versionId, idempotentRetry.Identity!.VersionId);
        Assert.Equal(1, await services.VersionRepository.CountForSourceAsync(
            organizationId,
            sourceId,
            TestContext.Current.CancellationToken));

        await using var verifyConnection = await services.ConnectionAccessor.OpenConnectionAsync(
            TestContext.Current.CancellationToken);
        var idempotencyCount = await verifyConnection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM configuration_source_version_idempotency
            WHERE organization_id = @OrganizationId
              AND configuration_source_id = @ConfigurationSourceId
              AND idempotency_key = @IdempotencyKey;
            """,
            new
            {
                OrganizationId = organizationId,
                ConfigurationSourceId = sourceId,
                IdempotencyKey = idempotencyKey,
            });

        Assert.Equal(1, idempotencyCount);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FlexAgent.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
