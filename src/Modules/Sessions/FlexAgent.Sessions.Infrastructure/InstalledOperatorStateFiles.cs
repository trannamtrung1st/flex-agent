using System.Text.Json;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public static class InstalledOperatorStateFiles
{
    public const int MaxUtf8Bytes = 262_144;
}

public static class InstalledModelDeploymentProfileFile
{
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.Ordinal)
    {
        "profileId",
        "profileVersion",
        "adapterKind",
        "adapterContractVersion",
        "approvedHttpsOrigin",
        "requestedModel",
        "resolvedModelVersion",
        "capabilityProfileId",
        "credentialMode",
        "maxOutputTokens",
        "controlTimeoutMilliseconds",
        "contentTimeoutMilliseconds",
        "maxProviderRequestAttempts",
        "providerId",
        "adapterConfigurationDigest",
    };

    public static InstalledModelDeploymentProfile[] Load(string path)
    {
        using var document = InstalledJsonArrayFile.Parse(path);
        var profiles = new List<InstalledModelDeploymentProfile>();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Installed profiles must contain objects.");
            }

            InstalledJsonObjectReader.RejectUnexpectedOrDuplicateProperties(item, AllowedProperties);
            string? adapterDigest = null;
            if (item.TryGetProperty("adapterConfigurationDigest", out var digestElement))
            {
                adapterDigest = digestElement.GetString();
            }

            var profile = InstalledModelDeploymentProfile.Create(
                InstalledJsonObjectReader.RequiredString(item, "profileId"),
                InstalledJsonObjectReader.RequiredString(item, "profileVersion"),
                InstalledJsonObjectReader.RequiredString(item, "adapterKind"),
                InstalledJsonObjectReader.RequiredString(item, "adapterContractVersion"),
                new Uri(InstalledJsonObjectReader.RequiredString(item, "approvedHttpsOrigin"), UriKind.Absolute),
                InstalledJsonObjectReader.RequiredString(item, "requestedModel"),
                InstalledJsonObjectReader.RequiredString(item, "resolvedModelVersion"),
                InstalledJsonObjectReader.RequiredString(item, "capabilityProfileId"),
                InstalledJsonObjectReader.RequiredString(item, "credentialMode"),
                InstalledJsonObjectReader.RequiredInt32(item, "maxOutputTokens"),
                TimeSpan.FromMilliseconds(InstalledJsonObjectReader.RequiredInt32(item, "controlTimeoutMilliseconds")),
                TimeSpan.FromMilliseconds(InstalledJsonObjectReader.RequiredInt32(item, "contentTimeoutMilliseconds")),
                InstalledJsonObjectReader.RequiredInt32(item, "maxProviderRequestAttempts"),
                InstalledJsonObjectReader.RequiredString(item, "providerId"),
                adapterDigest);
            var identity = $"{profile.ProfileId}\n{profile.ProfileVersion}\n{profile.ProfileDigest}";
            if (!identities.Add(identity))
            {
                throw new ArgumentException("Installed profiles must not contain duplicate identities.");
            }

            profiles.Add(profile);
        }

        return [.. profiles];
    }
}

public static class InstalledCredentialCatalogFile
{
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.Ordinal)
    {
        "bindingReference",
        "bindingVersion",
        "ownerOrganizationId",
        "providerId",
        "credentialMode",
        "revoked",
        "secretName",
    };

    public static IModelDeploymentCredentialCatalog Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new InMemoryModelDeploymentCredentialCatalog();
        }

        using var document = InstalledJsonArrayFile.Parse(path);
        var records = new List<ModelDeploymentCredentialCatalogRecord>();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Credential catalog entries must be objects.");
            }

            InstalledJsonObjectReader.RejectUnexpectedOrDuplicateProperties(item, AllowedProperties);
            var record = new ModelDeploymentCredentialCatalogRecord(
                InstalledJsonObjectReader.RequiredString(item, "bindingReference"),
                InstalledJsonObjectReader.RequiredString(item, "bindingVersion"),
                Guid.Parse(InstalledJsonObjectReader.RequiredString(item, "ownerOrganizationId")),
                InstalledJsonObjectReader.RequiredString(item, "providerId"),
                InstalledJsonObjectReader.RequiredString(item, "credentialMode"),
                InstalledJsonObjectReader.RequiredBoolean(item, "revoked"),
                InstalledJsonObjectReader.RequiredString(item, "secretName"));
            var identity = $"{record.BindingReference}\n{record.BindingVersion}";
            if (!identities.Add(identity))
            {
                throw new ArgumentException("Credential catalog must not contain duplicate identities.");
            }

            records.Add(record);
        }

        return new InMemoryModelDeploymentCredentialCatalog([.. records]);
    }
}

internal static class InstalledJsonArrayFile
{
    public static JsonDocument Parse(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var info = new FileInfo(path);
        if (!info.Exists || info.Length <= 0 || info.Length > InstalledOperatorStateFiles.MaxUtf8Bytes)
        {
            throw new ArgumentException("Installed operator state file is missing, empty, or oversized.");
        }

        var json = File.ReadAllText(path);
        if (System.Text.Encoding.UTF8.GetByteCount(json) > InstalledOperatorStateFiles.MaxUtf8Bytes)
        {
            throw new ArgumentException("Installed operator state file is oversized.");
        }

        var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            document.Dispose();
            throw new ArgumentException("Installed operator state file must be a JSON array.");
        }

        return document;
    }
}

internal static class InstalledJsonObjectReader
{
    public static void RejectUnexpectedOrDuplicateProperties(JsonElement item, IReadOnlySet<string> allowed)
    {
        ArgumentNullException.ThrowIfNull(allowed);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in item.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                throw new ArgumentException($"Installed operator state contains duplicate property '{property.Name}'.");
            }

            if (!allowed.Contains(property.Name))
            {
                throw new ArgumentException($"Installed operator state contains unexpected property '{property.Name}'.");
            }
        }
    }

    public static string RequiredString(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"Installed operator state is missing '{name}'.");
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException($"Installed operator state is missing '{name}'.");
        }

        return text;
    }

    public static int RequiredInt32(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out var number))
        {
            throw new ArgumentException($"Installed operator state is missing '{name}'.");
        }

        return number;
    }

    public static bool RequiredBoolean(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value)
            || (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
        {
            throw new ArgumentException($"Installed operator state is missing '{name}'.");
        }

        return value.GetBoolean();
    }
}
