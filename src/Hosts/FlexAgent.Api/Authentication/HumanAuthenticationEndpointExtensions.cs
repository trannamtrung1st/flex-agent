using System.Security.Cryptography;
using System.Text;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using Microsoft.AspNetCore.Antiforgery;

namespace FlexAgent.Api;

public static class HumanAuthenticationEndpointExtensions
{
    public static IEndpointRouteBuilder MapHumanAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/auth/login", StartLogin);
        endpoints.MapGet("/auth/callback", CompleteLogin);
        endpoints.MapGet("/auth/session", GetCurrentSession);
        endpoints.MapPost("/auth/logout", Logout);
        endpoints.MapPost("/auth/backchannel-logout", BackChannelLogout);
        endpoints.MapPost("/internal/auth/provider-lifecycle", ProviderLifecycle);
        return endpoints;
    }

    private static async Task StartLogin(
        HttpContext context,
        IOidcLoginTransactionStore transactions,
        ILookupDigestCalculator digests,
        HumanAuthenticationHostOptions options,
        IDatabaseClock clock)
    {
        if (!options.IsComplete)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        if (!SafeReturnPaths.TryNormalize(context.Request.Query["return_path"].FirstOrDefault(), out var returnPath))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = HumanAuthenticationReasonCodes.UnsafeReturnPath });
            return;
        }

        var now = await clock.GetUtcNowAsync(context.RequestAborted);
        var state = OpaqueSessionCredential.Create();
        var correlation = OpaqueSessionCredential.Create();
        var nonce = OpaqueSessionCredential.Create();
        var verifier = OpaqueSessionCredential.Create();
        await transactions.CreateAsync(
            new OidcLoginTransaction(
                Guid.NewGuid(),
                digests.Compute(state),
                digests.Compute(correlation),
                nonce,
                verifier,
                returnPath,
                now.AddMinutes(10),
                Guid.NewGuid()),
            context.RequestAborted);
        AppendCorrelationCookie(context, correlation, options, persistent: true);

        var challenge = Sha256Base64Url(verifier);
        var location = options.AuthorizationEndpoint
            + "?response_type=code"
            + "&client_id=" + Uri.EscapeDataString(options.ClientId)
            + "&redirect_uri=" + Uri.EscapeDataString(options.RedirectUri)
            + "&scope=openid"
            + "&state=" + Uri.EscapeDataString(state)
            + "&nonce=" + Uri.EscapeDataString(nonce)
            + "&code_challenge=" + Uri.EscapeDataString(challenge)
            + "&code_challenge_method=S256";
        context.Response.Redirect(location);
    }

    private static async Task CompleteLogin(
        HttpContext context,
        IOidcLoginTransactionStore transactions,
        IOidcAuthorizationClient tokens,
        IJwksKeySource jwks,
        IHumanAuthenticationCoordinator coordinator,
        ILookupDigestCalculator digests,
        HumanAuthenticationHostOptions options,
        IDatabaseClock clock,
        TimeProvider timeProvider)
    {
        if (!options.IsComplete)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        var code = context.Request.Query["code"].FirstOrDefault();
        var state = context.Request.Query["state"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (Guid.TryParse(context.Request.Query["organization_id"].FirstOrDefault(), out var clientOrganization))
        {
            ClearCorrelationCookie(context, options);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new { error = HumanAuthenticationReasonCodes.ClientSuppliedOrganizationRejected });
            return;
        }

        var presentedCorrelation = context.Request.Cookies[HumanAuthenticationHostOptions.CorrelationCookieName];
        if (string.IsNullOrWhiteSpace(presentedCorrelation))
        {
            ClearCorrelationCookie(context, options);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var now = await clock.GetUtcNowAsync(context.RequestAborted);
        var transaction = await transactions.ConsumeAsync(
            digests.Compute(state),
            digests.Compute(presentedCorrelation),
            now,
            context.RequestAborted);
        if (transaction is null)
        {
            ClearCorrelationCookie(context, options);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new { error = HumanAuthenticationReasonCodes.ReplayOrConsumedTransaction });
            return;
        }

        var exchange = await tokens.ExchangeAuthorizationCodeAsync(code, transaction.CodeVerifier, context.RequestAborted);
        if (exchange.IdToken is null)
        {
            ClearCorrelationCookie(context, options);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = exchange.ErrorReason });
            return;
        }

        using var keys = await jwks.TryGetKeysAsync(
            options.JwksUri,
            OidcIdTokenValidator.TryReadSigningKeyId(exchange.IdToken),
            context.RequestAborted);
        if (keys is null)
        {
            ClearCorrelationCookie(context, options);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { error = HumanAuthenticationReasonCodes.ProviderUnavailable });
            return;
        }

        var validated = OidcIdTokenValidator.Validate(
            exchange.IdToken,
            transaction.Nonce,
            options.ValidationProfile,
            keys.Keys,
            timeProvider);
        if (!validated.Succeeded || validated.Token is null)
        {
            ClearCorrelationCookie(context, options);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = validated.ReasonCode });
            return;
        }

        var completed = await coordinator.CompleteLoginAsync(
            new ValidatedHumanLogin(
                validated.Token.Identity,
                validated.Token.Strength,
                validated.Token.ProviderSessionId,
                validated.Token.IssuedAt),
            clientSuppliedOrganizationId: null,
            transaction.CorrelationId,
            context.RequestAborted);
        if (!completed.Succeeded || completed.RawCredential is null)
        {
            ClearCorrelationCookie(context, options);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = completed.ReasonCode });
            return;
        }

        ClearCorrelationCookie(context, options);
        AppendSessionCookie(context, completed.RawCredential, options, persistent: true);
        context.Response.Redirect(transaction.ReturnPath);
    }

    private static async Task GetCurrentSession(
        HttpContext context,
        IHumanAuthenticationCoordinator coordinator,
        IAntiforgery antiforgery,
        HumanAuthenticationHostOptions options)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        var credential = context.Request.Cookies[HumanAuthenticationHostOptions.CookieName];
        var session = string.IsNullOrWhiteSpace(credential)
            ? null
            : await coordinator.AuthenticateAsync(credential, advanceActivity: true, context.RequestAborted);
        await context.Response.WriteAsJsonAsync(new
        {
            authenticated = session is not null,
            authentication_state = session is null ? "anonymous" : "active",
            csrf_token = tokens.RequestToken,
            mfa_present = session is not null
                && session.Strength.HasRecognizedEvidence(options.AcceptedAcr, options.AcceptedAmr),
        });
    }

    private static async Task Logout(
        HttpContext context,
        IHumanAuthenticationCoordinator coordinator,
        IAntiforgery antiforgery,
        HumanAuthenticationHostOptions options)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        var credential = context.Request.Cookies[HumanAuthenticationHostOptions.CookieName];
        if (!string.IsNullOrWhiteSpace(credential))
        {
            await coordinator.LogoutAsync(credential, Guid.NewGuid(), context.RequestAborted);
        }

        AppendSessionCookie(context, string.Empty, options, persistent: false);
        if (!string.IsNullOrWhiteSpace(options.EndSessionEndpoint))
        {
            context.Response.Redirect(options.EndSessionEndpoint + "?client_id=" + Uri.EscapeDataString(options.ClientId));
            return;
        }

        await context.Response.WriteAsJsonAsync(new { logged_out = true });
    }

    private static async Task BackChannelLogout(
        HttpContext context,
        IHumanAuthenticationCoordinator coordinator,
        IJwksKeySource jwks,
        HumanAuthenticationHostOptions options,
        TimeProvider timeProvider)
    {
        if (!options.IsComplete)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var logoutToken = form["logout_token"].FirstOrDefault();
        using var keys = await jwks.TryGetKeysAsync(
            options.JwksUri,
            OidcIdTokenValidator.TryReadSigningKeyId(logoutToken),
            context.RequestAborted);
        if (keys is null || string.IsNullOrWhiteSpace(logoutToken))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var validated = OidcIdTokenValidator.ValidateLogoutToken(
            logoutToken,
            options.ValidationProfile,
            keys.Keys,
            timeProvider);
        if (!validated.Succeeded || validated.LogoutToken is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var applied = await coordinator.ApplyBackChannelLogoutAsync(
            validated.LogoutToken,
            Guid.NewGuid(),
            context.RequestAborted);
        context.Response.StatusCode = applied.Accepted
            ? StatusCodes.Status204NoContent
            : StatusCodes.Status400BadRequest;
    }

    private static async Task ProviderLifecycle(
        HttpContext context,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options)
    {
        var presented = context.Request.Headers[HumanAuthenticationHostOptions.LifecycleKeyHeaderName]
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(options.LifecycleBridgeKey)
            || !string.Equals(presented, options.LifecycleBridgeKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var body = await context.Request.ReadFromJsonAsync<ProviderLifecycleRequest>(context.RequestAborted);
        if (body is null || string.IsNullOrWhiteSpace(body.Subject))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var identity = ExactIssuerSubject.TryCreate(options.Issuer, body.Subject);
        if (identity is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await coordinator.ApplyAccountDisablementAsync(identity, Guid.NewGuid(), context.RequestAborted);
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private static void AppendCorrelationCookie(
        HttpContext context,
        string value,
        HumanAuthenticationHostOptions options,
        bool persistent) =>
        context.Response.Cookies.Append(
            HumanAuthenticationHostOptions.CorrelationCookieName,
            value,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps || options.RequireHttpsEndpoints,
                SameSite = SameSiteMode.Lax,
                Path = "/auth",
                Expires = persistent ? DateTimeOffset.UtcNow.AddMinutes(10) : DateTimeOffset.UnixEpoch,
            });

    private static void ClearCorrelationCookie(HttpContext context, HumanAuthenticationHostOptions options) =>
        AppendCorrelationCookie(context, string.Empty, options, persistent: false);

    public static void AppendSessionCookie(
        HttpContext context,
        string value,
        HumanAuthenticationHostOptions options,
        bool persistent)
    {
        context.Response.Cookies.Append(
            HumanAuthenticationHostOptions.CookieName,
            value,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps || options.RequireHttpsEndpoints,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = persistent ? DateTimeOffset.UtcNow.Add(options.AbsoluteLifetime) : DateTimeOffset.UnixEpoch,
            });
    }

    private static string Sha256Base64Url(string value)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(value));
        return OpaqueSessionCredential.Base64UrlEncode(hash);
    }

    private sealed record ProviderLifecycleRequest(string Subject);
}
