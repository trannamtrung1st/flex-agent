using System.Text;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationModelExecutionServiceTests
{
    private static readonly EvaluationProcedureV1 Procedure = EvaluationFixtures.LoadSyntheticProcedure();
    private static readonly EvaluationModelExecutionService Service = new();
    private static readonly Guid EvaluationId = Guid.Parse("11111111-1111-4111-8111-111111111115");
    private static readonly Guid RequestId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab");
    private static readonly Guid EvidenceId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid DeterministicInvocationId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public async Task Deterministic_criterion_is_rejected_before_model_execution()
    {
        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.objective.word-count", "crit.objective.word-count.v1"),
            verifiedDeterministicFacts: null,
            CreateJudgmentContext(),
            new SyntheticEvaluationModelExecutionAdapter(),
            deterministicOutputStore: null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("evaluator_mode", result.Field);
    }

    [Fact]
    public async Task Agent_assisted_execution_requires_verified_deterministic_facts()
    {
        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            verifiedDeterministicFacts: null,
            CreateAssistedContext(),
            new SyntheticEvaluationModelExecutionAdapter(),
            new FakeOutputStore(AssistedFacts("""{"valid":true}""").Values.Single()),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public async Task Agent_assisted_execution_succeeds_and_passes_store_backed_facts_to_port()
    {
        var facts = AssistedFacts("""{"valid":true}""");
        var capturingPort = new CapturingEvaluationModelExecutionPort(new SyntheticEvaluationModelExecutionAdapter());

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            CreateAssistedContext(),
            capturingPort,
            new FakeOutputStore(facts.Values.Single()),
            CancellationToken.None);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(EvaluatorModes.AgentAssisted, result.Value!.EvaluatorMode);
        Assert.Equal(CriterionStatuses.Satisfied, result.Value.Status);
        Assert.Equal(EvaluationId, result.Value.EvaluationId);
        Assert.Equal(DeterministicInvocationId, result.Value.DeterministicInvocationId);
        Assert.NotNull(capturingPort.LastRequest);
        Assert.NotNull(capturingPort.LastRequest!.DeterministicFacts);
        Assert.Single(capturingPort.LastRequest.DeterministicFacts!);
        Assert.Equal(facts.Keys.Single(), capturingPort.LastRequest.DeterministicFacts![0].SourceId);
        Assert.Equal("prot.eval.fact.0002", capturingPort.LastRequest.DeterministicFacts![0].ProtectedRef.ProtectedRef);
    }

    [Fact]
    public async Task Provider_response_for_different_criterion_is_rejected()
    {
        var facts = AssistedFacts("""{"valid":true}""");

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            CreateAssistedContext(syntheticScenario: "wrong_criterion"),
            new SyntheticEvaluationModelExecutionAdapter(),
            new FakeOutputStore(facts.Values.Single()),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("criterion_id", result.Field);
    }

    [Fact]
    public async Task Fail_closed_port_denies_model_execution()
    {
        var facts = AssistedFacts("""{"valid":true}""");

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            CreateAssistedContext(),
            new FailClosedEvaluationModelExecutionPort(),
            new FakeOutputStore(facts.Values.Single()),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProcessingDisabled, result.OutcomeCode);
    }

    [Fact]
    public async Task Missing_store_material_fails_before_provider_call()
    {
        var facts = AssistedFacts("""{"valid":true}""");

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            CreateAssistedContext(),
            new SyntheticEvaluationModelExecutionAdapter(),
            new FakeOutputStore(projection: null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProtectedContent, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public async Task Synthetic_adapter_can_return_insufficient_evidence()
    {
        var facts = AssistedFacts("""{"valid":true}""");

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            CreateAssistedContext(syntheticScenario: "insufficient"),
            new SyntheticEvaluationModelExecutionAdapter(),
            new FakeOutputStore(facts.Values.Single()),
            CancellationToken.None);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(CriterionStatuses.InsufficientEvidence, result.Value!.Status);
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

    private static EvaluationModelExecutionContext CreateAssistedContext(string? syntheticScenario = null)
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        return new EvaluationModelExecutionContext(
            OwnershipRef(),
            EvaluationFixtures.Ownership(),
            RequestId,
            "ereq.synthetic.0001",
            "eatt.synthetic.0001",
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
            DeterministicInvocationId,
            "dinv.synthetic.0002",
            syntheticScenario);
    }

    private static EvaluationModelExecutionContext CreateJudgmentContext() =>
        new(
            OwnershipRef(),
            EvaluationFixtures.Ownership(),
            RequestId,
            "ereq.synthetic.0002",
            "eatt.synthetic.0002",
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
            null,
            null,
            null);

    private static SessionOwnershipRefV1 OwnershipRef() =>
        new(
            "org.synthetic.demo",
            "act.synthetic.demo",
            "part.synthetic.demo",
            "att.synthetic.demo",
            "sess.synthetic.demo");

    private sealed class CapturingEvaluationModelExecutionPort(IEvaluationModelExecutionPort inner) : IEvaluationModelExecutionPort
    {
        public EvaluationModelRequestV1? LastRequest { get; private set; }

        public async Task<EvaluationModelAttemptResult> ExecuteAsync(
            EvaluationModelRequestV1 request,
            EvaluationModelExecutionContext context,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return await inner.ExecuteAsync(request, context, cancellationToken);
        }
    }

    private sealed class FakeOutputStore(EvaluationSafeFactProjection? projection) : IProtectedDeterministicOutputStore
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
            Task.FromResult(projection);

        public Task<VerifiedDeterministicOutputMaterial?> TryLoadVerifiedMaterialAsync(
            Guid organizationId,
            Guid requestId,
            Guid deterministicAttemptId,
            string expectedContentDigest,
            string expectedCriterionId,
            string expectedCriterionVersion,
            CancellationToken cancellationToken)
        {
            if (projection is null
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
}
