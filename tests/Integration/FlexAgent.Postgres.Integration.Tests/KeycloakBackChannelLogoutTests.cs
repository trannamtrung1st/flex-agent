using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class KeycloakBackChannelLogoutTests
{
    [Fact]
    public async Task Keycloak_signed_logout_token_satisfies_the_backchannel_contract()
    {
        var realmPath = Path.Combine(FindRepositoryRoot(), "deploy", "compose", "keycloak", "flex-agent-realm.json");
        var listenerPort = GetFreeTcpPort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://+:{listenerPort}/");
        try
        {
            listener.Start();
        }
        catch (HttpListenerException exception)
        {
            Assert.Skip($"Host HTTP listener is unavailable: {exception.Message}");
        }

        await using var keycloak = new ContainerBuilder("quay.io/keycloak/keycloak:26.7.0")
            .WithPortBinding(8080, true)
            .WithCommand(
                "start-dev",
                "--import-realm",
                "--http-enabled=true",
                "--hostname-strict=false")
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
            .WithResourceMapping(realmPath, "/opt/keycloak/data/import/flex-agent-realm.json")
            .WithExtraHost("host.docker.internal", "host-gateway")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPath("/realms/flex-agent").ForPort(8080)))
            .Build();

        try
        {
            await keycloak.StartAsync(TestContext.Current.CancellationToken);
        }
        catch (Exception exception) when (IsDockerUnavailable(exception))
        {
            Assert.Skip($"Docker is unavailable: {exception.Message}");
        }

        var baseAddress = new UriBuilder("http", keycloak.Hostname, keycloak.GetMappedPublicPort(8080)).Uri;
        using var http = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(15) };
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
        clientsRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        using var clientsResponse = await http.SendAsync(clientsRequest, TestContext.Current.CancellationToken);
        clientsResponse.EnsureSuccessStatusCode();
        using var clients = JsonDocument.Parse(await clientsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var client = clients.RootElement[0];
        var clientUuid = client.GetProperty("id").GetString();
        var attributes = JsonSerializer.Deserialize<Dictionary<string, string>>(client.GetProperty("attributes").GetRawText())
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
        attributes["backchannel.logout.url"] = $"http://host.docker.internal:{listenerPort}/auth/backchannel-logout";
        attributes["backchannel.logout.session.required"] = "false";
        using var updateClient = new HttpRequestMessage(HttpMethod.Put, $"/admin/realms/flex-agent/clients/{clientUuid}")
        {
            Content = JsonContent(new
            {
                clientId = "flex-agent-api",
                enabled = true,
                protocol = "openid-connect",
                publicClient = false,
                secret = "flex-agent-contract-client-secret",
                standardFlowEnabled = true,
                directAccessGrantsEnabled = true,
                adminUrl = $"http://host.docker.internal:{listenerPort}",
                attributes,
            }),
        };
        updateClient.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        using var updated = await http.SendAsync(updateClient, TestContext.Current.CancellationToken);
        updated.EnsureSuccessStatusCode();

        await RequestFormAsync(
            http,
            "/realms/flex-agent/protocol/openid-connect/token",
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "flex-agent-api",
                ["client_secret"] = "flex-agent-contract-client-secret",
                ["username"] = "synthetic.participant",
                ["password"] = "synthetic-participant-password",
                ["scope"] = "openid",
            });

        using var usersRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/admin/realms/flex-agent/users?username=synthetic.participant");
        usersRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        using var usersResponse = await http.SendAsync(usersRequest, TestContext.Current.CancellationToken);
        usersResponse.EnsureSuccessStatusCode();
        using var users = JsonDocument.Parse(await usersResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var userId = users.RootElement[0].GetProperty("id").GetString();
        var received = listener.GetContextAsync();
        using var logoutRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/admin/realms/flex-agent/users/{userId}/logout");
        logoutRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        using var logoutResponse = await http.SendAsync(logoutRequest, TestContext.Current.CancellationToken);
        logoutResponse.EnsureSuccessStatusCode();

        var completed = await Task.WhenAny(received, Task.Delay(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken));
        Assert.Same(received, completed);
        var context = await received;
        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        var form = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        context.Response.StatusCode = 204;
        context.Response.Close();
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

    private static async Task<string> RequestFormAsync(
        HttpClient http,
        string path,
        Dictionary<string, string> values)
    {
        using var response = await http.PostAsync(
            path,
            new FormUrlEncodedContent(values),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Token response omitted access_token.");
    }

    private static StringContent JsonContent(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

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

    private static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
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
