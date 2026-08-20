namespace FlexAgent.Sessions.Domain;

public static class ModelDeploymentCredentialModes
{
    public const string DeploymentDefault = "deployment_default";
    public const string OrganizationByok = "organization_byok";
}

public static class ModelDeploymentAdapterKinds
{
    public const string DirectOpenAi = "direct_openai";
    public const string OpenAiCompatible = "openai_compatible";
    public const string DeterministicFake = "deterministic_fake";
    public const string OpenRouter = "openrouter";
}

public static class ModelProviderRequestPhases
{
    public const string Control = "control";
    public const string Content = "content";
}

public static class ModelProviderRequestFacts
{
    public const string Started = "started";
    public const string Finished = "finished";
}

public static class FrozenModelDeploymentOutcomeCodes
{
    public const string Succeeded = "frozen_model_deployment.succeeded";
    public const string FrozenBindingMissing = "frozen_model_deployment.missing";
    public const string ProfileMissing = "frozen_model_deployment.profile_missing";
    public const string ProfileMismatch = "frozen_model_deployment.profile_mismatch";
    public const string CredentialMissing = "frozen_model_deployment.credential_missing";
    public const string CredentialIncomplete = "frozen_model_deployment.credential_incomplete";
    public const string CredentialRevoked = "frozen_model_deployment.credential_revoked";
    public const string CredentialWrongOrganization = "frozen_model_deployment.credential_wrong_organization";
    public const string CredentialProviderMismatch = "frozen_model_deployment.credential_provider_mismatch";
    public const string CredentialModeMismatch = "frozen_model_deployment.credential_mode_mismatch";
}

public sealed record FrozenModelDeploymentBinding(
    string ProfileId,
    string ProfileVersion,
    string ProfileDigest,
    string ProviderId,
    string CredentialMode,
    string CredentialBindingReference,
    string CredentialBindingVersion);

public sealed record InstalledModelDeploymentProfile(
    string ProfileId,
    string ProfileVersion,
    string ProfileDigest,
    string AdapterKind,
    string AdapterContractVersion,
    Uri ApprovedHttpsOrigin,
    string RequestedModel,
    string ResolvedModelVersion,
    string CapabilityProfileId,
    string CredentialMode,
    int MaxOutputTokens,
    TimeSpan ControlTimeout,
    TimeSpan ContentTimeout,
    int MaxProviderRequestAttempts,
    string ProviderId,
    string? AdapterConfigurationDigest = null)
{
    public static InstalledModelDeploymentProfile Create(
        string profileId,
        string profileVersion,
        string adapterKind,
        string adapterContractVersion,
        Uri approvedHttpsOrigin,
        string requestedModel,
        string resolvedModelVersion,
        string capabilityProfileId,
        string credentialMode,
        int maxOutputTokens,
        TimeSpan controlTimeout,
        TimeSpan contentTimeout,
        int maxProviderRequestAttempts,
        string providerId,
        string? adapterConfigurationDigest = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterContractVersion);
        ArgumentNullException.ThrowIfNull(approvedHttpsOrigin);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedModel);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedModelVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityProfileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialMode);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        var canonicalOrigin = global::FlexAgent.Sessions.Domain.ApprovedHttpsOrigin.Canonicalize(approvedHttpsOrigin);
        var normalizedAdapterDigest = NormalizeAdapterConfigurationDigest(adapterConfigurationDigest);
        if (string.Equals(adapterKind, ModelDeploymentAdapterKinds.OpenAiCompatible, StringComparison.Ordinal)
            && normalizedAdapterDigest is null)
        {
            throw new ArgumentException(
                "OpenAI-compatible installed profiles require an adapter-configuration digest.",
                nameof(adapterConfigurationDigest));
        }

        if (string.Equals(adapterKind, ModelDeploymentAdapterKinds.OpenRouter, StringComparison.Ordinal))
        {
            if (normalizedAdapterDigest is null)
            {
                throw new ArgumentException(
                    "OpenRouter installed profiles require an adapter-configuration digest.",
                    nameof(adapterConfigurationDigest));
            }

            if (IsOpenRouterDiscoveryIdentity(requestedModel) || IsOpenRouterDiscoveryIdentity(resolvedModelVersion))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedModel),
                    "OpenRouter repeatable Sessions require one concrete :free model, not a discovery alias.");
            }
        }

        var digestSource = string.Join(
            "\n",
            profileId,
            profileVersion,
            adapterKind,
            adapterContractVersion,
            global::FlexAgent.Sessions.Domain.ApprovedHttpsOrigin.DigestSource(canonicalOrigin),
            requestedModel,
            resolvedModelVersion,
            capabilityProfileId,
            credentialMode,
            maxOutputTokens.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ((int)controlTimeout.TotalMilliseconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ((int)contentTimeout.TotalMilliseconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
            maxProviderRequestAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture),
            providerId);
        if (normalizedAdapterDigest is not null)
        {
            digestSource += "\n" + normalizedAdapterDigest;
        }

        var digest = ProtectedContentRef.DigestUtf8(digestSource);
        return new InstalledModelDeploymentProfile(
            profileId,
            profileVersion,
            digest,
            adapterKind,
            adapterContractVersion,
            canonicalOrigin,
            requestedModel,
            resolvedModelVersion,
            capabilityProfileId,
            credentialMode,
            maxOutputTokens,
            controlTimeout,
            contentTimeout,
            maxProviderRequestAttempts,
            providerId,
            normalizedAdapterDigest);
    }

    private static string? NormalizeAdapterConfigurationDigest(string? adapterConfigurationDigest)
    {
        if (string.IsNullOrWhiteSpace(adapterConfigurationDigest))
        {
            return null;
        }

        if (adapterConfigurationDigest.Length != 64
            || adapterConfigurationDigest.Any(ch => ch is < '0' or > '9' and (< 'a' or > 'f')))
        {
            throw new ArgumentOutOfRangeException(
                nameof(adapterConfigurationDigest),
                "Adapter-configuration digest must be a lowercase SHA-256 hex string.");
        }

        return adapterConfigurationDigest;
    }

    private static bool IsOpenRouterDiscoveryIdentity(string model) =>
        string.Equals(model, "openrouter/free", StringComparison.Ordinal)
        || string.Equals(model, "openrouter/auto", StringComparison.Ordinal)
        || !model.EndsWith(":free", StringComparison.Ordinal);
}

