using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationModelExecutionCompositionTests
{
    private static readonly EvaluationOwnership Ownership = EvaluationFixtures.Ownership();
    private static readonly FrozenModelIdentity SyntheticModel = EvaluationFixtures.Model();
    private static readonly Guid OrganizationId = Ownership.OrganizationId;
    private static readonly Guid EvaluationId = Guid.Parse("11111111-1111-4111-8111-111111111115");
    private static readonly Guid RequestId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab");
    private static readonly Guid InvocationAttemptId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbc");

    [Fact]
    public void Default_and_unknown_adapters_fail_closed()
    {
        foreach (var adapter in new[] { "", "fail_closed", "openai_compatible", "openrouter", "unknown.adapter" })
        {
            var composition = Compose(
                adapter,
                qualified: true,
                environmentName: "Development",
                workloadVerified: true,
                workloadProfile: EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor);

            Assert.False(composition.Qualified);
            Assert.IsType<FailClosedEvaluationModelExecutionPort>(composition.Port);
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Production_and_staging_reject_synthetic_development_even_when_qualified(
        string environmentName)
    {
        var composition = Compose(
            EvaluationModelAdapterKinds.SyntheticDevelopment,
            qualified: true,
            environmentName,
            workloadVerified: true,
            workloadProfile: EvaluationWorkloadIdentityProfiles.OAuthClientCredentialsJwt);

        Assert.Equal(EvaluationModelAdapterKinds.SyntheticDevelopment, composition.Adapter);
        Assert.False(composition.Qualified);
        Assert.IsType<FailClosedEvaluationModelExecutionPort>(composition.Port);
    }

    [Fact]
    public void Development_synthetic_requires_verified_workload_identity()
    {
        var composition = Compose(
            EvaluationModelAdapterKinds.SyntheticDevelopment,
            qualified: true,
            environmentName: "Development",
            workloadVerified: false,
            workloadProfile: EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor);

        Assert.False(composition.Qualified);
        Assert.IsType<FailClosedEvaluationModelExecutionPort>(composition.Port);
    }

    [Fact]
    public void Development_synthetic_requires_synthetic_configured_actor_profile()
    {
        var composition = Compose(
            EvaluationModelAdapterKinds.SyntheticDevelopment,
            qualified: true,
            environmentName: "Development",
            workloadVerified: true,
            workloadProfile: EvaluationWorkloadIdentityProfiles.OAuthClientCredentialsJwt);

        Assert.False(composition.Qualified);
        Assert.IsType<FailClosedEvaluationModelExecutionPort>(composition.Port);
    }

    [Fact]
    public void Development_synthetic_requires_matching_frozen_model_profile()
    {
        var mismatchedModel = FrozenModelIdentity.TryCreate(
            "mdl.other.profile",
            "mdl.other.profile.v1",
            new string('b', 64),
            "provider.synthetic",
            "organization_byok",
            EvaluationSyntheticDevelopmentModelProfile.CredentialBindingReference,
            EvaluationSyntheticDevelopmentModelProfile.CredentialBindingVersion).Value!;

        var composition = Compose(
            EvaluationModelAdapterKinds.SyntheticDevelopment,
            qualified: true,
            environmentName: "Development",
            workloadVerified: true,
            workloadProfile: EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor,
            frozenModel: mismatchedModel);

        Assert.False(composition.Qualified);
        Assert.IsType<FailClosedEvaluationModelExecutionPort>(composition.Port);
    }

    [Fact]
    public void Development_synthetic_requires_resolvable_non_revoked_credential_binding()
    {
        var catalog = new InMemoryEvaluationModelCredentialCatalog(
            EvaluationSyntheticDevelopmentModelProfile.CreateCatalogRecord() with { Revoked = true });

        var composition = Compose(
            EvaluationModelAdapterKinds.SyntheticDevelopment,
            qualified: true,
            environmentName: "Development",
            workloadVerified: true,
            workloadProfile: EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor,
            credentialCatalog: catalog);

        Assert.False(composition.Qualified);
        Assert.IsType<FailClosedEvaluationModelExecutionPort>(composition.Port);
    }

    [Fact]
    public void Development_synthetic_rejects_empty_organization()
    {
        var composition = EvaluationModelExecutionCompositionComposer.Compose(
            new EvaluationModelExecutionCompositionRequest(
                EvaluationModelAdapterKinds.SyntheticDevelopment,
                true,
                "Development",
                true,
                EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor,
                SyntheticModel,
                Guid.Empty));

        Assert.False(composition.Qualified);
        Assert.IsType<FailClosedEvaluationModelExecutionPort>(composition.Port);
    }

    [Fact]
    public void Development_synthetic_rejects_organization_scoped_catalog_mismatch()
    {
        var catalog = new InMemoryEvaluationModelCredentialCatalog(
            EvaluationSyntheticDevelopmentModelProfile.CreateCatalogRecord() with
            {
                OrganizationId = Guid.Parse("ffffffff-ffff-4fff-8fff-ffffffffffff"),
            });

        var composition = Compose(
            EvaluationModelAdapterKinds.SyntheticDevelopment,
            qualified: true,
            environmentName: "Development",
            workloadVerified: true,
            workloadProfile: EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor,
            credentialCatalog: catalog);

        Assert.False(composition.Qualified);
        Assert.IsType<FailClosedEvaluationModelExecutionPort>(composition.Port);
    }

    [Fact]
    public void Development_synthetic_succeeds_when_all_gates_pass()
    {
        var composition = Compose(
            EvaluationModelAdapterKinds.SyntheticDevelopment,
            qualified: true,
            environmentName: "Testing",
            workloadVerified: true,
            workloadProfile: EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor);

        Assert.True(composition.Qualified);
        Assert.Equal(EvaluationModelAdapterKinds.SyntheticDevelopment, composition.Adapter);
        Assert.IsType<SyntheticEvaluationModelExecutionAdapter>(composition.Port);
        Assert.NotNull(composition.CredentialCatalog);
    }

    [Fact]
    public async Task Fail_closed_composition_port_denies_execution()
    {
        var composition = Compose(
            "openai_compatible",
            qualified: true,
            environmentName: "Development",
            workloadVerified: true,
            workloadProfile: EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor);

        var result = await composition.Port.ExecuteAsync(
            CreateMinimalRequest(),
            CreateMinimalContext(),
            CancellationToken.None);

        var failed = Assert.IsType<EvaluationModelAttemptFailed>(result);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.AuthorizationDenied, failed.OutcomeCategory);
    }

    private static EvaluationModelExecutionComposition Compose(
        string adapter,
        bool qualified,
        string environmentName,
        bool workloadVerified,
        string workloadProfile,
        FrozenModelIdentity? frozenModel = null,
        IEvaluationModelCredentialCatalog? credentialCatalog = null) =>
        EvaluationModelExecutionCompositionComposer.Compose(
            new EvaluationModelExecutionCompositionRequest(
                adapter,
                qualified,
                environmentName,
                workloadVerified,
                workloadProfile,
                frozenModel ?? SyntheticModel,
                OrganizationId,
                credentialCatalog));

    private static EvaluationModelRequestV1 CreateMinimalRequest() =>
        new(
            "v1",
            EvaluationStableOwnershipReferenceFactory.StableRequestId(RequestId),
            EvaluationStableOwnershipReferenceFactory.StableInvocationAttemptId(InvocationAttemptId),
            "crit.judgment.quality",
            "crit.judgment.quality.v1",
            EvaluatorModes.AgentJudgment,
            EvaluationStableOwnershipReferenceFactory.ToSessionOwnershipRef(Ownership),
            "eval.input.agent-judgment.v1",
            "eval.output.agent-judgment.v1",
            "eval.instructions.p0.v1",
            SyntheticModel.ProfileId,
            SyntheticModel.ProfileVersion,
            SyntheticModel.ProfileDigest,
            SyntheticModel.CredentialBindingReference,
            [],
            null,
            4096);

    private static EvaluationModelExecutionContext CreateMinimalContext() =>
        new(
            EvaluationStableOwnershipReferenceFactory.ToSessionOwnershipRef(Ownership),
            Ownership,
            RequestId,
            EvaluationStableOwnershipReferenceFactory.StableRequestId(RequestId),
            InvocationAttemptId,
            EvaluationStableOwnershipReferenceFactory.StableInvocationAttemptId(InvocationAttemptId),
            EvaluationId,
            SyntheticModel,
            "eval.instructions.p0.v1",
            [],
            new Dictionary<string, Guid>(StringComparer.Ordinal),
            null,
            null);
}
