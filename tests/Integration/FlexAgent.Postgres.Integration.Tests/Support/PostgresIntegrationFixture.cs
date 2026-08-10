using System.Diagnostics;
using Dapper;
using FlexAgent.Configuration;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FlexAgent.Postgres.Integration.Tests.Support;

public sealed class PostgresIntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("flexagent_test")
        .WithUsername("flexagent")
        .WithPassword("flexagent_test_password")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public ConfigurationServiceCollection.ServiceBundle Services { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        await RunMigrationsAsync();
        Services = ConfigurationServiceCollection.Create(ConnectionString);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    public async Task RunMigrationsAsync()
    {
        var root = FindRepositoryRoot();
        var migrationsDirectory = Path.Combine(root, "database", "migrations");
        await GrateMigrationRunner.RunAsync(
            ConnectionString,
            migrationsDirectory,
            allowEmbeddedFallback: true);
    }

    public async Task<SeededOrganization> SeedOrganizationAsync(string suffix = "")
    {
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var connection = await Services.ConnectionAccessor.OpenConnectionAsync();
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
            """,
            new
            {
                OrganizationId = organizationId,
                ActorId = actorId,
                GrantedAction = AuthorizationActions.RegisterConfigurationSourceVersion,
                SourceId = sourceId,
                SourceKind = ConfigurationSourceKinds.SyntheticV1,
                CreatedAt = now,
            });

        return new SeededOrganization(
            organizationId,
            actorId,
            sourceId,
            new TrustedActor(actorId, "synthetic.test_actor"),
            new OrganizationScope(organizationId));
    }

    public static byte[] LoadMinimalStableDomainCanonicalUtf8()
    {
        var root = FindRepositoryRoot();
        var fixturePath = Path.Combine(
            root,
            "contracts",
            "fixtures",
            "jcs",
            "rsc-jcs-sha256-v1",
            "minimal-stable-domain",
            "fixture.json");

        var json = File.ReadAllText(fixturePath);
        var hexStart = json.IndexOf("\"expected_canonical_utf8_hex\": \"", StringComparison.Ordinal);
        if (hexStart < 0)
        {
            throw new InvalidOperationException("Fixture missing expected_canonical_utf8_hex.");
        }

        hexStart += "\"expected_canonical_utf8_hex\": \"".Length;
        var hexEnd = json.IndexOf('"', hexStart);
        var hex = json[hexStart..hexEnd];
        return Convert.FromHexString(hex);
    }

    public static string MinimalStableDomainDigest =>
        "ac061086af2a5869dbbfe45ee45b48204e163865186664c49c9874d6de961c13";

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

public sealed record SeededOrganization(
    Guid OrganizationId,
    Guid ActorId,
    Guid ConfigurationSourceId,
    TrustedActor Actor,
    OrganizationScope Scope);

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresIntegrationFixture>;

[Collection(nameof(PostgresCollection))]
public abstract class PostgresIntegrationTest(PostgresIntegrationFixture fixture)
{
    protected PostgresIntegrationFixture Fixture { get; } = fixture;

    protected CancellationToken CancellationToken => TestContext.Current.CancellationToken;
}