public sealed record ModelDeploymentCredentialCatalogRecord(
    string BindingReference,
    string BindingVersion,
    Guid OwnerOrganizationId,
    string ProviderId,
    string CredentialMode,
    bool Revoked,
    string SecretName);

public interface IInstalledModelDeploymentProfileRegistry
{
    InstalledModelDeploymentProfile? TryGet(string profileId, string profileVersion, string profileDigest);
}

public interface IModelDeploymentCredentialCatalog
{
    ModelDeploymentCredentialCatalogRecord? TryGet(string bindingReference, string bindingVersion);
}

public sealed record FrozenModelDeploymentResolution(
    bool Succeeded,
    string OutcomeCode,
    FrozenModelDeploymentBinding? Frozen,
    InstalledModelDeploymentProfile? Profile,
    ModelDeploymentCredentialBinding? Binding,
    string? SecretName);

public sealed class InMemoryInstalledModelDeploymentProfileRegistry : IInstalledModelDeploymentProfileRegistry
{
    private readonly Dictionary<string, InstalledModelDeploymentProfile> _profiles = new(StringComparer.Ordinal);

    public InMemoryInstalledModelDeploymentProfileRegistry(params InstalledModelDeploymentProfile[] profiles)
    {
        foreach (var profile in profiles)
        {
            ArgumentNullException.ThrowIfNull(profile);
            _profiles[Key(profile.ProfileId, profile.ProfileVersion, profile.ProfileDigest)] = profile;
        }
    }

    public InstalledModelDeploymentProfile? TryGet(string profileId, string profileVersion, string profileDigest) =>
        _profiles.TryGetValue(Key(profileId, profileVersion, profileDigest), out var profile) ? profile : null;

    private static string Key(string profileId, string profileVersion, string profileDigest) =>
        $"{profileId}\n{profileVersion}\n{profileDigest}";
}

public sealed class InMemoryModelDeploymentCredentialCatalog : IModelDeploymentCredentialCatalog
{
    private readonly Dictionary<string, ModelDeploymentCredentialCatalogRecord> _records = new(StringComparer.Ordinal);

    public InMemoryModelDeploymentCredentialCatalog(params ModelDeploymentCredentialCatalogRecord[] records)
    {
        foreach (var record in records)
        {
            ArgumentNullException.ThrowIfNull(record);
            _records[Key(record.BindingReference, record.BindingVersion)] = record;
        }
    }

    public ModelDeploymentCredentialCatalogRecord? TryGet(string bindingReference, string bindingVersion) =>
        _records.TryGetValue(Key(bindingReference, bindingVersion), out var record) ? record : null;

    private static string Key(string bindingReference, string bindingVersion) =>
        $"{bindingReference}\n{bindingVersion}";
}

