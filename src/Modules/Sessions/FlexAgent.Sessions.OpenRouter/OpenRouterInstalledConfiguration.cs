using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenRouter;

public static class OpenRouterAdapterContracts
{
    public const string AdapterContractVersion = "sessions.openrouter.v1";
    public const string QualificationScope = "synthetic_development";
    public const string DiscoveryModel = "openrouter/free";
    public const string ChatCompletionsPath = "/api/v1/chat/completions";
    public const int MaxOutputTokens = 256;
    public const int MaxApplicationAttempts = 2;
    public const int MaxControlEnvelopeUtf8Bytes = 262_144;
    public const int MaxSseEventUtf8Bytes = 65_536;
    public const int MaxVisibleContentUtf8Bytes = 65_536;
    public const string ResponseCacheStatusHeader = "X-OpenRouter-Cache-Status";
    public static readonly Uri ApprovedOrigin = new("https://openrouter.ai/", UriKind.Absolute);
    public static readonly TimeSpan ControlTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan ContentTimeout = TimeSpan.FromSeconds(60);
}

public static class OpenRouterDestination
{
    public static Uri ChatCompletionsUri { get; } =
        new("https://openrouter.ai/api/v1/chat/completions", UriKind.Absolute);

    public static bool IsAllowed(Uri destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!string.Equals(destination.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(destination.UserInfo)
            || !string.IsNullOrEmpty(destination.Fragment)
            || !string.Equals(destination.Host, "openrouter.ai", StringComparison.OrdinalIgnoreCase)
            || EffectivePort(destination) != 443)
        {
            return false;
        }

        if (System.Net.IPAddress.TryParse(destination.Host, out _))
        {
            return false;
        }

        if (string.Equals(destination.AbsolutePath, OpenRouterAdapterContracts.ChatCompletionsPath, StringComparison.Ordinal))
        {
            return string.IsNullOrEmpty(destination.Query);
        }

        return false;
    }

    private static int EffectivePort(Uri uri) => uri.IsDefaultPort ? 443 : uri.Port;
}

public sealed record OpenRouterInstalledConfiguration(
    InstalledModelDeploymentProfile Profile,
    string ProviderSlug,
    string ExpectedReturnedProviderIdentity)
{
    public string AdapterConfigurationDigest =>
        Profile.AdapterConfigurationDigest
        ?? throw new InvalidOperationException("OpenRouter configuration is missing its adapter digest.");

    public static OpenRouterInstalledConfiguration Create(
        string profileId,
        string profileVersion,
        string requestedModel,
        string resolvedModelVersion,
        string providerSlug,
        string expectedReturnedProviderIdentity,
        string credentialMode,
        string providerId,
        int maxProviderRequestAttempts = 2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSlug);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedReturnedProviderIdentity);
        if (providerSlug.Contains(',', StringComparison.Ordinal)
            || providerSlug.Contains(' ', StringComparison.Ordinal)
            || expectedReturnedProviderIdentity.Contains(',', StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(providerSlug), "OpenRouter permits exactly one provider slug.");
        }

        if (maxProviderRequestAttempts is < 1 or > OpenRouterAdapterContracts.MaxApplicationAttempts)
        {
            throw new ArgumentOutOfRangeException(nameof(maxProviderRequestAttempts));
        }

        var adapterDigest = ComputeAdapterConfigurationDigest(providerSlug, expectedReturnedProviderIdentity);
        var profile = InstalledModelDeploymentProfile.Create(
            profileId,
            profileVersion,
            ModelDeploymentAdapterKinds.OpenRouter,
            OpenRouterAdapterContracts.AdapterContractVersion,
            OpenRouterAdapterContracts.ApprovedOrigin,
            requestedModel,
            resolvedModelVersion,
            "p0.text.structured-control",
            credentialMode,
            OpenRouterAdapterContracts.MaxOutputTokens,
            OpenRouterAdapterContracts.ControlTimeout,
            OpenRouterAdapterContracts.ContentTimeout,
            maxProviderRequestAttempts,
            providerId,
            adapterDigest);
        return new OpenRouterInstalledConfiguration(profile, providerSlug, expectedReturnedProviderIdentity);
    }

    public static string ComputeAdapterConfigurationDigest(string providerSlug, string expectedReturnedProviderIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSlug);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedReturnedProviderIdentity);
        var source = string.Join(
            "\n",
            ModelDeploymentAdapterKinds.OpenRouter,
            OpenRouterAdapterContracts.AdapterContractVersion,
            ApprovedHttpsOrigin.DigestSource(OpenRouterAdapterContracts.ApprovedOrigin),
            OpenRouterAdapterContracts.ChatCompletionsPath,
            providerSlug,
            expectedReturnedProviderIdentity,
            "allow_fallbacks=false",
            "require_parameters=true",
            "data_collection=deny",
            "zdr=true",
            "metadata=enabled",
            "cache=false");
        return ProtectedContentRef.DigestUtf8(source);
    }
}

public interface IOpenRouterInstalledConfigurationRegistry
{
    OpenRouterInstalledConfiguration? TryGet(string profileId, string profileVersion, string profileDigest);
}

public sealed class InMemoryOpenRouterInstalledConfigurationRegistry : IOpenRouterInstalledConfigurationRegistry
{
    private readonly Dictionary<string, OpenRouterInstalledConfiguration> _items = new(StringComparer.Ordinal);

    public InMemoryOpenRouterInstalledConfigurationRegistry(params OpenRouterInstalledConfiguration[] configurations)
    {
        foreach (var configuration in configurations)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            _items[Key(configuration.Profile.ProfileId, configuration.Profile.ProfileVersion, configuration.Profile.ProfileDigest)] =
                configuration;
        }
    }

    public OpenRouterInstalledConfiguration? TryGet(string profileId, string profileVersion, string profileDigest) =>
        _items.TryGetValue(Key(profileId, profileVersion, profileDigest), out var configuration) ? configuration : null;

    private static string Key(string profileId, string profileVersion, string profileDigest) =>
        $"{profileId}\n{profileVersion}\n{profileDigest}";
}
