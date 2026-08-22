using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class EnrollmentCoordinator(
    IEnrollmentAuthorizationPort authorization,
    IActivatedCohortPort cohorts,
    IEnrollmentCandidatePort candidates,
    IEnrollmentStore enrollments,
    IEnrollmentOperationStore operations,
    IEnrollmentAuditPort audit,
    IEnrollmentUnitOfWork unitOfWork,
    IEnrollmentSessionPort sessions,
    IEnrollmentClock? clock = null) : IEnrollmentCoordinator
{
    private readonly IEnrollmentClock _clock = clock ?? new SystemEnrollmentClock();

    public Task<EnrollmentMutationOutcome> AssignAsync(
        AssignEnrollmentCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            command.Actor,
            EnrollmentAuthorizationActions.Assign,
            EnrollmentOperationKinds.Assign,
            command.ActivityId,
            command.CohortId,
            command.CohortId,
            EnrollmentResourceTypes.Cohort,
            command.IdempotencyKey,
            command.TrustedCommandDigest,
            EnrollmentCommandDigest.Compute(
                EnrollmentOperationKinds.Assign,
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                null,
                command.ParticipantActorId,
                null,
                null),
            async (transaction, binding) =>
            {
                var candidate = await candidates.RevalidateEligibleAsync(
                    command.Actor.Organization.OrganizationId,
                    command.ParticipantActorId,
                    transaction,
                    cancellationToken);
                if (candidate is null)
                {
                    return Fail(EnrollmentFailureCodes.Ineligible);
                }

                await operations.AcquireLiveParticipantLockAsync(
                    command.Actor.Organization.OrganizationId,
                    command.ActivityId,
                    command.ParticipantActorId,
                    transaction,
                    cancellationToken);
                var live = await enrollments.FindLiveForParticipantAsync(
                    command.Actor.Organization.OrganizationId,
                    command.ActivityId,
                    command.ParticipantActorId,
                    transaction,
                    cancellationToken);
                if (live is not null)
                {
                    if (live.CohortId == command.CohortId)
                    {
                        return Success(live, EnrollmentOutcomes.Deduplicated, command.Actor.GrantedActions);
                    }

                    return Fail(EnrollmentFailureCodes.Conflict);
                }

                var created = Enrollment.Create(
                    Guid.CreateVersion7(),
                    command.Actor.Organization.OrganizationId,
                    command.ActivityId,
                    command.CohortId,
                    binding.BaselineId,
                    binding.TaskSourceId,
                    binding.TaskVersionId,
                    binding.TaskContentDigest,
                    binding.LifecyclePolicyId,
                    binding.LifecyclePolicyVersion,
                    command.ParticipantActorId,
                    command.Actor.Actor.ActorId,
                    _clock.UtcNow);
                if (!created.Succeeded || created.Value is null)
                {
                    return Fail(created.OutcomeCode);
                }

                var enrollmentEvent = NewEvent(
                    created.Value,
                    "absent",
                    EnrollmentStates.Active,
                    EnrollmentReasonCodes.RestrictionRemoved,
                    command.Actor);
                try
                {
                    await enrollments.InsertAsync(created.Value, enrollmentEvent, transaction, cancellationToken);
                }
                catch (EnrollmentLiveUniquenessException)
                {
                    var raced = await enrollments.FindLiveForParticipantAsync(
                        command.Actor.Organization.OrganizationId,
                        command.ActivityId,
                        command.ParticipantActorId,
                        transaction,
                        cancellationToken);
                    if (raced is not null && raced.CohortId == command.CohortId)
                    {
                        return Success(raced, EnrollmentOutcomes.Deduplicated, command.Actor.GrantedActions);
                    }

                    return Fail(EnrollmentFailureCodes.Conflict);
                }

                await audit.WriteAvailabilityAsync(created.Value, command.Actor, transaction, cancellationToken);
                return Success(created.Value, EnrollmentOutcomes.Assigned, command.Actor.GrantedActions);
            },
            cancellationToken);

    public Task<EnrollmentMutationOutcome> MutateAsync(
        EnrollmentLifecycleCommand command,
        CancellationToken cancellationToken = default)
    {
        var action = command.OperationKind switch
        {
            EnrollmentOperationKinds.Suspend => EnrollmentAuthorizationActions.Suspend,
            EnrollmentOperationKinds.Restore => EnrollmentAuthorizationActions.Restore,
            EnrollmentOperationKinds.Close => EnrollmentAuthorizationActions.Close,
            EnrollmentOperationKinds.Revoke => EnrollmentAuthorizationActions.Revoke,
            _ => string.Empty,
        };
        if (action.Length == 0
            || !string.Equals(
                command.ReasonCode,
                EnrollmentLifecycle.RequiredReason(command.OperationKind),
                StringComparison.Ordinal))
        {
            return Task.FromResult(Fail(EnrollmentFailureCodes.InvalidReason));
        }

        return ExecuteAsync(
            command.Actor,
            action,
            command.OperationKind,
            command.ActivityId,
            command.CohortId,
            command.EnrollmentId,
            EnrollmentResourceTypes.Enrollment,
            command.IdempotencyKey,
            command.TrustedCommandDigest,
            EnrollmentCommandDigest.Compute(
                command.OperationKind,
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                command.EnrollmentId,
                null,
                command.ReasonCode,
                command.ExpectedRevision),
            async (transaction, _) =>
            {
                var current = await enrollments.FindAsync(
                    command.Actor.Organization.OrganizationId,
                    command.EnrollmentId,
                    transaction,
                    cancellationToken);
                if (current is null
                    || current.ActivityId != command.ActivityId
                    || current.CohortId != command.CohortId)
                {
                    return Fail(EnrollmentFailureCodes.Denied);
                }

                var transitioned = current.Transition(
                    EnrollmentLifecycle.TargetStatus(command.OperationKind),
                    command.ReasonCode,
                    command.ExpectedRevision,
                    _clock.UtcNow);
                if (!transitioned.Succeeded || transitioned.Value is null)
                {
                    return Fail(transitioned.OutcomeCode);
                }

                var enrollmentEvent = NewEvent(
                    transitioned.Value,
                    current.Status,
                    transitioned.Value.Status,
                    command.ReasonCode,
                    command.Actor);
                try
                {
                    await enrollments.UpdateAsync(transitioned.Value, enrollmentEvent, transaction, cancellationToken);
                }
                catch (EnrollmentStaleRevisionException)
                {
                    return Fail(EnrollmentFailureCodes.StaleRevision);
                }

                return Success(transitioned.Value, transitioned.OutcomeCode, command.Actor.GrantedActions);
            },
            cancellationToken);
    }

    private async Task<EnrollmentMutationOutcome> ExecuteAsync(
        EnrollmentActorContext actor,
        string action,
        string operationKind,
        Guid activityId,
        Guid cohortId,
        Guid resourceId,
        string resourceType,
        string idempotencyKey,
        string trustedDigest,
        string expectedDigest,
        Func<IEnrollmentTransaction, ActivatedCohortBinding, Task<EnrollmentMutationOutcome>> commit,
        CancellationToken cancellationToken)
    {
        if (EnrollmentIdempotencyKey.Validate(idempotencyKey) is { } invalid)
        {
            return Fail(invalid);
        }

        var strength = EnrollmentAuthenticationPolicy.Evaluate(actor, action);
        if (strength is not null)
        {
            return Fail(strength);
        }

        if (!string.Equals(expectedDigest, trustedDigest, StringComparison.Ordinal))
        {
            return Fail(EnrollmentFailureCodes.IdempotencyConflict);
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            actor,
            action,
            resourceId,
            resourceType,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            return Fail(EnrollmentFailureCodes.Denied);
        }

        try
        {
        return await unitOfWork.ExecuteAsync(async transaction =>
        {
            await operations.AcquireLockAsync(
                actor.Organization.OrganizationId,
                actor.Actor.ActorId,
                operationKind,
                resourceId,
                idempotencyKey,
                transaction,
                cancellationToken);
            if (!await sessions.RevalidateLiveAsync(actor, transaction, cancellationToken))
            {
                return Fail(EnrollmentFailureCodes.Denied);
            }

            var existing = await operations.FindAsync(
                actor.Organization.OrganizationId,
                actor.Actor.ActorId,
                operationKind,
                resourceId,
                idempotencyKey,
                transaction,
                cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.CommandDigest, expectedDigest, StringComparison.Ordinal))
                {
                    return Fail(EnrollmentFailureCodes.IdempotencyConflict);
                }

                var replayAuth = await authorization.ReauthorizeAsync(
                    actor,
                    action,
                    resourceId,
                    resourceType,
                    transaction,
                    cancellationToken);
                if (!replayAuth.IsPermitted)
                {
                    return Fail(EnrollmentFailureCodes.Denied);
                }

                if (existing.EnrollmentId is { } existingId)
                {
                    var replayed = await enrollments.FindAsync(
                        actor.Organization.OrganizationId,
                        existingId,
                        transaction,
                        cancellationToken);
                    if (replayed is not null)
                    {
                        return Success(replayed, existing.OutcomeCode, actor.GrantedActions);
                    }
                }

                return new EnrollmentMutationOutcome(
                    existing.OutcomeCode is EnrollmentOutcomes.Assigned
                        or EnrollmentOutcomes.Deduplicated
                        or EnrollmentOutcomes.Suspended
                        or EnrollmentOutcomes.Restored
                        or EnrollmentOutcomes.Closed
                        or EnrollmentOutcomes.Revoked,
                    existing.OutcomeCode,
                    existing.EnrollmentId,
                    null,
                    null,
                    null,
                    []);
            }

            var binding = await cohorts.RevalidateAsync(
                actor.Organization.OrganizationId,
                activityId,
                cohortId,
                transaction,
                cancellationToken);
            if (binding is null
                || binding.CohortState != "activated"
                || (operationKind == EnrollmentOperationKinds.Assign && binding.VerificationDegraded))
            {
                return Fail(EnrollmentFailureCodes.Unavailable);
            }

            var reauthorized = await authorization.ReauthorizeAsync(
                actor,
                action,
                resourceId,
                resourceType,
                transaction,
                cancellationToken);
            if (!reauthorized.IsPermitted)
            {
                return Fail(EnrollmentFailureCodes.Denied);
            }

            var outcome = await commit(transaction, binding);
            if (!transaction.AuditAccepted || !transaction.OutboxAccepted)
            {
                throw new EnrollmentAuditUnavailableException();
            }

            if (outcome.Succeeded || outcome.OutcomeCode is EnrollmentFailureCodes.Conflict)
            {
                await operations.InsertAsync(
                    new EnrollmentOperation(
                        actor.Organization.OrganizationId,
                        actor.Actor.ActorId,
                        operationKind,
                        resourceId,
                        idempotencyKey,
                        expectedDigest,
                        outcome.OutcomeCode,
                        outcome.EnrollmentId,
                        _clock.UtcNow,
                        _clock.UtcNow.Add(EnrollmentIdempotencyKey.Retention)),
                    transaction,
                    cancellationToken);
            }

            var auditResourceId = outcome.EnrollmentId ?? resourceId;
            var auditResourceType = outcome.EnrollmentId is not null
                || operationKind != EnrollmentOperationKinds.Assign
                    ? EnrollmentResourceTypes.Enrollment
                    : EnrollmentResourceTypes.Cohort;
            await audit.WriteRequiredDurableAsync(
                actor,
                action,
                auditResourceId,
                auditResourceType,
                outcome.Succeeded ? AuthorizationOutcomes.Permit : AuthorizationOutcomes.Deny,
                outcome.Succeeded ? null : outcome.OutcomeCode,
                reauthorized,
                transaction,
                cancellationToken);
            if (!transaction.AuditAccepted || !transaction.OutboxAccepted)
            {
                throw new EnrollmentAuditUnavailableException();
            }

            return outcome;
        }, cancellationToken);
        }
        catch (EnrollmentAuditUnavailableException)
        {
            return Fail(EnrollmentFailureCodes.AuditUnavailable);
        }
        catch (EnrollmentStaleRevisionException)
        {
            return Fail(EnrollmentFailureCodes.StaleRevision);
        }
        catch (EnrollmentLiveUniquenessException)
        {
            return Fail(EnrollmentFailureCodes.Conflict);
        }
    }

    private EnrollmentEvent NewEvent(
        Enrollment enrollment,
        string priorStatus,
        string newStatus,
        string reasonCode,
        EnrollmentActorContext actor) =>
        new(
            Guid.CreateVersion7(),
            enrollment.EnrollmentId,
            enrollment.OrganizationId,
            enrollment.Revision,
            priorStatus,
            newStatus,
            reasonCode,
            actor.Actor.ActorId,
            _clock.UtcNow,
            actor.CorrelationId,
            null,
            enrollment.Revision);

    private static EnrollmentMutationOutcome Success(
        Enrollment enrollment,
        string outcomeCode,
        IReadOnlyList<string> granted) =>
        new(
            true,
            outcomeCode,
            enrollment.EnrollmentId,
            enrollment.Status,
            enrollment.Revision,
            enrollment.VisibilityForParticipant(),
            EnrollmentProjection.AdministratorActions(enrollment.Status, granted.ToHashSet(StringComparer.Ordinal)));

    private static EnrollmentMutationOutcome Fail(string outcomeCode) =>
        new(false, outcomeCode, null, null, null, null, []);
}

public sealed class EnrollmentAuditUnavailableException : Exception;

public sealed class EnrollmentStaleRevisionException : Exception;

public sealed class EnrollmentLiveUniquenessException : Exception;
