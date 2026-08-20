using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Sessions.OpenAiCompatible;

namespace FlexAgent.Sessions.OpenAiCompatible.Tests;

public sealed class OpenAiCompatibleConfigurationFileTests
{
    [Fact]
    public void Matching_configuration_round_trips_and_rejects_legacy_mismatch_duplicate_and_cross_profile()
    {
        var created = ExampleConfiguration();
        var root = Directory.CreateTempSubdirectory("flex-agent-oai-config-").FullName;
        try
        {
            var profilesPath = Path.Combine(root, "profiles.json");
            var configurationsPath = Path.Combine(root, "configurations.json");
            WriteProfile(profilesPath, created);
            WriteConfiguration(configurationsPath, created);
            var loadedProfiles = InstalledModelDeploymentProfileFile.Load(profilesPath);
            var loaded = Assert.Single(OpenAiCompatibleInstalledConfigurationFile.Load(configurationsPath, loadedProfiles));
            Assert.Equal(created.Profile.ProfileDigest, loaded.Profile.ProfileDigest);
            Assert.Equal("/v1", loaded.ApiBasePath);
            Assert.Equal(OpenAiCompatibleAdapterContracts.DestinationPolicyPublicOnly, loaded.DestinationPolicy.Kind);

            Assert.Throws<ArgumentException>(() =>
                OpenAiCompatibleInstalledConfigurationFile.Load(
                    configurationsPath,
                    [
                        InstalledModelDeploymentProfile.Create(
                            "direct-openai.example.do-not-enable",
                            "1",
                            ModelDeploymentAdapterKinds.DirectOpenAi,
                            "sessions.openai.v1",
                            new Uri("https://api.openai.com/"),
                            "replace-with-owner-selected-model",
                            "replace-with-immutable-version",
                            "p0.text.structured-control",
                            ModelDeploymentCredentialModes.OrganizationByok,
                            256,
                            TimeSpan.FromSeconds(30),
                            TimeSpan.FromSeconds(60),
                            2,
                            "openai.direct"),
                    ]));

            var extra = Path.Combine(root, "extra.json");
            File.WriteAllText(extra, """[{"profileId":"other","profileVersion":"1","profileDigest":"aa","adapterConfigurationDigest":"bb","apiBasePath":"/v1","destinationPolicy":"public_only"}]""");
            Assert.Throws<ArgumentException>(() =>
                OpenAiCompatibleInstalledConfigurationFile.Load(extra, loadedProfiles));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Example_and_placeholder_qualification_records_cannot_enable()
    {
        var created = ExampleConfiguration();
        Assert.True(OpenAiCompatibleQualificationRecords.IsNonEnableableIdentity(created.Profile.ProfileId));
        var record = new OpenAiCompatibleQualificationRecord(
            OpenAiCompatibleAdapterContracts.AdapterKind,
            OpenAiCompatibleAdapterContracts.AdapterContractVersion,
            created.Profile.ProfileId,
            created.Profile.ProfileVersion,
            created.Profile.ProfileDigest,
            created.AdapterConfigurationDigest,
            OpenAiCompatibleQualificationRecords.ExactProfile);
        Assert.False(OpenAiCompatibleQualificationRecords.TryAccept(record, created));

        var enableable = OpenAiCompatibleInstalledConfiguration.Create(
            "openai-compatible.operator.test",
            "1",
            new Uri("https://models.organization.example/"),
            "replace-with-operator-selected-model",
            "replace-with-immutable-version-or-fingerprint",
            ModelDeploymentCredentialModes.OrganizationByok,
            "replace-with-actual-provider-or-runtime-id",
            "/v1");
        Assert.True(OpenAiCompatibleQualificationRecords.TryAccept(
            record with
            {
                ProfileId = enableable.Profile.ProfileId,
                ProfileDigest = enableable.Profile.ProfileDigest,
                AdapterConfigurationDigest = enableable.AdapterConfigurationDigest,
            },
            enableable));
        Assert.False(OpenAiCompatibleQualificationRecords.TryAccept(
            record with
            {
                ProfileId = enableable.Profile.ProfileId,
                ProfileDigest = enableable.Profile.ProfileDigest,
                AdapterConfigurationDigest = enableable.AdapterConfigurationDigest,
                QualifiedFor = OpenAiCompatibleQualificationRecords.DoNotEnable,
            },
            enableable));
        Assert.False(OpenAiCompatibleQualificationRecords.TryAccept(
            record with
            {
                ProfileId = enableable.Profile.ProfileId,
                ProfileDigest = enableable.Profile.ProfileDigest,
                AdapterConfigurationDigest = new string('0', 64),
            },
            enableable));
    }

    internal static OpenAiCompatibleInstalledConfiguration ExampleConfiguration() =>
        OpenAiCompatibleInstalledConfiguration.Create(
            "openai-compatible.example.do-not-enable",
            "1",
            new Uri("https://models.organization.example/"),
            "replace-with-operator-selected-model",
            "replace-with-immutable-version-or-fingerprint",
            ModelDeploymentCredentialModes.OrganizationByok,
            "replace-with-actual-provider-or-runtime-id",
            "/v1");

    private static void WriteProfile(string path, OpenAiCompatibleInstalledConfiguration created)
    {
        var profile = created.Profile;
        File.WriteAllText(path, $$"""
            [
              {
                "profileId": "{{profile.ProfileId}}",
                "profileVersion": "{{profile.ProfileVersion}}",
                "adapterKind": "{{profile.AdapterKind}}",
                "adapterContractVersion": "{{profile.AdapterContractVersion}}",
                "approvedHttpsOrigin": "https://models.organization.example/",
                "requestedModel": "{{profile.RequestedModel}}",
                "resolvedModelVersion": "{{profile.ResolvedModelVersion}}",
                "capabilityProfileId": "{{profile.CapabilityProfileId}}",
                "credentialMode": "{{profile.CredentialMode}}",
                "maxOutputTokens": {{profile.MaxOutputTokens}},
                "controlTimeoutMilliseconds": {{(int)profile.ControlTimeout.TotalMilliseconds}},
                "contentTimeoutMilliseconds": {{(int)profile.ContentTimeout.TotalMilliseconds}},
                "maxProviderRequestAttempts": {{profile.MaxProviderRequestAttempts}},
                "providerId": "{{profile.ProviderId}}",
                "adapterConfigurationDigest": "{{profile.AdapterConfigurationDigest}}"
              }
            ]
            """);
    }

    private static void WriteConfiguration(string path, OpenAiCompatibleInstalledConfiguration created) =>
        File.WriteAllText(path, $$"""
            [
              {
                "profileId": "{{created.Profile.ProfileId}}",
                "profileVersion": "{{created.Profile.ProfileVersion}}",
                "profileDigest": "{{created.Profile.ProfileDigest}}",
                "adapterConfigurationDigest": "{{created.AdapterConfigurationDigest}}",
                "apiBasePath": "/v1",
                "destinationPolicy": "public_only"
              }
            ]
            """);
}
