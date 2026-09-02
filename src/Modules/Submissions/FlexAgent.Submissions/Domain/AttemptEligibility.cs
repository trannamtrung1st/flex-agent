namespace FlexAgent.Submissions.Domain;

public sealed record AttemptReadinessFacts(
    string EnrollmentStatus,
    string TimingEligibilityState,
    int BaselineAttemptLimit,
    IReadOnlyList<Attempt> History,
    IReadOnlyList<RetryEntitlementFact> RetryEntitlements,
    bool RequiredAcceptedMaterialPresent,
    bool AgentInspectionRequired,
    bool RequiredMaterialAgentReadable,
    bool ConfigurationReady,
    bool RequiredNoticeProjectionReady,
    DateTimeOffset NowUtc);

public sealed record AttemptReadiness(
    string State,
    int NextOrdinal,
    int RemainingEntitlement,
    string EntitlementSource,
    Guid? ActiveAttemptId,
    Guid? ActiveSessionId,
    IReadOnlyList<string> PermittedActions);

public static class AttemptEligibility
{
    public static AttemptReadiness Evaluate(AttemptReadinessFacts facts)
    {
        var active = facts.History.FirstOrDefault(attempt => attempt.Status == AttemptStates.Active);
        var remaining = AttemptEntitlementCalculator.Remaining(
            facts.BaselineAttemptLimit,
            facts.History,
            facts.RetryEntitlements,
            facts.NowUtc);
        var nextOrdinal = AttemptEntitlementCalculator.NextOrdinal(facts.History);
        var source = AttemptEntitlementCalculator.NextEntitlementSource(
            facts.BaselineAttemptLimit,
            facts.History);

        if (facts.EnrollmentStatus != EnrollmentStates.Active)
        {
            return Blocked(
                AttemptReadinessStates.EnrollmentUnavailable,
                nextOrdinal,
                remaining,
                source,
                active);
        }

        if (active is not null)
        {
            return new AttemptReadiness(
                AttemptReadinessStates.ActiveConflict,
                nextOrdinal,
                remaining,
                source,
                active.AttemptId,
                active.Binding.SessionId,
                [AttemptClientActions.ContinueAttempt, AttemptClientActions.ReturnToMyWork]);
        }

        if (facts.TimingEligibilityState == TimingEligibilityStates.TooEarly)
        {
            return Blocked(AttemptReadinessStates.TooEarly, nextOrdinal, remaining, source, active);
        }

        if (facts.TimingEligibilityState is TimingEligibilityStates.AttemptStartClosed
            or TimingEligibilityStates.SubmissionClosed)
        {
            return Blocked(AttemptReadinessStates.Expired, nextOrdinal, remaining, source, active);
        }

        if (facts.TimingEligibilityState != TimingEligibilityStates.Open)
        {
            return Blocked(
                AttemptReadinessStates.EnrollmentUnavailable,
                nextOrdinal,
                remaining,
                source,
                active);
        }

        if (remaining <= 0)
        {
            return Blocked(AttemptReadinessStates.Exhausted, nextOrdinal, remaining, source, active);
        }

        if (!facts.RequiredAcceptedMaterialPresent)
        {
            return Blocked(
                AttemptReadinessStates.MissingAcceptedMaterial,
                nextOrdinal,
                remaining,
                source,
                active);
        }

        if (facts.AgentInspectionRequired && !facts.RequiredMaterialAgentReadable)
        {
            return Blocked(
                AttemptReadinessStates.MaterialNotAgentReadable,
                nextOrdinal,
                remaining,
                source,
                active);
        }

        if (!facts.RequiredNoticeProjectionReady)
        {
            return Blocked(
                AttemptReadinessStates.DependencyUnavailable,
                nextOrdinal,
                remaining,
                source,
                active);
        }

        if (!facts.ConfigurationReady)
        {
            return Blocked(
                AttemptReadinessStates.ConfigurationUnavailable,
                nextOrdinal,
                remaining,
                source,
                active);
        }

        return new AttemptReadiness(
            AttemptReadinessStates.Eligible,
            nextOrdinal,
            remaining,
            source,
            null,
            null,
            [AttemptClientActions.StartAttempt, AttemptClientActions.ReturnToMyWork]);
    }

    private static AttemptReadiness Blocked(
        string state,
        int nextOrdinal,
        int remaining,
        string source,
        Attempt? active) =>
        new(
            state,
            nextOrdinal,
            remaining,
            source,
            active?.AttemptId,
            active?.Binding.SessionId,
            [AttemptClientActions.ReturnToMyWork]);
}
