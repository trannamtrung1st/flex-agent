using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationModelResponseValidator
{
    public static EvaluationDecision<CriterionJudgmentDraft> TryValidateFromDocument(
        ReadOnlySpan<byte> canonicalUtf8,
        EvaluationProcedureV1 procedure,
        EvaluationModelExpectedInvocation expected,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts)
    {
        var read = EvaluationModelResponseDocumentReader.Read(canonicalUtf8);
        if (!read.Succeeded || read.Value is null)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                read.Field);
        }

        return TryValidate(procedure, expected, read.Value, verifiedDeterministicFacts);
    }

    public static EvaluationDecision<CriterionJudgmentDraft> TryValidate(
        EvaluationProcedureV1 procedure,
        EvaluationModelExpectedInvocation expected,
        EvaluationModelResponseV1 response,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(response);

        if (!EvaluationModelResponseIdentityValidator.TryValidate(expected, response, out var identityField))
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                identityField);
        }

        if (EvaluationModelResponseValidatorHelpers.TryDetectDeterministicConflict(
                expected.Criterion,
                response,
                verifiedDeterministicFacts))
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                EvaluationFailureCodes.DeterministicConflict,
                "status");
        }

        var draftDecision = EvaluationModelResponseMapper.TryCreateDraft(expected, response);
        if (!draftDecision.Succeeded || draftDecision.Value is null)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                draftDecision.OutcomeCode,
                draftDecision.Field);
        }

        var judgment = CriterionJudgmentValidator.TryCreate(procedure, draftDecision.Value);
        if (!judgment.Succeeded)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                judgment.OutcomeCode,
                judgment.Field);
        }

        return EvaluationDecision<CriterionJudgmentDraft>.Ok(draftDecision.Value);
    }
}

internal static class EvaluationModelResponseIdentityValidator
{
    internal static bool TryValidate(
        EvaluationModelExpectedInvocation expected,
        EvaluationModelResponseV1 response,
        out string? field)
    {
        field = null;
        if (!string.Equals(expected.Criterion.CriterionId, response.CriterionId, StringComparison.Ordinal))
        {
            field = "criterion_id";
            return false;
        }

        if (!string.Equals(expected.Criterion.CriterionVersion, response.CriterionVersion, StringComparison.Ordinal))
        {
            field = "criterion_version";
            return false;
        }

        if (!string.Equals(expected.Criterion.EvaluatorMode, response.EvaluatorMode, StringComparison.Ordinal))
        {
            field = "evaluator_mode";
            return false;
        }

        if (expected.Criterion.AgentIo is null
            || !string.Equals(expected.Criterion.AgentIo.OutputSchemaId, response.OutputSchemaId, StringComparison.Ordinal))
        {
            field = "output_schema_id";
            return false;
        }

        if (expected.Criterion.EvaluatorMode == EvaluatorModes.AgentAssisted)
        {
            if (expected.DeterministicInvocationId is null
                || string.IsNullOrWhiteSpace(expected.DeterministicInvocationStableId)
                || !string.Equals(
                    response.DeterministicInvocationId,
                    expected.DeterministicInvocationStableId,
                    StringComparison.Ordinal))
            {
                field = "deterministic_invocation_id";
                return false;
            }
        }
        else if (response.DeterministicInvocationId is not null)
        {
            field = "deterministic_invocation_id";
            return false;
        }

        return true;
    }
}

internal static class EvaluationModelResponseMapper
{
    internal static EvaluationDecision<CriterionJudgmentDraft> TryCreateDraft(
        EvaluationModelExpectedInvocation expected,
        EvaluationModelResponseV1 response)
    {
        if (response.EvidenceIds.Count == 0
            || response.EvidenceIds.Count != response.EvidenceIds.Distinct(StringComparer.Ordinal).Count())
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(EvaluationFailureCodes.InvalidJudgment, "evidence_ids");
        }

        var evidenceIds = new List<Guid>(response.EvidenceIds.Count);
        foreach (var evidenceId in response.EvidenceIds)
        {
            if (!expected.PermittedEvidenceStableIds.Contains(evidenceId)
                || !expected.PermittedEvidenceIdBindings.TryGetValue(evidenceId, out var evidenceGuid)
                || evidenceGuid == Guid.Empty)
            {
                return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "evidence_ids");
            }

            if (!expected.PermittedEvidenceSourceTypes.TryGetValue(evidenceId, out var sourceType)
                || !expected.Criterion.EvidenceRequirements.PermittedSourceTypes.Contains(
                    sourceType,
                    StringComparer.Ordinal))
            {
                return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "evidence_ids");
            }

            evidenceIds.Add(evidenceGuid);
        }

        return EvaluationDecision<CriterionJudgmentDraft>.Ok(
            new CriterionJudgmentDraft(
                Guid.CreateVersion7(),
                expected.EvaluationId,
                expected.Criterion.CriterionId,
                expected.Criterion.CriterionVersion,
                expected.Criterion.EvaluatorMode,
                response.Status,
                response.Confidence,
                response.Uncertainty,
                response.Rationale,
                evidenceIds,
                response.Score,
                response.ProvisionalFeedback,
                expected.DeterministicInvocationId));
    }
}

internal static class EvaluationModelResponseValidatorHelpers
{
    internal static bool TryDetectDeterministicConflict(
        EvaluationProcedureCriterionV1 criterion,
        EvaluationModelResponseV1 response,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts)
    {
        if (criterion.EvaluatorMode != EvaluatorModes.AgentAssisted
            || verifiedDeterministicFacts is null
            || verifiedDeterministicFacts.Count == 0
            || !string.Equals(response.Status, CriterionStatuses.Satisfied, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var fact in verifiedDeterministicFacts.Values)
        {
            var projection = System.Text.Encoding.UTF8.GetString(fact.ProjectionUtf8.Span);
            if (projection.Contains("\"valid\":false", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
