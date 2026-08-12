using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class ModelDeploymentCredentialBindingResolverTests
{
    private static readonly Guid OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Resolve_prefers_organization_binding_when_valid()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: "binding.org.0001",
            OrganizationBindingVersion: "v1",
            DeploymentDefaultBindingReference: "binding.deploy.0001",
            DeploymentDefaultBindingVersion: "v1",
            OrganizationBindingRevoked: false,
            OrganizationBindingProviderMismatch: false,
            OrganizationBindingWrongOrganization: false);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("binding.org.0001", result.Binding!.BindingReference);
        Assert.Equal("v1", result.Binding.BindingVersion);
        Assert.Equal(ModelDeploymentCredentialBindingSource.Organization, result.Binding.Source);
    }

    [Fact]
    public void Resolve_uses_deployment_default_when_organization_binding_is_absent()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: null,
            OrganizationBindingVersion: null,
            DeploymentDefaultBindingReference: "binding.deploy.0001",
            DeploymentDefaultBindingVersion: "v1",
            OrganizationBindingRevoked: false,
            OrganizationBindingProviderMismatch: false,
            OrganizationBindingWrongOrganization: false);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("binding.deploy.0001", result.Binding!.BindingReference);
        Assert.Equal(ModelDeploymentCredentialBindingSource.DeploymentDefault, result.Binding.Source);
    }

    [Fact]
    public void Resolve_fails_closed_when_no_binding_is_available()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: null,
            OrganizationBindingVersion: null,
            DeploymentDefaultBindingReference: null,
            DeploymentDefaultBindingVersion: null,
            OrganizationBindingRevoked: false,
            OrganizationBindingProviderMismatch: false,
            OrganizationBindingWrongOrganization: false);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(ModelDeploymentCredentialBindingOutcomeCodes.BindingMissing, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_when_organization_binding_is_revoked()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: "binding.org.0001",
            OrganizationBindingVersion: "v1",
            DeploymentDefaultBindingReference: "binding.deploy.0001",
            DeploymentDefaultBindingVersion: "v1",
            OrganizationBindingRevoked: true,
            OrganizationBindingProviderMismatch: false,
            OrganizationBindingWrongOrganization: false);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(ModelDeploymentCredentialBindingOutcomeCodes.BindingRevoked, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_when_organization_binding_has_wrong_organization()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: "binding.org.0001",
            OrganizationBindingVersion: "v1",
            DeploymentDefaultBindingReference: "binding.deploy.0001",
            DeploymentDefaultBindingVersion: "v1",
            OrganizationBindingRevoked: false,
            OrganizationBindingProviderMismatch: false,
            OrganizationBindingWrongOrganization: true);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(ModelDeploymentCredentialBindingOutcomeCodes.BindingWrongOrganization, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_when_organization_binding_provider_mismatches()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: "binding.org.0001",
            OrganizationBindingVersion: "v1",
            DeploymentDefaultBindingReference: null,
            DeploymentDefaultBindingVersion: null,
            OrganizationBindingRevoked: false,
            OrganizationBindingProviderMismatch: true,
            OrganizationBindingWrongOrganization: false);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(ModelDeploymentCredentialBindingOutcomeCodes.BindingProviderMismatch, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_does_not_fall_back_to_deployment_default_when_organization_binding_is_revoked()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: "binding.org.0001",
            OrganizationBindingVersion: "v1",
            DeploymentDefaultBindingReference: "binding.deploy.0001",
            DeploymentDefaultBindingVersion: "v1",
            OrganizationBindingRevoked: true,
            OrganizationBindingProviderMismatch: false,
            OrganizationBindingWrongOrganization: false);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.NotEqual(ModelDeploymentCredentialBindingOutcomeCodes.Succeeded, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_when_organization_binding_reference_is_present_without_version()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: "binding.org.0001",
            OrganizationBindingVersion: null,
            DeploymentDefaultBindingReference: "binding.deploy.0001",
            DeploymentDefaultBindingVersion: "v1",
            OrganizationBindingRevoked: false,
            OrganizationBindingProviderMismatch: false,
            OrganizationBindingWrongOrganization: false);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(ModelDeploymentCredentialBindingOutcomeCodes.BindingIncomplete, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_when_deployment_default_binding_version_is_present_without_reference()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: null,
            OrganizationBindingVersion: null,
            DeploymentDefaultBindingReference: null,
            DeploymentDefaultBindingVersion: "v1",
            OrganizationBindingRevoked: false,
            OrganizationBindingProviderMismatch: false,
            OrganizationBindingWrongOrganization: false);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(ModelDeploymentCredentialBindingOutcomeCodes.BindingIncomplete, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_when_deployment_default_binding_provider_mismatches()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: null,
            OrganizationBindingVersion: null,
            DeploymentDefaultBindingReference: "binding.deploy.0001",
            DeploymentDefaultBindingVersion: "v1",
            OrganizationBindingRevoked: false,
            OrganizationBindingProviderMismatch: false,
            OrganizationBindingWrongOrganization: false,
            DeploymentDefaultBindingProviderMismatch: true);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(ModelDeploymentCredentialBindingOutcomeCodes.BindingProviderMismatch, result.OutcomeCode);
    }

    [Fact]
    public void Resolved_binding_exposes_only_opaque_reference_not_credential_material()
    {
        var request = new ModelDeploymentCredentialBindingRequest(
            OrganizationId,
            "openai",
            OrganizationBindingReference: "binding.org.0001",
            OrganizationBindingVersion: "v1",
            DeploymentDefaultBindingReference: null,
            DeploymentDefaultBindingVersion: null,
            OrganizationBindingRevoked: false,
            OrganizationBindingProviderMismatch: false,
            OrganizationBindingWrongOrganization: false);

        var result = ModelDeploymentCredentialBindingResolver.Resolve(request);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain("secret", result.Binding!.BindingReference, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("key", result.Binding.BindingReference, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", result.Binding.BindingReference, StringComparison.OrdinalIgnoreCase);
    }
}
