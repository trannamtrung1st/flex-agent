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
            return Fail(strength, command);
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            command.Actor,
            AssessmentAuthorizationActions.ActivateCohort,
            command.CohortId,
            AssessmentResourceTypes.Cohort,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            return Fail(AssessmentFailureCodes.Denied, command);
        }

        var expectedDigest = commandDigest.Compute(command);
        if (!string.Equals(expectedDigest, command.TrustedCommandDigest, StringComparison.Ordinal))
        {
            return Fail(AssessmentFailureCodes.IdempotencyConflict, command);
        }

        return await unitOfWork.ExecuteAsync(async transaction =>
        {
            var commitAuth = await authorization.ReauthorizeAsync(
                command.Actor,
                AssessmentAuthorizationActions.ActivateCohort,
                command.CohortId,
                AssessmentResourceTypes.Cohort,
                transaction,
                cancellationToken);
            if (!commitAuth.IsPermitted)
            {
                return Fail(AssessmentFailureCodes.Denied, command);
            }

            var existingAttempt = await attempts.FindAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                command.IdempotencyKey,
                transaction,
                cancellationToken);
            if (existingAttempt is not null)
            {
                return string.Equals(existingAttempt.CommandDigest, expectedDigest, StringComparison.Ordinal)
                    ? FromAttempt(existingAttempt)
                    : Fail(AssessmentFailureCodes.IdempotencyConflict, command);
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
                return Fail(AssessmentFailureCodes.Denied, command);
            }

            if (draft.RevisionId != command.ExpectedRevisionId
                || draft.RevisionNumber != command.ExpectedRevisionNumber)
            {
                return Fail(AssessmentFailureCodes.StaleRevision, command);
            }

            if (cohort.State == CohortStates.Activated)
            {
                return Fail(AssessmentFailureCodes.ConcurrentActivation, command);
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
                return Fail(readiness.Issues.First(issue => issue.Severity == ReadinessSeverities.Blocked).ReasonCode, command);
            }

            var document = ActivationBaselineDocument.FromReadyDraft(draft, descriptors);
            if (!document.Succeeded || document.Value is null)
            {
                return Fail(document.OutcomeCode, command);
            }

            var digest = digester.Digest(document.Value);
            if (!digest.Succeeded || digest.Value is null)
            {
                return Fail(digest.OutcomeCode, command);
            }

            var baselineId = Guid.CreateVersion7();
            var bound = cohort.BindActivation(
                command.ExpectedRevisionId,
                command.ExpectedRevisionNumber,
                baselineId,
                digest.Value);
            if (!bound.Succeeded || bound.Value is null)
            {
                return Fail(bound.OutcomeCode, command);
            }

            if (!transaction.AuditAccepted || !transaction.OutboxAccepted)
            {
                return Fail(AssessmentFailureCodes.AuditUnavailable, command);
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
                return Fail(AssessmentFailureCodes.StaleRevision, command);
            }

            await baselines.InsertAsync(
                draft.OrganizationId,
                draft.ActivityId,
                bound.Value.CohortId,
                baselineId,
                document.Value,
                digest.Value,
                transaction,
                cancellationToken);
            await store.UpdateCohortAsync(bound.Value, transaction, cancellationToken);

            var attempt = new AssessmentActivationAttempt(
                draft.OrganizationId,
                draft.ActivityId,
                bound.Value.CohortId,
                Guid.CreateVersion7(),
                command.ExpectedRevisionId,
                command.ExpectedRevisionNumber,
                command.IdempotencyKey,
                expectedDigest,
                "assessment.activated",
                bound.Value.BaselineId,
                bound.Value.BaselineDigest,
                bound.Value.State);
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
            var attempt = await attempts.FindAsync(
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
