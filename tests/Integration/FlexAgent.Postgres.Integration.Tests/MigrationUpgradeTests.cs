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
    private const string Historical0002ScriptName = "0002_idempotency_and_version_immutability.sql";
    private const string Historical0003ScriptName = "0003_repair_idempotency_backfill_and_source_version_fk.sql";
    private const string Current0004ScriptName = "0004_harden_constraint_scope_checks.sql";
    private const string Current0005ScriptName = "0005_session_runtime_schema.sql";
    private const string Current0006ScriptName = "0006_harden_session_runtime_invariants.sql";

    [Fact]
    public async Task Upgrade_from_0001_backfills_idempotency_and_rejects_conflicting_retry()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: "0001_initial_authorization_configuration_schema.sql");

        var seededState = await SeedLegacyVersionAsync(connectionString);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName);

        await AssertRepairEvidenceAsync(connectionString, seededState);
    }

    [Fact]
    public async Task Upgrade_from_recorded_historical_0002_repairs_via_0003_without_checksum_failure()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var historical0002Sql = await ReadHistoricalFixtureAsync("0002_idempotency_and_version_immutability_4e21917.sql");

        Assert.Equal(
            GrateMigrationRunner.ComputeScriptHash(historical0002Sql),
            GrateMigrationRunner.ComputeScriptHash(
                await File.ReadAllTextAsync(
                    Path.Combine(migrationsDirectory, "up", Historical0002ScriptName),
                    TestContext.Current.CancellationToken)));

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: "0001_initial_authorization_configuration_schema.sql");

        var seededState = await SeedLegacyVersionAsync(connectionString);

        await GrateMigrationRunner.ApplyRecordedMigrationForTestsAsync(
            connectionString,
            Historical0002ScriptName,
            historical0002Sql,
            TestContext.Current.CancellationToken);

        await AssertIdempotencyRowCountAsync(connectionString, seededState, expectedCount: 0);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName);

        await AssertRepairEvidenceAsync(connectionString, seededState);
    }

    [Fact]
    public async Task Upgrade_from_recorded_historical_0003_applies_0004_without_checksum_failure()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var historical0002Sql = await ReadHistoricalFixtureAsync("0002_idempotency_and_version_immutability_4e21917.sql");
        var historical0003Sql = await ReadHistoricalFixtureAsync("0003_repair_idempotency_backfill_and_source_version_fk_d244a6a.sql");

        Assert.Equal(
            GrateMigrationRunner.ComputeScriptHash(historical0003Sql),
            GrateMigrationRunner.ComputeScriptHash(
                await File.ReadAllTextAsync(
                    Path.Combine(migrationsDirectory, "up", Historical0003ScriptName),
                    TestContext.Current.CancellationToken)));

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: "0001_initial_authorization_configuration_schema.sql");

        var seededState = await SeedLegacyVersionAsync(connectionString);

        await GrateMigrationRunner.ApplyRecordedMigrationForTestsAsync(
            connectionString,
            Historical0002ScriptName,
            historical0002Sql,
            TestContext.Current.CancellationToken);

        await GrateMigrationRunner.ApplyRecordedMigrationForTestsAsync(
            connectionString,
            Historical0003ScriptName,
            historical0003Sql,
            TestContext.Current.CancellationToken);

        await AssertIdempotencyRowCountAsync(connectionString, seededState, expectedCount: 1);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName);

        await AssertRepairEvidenceAsync(connectionString, seededState);
    }

    private static async Task AssertAppliedScriptsAsync(
        string connectionString,
        params string[] expectedScripts)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var appliedScripts = (await connection.QueryAsync<string>(
            "SELECT script_name FROM grate_migrations ORDER BY script_name;")).AsList();

        Assert.Equal(expectedScripts, appliedScripts);
    }

    private static async Task AssertIdempotencyRowCountAsync(
        string connectionString,
        LegacyVersionSeed seededState,
        int expectedCount)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var idempotencyCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM configuration_source_version_idempotency
            WHERE organization_id = @OrganizationId
              AND configuration_source_id = @ConfigurationSourceId
              AND idempotency_key = @IdempotencyKey;
            """,
            new
            {
                seededState.OrganizationId,
                ConfigurationSourceId = seededState.SourceId,
                seededState.IdempotencyKey,
            });

        Assert.Equal(expectedCount, idempotencyCount);
    }

    private static async Task<string> ReadHistoricalFixtureAsync(string fileName) =>
        await File.ReadAllTextAsync(
            Path.Combine(
                FindRepositoryRoot(),
                "tests",
                "Integration",
                "FlexAgent.Postgres.Integration.Tests",
                "Fixtures",
                "migrations",
                fileName),
            TestContext.Current.CancellationToken);

    private static async Task<LegacyVersionSeed> SeedLegacyVersionAsync(string connectionString)
    {
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        const string idempotencyKey = "upgrade-key-1";
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        var now = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(connectionString);
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

        return new LegacyVersionSeed(
            organizationId,
            actorId,
            sourceId,
            versionId,
            idempotencyKey,
            digest);
    }

    private static async Task AssertRepairEvidenceAsync(string connectionString, LegacyVersionSeed seededState)
    {
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var expectedFingerprint = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        $"{ConfigurationProcedureIds.RscJcsSha256V1}|{ConfigurationSchemaVersions.V1}|{seededState.Digest}")))
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
                    seededState.OrganizationId,
                    ConfigurationSourceId = seededState.SourceId,
                    seededState.IdempotencyKey,
                });

            Assert.Equal(expectedFingerprint, backfilledFingerprint);
        }

        var services = ConfigurationServiceCollection.Create(connectionString);
        var seeded = new SeededOrganization(
            seededState.OrganizationId,
            seededState.ActorId,
            seededState.SourceId,
            new TrustedActor(seededState.ActorId, "synthetic.test_actor"),
            new OrganizationScope(seededState.OrganizationId));

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
                seededState.IdempotencyKey,
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
                seededState.Digest,
                seededState.IdempotencyKey,
                Guid.NewGuid(),
                "integration.test"),
            TestContext.Current.CancellationToken);

        Assert.True(idempotentRetry.Succeeded);
        Assert.Equal(seededState.VersionId, idempotentRetry.Identity!.VersionId);
        Assert.Equal(1, await services.VersionRepository.CountForSourceAsync(
            seededState.OrganizationId,
            seededState.SourceId,
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
                seededState.OrganizationId,
                ConfigurationSourceId = seededState.SourceId,
                seededState.IdempotencyKey,
            });

        Assert.Equal(1, idempotencyCount);
    }

    private static async Task<PostgreSqlContainer> StartContainerAsync()
    {
        var container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("flexagent_upgrade_test")
            .WithUsername("flexagent")
            .WithPassword("flexagent_upgrade_password")
            .Build();

        await container.StartAsync(TestContext.Current.CancellationToken);
        return container;
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

    private sealed record LegacyVersionSeed(
        Guid OrganizationId,
        Guid ActorId,
        Guid SourceId,
        Guid VersionId,
        string IdempotencyKey,
        string Digest);
}
