using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

internal sealed record OpenRouterLivePinnedRouteExpectation(
    string ProfileId,
    string Model,
    string ProviderSlug,
    string ProviderIdentity,
    string AdapterDigest,
    string ProfileDigest);

internal static class OpenRouterLivePinnedRouteAcceptance
{
    public static readonly OpenRouterLivePinnedRouteExpectation GemmaDarkbloom = CreateExpectation(
        OpenRouterLiveQualification.GemmaDarkbloomProfileId,
        OpenRouterLiveQualification.GemmaDarkbloomModel,
        OpenRouterLiveQualification.GemmaDarkbloomProviderSlug,
        OpenRouterLiveQualification.GemmaDarkbloomProviderIdentity);

    public static readonly OpenRouterLivePinnedRouteExpectation NemotronNanoBackup = CreateExpectation(
        OpenRouterLiveQualification.NemotronNanoBackupProfileId,
        OpenRouterLiveQualification.NemotronNanoBackupModel,
        OpenRouterLiveQualification.NemotronNanoBackupProviderSlug,
        OpenRouterLiveQualification.NemotronNanoBackupProviderIdentity);

    public static bool TryAccept(
        OpenRouterInstalledConfiguration configuration,
        OpenRouterLivePinnedRouteExpectation expected,
        out string denialReason)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(expected);
        if (!string.Equals(configuration.Profile.ProfileId, expected.ProfileId, StringComparison.Ordinal))
        {
            denialReason = "profile_id_mismatch";
            return false;
        }

        if (!string.Equals(configuration.Profile.RequestedModel, expected.Model, StringComparison.Ordinal)
            || !string.Equals(configuration.Profile.ResolvedModelVersion, expected.Model, StringComparison.Ordinal))
        {
            denialReason = "model_mismatch";
            return false;
        }

        if (!string.Equals(configuration.ProviderSlug, expected.ProviderSlug, StringComparison.Ordinal)
            || !string.Equals(configuration.ExpectedReturnedProviderIdentity, expected.ProviderIdentity, StringComparison.Ordinal))
        {
            denialReason = "provider_identity_mismatch";
            return false;
        }

        if (!string.Equals(configuration.AdapterConfigurationDigest, expected.AdapterDigest, StringComparison.Ordinal)
            || !string.Equals(configuration.Profile.ProfileDigest, expected.ProfileDigest, StringComparison.Ordinal))
        {
            denialReason = "digest_mismatch";
            return false;
        }

        denialReason = string.Empty;
        return true;
    }

    private static OpenRouterLivePinnedRouteExpectation CreateExpectation(
        string profileId,
        string model,
        string providerSlug,
        string providerIdentity)
    {
        var created = OpenRouterInstalledConfiguration.Create(
            profileId,
            "1",
            model,
            model,
            providerSlug,
            providerIdentity,
            "organization_byok",
            "openrouter.synthetic");
        return new OpenRouterLivePinnedRouteExpectation(
            profileId,
            model,
            providerSlug,
            providerIdentity,
            created.AdapterConfigurationDigest,
            created.Profile.ProfileDigest);
    }
}
