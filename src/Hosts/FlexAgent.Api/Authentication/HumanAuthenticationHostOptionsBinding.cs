using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.Api;

internal static class HumanAuthenticationHostOptionsBinding
{
    public static HumanAuthenticationHostOptions CreateSnapshot(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = new HumanAuthenticationHostOptions();
        Configure(options, configuration, environment);
        return options;
    }

    public static void Configure(
        HumanAuthenticationHostOptions options,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var section = configuration.GetSection("HumanAuthentication");
        var acceptedAcr = section.GetSection("AcceptedAcr").Get<string[]>() ?? [];
        var acceptedAmr = section.GetSection("AcceptedAmr").Get<string[]>() ?? [];
        var trustedProxies = section.GetSection("TrustedProxies").Get<string[]>() ?? [];
        var requireHttps = !(environment.IsDevelopment() || environment.IsEnvironment("Testing"));
        var parsedSkew = TimeSpan.FromSeconds(
            int.TryParse(section["ClockSkewSeconds"], out var skew) ? skew : 60);
        var parsedIdle = TimeSpan.FromMinutes(
            int.TryParse(section["InactivityMinutes"], out var idle) ? idle : 30);
        var parsedAbsolute = TimeSpan.FromHours(
            int.TryParse(section["AbsoluteLifetimeHours"], out var absolute) ? absolute : 12);

        options.Enabled = string.Equals(section["Enabled"], "true", StringComparison.OrdinalIgnoreCase);
        options.Issuer = section["Issuer"] ?? string.Empty;
        options.ClientId = section["ClientId"] ?? string.Empty;
        options.AuthorizationEndpoint = section["AuthorizationEndpoint"] ?? string.Empty;
        options.TokenEndpoint = section["TokenEndpoint"] ?? string.Empty;
        options.JwksUri = section["JwksUri"] ?? string.Empty;
        options.EndSessionEndpoint = section["EndSessionEndpoint"];
        options.RedirectUri = section["RedirectUri"] ?? string.Empty;
        options.LifecycleBridgeKey = section["LifecycleBridgeKey"];
        options.SecretDirectory = section["SecretDirectory"];
        options.AcceptedAcr = acceptedAcr.ToHashSet(StringComparer.Ordinal);
        options.AcceptedAmr = acceptedAmr.ToHashSet(StringComparer.Ordinal);
        options.ClockSkew = OidcValidationProfile.MaximumClockSkew < parsedSkew
            ? OidcValidationProfile.MaximumClockSkew
            : parsedSkew;
        options.Inactivity = ApplicationSessionPolicy.BoundInactivity(parsedIdle);
        options.AbsoluteLifetime = ApplicationSessionPolicy.BoundAbsoluteLifetime(parsedAbsolute);
        options.TrustedProxies = trustedProxies;
        options.RequireHttpsEndpoints = requireHttps;
    }
}
