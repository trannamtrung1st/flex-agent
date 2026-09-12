using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluationModelResponseValidationMatrixTests
{
    private static readonly EvaluationProcedureV1 Procedure = EvaluationFixtures.LoadSyntheticProcedure();
    private static readonly Guid EvaluationId = Guid.Parse("11111111-1111-4111-8111-111111111115");
    private static readonly Guid EvidenceId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid DeterministicInvocationId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public void Payload_digest_mismatch_fails_before_semantic_validation()
    {
        var response = CreateAssistedResponse(CriterionStatuses.Satisfied, "Structure is complete.");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);
        var forgedRef = new ProtectedPayloadRefV1(
            bound.ResponseRef.ProtectedRef,
            new string('f', 64));

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            forgedRef,
            Procedure,
            CreateExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.SchemaInvalid, result.OutcomeCategory);
        Assert.Equal("response_ref", result.Decision.Field);
    }

    [Fact]
    public void Citation_with_disallowed_source_type_is_rejected()
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        var expected = CreateExpectedInvocation() with
        {
            PermittedEvidenceSourceTypes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [evidenceStableId] = "session.transcript_item",
            },
        };
        var response = CreateAssistedResponse(CriterionStatuses.Satisfied, "Structure is complete.");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            expected,
            AssistedFacts("""{"valid":true}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.OutputSemanticInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.Decision.OutcomeCode);
        Assert.Equal("evidence_ids", result.Decision.Field);
    }

    [Fact]
    public void Aggregation_blocking_status_is_rejected_for_assisted_criterion()
    {
        var response = CreateAssistedResponse(CriterionStatuses.NotApplicable, "Not applicable.");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.OutputSemanticInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.Decision.OutcomeCode);
        Assert.Equal("status", result.Decision.Field);
    }

    private static EvaluationModelExpectedInvocation CreateExpectedInvocation()
    {
        var criterion = Procedure.Criteria.Single(item =>
            string.Equals(item.CriterionId, "crit.assisted.structure", StringComparison.Ordinal));
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        return new EvaluationModelExpectedInvocation(
            criterion,
            EvaluationId,
            DeterministicInvocationId,
            "dinv.synthetic.0002",
            new HashSet<string>(StringComparer.Ordinal)
            {
                evidenceStableId,
            },
            new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                [evidenceStableId] = EvidenceId,
            },
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [evidenceStableId] = "submission.direct_text",
            });
    }

    private static EvaluationModelResponseV1 CreateAssistedResponse(string status, string rationale) =>
        new(
            "v1",
            "eval.agent.assisted.output.v1",
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            EvaluatorModes.AgentAssisted,
            status,
            "high",
            ["ambiguous_language"],
            rationale,
            [EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId)],
            new ProtectedPayloadRefV1("prot.eval.res.matrix", new string('0', 64)),
            "pass",
            null,
            "dinv.synthetic.0002");

    private static Dictionary<string, EvaluationSafeFactProjection> AssistedFacts(string json) =>
        new(StringComparer.Ordinal)
        {
            ["detfact.synthetic.schema"] = new(
                "detfact.synthetic.schema",
                "detfact.synthetic.schema.v1",
                new string('c', 64),
                System.Text.Encoding.UTF8.GetBytes(json)),
        };
}
