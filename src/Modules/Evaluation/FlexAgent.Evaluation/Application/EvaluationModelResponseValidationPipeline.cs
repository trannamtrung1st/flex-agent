using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationModelResponseValidationPipeline
{
    public static EvaluationModelResponseValidationResult Validate(
        ReadOnlySpan<byte> wireUtf8,
        ProtectedPayloadRefV1 wireResponseRef,
        EvaluationProcedureV1 procedure,
        EvaluationModelExpectedInvocation expected,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts)
    {
        var read = EvaluationModelResponseDocumentReader.Read(wireUtf8);
        if (!read.Succeeded || read.Value is null)
        {
            return EvaluationModelResponseValidationResult.Fail(
                EvaluationModelExecutionOutcomeCategories.SchemaInvalid,
                EvaluationDecision<CriterionJudgmentDraft>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    read.Field ?? "model_response"),
                null);
        }

        var payloadDigest = ProtectedModelResponseContentDigest.TryVerify(
            wireUtf8,
            read.Value.ResponseRef.ContentDigest);
        if (!payloadDigest.Succeeded)
        {
            return EvaluationModelResponseValidationResult.Fail(
                EvaluationModelExecutionOutcomeCategories.SchemaInvalid,
                EvaluationDecision<CriterionJudgmentDraft>.Fail(
                    payloadDigest.OutcomeCode,
                    payloadDigest.Field),
                null);
        }

        if (!ProtectedPayloadRefComparer.Matches(read.Value.ResponseRef, wireResponseRef))
        {
            return EvaluationModelResponseValidationResult.Fail(
                EvaluationModelExecutionOutcomeCategories.SchemaInvalid,
                EvaluationDecision<CriterionJudgmentDraft>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "response_ref"),
                null);
        }

        var semantic = EvaluationModelResponseValidator.TryValidate(
            procedure,
            expected,
            read.Value,
            verifiedDeterministicFacts);
        if (!semantic.Succeeded)
        {
            return EvaluationModelResponseValidationResult.Fail(
                EvaluationModelExecutionOutcomeCategories.OutputSemanticInvalid,
                semantic,
                read.Value.ResponseRef);
        }

        return EvaluationModelResponseValidationResult.Ok(semantic, read.Value.ResponseRef);
    }
}

public sealed record EvaluationModelResponseValidationResult(
    bool Succeeded,
    string OutcomeCategory,
    EvaluationDecision<CriterionJudgmentDraft> Decision,
    ProtectedPayloadRefV1? BoundResponseRef)
{
    public static EvaluationModelResponseValidationResult Ok(
        EvaluationDecision<CriterionJudgmentDraft> decision,
        ProtectedPayloadRefV1 boundResponseRef) =>
        new(
            true,
            EvaluationModelExecutionOutcomeCategories.Succeeded,
            decision,
            boundResponseRef);

    public static EvaluationModelResponseValidationResult Fail(
        string outcomeCategory,
        EvaluationDecision<CriterionJudgmentDraft> decision,
        ProtectedPayloadRefV1? boundResponseRef) =>
        new(false, outcomeCategory, decision, boundResponseRef);
}
