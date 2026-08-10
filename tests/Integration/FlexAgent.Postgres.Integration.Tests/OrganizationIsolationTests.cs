using Dapper;
using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class OrganizationIsolationTests(PostgresIntegrationFixture fixture) : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Wrong_organization_scope_returns_no_rows()
    {
        var orgA = await Fixture.SeedOrganizationAsync("a");
        var orgB = await Fixture.SeedOrganizationAsync("b");
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        Assert.True((await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(orgA, digest, content),
            CancellationToken)).Succeeded);

        var listForB = await Fixture.Services.VersionRepository.ListForSourceAsync(
            orgB.OrganizationId,
            orgB.ConfigurationSourceId,
            CancellationToken);

        var countForB = await Fixture.Services.VersionRepository.CountForSourceAsync(
            orgB.OrganizationId,
            orgB.ConfigurationSourceId,
            CancellationToken);

        Assert.Empty(listForB);
        Assert.Equal(0, countForB);
    }

    [Fact]
    public async Task Forged_organization_scope_is_denied()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var forgedScope = new OrganizationScope(Guid.NewGuid());
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        var command = new RegisterConfigurationSourceVersionCommand(
            seeded.Actor,
            forgedScope,
            seeded.ConfigurationSourceId,
            ConfigurationProcedureIds.RscJcsSha256V1,
            ConfigurationSchemaVersions.V1,
            content,
            digest,
            "forged-scope",
            Guid.NewGuid(),
            "integration.test");

        var result = await Fixture.Services.RegisterHandler.HandleAsync(command, CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.Denied, result.OutcomeCode);
    }

    [Fact]
    public async Task Guessed_configuration_source_id_is_denied()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        var command = new RegisterConfigurationSourceVersionCommand(
            seeded.Actor,
            seeded.Scope,
            Guid.NewGuid(),
            ConfigurationProcedureIds.RscJcsSha256V1,
            ConfigurationSchemaVersions.V1,
            content,
            digest,
            "guessed-source",
            Guid.NewGuid(),
            "integration.test");

        var result = await Fixture.Services.RegisterHandler.HandleAsync(command, CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.Denied, result.OutcomeCode);
    }

    private static RegisterConfigurationSourceVersionCommand CreateCommand(
        SeededOrganization seeded,
        string digest,
        byte[] content) =>
        new(
            seeded.Actor,
            seeded.Scope,
            seeded.ConfigurationSourceId,
            ConfigurationProcedureIds.RscJcsSha256V1,
            ConfigurationSchemaVersions.V1,
            content,
            digest,
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid(),
            "integration.test");
}
