using System.Text;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationModelExecutionGreenMatrixTests
{
    private static readonly EvaluationProcedureV1 Procedure = EvaluationFixtures.LoadSyntheticProcedure();
    private static readonly EvaluationModelExecutionService Service = new();
    private static readonly EvaluationOwnership Ownership = EvaluationFixtures.Ownership();
    private static readonly Guid EvaluationId = Guid.Parse("11111111-1111-4111-8111-111111111115");
    private static readonly Guid RequestId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab");
    private static readonly Guid InvocationAttemptId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbc");
    private static readonly Guid EvidenceId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid DeterministicInvocationId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public async Task Agent_judgment_satisfied_passes_end_to_end()
    {
        var result = await ExecuteJudgmentAsync(
            new SyntheticEvaluationModelExecutionAdapter(),
            providerArtifactStore: null);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(CriterionStatuses.Satisfied, result.Value!.Status);
        Assert.Equal(3, result.Value.Score);
    }

    [Fact]
    public async Task Agent_assisted_conflict_status_passes_end_to_end()
    {
        var result = await ExecuteAssistedAsync(
            syntheticScenario: "conflict",
            providerArtifactStore: null);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(CriterionStatuses.Conflict, result.Value!.Status);
    }

    [Fact]
    public async Task Agent_assisted_insufficient_evidence_passes_end_to_end()
    {
        var result = await ExecuteAssistedAsync(
            syntheticScenario: "insufficient",
            providerArtifactStore: null);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(CriterionStatuses.InsufficientEvidence, result.Value!.Status);
    }

    [Fact]
    public async Task Adapter_provider_unavailable_persists_failed_artifact()
    {
        var store = new RecordingProviderArtifactStore();

        var result = await ExecuteJudgmentAsync(
            new SyntheticEvaluationModelExecutionAdapter(),
            providerArtifactStore: store,
            syntheticScenario: "provider_unavailable");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal(1, store.AppendCount);
        Assert.Equal(ProviderArtifactOutcomes.Failed, store.LastCommand!.Outcome);
        Assert.Equal(
            EvaluationModelExecutionOutcomeCategories.ProviderUnavailable,
            store.LastCommand.FailureCategory);
        Assert.Null(store.LastCommand.ProtectedResponseRef);
    }

    [Fact]
    public async Task Adapter_schema_invalid_persists_invalid_output_artifact()
    {
        var store = new RecordingProviderArtifactStore();

        var result = await ExecuteJudgmentAsync(
            new SyntheticEvaluationModelExecutionAdapter(),
            providerArtifactStore: store,
            syntheticScenario: "schema_invalid");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal(1, store.AppendCount);
        Assert.Equal(ProviderArtifactOutcomes.InvalidOutput, store.LastCommand!.Outcome);
        Assert.Equal(
            EvaluationModelExecutionOutcomeCategories.SchemaInvalid,
            store.LastCommand.FailureCategory);
    }

    [Fact]
    public async Task Adapter_timeout_persists_timed_out_artifact_for_retry_visibility()
    {
        var store = new RecordingProviderArtifactStore();

        var result = await ExecuteJudgmentAsync(
            new SyntheticEvaluationModelExecutionAdapter(),
            providerArtifactStore: store,
            syntheticScenario: "timeout");

        Assert.False(result.Succeeded);
        Assert.Equal(1, store.AppendCount);
        Assert.Equal(ProviderArtifactOutcomes.TimedOut, store.LastCommand!.Outcome);
        Assert.Equal(
            EvaluationModelExecutionOutcomeCategories.ProviderTimeout,
            store.LastCommand.FailureCategory);
    }

    [Fact]
    public async Task Equivalent_reexecution_appends_distinct_provider_artifacts()
    {
        var store = new RecordingProviderArtifactStore();
        var context = CreateJudgmentContext();

        var first = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.judgment.quality", "crit.judgment.quality.v1"),
            verifiedDeterministicFacts: null,
            context,
            new SyntheticEvaluationModelExecutionAdapter(),
            CreateAuthorityStore(),
            deterministicOutputStore: null,
            store,
            CancellationToken.None);
        var second = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.judgment.quality", "crit.judgment.quality.v1"),
            verifiedDeterministicFacts: null,
            context,
            new SyntheticEvaluationModelExecutionAdapter(),
            CreateAuthorityStore(),
            deterministicOutputStore: null,
            store,
            CancellationToken.None);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(2, store.AppendCount);
        Assert.Equal(ProviderArtifactOutcomes.Succeeded, store.LastCommand!.Outcome);
    }

    [Fact]
    public async Task Disclosure_minimization_rejects_hidden_prompt_end_to_end()
    {
        var result = await ExecuteAssistedAsync(
            syntheticScenario: "disclosed_hidden_prompt",
            providerArtifactStore: null);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProtectedContent, result.OutcomeCode);
    }

    [Fact]
    public async Task Disclosure_minimization_allows_describe_injection_end_to_end()
    {
        var result = await ExecuteAssistedAsync(
            syntheticScenario: "injection_rationale",
            providerArtifactStore: null);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Contains(
            "change the rubric",
            result.Value!.Rationale,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Deterministic_conflict_rejects_satisfied_with_spaced_false_fact()
    {
        var facts = AssistedFacts("""{"valid": false, "schema": "eval.agent.assisted.input.v1"}""");

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            CreateAssistedContext("default"),
            new SyntheticEvaluationModelExecutionAdapter(),
            CreateAuthorityStore(),
            new FakeOutputStore(facts.Values.Single()),
            providerArtifactStore: null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, result.OutcomeCode);
        Assert.Equal("status", result.Field);
    }

    [Fact]
    public async Task Qualified_composition_port_executes_judgment_end_to_end()
    {
        var composition = EvaluationModelExecutionCompositionComposer.Compose(
            new EvaluationModelExecutionCompositionRequest(
                EvaluationModelAdapterKinds.SyntheticDevelopment,
                true,
                "Testing",
                true,
                EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor,
                EvaluationFixtures.Model(),
                Ownership.OrganizationId));

        Assert.True(composition.Qualified);

        var result = await ExecuteJudgmentAsync(composition.Port!, providerArtifactStore: null);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(CriterionStatuses.Satisfied, result.Value!.Status);
    }

    [Fact]
    public async Task Fail_closed_composition_port_denies_before_response_validation()
    {
        var composition = EvaluationModelExecutionCompositionComposer.Compose(
            new EvaluationModelExecutionCompositionRequest(
                EvaluationModelAdapterKinds.SyntheticDevelopment,
                false,
                "Testing",
                true,
                EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor,
                EvaluationFixtures.Model(),
                Ownership.OrganizationId));

        var store = new RecordingProviderArtifactStore();
        var result = await ExecuteJudgmentAsync(
            composition.Port!,
            providerArtifactStore: store);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProcessingDisabled, result.OutcomeCode);
        Assert.Equal(1, store.AppendCount);
        Assert.Equal(ProviderArtifactOutcomes.Failed, store.LastCommand!.Outcome);
        Assert.Equal(
            EvaluationModelExecutionOutcomeCategories.AuthorizationDenied,
            store.LastCommand.FailureCategory);
        Assert.Null(store.LastCommand.ProtectedResponseRef);
    }

    private static async Task<EvaluationDecision<CriterionJudgmentDraft>> ExecuteJudgmentAsync(
        IEvaluationModelExecutionPort port,
        IEvaluationProviderArtifactStore? providerArtifactStore,
        string? syntheticScenario = null) =>
        await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.judgment.quality", "crit.judgment.quality.v1"),
            verifiedDeterministicFacts: null,
            CreateJudgmentContext(syntheticScenario),
            port,
            CreateAuthorityStore(),
            deterministicOutputStore: null,
            providerArtifactStore,
            CancellationToken.None);

    private static async Task<EvaluationDecision<CriterionJudgmentDraft>> ExecuteAssistedAsync(
        string syntheticScenario,
        IEvaluationProviderArtifactStore? providerArtifactStore)
    {
        var facts = AssistedFacts("""{"valid":true}""");
        return await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            CreateAssistedContext(syntheticScenario),
            new SyntheticEvaluationModelExecutionAdapter(),
            CreateAuthorityStore(),
            new FakeOutputStore(facts.Values.Single()),
            providerArtifactStore,
            CancellationToken.None);
    }

    private static Dictionary<string, EvaluationSafeFactProjection> AssistedFacts(string json)
    {
        var digest = new string('c', 64);
        var sourceId = EvaluationEvidenceSourceIdentity.DeterministicFactSourceId(DeterministicInvocationId);
        return new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
        {
            [sourceId] = new(
                sourceId,
                EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(digest),
                digest,
                Encoding.UTF8.GetBytes(json)),
        };
    }

    private static EvaluationModelExecutionContext CreateAssistedContext(string syntheticScenario)
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        return new EvaluationModelExecutionContext(
            EvaluationStableOwnershipReferenceFactory.ToSessionOwnershipRef(Ownership),
            Ownership,
            RequestId,
            EvaluationStableOwnershipReferenceFactory.StableRequestId(RequestId),
            InvocationAttemptId,
            EvaluationStableOwnershipReferenceFactory.StableInvocationAttemptId(InvocationAttemptId),
            EvaluationId,
            EvaluationFixtures.Model(),
            "eval.instructions.p0.v1",
            [
                new EvaluationModelPermittedEvidenceV1(
                    evidenceStableId,
                    "submission.direct_text",
                    new string('b', 64)),
            ],
            new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                [evidenceStableId] = EvidenceId,
            },
            EvaluationFixtures.VerifiedPermittedEvidence(
                evidenceStableId,
                EvidenceId,
                "submission.direct_text",
                new string('b', 64)),
            DeterministicInvocationId,
            EvaluationStableOwnershipReferenceFactory.StableDeterministicInvocationId(DeterministicInvocationId),
            syntheticScenario);
    }

    private static EvaluationModelExecutionContext CreateJudgmentContext(string? syntheticScenario = null) =>
        new(
            EvaluationStableOwnershipReferenceFactory.ToSessionOwnershipRef(Ownership),
            Ownership,
            RequestId,
            EvaluationStableOwnershipReferenceFactory.StableRequestId(RequestId),
            InvocationAttemptId,
            EvaluationStableOwnershipReferenceFactory.StableInvocationAttemptId(InvocationAttemptId),
            EvaluationId,
            EvaluationFixtures.Model(),
            "eval.instructions.p0.v1",
            [
                new EvaluationModelPermittedEvidenceV1(
                    EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId),
                    "submission.direct_text",
                    new string('b', 64)),
            ],
            new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                [EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId)] = EvidenceId,
            },
            EvaluationFixtures.VerifiedPermittedEvidence(
                EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId),
                EvidenceId),
            null,
            null,
            syntheticScenario);

    private static FixedAuthorityStore CreateAuthorityStore() =>
        new(
            new AdmittedEvaluationRequestAuthority(
                RequestId,
                InvocationAttemptId,
                Ownership,
                new string('f', 64),
                EvaluationFixtures.SyntheticProcedureRef(),
                EvaluatorRegistryVersions.P0));

    private sealed class FixedAuthorityStore(AdmittedEvaluationRequestAuthority authority) : IEvaluationRequestAuthorityStore
    {
        public Task<AdmittedEvaluationRequestAuthority?> TryLoadAsync(
            EvaluationOwnership ownership,
            Guid requestId,
            Guid invocationAttemptId,
            CancellationToken cancellationToken)
        {
            if (requestId != authority.RequestId
                || invocationAttemptId != authority.InvocationAttemptId
                || ownership.OrganizationId != authority.Ownership.OrganizationId)
            {
                return Task.FromResult<AdmittedEvaluationRequestAuthority?>(null);
            }

            return Task.FromResult<AdmittedEvaluationRequestAuthority?>(authority);
        }
    }

    private sealed class FakeOutputStore(EvaluationSafeFactProjection projection) : IProtectedDeterministicOutputStore
    {
        public Task<EvaluationDecision<bool>> TryPersistAsync(
            ProtectedDeterministicOutputPersistCommand command,
            CancellationToken cancellationToken) =>
            Task.FromResult(EvaluationDecision<bool>.Ok(true));

        public Task<EvaluationSafeFactProjection?> TryLoadProjectionAsync(
            Guid organizationId,
            Guid requestId,
            Guid deterministicAttemptId,
            string expectedContentDigest,
            string expectedCriterionId,
            string expectedCriterionVersion,
            CancellationToken cancellationToken) =>
            Task.FromResult<EvaluationSafeFactProjection?>(projection);

        public Task<VerifiedDeterministicOutputMaterial?> TryLoadVerifiedMaterialAsync(
            EvaluationOwnership ownership,
            Guid requestId,
            Guid deterministicAttemptId,
            string expectedContentDigest,
            string expectedCriterionId,
            string expectedCriterionVersion,
            CancellationToken cancellationToken)
        {
            if (ownership.OrganizationId != Ownership.OrganizationId
                || requestId != RequestId
                || deterministicAttemptId != DeterministicInvocationId
                || !string.Equals(expectedCriterionId, "crit.assisted.structure", StringComparison.Ordinal)
                || !string.Equals(expectedCriterionVersion, "crit.assisted.structure.v1", StringComparison.Ordinal)
                || !string.Equals(expectedContentDigest, projection.ContentDigest, StringComparison.Ordinal))
            {
                return Task.FromResult<VerifiedDeterministicOutputMaterial?>(null);
            }

            return Task.FromResult<VerifiedDeterministicOutputMaterial?>(
                new VerifiedDeterministicOutputMaterial(
                    projection,
                    new ProtectedPayloadRefV1("prot.eval.fact.0002", projection.ContentDigest)));
        }
    }

    private sealed class RecordingProviderArtifactStore : IEvaluationProviderArtifactStore
    {
        private readonly InMemoryEvaluationProviderArtifactStore _inner = new();

        public int AppendCount { get; private set; }

        public ProviderArtifactAppendCommand? LastCommand { get; private set; }

        public Task<EvaluationDecision<Guid>> TryAppendAsync(
            ProviderArtifactAppendCommand command,
            CancellationToken cancellationToken)
        {
            AppendCount++;
            LastCommand = command;
            return _inner.TryAppendAsync(command, cancellationToken);
        }
    }
}
