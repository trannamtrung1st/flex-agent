using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class OpenAiCompatibleFrozenProfileTests
{
    [Fact]
    public void OpenAi_compatible_kind_and_contract_are_distinct_from_legacy_direct_openai()
    {
        Assert.Equal("openai_compatible", ModelDeploymentAdapterKinds.OpenAiCompatible);
        Assert.Equal("direct_openai", ModelDeploymentAdapterKinds.DirectOpenAi);
        Assert.NotEqual(ModelDeploymentAdapterKinds.OpenAiCompatible, ModelDeploymentAdapterKinds.DirectOpenAi);
        Assert.NotEqual(ModelDeploymentAdapterKinds.OpenAiCompatible, ModelDeploymentAdapterKinds.OpenRouter);
        Assert.NotEqual("sessions.openai_compatible.v1", "sessions.openai.v1");
    }

    [Fact]
    public void OpenAi_compatible_profile_requires_an_adapter_configuration_digest()
    {
        Assert.Throws<ArgumentException>(() => InstalledModelDeploymentProfile.Create(
            "openai-compatible.example.do-not-enable",
            "1",
            ModelDeploymentAdapterKinds.OpenAiCompatible,
            "sessions.openai_compatible.v1",
            new Uri("https://models.organization.example/"),
            "replace-with-operator-selected-model",
            "replace-with-immutable-version-or-fingerprint",
            "p0.text.structured-control",
            ModelDeploymentCredentialModes.OrganizationByok,
            256,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            2,
            "replace-with-actual-provider-or-runtime-id"));
    }

    [Fact]
    public void Known_direct_openai_profile_digest_remains_historical_and_non_executable_identity()
    {
        var profile = InstalledModelDeploymentProfile.Create(
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
            "openai.direct");

        Assert.Equal("direct_openai", profile.AdapterKind);
        Assert.Equal("sessions.openai.v1", profile.AdapterContractVersion);
        Assert.Null(profile.AdapterConfigurationDigest);
        Assert.Equal("11fd39ad22fa975ad3db30a257405b33d8760d13d0ef7592f31e8cac6281ff2f", profile.ProfileDigest);
    }

    [Fact]
    public void Adapter_configuration_digest_changes_the_openai_compatible_profile_identity()
    {
        var first = new string('a', 64);
        var second = new string('b', 64);
        var left = CreateCompatible(first);
        var right = CreateCompatible(second);

        Assert.NotEqual(left.ProfileDigest, right.ProfileDigest);
        Assert.Equal(first, left.AdapterConfigurationDigest);
        Assert.Equal(second, right.AdapterConfigurationDigest);
    }

    [Fact]
    public void Example_openai_compatible_profile_digest_is_pinned()
    {
        var profile = CreateCompatible("66f729ceff48a979b8ec5d2bc8c76250a4807ce886ec46ff8a9aaff48669a858");
        Assert.Equal("6bbfa4715615a50f006679381333a17a074fec74a2f7967c97c64338e05428c7", profile.ProfileDigest);
    }

    private static InstalledModelDeploymentProfile CreateCompatible(string adapterDigest) =>
        InstalledModelDeploymentProfile.Create(
            "openai-compatible.example.do-not-enable",
            "1",
            ModelDeploymentAdapterKinds.OpenAiCompatible,
            "sessions.openai_compatible.v1",
            new Uri("https://models.organization.example/"),
            "replace-with-operator-selected-model",
            "replace-with-immutable-version-or-fingerprint",
            "p0.text.structured-control",
            ModelDeploymentCredentialModes.OrganizationByok,
            256,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            2,
            "replace-with-actual-provider-or-runtime-id",
            adapterDigest);
}
