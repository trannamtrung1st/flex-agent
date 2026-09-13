using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationCompletionAuthorityVerifier
{
    public static EvaluationDecision<CompletedEvaluation> TryVerify(
        EvaluationRequest authoritativeRequest,
        EvaluationProcedureV1 procedure,
        EvaluationCompletionCommand command,
        IReadOnlyList<EvidenceItem> authoritativeEvidenceItems,
        IReadOnlyList<EvaluationEvidenceLocatorRecord>? authoritativeLocatorRecords = null)
    {
        ArgumentNullException.ThrowIfNull(authoritativeRequest);
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(authoritativeEvidenceItems);

        if (command.Completed.Ownership != authoritativeRequest.FrozenInput.Ownership)
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                EvaluationFailureCodes.IncompleteOwnership);
        }

        if (command.Completed.RequestId != authoritativeRequest.RequestId)
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                EvaluationCompletionOutcomeCodes.IntegrityConflict,
                "request_id");
        }

        if (command.Completed.ProcedureRef != authoritativeRequest.FrozenInput.Rubric)
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                EvaluationCompletionOutcomeCodes.IntegrityConflict,
                "procedure_ref");
        }

        if (command.Completed.FrozenInput != authoritativeRequest.FrozenInput)
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                EvaluationCompletionOutcomeCodes.IntegrityConflict,
                "frozen_input");
        }

        if (command.Completed.PredecessorEvaluationId != authoritativeRequest.PredecessorEvaluationId)
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                EvaluationCompletionOutcomeCodes.IntegrityConflict,
                "predecessor");
        }

        var validatedJudgments = new List<CriterionJudgment>(command.Judgments.Count);
        foreach (var judgment in command.Judgments)
        {
            var validated = CriterionJudgmentValidator.TryCreate(
                procedure,
                ToDraft(judgment));
            if (!validated.Succeeded || validated.Value is null)
            {
                return EvaluationDecision<CompletedEvaluation>.Fail(
                    validated.OutcomeCode,
                    validated.Field);
            }

            if (!JudgmentMatches(validated.Value, judgment))
            {
                return EvaluationDecision<CompletedEvaluation>.Fail(
                    EvaluationCompletionOutcomeCodes.IntegrityConflict,
                    "judgment");
            }

            validatedJudgments.Add(validated.Value);
        }

        var expectedSealDigest = authoritativeLocatorRecords is { Count: > 0 }
            ? EvaluationCompletionEvidenceSeal.TryComputeFromPersistedRecords(
                command.Completed.EvidenceSetId,
                command.InvocationAttemptId,
                authoritativeRequest.FrozenInput,
                authoritativeLocatorRecords)
            : EvaluationCompletionEvidenceSeal.TryComputeExpectedDigest(
                command.Completed.EvidenceSetId,
                command.InvocationAttemptId,
                authoritativeRequest.FrozenInput,
                authoritativeEvidenceItems);
        if (!expectedSealDigest.Succeeded || expectedSealDigest.Value is null)
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                expectedSealDigest.OutcomeCode,
                expectedSealDigest.Field);
        }

        if (!string.Equals(
                command.Completed.EvidenceSetDigest,
                expectedSealDigest.Value,
                StringComparison.Ordinal))
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                EvaluationCompletionOutcomeCodes.IntegrityConflict,
                "evidence_set_digest");
        }

        var evidenceSet = EvidenceSet.TryCreate(
            command.Completed.EvidenceSetId,
            command.Completed.EvaluationId,
            authoritativeRequest.FrozenInput.Ownership,
            authoritativeEvidenceItems,
            expectedSealDigest.Value);
        if (!evidenceSet.Succeeded || evidenceSet.Value is null)
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                evidenceSet.OutcomeCode,
                evidenceSet.Field);
        }

        var completingRequest = authoritativeRequest with
        {
            State = EvaluationRequestStates.Completing,
        };
        var validatedCompletion = CompletedEvaluation.TryCreate(
            command.Completed.EvaluationId,
            completingRequest,
            procedure,
            evidenceSet.Value,
            authoritativeEvidenceItems,
            validatedJudgments,
            command.Completed.CompletedAtUtc,
            command.Completed.CreationServiceId);
        if (!validatedCompletion.Succeeded || validatedCompletion.Value is null)
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                validatedCompletion.OutcomeCode,
                validatedCompletion.Field);
        }

        if (!CompletionMatches(validatedCompletion.Value, command.Completed))
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                EvaluationCompletionOutcomeCodes.IntegrityConflict,
                "completed");
        }

        return validatedCompletion;
    }

    private static CriterionJudgmentDraft ToDraft(CriterionJudgment judgment) =>
        new(
            judgment.JudgmentId,
            judgment.EvaluationId,
            judgment.CriterionId,
            judgment.CriterionVersion,
            judgment.EvaluatorMode,
            judgment.Status,
            judgment.Confidence,
            judgment.Uncertainty,
            judgment.Rationale,
            judgment.EvidenceIds,
            judgment.Score,
            judgment.ProvisionalFeedback,
            judgment.DeterministicInvocationId);

    private static bool JudgmentMatches(CriterionJudgment expected, CriterionJudgment actual) =>
        expected.JudgmentId == actual.JudgmentId
        && expected.EvaluationId == actual.EvaluationId
        && string.Equals(expected.CriterionId, actual.CriterionId, StringComparison.Ordinal)
        && string.Equals(expected.CriterionVersion, actual.CriterionVersion, StringComparison.Ordinal)
        && string.Equals(expected.EvaluatorMode, actual.EvaluatorMode, StringComparison.Ordinal)
        && string.Equals(expected.Status, actual.Status, StringComparison.Ordinal)
        && string.Equals(expected.Confidence, actual.Confidence, StringComparison.Ordinal)
        && expected.Uncertainty.SequenceEqual(actual.Uncertainty)
        && string.Equals(expected.Rationale, actual.Rationale, StringComparison.Ordinal)
        && expected.EvidenceIds.SequenceEqual(actual.EvidenceIds)
        && Equals(expected.Score, actual.Score)
        && string.Equals(expected.ProvisionalFeedback, actual.ProvisionalFeedback, StringComparison.Ordinal)
        && expected.DeterministicInvocationId == actual.DeterministicInvocationId;

    private static bool CompletionMatches(CompletedEvaluation expected, CompletedEvaluation actual) =>
        expected.EvaluationId == actual.EvaluationId
        && expected.RequestId == actual.RequestId
        && expected.Ownership == actual.Ownership
        && expected.ProcedureRef == actual.ProcedureRef
        && expected.FrozenInput == actual.FrozenInput
        && expected.EvidenceSetId == actual.EvidenceSetId
        && string.Equals(expected.EvidenceSetDigest, actual.EvidenceSetDigest, StringComparison.Ordinal)
        && expected.CriterionIds.SequenceEqual(actual.CriterionIds)
        && string.Equals(expected.AggregateStatus, actual.AggregateStatus, StringComparison.Ordinal)
        && expected.CompletedAtUtc == actual.CompletedAtUtc
        && string.Equals(expected.CreationServiceId, actual.CreationServiceId, StringComparison.Ordinal)
        && expected.PredecessorEvaluationId == actual.PredecessorEvaluationId;
}
