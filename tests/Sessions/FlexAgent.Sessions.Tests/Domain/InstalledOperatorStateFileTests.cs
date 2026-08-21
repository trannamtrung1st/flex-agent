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

    [Fact]
    public void Duplicate_or_unknown_profile_and_catalog_properties_fail_closed()
    {
        Assert.Throws<ArgumentException>(() => InstalledModelDeploymentProfileFile.Load(WriteTemp("""
            [
              {
                "profileId": "direct-openai.example.do-not-enable",
                "profileVersion": "1",
                "adapterKind": "direct_openai",
                "adapterKind": "openai_compatible",
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
            """)));

        Assert.Throws<ArgumentException>(() => InstalledModelDeploymentProfileFile.Load(WriteTemp("""
            [
              {
                "profileId": "openai-compatible.example.do-not-enable",
                "profileVersion": "1",
                "adapterKind": "openai_compatible",
                "adapterContractVersion": "sessions.openai_compatible.v1",
                "approvedHttpsOrigin": "https://models.organization.example/",
                "requestedModel": "replace-with-operator-selected-model",
                "resolvedModelVersion": "replace-with-immutable-version-or-fingerprint",
                "capabilityProfileId": "p0.text.structured-control",
                "credentialMode": "organization_byok",
                "maxOutputTokens": 256,
                "controlTimeoutMilliseconds": 30000,
                "contentTimeoutMilliseconds": 60000,
                "maxProviderRequestAttempts": 2,
                "providerId": "replace-with-actual-provider-or-runtime-id",
                "adapterConfigurationDigest": "0000000000000000000000000000000000000000000000000000000000000000",
                "adapterConfigurationDigest": "66f729ceff48a979b8ec5d2bc8c76250a4807ce886ec46ff8a9aaff48669a858"
              }
            ]
            """)));

        Assert.Throws<ArgumentException>(() => InstalledModelDeploymentProfileFile.Load(WriteTemp("""
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
                "providerId": "openai.direct",
                "legacyEndpoint": "https://api.openai.com/v1"
              }
            ]
            """)));

        Assert.Throws<ArgumentException>(() => InstalledCredentialCatalogFile.Load(WriteTemp("""
            [
              {
                "bindingReference": "bind.opaque.0001",
                "bindingVersion": "bind.v1",
                "ownerOrganizationId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                "providerId": "openai.compatible.test",
                "credentialMode": "organization_byok",
                "revoked": true,
                "revoked": false,
                "secretName": "org-a-openai"
              }
            ]
            """)));

        Assert.Throws<ArgumentException>(() => InstalledCredentialCatalogFile.Load(WriteTemp("""
            [
              {
                "bindingReference": "bind.opaque.0001",
                "bindingVersion": "bind.v1",
                "ownerOrganizationId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                "providerId": "openai.compatible.test",
                "credentialMode": "organization_byok",
                "revoked": true,
                "secretName": "org-a-openai",
                "payerOverride": "deployment_default"
              }
            ]
            """)));
    }

    [Fact]
    public void Revoked_catalog_entry_loads_only_when_the_object_is_strict()
    {
        var catalog = InstalledCredentialCatalogFile.Load(WriteTemp("""
            [
              {
                "bindingReference": "bind.opaque.0001",
                "bindingVersion": "bind.v1",
                "ownerOrganizationId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                "providerId": "openai.compatible.test",
                "credentialMode": "organization_byok",
                "revoked": true,
                "secretName": "org-a-openai"
              }
            ]
            """));

        var record = catalog.TryGet("bind.opaque.0001", "bind.v1");
        Assert.NotNull(record);
        Assert.True(record.Revoked);
    }

    private static string WriteTemp(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), "flex-agent-installed-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        return path;
    }
}
