using Dapper;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class ConfigurationSourceVersionImmutabilityTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Configuration_source_versions_reject_update_and_delete()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            new FlexAgent.Configuration.Application.RegisterConfigurationSourceVersionCommand(
                seeded.Actor,
                seeded.Scope,
                seeded.ConfigurationSourceId,
                FlexAgent.Configuration.Domain.ConfigurationProcedureIds.RscJcsSha256V1,
                FlexAgent.Configuration.Domain.ConfigurationSchemaVersions.V1,
                content,
                digest,
                "immutable-version",
                Guid.NewGuid(),
                "integration.test"),
            CancellationToken);

        Assert.True(result.Succeeded);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);

        var updateException = await Assert.ThrowsAsync<Npgsql.PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE configuration_source_versions
                    SET content_digest = 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'
                    WHERE organization_id = @OrganizationId
                      AND id = @VersionId;
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
                    DELETE FROM configuration_source_versions
                    WHERE organization_id = @OrganizationId
                      AND id = @VersionId;
                    """,
                    new
                    {
                        OrganizationId = seeded.OrganizationId,
                        VersionId = result.Identity!.VersionId,
                    },
                    cancellationToken: CancellationToken)));

        Assert.Contains("immutable", deleteException.MessageText, StringComparison.OrdinalIgnoreCase);
    }
}
