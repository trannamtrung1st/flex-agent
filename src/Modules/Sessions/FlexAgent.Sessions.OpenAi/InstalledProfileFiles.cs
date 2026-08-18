using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenAi;

public static class InstalledModelDeploymentProfileFile
{
    public static InstalledModelDeploymentProfile[] Load(string path)
    {
        var json = File.ReadAllText(path);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var profiles = new List<InstalledModelDeploymentProfile>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            profiles.Add(InstalledModelDeploymentProfile.Create(
                item.GetProperty("profileId").GetString()!,
                item.GetProperty("profileVersion").GetString()!,
                item.GetProperty("adapterKind").GetString()!,
                item.GetProperty("adapterContractVersion").GetString()!,
                new Uri(item.GetProperty("approvedHttpsOrigin").GetString()!, UriKind.Absolute),
                item.GetProperty("requestedModel").GetString()!,
                item.GetProperty("resolvedModelVersion").GetString()!,
                item.GetProperty("capabilityProfileId").GetString()!,
                item.GetProperty("credentialMode").GetString()!,
                item.GetProperty("maxOutputTokens").GetInt32(),
                TimeSpan.FromMilliseconds(item.GetProperty("controlTimeoutMilliseconds").GetInt32()),
                TimeSpan.FromMilliseconds(item.GetProperty("contentTimeoutMilliseconds").GetInt32()),
                item.GetProperty("maxProviderRequestAttempts").GetInt32(),
                item.GetProperty("providerId").GetString()!));
        }

        return [.. profiles];
    }
}

public static class InstalledCredentialCatalogFile
{
    public static IModelDeploymentCredentialCatalog Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new InMemoryModelDeploymentCredentialCatalog();
        }

        var json = File.ReadAllText(path);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var records = new List<ModelDeploymentCredentialCatalogRecord>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            records.Add(new ModelDeploymentCredentialCatalogRecord(
                item.GetProperty("bindingReference").GetString()!,
                item.GetProperty("bindingVersion").GetString()!,
                Guid.Parse(item.GetProperty("ownerOrganizationId").GetString()!),
                item.GetProperty("providerId").GetString()!,
                item.GetProperty("credentialMode").GetString()!,
                item.GetProperty("revoked").GetBoolean(),
                item.GetProperty("secretName").GetString()!));
        }

        return new InMemoryModelDeploymentCredentialCatalog([.. records]);
    }
}
