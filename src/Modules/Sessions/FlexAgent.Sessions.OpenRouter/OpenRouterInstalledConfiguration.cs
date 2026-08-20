using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenRouter;

public static class OpenRouterAdapterContracts
{
    public const string AdapterContractVersion = "sessions.openrouter.v1";
    public const string QualificationScope = "synthetic_development";
    public const string DiscoveryModel = "openrouter/free";
    public const string ChatCompletionsPath = "/api/v1/chat/completions";
    public const int MaxOutputTokens = 256;
    public const int Phase21MaxOutputTokens = 1024;
    public const int VisibleContentAcceptanceMaxOutputTokens = 256;
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

public sealed record OpenRouterRequestPolicy(int MaxOutputTokens, string? ReasoningEffort, bool ReasoningExcluded)
{
    public static OpenRouterRequestPolicy Default { get; } =
        new(OpenRouterAdapterContracts.MaxOutputTokens, null, false);

    public static OpenRouterRequestPolicy Phase21GptOss { get; } =
        new(OpenRouterAdapterContracts.Phase21MaxOutputTokens, "low", true);

    public static OpenRouterRequestPolicy ForInstalledProfile(InstalledModelDeploymentProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.MaxOutputTokens == OpenRouterAdapterContracts.MaxOutputTokens)
        {
            return Default;
        }

        if (profile.MaxOutputTokens == OpenRouterAdapterContracts.Phase21MaxOutputTokens)
        {
            return Phase21GptOss;
        }

        throw new ArgumentOutOfRangeException(
            nameof(profile),
            "OpenRouter installed profiles permit only the default 256-token policy or the Phase 21 1,024-token GPT-OSS policy.");
    }
}

public sealed record OpenRouterInstalledConfiguration(
    InstalledModelDeploymentProfile Profile,
    string ProviderSlug,
    string ExpectedReturnedProviderIdentity,
    OpenRouterRequestPolicy RequestPolicy)
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
        int maxProviderRequestAttempts = 2,
        OpenRouterRequestPolicy? requestPolicy = null)
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

        var policy = requestPolicy ?? OpenRouterRequestPolicy.Default;
        if (!IsKnownRequestPolicy(policy))
        {
            throw new ArgumentOutOfRangeException(nameof(requestPolicy), "OpenRouter request policy is not an approved installed policy.");
        }

        if (policy == OpenRouterRequestPolicy.Phase21GptOss
            && (!string.Equals(requestedModel, OpenRouterLiveQualification.GptOssDarkbloomModel, StringComparison.Ordinal)
                || !string.Equals(resolvedModelVersion, OpenRouterLiveQualification.GptOssDarkbloomModel, StringComparison.Ordinal)
                || !string.Equals(providerSlug, OpenRouterLiveQualification.GptOssDarkbloomProviderSlug, StringComparison.Ordinal)
                || !string.Equals(expectedReturnedProviderIdentity, OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity, StringComparison.Ordinal)))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestPolicy),
                "The Phase 21 1,024-token reasoning policy is bound to the approved GPT-OSS/Darkbloom identity.");
        }

        var adapterDigest = ComputeAdapterConfigurationDigest(providerSlug, expectedReturnedProviderIdentity, policy);
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
            policy.MaxOutputTokens,
            OpenRouterAdapterContracts.ControlTimeout,
            OpenRouterAdapterContracts.ContentTimeout,
            maxProviderRequestAttempts,
            providerId,
            adapterDigest);
        return new OpenRouterInstalledConfiguration(profile, providerSlug, expectedReturnedProviderIdentity, policy);
    }

    public static string ComputeAdapterConfigurationDigest(
        string providerSlug,
        string expectedReturnedProviderIdentity,
        OpenRouterRequestPolicy? requestPolicy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSlug);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedReturnedProviderIdentity);
        var policy = requestPolicy ?? OpenRouterRequestPolicy.Default;
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
            "data_collection=allow",
            "zdr=false",
            "metadata=enabled",
            "cache=false");
        if (policy == OpenRouterRequestPolicy.Phase21GptOss)
        {
            source = string.Join(
                "\n",
                source,
                "max_tokens=1024",
                "reasoning.effort=low",
                "reasoning.exclude=true");
        }

        return ProtectedContentRef.DigestUtf8(source);
    }

    private static bool IsKnownRequestPolicy(OpenRouterRequestPolicy policy) =>
        policy == OpenRouterRequestPolicy.Default || policy == OpenRouterRequestPolicy.Phase21GptOss;
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
