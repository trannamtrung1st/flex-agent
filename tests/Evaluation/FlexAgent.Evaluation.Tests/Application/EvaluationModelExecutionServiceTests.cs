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
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public async Task Agent_assisted_execution_succeeds_and_passes_verified_facts_to_port()
    {
        var facts = AssistedFacts("""{"valid":true}""");
        var capturingPort = new CapturingEvaluationModelExecutionPort(new SyntheticEvaluationModelExecutionAdapter());

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            CreateAssistedContext(),
            capturingPort,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(EvaluatorModes.AgentAssisted, result.Value!.EvaluatorMode);
        Assert.Equal(CriterionStatuses.Satisfied, result.Value.Status);
        Assert.Equal(EvaluationId, result.Value.EvaluationId);
        Assert.Equal(DeterministicInvocationId, result.Value.DeterministicInvocationId);
        Assert.NotNull(capturingPort.LastRequest);
        Assert.NotNull(capturingPort.LastRequest!.DeterministicFacts);
        Assert.Single(capturingPort.LastRequest.DeterministicFacts!);
        Assert.Equal("detfact.synthetic.schema", capturingPort.LastRequest.DeterministicFacts![0].SourceId);
        Assert.Equal("eval.instructions.p0.v1", capturingPort.LastRequest.InstructionVersion);
        Assert.Equal("crit.assisted.structure", capturingPort.LastRequest.CriterionId);
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
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProcessingDisabled, result.OutcomeCode);
    }

    [Fact]
    public async Task Extra_unverified_deterministic_fact_fails_before_provider_call()
    {
        var facts = AssistedFacts("""{"valid":true}""");
        var context = CreateAssistedContext() with
        {
            DeterministicFactProtectedRefs = new Dictionary<string, ProtectedPayloadRefV1>(StringComparer.Ordinal)
            {
                ["detfact.synthetic.schema"] = new ProtectedPayloadRefV1("prot.eval.fact.0002", new string('c', 64)),
                ["detfact.synthetic.forged"] = new ProtectedPayloadRefV1("prot.eval.fact.forged", new string('f', 64)),
            },
        };

        var result = await Service.TryExecuteAsync(
            Procedure,
            new EvaluationModelInvocationContext("crit.assisted.structure", "crit.assisted.structure.v1"),
            facts,
            context,
            new SyntheticEvaluationModelExecutionAdapter(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
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
            CancellationToken.None);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(CriterionStatuses.InsufficientEvidence, result.Value!.Status);
    }

    private static Dictionary<string, EvaluationSafeFactProjection> AssistedFacts(string json) =>
        new(StringComparer.Ordinal)
        {
            ["detfact.synthetic.schema"] = new(
                "detfact.synthetic.schema",
                "detfact.synthetic.schema.v1",
                new string('c', 64),
                Encoding.UTF8.GetBytes(json)),
        };

    private static EvaluationModelExecutionContext CreateAssistedContext(string? syntheticScenario = null)
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        return new EvaluationModelExecutionContext(
            OwnershipRef(),
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
            new Dictionary<string, ProtectedPayloadRefV1>(StringComparer.Ordinal)
            {
                ["detfact.synthetic.schema"] = new ProtectedPayloadRefV1(
                    "prot.eval.fact.0002",
                    new string('c', 64)),
            },
            DeterministicInvocationId,
            "dinv.synthetic.0002",
            syntheticScenario);
    }

    private static EvaluationModelExecutionContext CreateJudgmentContext() =>
        new(
            OwnershipRef(),
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
}
