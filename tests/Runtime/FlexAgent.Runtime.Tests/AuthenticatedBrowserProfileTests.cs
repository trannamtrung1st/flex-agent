using System.Text.Json;

namespace FlexAgent.Runtime.Tests;

public sealed class AuthenticatedBrowserProfileTests
{
    [Fact]
    public void Documented_command_and_compose_file_exist()
    {
        var root = FindRepositoryRoot();
        Assert.True(File.Exists(Path.Combine(root, "build", "scripts", "authenticated-browser-profile.sh")));
        Assert.True(File.Exists(Path.Combine(root, "build", "scripts", "validate-authenticated-browser-compose.py")));
        Assert.True(File.Exists(Path.Combine(root, "build", "scripts", "render-oidc-realm.py")));
        Assert.True(File.Exists(Path.Combine(root, "build", "scripts", "verify-oidc.sh")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "compose", "authenticated-browser.compose.yaml")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "compose", "nginx", "authenticated-browser.conf")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "compose", "authenticated-browser", "seed.sql")));
        Assert.False(File.Exists(Path.Combine(root, "deploy", "compose", "keycloak-contract.compose.yaml")));
        Assert.False(File.Exists(Path.Combine(root, "deploy", "compose", "nginx", "keycloak-contract.conf")));
        Assert.False(File.Exists(Path.Combine(root, "deploy", "compose", "authenticated-browser", "secrets", "oidc-client-secret")));
    }

    [Fact]
    public void Compose_pins_images_and_uses_the_canonical_gateway_without_a_host_database_port()
    {
        var compose = File.ReadAllText(ComposePath());
        Assert.Contains("127.0.0.1:18080:80", compose);
        Assert.DoesNotMatch(@"-\s*""18080:80""", compose);
        Assert.DoesNotContain("5432:5432", compose);
        Assert.Contains("Host=postgres;Port=5432", compose);
        Assert.Contains("reach PostgreSQL at postgres:5432", compose);
        Assert.Contains("ASPNETCORE_ENVIRONMENT: Development", compose);
        Assert.Contains("HumanAuthentication__Enabled: true", compose);
        Assert.Contains(
            "HumanAuthentication__EndSessionEndpoint: http://localhost:18080/realms/flex-agent/protocol/openid-connect/logout",
            compose);
        Assert.Contains("http://localhost:18080/realms/flex-agent", compose);
        Assert.Contains("http://localhost:18080/auth/callback", compose);
        Assert.Contains("http://keycloak:8080/realms/flex-agent/protocol/openid-connect/token", compose);
        Assert.Contains("http://keycloak:8080/realms/flex-agent/protocol/openid-connect/certs", compose);
        Assert.Contains("VITE_API_MODE: production", compose);
        Assert.Contains(".generated/secrets", compose);
        Assert.Contains(".generated/flex-agent-realm.json", compose);
        Assert.Contains("@sha256:", compose);
        Assert.Contains("postgres:18@sha256:", compose);
        Assert.Contains("quay.io/keycloak/keycloak:26.7.0@sha256:", compose);
        Assert.Contains("nginx:1.30.4@sha256:", compose);
        Assert.Contains("chrislusf/seaweedfs:4.29@sha256:", compose);
        Assert.Contains("condition: service_healthy", compose);
        Assert.DoesNotContain("/browser", compose);
        Assert.DoesNotContain("host.docker.internal", compose);
        Assert.Contains("tmpfs:", compose);
        Assert.Contains("/var/lib/postgresql", compose);
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
        Assert.Contains("location /v2/assessment", nginx);
        Assert.Contains("location /sessions/", nginx);
        Assert.Contains("proxy_pass http://api:8080", nginx);
        Assert.Contains("location /realms/flex-agent", nginx);
        Assert.Contains("proxy_pass http://keycloak:8080/realms/flex-agent", nginx);
        Assert.Contains("location /admin", nginx);
        Assert.Contains("location /health", nginx);
        Assert.Contains("location /metrics", nginx);
        Assert.Contains("location /realms/master", nginx);
        Assert.Contains("location /browser", nginx);
        Assert.Contains("return 404", nginx);
        Assert.DoesNotContain("proxy_pass http://keycloak:8080/realms/master", nginx);
        Assert.DoesNotContain("host.docker.internal", nginx);
    }

    [Fact]
    public void Realm_template_registers_the_in_compose_backchannel_without_a_client_secret()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RealmPath()));
        var client = document.RootElement.GetProperty("clients").EnumerateArray()
            .Single(item => item.GetProperty("clientId").GetString() == "flex-agent-api");
        Assert.False(client.TryGetProperty("secret", out var secret) && secret.ValueKind == JsonValueKind.String && secret.GetString()?.Length > 0);
        var redirects = client.GetProperty("redirectUris").EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        Assert.Contains("http://localhost:18080/auth/callback", redirects);
        Assert.Contains("http://localhost:5274/auth/callback", redirects);
        Assert.Equal(
            "http://localhost:18080/##http://localhost:5274/",
            client.GetProperty("attributes").GetProperty("post.logout.redirect.uris").GetString());
        Assert.Equal("http://api:8080", client.GetProperty("adminUrl").GetString());
        Assert.Equal(
            "http://api:8080/auth/backchannel-logout",
            client.GetProperty("attributes").GetProperty("backchannel.logout.url").GetString());
        Assert.Equal("S256", client.GetProperty("attributes").GetProperty("pkce.code.challenge.method").GetString());

        var mappers = client.GetProperty("protocolMappers").EnumerateArray().ToArray();
        Assert.Contains(mappers, mapper =>
            mapper.GetProperty("config").GetProperty("claim.name").GetString() == "acr"
            && mapper.GetProperty("config").GetProperty("claim.value").GetString() == "acr:mfa"
            && mapper.GetProperty("name").GetString()!.Contains("synthetic-accepted-strength", StringComparison.Ordinal));
        Assert.Contains(mappers, mapper =>
            mapper.GetProperty("config").GetProperty("claim.name").GetString() == "amr"
            && mapper.GetProperty("config").GetProperty("claim.value").GetString()!.Contains("mfa", StringComparison.Ordinal));

        var usernames = document.RootElement.GetProperty("users").EnumerateArray()
            .Select(item => item.GetProperty("username").GetString())
            .ToArray();
        Assert.Contains("demo.admin", usernames);
        Assert.Contains("demo.participant", usernames);
        Assert.Contains("demo.unbound", usernames);
        Assert.Contains("demo.zeroorg", usernames);
        Assert.Contains("demo.ambiguous", usernames);
    }

    [Fact]
    public void Seed_binds_the_exact_gateway_identity_and_fail_closed_fixtures()
    {
        var seed = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "authenticated-browser",
            "seed.sql"));

        Assert.Contains("http://localhost:18080/realms/flex-agent", seed);
        Assert.Contains("demo.admin", seed);
        Assert.Contains("assessment.activity.create", seed);
        Assert.Contains("assessment.activity.read", seed);
        Assert.DoesNotContain("ffffffff-ffff-4fff-8fff-ffffffffffff", seed);
        Assert.Contains("11111111-1111-4111-8111-111111111111", seed);
        Assert.Contains("22222222-2222-4222-8222-222222222222", seed);
        Assert.Contains("cccccccc-cccc-4ccc-8ccc-cccccccccccd", seed);
        Assert.Contains("identity_human_display_profiles", seed);
        Assert.Contains("demo.participant", seed);
        Assert.DoesNotContain("INSERT INTO assessment_activities", seed);
        Assert.DoesNotContain("/browser", seed);
    }

    [Fact]
    public void Script_requires_docker_and_validates_the_rendered_file_set()
    {
        var script = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "build",
            "scripts",
            "authenticated-browser-profile.sh"));

        Assert.Contains("set -euo pipefail", script);
        Assert.Contains("require_docker", script);
        Assert.Contains("Docker Compose is required", script);
        Assert.Contains("--overlay", script);
        Assert.Contains("--project-name", script);
        Assert.Contains("candidate", script);
        Assert.Contains("validate-authenticated-browser-compose.py", script);
        Assert.Contains("render-oidc-realm.py", script);
        Assert.Contains("config --format json", script);
        Assert.DoesNotContain("read -p", script);
    }

    [Fact]
    public void Candidate_overlay_is_explicitly_non_production()
    {
        var overlay = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "authenticated-browser.candidate-dev.compose.yaml"));
        Assert.Contains("Not Production", overlay);
        Assert.Contains("http://localhost:5274/auth/callback", overlay);
        Assert.Contains("authenticated-browser-profile.sh --overlay candidate", overlay);
        Assert.Contains("pnpm compose:candidate", overlay);
    }

    [Fact]
    public void Root_package_json_exposes_compose_scripts_that_delegate_to_profile_wrapper()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "package.json")));
        var scripts = document.RootElement.GetProperty("scripts");
        var expected = new Dictionary<string, string>
        {
            ["compose:up"] = "bash build/scripts/authenticated-browser-profile.sh up",
            ["compose:down"] = "bash build/scripts/authenticated-browser-profile.sh down",
            ["compose:reset"] = "bash build/scripts/authenticated-browser-profile.sh reset",
            ["compose:status"] = "bash build/scripts/authenticated-browser-profile.sh status",
            ["compose:validate"] = "bash build/scripts/authenticated-browser-profile.sh validate",
            ["compose:candidate"] = "bash build/scripts/authenticated-browser-profile.sh --overlay candidate up",
        };

        foreach (var (name, command) in expected)
        {
            Assert.True(scripts.TryGetProperty(name, out var value), $"missing script {name}");
            Assert.Equal(command, value.GetString());
        }
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
