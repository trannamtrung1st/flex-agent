using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterLivePinnedRouteAcceptanceTests
{
    [Fact]
    public void Gpt_oss_route_accepts_only_the_qualified_identity_and_reasoning_policy()
    {
        var matching = OpenRouterInstalledConfiguration.Create(
            OpenRouterLiveQualification.GptOssDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomProviderSlug,
            OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity,
            "organization_byok",
            "openrouter.synthetic",
            requestPolicy: OpenRouterRequestPolicy.Phase21GptOss);

        Assert.True(
            OpenRouterLivePinnedRouteAcceptance.TryAccept(
                matching,
                OpenRouterLivePinnedRouteAcceptance.GptOssDarkbloom,
                out var accepted));
        Assert.Equal(string.Empty, accepted);
        Assert.Equal(OpenRouterLiveQualification.GptOssDarkbloomAdapterDigest, matching.AdapterConfigurationDigest);
        Assert.Equal(OpenRouterLiveQualification.GptOssDarkbloomProfileDigest, matching.Profile.ProfileDigest);
    }

    [Fact]
    public void Historical_lightning_identity_cannot_satisfy_the_gpt_oss_route()
    {
        var lightning = OpenRouterInstalledConfiguration.Create(
            "openrouter.synthetic.local.nemotron-3.5-lightning",
            "1",
            "nvidia/nemotron-3.5-lightning:free",
            "nvidia/nemotron-3.5-lightning:free",
            "nvidia",
            "Nvidia",
            "organization_byok",
            "openrouter.synthetic");

        Assert.False(
            OpenRouterLivePinnedRouteAcceptance.TryAccept(
                lightning,
                OpenRouterLivePinnedRouteAcceptance.GptOssDarkbloom,
                out var denial));
        Assert.Equal("profile_id_mismatch", denial);
    }

    [Fact]
    public void Sibling_provider_cannot_satisfy_the_gpt_oss_route()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OpenRouterInstalledConfiguration.Create(
                OpenRouterLiveQualification.GptOssDarkbloomProfileId,
                "1",
                OpenRouterLiveQualification.GptOssDarkbloomModel,
                OpenRouterLiveQualification.GptOssDarkbloomModel,
                "google-ai-studio",
                "Google AI Studio",
                "organization_byok",
                "openrouter.synthetic",
                requestPolicy: OpenRouterRequestPolicy.Phase21GptOss));
    }

    [Fact]
    public void Default_policy_gpt_oss_identity_cannot_satisfy_the_qualified_route()
    {
        var historicalPolicy = OpenRouterInstalledConfiguration.Create(
            OpenRouterLiveQualification.GptOssDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomProviderSlug,
            OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity,
            "organization_byok",
            "openrouter.synthetic");

        Assert.False(
            OpenRouterLivePinnedRouteAcceptance.TryAccept(
                historicalPolicy,
                OpenRouterLivePinnedRouteAcceptance.GptOssDarkbloom,
                out var denial));
        Assert.Equal("digest_mismatch", denial);
        Assert.Equal(256, historicalPolicy.Profile.MaxOutputTokens);
        Assert.Null(historicalPolicy.RequestPolicy.ReasoningEffort);
    }
}
