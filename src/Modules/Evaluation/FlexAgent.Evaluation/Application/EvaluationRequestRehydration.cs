using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record AuthoritativeEvaluationRequestSnapshot(
    Guid RequestId,
    string RequestKind,
    string State,
    Guid? PredecessorEvaluationId,
    string? ReplacementReason,
    string IdempotencyKey,
    Guid DelegationId,
    Guid OrganizationId,
    Guid ActivityId,
    Guid ParticipantId,
    Guid AttemptId,
    Guid SessionId,
    string HandoffId,
    Guid TerminalRecordId,
    string HandoffTerminalState,
    long CutoffSequence,
    string ManifestSealProcedureId,
    string TerminalSealDigest,
    Guid ConfigurationRecordId,
    string ConfigurationDigest,
    Guid ManifestRecordId,
    string ManifestDigest,
    Guid RubricSourceId,
    Guid RubricSourceVersionId,
    string RubricContentDigest,
    Guid SubmissionSourceId,
    Guid SubmissionVersionId,
    string SubmissionContentDigest,
    string ModelProfileId,
    string ModelProfileVersion,
    string ModelProfileDigest,
    string ProviderId,
    string CredentialMode,
    string CredentialBindingReference,
    string CredentialBindingVersion,
    string EvaluatorRegistryVersion,
    string LifecyclePolicyRef);

public static class EvaluationRequestRehydration
{
    public static EvaluationDecision<EvaluationRequest> TryRebuild(
        AuthoritativeEvaluationRequestSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var ownership = EvaluationOwnership.TryCreate(
            snapshot.OrganizationId,
            snapshot.ActivityId,
            snapshot.ParticipantId,
            snapshot.AttemptId,
            snapshot.SessionId);
        if (!ownership.Succeeded || ownership.Value is null)
        {
            return EvaluationDecision<EvaluationRequest>.Fail(EvaluationFailureCodes.IncompleteOwnership);
        }

        var rubric = ExactSourceIdentity.TryCreate(
            "rubric_evaluation",
            snapshot.RubricSourceId,
            snapshot.RubricSourceVersionId,
            snapshot.RubricContentDigest);
        if (!rubric.Succeeded || rubric.Value is null)
        {
            return EvaluationDecision<EvaluationRequest>.Fail(EvaluationFailureCodes.InvalidField, "rubric");
        }

        var submission = ExactSourceIdentity.TryCreate(
            "task_submission",
            snapshot.SubmissionSourceId,
            snapshot.SubmissionVersionId,
            snapshot.SubmissionContentDigest);
        if (!submission.Succeeded || submission.Value is null)
        {
            return EvaluationDecision<EvaluationRequest>.Fail(EvaluationFailureCodes.InvalidField, "submission");
        }

        var model = FrozenModelIdentity.TryCreate(
            snapshot.ModelProfileId,
            snapshot.ModelProfileVersion,
            snapshot.ModelProfileDigest,
            snapshot.ProviderId,
            snapshot.CredentialMode,
            snapshot.CredentialBindingReference,
            snapshot.CredentialBindingVersion);
        if (!model.Succeeded || model.Value is null)
        {
            return EvaluationDecision<EvaluationRequest>.Fail(EvaluationFailureCodes.UnqualifiedModel);
        }

        var frozenInput = FrozenInputIdentity.TryCreate(
            snapshot.HandoffId,
            ownership.Value,
            snapshot.TerminalRecordId,
            snapshot.HandoffTerminalState,
            snapshot.CutoffSequence,
            snapshot.ManifestSealProcedureId,
            snapshot.TerminalSealDigest,
            snapshot.ConfigurationRecordId,
            snapshot.ConfigurationDigest,
            snapshot.ManifestRecordId,
            snapshot.ManifestDigest,
            rubric.Value,
            submission.Value,
            snapshot.EvaluatorRegistryVersion,
            model.Value,
            snapshot.LifecyclePolicyRef);
        if (!frozenInput.Succeeded || frozenInput.Value is null)
        {
            return EvaluationDecision<EvaluationRequest>.Fail(
                frozenInput.OutcomeCode,
                frozenInput.Field);
        }

        return EvaluationRequest.TryCreate(
            snapshot.RequestId,
            snapshot.RequestKind,
            frozenInput.Value,
            snapshot.IdempotencyKey,
            EvaluationDelegationReference.Format(snapshot.DelegationId),
            snapshot.State,
            snapshot.PredecessorEvaluationId,
            snapshot.ReplacementReason);
    }
}
