using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterExampleProfileConsistencyTests
{
    [Fact]
    public void Operator_example_files_round_trip_through_Create_and_fail_closed_loaders()
    {
        var created = OpenRouterInstalledConfiguration.Create(
            "openrouter.synthetic.example.do-not-enable",
            "1",
            "acme/example-instruct:free",
            "acme/example-instruct:free",
            "Together",
            "Together",
            ModelDeploymentCredentialModes.OrganizationByok,
            "openrouter.synthetic");
        var root = FindRepositoryRoot();
        var profilesPath = Path.Combine(root, "docs", "operations", "provider-profiles", "openrouter-synthetic.profile.example.json");
        var configurationsPath = Path.Combine(root, "docs", "operations", "provider-profiles", "openrouter-synthetic.configuration.example.json");

        var loadedProfiles = InstalledModelDeploymentProfileFile.Load(profilesPath);
        var profile = Assert.Single(loadedProfiles);
        Assert.Equal(created.Profile.ProfileDigest, profile.ProfileDigest);
        Assert.Equal(created.AdapterConfigurationDigest, profile.AdapterConfigurationDigest);

        var loaded = Assert.Single(OpenRouterInstalledConfigurationFile.Load(configurationsPath, loadedProfiles));
        Assert.Equal(created.Profile.ProfileDigest, loaded.Profile.ProfileDigest);
        Assert.Equal("Together", loaded.ProviderSlug);
        Assert.Equal("Together", loaded.ExpectedReturnedProviderIdentity);
    }

    [Fact]
    public void Gemma_darkbloom_constants_round_trip_through_fail_closed_loaders()
    {
        var created = OpenRouterInstalledConfiguration.Create(
            OpenRouterLiveQualification.GemmaDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GemmaDarkbloomModel,
            OpenRouterLiveQualification.GemmaDarkbloomModel,
            OpenRouterLiveQualification.GemmaDarkbloomProviderSlug,
            OpenRouterLiveQualification.GemmaDarkbloomProviderIdentity,
            ModelDeploymentCredentialModes.OrganizationByok,
            "openrouter.synthetic");
        Assert.Equal(OpenRouterLiveQualification.GemmaDarkbloomAdapterDigest, created.AdapterConfigurationDigest);
        Assert.Equal(OpenRouterLiveQualification.GemmaDarkbloomProfileDigest, created.Profile.ProfileDigest);

        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var profilesPath = Path.Combine(directory.FullName, "profile.json");
            var configurationsPath = Path.Combine(directory.FullName, "configuration.json");
            File.WriteAllText(
                profilesPath,
                $$"""
                [
                  {
                    "profileId": "{{created.Profile.ProfileId}}",
                    "profileVersion": "{{created.Profile.ProfileVersion}}",
                    "adapterKind": "{{created.Profile.AdapterKind}}",
                    "adapterContractVersion": "{{created.Profile.AdapterContractVersion}}",
                    "approvedHttpsOrigin": "https://openrouter.ai/",
                    "requestedModel": "{{created.Profile.RequestedModel}}",
                    "resolvedModelVersion": "{{created.Profile.ResolvedModelVersion}}",
                    "capabilityProfileId": "{{created.Profile.CapabilityProfileId}}",
                    "credentialMode": "{{created.Profile.CredentialMode}}",
                    "maxOutputTokens": {{created.Profile.MaxOutputTokens}},
                    "controlTimeoutMilliseconds": {{(int)created.Profile.ControlTimeout.TotalMilliseconds}},
                    "contentTimeoutMilliseconds": {{(int)created.Profile.ContentTimeout.TotalMilliseconds}},
                    "maxProviderRequestAttempts": {{created.Profile.MaxProviderRequestAttempts}},
                    "providerId": "{{created.Profile.ProviderId}}",
                    "adapterConfigurationDigest": "{{created.AdapterConfigurationDigest}}"
                  }
                ]
                """);
            File.WriteAllText(
                configurationsPath,
                $$"""
                [
                  {
                    "profileId": "{{created.Profile.ProfileId}}",
                    "profileVersion": "{{created.Profile.ProfileVersion}}",
                    "profileDigest": "{{created.Profile.ProfileDigest}}",
                    "adapterConfigurationDigest": "{{created.AdapterConfigurationDigest}}",
                    "providerSlug": "{{created.ProviderSlug}}",
                    "expectedReturnedProviderIdentity": "{{created.ExpectedReturnedProviderIdentity}}"
                  }
                ]
                """);

            var loadedProfiles = InstalledModelDeploymentProfileFile.Load(profilesPath);
            var loaded = Assert.Single(OpenRouterInstalledConfigurationFile.Load(configurationsPath, loadedProfiles));
            Assert.True(
                OpenRouterLivePinnedRouteAcceptance.TryAccept(
                    loaded,
                    OpenRouterLivePinnedRouteAcceptance.GemmaDarkbloom,
                    out var denial));
            Assert.Equal(string.Empty, denial);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FlexAgent.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
