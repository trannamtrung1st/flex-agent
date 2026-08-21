namespace FlexAgent.AssessmentConfiguration.Domain;

public sealed record AssessmentCohort(
    Guid OrganizationId,
    Guid ActivityId,
    Guid CohortId,
    string State,
    Guid BoundRevisionId,
    long BoundRevisionNumber,
    Guid? BaselineId,
    string? BaselineDigest)
{
    public static AssessmentDecision<AssessmentCohort> CreateEmpty(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid boundRevisionId,
        long boundRevisionNumber)
    {
        if (organizationId == Guid.Empty
            || activityId == Guid.Empty
            || cohortId == Guid.Empty
            || boundRevisionId == Guid.Empty
            || boundRevisionNumber < 1)
        {
            return AssessmentDecision<AssessmentCohort>.Fail(AssessmentFailureCodes.InvalidField);
        }

        return AssessmentDecision<AssessmentCohort>.Ok(
            new AssessmentCohort(
                organizationId,
                activityId,
                cohortId,
                CohortStates.Draft,
                boundRevisionId,
                boundRevisionNumber,
                BaselineId: null,
                BaselineDigest: null));
    }

    public AssessmentDecision<AssessmentCohort> RetargetDraftRevision(Guid revisionId, long revisionNumber)
    {
        if (State != CohortStates.Draft)
        {
            return AssessmentDecision<AssessmentCohort>.Fail(AssessmentFailureCodes.NewCohortRequired);
        }

        if (revisionId == Guid.Empty || revisionNumber < 1)
        {
            return AssessmentDecision<AssessmentCohort>.Fail(AssessmentFailureCodes.InvalidField);
        }

        return AssessmentDecision<AssessmentCohort>.Ok(
            this with
            {
                BoundRevisionId = revisionId,
                BoundRevisionNumber = revisionNumber,
            });
    }

    public AssessmentDecision<AssessmentCohort> BindActivation(
        Guid expectedRevisionId,
        long expectedRevisionNumber,
        Guid baselineId,
        string baselineDigest)
    {
        if (State == CohortStates.Activated)
        {
            if (BaselineId == baselineId
                && string.Equals(BaselineDigest, baselineDigest, StringComparison.Ordinal)
                && BoundRevisionId == expectedRevisionId)
            {
                return AssessmentDecision<AssessmentCohort>.Ok(this);
            }

            return AssessmentDecision<AssessmentCohort>.Fail(AssessmentFailureCodes.Immutable);
        }

        if (expectedRevisionId != BoundRevisionId || expectedRevisionNumber != BoundRevisionNumber)
        {
            return AssessmentDecision<AssessmentCohort>.Fail(AssessmentFailureCodes.StaleRevision);
        }

        if (baselineId == Guid.Empty
            || string.IsNullOrWhiteSpace(baselineDigest)
            || baselineDigest.Length != 64
            || baselineDigest != baselineDigest.ToLowerInvariant())
        {
            return AssessmentDecision<AssessmentCohort>.Fail(AssessmentFailureCodes.InvalidField);
        }

        return AssessmentDecision<AssessmentCohort>.Ok(
            this with
            {
                State = CohortStates.Activated,
                BaselineId = baselineId,
                BaselineDigest = baselineDigest,
            });
    }

    public AssessmentDecision<AssessmentCohort> RejectMaterialMutation()
    {
        if (State == CohortStates.Activated)
        {
            return AssessmentDecision<AssessmentCohort>.Fail(AssessmentFailureCodes.NewCohortRequired);
        }

        return AssessmentDecision<AssessmentCohort>.Ok(this);
    }
}
