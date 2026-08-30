using FlexAgent.Api;

namespace FlexAgent.Runtime.Tests;

public sealed class HumanAuthenticationEndSessionTests
{
    private const string IdTokenHint = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ0ZXN0In0.sig";

    [Fact]
    public void Https_end_session_includes_client_id_post_logout_origin_and_id_token_hint()
    {
        var options = CompleteHttps();
        options.EndSessionEndpoint = "https://issuer.example/realms/flex/protocol/openid-connect/logout";

        Assert.Equal(
            "https://issuer.example/realms/flex/protocol/openid-connect/logout"
                + "?client_id=flex-agent-api"
                + "&id_token_hint=" + Uri.EscapeDataString(IdTokenHint)
                + "&post_logout_redirect_uri=" + Uri.EscapeDataString("https://app.example/"),
            options.TryBrowserEndSessionUrl(IdTokenHint));
        Assert.Equal(
            "https://issuer.example/realms/flex/protocol/openid-connect/logout"
                + "?client_id=flex-agent-api"
                + "&id_token_hint=" + Uri.EscapeDataString(IdTokenHint)
                + "&post_logout_redirect_uri=" + Uri.EscapeDataString("https://app.example/?signin=denied"),
            options.TryBrowserEndSessionUrl(IdTokenHint, signInDenied: true));
    }

    [Fact]
    public void Loopback_http_end_session_is_returned_when_https_is_not_required()
    {
        var options = CompleteHttps();
        options.RequireHttpsEndpoints = false;
        options.EndSessionEndpoint = "http://localhost:18080/realms/flex-agent/protocol/openid-connect/logout";
        options.RedirectUri = "http://localhost:18080/auth/callback";

        Assert.Equal(
            "http://localhost:18080/realms/flex-agent/protocol/openid-connect/logout"
                + "?client_id=flex-agent-api"
                + "&id_token_hint=" + Uri.EscapeDataString(IdTokenHint)
                + "&post_logout_redirect_uri=" + Uri.EscapeDataString("http://localhost:18080/"),
            options.TryBrowserEndSessionUrl(IdTokenHint));
    }

    [Theory]
    [InlineData("http://127.0.0.1:18080/realms/flex-agent/protocol/openid-connect/logout")]
    [InlineData("http://[::1]:18080/realms/flex-agent/protocol/openid-connect/logout")]
    public void Loopback_literal_addresses_are_accepted_when_https_is_not_required(string endSession)
    {
        var options = CompleteHttps();
        options.RequireHttpsEndpoints = false;
        options.EndSessionEndpoint = endSession;

        Assert.StartsWith(endSession + "?client_id=", options.TryBrowserEndSessionUrl(IdTokenHint), StringComparison.Ordinal);
        Assert.Contains("id_token_hint=", options.TryBrowserEndSessionUrl(IdTokenHint), StringComparison.Ordinal);
    }

    [Fact]
    public void End_session_omits_id_token_hint_when_not_available()
    {
        var options = CompleteHttps();
        options.EndSessionEndpoint = "https://issuer.example/realms/flex/protocol/openid-connect/logout";

        Assert.Equal(
            "https://issuer.example/realms/flex/protocol/openid-connect/logout"
                + "?client_id=flex-agent-api"
                + "&post_logout_redirect_uri=" + Uri.EscapeDataString("https://app.example/"),
            options.TryBrowserEndSessionUrl());
    }

    [Fact]
    public void Non_loopback_http_end_session_is_never_returned()
    {
        var options = CompleteHttps();
        options.RequireHttpsEndpoints = false;
        options.EndSessionEndpoint = "http://evil.example/realms/flex/protocol/openid-connect/logout";

        Assert.Null(options.TryBrowserEndSessionUrl(IdTokenHint));
    }

    [Fact]
    public void Loopback_http_end_session_is_rejected_when_https_is_required()
    {
        var options = CompleteHttps();
        options.RequireHttpsEndpoints = true;
        options.EndSessionEndpoint = "http://localhost:18080/realms/flex-agent/protocol/openid-connect/logout";

        Assert.Null(options.TryBrowserEndSessionUrl(IdTokenHint));
    }

    [Fact]
    public void End_session_with_userinfo_is_rejected()
    {
        var options = CompleteHttps();
        options.EndSessionEndpoint = "https://user:secret@issuer.example/logout";

        Assert.Null(options.TryBrowserEndSessionUrl(IdTokenHint));
    }

    private static HumanAuthenticationHostOptions CompleteHttps() =>
        new()
        {
            Enabled = true,
            Issuer = "https://issuer.example/realms/flex",
            ClientId = "flex-agent-api",
            AuthorizationEndpoint = "https://issuer.example/realms/flex/protocol/openid-connect/auth",
            TokenEndpoint = "https://issuer.example/realms/flex/protocol/openid-connect/token",
            JwksUri = "https://issuer.example/realms/flex/protocol/openid-connect/certs",
            RedirectUri = "https://app.example/auth/callback",
            RequireHttpsEndpoints = true,
        };
}
