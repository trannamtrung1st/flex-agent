namespace FlexAgent.Submissions.Domain;

public sealed record Enrollment(
    Guid EnrollmentId,
    Guid OrganizationId,
    Guid ActivityId,
    Guid CohortId,
    Guid BaselineId,
    Guid TaskSourceId,
    Guid TaskVersionId,
    string TaskContentDigest,
    Guid LifecyclePolicyId,
    int LifecyclePolicyVersion,
    Guid ParticipantActorId,
    string Status,
    long Revision,
    Guid AssignedByActorId,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static EnrollmentDecision<Enrollment> Create(
        Guid enrollmentId,
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid baselineId,
        Guid taskSourceId,
        Guid taskVersionId,
        string taskContentDigest,
        Guid lifecyclePolicyId,
        int lifecyclePolicyVersion,
        Guid participantActorId,
        Guid assignedByActorId,
        DateTimeOffset assignedAtUtc)
    {
        if (enrollmentId == Guid.Empty
            || organizationId == Guid.Empty
            || activityId == Guid.Empty
            || cohortId == Guid.Empty
            || baselineId == Guid.Empty
            || taskSourceId == Guid.Empty
            || taskVersionId == Guid.Empty
            || participantActorId == Guid.Empty
            || assignedByActorId == Guid.Empty
            || lifecyclePolicyId == Guid.Empty
            || lifecyclePolicyVersion < 1
            || string.IsNullOrWhiteSpace(taskContentDigest)
            || taskContentDigest.Length != 64
            || taskContentDigest != taskContentDigest.ToLowerInvariant())
        {
            return EnrollmentDecision<Enrollment>.Fail(EnrollmentFailureCodes.InvalidField);
        }

        if (lifecyclePolicyId != EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId
            || lifecyclePolicyVersion != EnrollmentLifecyclePolicy.RestrictedPreservationVersion)
        {
            return EnrollmentDecision<Enrollment>.Fail(EnrollmentFailureCodes.MissingLifecyclePolicy);
        }

        return EnrollmentDecision<Enrollment>.Ok(
            new Enrollment(
                enrollmentId,
                organizationId,
                activityId,
                cohortId,
                baselineId,
                taskSourceId,
                taskVersionId,
                taskContentDigest,
                lifecyclePolicyId,
                lifecyclePolicyVersion,
                participantActorId,
                EnrollmentStates.Active,
                1,
                assignedByActorId,
                assignedAtUtc,
                assignedAtUtc),
            EnrollmentOutcomes.Assigned);
    }

    public EnrollmentDecision<Enrollment> Transition(
        string targetStatus,
        string reasonCode,
        long expectedRevision,
        DateTimeOffset updatedAtUtc)
    {
        if (expectedRevision != Revision)
        {
            return EnrollmentDecision<Enrollment>.Fail(EnrollmentFailureCodes.StaleRevision);
        }

        if (Status is EnrollmentStates.Closed or EnrollmentStates.Revoked)
        {
            return EnrollmentDecision<Enrollment>.Fail(EnrollmentFailureCodes.Terminal);
        }

        if (!EnrollmentLifecycle.TryValidate(Status, targetStatus, reasonCode, out var outcomeCode))
        {
            return EnrollmentDecision<Enrollment>.Fail(
                outcomeCode ?? EnrollmentFailureCodes.InvalidReason);
        }

        return EnrollmentDecision<Enrollment>.Ok(
            this with
            {
                Status = targetStatus,
                Revision = Revision + 1,
                UpdatedAtUtc = updatedAtUtc,
            },
            outcomeCode!);
    }

    public string VisibilityForParticipant() => EnrollmentProjection.Visibility(Status);

    public bool PermitsNewIntakeOrStart() => EnrollmentProjection.PermitsNewIntakeOrStart(Status);
}

public sealed record EnrollmentEvent(
    Guid EventId,
    Guid EnrollmentId,
    Guid OrganizationId,
    long Sequence,
    string PriorStatus,
    string NewStatus,
    string ReasonCode,
    Guid ActorId,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId,
    Guid? AuthorizationReferenceId,
    long EnrollmentRevision);

public sealed record EnrollmentOperation(
    Guid OrganizationId,
    Guid ActorId,
    string OperationKind,
    Guid ResourceId,
    string IdempotencyKey,
    string CommandDigest,
    string OutcomeCode,
    Guid? EnrollmentId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);
