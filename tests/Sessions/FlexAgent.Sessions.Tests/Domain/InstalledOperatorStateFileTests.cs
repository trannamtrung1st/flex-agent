using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class InstalledOperatorStateFileTests
{
    [Fact]
    public void Direct_openai_example_profile_loads_without_adapter_configuration_digest()
    {
        var path = WriteTemp("""
            [
              {
                "profileId": "direct-openai.example.do-not-enable",
                "profileVersion": "1",
                "adapterKind": "direct_openai",
                "adapterContractVersion": "sessions.openai.v1",
                "approvedHttpsOrigin": "https://api.openai.com/",
                "requestedModel": "replace-with-owner-selected-model",
                "resolvedModelVersion": "replace-with-immutable-version",
                "capabilityProfileId": "p0.text.structured-control",
                "credentialMode": "organization_byok",
                "maxOutputTokens": 256,
                "controlTimeoutMilliseconds": 30000,
                "contentTimeoutMilliseconds": 60000,
                "maxProviderRequestAttempts": 2,
                "providerId": "openai.direct"
              }
            ]
            """);

        var profiles = InstalledModelDeploymentProfileFile.Load(path);

        Assert.Null(Assert.Single(profiles).AdapterConfigurationDigest);
        Assert.Equal("11fd39ad22fa975ad3db30a257405b33d8760d13d0ef7592f31e8cac6281ff2f", profiles[0].ProfileDigest);
    }

    [Fact]
    public void Non_array_duplicate_and_incomplete_files_fail_closed()
    {
        Assert.Throws<ArgumentException>(() => InstalledModelDeploymentProfileFile.Load(WriteTemp("""{"profileId":"x"}""")));
        Assert.Throws<ArgumentException>(() => InstalledModelDeploymentProfileFile.Load(WriteTemp("[]".PadRight(InstalledOperatorStateFiles.MaxUtf8Bytes + 8, ' '))));
        Assert.Throws<ArgumentException>(() => InstalledCredentialCatalogFile.Load(WriteTemp("""[{"bindingReference":"a"}]""")));
    }

    private static string WriteTemp(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), "flex-agent-installed-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        return path;
    }
}
