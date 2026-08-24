namespace FlexAgent.Submissions.Domain;

public static class IntakeStates
{
    public const string Receiving = "receiving";
    public const string Received = "received";
    public const string Validating = "validating";
    public const string Cancelling = "cancelling";
    public const string Cancelled = "cancelled";
    public const string Rejected = "rejected";
    public const string Failed = "failed";
    public const string Reconciling = "reconciling";
    public const string Accepted = "accepted";
}

public static class SubmissionFailureCodes
{
    public const string EnrollmentUnavailable = "enrollment_unavailable";
    public const string EnrollmentNotActive = "enrollment_not_active";
    public const string PolicyUnavailable = "policy_unavailable";
    public const string PolicyStale = "policy_stale";
    public const string CutoffPassed = "cutoff_passed";
    public const string Unauthorized = "unauthorized";
    public const string StaleRevision = "stale_revision";
    public const string InvalidCategory = "invalid_category";
    public const string InvalidEncoding = "invalid_encoding";
    public const string InvalidContentType = "invalid_content_type";
    public const string Oversized = "oversized";
    public const string TooManyItems = "too_many_items";
    public const string AggregateOversized = "aggregate_oversized";
    public const string UploadIncomplete = "upload_incomplete";
    public const string ValidationRejected = "validation_rejected";
    public const string ValidationTimeout = "validation_timeout";
    public const string ScannerRequiredUnavailable = "scanner_required_unavailable";
    public const string ScannerRejected = "scanner_rejected";
    public const string AlreadyAccepted = "already_accepted";
    public const string CancellationRace = "cancellation_race";
    public const string IdempotencyConflict = "idempotency_conflict";
    public const string AuditUnavailable = "audit_unavailable";
    public const string StorageUnavailable = "storage_unavailable";
    public const string Reconciling = "reconciling";
    public const string NotFound = "not_found";
    public const string CapabilityExpired = "capability_expired";
    public const string CapabilityMismatch = "capability_mismatch";
}

public static class IntakeOperationKinds
{
    public const string Begin = "intake_begin";
    public const string CompleteItem = "intake_complete_item";
    public const string Cancel = "intake_cancel";
    public const string Finalize = "intake_finalize";
}
