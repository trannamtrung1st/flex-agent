namespace FlexAgent.Sessions.Domain;

/// <summary>
/// Frozen synthetic-development model identity shared by Attempt start and the
/// Development/Testing Worker fake adapter. Do not change these fields without
/// also rotating already-started Session bindings.
/// </summary>
public static class SyntheticDevelopmentModelDeployment
{
    public const string BindingReference = "bind.opaque.dev";
    public const string BindingVersion = "bind.v1";
    public const string SecretName = "synthetic.fake.dev";

    public static InstalledModelDeploymentProfile CreateProfile() =>
        InstalledModelDeploymentProfile.Create(
            "synthetic.fake.v1",
            "1",
            ModelDeploymentAdapterKinds.DeterministicFake,
            "sessions.fake.v1",
            new Uri("https://api.openai.com/"),
            "synthetic.model.pinned",
            "synthetic.model.pinned.2026-01-01",
            "p0.text.structured-control",
            ModelDeploymentCredentialModes.OrganizationByok,
            256,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            4,
            "synthetic.provider");

    public static FrozenModelDeploymentBinding CreateFrozenBinding()
    {
        var profile = CreateProfile();
        return new FrozenModelDeploymentBinding(
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.ProviderId,
            ModelDeploymentCredentialModes.OrganizationByok,
            BindingReference,
            BindingVersion);
    }

    /// <summary>
    /// Catalog row for the compose Worker. <see cref="Guid.Empty"/> owner is the
    /// synthetic-development wildcard: any organization may resolve this binding.
    /// </summary>
    public static ModelDeploymentCredentialCatalogRecord CreateCatalogRecord() =>
        new(
            BindingReference,
            BindingVersion,
            Guid.Empty,
            "synthetic.provider",
            ModelDeploymentCredentialModes.OrganizationByok,
            false,
            SecretName);
}
