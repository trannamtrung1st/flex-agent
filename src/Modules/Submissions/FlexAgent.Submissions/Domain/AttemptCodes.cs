namespace FlexAgent.Submissions.Domain;

public static class AttemptStates
{
    public const string Active = "active";
    public const string Completed = "completed";
    public const string Aborted = "aborted";
}

public static class AttemptEntitlementSources
{
    public const string Baseline = "baseline";
    public const string Retry = "retry";
}

public static class StartOperationStates
{
    public const string Claimed = "claimed";
    public const string Committed = "committed";
    public const string Failed = "failed";
}

public static class AttemptReadinessStates
{
    public const string Eligible = "eligible";
    public const string TooEarly = "too_early";
    public const string Expired = "expired";
    public const string Exhausted = "exhausted";
    public const string MissingAcceptedMaterial = "missing_accepted_material";
    public const string MaterialNotAgentReadable = "material_not_agent_readable";
    public const string ActiveConflict = "active_conflict";
    public const string ConfigurationUnavailable = "configuration_unavailable";
    public const string EnrollmentUnavailable = "enrollment_unavailable";
    public const string DependencyUnavailable = "dependency_unavailable";
}

public static class AttemptFailureCodes
{
    public const string InvalidField = "attempt.invalid_field";
    public const string Denied = "attempt.denied";
    public const string Ineligible = "attempt.ineligible";
    public const string IdempotencyConflict = "attempt.idempotency_conflict";
    public const string ActiveConflict = "attempt.active_conflict";
    public const string StaleClaim = "attempt.stale_claim";
    public const string Terminal = "attempt.terminal";
    public const string AuditUnavailable = "attempt.audit_unavailable";
    public const string Unavailable = "attempt.unavailable";
    public const string AcknowledgmentInvalid = "attempt.acknowledgment_invalid";
}

public static class AttemptOutcomes
{
    public const string Activated = "attempt.activated";
    public const string Completed = "attempt.completed";
    public const string Aborted = "attempt.aborted";
    public const string Reconciled = "attempt.reconciled";
    public const string Claimed = "attempt.claimed";
    public const string ClaimRecovered = "attempt.claim_recovered";
    public const string StartFailed = "attempt.start_failed";
}

public static class AttemptOperationKinds
{
    public const string Start = "attempt_start";
    public const string Acknowledge = "acknowledgment_record";
    public const string Reconcile = "attempt_reconcile";
    public const string Readiness = "attempt_readiness";
}

public static class AttemptClientActions
{
    public const string StartAttempt = "start_attempt";
    public const string ContinueAttempt = "continue_attempt";
    public const string ReturnToMyWork = "return_to_my_work";
}

public static class AttemptAuthorizationActions
{
    public const string Read = "submissions.attempt.read";
    public const string Start = "submissions.attempt.start";
}

public static class StartOperationLease
{
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(2);
}

public static class AttemptPageBounds
{
    public const int MaximumHistory = 50;
}
