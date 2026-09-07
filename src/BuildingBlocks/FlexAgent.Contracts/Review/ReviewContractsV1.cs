using FlexAgent.Contracts.Evidence;

namespace FlexAgent.Contracts.Review;

public sealed record ReviewWorkItemV1(
    string SchemaVersion,
    Guid ReviewCaseId,
    string EvaluationProcessingState,
    string AssignmentState,
    string IntegrityState,
    string InternalEvaluationNotice,
    string UpdatedAt,
    string TimeZoneId,
    string NextAction,
    string? EvaluationId,
    string? CampaignLabel,
    string? TaskLabel,
    string? ParticipantLabel);

public sealed record ReviewCriterionSummaryV1(
    string CriterionId,
    string DisplayLabel,
    string EvaluatorMode,
    string Status,
    string? EvaluatorModeLabel);

public sealed record ReviewCaseReadV1(
    string SchemaVersion,
    Guid ReviewCaseId,
    string EvaluationProcessingState,
    string AssignmentState,
    string IntegrityState,
    string InternalEvaluationNotice,
    IReadOnlyList<ReviewCriterionSummaryV1> CriterionSummaries,
    string UpdatedAt,
    string TimeZoneId,
    string? EvaluationId,
    string? CandidateState,
    string? ProcessingNotice,
    string? CampaignLabel,
    string? TaskLabel,
    string? ParticipantLabel,
    string? ProcedureLabel,
    string? CompletedAt);

public sealed record ReviewEvidenceReferenceV1(
    string EvidenceId,
    string SourceType,
    string Precision,
    string VerificationState);

public sealed record ReviewCriterionReadV1(
    string SchemaVersion,
    Guid ReviewCaseId,
    string EvaluationId,
    string CriterionId,
    string CriterionVersion,
    string DisplayLabel,
    string EvaluatorMode,
    string EvaluatorModeLabel,
    string Status,
    string? Confidence,
    IReadOnlyList<string> Uncertainty,
    string? Rationale,
    IReadOnlyList<ReviewEvidenceReferenceV1> EvidenceReferences,
    string InternalEvaluationNotice,
    object? Score,
    string? ProvisionalFeedback);

public sealed record ReviewEvidenceOpenV1(
    string SchemaVersion,
    Guid ReviewCaseId,
    string EvaluationId,
    string EvidenceId,
    EvidenceLocatorV1 Locator,
    string Availability,
    string? DisplayText,
    string InternalEvaluationNotice,
    string? UnavailabilityNotice);
