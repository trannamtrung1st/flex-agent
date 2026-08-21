using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public sealed class AssessmentActivationCoordinator(
    IAssessmentAuthorizationPort authorization,
    IAssessmentSourceTransactionPort sources,
    IAssessmentDraftStore store,
    IAssessmentActivationUnitOfWork unitOfWork,
    IActivationBaselineDigester digester,
    IAssessmentCommandDigest commandDigest,
    IAssessmentBaselineStore baselines,
    IAssessmentActivationAttemptStore attempts) : IAssessmentActivationCoordinator
{
    public async Task<ActivationOutcome> ActivateAsync(
        ActivateCohortCommand command,
        CancellationToken cancellationToken = default)
    {
        var strength = AssessmentAuthenticationPolicy.Evaluate(
            command.Actor,
            AssessmentAuthorizationActions.ActivateCohort);
        if (strength is not null)
        {
            return await PersistIdempotentFailureAsync(command, strength, cancellationToken);
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            command.Actor,
            AssessmentAuthorizationActions.ActivateCohort,
            command.CohortId,
            AssessmentResourceTypes.Cohort,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            return await PersistIdempotentFailureAsync(command, AssessmentFailureCodes.Denied, cancellationToken);
        }

        var expectedDigest = commandDigest.Compute(command);
        if (!string.Equals(expectedDigest, command.TrustedCommandDigest, StringComparison.Ordinal))
        {
            return await PersistIdempotentFailureAsync(command, AssessmentFailureCodes.IdempotencyConflict, cancellationToken);
        }

        return await unitOfWork.ExecuteAsync(async transaction =>
        {
            await attempts.AcquireIdempotencyLockAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                command.IdempotencyKey,
                transaction,
                cancellationToken);

            var existingSuccess = await attempts.FindSuccessfulAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                command.IdempotencyKey,
                transaction,
                cancellationToken);
            if (existingSuccess is not null)
            {
                return ExistingOrConflict(existingSuccess, expectedDigest, command);
            }

            var latest = await attempts.FindAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                command.IdempotencyKey,
                transaction,
                cancellationToken);
            if (latest is not null
                && !string.Equals(latest.CommandDigest, expectedDigest, StringComparison.Ordinal))
            {
                return Fail(AssessmentFailureCodes.IdempotencyConflict, command);
            }

            var commitAuth = await authorization.ReauthorizeAsync(
                command.Actor,
                AssessmentAuthorizationActions.ActivateCohort,
                command.CohortId,
                AssessmentResourceTypes.Cohort,
                transaction,
                cancellationToken);
            if (!commitAuth.IsPermitted)
            {
                return await PersistFailureAsync(command, expectedDigest, AssessmentFailureCodes.Denied, transaction, null, cancellationToken);
            }

            var draft = await store.GetDraftAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                transaction,
                cancellationToken);
            var cohort = await store.GetCohortAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                transaction,
                cancellationToken);
            if (draft is null || cohort is null)
            {
                return await PersistFailureAsync(
                    command,
                    expectedDigest,
                    AssessmentFailureCodes.Denied,
                    transaction,
                    draft,
                    cancellationToken);
            }

            existingSuccess = await attempts.FindSuccessfulAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                command.IdempotencyKey,
                transaction,
                cancellationToken);
            if (existingSuccess is not null)
            {
                return ExistingOrConflict(existingSuccess, expectedDigest, command);
            }

            if (draft.RevisionId != command.ExpectedRevisionId
                || draft.RevisionNumber != command.ExpectedRevisionNumber)
            {
                return await PersistFailureAsync(
                    command,
                    expectedDigest,
                    AssessmentFailureCodes.StaleRevision,
                    transaction,
                    draft,
                    cancellationToken);
            }

            if (cohort.State == CohortStates.Activated)
            {
                return await PersistFailureAsync(
                    command,
                    expectedDigest,
                    AssessmentFailureCodes.ConcurrentActivation,
                    transaction,
                    draft,
                    cancellationToken);
            }

            var descriptors = await sources.RevalidateExactAsync(
                draft.OrganizationId,
                AssessmentDraftHandler.CollectReferences(draft),
                transaction,
                cancellationToken);
            var readiness = ReadinessEvaluator.Evaluate(
                new ReadinessContext(draft, descriptors, transaction.AuditAccepted, command.Environment));
            if (readiness.HasBlocker)
            {
                return await PersistFailureAsync(
                    command,
                    expectedDigest,
                    readiness.Issues.First(issue => issue.Severity == ReadinessSeverities.Blocked).ReasonCode,
                    transaction,
                    draft,
                    cancellationToken);
            }

            var occurredAt = DateTimeOffset.UtcNow;
            var provenance = new ActivationProvenance(
                command.Actor.Actor.ActorId,
                command.Actor.Actor.ActorType,
                command.Actor.CorrelationId,
                occurredAt);
            var document = ActivationBaselineDocument.FromReadyDraft(draft, descriptors, provenance);
            if (!document.Succeeded || document.Value is null)
            {
                return await PersistFailureAsync(command, expectedDigest, document.OutcomeCode, transaction, draft, cancellationToken);
            }

            var digest = digester.Digest(document.Value);
            if (!digest.Succeeded || digest.Value is null)
            {
                return await PersistFailureAsync(command, expectedDigest, digest.OutcomeCode, transaction, draft, cancellationToken);
            }

            var baselineId = Guid.CreateVersion7();
            var bound = cohort.BindActivation(
                command.ExpectedRevisionId,
                command.ExpectedRevisionNumber,
                baselineId,
                digest.Value);
            if (!bound.Succeeded || bound.Value is null)
            {
                return await PersistFailureAsync(command, expectedDigest, bound.OutcomeCode, transaction, draft, cancellationToken);
            }

            if (!transaction.AuditAccepted || !transaction.OutboxAccepted)
            {
                return await PersistFailureAsync(command, expectedDigest, AssessmentFailureCodes.AuditUnavailable, transaction, draft, cancellationToken);
            }

            var marked = await store.MarkActivatedAsync(
                draft.OrganizationId,
                draft.ActivityId,
                draft.RevisionId,
                draft.RevisionNumber,
                transaction,
                cancellationToken);
            if (!marked)
            {
                return await PersistFailureAsync(command, expectedDigest, AssessmentFailureCodes.StaleRevision, transaction, draft, cancellationToken);
            }

            await baselines.InsertAsync(
                draft.OrganizationId,
                draft.ActivityId,
                bound.Value.CohortId,
                baselineId,
                document.Value,
                digest.Value,
                transaction,
                command.Actor,
                occurredAt,
                cancellationToken);
            await store.UpdateCohortAsync(bound.Value, transaction, cancellationToken);

            var attempt = CreateAttempt(
                command,
                expectedDigest,
                "assessment.activated",
                bound.Value.BaselineId,
                bound.Value.BaselineDigest,
                bound.Value.State,
                draft);
            await attempts.InsertAsync(attempt, transaction, cancellationToken);
            return FromAttempt(attempt);
        }, cancellationToken);
    }

    public async Task<ActivationOutcome> ReconcileAsync(
        ReconcileActivationQuery query,
        CancellationToken cancellationToken = default)
    {
        var strength = AssessmentAuthenticationPolicy.Evaluate(
            query.Actor,
            AssessmentAuthorizationActions.ReconcileActivation);
        if (strength is not null)
        {
            return new ActivationOutcome(false, strength, query.ActivityId, query.CohortId, null, null, CohortStates.Draft);
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            query.Actor,
            AssessmentAuthorizationActions.ReconcileActivation,
            query.CohortId,
            AssessmentResourceTypes.Cohort,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            return new ActivationOutcome(false, AssessmentFailureCodes.Denied, query.ActivityId, query.CohortId, null, null, CohortStates.Draft);
        }

        return await unitOfWork.ExecuteAsync(async transaction =>
        {
            await attempts.AcquireIdempotencyLockAsync(
                query.Actor.Organization.OrganizationId,
                query.ActivityId,
                query.CohortId,
                query.IdempotencyKey,
                transaction,
                cancellationToken);

            var attempt = await attempts.FindSuccessfulAsync(
                query.Actor.Organization.OrganizationId,
                query.ActivityId,
                query.CohortId,
                query.IdempotencyKey,
                transaction,
                cancellationToken);
            if (attempt is not null)
            {
                return FromAttempt(attempt);
            }

            var cohort = await store.GetCohortAsync(
                query.Actor.Organization.OrganizationId,
                query.ActivityId,
                query.CohortId,
                transaction,
                cancellationToken);
            attempt = await attempts.FindSuccessfulAsync(
                query.Actor.Organization.OrganizationId,
                query.ActivityId,
                query.CohortId,
                query.IdempotencyKey,
                transaction,
                cancellationToken)
                ?? await attempts.FindAsync(
                    query.Actor.Organization.OrganizationId,
                    query.ActivityId,
                    query.CohortId,
                    query.IdempotencyKey,
                    transaction,
                    cancellationToken);
            if (attempt is not null)
            {
                return FromAttempt(attempt);
            }

            if (cohort is null)
            {
                return new ActivationOutcome(false, AssessmentFailureCodes.Denied, query.ActivityId, query.CohortId, null, null, CohortStates.Draft);
            }

            return new ActivationOutcome(
                false,
                AssessmentFailureCodes.Denied,
                query.ActivityId,
                query.CohortId,
                cohort.BaselineId,
                cohort.BaselineDigest,
                cohort.State);
        }, cancellationToken);
    }

    private Task<ActivationOutcome> PersistIdempotentFailureAsync(
        ActivateCohortCommand command,
        string code,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(
            transaction => PersistIdempotentFailureAsync(command, commandDigest.Compute(command), code, transaction, cancellationToken),
            cancellationToken);

    private async Task<ActivationOutcome> PersistIdempotentFailureAsync(
        ActivateCohortCommand command,
        string commandDigestValue,
        string code,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        await attempts.AcquireIdempotencyLockAsync(
            command.Actor.Organization.OrganizationId,
            command.ActivityId,
            command.CohortId,
            command.IdempotencyKey,
            transaction,
            cancellationToken);

        var existingSuccess = await attempts.FindSuccessfulAsync(
            command.Actor.Organization.OrganizationId,
            command.ActivityId,
            command.CohortId,
            command.IdempotencyKey,
            transaction,
            cancellationToken);
        if (existingSuccess is not null)
        {
            return ExistingOrConflict(existingSuccess, commandDigestValue, command);
        }

        var latest = await attempts.FindAsync(
            command.Actor.Organization.OrganizationId,
            command.ActivityId,
            command.CohortId,
            command.IdempotencyKey,
            transaction,
            cancellationToken);
        if (latest is not null
            && !string.Equals(latest.CommandDigest, commandDigestValue, StringComparison.Ordinal))
        {
            return Fail(AssessmentFailureCodes.IdempotencyConflict, command);
        }

        var draft = await store.GetDraftAsync(
            command.Actor.Organization.OrganizationId,
            command.ActivityId,
            transaction,
            cancellationToken);
        return await PersistFailureAsync(command, commandDigestValue, code, transaction, draft, cancellationToken);
    }

    private async Task<ActivationOutcome> PersistFailureAsync(
        ActivateCohortCommand command,
        string commandDigestValue,
        string code,
        IAssessmentActivationTransaction transaction,
        ActivityDraft? draft,
        CancellationToken cancellationToken)
    {
        var cohort = await store.GetCohortAsync(
            command.Actor.Organization.OrganizationId,
            command.ActivityId,
            command.CohortId,
            transaction,
            cancellationToken);
        if (cohort is null)
        {
            return Fail(code, command);
        }

        var attempt = CreateAttempt(command, commandDigestValue, code, null, null, CohortStates.Draft, draft);
        await attempts.InsertAsync(attempt, transaction, cancellationToken);
        return FromAttempt(attempt);
    }

    private static AssessmentActivationAttempt CreateAttempt(
        ActivateCohortCommand command,
        string digest,
        string outcomeCode,
        Guid? baselineId,
        string? baselineDigest,
        string cohortState,
        ActivityDraft? draft) =>
        new(
            command.Actor.Organization.OrganizationId,
            command.ActivityId,
            command.CohortId,
            Guid.CreateVersion7(),
            command.ExpectedRevisionId,
            command.ExpectedRevisionNumber,
            draft?.RevisionId,
            draft?.RevisionNumber,
            command.IdempotencyKey,
            digest,
            outcomeCode,
            baselineId,
            baselineDigest,
            cohortState,
            command.Actor.Actor.ActorId,
            command.Actor.CorrelationId,
            command.Actor.Actor.ActorType,
            command.Actor.SourceChannel);

    private static ActivationOutcome ExistingOrConflict(
        AssessmentActivationAttempt existing,
        string expectedDigest,
        ActivateCohortCommand command) =>
        string.Equals(existing.CommandDigest, expectedDigest, StringComparison.Ordinal)
            ? FromAttempt(existing)
            : Fail(AssessmentFailureCodes.IdempotencyConflict, command);

    private static ActivationOutcome FromAttempt(AssessmentActivationAttempt attempt) =>
        new(
            string.Equals(attempt.OutcomeCode, "assessment.activated", StringComparison.Ordinal),
            attempt.OutcomeCode,
            attempt.ActivityId,
            attempt.CohortId,
            attempt.BaselineId,
            attempt.BaselineDigest,
            attempt.CohortState);

    private static ActivationOutcome Fail(string code, ActivateCohortCommand command) =>
        new(false, code, command.ActivityId, command.CohortId, null, null, CohortStates.Draft);
}

public interface IAssessmentCommandDigest
{
    string Compute(ActivateCohortCommand command);
}
