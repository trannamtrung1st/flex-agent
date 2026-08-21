using System.Text.Json;

namespace FlexAgent.Runtime.Tests;

public sealed class AuthenticatedBrowserProfileTests
{
    [Fact]
    public void Documented_command_and_compose_file_exist()
    {
        var root = FindRepositoryRoot();
        Assert.True(File.Exists(Path.Combine(root, "build", "scripts", "authenticated-browser-profile.sh")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "compose", "authenticated-browser.compose.yaml")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "compose", "nginx", "authenticated-browser.conf")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "compose", "authenticated-browser", "seed.sql")));
    }

    [Fact]
    public void Compose_uses_the_canonical_gateway_without_a_host_database_port()
    {
        var compose = File.ReadAllText(ComposePath());
        Assert.Contains("18080:80", compose);
        Assert.DoesNotContain("5432:5432", compose);
        Assert.Contains("Host=postgres;Port=5432", compose);
        Assert.Contains("reach PostgreSQL at postgres:5432", compose);
        Assert.Contains("ASPNETCORE_ENVIRONMENT: Development", compose);
        Assert.Contains("HumanAuthentication__Enabled: true", compose);
        Assert.Contains("http://localhost:18080/realms/flex-agent", compose);
        Assert.Contains("http://localhost:18080/auth/callback", compose);
        Assert.Contains("http://keycloak:8080/realms/flex-agent/protocol/openid-connect/token", compose);
        Assert.Contains("http://keycloak:8080/realms/flex-agent/protocol/openid-connect/certs", compose);
        Assert.Contains("VITE_API_MODE: production", compose);
        Assert.DoesNotContain("/browser", compose);
    }

    [Fact]
    public void Gateway_routes_spa_api_and_restricted_keycloak_paths()
    {
        var nginx = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "nginx",
            "authenticated-browser.conf"));

        Assert.Contains("location / {", nginx);
        Assert.Contains("proxy_pass http://spa:8080", nginx);
        Assert.Contains("location /auth/", nginx);
        Assert.Contains("location /v1/assessment", nginx);
        Assert.Contains("location /sessions/", nginx);
        Assert.Contains("proxy_pass http://api:8080", nginx);
        Assert.Contains("location /realms/flex-agent", nginx);
        Assert.Contains("proxy_pass http://keycloak:8080/realms/flex-agent", nginx);
        Assert.Contains("location /admin", nginx);
        Assert.Contains("location /health", nginx);
        Assert.Contains("return 404", nginx);
        Assert.DoesNotContain("/browser", nginx);
        Assert.DoesNotContain("realms/master", nginx);
    }

    [Fact]
    public void Realm_registers_the_gateway_callback_and_an_administrator_with_mfa_claims()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RealmPath()));
        var client = document.RootElement.GetProperty("clients").EnumerateArray()
            .Single(item => item.GetProperty("clientId").GetString() == "flex-agent-api");
        var redirects = client.GetProperty("redirectUris").EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        Assert.Contains("http://localhost:18080/auth/callback", redirects);

        var mappers = client.GetProperty("protocolMappers").EnumerateArray().ToArray();
        Assert.Contains(mappers, mapper =>
            mapper.GetProperty("config").GetProperty("claim.name").GetString() == "acr"
            && mapper.GetProperty("config").GetProperty("claim.value").GetString() == "acr:mfa");
        Assert.Contains(mappers, mapper =>
            mapper.GetProperty("config").GetProperty("claim.name").GetString() == "amr"
            && mapper.GetProperty("config").GetProperty("claim.value").GetString()!.Contains("mfa", StringComparison.Ordinal));

        var admin = document.RootElement.GetProperty("users").EnumerateArray()
            .Single(item => item.GetProperty("username").GetString() == "synthetic.administrator");
        Assert.True(admin.GetProperty("enabled").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(admin.GetProperty("id").GetString()));
    }

    [Fact]
    public void Seed_binds_the_exact_gateway_identity_and_minimum_assessment_grants()
    {
        var seed = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "authenticated-browser",
            "seed.sql"));

        Assert.Contains("http://localhost:18080/realms/flex-agent", seed);
        Assert.Contains("synthetic.administrator", seed);
        Assert.Contains("assessment.activity.create", seed);
        Assert.Contains("assessment.activity.read", seed);
        Assert.Contains("assessment.activity.save", seed);
        Assert.Contains("assessment.readiness.check", seed);
        Assert.Contains("assessment.cohort.activate", seed);
        Assert.Contains("assessment.source.select", seed);
        Assert.Contains("assessment.activation.reconcile", seed);
        Assert.Contains("assessment.baseline.read", seed);
        Assert.Contains("configuration_source_readiness_descriptors", seed);
        Assert.DoesNotContain("INSERT INTO assessment_activities", seed);
        Assert.DoesNotContain("/browser", seed);
    }

    [Fact]
    public void Script_is_non_interactive_and_exposes_start_readiness_seed_and_reset()
    {
        var script = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "build",
            "scripts",
            "authenticated-browser-profile.sh"));

        Assert.Contains("set -euo pipefail", script);
        Assert.Contains("up", script);
        Assert.Contains("reset", script);
        Assert.Contains("status", script);
        Assert.Contains("down", script);
        Assert.Contains("validate", script);
        Assert.DoesNotContain("read -p", script);
        Assert.DoesNotContain("read -r", script);
        Assert.Contains("http://localhost:18080", script);
        Assert.Contains("authenticated-browser.compose.yaml", script);
    }

    [Fact]
    public void Negative_configuration_rejects_a_host_published_database_and_synthetic_browser_route()
    {
        var compose = File.ReadAllText(ComposePath());
        var nginx = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "nginx",
            "authenticated-browser.conf"));

        Assert.DoesNotContain("5432:5432", compose);
        Assert.DoesNotContain("location /browser", nginx);
        Assert.DoesNotContain("proxy_pass http://host.docker.internal:18082", nginx);
    }

    private static string ComposePath() =>
        Path.Combine(FindRepositoryRoot(), "deploy", "compose", "authenticated-browser.compose.yaml");

    private static string RealmPath() =>
        Path.Combine(FindRepositoryRoot(), "deploy", "compose", "keycloak", "flex-agent-realm.json");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FlexAgent.slnx"))
                || File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
