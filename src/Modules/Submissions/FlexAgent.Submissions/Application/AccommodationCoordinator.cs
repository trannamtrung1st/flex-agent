using System.Diagnostics;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class AccommodationCoordinator(
    IEnrollmentAuthorizationPort authorization,
    IActivatedCohortPort cohorts,
    IEnrollmentStore enrollments,
    IAccommodationStore accommodations,
    IEnrollmentOperationStore operations,
    IEnrollmentAuditPort audit,
    IEnrollmentUnitOfWork unitOfWork,
    IEnrollmentSessionPort sessions,
    IAccommodationPolicyPort policies,
    IEnrollmentClock? clock = null,
    IEnrollmentTelemetry? telemetry = null) : IAccommodationCoordinator
{
    private readonly IEnrollmentClock _clock = clock ?? new SystemEnrollmentClock();
    private readonly IEnrollmentTelemetry _telemetry = telemetry ?? NullEnrollmentTelemetry.Instance;

    public Task<AccommodationMutationOutcome> GrantAsync(
        GrantAccommodationCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            command.Actor,
            EnrollmentAuthorizationActions.GrantAccommodation,
            AccommodationOperationKinds.Grant,
            command.ActivityId,
            command.CohortId,
            command.EnrollmentId,
            command.IdempotencyKey,
            command.TrustedCommandDigest,
            AccommodationCommandDigest.Compute(
                AccommodationOperationKinds.Grant,
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                command.EnrollmentId,
                null,
                command.Dimension,
                command.RequestedValue,
                command.ReasonCategory,
                command.FairnessException,
                command.ExpectedRevision,
                command.ExpiresAtUtc),
            async (transaction, binding, enrollment) =>
            {
                if (enrollment.Revision != command.ExpectedRevision)
                {
                    return Fail(EnrollmentFailureCodes.StaleRevision);
                }

                var baseline = TimingMapper.BaselineFrom(binding);
                var policy = await policies.ResolveCurrentAsync(
                    enrollment.OrganizationId,
                    baseline,
                    _clock.UtcNow,
                    transaction,
                    cancellationToken);
                var effectivePolicy = baseline.VerificationDegraded
                    ? null
                    : AccommodationPolicyNormalizer.EffectiveBounds(
                        baseline.FrozenPolicy,
                        baseline.FrozenPolicySnapshot,
                        policy);
                if (effectivePolicy is null || !effectivePolicy.EnvironmentEligible)
                {
                    return Fail(AccommodationFailureCodes.PolicyUnavailable);
                }

                var created = Accommodation.Request(
                    TimingMapper.ParentFrom(enrollment),
                    command.Dimension,
                    command.RequestedValue,
                    baseline.FrozenPolicy,
                    effectivePolicy,
                    command.ReasonCategory,
                    _clock.UtcNow,
                    command.ExpiresAtUtc,
                    command.Actor.Actor.ActorId,
                    1,
                    command.FairnessException);
                if (!created.Succeeded || created.Value is null)
                {
                    return Fail(created.OutcomeCode);
                }

                if (created.Value.Status == AccommodationStates.Granted)
                {
                    await SupersedeCurrentAsync(
                        enrollment,
                        created.Value,
                        command.Actor.Actor.ActorId,
                        transaction,
                        cancellationToken);
                }

                await accommodations.InsertAsync(
                    created.Value,
                    command.Actor.Actor.ActorId,
                    transaction,
                    cancellationToken);
                return Success(created.Value, created.OutcomeCode, command.Actor.GrantedActions, enrollment);
            },
            cancellationToken);

    public Task<AccommodationMutationOutcome> DecideAsync(
        DecideAccommodationCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            command.Actor,
            EnrollmentAuthorizationActions.DecideAccommodation,
            AccommodationOperationKinds.Decide,
            command.ActivityId,
            command.CohortId,
            command.EnrollmentId,
            command.IdempotencyKey,
            command.TrustedCommandDigest,
            AccommodationCommandDigest.Compute(
                AccommodationOperationKinds.Decide,
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                command.EnrollmentId,
                command.AccommodationId,
                null,
                null,
                null,
                command.Approve,
                command.ExpectedRevision),
            async (transaction, binding, enrollment) =>
            {
                var current = await accommodations.FindAsync(
                    enrollment.OrganizationId,
                    command.AccommodationId,
                    transaction,
                    cancellationToken);
                if (current is null || current.Parent.EnrollmentId != enrollment.EnrollmentId)
                {
                    return Fail(EnrollmentFailureCodes.Denied);
                }

                var baseline = TimingMapper.BaselineFrom(binding);
                var policy = await policies.ResolveCurrentAsync(
                    enrollment.OrganizationId,
                    baseline,
                    _clock.UtcNow,
                    transaction,
                    cancellationToken);
                var effectivePolicy = baseline.VerificationDegraded
                    ? null
                    : AccommodationPolicyNormalizer.EffectiveBounds(
                        baseline.FrozenPolicy,
                        baseline.FrozenPolicySnapshot,
                        policy);
                if (effectivePolicy is null)
                {
                    return Fail(AccommodationFailureCodes.PolicyUnavailable);
                }

                var decided = current.Decide(
                    command.Actor.Actor.ActorId,
                    command.Approve,
                    baseline.FrozenPolicy,
                    effectivePolicy,
                    command.ExpectedRevision,
                    _clock.UtcNow);
                if (!decided.Succeeded || decided.Value is null)
                {
                    return Fail(decided.OutcomeCode);
                }

                if (decided.Value.Status == AccommodationStates.Granted)
                {
                    await SupersedeCurrentAsync(
                        enrollment,
                        decided.Value,
                        command.Actor.Actor.ActorId,
                        transaction,
                        cancellationToken);
                }

                await accommodations.UpdateAsync(
                    decided.Value,
                    current.Status,
                    command.Actor.Actor.ActorId,
                    transaction,
                    cancellationToken);
                return Success(decided.Value, decided.OutcomeCode, command.Actor.GrantedActions, enrollment);
            },
            cancellationToken);

    public Task<AccommodationMutationOutcome> RevokeAsync(
        RevokeAccommodationCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            command.Actor,
            EnrollmentAuthorizationActions.RevokeAccommodation,
            AccommodationOperationKinds.Revoke,
            command.ActivityId,
            command.CohortId,
            command.EnrollmentId,
            command.IdempotencyKey,
            command.TrustedCommandDigest,
            AccommodationCommandDigest.Compute(
                AccommodationOperationKinds.Revoke,
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                command.EnrollmentId,
                command.AccommodationId,
                null,
                null,
                null,
                false,
                command.ExpectedRevision),
            async (transaction, binding, enrollment) =>
            {
                var current = await accommodations.FindAsync(
                    enrollment.OrganizationId,
                    command.AccommodationId,
                    transaction,
                    cancellationToken);
                if (current is null || current.Parent.EnrollmentId != enrollment.EnrollmentId)
                {
                    return Fail(EnrollmentFailureCodes.Denied);
                }

                if (current.Revision != command.ExpectedRevision)
                {
                    return Fail(EnrollmentFailureCodes.StaleRevision);
                }

                var revoked = current.Revoke(command.Actor.Actor.ActorId, _clock.UtcNow);
                if (!revoked.Succeeded || revoked.Value is null)
                {
                    return Fail(revoked.OutcomeCode);
                }

                await accommodations.UpdateAsync(
                    revoked.Value,
                    current.Status,
                    command.Actor.Actor.ActorId,
                    transaction,
                    cancellationToken);
                return Success(revoked.Value, revoked.OutcomeCode, command.Actor.GrantedActions, enrollment);
            },
            cancellationToken);

    private async Task<AccommodationMutationOutcome> ExecuteAsync(
        EnrollmentActorContext actor,
        string action,
        string operationKind,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        string idempotencyKey,
        string trustedDigest,
        string expectedDigest,
        Func<IEnrollmentTransaction, ActivatedCohortBinding, Enrollment, Task<AccommodationMutationOutcome>> commit,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            if (EnrollmentIdempotencyKey.Validate(idempotencyKey) is { } invalid)
            {
                return Fail(invalid);
            }

            if (EnrollmentAuthenticationPolicy.Evaluate(actor, action) is not null)
            {
                return Fail(EnrollmentFailureCodes.Denied);
            }

            if (!string.Equals(expectedDigest, trustedDigest, StringComparison.Ordinal))
            {
                return Fail(EnrollmentFailureCodes.IdempotencyConflict);
            }

            var admission = await authorization.AuthorizeAdmissionAsync(
                actor,
                action,
                enrollmentId,
                EnrollmentResourceTypes.Enrollment,
                cancellationToken);
            if (!admission.IsPermitted)
            {
                return Fail(EnrollmentFailureCodes.Denied);
            }

            var outcome = await unitOfWork.ExecuteAsync(actor, async transaction =>
            {
                await operations.AcquireLockAsync(
                    actor.Organization.OrganizationId,
                    actor.Actor.ActorId,
                    operationKind,
                    enrollmentId,
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
                    enrollmentId,
                    idempotencyKey,
                    transaction,
                    cancellationToken);
                if (existing is not null)
                {
                    if (!string.Equals(existing.CommandDigest, expectedDigest, StringComparison.Ordinal))
                    {
                        return Fail(EnrollmentFailureCodes.IdempotencyConflict);
                    }

                    return new AccommodationMutationOutcome(
                        existing.OutcomeCode is AccommodationOutcomes.Granted
                            or AccommodationOutcomes.ApprovalRequired
                            or AccommodationOutcomes.Rejected
                            or AccommodationOutcomes.Revoked,
                        existing.OutcomeCode,
                        existing.EnrollmentId,
                        enrollmentId,
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
                if (binding is null || binding.VerificationDegraded)
                {
                    return Fail(EnrollmentFailureCodes.Unavailable);
                }

                var enrollment = await enrollments.FindAsync(
                    actor.Organization.OrganizationId,
                    enrollmentId,
                    transaction,
                    cancellationToken);
                if (enrollment is null
                    || enrollment.ActivityId != activityId
                    || enrollment.CohortId != cohortId
                    || !EnrollmentProjection.IsLive(enrollment.Status))
                {
                    return Fail(EnrollmentFailureCodes.Denied);
                }

                var reauthorized = await authorization.ReauthorizeAsync(
                    actor,
                    action,
                    enrollmentId,
                    EnrollmentResourceTypes.Enrollment,
                    transaction,
                    cancellationToken);
                if (!reauthorized.IsPermitted)
                {
                    return Fail(EnrollmentFailureCodes.Denied);
                }

                var committed = await commit(transaction, binding, enrollment);
                await operations.InsertAsync(
                    new EnrollmentOperation(
                        actor.Organization.OrganizationId,
                        actor.Actor.ActorId,
                        operationKind,
                        enrollmentId,
                        idempotencyKey,
                        expectedDigest,
                        committed.OutcomeCode,
                        committed.AccommodationId,
                        _clock.UtcNow,
                        _clock.UtcNow.Add(EnrollmentIdempotencyKey.Retention)),
                    transaction,
                    cancellationToken);
                await audit.WriteRequiredDurableAsync(
                    actor,
                    action,
                    enrollmentId,
                    EnrollmentResourceTypes.Enrollment,
                    committed.Succeeded ? AuthorizationOutcomes.Permit : AuthorizationOutcomes.Deny,
                    committed.Succeeded ? null : committed.OutcomeCode,
                    reauthorized,
                    transaction,
                    cancellationToken);
                if (!transaction.AuditAccepted || !transaction.OutboxAccepted)
                {
                    throw new EnrollmentAuditUnavailableException();
                }

                return committed;
            }, cancellationToken);
            _telemetry.RecordMutation(operationKind, EnrollmentTelemetryLabels.ClassifyMutation(outcome.Succeeded, outcome.OutcomeCode), Stopwatch.GetElapsedTime(started));
            return outcome;
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
        catch (EnrollmentSessionExpiredException)
        {
            return Fail(EnrollmentFailureCodes.Denied);
        }
    }

    private async Task SupersedeCurrentAsync(
        Enrollment enrollment,
        Accommodation incoming,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var existing = await accommodations.ListForEnrollmentAsync(
            enrollment.OrganizationId,
            enrollment.EnrollmentId,
            transaction,
            cancellationToken);
        foreach (var record in existing.Where(item =>
                     item.Dimension == incoming.Dimension
                     && item.Status == AccommodationStates.Granted
                     && item.AccommodationId != incoming.AccommodationId))
        {
            await accommodations.UpdateAsync(
                record.Supersede(incoming.AccommodationId, _clock.UtcNow),
                record.Status,
                actorId,
                transaction,
                cancellationToken);
        }
    }

    private static AccommodationMutationOutcome Success(
        Accommodation accommodation,
        string outcomeCode,
        IReadOnlyList<string> granted,
        Enrollment enrollment) =>
        new(
            true,
            outcomeCode,
            accommodation.AccommodationId,
            enrollment.EnrollmentId,
            accommodation.Status,
            accommodation.Revision,
            EnrollmentProjection.AdministratorActions(enrollment.Status, granted.ToHashSet(StringComparer.Ordinal)));

    private static AccommodationMutationOutcome Fail(string outcomeCode) =>
        new(false, outcomeCode, null, null, null, null, []);
}

public static class TimingMapper
{
    public static BaselineTiming BaselineFrom(ActivatedCohortBinding binding)
    {
        var identity = new AccommodationPolicyIdentity(
            binding.FrozenPolicySourceId == Guid.Empty
                ? Guid.Parse("22222222-2222-2222-2222-222222222201")
                : binding.FrozenPolicySourceId,
            binding.FrozenPolicyVersionId == Guid.Empty
                ? Guid.Parse("33333333-3333-3333-3333-333333333301")
                : binding.FrozenPolicyVersionId,
            string.IsNullOrWhiteSpace(binding.FrozenPolicyDigest)
                ? new string('b', 64)
                : binding.FrozenPolicyDigest);
        var snapshot = binding.FrozenAccommodationPolicy;
        var snapshotInvalid = snapshot is not null
            && (snapshot.Identity != identity || snapshot.OrganizationId != binding.OrganizationId);
        return new BaselineTiming(
            binding.StartsAtUtc,
            binding.EndsAtUtc,
            binding.DeadlineUtc,
            binding.TimeZoneId,
            binding.AttemptLimit,
            binding.PerAttemptDurationSeconds,
            identity,
            binding.VerificationDegraded || snapshotInvalid,
            snapshotInvalid ? null : snapshot);
    }

    public static AccommodationParentBinding ParentFrom(Enrollment enrollment) =>
        new(
            enrollment.OrganizationId,
            enrollment.ActivityId,
            enrollment.CohortId,
            enrollment.BaselineId,
            enrollment.EnrollmentId,
            enrollment.ParticipantActorId);
}
