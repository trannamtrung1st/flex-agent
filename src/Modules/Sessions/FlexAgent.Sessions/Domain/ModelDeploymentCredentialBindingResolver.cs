namespace FlexAgent.Sessions.Domain;

public static class ModelDeploymentCredentialBindingOutcomeCodes
{
    public const string Succeeded = "model_deployment_credential_binding.succeeded";
    public const string BindingMissing = "model_deployment_credential_binding.missing";
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

public static class ModelDeploymentCredentialBindingResolver
{
    public static ModelDeploymentCredentialBindingResult Resolve(
        ModelDeploymentCredentialBindingRequest request)
    {
        if (HasOrganizationBinding(request))
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

        if (HasDeploymentDefaultBinding(request))
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

    private static bool HasOrganizationBinding(ModelDeploymentCredentialBindingRequest request) =>
        !string.IsNullOrWhiteSpace(request.OrganizationBindingReference)
        && !string.IsNullOrWhiteSpace(request.OrganizationBindingVersion);

    private static bool HasDeploymentDefaultBinding(ModelDeploymentCredentialBindingRequest request) =>
        !string.IsNullOrWhiteSpace(request.DeploymentDefaultBindingReference)
        && !string.IsNullOrWhiteSpace(request.DeploymentDefaultBindingVersion);

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
