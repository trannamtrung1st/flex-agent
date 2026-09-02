namespace FlexAgent.Submissions.Domain;

public sealed record AttemptDecision<T>(
    bool Succeeded,
    string OutcomeCode,
    T? Value,
    string? Field = null)
{
    public static AttemptDecision<T> Ok(T value, string outcomeCode) =>
        new(true, outcomeCode, value);

    public static AttemptDecision<T> Fail(string outcomeCode, string? field = null) =>
        new(false, outcomeCode, default, field);
}

public sealed record AttemptBinding(
    Guid SessionId,
    Guid ResolvedConfigurationId,
    Guid InitialManifestId,
    string ConfigurationDigest,
    string ManifestDigest);

public sealed record AttemptSubmissionBinding(
    Guid VersionId,
    int VersionNumber,
    int BindingOrder,
    string ContentDigest);

public sealed record Attempt(
    Guid AttemptId,
    Guid OrganizationId,
    Guid ActivityId,
    Guid CohortId,
    Guid BaselineId,
    Guid EnrollmentId,
    Guid ParticipantActorId,
    Guid TaskSourceId,
    int Ordinal,
    string EntitlementSource,
    Guid? RetryEntitlementId,
    string Status,
    bool Consumed,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? TerminalAtUtc,
    string? TerminalReasonCategory,
    AttemptBinding Binding,
    IReadOnlyList<AttemptSubmissionBinding> SubmissionBindings)
{
    public static AttemptDecision<Attempt> Activate(
        Guid attemptId,
        SubmissionParentScope scope,
        int ordinal,
        string entitlementSource,
        Guid? retryEntitlementId,
        DateTimeOffset requestedAtUtc,
        DateTimeOffset startedAtUtc,
        AttemptBinding binding,
        IReadOnlyList<AttemptSubmissionBinding> submissionBindings)
    {
        if (attemptId == Guid.Empty
            || scope.OrganizationId == Guid.Empty
            || scope.ActivityId == Guid.Empty
            || scope.CohortId == Guid.Empty
            || scope.BaselineId == Guid.Empty
            || scope.EnrollmentId == Guid.Empty
            || scope.ParticipantActorId == Guid.Empty
            || scope.TaskSourceId == Guid.Empty
            || ordinal < 1
            || binding.SessionId == Guid.Empty
            || binding.ResolvedConfigurationId == Guid.Empty
            || binding.InitialManifestId == Guid.Empty
            || string.IsNullOrWhiteSpace(binding.ConfigurationDigest)
            || binding.ConfigurationDigest.Length != 64
            || string.IsNullOrWhiteSpace(binding.ManifestDigest)
            || binding.ManifestDigest.Length != 64
            || !IsUtc(requestedAtUtc)
            || !IsUtc(startedAtUtc)
            || startedAtUtc < requestedAtUtc)
        {
            return AttemptDecision<Attempt>.Fail(AttemptFailureCodes.InvalidField);
        }

        if (entitlementSource is not (AttemptEntitlementSources.Baseline or AttemptEntitlementSources.Retry))
        {
            return AttemptDecision<Attempt>.Fail(AttemptFailureCodes.InvalidField, "entitlement_source");
        }

        if (entitlementSource == AttemptEntitlementSources.Retry && retryEntitlementId is null)
        {
            return AttemptDecision<Attempt>.Fail(AttemptFailureCodes.InvalidField, "retry_entitlement_id");
        }

        if (entitlementSource == AttemptEntitlementSources.Baseline && retryEntitlementId is not null)
        {
            return AttemptDecision<Attempt>.Fail(AttemptFailureCodes.InvalidField, "retry_entitlement_id");
        }

        if (submissionBindings.Count == 0
            || submissionBindings.Select(bindingItem => bindingItem.BindingOrder).Distinct().Count() != submissionBindings.Count
            || submissionBindings.Any(bindingItem =>
                bindingItem.VersionId == Guid.Empty
                || bindingItem.VersionNumber < 1
                || bindingItem.BindingOrder < 1
                || string.IsNullOrWhiteSpace(bindingItem.ContentDigest)
                || bindingItem.ContentDigest.Length != 64))
        {
            return AttemptDecision<Attempt>.Fail(AttemptFailureCodes.InvalidField, "submission_bindings");
        }

        return AttemptDecision<Attempt>.Ok(
            new Attempt(
                attemptId,
                scope.OrganizationId,
                scope.ActivityId,
                scope.CohortId,
                scope.BaselineId,
                scope.EnrollmentId,
                scope.ParticipantActorId,
                scope.TaskSourceId,
                ordinal,
                entitlementSource,
                retryEntitlementId,
                AttemptStates.Active,
                Consumed: true,
                requestedAtUtc,
                startedAtUtc,
                null,
                null,
                binding,
                [.. submissionBindings.OrderBy(item => item.BindingOrder)]),
            AttemptOutcomes.Activated);
    }

    public AttemptDecision<Attempt> Complete(DateTimeOffset terminalAtUtc, string reasonCategory) =>
        Terminate(AttemptStates.Completed, AttemptOutcomes.Completed, terminalAtUtc, reasonCategory);

    public AttemptDecision<Attempt> Abort(DateTimeOffset terminalAtUtc, string reasonCategory) =>
        Terminate(AttemptStates.Aborted, AttemptOutcomes.Aborted, terminalAtUtc, reasonCategory);

    private AttemptDecision<Attempt> Terminate(
        string targetStatus,
        string outcome,
        DateTimeOffset terminalAtUtc,
        string reasonCategory)
    {
        if (Status is AttemptStates.Completed or AttemptStates.Aborted)
        {
            return AttemptDecision<Attempt>.Fail(AttemptFailureCodes.Terminal);
        }

        if (Status != AttemptStates.Active
            || !Consumed
            || !IsUtc(terminalAtUtc)
            || terminalAtUtc < StartedAtUtc
            || string.IsNullOrWhiteSpace(reasonCategory))
        {
            return AttemptDecision<Attempt>.Fail(AttemptFailureCodes.InvalidField);
        }

        return AttemptDecision<Attempt>.Ok(
            this with
            {
                Status = targetStatus,
                TerminalAtUtc = terminalAtUtc,
                TerminalReasonCategory = reasonCategory,
            },
            outcome);
    }

    private static bool IsUtc(DateTimeOffset value) =>
        value.Offset == TimeSpan.Zero;
}

