using System.Text.Json;
using Dapper;
using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class ConfigurationSourcePayloadPersistenceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Registering_a_source_version_persists_canonical_utf8_payload()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, content, digest, "payload-persist"),
            CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Identity);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var payload = await connection.QuerySingleOrDefaultAsync<byte[]>(
            new CommandDefinition(
                """
                SELECT canonical_utf8
                FROM configuration_source_payloads
                WHERE organization_id = @OrganizationId
                  AND source_version_id = @VersionId
                  AND content_digest = @ContentDigest;
                """,
                new
                {
                    OrganizationId = seeded.OrganizationId,
                    VersionId = result.Identity!.VersionId,
                    ContentDigest = digest,
                },
                cancellationToken: CancellationToken));

        Assert.NotNull(payload);
        Assert.Equal(content, payload);
    }

    [Fact]
    public async Task Public_source_version_reads_remain_content_free()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, content, digest, "payload-metadata"),
            CancellationToken);
        Assert.True(result.Succeeded);

        var listed = await Fixture.Services.VersionRepository.ListForSourceAsync(
            seeded.OrganizationId,
            seeded.ConfigurationSourceId,
            CancellationToken);

        Assert.Contains(listed, row => row.Id == result.Identity!.VersionId);
        var serialized = JsonSerializer.Serialize(listed);
        Assert.DoesNotContain("canonical", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Convert.ToHexString(content), serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Payload_reads_are_organization_scoped()
    {
        var orgA = await Fixture.SeedOrganizationAsync("a");
        var orgB = await Fixture.SeedOrganizationAsync("b");
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        var registered = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(orgA, content, digest, "payload-scope"),
            CancellationToken);
        Assert.True(registered.Succeeded);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var visible = await connection.QuerySingleOrDefaultAsync<byte[]>(
            new CommandDefinition(
                """
                SELECT canonical_utf8
                FROM configuration_source_payloads
                WHERE organization_id = @OrganizationId
                  AND source_version_id = @VersionId;
                """,
                new
                {
                    OrganizationId = orgA.OrganizationId,
                    VersionId = registered.Identity!.VersionId,
                },
                cancellationToken: CancellationToken));
        var hidden = await connection.QuerySingleOrDefaultAsync<byte[]>(
            new CommandDefinition(
                """
                SELECT canonical_utf8
                FROM configuration_source_payloads
                WHERE organization_id = @OrganizationId
                  AND source_version_id = @VersionId;
                """,
                new
                {
                    OrganizationId = orgB.OrganizationId,
                    VersionId = registered.Identity!.VersionId,
                },
                cancellationToken: CancellationToken));

        Assert.NotNull(visible);
        Assert.Equal(content, visible);
        Assert.Null(hidden);

        var owned = await Fixture.Services.PayloadReader.GetPayloadForVersionAsync(
            orgA.OrganizationId,
            orgA.ConfigurationSourceId,
            registered.Identity.VersionId,
            CancellationToken);
        var crossOrganization = await Fixture.Services.PayloadReader.GetPayloadForVersionAsync(
            orgB.OrganizationId,
            orgA.ConfigurationSourceId,
            registered.Identity.VersionId,
            CancellationToken);
        var wrongSource = await Fixture.Services.PayloadReader.GetPayloadForVersionAsync(
            orgA.OrganizationId,
            orgB.ConfigurationSourceId,
            registered.Identity.VersionId,
            CancellationToken);

        Assert.NotNull(owned);
        Assert.Equal(content, owned.CanonicalUtf8);
        Assert.Null(crossOrganization);
        Assert.Null(wrongSource);
    }

    [Fact]
    public async Task Payload_rejects_configuration_source_id_that_does_not_match_the_version()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var otherSourceId = Guid.CreateVersion7();
        var versionId = Guid.CreateVersion7();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO configuration_sources (id, organization_id, source_kind, created_at)
                VALUES (@OtherSourceId, @OrganizationId, @SourceKind, CLOCK_TIMESTAMP());
                INSERT INTO configuration_source_versions (
                    id, organization_id, configuration_source_id, schema_version, procedure_id,
                    content_digest, idempotency_key, created_at)
                VALUES (
                    @VersionId, @OrganizationId, @SourceId, 'v1', 'rsc-jcs-sha256-v1',
                    @ContentDigest, @IdempotencyKey, CLOCK_TIMESTAMP());
                """,
                new
                {
                    seeded.OrganizationId,
                    seeded.ConfigurationSourceId,
                    OtherSourceId = otherSourceId,
                    VersionId = versionId,
                    SourceId = seeded.ConfigurationSourceId,
                    SourceKind = FlexAgent.Configuration.Domain.ConfigurationSourceKinds.SyntheticV1,
                    ContentDigest = digest,
                    IdempotencyKey = versionId.ToString("D"),
                },
                cancellationToken: CancellationToken));

        var exception = await Assert.ThrowsAsync<Npgsql.PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO configuration_source_payloads (
                        organization_id, configuration_source_id, source_version_id,
                        content_digest, canonical_utf8, created_at)
                    VALUES (
                        @OrganizationId, @OtherSourceId, @VersionId,
                        @ContentDigest, @CanonicalUtf8, CLOCK_TIMESTAMP());
                    """,
                    new
                    {
                        seeded.OrganizationId,
                        OtherSourceId = otherSourceId,
                        VersionId = versionId,
                        ContentDigest = digest,
                        CanonicalUtf8 = content,
                    },
                    cancellationToken: CancellationToken)));
        Assert.Equal(Npgsql.PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
    }

    [Fact]
    public async Task Configuration_source_payloads_reject_ordinary_update_and_delete()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, content, digest, "payload-immutable"),
            CancellationToken);
        Assert.True(result.Succeeded);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var updateException = await Assert.ThrowsAsync<Npgsql.PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE configuration_source_payloads
                    SET content_digest = 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'
                    WHERE organization_id = @OrganizationId
                      AND source_version_id = @VersionId;
                    """,
                    new
                    {
                        OrganizationId = seeded.OrganizationId,
                        VersionId = result.Identity!.VersionId,
                    },
                    cancellationToken: CancellationToken)));
        Assert.Contains("immutable", updateException.MessageText, StringComparison.OrdinalIgnoreCase);

        var deleteException = await Assert.ThrowsAsync<Npgsql.PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM configuration_source_payloads
                    WHERE organization_id = @OrganizationId
                      AND source_version_id = @VersionId;
                    """,
                    new
                    {
                        OrganizationId = seeded.OrganizationId,
                        VersionId = result.Identity!.VersionId,
                    },
                    cancellationToken: CancellationToken)));
        Assert.Contains("immutable", deleteException.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Active_legal_hold_blocks_payload_lifecycle_disposition()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, content, digest, "payload-hold"),
            CancellationToken);
        Assert.True(result.Succeeded);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO configuration_source_payload_holds (
                    organization_id, hold_id, source_version_id, reason_code, active, created_at)
                VALUES (@OrganizationId, @HoldId, @VersionId, 'legal_hold', TRUE, CLOCK_TIMESTAMP());
                """,
                new
                {
                    OrganizationId = seeded.OrganizationId,
                    HoldId = Guid.CreateVersion7(),
                    VersionId = result.Identity!.VersionId,
                },
                cancellationToken: CancellationToken));

        var holdException = await Assert.ThrowsAsync<Npgsql.PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "SELECT dispose_configuration_source_payload(@OrganizationId, @VersionId);",
                    new
                    {
                        OrganizationId = seeded.OrganizationId,
                        VersionId = result.Identity!.VersionId,
                    },
                    cancellationToken: CancellationToken)));
        Assert.Contains("legal hold", holdException.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inactive_hold_does_not_block_payload_lifecycle_disposition()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, content, digest, "payload-inactive-hold"),
            CancellationToken);
        Assert.True(result.Succeeded);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO configuration_source_payload_holds (
                    organization_id, hold_id, source_version_id, reason_code, active, created_at)
                VALUES (@OrganizationId, @HoldId, @VersionId, 'legal_hold', FALSE, CLOCK_TIMESTAMP());
                """,
                new
                {
                    OrganizationId = seeded.OrganizationId,
                    HoldId = Guid.CreateVersion7(),
                    VersionId = result.Identity!.VersionId,
                },
                cancellationToken: CancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                "SELECT dispose_configuration_source_payload(@OrganizationId, @VersionId);",
                new
                {
                    OrganizationId = seeded.OrganizationId,
                    VersionId = result.Identity.VersionId,
                },
                cancellationToken: CancellationToken));

        var remaining = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*)
                FROM configuration_source_payloads
                WHERE organization_id = @OrganizationId
                  AND source_version_id = @VersionId;
                """,
                new
                {
                    OrganizationId = seeded.OrganizationId,
                    VersionId = result.Identity.VersionId,
                },
                cancellationToken: CancellationToken));
        Assert.Equal(0, remaining);
    }

    private static RegisterConfigurationSourceVersionCommand CreateCommand(
        SeededOrganization seeded,
        byte[] content,
        string digest,
        string idempotencyKey) =>
        new(
            seeded.Actor,
            seeded.Scope,
            seeded.ConfigurationSourceId,
            ConfigurationProcedureIds.RscJcsSha256V1,
            ConfigurationSchemaVersions.V1,
            content,
            digest,
            idempotencyKey,
            Guid.NewGuid(),
            "integration.test");
}
