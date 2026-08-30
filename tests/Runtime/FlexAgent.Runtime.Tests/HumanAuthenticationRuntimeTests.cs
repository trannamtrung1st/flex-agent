using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Buffers.Text;
using FlexAgent.Api;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ApiProgram = FlexAgent.Api.Program;

namespace FlexAgent.Runtime.Tests;

public sealed class HumanAuthenticationRuntimeTests
{
    private const string Issuer = "https://issuer.example/realms/flex";
    private const string ClientId = "flex-agent-api";
    private const string Subject = "subject-1";

    [Fact]
    public async Task Login_uses_pkce_and_rejects_unsafe_return_paths()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;

        using var unsafeRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/login?return_path=https://evil.example");
        using var unsafeResponse = await client.SendAsync(unsafeRequest, cancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, unsafeResponse.StatusCode);

        using var login = await client.GetAsync("/auth/login?return_path=/work", cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var location = login.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("code_challenge_method=S256", location, StringComparison.Ordinal);
        Assert.Contains("code_challenge=", location, StringComparison.Ordinal);
        Assert.Contains("state=", location, StringComparison.Ordinal);
        Assert.Contains("nonce=", location, StringComparison.Ordinal);
        Assert.DoesNotContain("client_secret", location, StringComparison.Ordinal);
        var correlation = login.Headers.GetValues("Set-Cookie").Single(value =>
            value.StartsWith(HumanAuthenticationHostOptions.CorrelationCookieName, StringComparison.Ordinal));
        Assert.Contains("httponly", correlation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", correlation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Callback_rejects_state_that_is_not_bound_to_this_browser()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(rsa, tokens);
        var attacker = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var victim = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory);

        using var login = await attacker.GetAsync("/auth/login?return_path=/work", cancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        tokens.IdToken = CreateIdToken(rsa, query["nonce"].ToString());

        using var stolen = await victim.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        await AssertCallbackFailedToSpa(stolen, endProviderSession: false);
        var stolenCookies = stolen.Headers.TryGetValues("Set-Cookie", out var setCookies)
            ? string.Join('\n', setCookies)
            : string.Empty;
        Assert.DoesNotContain(HumanAuthenticationHostOptions.CookieName + "=", stolenCookies, StringComparison.Ordinal);

        using var legitimate = await attacker.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, legitimate.StatusCode);
        var sessionCookie = legitimate.Headers.GetValues("Set-Cookie").Single(value =>
            value.StartsWith(HumanAuthenticationHostOptions.CookieName, StringComparison.Ordinal));
        Assert.Contains("httponly", sessionCookie, StringComparison.OrdinalIgnoreCase);
        var cleared = legitimate.Headers.GetValues("Set-Cookie").Single(value =>
            value.StartsWith(HumanAuthenticationHostOptions.CorrelationCookieName, StringComparison.Ordinal));
        Assert.Contains("1970", cleared, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Callback_issues_opaque_cookie_and_hides_stable_identifiers()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(rsa, tokens);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory);

        using var login = await client.GetAsync("/auth/login?return_path=/work", cancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        Assert.Equal("openid profile", query["scope"].ToString());
        tokens.IdToken = CreateIdToken(rsa, query["nonce"].ToString());

        using var callback = await client.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/work", callback.Headers.Location?.ToString());
        var cookie = callback.Headers.GetValues("Set-Cookie").Single(value =>
            value.StartsWith(HumanAuthenticationHostOptions.CookieName, StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("subject-1", cookie, StringComparison.Ordinal);

        using var replay = await client.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        await AssertCallbackFailedToSpa(replay);

        client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';', 2)[0]);
        using var session = await client.GetAsync("/auth/session", cancellationToken);
        var body = await session.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("\"authenticated\":true", body, StringComparison.Ordinal);
        Assert.DoesNotContain(Subject, body, StringComparison.Ordinal);
        Assert.DoesNotContain("organization", body, StringComparison.Ordinal);
        Assert.DoesNotContain(tokens.IdToken, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logout_requires_antiforgery_and_clears_the_cookie()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(rsa, tokens);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory);
        await LoginAsync(client, rsa, tokens, cancellationToken);

        using var forged = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        forged.Headers.TryAddWithoutValidation("Origin", "https://evil.example");
        using var forgedResponse = await client.SendAsync(forged, cancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, forgedResponse.StatusCode);

        using var current = await client.GetAsync("/auth/session", cancellationToken);
        var payload = JsonDocument.Parse(await current.Content.ReadAsStringAsync(cancellationToken));
        var csrf = payload.RootElement.GetProperty("csrf_token").GetString();
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        logout.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, csrf);
        using var loggedOut = await client.SendAsync(logout, cancellationToken);
        var body = await loggedOut.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);

