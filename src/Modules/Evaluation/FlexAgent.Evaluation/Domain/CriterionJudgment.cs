using System.Text;
using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationProcedureResolver
{
    public static EvaluationDecision<EvaluationProcedureV1> TryResolve(ReadOnlyMemory<byte> canonicalUtf8)
    {
        if (canonicalUtf8.Length is < 1 or > 1_048_576)
        {
            return EvaluationDecision<EvaluationProcedureV1>.Fail(EvaluationFailureCodes.InvalidProcedure);
        }

        if (!EvaluationProcedureDocumentParser.TryParse(canonicalUtf8, out var procedure, out _))
        {
            return EvaluationDecision<EvaluationProcedureV1>.Fail(EvaluationFailureCodes.InvalidProcedure);
        }

        return EvaluationDecision<EvaluationProcedureV1>.Ok(procedure);
    }
}

public static class CriterionJudgmentValidator
{
    public static EvaluationDecision<CriterionJudgment> TryCreate(
        EvaluationProcedureV1 procedure,
        CriterionJudgmentDraft draft)
    {
        var criterion = procedure.Criteria.FirstOrDefault(item =>
            string.Equals(item.CriterionId, draft.CriterionId, StringComparison.Ordinal)
            && string.Equals(item.CriterionVersion, draft.CriterionVersion, StringComparison.Ordinal));
        if (criterion is null)
        {
            return EvaluationDecision<CriterionJudgment>.Fail(EvaluationFailureCodes.InvalidJudgment, "criterion");
        }

        if (!string.Equals(criterion.EvaluatorMode, draft.EvaluatorMode, StringComparison.Ordinal)
            || !criterion.PermittedStatuses.Contains(draft.Status, StringComparer.Ordinal))
        {
            return EvaluationDecision<CriterionJudgment>.Fail(EvaluationFailureCodes.InvalidJudgment, "status");
        }

        if (draft.JudgmentId == Guid.Empty
            || draft.EvaluationId == Guid.Empty
            || !criterion.ConfidenceField.PermittedValues.Contains(draft.Confidence, StringComparer.Ordinal)
            || draft.Uncertainty.Count == 0
            || draft.Uncertainty.Count != draft.Uncertainty.Distinct(StringComparer.Ordinal).Count()
            || draft.Uncertainty.Any(item => !criterion.UncertaintyCategories.Contains(item, StringComparer.Ordinal))
            || RuneCount(draft.Rationale) is < 1 or > 4000
            || RuneCount(draft.Rationale) > criterion.RationaleMaxUnicodeScalars
            || ContainsProtectedContent(draft.Rationale)
            || draft.EvidenceIds.Count < criterion.EvidenceRequirements.MinimumItems
            || draft.EvidenceIds.Count > criterion.EvidenceRequirements.MaximumItems
            || draft.EvidenceIds.Count != draft.EvidenceIds.Distinct().Count()
            || draft.EvidenceIds.Any(id => id == Guid.Empty))
        {
            return EvaluationDecision<CriterionJudgment>.Fail(
                ContainsProtectedContent(draft.Rationale)
                    ? EvaluationFailureCodes.ProtectedContent
                    : EvaluationFailureCodes.InvalidJudgment);
        }

        if (draft.ProvisionalFeedback is not null)
        {
            if (!criterion.ProvisionalFeedbackPermitted
                || RuneCount(draft.ProvisionalFeedback) is < 1 or > 2000
                || ContainsProtectedContent(draft.ProvisionalFeedback))
            {
                return EvaluationDecision<CriterionJudgment>.Fail(
                    ContainsProtectedContent(draft.ProvisionalFeedback)
                        ? EvaluationFailureCodes.ProtectedContent
                        : EvaluationFailureCodes.InvalidJudgment,
                    "provisional_feedback");
            }
        }

        if (!ScoreMatches(criterion.ScoreField, draft.Score))
        {
            return EvaluationDecision<CriterionJudgment>.Fail(EvaluationFailureCodes.InvalidJudgment, "score");
        }

        if (criterion.EvaluatorMode is EvaluatorModes.Deterministic or EvaluatorModes.AgentAssisted
            && draft.DeterministicInvocationId is null)
        {
            return EvaluationDecision<CriterionJudgment>.Fail(EvaluationFailureCodes.InvalidJudgment, "deterministic_invocation");
        }

        if (criterion.EvaluatorMode == EvaluatorModes.AgentJudgment && draft.DeterministicInvocationId is not null)
        {
            return EvaluationDecision<CriterionJudgment>.Fail(EvaluationFailureCodes.DeterministicConflict, "deterministic_invocation");
        }

        return EvaluationDecision<CriterionJudgment>.Ok(
            new CriterionJudgment(
                draft.JudgmentId,
                draft.EvaluationId,
                draft.CriterionId,
                draft.CriterionVersion,
                draft.EvaluatorMode,
                draft.Status,
                draft.Confidence,
                draft.Uncertainty,
                draft.Rationale,
                draft.EvidenceIds,
                draft.Score,
                draft.ProvisionalFeedback,
                draft.DeterministicInvocationId));
    }

    private static bool ScoreMatches(object scoreField, object? score) =>
        scoreField switch
        {
            ScoreFieldNoneV1 => score is null,
            IntegerRangeScoreFieldV1 range => score is int value && value >= range.Minimum && value <= range.Maximum,
            EnumeratedDecisionScoreFieldV1 enumerated =>
                score is string value && enumerated.PermittedValues.Contains(value, StringComparer.Ordinal),
            _ => false,
        };

    private static int RuneCount(string value) => value.EnumerateRunes().Count();

    private static bool ContainsProtectedContent(string? value) =>
        EvaluationProhibitedModelOutputDisclosurePolicy.ContainsProhibitedDisclosure(value);
}

public sealed record CriterionJudgmentDraft(
    Guid JudgmentId,
    Guid EvaluationId,
    string CriterionId,
    string CriterionVersion,
    string EvaluatorMode,
    string Status,
    string Confidence,
    IReadOnlyList<string> Uncertainty,
    string Rationale,
    IReadOnlyList<Guid> EvidenceIds,
    object? Score,
    string? ProvisionalFeedback,
    Guid? DeterministicInvocationId);

public sealed record CriterionJudgment(
    Guid JudgmentId,
    Guid EvaluationId,
    string CriterionId,
    string CriterionVersion,
    string EvaluatorMode,
    string Status,
    string Confidence,
    IReadOnlyList<string> Uncertainty,
    string Rationale,
    IReadOnlyList<Guid> EvidenceIds,
    object? Score,
    string? ProvisionalFeedback,
    Guid? DeterministicInvocationId);
