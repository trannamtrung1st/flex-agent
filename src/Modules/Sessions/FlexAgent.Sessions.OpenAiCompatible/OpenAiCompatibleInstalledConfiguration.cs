using System.Globalization;
using System.Net;
using System.Net.Sockets;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenAiCompatible;

public static class OpenAiCompatibleAdapterContracts
{
    public const string AdapterKind = ModelDeploymentAdapterKinds.OpenAiCompatible;
    public const string AdapterContractVersion = "sessions.openai_compatible.v1";
    public const string HistoricalAdapterKind = ModelDeploymentAdapterKinds.DirectOpenAi;
    public const string HistoricalAdapterContractVersion = "sessions.openai.v1";
    public const string ChatCompletionsSuffix = "/chat/completions";
    public const string QualificationScope = "exact_profile";
    public const string LiveQualificationEnvironmentVariable = "FLEXAGENT_LIVE_OPENAI_COMPATIBLE_QUALIFICATION";
    public const string DestinationPolicyPublicOnly = "public_only";
    public const string DestinationPolicyPrivateAllowlist = "private_allowlist";

    public static readonly TimeSpan ControlTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan ContentTimeout = TimeSpan.FromSeconds(60);
}

public sealed record OpenAiCompatibleDestinationPolicy(
    string Kind,
    IReadOnlyList<string> AllowedPrivateCidrs)
{
    public static OpenAiCompatibleDestinationPolicy PublicOnly { get; } =
        new(OpenAiCompatibleAdapterContracts.DestinationPolicyPublicOnly, []);

    public static OpenAiCompatibleDestinationPolicy PrivateAllowlist(params string[] cidrs)
    {
        ArgumentNullException.ThrowIfNull(cidrs);
        if (cidrs.Length == 0)
        {
            throw new ArgumentException("Private-allowlist destination policy requires at least one CIDR.");
        }

        var normalized = cidrs
            .Select(OpenAiCompatibleCidr.Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (normalized.Length != cidrs.Length)
        {
            throw new ArgumentException("Private-allowlist CIDRs must be unique.");
        }

        return new OpenAiCompatibleDestinationPolicy(
            OpenAiCompatibleAdapterContracts.DestinationPolicyPrivateAllowlist,
            normalized);
    }

    public string IdentitySource =>
        Kind == OpenAiCompatibleAdapterContracts.DestinationPolicyPublicOnly
            ? "destination_policy=public_only"
            : "destination_policy=private_allowlist\nallowed_cidrs=" + string.Join(',', AllowedPrivateCidrs);
}

public sealed record OpenAiCompatibleInstalledConfiguration(
    InstalledModelDeploymentProfile Profile,
    string ApiBasePath,
    OpenAiCompatibleDestinationPolicy DestinationPolicy)
{
    public string AdapterConfigurationDigest =>
        Profile.AdapterConfigurationDigest
        ?? throw new InvalidOperationException("OpenAI-compatible configuration is missing its adapter digest.");

    public Uri Endpoint =>
        new(
            ApprovedHttpsOrigin.Canonicalize(Profile.ApprovedHttpsOrigin),
            ApiBasePath.TrimEnd('/') + "/");

    public string ChatCompletionsPath => ApiBasePath.TrimEnd('/') + OpenAiCompatibleAdapterContracts.ChatCompletionsSuffix;

    public static OpenAiCompatibleInstalledConfiguration Create(
        string profileId,
        string profileVersion,
        Uri approvedHttpsOrigin,
        string requestedModel,
        string resolvedModelVersion,
        string credentialMode,
        string providerId,
        string apiBasePath,
        OpenAiCompatibleDestinationPolicy? destinationPolicy = null,
        int maxOutputTokens = 256,
        TimeSpan? controlTimeout = null,
        TimeSpan? contentTimeout = null,
        int maxProviderRequestAttempts = 2)
    {
        var policy = destinationPolicy ?? OpenAiCompatibleDestinationPolicy.PublicOnly;
        var normalizedBasePath = NormalizeApiBasePath(apiBasePath);
        var adapterDigest = ComputeAdapterConfigurationDigest(approvedHttpsOrigin, normalizedBasePath, policy);
        var profile = InstalledModelDeploymentProfile.Create(
            profileId,
            profileVersion,
            OpenAiCompatibleAdapterContracts.AdapterKind,
            OpenAiCompatibleAdapterContracts.AdapterContractVersion,
            approvedHttpsOrigin,
            requestedModel,
            resolvedModelVersion,
            "p0.text.structured-control",
            credentialMode,
            maxOutputTokens,
            controlTimeout ?? OpenAiCompatibleAdapterContracts.ControlTimeout,
            contentTimeout ?? OpenAiCompatibleAdapterContracts.ContentTimeout,
            maxProviderRequestAttempts,
            providerId,
            adapterDigest);
        return new OpenAiCompatibleInstalledConfiguration(profile, normalizedBasePath, policy);
    }

    public static string ComputeAdapterConfigurationDigest(
        Uri approvedHttpsOrigin,
        string apiBasePath,
        OpenAiCompatibleDestinationPolicy destinationPolicy)
    {
        ArgumentNullException.ThrowIfNull(approvedHttpsOrigin);
        ArgumentNullException.ThrowIfNull(destinationPolicy);
        var source = string.Join(
            "\n",
            OpenAiCompatibleAdapterContracts.AdapterKind,
            OpenAiCompatibleAdapterContracts.AdapterContractVersion,
            ApprovedHttpsOrigin.DigestSource(approvedHttpsOrigin),
            NormalizeApiBasePath(apiBasePath),
            destinationPolicy.IdentitySource);
        return ProtectedContentRef.DigestUtf8(source);
    }

    public static string NormalizeApiBasePath(string apiBasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiBasePath);
        if (!apiBasePath.StartsWith('/')
            || apiBasePath.Contains('\\', StringComparison.Ordinal)
            || apiBasePath.Contains('?', StringComparison.Ordinal)
            || apiBasePath.Contains('#', StringComparison.Ordinal)
            || apiBasePath.Contains("//", StringComparison.Ordinal)
            || apiBasePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment is "." or ".."))
        {
            throw new ArgumentOutOfRangeException(nameof(apiBasePath), "API base path must be an absolute confined path.");
        }

        return apiBasePath == "/" ? "/" : apiBasePath.TrimEnd('/');
    }
}

