using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluationModelResponseWiderValidationMatrixTests
{
    private static readonly EvaluationProcedureV1 Procedure = EvaluationFixtures.LoadSyntheticProcedure();
    private static readonly Guid EvaluationId = Guid.Parse("11111111-1111-4111-8111-111111111115");
    private static readonly Guid EvidenceId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid DeterministicInvocationId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public void Agent_judgment_satisfied_response_passes_full_pipeline()
    {
        var response = CreateJudgmentResponse(CriterionStatuses.Satisfied, 3, "Quality matches the rubric.");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateJudgmentExpectedInvocation(),
            null);

        Assert.True(result.Succeeded, result.Decision.OutcomeCode);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.Succeeded, result.OutcomeCategory);
        Assert.Equal(CriterionStatuses.Satisfied, result.Decision.Value!.Status);
        Assert.Equal(3, result.Decision.Value.Score);
    }

    [Fact]
    public void Agent_assisted_insufficient_evidence_passes_full_pipeline()
    {
        var response = CreateAssistedResponse(
            CriterionStatuses.InsufficientEvidence,
            "Required sections are missing from the frozen Evidence.");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateAssistedExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.True(result.Succeeded, result.Decision.OutcomeCode);
        Assert.Equal(CriterionStatuses.InsufficientEvidence, result.Decision.Value!.Status);
    }

    [Fact]
    public void Agent_assisted_conflict_status_passes_full_pipeline()
    {
        var response = CreateAssistedResponse(
            CriterionStatuses.Conflict,
            "Deterministic schema validation conflicts with visible structure.");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateAssistedExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.True(result.Succeeded, result.Decision.OutcomeCode);
        Assert.Equal(CriterionStatuses.Conflict, result.Decision.Value!.Status);
    }

    [Fact]
    public void Agent_judgment_not_applicable_passes_full_pipeline()
    {
        var response = CreateJudgmentResponse(
            CriterionStatuses.NotApplicable,
            0,
            "The rubric quality dimension does not apply to this submission format.");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateJudgmentExpectedInvocation(),
            null);

        Assert.True(result.Succeeded, result.Decision.OutcomeCode);
        Assert.Equal(CriterionStatuses.NotApplicable, result.Decision.Value!.Status);
    }

    [Fact]
    public void Empty_evidence_ids_fail_schema_validation()
    {
        var response = CreateAssistedResponse(CriterionStatuses.Satisfied, "Structure is complete.") with
        {
            EvidenceIds = [],
        };
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateAssistedExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.SchemaInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.Decision.OutcomeCode);
        Assert.Equal("model_response", result.Decision.Field);
    }

    [Fact]
    public void Duplicate_evidence_ids_fail_schema_validation()
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        var response = CreateAssistedResponse(CriterionStatuses.Satisfied, "Structure is complete.") with
        {
            EvidenceIds = [evidenceStableId, evidenceStableId],
        };
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateAssistedExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.SchemaInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.Decision.OutcomeCode);
        Assert.Equal("model_response", result.Decision.Field);
    }

    [Fact]
    public void Invalid_uncertainty_category_fails_semantic_validation()
    {
        var response = CreateAssistedResponse(CriterionStatuses.Satisfied, "Structure is complete.") with
        {
            Uncertainty = ["evaluator_bound"],
        };
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateAssistedExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.OutputSemanticInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.Decision.OutcomeCode);
    }

    [Fact]
    public void Empty_uncertainty_list_fails_schema_validation()
    {
        var response = CreateAssistedResponse(CriterionStatuses.Satisfied, "Structure is complete.") with
        {
            Uncertainty = [],
        };
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateAssistedExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.SchemaInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.Decision.OutcomeCode);
        Assert.Equal("model_response", result.Decision.Field);
    }

    [Fact]
    public void Invalid_enumerated_score_fails_semantic_validation()
    {
        var response = CreateAssistedResponse(CriterionStatuses.Satisfied, "Structure is complete.") with
        {
            Score = "maybe",
        };
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateAssistedExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.OutputSemanticInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.Decision.OutcomeCode);
        Assert.Equal("score", result.Decision.Field);
    }

    [Fact]
    public void Rationale_exceeding_max_unicode_scalars_fails_semantic_validation()
    {
        var response = CreateAssistedResponse(
            CriterionStatuses.Satisfied,
            new string('x', 1501));
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateAssistedExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.OutputSemanticInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.Decision.OutcomeCode);
    }

    [Fact]
    public void Satisfied_assisted_response_conflicts_with_invalid_deterministic_fact()
    {
        var response = CreateAssistedResponse(CriterionStatuses.Satisfied, "Structure is complete.");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            bound.WireUtf8,
            bound.ResponseRef,
            Procedure,
            CreateAssistedExpectedInvocation(),
            AssistedFacts("""{"valid":false}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.OutputSemanticInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, result.Decision.OutcomeCode);
        Assert.Equal("status", result.Decision.Field);
    }

    [Fact]
    public void Schema_invalid_wire_bytes_fail_before_semantic_validation()
    {
        var wireUtf8 = """{"schema_version":"v1","status":"satisfied"}"""u8.ToArray();
        var responseRef = new ProtectedPayloadRefV1("prot.eval.res.invalid", new string('a', 64));

        var result = EvaluationModelResponseValidationPipeline.Validate(
            wireUtf8,
            responseRef,
            Procedure,
            CreateAssistedExpectedInvocation(),
            AssistedFacts("""{"valid":true}"""));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.SchemaInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.Decision.OutcomeCode);
        Assert.Equal("model_response", result.Decision.Field);
    }

    private static EvaluationModelExpectedInvocation CreateAssistedExpectedInvocation()
    {
        var criterion = Procedure.Criteria.Single(item =>
            string.Equals(item.CriterionId, "crit.assisted.structure", StringComparison.Ordinal));
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        return new EvaluationModelExpectedInvocation(
            criterion,
            EvaluationId,
            DeterministicInvocationId,
            EvaluationStableOwnershipReferenceFactory.StableDeterministicInvocationId(DeterministicInvocationId),
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

    private static EvaluationModelExpectedInvocation CreateJudgmentExpectedInvocation()
    {
        var criterion = Procedure.Criteria.Single(item =>
            string.Equals(item.CriterionId, "crit.judgment.quality", StringComparison.Ordinal));
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        return new EvaluationModelExpectedInvocation(
            criterion,
            EvaluationId,
            null,
            null,
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

    private static EvaluationModelResponseV1 CreateAssistedResponse(string status, string rationale)
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        return new EvaluationModelResponseV1(
            "v1",
            "eval.agent.assisted.output.v1",
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            EvaluatorModes.AgentAssisted,
            status,
            "high",
            ["ambiguous_language"],
            rationale,
            [evidenceStableId],
            new ProtectedPayloadRefV1("prot.eval.res.wider", new string('d', 64)),
            "pass",
            "Keep headings in the same order.",
            EvaluationStableOwnershipReferenceFactory.StableDeterministicInvocationId(DeterministicInvocationId));
    }

    private static EvaluationModelResponseV1 CreateJudgmentResponse(
        string status,
        int? score,
        string rationale)
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        return new EvaluationModelResponseV1(
            "v1",
            "eval.agent.judgment.output.v1",
            "crit.judgment.quality",
            "crit.judgment.quality.v1",
            EvaluatorModes.AgentJudgment,
            status,
            "medium",
            ["limited_context"],
            rationale,
            [evidenceStableId],
            new ProtectedPayloadRefV1("prot.eval.res.judgment", new string('d', 64)),
            score,
            "Keep examples concrete.",
            null);
    }

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
