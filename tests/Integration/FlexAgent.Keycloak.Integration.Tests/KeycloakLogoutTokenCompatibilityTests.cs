using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.Keycloak.Integration.Tests;

public sealed class KeycloakLogoutTokenCompatibilityTests
{
    private const string NodeImage =
        "node:22.18.0-alpine@sha256:1b2479dd35a99687d6638f5976fd235e26c5b37e8122f786fcd5fe231d63de5b";

    [Fact]
    public void Compatibility_project_is_not_an_application_or_database_logout_suite()
    {
        var root = FindRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(root, "deploy", "compose", "authenticated-browser.compose.yaml"));
        Assert.Contains("quay.io/keycloak/keycloak:26.7.0@sha256:", compose);
        Assert.Contains("logout-sink", File.ReadAllText(Path.Combine(
            root,
            "tests",
            "Integration",
            "FlexAgent.Keycloak.Integration.Tests",
            "KeycloakLogoutTokenCompatibilityTests.cs")));
    }

    [Fact]
    public async Task Pinned_keycloak_emits_a_signed_logout_token_accepted_by_the_adapter()
    {
        if (!IsRequiredMode)
        {
            Assert.Skip("Keycloak logout-token compatibility runs through pnpm verify:oidc (FLEXAGENT_OIDC_REQUIRED=1).");
        }

        var root = FindRepositoryRoot();
        var templatePath = Path.Combine(root, "deploy", "compose", "keycloak", "flex-agent-realm.json");
        var keycloakImage = ReadPinnedKeycloakImage(root);
        var sinkScript = Path.Combine(AppContext.BaseDirectory, "logout-token-sink.js");
        var work = Directory.CreateTempSubdirectory("flex-agent-keycloak-compat-");
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var renderedRealm = Path.Combine(work.FullName, "flex-agent-realm.json");
        RenderRealm(templatePath, renderedRealm, secret, "http://logout-sink:8080/auth/backchannel-logout", "http://logout-sink:8080");

        await using var network = new NetworkBuilder().Build();
        await using var sink = new ContainerBuilder(NodeImage)
            .WithNetwork(network)
            .WithNetworkAliases("logout-sink")
            .WithResourceMapping(sinkScript, "/opt/")
            .WithCommand("node", "/opt/logout-token-sink.js")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("listening"))
            .Build();
        await using var keycloak = new ContainerBuilder(keycloakImage)
            .WithNetwork(network)
            .WithPortBinding(8080, true)
            .WithCommand(
                "start-dev",
                "--import-realm",
                "--http-enabled=true",
                "--hostname-strict=false")
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
            .WithResourceMapping(renderedRealm, "/opt/keycloak/data/import/")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPath("/realms/flex-agent").ForPort(8080)))
            .Build();

        try
        {
            await sink.StartAsync(TestContext.Current.CancellationToken);
            await keycloak.StartAsync(TestContext.Current.CancellationToken);
        }
        catch (Exception exception) when (IsDockerUnavailable(exception))
        {
            Assert.Fail($"Docker is required for Keycloak logout-token compatibility: {exception.Message}");
        }

        var baseAddress = new UriBuilder("http", keycloak.Hostname, keycloak.GetMappedPublicPort(8080)).Uri;
        using var http = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(20) };
        await AllowMasterRealmHttpFromPublishedPortAsync(keycloak);
        await WaitForTokenEndpointAsync(http);
        var adminToken = await RequestFormAsync(
            http,
            "/realms/master/protocol/openid-connect/token",
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = "admin",
                ["password"] = "admin",
            });
        using var clientsRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/admin/realms/flex-agent/clients?clientId=flex-agent-api");
        clientsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var clientsResponse = await http.SendAsync(clientsRequest, TestContext.Current.CancellationToken);
        clientsResponse.EnsureSuccessStatusCode();
        using var clients = JsonDocument.Parse(await clientsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var clientUuid = clients.RootElement[0].GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(clientUuid));

        await RequestFormAsync(
            http,
            "/realms/flex-agent/protocol/openid-connect/token",
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "flex-agent-api",
                ["client_secret"] = secret,
                ["username"] = "demo.participant",
                ["password"] = "zaQ@123456!",
                ["scope"] = "openid",
            });

        using var usersRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/admin/realms/flex-agent/users?username=demo.participant");
        usersRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var usersResponse = await http.SendAsync(usersRequest, TestContext.Current.CancellationToken);
        usersResponse.EnsureSuccessStatusCode();
        using var users = JsonDocument.Parse(await usersResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var userId = users.RootElement[0].GetProperty("id").GetString();
        using var logoutRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/admin/realms/flex-agent/users/{userId}/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var logoutResponse = await http.SendAsync(logoutRequest, TestContext.Current.CancellationToken);
        logoutResponse.EnsureSuccessStatusCode();

        var form = await WaitForSinkFormAsync(sink);
        var logoutToken = ReadFormValue(form, "logout_token");
        Assert.False(string.IsNullOrWhiteSpace(logoutToken));

        using var jwksResponse = await http.GetAsync(
            "/realms/flex-agent/protocol/openid-connect/certs",
            TestContext.Current.CancellationToken);
        jwksResponse.EnsureSuccessStatusCode();
        var keys = ReadJwks(await jwksResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var issuer = ReadJwtIssuer(logoutToken!);
        var validated = OidcIdTokenValidator.ValidateLogoutToken(
            logoutToken,
            new OidcValidationProfile(issuer, "flex-agent-api", TimeSpan.FromSeconds(60), TimeSpan.FromHours(1)),
            keys,
            TimeProvider.System);

        Assert.True(validated.Succeeded, validated.ReasonCode);
        Assert.False(string.IsNullOrWhiteSpace(validated.LogoutToken!.JwtId));
        Assert.True(
            !string.IsNullOrWhiteSpace(validated.LogoutToken.Subject)
            || !string.IsNullOrWhiteSpace(validated.LogoutToken.ProviderSessionId));
    }

    private static bool IsRequiredMode =>
        string.Equals(Environment.GetEnvironmentVariable("FLEXAGENT_OIDC_REQUIRED"), "1", StringComparison.Ordinal);

    private static void RenderRealm(string templatePath, string outputPath, string secret, string backchannel, string adminUrl)
    {
        var realm = JsonNode.Parse(File.ReadAllText(templatePath))
            ?? throw new InvalidOperationException("Realm template was empty.");
        var client = realm["clients"]!.AsArray().Single(item => item!["clientId"]?.GetValue<string>() == "flex-agent-api")!;
        client["secret"] = secret;
        client["adminUrl"] = adminUrl;
        client["rootUrl"] = adminUrl;
        client["attributes"]!["backchannel.logout.url"] = backchannel;
        File.WriteAllText(outputPath, realm.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string ReadPinnedKeycloakImage(string root)
    {
        foreach (var line in File.ReadLines(Path.Combine(root, "deploy", "compose", "authenticated-browser.compose.yaml")))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("image: quay.io/keycloak/keycloak:", StringComparison.Ordinal))
            {
                return trimmed["image: ".Length..];
            }
        }

        throw new InvalidOperationException("Pinned Keycloak image was not found.");
    }

    private static async Task<string> WaitForSinkFormAsync(DotNet.Testcontainers.Containers.IContainer sink)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var result = await sink.ExecAsync(["cat", "/tmp/last-form"], TestContext.Current.CancellationToken);
            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Stdout))
            {
                return result.Stdout;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException("Logout-token sink did not receive a Keycloak callback.");
    }

    private static async Task AllowMasterRealmHttpFromPublishedPortAsync(
        DotNet.Testcontainers.Containers.IContainer keycloak)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        var last = "";
        while (DateTime.UtcNow < deadline)
        {
            var credentials = await keycloak.ExecAsync(
                [
                    "/opt/keycloak/bin/kcadm.sh",
                    "config",
                    "credentials",
                    "--server",
                    "http://127.0.0.1:8080",
                    "--realm",
                    "master",
                    "--user",
                    "admin",
                    "--password",
                    "admin",
                ],
                TestContext.Current.CancellationToken);
            if (credentials.ExitCode == 0)
            {
                var update = await keycloak.ExecAsync(
                    [
                        "/opt/keycloak/bin/kcadm.sh",
                        "update",
                        "realms/master",
                        "-s",
                        "sslRequired=NONE",
                    ],
                    TestContext.Current.CancellationToken);
                if (update.ExitCode == 0)
                {
                    return;
                }

                last = update.Stderr;
            }
            else
            {
                last = credentials.Stderr;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException(
            "Could not allow HTTP token requests against the published Keycloak port. " + last);
    }

    private static async Task WaitForTokenEndpointAsync(HttpClient http)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await RequestFormAsync(
                    http,
                    "/realms/master/protocol/openid-connect/token",
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "password",
                        ["client_id"] = "admin-cli",
                        ["username"] = "admin",
                        ["password"] = "admin",
                    });
                return;
            }
            catch (HttpRequestException exception)
            {
                last = exception;
                await Task.Delay(500, TestContext.Current.CancellationToken);
            }
        }

        throw new TimeoutException("Keycloak token endpoint was not ready.", last);
    }

    private static async Task<string> RequestFormAsync(
        HttpClient http,
        string path,
        Dictionary<string, string> values)
    {
        HttpRequestException? last = null;
        for (var attempt = 0; attempt < 12; attempt++)
        {
            using var response = await http.PostAsync(
                path,
                new FormUrlEncodedContent(values),
                TestContext.Current.CancellationToken);
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(body);
                return document.RootElement.GetProperty("access_token").GetString()
                    ?? throw new InvalidOperationException("Token response omitted access_token.");
            }

            last = new HttpRequestException($"Token endpoint returned {(int)response.StatusCode}: {TrimErrorBody(body)}");
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        throw last ?? new HttpRequestException("Token endpoint failed.");
    }

    private static string TrimErrorBody(string body)
    {
        var trimmed = body.ReplaceLineEndings(" ").Trim();
        return trimmed.Length <= 180 ? trimmed : trimmed[..180];
    }

    private static IReadOnlyDictionary<string, RSA> ReadJwks(string json)
    {
        using var document = JsonDocument.Parse(json);
        var keys = new Dictionary<string, RSA>(StringComparer.Ordinal);
        foreach (var key in document.RootElement.GetProperty("keys").EnumerateArray())
        {
            if (key.GetProperty("kty").GetString() != "RSA"
                || !key.TryGetProperty("kid", out var kid)
                || string.IsNullOrWhiteSpace(kid.GetString()))
            {
                continue;
            }

            var rsa = RSA.Create();
            rsa.ImportParameters(
                new RSAParameters
                {
                    Modulus = Decode(key.GetProperty("n").GetString()!),
                    Exponent = Decode(key.GetProperty("e").GetString()!),
                });
            keys[kid.GetString()!] = rsa;
        }

        return keys;
    }

    private static byte[] Decode(string value)
    {
        var utf8 = Encoding.ASCII.GetBytes(value);
        var bytes = new byte[Base64Url.GetMaxDecodedLength(utf8.Length)];
        if (!Base64Url.TryDecodeFromUtf8(utf8, bytes, out var written))
        {
            throw new InvalidOperationException("JWKS modulus or exponent was not Base64URL.");
        }

        Array.Resize(ref bytes, written);
        return bytes;
    }

    private static string? ReadFormValue(string form, string name)
    {
        foreach (var pair in form.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(Uri.UnescapeDataString(parts[0]), name, StringComparison.Ordinal))
            {
                return Uri.UnescapeDataString(parts[1].Replace('+', ' '));
            }
        }

        return null;
    }

    private static string ReadJwtIssuer(string token)
    {
        var payload = token.Split('.')[1];
        var utf8 = Encoding.ASCII.GetBytes(payload);
        var bytes = new byte[Base64Url.GetMaxDecodedLength(utf8.Length)];
        if (!Base64Url.TryDecodeFromUtf8(utf8, bytes, out var written))
        {
            throw new InvalidOperationException("Logout token payload was not Base64URL.");
        }

        using var document = JsonDocument.Parse(bytes.AsMemory(0, written));
        return document.RootElement.GetProperty("iss").GetString()
            ?? throw new InvalidOperationException("Logout token omitted iss.");
    }

    private static bool IsDockerUnavailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("docker.sock", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("Docker is either not running", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("Cannot connect to the Docker", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