public interface IOpenAiCompatibleInstalledConfigurationRegistry
{
    OpenAiCompatibleInstalledConfiguration? TryGet(string profileId, string profileVersion, string profileDigest);
}

public sealed class InMemoryOpenAiCompatibleInstalledConfigurationRegistry : IOpenAiCompatibleInstalledConfigurationRegistry
{
    private readonly Dictionary<string, OpenAiCompatibleInstalledConfiguration> _items = new(StringComparer.Ordinal);

    public InMemoryOpenAiCompatibleInstalledConfigurationRegistry(
        params OpenAiCompatibleInstalledConfiguration[] configurations)
    {
        foreach (var configuration in configurations)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            _items[Key(configuration.Profile.ProfileId, configuration.Profile.ProfileVersion, configuration.Profile.ProfileDigest)] =
                configuration;
        }
    }

    public OpenAiCompatibleInstalledConfiguration? TryGet(string profileId, string profileVersion, string profileDigest) =>
        _items.TryGetValue(Key(profileId, profileVersion, profileDigest), out var configuration) ? configuration : null;

    private static string Key(string profileId, string profileVersion, string profileDigest) =>
        $"{profileId}\n{profileVersion}\n{profileDigest}";
}

internal static class OpenAiCompatibleCidr
{
    public static string Normalize(string cidr)
    {
        return Format(Parse(cidr));
    }

    public static bool Contains(string cidr, IPAddress address)
    {
        var parsed = Parse(cidr);
        var candidate = OpenAiCompatibleAddressClassification.Canonicalize(address);
        if (parsed.AddressFamily != candidate.AddressFamily)
        {
            return false;
        }

        var networkBytes = parsed.Network.GetAddressBytes();
        var candidateBytes = candidate.GetAddressBytes();
        var fullBytes = parsed.PrefixLength / 8;
        var remainingBits = parsed.PrefixLength % 8;
        for (var i = 0; i < fullBytes; i++)
        {
            if (networkBytes[i] != candidateBytes[i])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)(0xFF << (8 - remainingBits));
        return (networkBytes[fullBytes] & mask) == (candidateBytes[fullBytes] & mask);
    }

    private static (IPAddress Network, int PrefixLength, AddressFamily AddressFamily) Parse(string cidr)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cidr);
        var parts = cidr.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !IPAddress.TryParse(parts[0], out var network)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var prefix))
        {
            throw new ArgumentOutOfRangeException(nameof(cidr), "CIDR must be an IPv4 or IPv6 prefix.");
        }

        network = OpenAiCompatibleAddressClassification.Canonicalize(network);
        var maxPrefix = network.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        if (prefix is < 0 || prefix > maxPrefix)
        {
            throw new ArgumentOutOfRangeException(nameof(cidr), "CIDR prefix length is out of range.");
        }

        return (network, prefix, network.AddressFamily);
    }

    private static string Format((IPAddress Network, int PrefixLength, AddressFamily AddressFamily) value) =>
        value.Network + "/" + value.PrefixLength.ToString(CultureInfo.InvariantCulture);
}