public static class FrozenModelDeploymentResolver
{
    public static FrozenModelDeploymentResolution Resolve(
        TrustedSessionBinding sessionBinding,
        IInstalledModelDeploymentProfileRegistry profiles,
        IModelDeploymentCredentialCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(sessionBinding);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(catalog);

        var frozen = sessionBinding.FrozenModelDeployment;
        return Resolve(sessionBinding.Ownership, frozen, profiles, catalog);
    }

    public static FrozenModelDeploymentResolution Resolve(
        SessionOwnership ownership,
        FrozenModelDeploymentBinding? frozen,
        IInstalledModelDeploymentProfileRegistry profiles,
        IModelDeploymentCredentialCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(catalog);

        if (frozen is null
            || string.IsNullOrWhiteSpace(frozen.ProfileId)
            || string.IsNullOrWhiteSpace(frozen.ProfileVersion)
            || string.IsNullOrWhiteSpace(frozen.ProfileDigest)
            || string.IsNullOrWhiteSpace(frozen.ProviderId)
            || string.IsNullOrWhiteSpace(frozen.CredentialMode)
            || string.IsNullOrWhiteSpace(frozen.CredentialBindingReference)
            || string.IsNullOrWhiteSpace(frozen.CredentialBindingVersion))
        {
            return Failure(FrozenModelDeploymentOutcomeCodes.FrozenBindingMissing);
        }

        var profile = profiles.TryGet(frozen.ProfileId, frozen.ProfileVersion, frozen.ProfileDigest);
        if (profile is null)
        {
            return Failure(FrozenModelDeploymentOutcomeCodes.ProfileMissing);
        }

        if (!string.Equals(profile.ProviderId, frozen.ProviderId, StringComparison.Ordinal)
            || !string.Equals(profile.CredentialMode, frozen.CredentialMode, StringComparison.Ordinal)
            || !string.Equals(profile.ProfileDigest, frozen.ProfileDigest, StringComparison.Ordinal))
        {
            return Failure(FrozenModelDeploymentOutcomeCodes.ProfileMismatch);
        }

        var catalogRecord = catalog.TryGet(frozen.CredentialBindingReference, frozen.CredentialBindingVersion);
        if (catalogRecord is null)
        {
            return Failure(FrozenModelDeploymentOutcomeCodes.CredentialMissing);
        }

        if (catalogRecord.Revoked)
        {
            return Failure(FrozenModelDeploymentOutcomeCodes.CredentialRevoked);
        }

        if (!string.Equals(catalogRecord.ProviderId, frozen.ProviderId, StringComparison.Ordinal)
            || !string.Equals(catalogRecord.ProviderId, profile.ProviderId, StringComparison.Ordinal))
        {
            return Failure(FrozenModelDeploymentOutcomeCodes.CredentialProviderMismatch);
        }

        if (!string.Equals(catalogRecord.CredentialMode, frozen.CredentialMode, StringComparison.Ordinal))
        {
            return Failure(FrozenModelDeploymentOutcomeCodes.CredentialModeMismatch);
        }

        if (string.Equals(frozen.CredentialMode, ModelDeploymentCredentialModes.OrganizationByok, StringComparison.Ordinal)
            && catalogRecord.OwnerOrganizationId != ownership.OrganizationId)
        {
            return Failure(FrozenModelDeploymentOutcomeCodes.CredentialWrongOrganization);
        }

        if (string.IsNullOrWhiteSpace(catalogRecord.SecretName)
            || string.IsNullOrWhiteSpace(catalogRecord.BindingReference)
            || string.IsNullOrWhiteSpace(catalogRecord.BindingVersion))
        {
            return Failure(FrozenModelDeploymentOutcomeCodes.CredentialIncomplete);
        }

        var source = string.Equals(
            frozen.CredentialMode,
            ModelDeploymentCredentialModes.OrganizationByok,
            StringComparison.Ordinal)
            ? ModelDeploymentCredentialBindingSource.Organization
            : ModelDeploymentCredentialBindingSource.DeploymentDefault;

        return new FrozenModelDeploymentResolution(
            true,
            FrozenModelDeploymentOutcomeCodes.Succeeded,
            frozen,
            profile,
            new ModelDeploymentCredentialBinding(
                ownership.OrganizationId,
                frozen.ProviderId,
                frozen.CredentialBindingReference,
                frozen.CredentialBindingVersion,
                source),
            catalogRecord.SecretName);
    }

    private static FrozenModelDeploymentResolution Failure(string outcomeCode) =>
        new(false, outcomeCode, null, null, null, null);
}
