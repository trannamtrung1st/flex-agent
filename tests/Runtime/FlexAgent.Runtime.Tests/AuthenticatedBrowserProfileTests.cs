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
        Assert.True(File.Exists(Path.Combine(root, "deploy", "compose", "authenticated-browser", "seed-demo-work.sql")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "compose", "authenticated-browser.demo-work.compose.yaml")));
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
        Assert.Contains("location ~ ^/sessions/[^/]+/events", nginx);
        Assert.DoesNotContain("location /sessions/ {", nginx);
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
            "http://localhost:18080/##http://localhost:5274/##http://localhost:18080/?signin=denied##http://localhost:5274/?signin=denied",
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
        for (var i = 1; i <= 5; i++)
        {
            Assert.Contains($"demo.admin{i}", usernames);
        }

        for (var i = 1; i <= 30; i++)
        {
            Assert.Contains($"demo.participant{i}", usernames);
        }
    }

    [Fact]
    public void Seed_binds_every_realm_participant_account_with_matching_display_label()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RealmPath()));
        var seed = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "authenticated-browser",
            "seed.sql"));

        var participantUsers = document.RootElement.GetProperty("users").EnumerateArray()
            .Select(item => item)
            .Where(item =>
            {
                var username = item.GetProperty("username").GetString();
                return username == "demo.participant"
                    || (username?.StartsWith("demo.participant", StringComparison.Ordinal) == true
                        && username.Length > "demo.participant".Length
                        && int.TryParse(username["demo.participant".Length..], out _));
            })
            .ToArray();

        Assert.Equal(31, participantUsers.Length);

        foreach (var user in participantUsers)
        {
            var username = user.GetProperty("username").GetString()!;
            var subject = user.GetProperty("id").GetString()!;
            var displayLabel = $"{user.GetProperty("firstName").GetString()} {user.GetProperty("lastName").GetString()}";
            if (username == "demo.participant")
            {
                Assert.Contains(subject, seed, StringComparison.Ordinal);
                Assert.Contains(displayLabel, seed, StringComparison.Ordinal);
                Assert.Contains("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab", seed, StringComparison.Ordinal);
            }
            else
            {
                var number = int.Parse(username["demo.participant".Length..], System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal($"e2000000-0000-4000-8000-{number.ToString("D12", System.Globalization.CultureInfo.InvariantCulture)}", subject);
                Assert.Equal($"Demo Participant {number}", displayLabel);
                Assert.Contains("format('e2000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))", seed, StringComparison.Ordinal);
                Assert.Contains("format('a3000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))", seed, StringComparison.Ordinal);
                Assert.Contains("format('Demo Participant %s', gs.i)", seed, StringComparison.Ordinal);
                Assert.Contains("generate_series(1, 30)", seed, StringComparison.Ordinal);
            }
        }
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
        Assert.Contains("d2000000-0000-4000-8000-", seed);
        Assert.Contains("e2000000-0000-4000-8000-", seed);
        Assert.Contains("generate_series(1, 5)", seed);
        Assert.Contains("generate_series(1, 30)", seed);
        Assert.DoesNotContain("INSERT INTO assessment_activities", seed);
        Assert.DoesNotContain("/browser", seed);
    }

    [Fact]
    public void Demo_work_seed_keeps_assignable_candidates_on_keycloak_participants_only()
    {
        var demoWork = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "authenticated-browser",
            "seed-demo-work.sql"));

        Assert.Contains("DELETE FROM actor_organization_grants", demoWork, StringComparison.Ordinal);
        Assert.Contains("actor_id::text LIKE 'f1000000-%'", demoWork, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "format('f1000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid,\n    1,\n    'assessment.enrollment.receive'",
            demoWork,
            StringComparison.Ordinal);
        Assert.Contains("'Demo Participant'", demoWork, StringComparison.Ordinal);
        Assert.DoesNotContain("Morgan Ellis", demoWork, StringComparison.Ordinal);
    }

    [Fact]
    public void Demo_work_seed_inserts_campaigns_and_enrollments_only_in_demo_work_fixture()
    {
        var demoWork = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "authenticated-browser",
            "seed-demo-work.sql"));

        Assert.Contains("INSERT INTO assessment_activities", demoWork);
        Assert.Contains("INSERT INTO submissions_enrollments", demoWork);
        Assert.Contains("Q3 Safety Compliance", demoWork);
        Assert.Contains("'suspended'", demoWork);
        Assert.Contains("'revoked'", demoWork);
        Assert.Contains("New Hire Policy Acknowledgment", demoWork);
        Assert.Contains(DemoWorkSeedFixtureTests.ActivatedBaselineDigest, demoWork);
        Assert.DoesNotContain("/browser", demoWork);
    }

    [Fact]
    public void Prebuilt_image_overlay_never_pulls_unsigned_ci_tags()
    {
        var overlay = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "authenticated-browser.prebuilt-images.compose.yaml"));

        Assert.Contains("FLEXAGENT_OIDC_API_IMAGE", overlay);
        Assert.Contains("FLEXAGENT_OIDC_SPA_IMAGE", overlay);
        Assert.Contains("pull_policy: never", overlay);
    }

    [Fact]
    public void Smoke_profile_starts_prebuilt_app_images_without_a_registry_pull()
    {
        var script = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "build",
            "scripts",
            "authenticated-browser-profile.sh"));

        Assert.Contains("--prebuilt-images", script);
        Assert.Contains("up-smoke", script);
        Assert.Contains("--no-build", script);
        Assert.Contains("run --rm --no-deps", script);
        Assert.DoesNotContain("--pull never", script);
    }

    [Fact]
    public void Smoke_profile_raises_daemon_timeouts_and_prepulls_digest_pinned_images()
    {
        var script = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "build",
            "scripts",
            "authenticated-browser-profile.sh"));

        Assert.Contains("COMPOSE_HTTP_TIMEOUT", script);
        Assert.Contains("DOCKER_CLIENT_TIMEOUT", script);
        Assert.Contains("FLEXAGENT_DOCKER_PULL_TIMEOUT", script);
        Assert.Contains("config --images", script);
        Assert.Contains("mirror.gcr.io", script);
        Assert.Contains("@sha256:", script);
        Assert.Contains("infra tier failed", script);
        Assert.Contains("ensure_pinned_images", script);
    }

    [Fact]
    public void Oidc_ci_script_installs_playwright_with_os_deps_in_ci()
    {
        var script = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "build",
            "scripts",
            "verify-oidc-ci.sh"));

        Assert.Contains("playwright install --with-deps chromium", script);
    }

    [Fact]
    public void Oci_workflow_loads_native_daemon_images_for_oidc_smoke()
    {
        var workflow = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            ".github",
            "workflows",
            "implementation.yml"));

        Assert.Contains("driver: docker", workflow);
        Assert.Contains("provenance: false", workflow);
        Assert.Contains("VITE_API_MODE=production", workflow);
        Assert.Contains("docker run --rm --entrypoint /bin/true", workflow);
        Assert.Contains("COMPOSE_HTTP_TIMEOUT", workflow);
        Assert.Contains("DOCKER_CLIENT_TIMEOUT", workflow);
        Assert.DoesNotContain("platforms: linux/amd64", workflow);
    }

    [Fact]
    public void Profile_script_honors_demo_work_toggle_and_overlay()
    {
        var script = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "build",
            "scripts",
            "authenticated-browser-profile.sh"));

        Assert.Contains("FLEXAGENT_SEED_DEMO_WORK", script);
        Assert.Contains("authenticated-browser.demo-work.compose.yaml", script);
        Assert.Contains("seed-demo-work.sql", script);
        Assert.Contains("--demo-work", script);
        Assert.Contains("demo_work_enabled", script);
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
    public void Demo_work_overlay_is_development_only_and_not_production()
    {
        var overlay = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "authenticated-browser.demo-work.compose.yaml"));
        Assert.Contains("seed-demo-work.sql", overlay);
        Assert.Contains("Synthetic data only", overlay);
        Assert.DoesNotContain("Production", overlay);
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
