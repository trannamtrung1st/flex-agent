namespace FlexAgent.Sessions.Domain;

public static class ModelDeploymentCredentialBindingOutcomeCodes
{
    public const string Succeeded = "model_deployment_credential_binding.succeeded";
    public const string BindingMissing = "model_deployment_credential_binding.missing";
    public const string BindingIncomplete = "model_deployment_credential_binding.incomplete";
    public const string BindingRevoked = "model_deployment_credential_binding.revoked";
    public const string BindingWrongOrganization = "model_deployment_credential_binding.wrong_organization";
    public const string BindingProviderMismatch = "model_deployment_credential_binding.provider_mismatch";
}

public enum ModelDeploymentCredentialBindingSource
{
    Organization,
    DeploymentDefault,
}

public sealed record ModelDeploymentCredentialBinding(
    Guid OrganizationId,
    string ProviderId,
    string BindingReference,
    string BindingVersion,
    ModelDeploymentCredentialBindingSource Source);

public sealed record ModelDeploymentCredentialBindingRequest(
    Guid OrganizationId,
    string ProviderId,
    string? OrganizationBindingReference,
    string? OrganizationBindingVersion,
    string? DeploymentDefaultBindingReference,
    string? DeploymentDefaultBindingVersion,
    bool OrganizationBindingRevoked,
    bool OrganizationBindingProviderMismatch,
    bool OrganizationBindingWrongOrganization,
    bool DeploymentDefaultBindingProviderMismatch = false);

public sealed record ModelDeploymentCredentialBindingResult(
    bool Succeeded,
    string OutcomeCode,
    ModelDeploymentCredentialBinding? Binding);

internal readonly record struct CredentialBindingCandidate(string? Reference, string? Version)
{
    internal bool IsAbsent =>
        string.IsNullOrWhiteSpace(Reference) && string.IsNullOrWhiteSpace(Version);

    internal bool IsComplete =>
        !string.IsNullOrWhiteSpace(Reference) && !string.IsNullOrWhiteSpace(Version);

    internal bool IsIncomplete => !IsAbsent && !IsComplete;

    internal static CredentialBindingCandidate From(string? reference, string? version) =>
        new(reference, version);
}

public static class ModelDeploymentCredentialBindingResolver
{
    public static ModelDeploymentCredentialBindingResult Resolve(
        ModelDeploymentCredentialBindingRequest request)
    {
        var organizationCandidate = CredentialBindingCandidate.From(
            request.OrganizationBindingReference,
            request.OrganizationBindingVersion);
        var deploymentDefaultCandidate = CredentialBindingCandidate.From(
            request.DeploymentDefaultBindingReference,
            request.DeploymentDefaultBindingVersion);

        if (organizationCandidate.IsIncomplete || deploymentDefaultCandidate.IsIncomplete)
        {
            return Failure(ModelDeploymentCredentialBindingOutcomeCodes.BindingIncomplete);
        }

        if (organizationCandidate.IsComplete)
        {
            if (request.OrganizationBindingWrongOrganization)
            {
                return Failure(ModelDeploymentCredentialBindingOutcomeCodes.BindingWrongOrganization);
            }

            if (request.OrganizationBindingRevoked)
            {
                return Failure(ModelDeploymentCredentialBindingOutcomeCodes.BindingRevoked);
            }

            if (request.OrganizationBindingProviderMismatch)
            {
                return Failure(ModelDeploymentCredentialBindingOutcomeCodes.BindingProviderMismatch);
            }

            return Success(
                request,
                request.OrganizationBindingReference!,
                request.OrganizationBindingVersion!,
                ModelDeploymentCredentialBindingSource.Organization);
        }

        if (deploymentDefaultCandidate.IsComplete)
        {
            if (request.DeploymentDefaultBindingProviderMismatch)
            {
                return Failure(ModelDeploymentCredentialBindingOutcomeCodes.BindingProviderMismatch);
            }

            return Success(
                request,
                request.DeploymentDefaultBindingReference!,
                request.DeploymentDefaultBindingVersion!,
                ModelDeploymentCredentialBindingSource.DeploymentDefault);
        }

        return Failure(ModelDeploymentCredentialBindingOutcomeCodes.BindingMissing);
    }

    private static ModelDeploymentCredentialBindingResult Success(
        ModelDeploymentCredentialBindingRequest request,
        string bindingReference,
        string bindingVersion,
        ModelDeploymentCredentialBindingSource source) =>
        new(
            true,
            ModelDeploymentCredentialBindingOutcomeCodes.Succeeded,
            new ModelDeploymentCredentialBinding(
                request.OrganizationId,
                request.ProviderId,
                bindingReference,
                bindingVersion,
                source));

    private static ModelDeploymentCredentialBindingResult Failure(string outcomeCode) =>
        new(false, outcomeCode, null);
}
