using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public sealed class AssessmentActivationCoordinator(
    IAssessmentAuthorizationPort authorization,
    IAssessmentSourceTransactionPort sources,
    IAssessmentDraftStore store,
    IAssessmentActivationUnitOfWork unitOfWork,
    IActivationBaselineDigester digester,
    IAssessmentCommandDigest commandDigest,
    IAssessmentBaselineStore baselines,
    IAssessmentActivationAttemptStore attempts,
    IAssessmentClock? clock = null) : IAssessmentActivationCoordinator
{
    private readonly IAssessmentClock _clock = clock ?? new SystemAssessmentClock();

    public async Task<ActivationOutcome> ActivateAsync(
        ActivateCohortCommand command,
        CancellationToken cancellationToken = default)
    {
        if (AssessmentIdempotencyKey.Validate(command.IdempotencyKey) is { } invalidKey)
        {
            return await unitOfWork.ExecuteAsync(
                async transaction =>
                {
                    await attempts.InsertRequestAuditAsync(
                        command.Actor,
                        AssessmentAuthorizationActions.ActivateCohort,
                        command.CohortId,
                        AssessmentResourceTypes.Cohort,
                        "deny",
                        invalidKey,
                        transaction,
                        cancellationToken);
                    return Fail(invalidKey, command);
                },
                cancellationToken);
        }

        var startedAt = _clock.UtcNow;
        var expectedDigest = commandDigest.Compute(command);
        var strength = AssessmentAuthenticationPolicy.Evaluate(
            command.Actor,
            AssessmentAuthorizationActions.ActivateCohort);
        if (strength is not null)
        {
            return await PersistIdempotentFailureAsync(command, expectedDigest, strength, startedAt, cancellationToken);
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            command.Actor,
            AssessmentAuthorizationActions.ActivateCohort,
            command.CohortId,
            AssessmentResourceTypes.Cohort,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            return await PersistIdempotentFailureAsync(command, expectedDigest, AssessmentFailureCodes.Denied, startedAt, cancellationToken);
        }

        if (!string.Equals(expectedDigest, command.TrustedCommandDigest, StringComparison.Ordinal))
        {
            return await PersistAuthorizedIdempotentFailureAsync(
                command,
                expectedDigest,
                AssessmentFailureCodes.IdempotencyConflict,
                startedAt,
                cancellationToken);
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

            var commitAuth = await authorization.ReauthorizeAsync(
                command.Actor,
                AssessmentAuthorizationActions.ActivateCohort,
                command.CohortId,
                AssessmentResourceTypes.Cohort,
                transaction,
                cancellationToken);
            if (!commitAuth.IsPermitted)
            {
                return await PersistRedactedAfterBindingAsync(
                    command,
                    expectedDigest,
                    AssessmentFailureCodes.Denied,
                    transaction,
                    startedAt,
                    cancellationToken);
            }

            var existingSuccess = await attempts.FindSuccessfulAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                command.CohortId,
                command.IdempotencyKey,
                transaction,
                cancellationToken);
            if (existingSuccess is not null)
            {
                return await RecordExistingRequestAsync(
                    command,
                    expectedDigest,
                    existingSuccess,
                    transaction,
                    startedAt,
                    commitAuth,
                    cancellationToken);
            }

            var bindingConflict = await BindOrConflictAsync(
                command,
                expectedDigest,
                transaction,
                startedAt,
                includeAuthoritativeState: true,
                commitAuth,
                cancellationToken);
            if (bindingConflict is not null)
            {
                return bindingConflict;
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
                    startedAt,
                    commitAuth,
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
                return await RecordExistingRequestAsync(
                    command,
                    expectedDigest,
                    existingSuccess,
                    transaction,
                    startedAt,
                    commitAuth,
                    cancellationToken);
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
                    startedAt,
                    commitAuth,
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
                    startedAt,
                    commitAuth,
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
                    startedAt,
                    commitAuth,
                    cancellationToken);
            }

            var occurredAt = _clock.UtcNow;
            var provenance = new ActivationProvenance(
                command.Actor.Actor.ActorId,
                command.Actor.Actor.ActorType,
                command.Actor.CorrelationId,
                occurredAt);
            var document = ActivationBaselineDocument.FromReadyDraft(draft, descriptors, provenance);
            if (!document.Succeeded || document.Value is null)
            {
                return await PersistFailureAsync(command, expectedDigest, document.OutcomeCode, transaction, draft, startedAt, commitAuth, cancellationToken);
            }

            var digest = digester.Digest(document.Value);
            if (!digest.Succeeded || digest.Value is null)
            {
                return await PersistFailureAsync(command, expectedDigest, digest.OutcomeCode, transaction, draft, startedAt, commitAuth, cancellationToken);
            }

            var baselineId = Guid.CreateVersion7();
            var bound = cohort.BindActivation(
                command.ExpectedRevisionId,
                command.ExpectedRevisionNumber,
                baselineId,
                digest.Value);
            if (!bound.Succeeded || bound.Value is null)
            {
                return await PersistFailureAsync(command, expectedDigest, bound.OutcomeCode, transaction, draft, startedAt, commitAuth, cancellationToken);
            }

            if (!transaction.AuditAccepted || !transaction.OutboxAccepted)
            {
                return await PersistFailureAsync(command, expectedDigest, AssessmentFailureCodes.AuditUnavailable, transaction, draft, startedAt, commitAuth, cancellationToken);
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
                return await PersistFailureAsync(command, expectedDigest, AssessmentFailureCodes.StaleRevision, transaction, draft, startedAt, commitAuth, cancellationToken);
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
                cancellationToken,
                commitAuth);
            await store.UpdateCohortAsync(bound.Value, transaction, cancellationToken);

            var attempt = CreateAttempt(
                command,
                expectedDigest,
                AssessmentActivationOutcomes.Activated,
                bound.Value.BaselineId,
                bound.Value.BaselineDigest,
                bound.Value.State,
                draft,
                bound.Value.CohortId,
                startedAt,
                _clock.UtcNow,
                commitAuth);
            await attempts.InsertAsync(attempt, transaction, cancellationToken);
            return FromAttempt(attempt);
        }, cancellationToken);
    }

    public async Task<ActivationOutcome> ReconcileAsync(
        ReconcileActivationQuery query,
        CancellationToken cancellationToken = default)
    {
        if (AssessmentIdempotencyKey.Validate(query.IdempotencyKey) is { } invalidKey)
        {
            return await unitOfWork.ExecuteAsync(
                async transaction =>
                {
                    await attempts.InsertRequestAuditAsync(
                        query.Actor,
                        AssessmentAuthorizationActions.ReconcileActivation,
                        query.CohortId,
                        AssessmentResourceTypes.Cohort,
                        "deny",
                        invalidKey,
                        transaction,
                        cancellationToken);
                    return new ActivationOutcome(false, invalidKey, query.ActivityId, query.CohortId, null, null, CohortStates.Draft);
                },
                cancellationToken);
        }

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

            var commitAuth = await authorization.ReauthorizeAsync(
                query.Actor,
                AssessmentAuthorizationActions.ReconcileActivation,
                query.CohortId,
                AssessmentResourceTypes.Cohort,
                transaction,
                cancellationToken);
            if (!commitAuth.IsPermitted)
            {
                return new ActivationOutcome(false, AssessmentFailureCodes.Denied, query.ActivityId, query.CohortId, null, null, CohortStates.Draft);
            }

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
        string commandDigestValue,
        string code,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(
            transaction => PersistIdempotentFailureAsync(command, commandDigestValue, code, transaction, startedAt, cancellationToken),
            cancellationToken);

    private async Task<ActivationOutcome> PersistIdempotentFailureAsync(
        ActivateCohortCommand command,
        string commandDigestValue,
        string code,
        IAssessmentActivationTransaction transaction,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        await attempts.AcquireIdempotencyLockAsync(
            command.Actor.Organization.OrganizationId,
            command.ActivityId,
            command.CohortId,
            command.IdempotencyKey,
            transaction,
            cancellationToken);

        return await PersistRedactedAfterBindingAsync(
            command,
            commandDigestValue,
            code,
            transaction,
            startedAt,
            cancellationToken);
    }

    private Task<ActivationOutcome> PersistAuthorizedIdempotentFailureAsync(
        ActivateCohortCommand command,
        string commandDigestValue,
        string code,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(
            async transaction =>
            {
                await attempts.AcquireIdempotencyLockAsync(
                    command.Actor.Organization.OrganizationId,
                    command.ActivityId,
                    command.CohortId,
                    command.IdempotencyKey,
                    transaction,
                    cancellationToken);
                var commitAuth = await authorization.ReauthorizeAsync(
                    command.Actor,
                    AssessmentAuthorizationActions.ActivateCohort,
                    command.CohortId,
                    AssessmentResourceTypes.Cohort,
                    transaction,
                    cancellationToken);
                if (!commitAuth.IsPermitted)
                {
                    return await PersistRedactedAfterBindingAsync(
                        command,
                        commandDigestValue,
                        AssessmentFailureCodes.Denied,
                        transaction,
                        startedAt,
                        cancellationToken);
                }

                var bindingConflict = await BindOrConflictAsync(
                    command,
                    commandDigestValue,
                    transaction,
                    startedAt,
                    includeAuthoritativeState: true,
                    commitAuth,
                    cancellationToken);
                if (bindingConflict is not null)
                {
                    return bindingConflict;
                }

                return await PersistFailureAsync(
                    command,
                    commandDigestValue,
                    code,
                    transaction,
                    draft: null,
                    startedAt,
                    commitAuth,
                    cancellationToken);
            },
            cancellationToken);

    private async Task<ActivationOutcome> PersistRedactedAfterBindingAsync(
        ActivateCohortCommand command,
        string commandDigestValue,
        string code,
        IAssessmentActivationTransaction transaction,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        var bindingConflict = await BindOrConflictAsync(
            command,
            commandDigestValue,
            transaction,
            startedAt,
            includeAuthoritativeState: false,
            authorization: null,
            cancellationToken);
        if (bindingConflict is not null)
        {
            return bindingConflict;
        }

        return await PersistRedactedFailureAsync(command, commandDigestValue, code, transaction, startedAt, cancellationToken);
    }

    private async Task<ActivationOutcome?> BindOrConflictAsync(
        ActivateCohortCommand command,
        string commandDigestValue,
        IAssessmentActivationTransaction transaction,
        DateTimeOffset startedAt,
        bool includeAuthoritativeState,
        AuthorizationDecision? authorization,
        CancellationToken cancellationToken)
    {
        var bound = await attempts.BindCommandDigestAsync(
            command.Actor.Organization.OrganizationId,
            command.ActivityId,
            command.CohortId,
            command.IdempotencyKey,
            commandDigestValue,
            transaction,
            cancellationToken);
        if (string.Equals(bound, commandDigestValue, StringComparison.Ordinal))
        {
            return null;
        }

        return includeAuthoritativeState
            ? await PersistFailureAsync(
                command,
                commandDigestValue,
                AssessmentFailureCodes.IdempotencyConflict,
                transaction,
                draft: null,
                startedAt,
                authorization,
                cancellationToken)
            : await PersistRedactedFailureAsync(
                command,
                commandDigestValue,
                AssessmentFailureCodes.IdempotencyConflict,
                transaction,
                startedAt,
                cancellationToken);
    }

    private async Task<ActivationOutcome> PersistRedactedFailureAsync(
        ActivateCohortCommand command,
        string commandDigestValue,
        string code,
        IAssessmentActivationTransaction transaction,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        var attempt = CreateAttempt(
            command,
            commandDigestValue,
            code,
            null,
            null,
            CohortStates.Draft,
            draft: null,
            authoritativeCohortId: null,
            startedAt,
            _clock.UtcNow);
        await attempts.InsertAsync(attempt, transaction, cancellationToken);
        return FromAttempt(attempt);
    }

    private async Task<ActivationOutcome> PersistFailureAsync(
        ActivateCohortCommand command,
        string commandDigestValue,
        string code,
        IAssessmentActivationTransaction transaction,
        ActivityDraft? draft,
        DateTimeOffset startedAt,
        AuthorizationDecision? authorization,
        CancellationToken cancellationToken,
        AssessmentActivationAttempt? existingSuccess = null)
    {
        draft ??= await store.GetDraftAsync(
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
        var attempt = CreateAttempt(
            command,
            commandDigestValue,
            code,
            cohort?.BaselineId ?? existingSuccess?.BaselineId,
            cohort?.BaselineDigest ?? existingSuccess?.BaselineDigest,
            cohort?.State ?? existingSuccess?.CohortState ?? CohortStates.Draft,
            draft,
            cohort?.CohortId ?? existingSuccess?.AuthoritativeCohortId,
            startedAt,
            _clock.UtcNow,
            authorization);
        if (draft is null && existingSuccess is not null)
        {
            attempt = attempt with
            {
                AuthoritativeRevisionId = existingSuccess.AuthoritativeRevisionId,
                AuthoritativeRevisionNumber = existingSuccess.AuthoritativeRevisionNumber,
            };
        }

        await attempts.InsertAsync(attempt, transaction, cancellationToken);
        return FromAttempt(attempt);
    }

    private AssessmentActivationAttempt CreateAttempt(
        ActivateCohortCommand command,
        string digest,
        string outcomeCode,
        Guid? baselineId,
        string? baselineDigest,
        string cohortState,
        ActivityDraft? draft,
        Guid? authoritativeCohortId = null,
        DateTimeOffset? startedAtUtc = null,
        DateTimeOffset? finishedAtUtc = null,
        AuthorizationDecision? authorization = null)
    {
        var started = startedAtUtc ?? _clock.UtcNow;
        return new(
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
            command.Actor.SourceChannel,
            authoritativeCohortId,
            started,
            finishedAtUtc ?? _clock.UtcNow,
            authorization);
    }

    private async Task<ActivationOutcome> RecordExistingRequestAsync(
        ActivateCohortCommand command,
        string expectedDigest,
        AssessmentActivationAttempt existing,
        IAssessmentActivationTransaction transaction,
        DateTimeOffset startedAt,
        AuthorizationDecision authorization,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(existing.CommandDigest, expectedDigest, StringComparison.Ordinal))
        {
            return await PersistFailureAsync(
                command,
                expectedDigest,
                AssessmentFailureCodes.IdempotencyConflict,
                transaction,
                draft: null,
                startedAt,
                authorization,
                cancellationToken,
                existing);
        }

        var attempt = new AssessmentActivationAttempt(
            existing.OrganizationId,
            existing.ActivityId,
            existing.CohortId,
            Guid.CreateVersion7(),
            existing.RequestedRevisionId,
            existing.RequestedRevisionNumber,
            existing.AuthoritativeRevisionId,
            existing.AuthoritativeRevisionNumber,
            existing.IdempotencyKey,
            existing.CommandDigest,
            AssessmentActivationOutcomes.Deduplicated,
            existing.BaselineId,
            existing.BaselineDigest,
            existing.CohortState,
            command.Actor.Actor.ActorId,
            command.Actor.CorrelationId,
            command.Actor.Actor.ActorType,
            command.Actor.SourceChannel,
            existing.AuthoritativeCohortId,
            startedAt,
            _clock.UtcNow,
            authorization);
        await attempts.InsertAsync(attempt, transaction, cancellationToken);
        return FromAttempt(attempt);
    }

    private static ActivationOutcome FromAttempt(AssessmentActivationAttempt attempt) =>
        new(
            string.Equals(attempt.OutcomeCode, AssessmentActivationOutcomes.Activated, StringComparison.Ordinal)
                || string.Equals(attempt.OutcomeCode, AssessmentActivationOutcomes.Deduplicated, StringComparison.Ordinal),
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