public sealed record RetryEntitlementFact(
    Guid EntitlementId,
    Guid OriginalAttemptId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    Guid? ConsumedByAttemptId);

public static class AttemptEntitlementCalculator
{
    public static int NextOrdinal(IReadOnlyList<Attempt> history) =>
        history.Count == 0 ? 1 : history.Max(attempt => attempt.Ordinal) + 1;

    public static int ConsumedCount(IReadOnlyList<Attempt> history) =>
        history.Count(attempt => attempt.Consumed);

    public static int UnusedRetryCount(
        IReadOnlyList<RetryEntitlementFact> entitlements,
        DateTimeOffset nowUtc) =>
        entitlements.Count(item =>
            item.ConsumedByAttemptId is null
            && (item.ExpiresAtUtc is null || nowUtc < item.ExpiresAtUtc));

    public static int Remaining(
        int baselineLimit,
        IReadOnlyList<Attempt> history,
        IReadOnlyList<RetryEntitlementFact> retryEntitlements,
        DateTimeOffset nowUtc)
    {
        var remainingBaseline = Math.Max(0, baselineLimit - ConsumedCount(history));
        return remainingBaseline + UnusedRetryCount(retryEntitlements, nowUtc);
    }

    public static string NextEntitlementSource(
        int baselineLimit,
        IReadOnlyList<Attempt> history) =>
        ConsumedCount(history) < baselineLimit
            ? AttemptEntitlementSources.Baseline
            : AttemptEntitlementSources.Retry;
}
