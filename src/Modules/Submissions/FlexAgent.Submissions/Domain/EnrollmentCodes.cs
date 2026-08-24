namespace FlexAgent.Submissions.Domain;

public static class EnrollmentStates
{
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Closed = "closed";
    public const string Revoked = "revoked";
}

public static class EnrollmentVisibilityStates
{
    public const string Current = "current";
    public const string Restricted = "restricted";
    public const string Unavailable = "unavailable";
}

public static class EnrollmentReasonCodes
{
    public const string TemporaryRestriction = "temporary_restriction";
    public const string RestrictionRemoved = "restriction_removed";
    public const string ActivityOrEnrollmentEnd = "activity_or_enrollment_end";
    public const string AccessRevoked = "access_revoked";
}

public static class EnrollmentAuthorizationActions
{
    public const string CandidateRead = "assessment.enrollment.candidate.read";
    public const string Receive = "assessment.enrollment.receive";
    public const string List = "assessment.enrollment.list";
    public const string Read = "assessment.enrollment.read";
    public const string Assign = "assessment.enrollment.assign";
    public const string Suspend = "assessment.enrollment.suspend";
    public const string Restore = "assessment.enrollment.restore";
    public const string Close = "assessment.enrollment.close";
    public const string Revoke = "assessment.enrollment.revoke";
    public const string Discover = "assessment.assignment.discover";
    public const string ReadAccommodation = "assessment.enrollment.accommodation.read";
    public const string GrantAccommodation = "assessment.enrollment.accommodation.grant";
    public const string DecideAccommodation = "assessment.enrollment.accommodation.decide";
    public const string RevokeAccommodation = "assessment.enrollment.accommodation.revoke";
}

public static class EnrollmentResourceTypes
{
    public const string Enrollment = "enrollment";
    public const string Cohort = "assessment_cohort";
    public const string Assignment = "assignment";
}

public static class EnrollmentFailureCodes
{
    public const string Denied = "enrollment.denied";
    public const string InvalidField = "enrollment.invalid_field";
    public const string StaleRevision = "enrollment.stale_revision";
    public const string Terminal = "enrollment.terminal";
    public const string Conflict = "enrollment.conflict";
    public const string IdempotencyConflict = "enrollment.idempotency_conflict";
    public const string AuditUnavailable = "enrollment.audit_unavailable";
    public const string Unavailable = "enrollment.unavailable";
    public const string MissingLifecyclePolicy = "enrollment.missing_lifecycle_policy";
    public const string InvalidReason = "enrollment.invalid_reason";
    public const string Ineligible = "enrollment.ineligible";
    public const string RateLimited = "enrollment.rate_limited";
}

public static class EnrollmentOutcomes
{
    public const string Assigned = "enrollment.assigned";
    public const string Deduplicated = "enrollment.assignment.deduplicated";
    public const string Suspended = "enrollment.suspended";
    public const string Restored = "enrollment.restored";
    public const string Closed = "enrollment.closed";
    public const string Revoked = "enrollment.revoked";
}

public static class EnrollmentOperationKinds
{
    public const string Assign = "assign";
    public const string Suspend = "suspend";
    public const string Restore = "restore";
    public const string Close = "close";
    public const string Revoke = "revoke";
}

public static class EnrollmentAuditClasses
{
    public const string RequiredDurable = "required_durable";
    public const string Bufferable = "bufferable";
    public const string OperationalSample = "operational_sample";
}

public static class EnrollmentClientActions
{
    public const string AssignParticipants = "assign_participants";
    public const string Suspend = "suspend_enrollment";
    public const string Restore = "restore_enrollment";
    public const string Close = "close_enrollment";
    public const string Revoke = "revoke_enrollment";
    public const string OpenAssignment = "open_assignment";
    public const string ReturnToMyWork = "return_to_my_work";
    public const string RequestAccommodation = "request_accommodation";
    public const string RevokeAccommodation = "revoke_accommodation";
    public const string ApproveException = "approve_fairness_exception";
    public const string RejectException = "reject_fairness_exception";
}

public static class EnrollmentLifecyclePolicy
{
    public static readonly Guid RestrictedPreservationPolicyId =
        Guid.Parse("11111111-1111-4111-8111-111111111118");

    public const int RestrictedPreservationVersion = 1;
}

public static class EnrollmentPageBounds
{
    public const int DefaultLimit = 20;
    public const int MaximumLimit = 50;
    public const int MaximumCursorLength = 512;
    public const int MaximumQueryPrefixLength = 64;
}

public static class EnrollmentRequestSurfaces
{
    public const string Read = "read";
    public const string Mutation = "mutation";
}

public static class EnrollmentRequestLimitDefaults
{
    public const int ReadPermitLimit = 60;
    public const int MutationPermitLimit = 20;
    public const int WindowSeconds = 10;
    public const int MaximumTrackedPartitions = 10_000;
    public const int PolicyRevision = 1;
    public const int AdmissionTimeoutMilliseconds = 250;
    public const int CleanupBatchSize = 64;
}

public static class EnrollmentLatencyObjectives
{
    public static TimeSpan MutationP95 { get; } = TimeSpan.FromSeconds(2);

    public static TimeSpan AuthorizationP95 { get; } = TimeSpan.FromMilliseconds(50);

    public static TimeSpan Percentile(IReadOnlyList<TimeSpan> samples, double percentile)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count == 0)
        {
            throw new ArgumentException("At least one sample is required.", nameof(samples));
        }

        if (percentile is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile));
        }

        var ordered = samples.OrderBy(sample => sample).ToArray();
        var index = (int)Math.Ceiling(percentile / 100d * ordered.Length) - 1;
        return ordered[Math.Clamp(index, 0, ordered.Length - 1)];
    }
}
