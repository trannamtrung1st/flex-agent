namespace FlexAgent.Sessions.Domain;

public static class ModelDeploymentCredentialModes
{
    public const string DeploymentDefault = "deployment_default";
    public const string OrganizationByok = "organization_byok";
}

public static class ModelDeploymentAdapterKinds
{
    public const string DirectOpenAi = "direct_openai";
    public const string DeterministicFake = "deterministic_fake";
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
    string ProviderId)
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
        string providerId)
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
        if (!string.Equals(approvedHttpsOrigin.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || approvedHttpsOrigin.IsLoopback
            || string.IsNullOrWhiteSpace(approvedHttpsOrigin.Host))
        {
            throw new ArgumentOutOfRangeException(nameof(approvedHttpsOrigin));
        }

        var digestSource = string.Join(
            "\n",
            profileId,
            profileVersion,
            adapterKind,
            adapterContractVersion,
            approvedHttpsOrigin.GetLeftPart(UriPartial.Authority).ToLowerInvariant(),
            requestedModel,
            resolvedModelVersion,
            capabilityProfileId,
            credentialMode,
            maxOutputTokens.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ((int)controlTimeout.TotalMilliseconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ((int)contentTimeout.TotalMilliseconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
            maxProviderRequestAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture),
            providerId);
        var digest = ProtectedContentRef.DigestUtf8(digestSource);
        return new InstalledModelDeploymentProfile(
            profileId,
            profileVersion,
            digest,
            adapterKind,
            adapterContractVersion,
            approvedHttpsOrigin,
            requestedModel,
            resolvedModelVersion,
            capabilityProfileId,
            credentialMode,
            maxOutputTokens,
            controlTimeout,
            contentTimeout,
            maxProviderRequestAttempts,
            providerId);
    }
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
