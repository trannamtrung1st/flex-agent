using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationCompletionOutcomeCodes
{
    public const string Completed = "evaluation.completed";
    public const string Reconciled = "evaluation.completion_reconciled";
    public const string Denied = "evaluation.completion_denied";
    public const string IntegrityConflict = "evaluation.completion_integrity_conflict";
    public const string AuditRejected = "evaluation.completion_audit_rejected";
}

public sealed record EvaluationManifestRefDraft(
    Guid ManifestRefId,
    string RefKind,
    string ProtectedRef,
    string ContentDigest);

public sealed record EvaluationCompletionCommand(
    Guid WorkId,
    Guid RequestId,
    Guid InvocationAttemptId,
    Guid DelegationId,
    Guid ActorId,
    Guid CorrelationId,
    string SourceChannel,
    CompletedEvaluation Completed,
    IReadOnlyList<EvidenceItem> EvidenceItems,
    IReadOnlyList<EvidenceLocatorVerificationEntry> EvidenceLocators,
    IReadOnlyList<CriterionJudgment> Judgments,
    IReadOnlyList<EvaluationManifestRefDraft> ManifestRefs);

public sealed record EvaluationCompletionResult(
    bool Succeeded,
    string OutcomeCode,
    Guid? EvaluationId,
    Guid? ReviewHandoffId,
    bool Reconciled);

public interface IEvaluationCompletionCoordinator
{
    Task<EvaluationCompletionResult> TryCompleteAsync(
        EvaluationCompletionCommand command,
        CancellationToken cancellationToken);
}

public sealed record EvaluationAnnotationAppendCommand(
    Guid EvaluationId,
    Guid DelegationId,
    Guid ActorId,
    Guid CorrelationId,
    string SourceChannel,
    string Kind,
    string Disposition,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    Guid? EvidenceId = null);

public interface IEvaluationAnnotationService
{
    Task<EvaluationDecision<Guid>> TryAppendAsync(
        EvaluationAnnotationAppendCommand command,
        CancellationToken cancellationToken);
}

public sealed record EvaluationReplacementStaleCommand(
    Guid PredecessorEvaluationId,
    Guid SuccessorEvaluationId,
    Guid RequestId,
    Guid DelegationId,
    string Reason,
    Guid ActorId,
    Guid CorrelationId,
    string SourceChannel,
    DateTimeOffset OccurredAtUtc);

public interface IEvaluationReplacementReviewSignal
{
    Task<EvaluationDecision<bool>> TryPublishReplacementAvailableAsync(
        EvaluationReplacementStaleCommand command,
        CancellationToken cancellationToken);
}