        Assert.True(loggedOut.IsSuccessStatusCode);
        Assert.Null(loggedOut.Headers.Location);
        Assert.True(document.RootElement.GetProperty("logged_out").GetBoolean());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("end_session_url").ValueKind);
        Assert.Contains("no-store", loggedOut.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_antiforgery_failure_leaves_the_session()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(rsa, tokens);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory);
        await LoginAsync(client, rsa, tokens, cancellationToken);

        using var forged = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        forged.Headers.TryAddWithoutValidation("Origin", "https://evil.example");
        using var forgedResponse = await client.SendAsync(forged, cancellationToken);
        using var session = await client.GetAsync("/auth/session", cancellationToken);
        var payload = JsonDocument.Parse(await session.Content.ReadAsStringAsync(cancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, forgedResponse.StatusCode);
        Assert.True(payload.RootElement.GetProperty("authenticated").GetBoolean());
    }

    [Fact]
    public async Task Logout_returns_the_configured_end_session_url_instead_of_redirecting()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        const string endSession = "https://issuer.example/realms/flex/protocol/openid-connect/logout";
        await using var factory = CreateFactory(rsa, tokens, endSession);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory);
        await LoginAsync(client, rsa, tokens, cancellationToken);

        using var current = await client.GetAsync("/auth/session", cancellationToken);
        var csrf = JsonDocument.Parse(await current.Content.ReadAsStringAsync(cancellationToken))
            .RootElement.GetProperty("csrf_token").GetString();
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        logout.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, csrf);
        using var loggedOut = await client.SendAsync(logout, cancellationToken);
        var body = await loggedOut.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, loggedOut.StatusCode);
        Assert.Null(loggedOut.Headers.Location);
        Assert.True(document.RootElement.GetProperty("logged_out").GetBoolean());
        var idToken = tokens.IdToken ?? string.Empty;
        Assert.Equal(
            endSession
                + "?client_id=" + Uri.EscapeDataString(ClientId)
                + "&id_token_hint=" + Uri.EscapeDataString(idToken)
                + "&post_logout_redirect_uri=" + Uri.EscapeDataString("https://app.example/"),
            document.RootElement.GetProperty("end_session_url").GetString());
    }

    [Fact]
    public async Task Logout_returns_a_loopback_http_end_session_url_when_https_is_not_required()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        const string endSession = "http://localhost:18080/realms/flex-agent/protocol/openid-connect/logout";
        await using var factory = CreateFactory(rsa, tokens, endSession);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory);
        await LoginAsync(client, rsa, tokens, cancellationToken);

        using var current = await client.GetAsync("/auth/session", cancellationToken);
        var csrf = JsonDocument.Parse(await current.Content.ReadAsStringAsync(cancellationToken))
            .RootElement.GetProperty("csrf_token").GetString();
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        logout.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, csrf);
        using var loggedOut = await client.SendAsync(logout, cancellationToken);
        var body = await loggedOut.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, loggedOut.StatusCode);
        var idToken = tokens.IdToken ?? string.Empty;
        Assert.Equal(
            endSession
                + "?client_id=" + Uri.EscapeDataString(ClientId)
                + "&id_token_hint=" + Uri.EscapeDataString(idToken)
                + "&post_logout_redirect_uri=" + Uri.EscapeDataString("https://app.example/"),
            document.RootElement.GetProperty("end_session_url").GetString());
    }

    [Fact]
    public async Task Logout_does_not_return_a_non_loopback_http_end_session_url()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(
            rsa,
            tokens,
            "http://evil.example/realms/flex/protocol/openid-connect/logout");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory);
        await LoginAsync(client, rsa, tokens, cancellationToken);

        using var current = await client.GetAsync("/auth/session", cancellationToken);
        var csrf = JsonDocument.Parse(await current.Content.ReadAsStringAsync(cancellationToken))
            .RootElement.GetProperty("csrf_token").GetString();
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        logout.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, csrf);
        using var loggedOut = await client.SendAsync(logout, cancellationToken);
        using var document = JsonDocument.Parse(await loggedOut.Content.ReadAsStringAsync(cancellationToken));

        Assert.Equal(HttpStatusCode.OK, loggedOut.StatusCode);
        Assert.True(document.RootElement.GetProperty("logged_out").GetBoolean());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("end_session_url").ValueKind);
    }

    [Fact]
    public async Task Production_without_complete_oidc_configuration_fails_closed()
    {
        await using var factory = new WebApplicationFactory<ApiProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("HumanAuthentication:Enabled", "true");
        });
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/auth/login", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Unsigned_backchannel_logout_does_not_revoke_a_live_session()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(rsa, tokens);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory);
        await LoginAsync(client, rsa, tokens, cancellationToken);

        using var forged = new HttpRequestMessage(HttpMethod.Post, "/auth/backchannel-logout")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["logout_token"] = CreateUnsignedLogoutToken(),
            }),
        };
        using var forgedResponse = await client.SendAsync(forged, cancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, forgedResponse.StatusCode);

        using var session = await client.GetAsync("/auth/session", cancellationToken);
        var body = await session.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("\"authenticated\":true", body, StringComparison.Ordinal);

        using var valid = new HttpRequestMessage(HttpMethod.Post, "/auth/backchannel-logout")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["logout_token"] = CreateLogoutToken(rsa, includeSid: true),
            }),
        };
        using var validResponse = await client.SendAsync(valid, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, validResponse.StatusCode);

        using var after = await client.GetAsync("/auth/session", cancellationToken);
        var afterBody = await after.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("\"authenticated\":false", afterBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sub_only_logout_token_revokes_the_identity_and_rejects_nonce()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(rsa, tokens);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory);
        await LoginAsync(client, rsa, tokens, cancellationToken);

        using var withNonce = new HttpRequestMessage(HttpMethod.Post, "/auth/backchannel-logout")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["logout_token"] = CreateLogoutToken(rsa, includeSid: false, includeNonce: true),
            }),
        };
        using var nonceResponse = await client.SendAsync(withNonce, cancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, nonceResponse.StatusCode);

        using var stillLive = await client.GetAsync("/auth/session", cancellationToken);
        Assert.Contains(
            "\"authenticated\":true",
            await stillLive.Content.ReadAsStringAsync(cancellationToken),
            StringComparison.Ordinal);

        var subOnly = CreateLogoutToken(rsa, includeSid: false);
        using var valid = new HttpRequestMessage(HttpMethod.Post, "/auth/backchannel-logout")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["logout_token"] = subOnly,
            }),
        };
        using var validResponse = await client.SendAsync(valid, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, validResponse.StatusCode);

        using var replay = new HttpRequestMessage(HttpMethod.Post, "/auth/backchannel-logout")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["logout_token"] = subOnly,
            }),
        };
        using var replayResponse = await client.SendAsync(replay, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, replayResponse.StatusCode);

        using var after = await client.GetAsync("/auth/session", cancellationToken);
        Assert.Contains(
            "\"authenticated\":false",
            await after.Content.ReadAsStringAsync(cancellationToken),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Client_supplied_organization_is_rejected()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(rsa, tokens);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var login = await client.GetAsync("/auth/login?return_path=/work", TestContext.Current.CancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        using var callback = await client.GetAsync(
            $"/auth/callback?code=x&state={Uri.EscapeDataString(query["state"].ToString())}&organization_id={Guid.NewGuid():D}",
            TestContext.Current.CancellationToken);
        await AssertCallbackFailedToSpa(callback, endProviderSession: false);
    }

    [Fact]
    public async Task Callback_unknown_subject_redirects_to_provider_logout_without_reason_code()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(
            rsa,
            tokens,
            endSessionEndpoint: "https://issuer.example/realms/flex/protocol/openid-connect/logout");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;

        using var login = await client.GetAsync("/auth/login?return_path=/work", cancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        tokens.IdToken = CreateIdToken(rsa, query["nonce"].ToString(), subject: "unbound-subject");

        using var callback = await client.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        await AssertCallbackFailedToSpa(callback, endProviderSession: true);
        var location = callback.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("post_logout_redirect_uri=", location, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString("https://app.example/?signin=denied"), location, StringComparison.Ordinal);
        var cookies = callback.Headers.TryGetValues("Set-Cookie", out var setCookies)
            ? string.Join('\n', setCookies)
            : string.Empty;
        Assert.DoesNotContain(HumanAuthenticationHostOptions.CookieName + "=", cookies, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Callback_zero_organization_redirects_to_provider_logout_without_reason_code()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(
            rsa,
            tokens,
            endSessionEndpoint: "https://issuer.example/realms/flex/protocol/openid-connect/logout");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory, organizationCount: 0);

        using var login = await client.GetAsync("/auth/login?return_path=/work", cancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        tokens.IdToken = CreateIdToken(rsa, query["nonce"].ToString());

        using var callback = await client.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        await AssertCallbackFailedToSpa(callback, endProviderSession: true);
        var body = await callback.Content.ReadAsStringAsync(cancellationToken);
        Assert.DoesNotContain(HumanAuthenticationReasonCodes.ZeroOrganizationContext, body, StringComparison.Ordinal);
        Assert.DoesNotContain("zero_organization", callback.Headers.Location?.ToString() ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Callback_ambiguous_organization_redirects_to_provider_logout_without_reason_code()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(
            rsa,
            tokens,
            endSessionEndpoint: "https://issuer.example/realms/flex/protocol/openid-connect/logout");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory, organizationCount: 2);

        using var login = await client.GetAsync("/auth/login?return_path=/work", cancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        tokens.IdToken = CreateIdToken(rsa, query["nonce"].ToString());

        using var callback = await client.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        await AssertCallbackFailedToSpa(callback, endProviderSession: true);
        var body = await callback.Content.ReadAsStringAsync(cancellationToken);
        Assert.DoesNotContain(HumanAuthenticationReasonCodes.AmbiguousOrganizationContext, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Callback_disabled_identity_redirects_to_provider_logout_without_reason_code()
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        await using var factory = CreateFactory(
            rsa,
            tokens,
            endSessionEndpoint: "https://issuer.example/realms/flex/protocol/openid-connect/logout");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cancellationToken = TestContext.Current.CancellationToken;
        SeedBinding(factory, disableActor: true);

        using var login = await client.GetAsync("/auth/login?return_path=/work", cancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        tokens.IdToken = CreateIdToken(rsa, query["nonce"].ToString());

        using var callback = await client.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        await AssertCallbackFailedToSpa(callback, endProviderSession: true);
        var body = await callback.Content.ReadAsStringAsync(cancellationToken);
        Assert.DoesNotContain(HumanAuthenticationReasonCodes.DisabledIdentity, body, StringComparison.Ordinal);
    }

    private static async Task<string> LoginAsync(
        HttpClient client,
        RSA rsa,
        FakeOidcAuthorizationClient tokens,
        CancellationToken cancellationToken)
    {
        using var login = await client.GetAsync("/auth/login?return_path=/work", cancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        tokens.IdToken = CreateIdToken(rsa, query["nonce"].ToString());
        using var callback = await client.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        return callback.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(HumanAuthenticationHostOptions.CookieName, StringComparison.Ordinal))
            .Split(';', 2)[0];
    }

    private static void SeedBinding(
        WebApplicationFactory<ApiProgram> factory,
        Guid? actorId = null,
        Guid? organizationId = null,
        int organizationCount = 1,
        bool disableActor = false)
    {
        var bindings = factory.Services.GetRequiredService<MemoryHumanIdentityBindingStore>();
        actorId ??= Guid.NewGuid();
        bindings.RegisterActor(actorId.Value);
        for (var index = 0; index < organizationCount; index++)
        {
            bindings.GrantOrganization(actorId.Value, organizationId ?? Guid.NewGuid());
            organizationId = null;
        }

        bindings.TryProvisionAsync(
                new HumanIdentityBinding(
                    Guid.NewGuid(),
                    new ExactIssuerSubject(Issuer, Subject),
                    actorId.Value,
                    DateTimeOffset.UtcNow,
                    disableActor ? DateTimeOffset.UtcNow : null))
            .GetAwaiter()
            .GetResult();
        if (disableActor)
        {
            bindings.DisableActor(actorId.Value);
        }
    }

    private static WebApplicationFactory<ApiProgram> CreateFactory(
        RSA? rsa = null,
        FakeOidcAuthorizationClient? tokens = null,
        string? endSessionEndpoint = null)
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
            if (endSessionEndpoint is not null)
            {
                builder.UseSetting("HumanAuthentication:EndSessionEndpoint", endSessionEndpoint);
            }
            builder.UseSetting("HumanAuthentication:AcceptedAcr:0", "acr:mfa");
            builder.UseSetting("HumanAuthentication:AcceptedAmr:0", "mfa");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IOidcAuthorizationClient>(tokens);
                services.AddSingleton<IJwksKeySource>(new StaticJwksKeySource(keys));
            });
        });
    }

    private static string CreateLogoutToken(RSA rsa, bool includeSid, bool includeNonce = false)
    {
        var now = DateTimeOffset.UtcNow;
        var header = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT", kid = "test" });
        var payload = new Dictionary<string, object?>
        {
            ["iss"] = Issuer,
            ["aud"] = ClientId,
            ["sub"] = Subject,
            ["jti"] = "jti-" + Guid.NewGuid().ToString("N"),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(5).ToUnixTimeSeconds(),
            ["events"] = new Dictionary<string, object> { ["http://schemas.openid.net/event/backchannel-logout"] = new { } },
        };
        if (includeSid)
        {
            payload["sid"] = "sid-1";
        }

        if (includeNonce)
        {
            payload["nonce"] = "must-not-be-present";
        }

        var encodedHeader = Encode(Encoding.UTF8.GetBytes(header));
        var encodedPayload = Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var signingInput = $"{encodedHeader}.{encodedPayload}";
        var signature = Encode(rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        return $"{signingInput}.{signature}";
    }

    private static string CreateUnsignedLogoutToken()
    {
        var header = Encode("{}"u8.ToArray());
        var payload = Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            iss = Issuer,
            aud = ClientId,
            sid = "sid-1",
            exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),
        })));
        return $"{header}.{payload}.not-a-signature";
    }

    private static async Task AssertCallbackFailedToSpa(
        HttpResponseMessage callback,
        bool endProviderSession = false)
    {
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        var location = callback.Headers.Location?.ToString() ?? string.Empty;
        var body = await callback.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("authn.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("unknown_subject", location, StringComparison.Ordinal);
        if (endProviderSession)
        {
            Assert.Contains("/protocol/openid-connect/logout", location, StringComparison.Ordinal);
            Assert.Contains("signin%3Ddenied", location, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("id_token_hint=", location, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Equal("/?signin=denied", location);
        }
    }

    private static string CreateIdToken(RSA rsa, string nonce, string? subject = null)
    {
        var now = DateTimeOffset.UtcNow;
        var header = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT", kid = "test" });
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["iss"] = Issuer,
            ["aud"] = ClientId,
            ["sub"] = subject ?? Subject,
            ["nonce"] = nonce,
            ["sid"] = "sid-1",
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
