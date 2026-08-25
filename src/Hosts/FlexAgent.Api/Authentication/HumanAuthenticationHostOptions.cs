using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.Api;

public sealed class HumanAuthenticationHostOptions
{
    public const string CookieName = "flex_agent_application_session";
    public const string CorrelationCookieName = "flex_agent_oidc_correlation";
    public const string AntiforgeryCookieName = "flex_agent_antiforgery";
    public const string AntiforgeryHeaderName = "X-Flex-CSRF";
    public const string LifecycleKeyHeaderName = "X-Flex-Auth-Lifecycle-Key";

    public bool Enabled { get; set; }

    public string Issuer { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string AuthorizationEndpoint { get; set; } = string.Empty;

    public string TokenEndpoint { get; set; } = string.Empty;

    public string JwksUri { get; set; } = string.Empty;

    public string? EndSessionEndpoint { get; set; }

    public string RedirectUri { get; set; } = string.Empty;

    public string? LifecycleBridgeKey { get; set; }

    public IReadOnlySet<string> AcceptedAcr { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlySet<string> AcceptedAmr { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(60);

    public TimeSpan Inactivity { get; set; } = ApplicationSessionPolicy.MaximumInactivity;

    public TimeSpan AbsoluteLifetime { get; set; } = ApplicationSessionPolicy.MaximumAbsoluteLifetime;

    public IReadOnlyList<string> TrustedProxies { get; set; } = [];

    public bool RequireHttpsEndpoints { get; set; } = true;

    public string? SecretDirectory { get; set; }

    public bool IsComplete =>
        Enabled
        && !string.IsNullOrWhiteSpace(Issuer)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(AuthorizationEndpoint)
        && !string.IsNullOrWhiteSpace(TokenEndpoint)
        && !string.IsNullOrWhiteSpace(JwksUri)
        && !string.IsNullOrWhiteSpace(RedirectUri)
        && (!RequireHttpsEndpoints
            || (IsExactHttps(Issuer)
                && IsExactHttps(AuthorizationEndpoint)
                && IsExactHttps(TokenEndpoint)
                && IsExactHttps(JwksUri)
                && IsExactHttps(RedirectUri)
                && (string.IsNullOrWhiteSpace(EndSessionEndpoint) || IsExactHttps(EndSessionEndpoint))));

    public OidcValidationProfile ValidationProfile =>
        new(Issuer, ClientId, ClockSkew, TimeSpan.FromHours(1));

    public HumanAuthenticationOptions SessionOptions =>
        new()
        {
            Issuer = Issuer,
            Inactivity = Inactivity,
            AbsoluteLifetime = AbsoluteLifetime,
        };

    private static bool IsExactHttps(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && !uri.AllowAutoredirectionLikeUserInfo();
}

file static class UriExtensions
{
    public static bool AllowAutoredirectionLikeUserInfo(this Uri uri) =>
        !string.IsNullOrEmpty(uri.UserInfo);
}
