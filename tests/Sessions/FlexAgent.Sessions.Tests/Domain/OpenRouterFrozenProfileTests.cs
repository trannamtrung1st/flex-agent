using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class OpenRouterFrozenProfileTests
{
    [Fact]
    public void Known_direct_openai_profile_digest_is_unchanged_when_adapter_configuration_digest_is_absent()
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

        Assert.Null(profile.AdapterConfigurationDigest);
        Assert.Equal("11fd39ad22fa975ad3db30a257405b33d8760d13d0ef7592f31e8cac6281ff2f", profile.ProfileDigest);
    }

    [Fact]
    public void OpenRouter_kind_cannot_be_represented_as_direct_openai()
    {
        Assert.NotEqual(ModelDeploymentAdapterKinds.DirectOpenAi, ModelDeploymentAdapterKinds.OpenRouter);
        Assert.NotEqual(ModelDeploymentAdapterKinds.OpenAiCompatible, ModelDeploymentAdapterKinds.OpenRouter);
        Assert.Equal("openrouter", ModelDeploymentAdapterKinds.OpenRouter);
    }

    [Fact]
    public void OpenRouter_profile_requires_an_adapter_configuration_digest()
    {
        Assert.Throws<ArgumentException>(() => InstalledModelDeploymentProfile.Create(
            "openrouter.synthetic.example",
            "1",
            ModelDeploymentAdapterKinds.OpenRouter,
            "sessions.openrouter.v1",
            new Uri("https://openrouter.ai/"),
            "meta-llama/llama-3.1-8b-instruct:free",
            "meta-llama/llama-3.1-8b-instruct:free",
            "p0.text.structured-control",
            ModelDeploymentCredentialModes.OrganizationByok,
            256,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            2,
            "openrouter.synthetic"));
    }

    [Fact]
    public void OpenRouter_repeatable_profile_rejects_discovery_alias_identity()
    {
        var digest = new string('a', 64);
        Assert.Throws<ArgumentOutOfRangeException>(() => InstalledModelDeploymentProfile.Create(
            "openrouter.synthetic.example",
            "1",
            ModelDeploymentAdapterKinds.OpenRouter,
            "sessions.openrouter.v1",
            new Uri("https://openrouter.ai/"),
            "openrouter/free",
            "openrouter/free",
            "p0.text.structured-control",
            ModelDeploymentCredentialModes.OrganizationByok,
            256,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            2,
            "openrouter.synthetic",
            digest));
    }

    [Fact]
    public void Adapter_configuration_digest_is_included_only_when_present()
    {
        var digest = ProtectedContentRef.DigestUtf8("openrouter.adapter-policy.v1");
        var withDigest = InstalledModelDeploymentProfile.Create(
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
            "openai.direct",
            digest);
        var withoutDigest = InstalledModelDeploymentProfile.Create(
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

        Assert.NotEqual(withoutDigest.ProfileDigest, withDigest.ProfileDigest);
        Assert.Equal(digest, withDigest.AdapterConfigurationDigest);
        Assert.Equal("11fd39ad22fa975ad3db30a257405b33d8760d13d0ef7592f31e8cac6281ff2f", withoutDigest.ProfileDigest);
    }
}
