namespace FlexAgent.Submissions.Domain;

public static class TimingEligibilityStates
{
    public const string TooEarly = "too_early";
    public const string Open = "open";
    public const string SubmissionClosed = "submission_closed";
    public const string AttemptStartClosed = "attempt_start_closed";
    public const string Unavailable = "unavailable";
}

public static class AccommodationDimensions
{
    public const string SubmissionDeadlineUtc = "submission_deadline_utc";
    public const string AttemptStartNotBeforeUtc = "attempt_start_not_before_utc";
    public const string AttemptStartBeforeUtc = "attempt_start_before_utc";
    public const string PerAttemptDurationSeconds = "per_attempt_duration_seconds";

    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        SubmissionDeadlineUtc,
        AttemptStartNotBeforeUtc,
        AttemptStartBeforeUtc,
        PerAttemptDurationSeconds,
    };
}

public static class AccommodationValueKinds
{
    public const string UtcInstant = "utc_instant";
    public const string PositiveSeconds = "positive_seconds";
}

public static class AccommodationStates
{
    public const string PendingApproval = "pending_approval";
    public const string Granted = "granted";
    public const string Rejected = "rejected";
    public const string Revoked = "revoked";
    public const string Superseded = "superseded";
}

public static class AccommodationReasonCategories
{
    public const string DevelopmentSynthetic = "development.synthetic.timing";
}

public static class AccommodationOutcomes
{
    public const string Granted = "accommodation.granted";
    public const string ApprovalRequired = "accommodation.approval_required";
    public const string Rejected = "accommodation.rejected";
    public const string Revoked = "accommodation.revoked";
    public const string Superseded = "accommodation.superseded";
}

public static class AccommodationFailureCodes
{
    public const string UnsupportedDimension = "accommodation.unsupported_dimension";
    public const string InvalidReason = "accommodation.invalid_reason";
    public const string InvalidValue = "accommodation.invalid_value";
    public const string OutsideBounds = "accommodation.outside_bounds";
    public const string DistinctApproverRequired = "accommodation.distinct_approver_required";
    public const string StaleRevision = "accommodation.stale_revision";
    public const string Denied = "accommodation.denied";
    public const string PolicyUnavailable = "accommodation.policy_unavailable";
}

public static class AccommodationConsequenceCodes
{
    public const string None = "none";
    public const string DeadlineReplacement = "deadline_replacement";
    public const string AttemptStartReplacement = "attempt_start_replacement";
    public const string DurationReplacement = "duration_replacement";
    public const string MultipleReplacements = "multiple_replacements";
}

public static class AccommodationLifecyclePolicy
{
    public static readonly Guid HistoryRetentionPolicyId =
        Guid.Parse("55555555-5555-4555-8555-555555555501");

    public const int HistoryRetentionVersion = 1;
}

public static class AccommodationAuthorizationActions
{
    public const string Read = "assessment.enrollment.accommodation.read";
    public const string Grant = "assessment.enrollment.accommodation.grant";
    public const string Decide = "assessment.enrollment.accommodation.decide";
    public const string Revoke = "assessment.enrollment.accommodation.revoke";
}

public static class AccommodationClientActions
{
    public const string Request = "request_accommodation";
    public const string Revoke = "revoke_accommodation";
    public const string ApproveException = "approve_fairness_exception";
    public const string RejectException = "reject_fairness_exception";
}

public static class AccommodationOperationKinds
{
    public const string Grant = "accommodation_grant";
    public const string Decide = "accommodation_decide";
    public const string Revoke = "accommodation_revoke";
}
