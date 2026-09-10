using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class DeterministicEvaluatorAuthorityVerifier
{
    public static EvaluationDecision<VerifiedFrozenProcedureExecutionContext> TryVerify(
        AdmittedEvaluationRequestAuthority authority,
        ProtectedCanonicalUtf8 procedurePayload,
        DeterministicEvaluatorExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(procedurePayload);
        ArgumentNullException.ThrowIfNull(request);

        if (authority.RequestId != request.RequestId)
        {
            return EvaluationDecision<VerifiedFrozenProcedureExecutionContext>.Fail(
                EvaluationFailureCodes.InvalidField,
                "request_id");
        }

        if (authority.InvocationAttemptId != request.InvocationAttemptId)
        {
            return EvaluationDecision<VerifiedFrozenProcedureExecutionContext>.Fail(
                EvaluationFailureCodes.InvalidField,
                "invocation_attempt_id");
        }

        if (!OwnershipMatches(authority.Ownership, request.Ownership))
        {
            return EvaluationDecision<VerifiedFrozenProcedureExecutionContext>.Fail(
                EvaluationFailureCodes.InvalidField,
                "ownership");
        }

        if (!string.Equals(
                procedurePayload.ContentDigest,
                authority.ProcedureRef.ContentDigest,
                StringComparison.Ordinal))
        {
            return EvaluationDecision<VerifiedFrozenProcedureExecutionContext>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "procedure");
        }

        if (procedurePayload.SourceId != authority.ProcedureRef.SourceId
            || procedurePayload.SourceVersionId != authority.ProcedureRef.SourceVersionId)
        {
            return EvaluationDecision<VerifiedFrozenProcedureExecutionContext>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "procedure_ref");
        }

        var resolved = EvaluationProcedureResolver.TryResolve(procedurePayload.Utf8);
        if (!resolved.Succeeded || resolved.Value is null)
        {
            return EvaluationDecision<VerifiedFrozenProcedureExecutionContext>.Fail(
                resolved.OutcomeCode,
                resolved.Field);
        }

        var orchestration = DeterministicEvaluatorOrchestrationValidator.TryValidateRequest(
            resolved.Value,
            request);
        if (!orchestration.Succeeded)
        {
            return EvaluationDecision<VerifiedFrozenProcedureExecutionContext>.Fail(
                orchestration.OutcomeCode,
                orchestration.Field);
        }

        return EvaluationDecision<VerifiedFrozenProcedureExecutionContext>.Ok(
            new VerifiedFrozenProcedureExecutionContext(authority, resolved.Value));
    }

    private static bool OwnershipMatches(EvaluationOwnership authority, EvaluationOwnership request) =>
        authority.OrganizationId == request.OrganizationId
        && authority.ActivityId == request.ActivityId
        && authority.ParticipantId == request.ParticipantId
        && authority.AttemptId == request.AttemptId
        && authority.SessionId == request.SessionId;
}
