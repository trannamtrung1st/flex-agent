using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Sessions.OpenAiCompatible;

namespace FlexAgent.Sessions.OpenAiCompatible.Tests;

public sealed class OpenAiCompatibleExampleProfileConsistencyTests
{
    [Fact]
    public void Operator_example_files_round_trip_and_remain_non_enableable()
    {
        var created = OpenAiCompatibleConfigurationFileTests.ExampleConfiguration();
        var root = FindRepositoryRoot();
        var profilesPath = Path.Combine(root, "docs", "operations", "provider-profiles", "openai-compatible.profile.example.json");
        var configurationsPath = Path.Combine(root, "docs", "operations", "provider-profiles", "openai-compatible.configuration.example.json");
        var qualificationPath = Path.Combine(root, "docs", "operations", "provider-profiles", "openai-compatible.qualification.example.json");

        var loadedProfiles = InstalledModelDeploymentProfileFile.Load(profilesPath);
        var profile = Assert.Single(loadedProfiles);
        Assert.Equal(created.Profile.ProfileDigest, profile.ProfileDigest);
        Assert.Equal(created.AdapterConfigurationDigest, profile.AdapterConfigurationDigest);

        var loaded = Assert.Single(OpenAiCompatibleInstalledConfigurationFile.Load(configurationsPath, loadedProfiles));
        Assert.Equal(created.Profile.ProfileDigest, loaded.Profile.ProfileDigest);
        Assert.Equal("/v1", loaded.ApiBasePath);

        var qualification = OpenAiCompatibleQualificationRecords.Load(qualificationPath);
        Assert.Equal(OpenAiCompatibleQualificationRecords.DoNotEnable, qualification.QualifiedFor);
        Assert.False(OpenAiCompatibleQualificationRecords.TryAccept(qualification, loaded));
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
