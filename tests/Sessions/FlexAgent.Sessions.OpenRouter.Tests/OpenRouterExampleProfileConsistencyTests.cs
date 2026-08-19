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
