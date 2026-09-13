using FlexAgent.Contracts.Evidence;
using FlexAgent.Contracts.Review;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application.Review;

internal static class AssignedReviewProjectionMapper
{
    internal const string InternalEvaluationNotice = "Internal Evaluation · Not a released Result";
    internal const string DefaultTimeZoneId = "UTC";

    internal static ReviewWorkItemV1 MapWorkItem(AssignedReviewCaseSnapshot snapshot) =>
        new(
            "v1",
            snapshot.ReviewCaseId,
            snapshot.EvaluationProcessingState,
            snapshot.AssignmentState,
            snapshot.IntegrityState,
            InternalEvaluationNotice,
            FormatUtc(snapshot.UpdatedAtUtc),
            snapshot.TimeZoneId,
            snapshot.NextAction,
            snapshot.EvaluationId?.ToString(),
            snapshot.CampaignLabel,
            snapshot.TaskLabel,
            snapshot.ParticipantLabel);

    internal static ReviewCaseReadV1 MapCase(AssignedReviewCaseSnapshot snapshot, IReadOnlyList<ReviewCriterionSummaryV1> summaries) =>
        new(
            "v1",
            snapshot.ReviewCaseId,
            snapshot.EvaluationProcessingState,
            snapshot.AssignmentState,
            snapshot.IntegrityState,
            InternalEvaluationNotice,
            summaries,
            FormatUtc(snapshot.UpdatedAtUtc),
            snapshot.TimeZoneId,
            snapshot.EvaluationId?.ToString(),
            snapshot.CandidateState,
            snapshot.ProcessingNotice,
            snapshot.CampaignLabel,
            snapshot.TaskLabel,
            snapshot.ParticipantLabel,
            snapshot.ProcedureLabel,
            snapshot.CompletedAtUtc is null ? null : FormatUtc(snapshot.CompletedAtUtc.Value));

    internal static ReviewCriterionSummaryV1 MapCriterionSummary(AssignedReviewCriterionSnapshot criterion) =>
        new(
            criterion.CriterionId,
            criterion.DisplayLabel,
            criterion.EvaluatorMode,
            criterion.Status,
            MapEvaluatorModeLabel(criterion.EvaluatorMode));

    internal static ReviewCriterionReadV1 MapCriterion(
        Guid reviewCaseId,
        Guid evaluationId,
        AssignedReviewCriterionSnapshot criterion,
        IReadOnlyList<ReviewEvidenceReferenceV1> evidenceReferences) =>
        new(
            "v1",
            reviewCaseId,
            evaluationId.ToString(),
            criterion.CriterionId,
            criterion.CriterionVersion,
            criterion.DisplayLabel,
            criterion.EvaluatorMode,
            MapEvaluatorModeLabel(criterion.EvaluatorMode) ?? "Rule-based",
            criterion.Status,
            criterion.Confidence,
            criterion.Uncertainty,
            criterion.Rationale,
            evidenceReferences,
            InternalEvaluationNotice,
            criterion.Score,
            criterion.ProvisionalFeedback);

    internal static ReviewEvidenceReferenceV1 MapEvidenceReference(AssignedReviewEvidenceSnapshot evidence) =>
        new(
            EvaluationEvidenceSourceIdentity.StableEvidenceId(evidence.EvidenceId),
            evidence.SourceType,
            evidence.Precision,
            MapVerificationState(evidence.IntegrityState));

    internal static ReviewEvidenceOpenV1 MapEvidenceOpen(
        Guid reviewCaseId,
        Guid evaluationId,
        Guid evidenceId,
        EvidenceLocatorV1 locator,
        string availability,
        string? displayText,
        string? unavailabilityNotice) =>
        new(
            "v1",
            reviewCaseId,
            evaluationId.ToString(),
            EvaluationEvidenceSourceIdentity.StableEvidenceId(evidenceId),
            locator,
            availability,
            displayText,
            InternalEvaluationNotice,
            unavailabilityNotice);

    internal static string MapEvaluationProcessingState(
        string? requestState,
        string? aggregateStatus,
        bool hasEvaluation) =>
        hasEvaluation
            ? aggregateStatus switch
            {
                "conflict_review_required" => "review_required",
                _ => "completed",
            }
            : requestState switch
            {
                "queued" => "queued",
                "running" or "completing" => "running",
                "failed_retryable" => "retryable_failure",
                _ => "awaiting",
            };

    internal static string MapIntegrityState(string caseState, string candidateState) =>
        string.Equals(caseState, "candidate_stale", StringComparison.Ordinal)
        || string.Equals(candidateState, "stale", StringComparison.Ordinal)
        || string.Equals(candidateState, "replacement_available", StringComparison.Ordinal)
            ? "integrity_changed"
            : "intact";

    internal static string MapNextAction(string evaluationProcessingState) =>
        string.Equals(evaluationProcessingState, "completed", StringComparison.Ordinal)
        || string.Equals(evaluationProcessingState, "review_required", StringComparison.Ordinal)
            ? "open_review"
            : "none";

    internal static string? MapEvaluatorModeLabel(string evaluatorMode) =>
        evaluatorMode switch
        {
            "deterministic" => "Rule-based",
            "agent_assisted" => "Agent-assisted",
            "agent_judgment" => "Agent judgment",
            _ => null,
        };

    internal static string MapVerificationState(string integrityState) =>
        integrityState switch
        {
            "verified" => "verified",
            "lower_precision" => "degraded",
            _ => "failed",
        };

    internal static string MapEvidenceAvailability(string integrityState) =>
        integrityState switch
        {
            "verified" => "available",
            "lower_precision" => "lower_precision",
            "integrity_changed" => "integrity_changed",
            _ => "unavailable",
        };

    internal static string FormatUtc(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
}

internal sealed record AssignedReviewCaseSnapshot(
    Guid ReviewCaseId,
    string CaseState,
    string CandidateState,
    DateTimeOffset UpdatedAtUtc,
    string TimeZoneId,
    Guid? EvaluationId,
    DateTimeOffset? CompletedAtUtc,
    string EvaluationProcessingState,
    string AssignmentState,
    string IntegrityState,
    string NextAction,
    string? CampaignLabel,
    string? TaskLabel,
    string? ParticipantLabel,
    string? ProcedureLabel,
    string? ProcessingNotice);

internal sealed record AssignedReviewCriterionSnapshot(
    string CriterionId,
    string CriterionVersion,
    string DisplayLabel,
    string EvaluatorMode,
    string Status,
    string Confidence,
    IReadOnlyList<string> Uncertainty,
    string Rationale,
    object? Score,
    string? ProvisionalFeedback);

internal sealed record AssignedReviewEvidenceSnapshot(
    Guid EvidenceId,
    string SourceType,
    string SourceId,
    string SourceVersionId,
    string SourceContentDigest,
    string LocatorSchema,
    string LocatorDigest,
    string Precision,
    string IntegrityState,
    Guid ActivityId,
    Guid ParticipantId,
    Guid AttemptId,
    Guid SessionId);
