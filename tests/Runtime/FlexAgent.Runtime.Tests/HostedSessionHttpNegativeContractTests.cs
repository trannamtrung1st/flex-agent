using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Buffers.Text;
using FlexAgent.Api;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ApiProgram = FlexAgent.Api.Program;

namespace FlexAgent.Runtime.Tests;

public sealed class HostedSessionHttpNegativeContractTests
{
    private const string Issuer = "https://issuer.example/realms/flex";
    private const string ClientId = "flex-agent-api";
    private const string Subject = "hosted-session-http-subject";

    [Fact]
    public async Task Snapshot_without_a_session_is_unauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(
            $"/v1/sessions/{Guid.CreateVersion7()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString() ?? "no-store");
    }

    [Fact]
    public async Task Guessed_snapshot_is_concealed_as_not_found()
    {
        await using var context = await LoginAsync();
        var sessionId = Guid.CreateVersion7();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/sessions/{sessionId}");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("session.denied", body, StringComparison.Ordinal);
        Assert.DoesNotContain("transcript", body, StringComparison.Ordinal);
        Assert.DoesNotContain("permitted_actions", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Command_without_antiforgery_is_rejected_before_admission()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var sessionId = Guid.CreateVersion7();
        using var response = await client.PostAsync(
            $"/v1/sessions/{sessionId}/commands",
            CommandContent(sessionId),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("csrf.invalid", body, StringComparison.Ordinal);
        Assert.DoesNotContain("accepted_message_id", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Command_with_mismatched_locator_is_invalid()
    {
        await using var context = await LoginAsync();
        var routeId = Guid.CreateVersion7();
        var bodyId = Guid.CreateVersion7();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/sessions/{routeId}/commands")
        {
            Content = CommandContent(bodyId),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("session.command.invalid", body, StringComparison.Ordinal);
        Assert.DoesNotContain("accepted_message_id", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Guessed_command_is_concealed_as_not_found()
    {
        await using var context = await LoginAsync();
        var sessionId = Guid.CreateVersion7();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/sessions/{sessionId}/commands")
        {
            Content = CommandContent(sessionId),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("session.denied", body, StringComparison.Ordinal);
        Assert.DoesNotContain("accepted_message_id", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Authorized_snapshot_is_not_cached()
    {
        var visible = Guid.CreateVersion7();
        await using var context = await LoginAsync(visible);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/sessions/{visible}");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"projection_kind\":\"participant\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("hidden_prompt", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    private static async Task<LoggedInContext> LoginAsync(Guid? visibleSessionId = null)
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        var factory = CreateFactory(rsa, tokens, visibleSessionId);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        SeedBinding(factory);
        var cancellationToken = TestContext.Current.CancellationToken;
        using var login = await client.GetAsync("/auth/login?return_path=/my-work", cancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        tokens.IdToken = CreateIdToken(rsa, query["nonce"].ToString());
        using var callback = await client.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        var sessionCookie = callback.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(HumanAuthenticationHostOptions.CookieName, StringComparison.Ordinal))
            .Split(';', 2)[0];
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
        using var session = await client.GetAsync("/auth/session", cancellationToken);
        var payload = JsonDocument.Parse(await session.Content.ReadAsStringAsync(cancellationToken));
        return new LoggedInContext(
            factory,
            client,
            sessionCookie,
            payload.RootElement.GetProperty("csrf_token").GetString()!);
    }

    private static void SeedBinding(WebApplicationFactory<ApiProgram> factory)
    {
        var bindings = factory.Services.GetRequiredService<MemoryHumanIdentityBindingStore>();
        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        bindings.RegisterActor(actorId);
        bindings.GrantOrganization(actorId, organizationId);
        bindings.TryProvisionAsync(
                new HumanIdentityBinding(
                    Guid.NewGuid(),
                    new ExactIssuerSubject(Issuer, Subject),
                    actorId,
                    DateTimeOffset.UtcNow,
                    null))
            .GetAwaiter()
            .GetResult();
    }

    private static WebApplicationFactory<ApiProgram> CreateFactory(
        RSA? rsa = null,
        FakeOidcAuthorizationClient? tokens = null,
        Guid? visibleSessionId = null)
    {
        rsa ??= RSA.Create(2048);
        tokens ??= new FakeOidcAuthorizationClient();
        var keys = new Dictionary<string, RSA>(StringComparer.Ordinal) { ["test"] = rsa };
        return new WebApplicationFactory<ApiProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("HumanAuthentication:Enabled", "true");
            builder.UseSetting("HumanAuthentication:Issuer", Issuer);
            builder.UseSetting("HumanAuthentication:ClientId", ClientId);
            builder.UseSetting("HumanAuthentication:AuthorizationEndpoint", "https://issuer.example/realms/flex/protocol/openid-connect/auth");
            builder.UseSetting("HumanAuthentication:TokenEndpoint", "https://issuer.example/realms/flex/protocol/openid-connect/token");
            builder.UseSetting("HumanAuthentication:JwksUri", "https://issuer.example/realms/flex/protocol/openid-connect/certs");
            builder.UseSetting("HumanAuthentication:RedirectUri", "https://app.example/auth/callback");
            builder.UseSetting("HumanAuthentication:AcceptedAcr:0", "acr:mfa");
            builder.UseSetting("HumanAuthentication:AcceptedAmr:0", "mfa");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IOidcAuthorizationClient>(tokens);
                services.AddSingleton<IJwksKeySource>(new StaticJwksKeySource(keys));
                services.AddSingleton<IHostedSessionSnapshotQuery>(new StubHostedSessionSnapshotQuery(visibleSessionId));
                services.AddSingleton<IHostedSessionCommandCoordinator>(UnhostedHostedSessionCommandCoordinator.Instance);
            });
        });
    }

    private static StringContent CommandContent(Guid sessionId) =>
        new(
            "{\"schema_version\":\"v1\",\"command_type\":\"session.message.send.v1\",\"command_id\":\"cmd.synthetic0001\",\"idempotency_key\":\"idem-synthetic-0001\",\"session_locator\":{\"session_id\":\""
            + sessionId.ToString("D")
            + "\"},\"expected_session_version\":1,\"payload\":{\"message_text\":\"hello\"}}",
            Encoding.UTF8,
            "application/json");

    private static string CreateIdToken(RSA rsa, string nonce)
    {
        var now = DateTimeOffset.UtcNow;
        var header = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT", kid = "test" });
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["iss"] = Issuer,
            ["aud"] = ClientId,
            ["sub"] = Subject,
            ["nonce"] = nonce,
            ["sid"] = "sid-hosted-1",
            ["acr"] = "acr:mfa",
            ["amr"] = new[] { "mfa" },
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.AddMinutes(-1).ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(5).ToUnixTimeSeconds(),
        });
        var encodedHeader = Encode(Encoding.UTF8.GetBytes(header));
        var encodedPayload = Encode(Encoding.UTF8.GetBytes(payload));
        var signingInput = $"{encodedHeader}.{encodedPayload}";
        var signature = Encode(rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        return $"{signingInput}.{signature}";
    }

    private static string Encode(ReadOnlySpan<byte> bytes)
    {
        var buffer = new byte[Base64Url.GetEncodedLength(bytes.Length)];
        Base64Url.EncodeToUtf8(bytes, buffer, out _, out var written);
        return Encoding.ASCII.GetString(buffer.AsSpan(0, written));
    }

    private sealed record LoggedInContext(
        WebApplicationFactory<ApiProgram> Factory,
        HttpClient Client,
        string SessionCookie,
        string CsrfToken) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Factory.DisposeAsync();
    }

    private sealed class StubHostedSessionSnapshotQuery(Guid? visibleSessionId) : IHostedSessionSnapshotQuery
    {
        public Task<HostedSessionQueryResult> GetAsync(
            TrustedRuntimeActor actor,
            Guid untrustedSessionId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actor);
            _ = cancellationToken;
            if (visibleSessionId is not { } visible || visible != untrustedSessionId)
            {
                return Task.FromResult(new HostedSessionQueryResult(false, "session.denied", null));
            }

            return Task.FromResult(new HostedSessionQueryResult(
                true,
                "session.snapshot.loaded",
                new HostedSessionSnapshot(
                    HostedSessionProjectionKinds.Participant,
                    visible,
                    "active",
                    1,
                    1,
                    DateTimeOffset.Parse("2026-09-03T00:00:00Z"),
                    [HostedSessionPermittedActions.SendMessage],
                    "none",
                    null,
                    "Assessment Agent",
                    1,
                    [],
                    false,
                    "idle",
                    null)));
        }
    }

    private sealed class FakeOidcAuthorizationClient : IOidcAuthorizationClient
    {
        public string? IdToken { get; set; }

        public Task<OidcTokenExchangeResult> ExchangeAuthorizationCodeAsync(
            string code,
            string codeVerifier,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new OidcTokenExchangeResult(IdToken, IdToken is null ? HumanAuthenticationReasonCodes.InvalidProviderResponse : null));
    }

    private sealed class StaticJwksKeySource(IReadOnlyDictionary<string, RSA> keys) : IJwksKeySource
    {
        public Task<JwksKeySnapshot?> TryGetKeysAsync(
            string jwksUri,
            CancellationToken cancellationToken = default) =>
            TryGetKeysAsync(jwksUri, requiredKid: null, cancellationToken);

        public Task<JwksKeySnapshot?> TryGetKeysAsync(
            string jwksUri,
            string? requiredKid,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<JwksKeySnapshot?>(JwksKeySnapshot.Borrowed(keys));
    }
}
