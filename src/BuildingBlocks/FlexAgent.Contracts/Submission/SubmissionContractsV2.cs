using System.Text.Json.Serialization;

namespace FlexAgent.Contracts.Submission;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BeginIntakeCommandV2(
    string SchemaVersion,
    string IdempotencyKey);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CompleteIntakeItemCommandV2(
    string SchemaVersion,
    string Category,
    string? Filename,
    string? DeclaredMimeType,
    string Content,
    long ExpectedRevision,
    string IdempotencyKey);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record IntakeRevisionCommandV2(
    string SchemaVersion,
    long ExpectedRevision,
    string IdempotencyKey);

public sealed record IntakeMutationOutcomeV2(
    string SchemaVersion,
    bool Succeeded,
    string OutcomeCode,
    Guid? IntakeId,
    Guid? SubmissionId,
    string? Status,
    long? Revision,
    Guid? VersionId,
    int? VersionNumber,
    IReadOnlyList<string> PermittedActions);

public sealed record MaterialCategoryLimitV2(
    string Category,
    bool Available,
    long MaxBytes);

public sealed record MaterialRequirementsV2(
    string ContractVersion,
    int MaxAttachmentCount,
    long MaxAttachmentAggregateBytes,
    long MaxDirectTextBytes,
    string ScannerMode,
    IReadOnlyList<MaterialCategoryLimitV2> Categories);

public sealed record SubmissionIntakeItemV2(
    Guid ItemId,
    string Category,
    string? Filename,
    long ByteCount,
    string? ReceiptState);

public sealed record SubmissionIntakeV2(
    Guid IntakeId,
    Guid SubmissionId,
    string Status,
    long Revision,
    string CreatedAtUtc,
    string UpdatedAtUtc,
    string? CompleteReceiptAtUtc,
    IReadOnlyList<SubmissionIntakeItemV2> Items,
    IReadOnlyList<string> PermittedActions);

public sealed record AcceptedVersionSummaryV2(
    Guid VersionId,
    int VersionNumber,
    string AcceptedAtUtc,
    int ItemCount);

public sealed record MyWorkSubmissionV2(
    string SchemaVersion,
    Guid EnrollmentId,
    string EnrollmentStatus,
    bool IntakeAvailable,
    string? UnavailableReason,
    MaterialRequirementsV2? Requirements,
    SubmissionIntakeV2? ActiveIntake,
    IReadOnlyList<AcceptedVersionSummaryV2> VersionHistory,
    IReadOnlyList<string> PermittedActions);

public sealed record AcceptedVersionItemV2(
    Guid ItemId,
    string Category,
    string? Filename,
    long ByteCount,
    bool PreviewAuthorized,
    bool DownloadAuthorized);

public sealed record AcceptedVersionDetailV2(
    string SchemaVersion,
    Guid VersionId,
    int VersionNumber,
    string AcceptedAtUtc,
    IReadOnlyList<AcceptedVersionItemV2> Items,
    IReadOnlyList<string> PermittedActions);

public sealed record ProtectedItemPreviewV2(
    string SchemaVersion,
    Guid VersionId,
    Guid ItemId,
    string Category,
    string? Filename,
    string ContentType,
    string Content);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AcknowledgeAttemptNoticeCommandV2(
    string SchemaVersion,
    Guid NoticeId,
    Guid SourceVersionId,
    string Outcome,
    string IdempotencyKey);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StartAttemptCommandV2(
    string SchemaVersion,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record AttemptNoticeV2(
    Guid NoticeId,
    string NoticeType,
    string RequiredOutcome,
    string ProtectedContentRef,
    Guid SourceVersionId,
    string ContentDigest,
    Guid SourceId);

public sealed record AttemptHistoryItemV2(
    Guid AttemptId,
    int Ordinal,
    string Status,
    bool Consumed,
    Guid? SessionId,
    string StartedAtUtc,
    string? TerminalAtUtc,
    string? TerminalReasonCategory);

public sealed record MyWorkAttemptReadinessV2(
    string SchemaVersion,
    Guid EnrollmentId,
    string ReadinessState,
    int NextOrdinal,
    int RemainingEntitlement,
    string EntitlementSource,
    int BaselineAttemptLimit,
    Guid? ActiveAttemptId,
    Guid? ActiveSessionId,
    string StartCommandDigest,
    IReadOnlyList<AcceptedVersionSummaryV2> BoundVersionCandidates,
    IReadOnlyList<AttemptHistoryItemV2> History,
    IReadOnlyList<AttemptNoticeV2> RequiredNotices,
    IReadOnlyList<string> PermittedActions);

public sealed record AcknowledgmentMutationOutcomeV2(
    string SchemaVersion,
    bool Succeeded,
    string OutcomeCode,
    Guid? RecordId,
    string? Outcome);

public sealed record StartAttemptOutcomeV2(
    string SchemaVersion,
    bool Succeeded,
    string OutcomeCode,
    string? ReadinessState,
    Guid? AttemptId,
    int? Ordinal,
    Guid? SessionId,
    int RemainingEntitlement,
    IReadOnlyList<string> PermittedActions);
