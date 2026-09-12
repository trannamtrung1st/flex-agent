using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationModelResponseValidationPipelineTests
{
    private static readonly EvaluationProcedureV1 Procedure = EvaluationFixtures.LoadSyntheticProcedure();
    private static readonly Guid EvaluationId = Guid.Parse("11111111-1111-4111-8111-111111111115");
    private static readonly Guid EvidenceId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid DeterministicInvocationId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public void Response_ref_mismatch_fails_before_semantic_validation()
    {
        var facts = AssistedFacts("""{"valid":true}""");
        var response = CreateAssistedResponse(CriterionStatuses.Satisfied, "Structure is complete.");
        var wireUtf8 = EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(response);
        var mismatchedRef = new ProtectedPayloadRefV1("prot.eval.res.forged", new string('f', 64));

        var result = EvaluationModelResponseValidationPipeline.Validate(
            wireUtf8,
            mismatchedRef,
            Procedure,
            CreateExpectedInvocation(),
            facts);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.SchemaInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.Decision.OutcomeCode);
        Assert.Equal("response_ref", result.Decision.Field);
        Assert.Null(result.BoundResponseRef);
    }

    [Fact]
    public void Semantic_failure_uses_output_semantic_invalid_category()
    {
        var facts = AssistedFacts("""{"valid":true}""");
        var response = CreateAssistedResponse(CriterionStatuses.Satisfied, "Structure is complete.") with
        {
            CriterionId = "crit.judgment.quality",
            CriterionVersion = "crit.judgment.quality.v1",
            EvaluatorMode = EvaluatorModes.AgentJudgment,
            OutputSchemaId = "eval.agent.judgment.output.v1",
            DeterministicInvocationId = null,
        };
        var wireUtf8 = EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(response);

        var result = EvaluationModelResponseValidationPipeline.Validate(
            wireUtf8,
            response.ResponseRef,
            Procedure,
            CreateExpectedInvocation(),
            facts);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.OutputSemanticInvalid, result.OutcomeCategory);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.Decision.OutcomeCode);
        Assert.Equal("criterion_id", result.Decision.Field);
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
            new ProtectedPayloadRefV1("prot.eval.res.pipeline", new string('d', 64)),
            "pass",
            null,
            "dinv.synthetic.0002");

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
                System.Text.Encoding.UTF8.GetBytes(json)),
        };
    }
}
