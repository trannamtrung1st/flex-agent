using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterLivePinnedRouteAcceptanceTests
{
    [Fact]
    public void Gemma_darkbloom_route_accepts_only_the_approved_identity_and_digests()
    {
        var matching = OpenRouterInstalledConfiguration.Create(
            OpenRouterLiveQualification.GemmaDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GemmaDarkbloomModel,
            OpenRouterLiveQualification.GemmaDarkbloomModel,
            OpenRouterLiveQualification.GemmaDarkbloomProviderSlug,
            OpenRouterLiveQualification.GemmaDarkbloomProviderIdentity,
            "organization_byok",
            "openrouter.synthetic");

        Assert.True(
            OpenRouterLivePinnedRouteAcceptance.TryAccept(
                matching,
                OpenRouterLivePinnedRouteAcceptance.GemmaDarkbloom,
                out var accepted));
        Assert.Equal(string.Empty, accepted);
        Assert.Equal(
            OpenRouterLiveQualification.GemmaDarkbloomAdapterDigest,
            matching.AdapterConfigurationDigest);
        Assert.Equal(
            OpenRouterLiveQualification.GemmaDarkbloomProfileDigest,
            matching.Profile.ProfileDigest);
    }

    [Fact]
    public void Historical_lightning_identity_cannot_satisfy_the_gemma_route()
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
                OpenRouterLivePinnedRouteAcceptance.GemmaDarkbloom,
                out var denial));
        Assert.Equal("profile_id_mismatch", denial);
    }

    [Fact]
    public void Sibling_google_ai_studio_provider_cannot_satisfy_the_gemma_route()
    {
        var studio = OpenRouterInstalledConfiguration.Create(
            OpenRouterLiveQualification.GemmaDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GemmaDarkbloomModel,
            OpenRouterLiveQualification.GemmaDarkbloomModel,
            "google-ai-studio",
            "Google AI Studio",
            "organization_byok",
            "openrouter.synthetic");

        Assert.False(
            OpenRouterLivePinnedRouteAcceptance.TryAccept(
                studio,
                OpenRouterLivePinnedRouteAcceptance.GemmaDarkbloom,
                out var denial));
        Assert.Equal("provider_identity_mismatch", denial);
    }

    [Fact]
    public void Nemotron_nano_backup_route_rejects_the_primary_gemma_identity()
    {
        var gemma = OpenRouterInstalledConfiguration.Create(
            OpenRouterLiveQualification.GemmaDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GemmaDarkbloomModel,
            OpenRouterLiveQualification.GemmaDarkbloomModel,
            OpenRouterLiveQualification.GemmaDarkbloomProviderSlug,
            OpenRouterLiveQualification.GemmaDarkbloomProviderIdentity,
            "organization_byok",
            "openrouter.synthetic");

        Assert.False(
            OpenRouterLivePinnedRouteAcceptance.TryAccept(
                gemma,
                OpenRouterLivePinnedRouteAcceptance.NemotronNanoBackup,
                out var denial));
        Assert.Equal("profile_id_mismatch", denial);

        var backup = OpenRouterInstalledConfiguration.Create(
            OpenRouterLiveQualification.NemotronNanoBackupProfileId,
            "1",
            OpenRouterLiveQualification.NemotronNanoBackupModel,
            OpenRouterLiveQualification.NemotronNanoBackupModel,
            OpenRouterLiveQualification.NemotronNanoBackupProviderSlug,
            OpenRouterLiveQualification.NemotronNanoBackupProviderIdentity,
            "organization_byok",
            "openrouter.synthetic");
        Assert.True(
            OpenRouterLivePinnedRouteAcceptance.TryAccept(
                backup,
                OpenRouterLivePinnedRouteAcceptance.NemotronNanoBackup,
                out var accepted));
        Assert.Equal(string.Empty, accepted);
        Assert.Equal(
            OpenRouterLiveQualification.NemotronNanoBackupAdapterDigest,
            backup.AdapterConfigurationDigest);
        Assert.Equal(
            OpenRouterLiveQualification.NemotronNanoBackupProfileDigest,
            backup.Profile.ProfileDigest);
    }
}
