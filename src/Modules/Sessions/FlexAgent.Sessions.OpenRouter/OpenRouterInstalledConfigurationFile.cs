using System.Text.Json;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenRouter;

public static class OpenRouterInstalledConfigurationFile
{
    public static OpenRouterInstalledConfiguration[] Load(
        string path,
        IReadOnlyList<InstalledModelDeploymentProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        using var document = ParseArray(path);
        var configurations = new List<OpenRouterInstalledConfiguration>();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("OpenRouter configurations must contain objects.");
            }

            var profileId = RequiredString(item, "profileId");
            var profileVersion = RequiredString(item, "profileVersion");
            var profileDigest = RequiredString(item, "profileDigest");
            var adapterDigest = RequiredString(item, "adapterConfigurationDigest");
            var providerSlug = RequiredString(item, "providerSlug");
            var expectedIdentity = RequiredString(item, "expectedReturnedProviderIdentity");
            var profile = profiles.SingleOrDefault(candidate =>
                string.Equals(candidate.ProfileId, profileId, StringComparison.Ordinal)
                && string.Equals(candidate.ProfileVersion, profileVersion, StringComparison.Ordinal)
                && string.Equals(candidate.ProfileDigest, profileDigest, StringComparison.Ordinal))
                ?? throw new ArgumentException("OpenRouter configuration does not match an installed profile.");
            if (!string.Equals(profile.AdapterConfigurationDigest, adapterDigest, StringComparison.Ordinal))
            {
                throw new ArgumentException("OpenRouter adapter-configuration digest does not match the installed profile.");
            }

            var created = OpenRouterInstalledConfiguration.Create(
                profile.ProfileId,
                profile.ProfileVersion,
                profile.RequestedModel,
                profile.ResolvedModelVersion,
                providerSlug,
                expectedIdentity,
                profile.CredentialMode,
                profile.ProviderId,
                profile.MaxProviderRequestAttempts,
                OpenRouterRequestPolicy.ForInstalledProfile(profile));
            if (!string.Equals(created.Profile.ProfileDigest, profile.ProfileDigest, StringComparison.Ordinal)
                || !string.Equals(created.AdapterConfigurationDigest, adapterDigest, StringComparison.Ordinal))
            {
                throw new ArgumentException("OpenRouter configuration digest mismatch.");
            }

            var identity = $"{profileId}\n{profileVersion}\n{profileDigest}";
            if (!identities.Add(identity))
            {
                throw new ArgumentException("OpenRouter configurations must not contain duplicate identities.");
            }

            configurations.Add(created);
        }

        return [.. configurations];
    }

    private static JsonDocument ParseArray(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var info = new FileInfo(path);
        if (!info.Exists || info.Length <= 0 || info.Length > 262_144)
        {
            throw new ArgumentException("OpenRouter configuration file is missing, empty, or oversized.");
        }

        var json = File.ReadAllText(path);
        var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            document.Dispose();
            throw new ArgumentException("OpenRouter configuration file must be a JSON array.");
        }

        return document;
    }

    private static string RequiredString(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"OpenRouter configuration is missing '{name}'.");
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException($"OpenRouter configuration is missing '{name}'.");
        }

        return text;
    }
}
