using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public sealed class AssessmentActivationCoordinator(
    IAssessmentAuthorizationPort authorization,
    IAssessmentSourceTransactionPort sources,
    IAssessmentDraftStore store,
    IAssessmentActivationUnitOfWork unitOfWork,
    IActivationBaselineDigester digester,
    IAssessmentCommandDigest commandDigest,
    IAssessmentBaselineStore baselines) : IAssessmentActivationCoordinator
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

            var draft = await store.GetDraftAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                cancellationToken);
            var cohort = await store.GetCohortAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                cancellationToken);
            if (draft is null || cohort is null)
            {
                return Fail(AssessmentFailureCodes.Denied, command);
            }

            if (cohort.State == CohortStates.Activated)
            {
                return new ActivationOutcome(
                    true,
                    "assessment.already_activated",
                    draft.ActivityId,
                    cohort.CohortId,
                    cohort.BaselineId,
                    cohort.BaselineDigest,
                    cohort.State);
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

            var marked = draft.MarkActivatedCohort();
            if (!marked.Succeeded || marked.Value is null)
            {
                return Fail(marked.OutcomeCode, command);
            }

            await baselines.InsertAsync(
                marked.Value.OrganizationId,
                marked.Value.ActivityId,
                bound.Value.CohortId,
                baselineId,
                document.Value,
                digest.Value,
                transaction,
                cancellationToken);
            await store.UpdateDraftAsync(marked.Value, transaction, cancellationToken);
            await store.UpdateCohortAsync(bound.Value, transaction, cancellationToken);

            return new ActivationOutcome(
                true,
                "assessment.activated",
                marked.Value.ActivityId,
                bound.Value.CohortId,
                bound.Value.BaselineId,
                bound.Value.BaselineDigest,
                bound.Value.State);
        }, cancellationToken);
    }

    private static ActivationOutcome Fail(string code, ActivateCohortCommand command) =>
        new(false, code, command.ActivityId, command.CohortId, null, null, CohortStates.Draft);
}

public interface IAssessmentCommandDigest
{
    string Compute(ActivateCohortCommand command);
}
