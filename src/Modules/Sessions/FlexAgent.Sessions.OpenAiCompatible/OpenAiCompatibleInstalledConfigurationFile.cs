using System.Text.Json;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenAiCompatible;

public static class OpenAiCompatibleInstalledConfigurationFile
{
    public static OpenAiCompatibleInstalledConfiguration[] Load(
        string path,
        IReadOnlyList<InstalledModelDeploymentProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        using var document = ParseArray(path);
        var configurations = new List<OpenAiCompatibleInstalledConfiguration>();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("OpenAI-compatible configurations must contain objects.");
            }

            var profileId = RequiredString(item, "profileId");
            var profileVersion = RequiredString(item, "profileVersion");
            var profileDigest = RequiredString(item, "profileDigest");
            var adapterDigest = RequiredString(item, "adapterConfigurationDigest");
            var apiBasePath = RequiredString(item, "apiBasePath");
            var destinationPolicyKind = RequiredString(item, "destinationPolicy");
            var profile = profiles.SingleOrDefault(candidate =>
                string.Equals(candidate.ProfileId, profileId, StringComparison.Ordinal)
                && string.Equals(candidate.ProfileVersion, profileVersion, StringComparison.Ordinal)
                && string.Equals(candidate.ProfileDigest, profileDigest, StringComparison.Ordinal))
                ?? throw new ArgumentException("OpenAI-compatible configuration does not match an installed profile.");
            if (!string.Equals(profile.AdapterKind, OpenAiCompatibleAdapterContracts.AdapterKind, StringComparison.Ordinal)
                || !string.Equals(profile.AdapterContractVersion, OpenAiCompatibleAdapterContracts.AdapterContractVersion, StringComparison.Ordinal))
            {
                throw new ArgumentException("OpenAI-compatible configuration cannot bind a legacy or foreign adapter identity.");
            }

            if (!string.Equals(profile.AdapterConfigurationDigest, adapterDigest, StringComparison.Ordinal))
            {
                throw new ArgumentException("OpenAI-compatible adapter-configuration digest does not match the installed profile.");
            }

            var policy = destinationPolicyKind switch
            {
                OpenAiCompatibleAdapterContracts.DestinationPolicyPublicOnly => OpenAiCompatibleDestinationPolicy.PublicOnly,
                OpenAiCompatibleAdapterContracts.DestinationPolicyPrivateAllowlist =>
                    OpenAiCompatibleDestinationPolicy.PrivateAllowlist(ReadCidrs(item)),
                _ => throw new ArgumentException("OpenAI-compatible destination policy is not recognized."),
            };
            var created = OpenAiCompatibleInstalledConfiguration.Create(
                profile.ProfileId,
                profile.ProfileVersion,
                profile.ApprovedHttpsOrigin,
                profile.RequestedModel,
                profile.ResolvedModelVersion,
                profile.CredentialMode,
                profile.ProviderId,
                apiBasePath,
                policy,
                profile.MaxOutputTokens,
                profile.ControlTimeout,
                profile.ContentTimeout,
                profile.MaxProviderRequestAttempts);
            if (!string.Equals(created.Profile.ProfileDigest, profile.ProfileDigest, StringComparison.Ordinal)
                || !string.Equals(created.AdapterConfigurationDigest, adapterDigest, StringComparison.Ordinal))
            {
                throw new ArgumentException("OpenAI-compatible configuration digest mismatch.");
            }

            var identity = $"{profileId}\n{profileVersion}\n{profileDigest}";
            if (!identities.Add(identity))
            {
                throw new ArgumentException("OpenAI-compatible configurations must not contain duplicate identities.");
            }

            configurations.Add(created);
        }

        if (configurations.Count != profiles.Count)
        {
            throw new ArgumentException("OpenAI-compatible configurations must match installed profiles one-to-one.");
        }

        return [.. configurations];
    }

    private static string[] ReadCidrs(JsonElement item)
    {
        if (!item.TryGetProperty("allowedPrivateCidrs", out var cidrs)
            || cidrs.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Private-allowlist destination policy requires allowedPrivateCidrs.");
        }

        return [.. cidrs.EnumerateArray().Select(element =>
        {
            if (element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString()))
            {
                throw new ArgumentException("Private-allowlist CIDRs must be strings.");
            }

            return element.GetString()!;
        })];
    }

    private static JsonDocument ParseArray(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var info = new FileInfo(path);
        if (!info.Exists || info.Length <= 0 || info.Length > 262_144)
        {
            throw new ArgumentException("OpenAI-compatible configuration file is missing, empty, or oversized.");
        }

        var json = File.ReadAllText(path);
        var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            document.Dispose();
            throw new ArgumentException("OpenAI-compatible configuration file must be a JSON array.");
        }

        return document;
    }

    private static string RequiredString(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"OpenAI-compatible configuration is missing '{name}'.");
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException($"OpenAI-compatible configuration is missing '{name}'.");
        }

        return text;
    }
}
