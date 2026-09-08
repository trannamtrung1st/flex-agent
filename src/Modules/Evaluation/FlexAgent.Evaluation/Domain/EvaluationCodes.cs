using System.Text.RegularExpressions;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationRequestKinds
{
    public const string Initial = "initial";
    public const string Replacement = "replacement";
}

public static class EvaluationRequestStates
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Validating = "validating";
    public const string Completing = "completing";
    public const string Completed = "completed";
    public const string FailedRetryable = "failed_retryable";
    public const string FailedReviewRequired = "failed_review_required";
    public const string Cancelled = "cancelled";
}

public static class EvaluatorModes
{
    public const string Deterministic = "deterministic";
    public const string AgentAssisted = "agent_assisted";
    public const string AgentJudgment = "agent_judgment";
}

public static class CriterionStatuses
{
    public const string Satisfied = "satisfied";
    public const string NotSatisfied = "not_satisfied";
    public const string InsufficientEvidence = "insufficient_evidence";
    public const string NotApplicable = "not_applicable";
    public const string Conflict = "conflict";
}

public static class EvaluationAggregateStatuses
{
    public const string Complete = "complete";
    public const string InsufficientEvidence = "insufficient_evidence";
    public const string RequirementsNotSatisfied = "requirements_not_satisfied";
    public const string ConflictReviewRequired = "conflict_review_required";
    public const string NotApplicableExcluded = "not_applicable_excluded";
}

public static class EvaluationAnnotationKinds
{
    public const string SourceIntegrityChanged = "source_integrity_changed";
    public const string SourceLawfullyUnavailable = "source_lawfully_unavailable";
    public const string LowerPrecisionRecorded = "lower_precision_recorded";
    public const string LifecycleHold = "lifecycle_hold";
    public const string LifecycleExpiry = "lifecycle_expiry";
}

public static class EvaluationDispositions
{
    public const string AttentionRequired = "attention_required";
    public const string LawfullyUnavailable = "lawfully_unavailable";
    public const string LowerPrecision = "lower_precision";
    public const string Held = "held";
    public const string ExpiredMinimumProvenance = "expired_minimum_provenance";
}

public static class EvaluationActorTypes
{
    public const string Human = "human";
    public const string Service = "service";
    public const string System = "system";
}

public static class EvaluationFailureCodes
{
    public const string InvalidField = "evaluation.invalid_field";
    public const string IncompleteOwnership = "evaluation.incomplete_ownership";
    public const string MutableAlias = "evaluation.mutable_alias";
    public const string UnqualifiedModel = "evaluation.unqualified_model";
    public const string InvalidProcedure = "evaluation.invalid_procedure";
    public const string IncompleteCriteria = "evaluation.incomplete_criteria";
    public const string InvalidJudgment = "evaluation.invalid_judgment";
    public const string ProtectedContent = "evaluation.protected_content";
    public const string DuplicateIdentity = "evaluation.duplicate_identity";
    public const string CitationIntegrity = "evaluation.citation_integrity";
    public const string DeterministicConflict = "evaluation.deterministic_conflict";
    public const string ProcessingDisabled = "evaluation.processing_disabled";
}

public static class EvaluationIdentity
{
    private static readonly Regex StableId = new(
        "^[a-z][a-z0-9._-]{7,127}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AliasToken = new(
        "(^|[._-])(latest|current)([._-]|$)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex Sha256Hex = new(
        "^[0-9a-f]{64}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsStableId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && StableId.IsMatch(value);

    public static bool ContainsMutableAlias(string? value) =>
        !string.IsNullOrWhiteSpace(value) && AliasToken.IsMatch(value);

    public static bool IsSha256Hex(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Sha256Hex.IsMatch(value);

    public static bool IsUtc(DateTimeOffset value) => value.Offset == TimeSpan.Zero;
}

public sealed record EvaluationDecision<T>(
    bool Succeeded,
    string OutcomeCode,
    T? Value,
    string? Field = null)
{
    public static EvaluationDecision<T> Ok(T value, string outcomeCode = "evaluation.ok") =>
        new(true, outcomeCode, value);

    public static EvaluationDecision<T> Fail(string outcomeCode, string? field = null) =>
        new(false, outcomeCode, default, field);
}
