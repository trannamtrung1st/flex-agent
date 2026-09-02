using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class AttemptStartCoordinator(
    IEnrollmentAuthorizationPort authorization,
    IEnrollmentStore enrollments,
    IActivatedCohortPort cohorts,
    IEnrollmentTimingQueryService timing,
    ISubmissionVersionStore versions,
    IExactAcceptedVersionReader exactVersions,
    IAttemptStore attempts,
    IStartOperationStore startOperations,
    IRetryEntitlementReader retryEntitlements,
    IParticipantNoticePort noticePort,
    IAcknowledgmentLifecyclePort acknowledgments,
    ISessionStartCommitPort sessionStarts,
    IEnrollmentAuditPort audit,
    IEnrollmentUnitOfWork unitOfWork,
    IEnrollmentSessionPort sessions,
    IEnrollmentClock? clock = null) : IAttemptStartCoordinator, IAttemptReadinessQuery
{
    private readonly IEnrollmentClock _clock = clock ?? new SystemEnrollmentClock();

    internal static readonly AsyncLocal<Func<Task>?> AfterStartTransactionBeforeFailedPersist = new();

    public async Task<QueryResult<AttemptReadinessProjection>> GetAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfUnauthorizedAsync(actor, enrollmentId, cancellationToken);
        if (denied is not null)
        {
            return new QueryResult<AttemptReadinessProjection>(false, null, denied);
        }

        var enrollment = await enrollments.FindAsync(
            actor.Organization.OrganizationId,
            enrollmentId,
            null,
            cancellationToken);
        if (enrollment is null || enrollment.ParticipantActorId != actor.Actor.ActorId)
        {
            return new QueryResult<AttemptReadinessProjection>(false, null, AttemptFailureCodes.Denied);
        }

        var snapshot = await LoadSnapshotAsync(actor, enrollment, null, cancellationToken);
        var readiness = AttemptEligibility.Evaluate(snapshot.Facts);
        return new QueryResult<AttemptReadinessProjection>(
            true,
            ToProjection(enrollment, snapshot, readiness),
            "attempt.ok");
    }

    public Task<StartAttemptOutcome> ReconcileAsync(
        StartAttemptCommand command,
        CancellationToken cancellationToken = default) =>
        StartAsync(command, cancellationToken);

    public async Task<StartAttemptOutcome> StartAsync(
        StartAttemptCommand command,
        CancellationToken cancellationToken = default)
    {
        if (EnrollmentIdempotencyKey.Validate(command.IdempotencyKey) is { } invalid)
        {
            return Fail(invalid);
        }

        if (EnrollmentAuthenticationPolicy.Evaluate(command.Actor, EnrollmentAuthorizationActions.Discover) is not null)
        {
            return Fail(AttemptFailureCodes.Denied);
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            command.Actor,
            EnrollmentAuthorizationActions.Discover,
            command.EnrollmentId,
            EnrollmentResourceTypes.Assignment,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            return Fail(AttemptFailureCodes.Denied);
        }

        try
        {
            StartOperation? failedAfterAbort = null;
            var outcome = await unitOfWork.ExecuteAsync(command.Actor, async transaction =>
            {
                if (!await sessions.RevalidateLiveAsync(command.Actor, transaction, cancellationToken))
                {
                    return Fail(AttemptFailureCodes.Denied);
                }

                var reauth = await authorization.ReauthorizeAsync(
                    command.Actor,
                    EnrollmentAuthorizationActions.Discover,
                    command.EnrollmentId,
                    EnrollmentResourceTypes.Assignment,
                    transaction,
                    cancellationToken);
                if (!reauth.IsPermitted)
                {
                    return Fail(AttemptFailureCodes.Denied);
                }

                var enrollment = await enrollments.FindAsync(
                    command.Actor.Organization.OrganizationId,
                    command.EnrollmentId,
                    transaction,
                    cancellationToken);
                if (enrollment is null || enrollment.ParticipantActorId != command.Actor.Actor.ActorId)
                {
                    return Fail(AttemptFailureCodes.Denied);
                }

                await startOperations.AcquireLockAsync(
                    enrollment.OrganizationId,
                    enrollment.EnrollmentId,
                    command.IdempotencyKey,
                    transaction,
                    cancellationToken);

                var existing = await startOperations.FindAsync(
                    enrollment.OrganizationId,
                    enrollment.EnrollmentId,
                    command.IdempotencyKey,
                    transaction,
                    cancellationToken);
                if (existing is not null)
                {
                    var replayed = StartOperationPolicy.Claim(
                        enrollment.OrganizationId,
                        enrollment.ParticipantActorId,
                        enrollment.EnrollmentId,
                        command.IdempotencyKey,
                        command.TrustedCommandDigest,
                        command.Actor.ApplicationSessionId,
                        _clock.UtcNow,
                        existing);
                    if (!replayed.Succeeded)
                    {
                        return Fail(replayed.OutcomeCode);
                    }

                    if (replayed.Value!.Status == StartOperationStates.Committed)
                    {
                        var replayHistory = await attempts.ListForEnrollmentAsync(
                            enrollment.OrganizationId,
                            enrollment.EnrollmentId,
                            transaction,
                            cancellationToken);
                        var replayReadiness = AttemptEligibility.Evaluate(
                            (await LoadSnapshotAsync(command.Actor, enrollment, transaction, cancellationToken)).Facts);
                        return Reconciled(replayed.Value, replayHistory, replayReadiness);
                    }

                    if (replayed.Value.Status == StartOperationStates.Failed)
                    {
                        return Fail(replayed.Value.OutcomeCode ?? AttemptFailureCodes.Ineligible);
                    }

                    existing = replayed.Value;
                }

                var snapshot = await LoadSnapshotAsync(command.Actor, enrollment, transaction, cancellationToken);
                var readiness = AttemptEligibility.Evaluate(snapshot.Facts);
                var digest = AttemptCommandDigest.Compute(
                    enrollment.OrganizationId,
                    enrollment.EnrollmentId,
                    enrollment.ParticipantActorId,
                    readiness.NextOrdinal,
                    readiness.EntitlementSource,
                    snapshot.AcceptedVersionIds,
                    snapshot.Notices.Select(notice => notice.SourceVersionId).ToArray());
                if (!string.Equals(digest, command.TrustedCommandDigest, StringComparison.Ordinal))
                {
                    return Fail(AttemptFailureCodes.IdempotencyConflict, readiness.State);
                }

                var claimed = StartOperationPolicy.Claim(
                    enrollment.OrganizationId,
                    enrollment.ParticipantActorId,
                    enrollment.EnrollmentId,
                    command.IdempotencyKey,
                    digest,
                    command.Actor.ApplicationSessionId,
                    _clock.UtcNow,
                    existing);
                if (!claimed.Succeeded)
                {
                    return Fail(claimed.OutcomeCode, readiness.State);
                }

                if (claimed.Value!.Status == StartOperationStates.Committed)
                {
                    return Reconciled(claimed.Value, snapshot.History, readiness);
                }

                if (claimed.Value.Status == StartOperationStates.Failed)
                {
                    return Fail(claimed.Value.OutcomeCode ?? AttemptFailureCodes.Ineligible, readiness.State);
                }

                var liveClaims = await startOperations.ListForEnrollmentAsync(
                    enrollment.OrganizationId,
                    enrollment.EnrollmentId,
                    transaction,
                    cancellationToken);
                if (StartOperationPolicy.HasActiveConflict(liveClaims, command.IdempotencyKey, _clock.UtcNow))
                {
                    return Fail(AttemptFailureCodes.ActiveConflict, AttemptReadinessStates.ActiveConflict);
                }

                await startOperations.UpsertAsync(claimed.Value, transaction, cancellationToken);

                if (readiness.State != AttemptReadinessStates.Eligible)
                {
                    var blocked = StartOperationPolicy.Fail(claimed.Value, AttemptFailureCodes.Ineligible, _clock.UtcNow);
                    if (blocked.Succeeded)
                    {
                        await startOperations.UpsertAsync(blocked.Value!, transaction, cancellationToken);
                    }

                    return Fail(AttemptFailureCodes.Ineligible, readiness.State);
                }

                var ackSelection = await SelectBindableAcknowledgmentsAsync(
                    command.Actor,
                    enrollment,
                    snapshot.Notices,
                    transaction,
                    cancellationToken);
                if (ackSelection.Error is not null)
                {
                    var blocked = StartOperationPolicy.Fail(claimed.Value, ackSelection.Error, _clock.UtcNow).Value!;
                    await startOperations.UpsertAsync(blocked, transaction, cancellationToken);
                    return Fail(ackSelection.Error, readiness.State);
                }

                var bindings = new List<AttemptSubmissionBinding>();
                for (var index = 0; index < snapshot.AcceptedVersionIds.Count; index++)
                {
                    var versionId = snapshot.AcceptedVersionIds[index];
                    var exact = await exactVersions.GetExactAsync(
                        ScopeFrom(enrollment),
                        versionId,
                        transaction.CommitHandle,
                        cancellationToken);
                    if (exact is null)
                    {
                        var blocked = StartOperationPolicy.Fail(
                            claimed.Value,
                            AttemptFailureCodes.Ineligible,
                            _clock.UtcNow).Value!;
                        await startOperations.UpsertAsync(blocked, transaction, cancellationToken);
                        return Fail(AttemptFailureCodes.Ineligible, AttemptReadinessStates.MissingAcceptedMaterial);
                    }

                    bindings.Add(new AttemptSubmissionBinding(
                        exact.VersionId,
                        exact.VersionNumber,
                        index + 1,
                        AttemptSubmissionProvenance.ForAcceptedVersion(exact)));
                }

                var attemptId = Guid.CreateVersion7();
                var sessionId = Guid.CreateVersion7();
                var configurationId = Guid.CreateVersion7();
                var manifestId = Guid.CreateVersion7();
                var now = _clock.UtcNow;
                var currentAcks = ackSelection.Bindable;
                var bindError = await acknowledgments.BindToAttemptAsync(
                    currentAcks,
                    attemptId,
                    enrollment.EnrollmentId,
                    enrollment.ParticipantActorId,
                    transaction.CommitHandle,
                    cancellationToken);
                if (bindError is not null)
                {
                    var blocked = StartOperationPolicy.Fail(claimed.Value, bindError, now).Value!;
                    await startOperations.UpsertAsync(blocked, transaction, cancellationToken);
                    return Fail(bindError, readiness.State);
                }

                var sessionCommit = await sessionStarts.CommitActiveAsync(
                    new SessionStartCommitRequest(
                        attemptId,
                        sessionId,
                        configurationId,
                        manifestId,
                        ScopeFrom(enrollment),
                        bindings,
                        now),
                    transaction.CommitHandle,
                    cancellationToken);
                if (!sessionCommit.Succeeded
                    || string.IsNullOrWhiteSpace(sessionCommit.ConfigurationDigest)
                    || string.IsNullOrWhiteSpace(sessionCommit.ManifestDigest))
                {
                    transaction.AbortCommit();
                    failedAfterAbort = StartOperationPolicy.Fail(
                        claimed.Value,
                        sessionCommit.OutcomeCode,
                        now).Value!;
                    return Fail(AttemptFailureCodes.Unavailable, AttemptReadinessStates.ConfigurationUnavailable);
                }

                var activated = Attempt.Activate(
                    attemptId,
                    ScopeFrom(enrollment),
                    readiness.NextOrdinal,
                    readiness.EntitlementSource,
                    null,
                    now,
                    now,
                    new AttemptBinding(
                        sessionId,
                        configurationId,
                        manifestId,
                        sessionCommit.ConfigurationDigest,
                        sessionCommit.ManifestDigest),
                    bindings);
                if (!activated.Succeeded)
                {
                    throw new EnrollmentStartInvariantException(
                        StartOperationPolicy.Fail(claimed.Value, activated.OutcomeCode, now).Value!);
                }

                await attempts.InsertAsync(activated.Value!, transaction, cancellationToken);
                var committed = StartOperationPolicy.Commit(claimed.Value, attemptId, sessionId, now).Value!;
                await startOperations.UpsertAsync(committed, transaction, cancellationToken);
                await audit.WriteRequiredDurableAsync(
                    command.Actor,
                    AttemptAuthorizationActions.Start,
                    attemptId,
                    "attempt",
                    AttemptOutcomes.Activated,
                    null,
                    reauth,
                    transaction,
                    cancellationToken);
                if (!transaction.AuditAccepted || !transaction.OutboxAccepted)
                {
                    throw new EnrollmentAuditUnavailableException();
                }

                var remaining = Math.Max(0, readiness.RemainingEntitlement - 1);
                return new StartAttemptOutcome(
                    true,
                    AttemptOutcomes.Activated,
                    AttemptReadinessStates.ActiveConflict,
                    attemptId,
                    activated.Value!.Ordinal,
                    sessionId,
                    remaining,
                    [AttemptClientActions.ContinueAttempt, AttemptClientActions.ReturnToMyWork]);
            },
            cancellationToken);
            if (failedAfterAbort is not null)
            {
                await PersistFailedStartAsync(command.Actor, failedAfterAbort, cancellationToken);
            }

            return outcome;
        }
        catch (EnrollmentStartInvariantException exception)
        {
            await PersistFailedStartAsync(command.Actor, exception.FailedOperation, cancellationToken);
            return Fail(exception.FailedOperation.OutcomeCode ?? AttemptFailureCodes.Unavailable);
        }
        catch (EnrollmentAuditUnavailableException)
        {
            return Fail(AttemptFailureCodes.AuditUnavailable);
        }
        catch (EnrollmentSessionExpiredException)
        {
            return Fail(AttemptFailureCodes.Denied);
        }
    }

    private async Task PersistFailedStartAsync(
        EnrollmentActorContext actor,
        StartOperation failed,
        CancellationToken cancellationToken)
    {
        if (AfterStartTransactionBeforeFailedPersist.Value is { } delay)
        {
            await delay();
        }

        await unitOfWork.ExecuteAsync(
            actor,
            async transaction =>
            {
                await startOperations.AcquireLockAsync(
                    failed.OrganizationId,
                    failed.EnrollmentId,
                    failed.IdempotencyKey,
                    transaction,
                    cancellationToken);
                var current = await startOperations.FindAsync(
                    failed.OrganizationId,
                    failed.EnrollmentId,
                    failed.IdempotencyKey,
                    transaction,
                    cancellationToken);
                if (current is not null
                    && (current.Status == StartOperationStates.Committed
                        || current.Status == StartOperationStates.Failed))
                {
                    return true;
                }

                await startOperations.UpsertAsync(failed, transaction, cancellationToken);
                return true;
            },
            cancellationToken);
    }

    private async Task<(string? Error, IReadOnlyList<CurrentAcknowledgmentFact> Bindable)> SelectBindableAcknowledgmentsAsync(
        EnrollmentActorContext actor,
        Enrollment enrollment,
        IReadOnlyList<RequiredNoticeProjection> required,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (required.Count == 0)
        {
            return (null, []);
        }

        var current = AcknowledgmentSelection.CurrentBindable(
            await acknowledgments.ListCurrentAsync(
                enrollment.OrganizationId,
                enrollment.EnrollmentId,
                enrollment.ParticipantActorId,
                required,
                transaction.CommitHandle,
                cancellationToken),
            required);
        foreach (var notice in required)
        {
            if (notice.RequiredOutcome != "affirmed")
            {
                continue;
            }

            var match = current.FirstOrDefault(item =>
                item.NoticeId == notice.NoticeId && item.SourceVersionId == notice.SourceVersionId);
            if (match is null
                || match.EnrollmentId != enrollment.EnrollmentId
                || match.ParticipantActorId != actor.Actor.ActorId)
            {
                return (AttemptFailureCodes.AcknowledgmentInvalid, []);
            }
        }

        return (null, current);
    }

    private async Task<string?> DenyIfUnauthorizedAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        CancellationToken cancellationToken)
    {
        if (EnrollmentAuthenticationPolicy.Evaluate(actor, EnrollmentAuthorizationActions.Discover) is not null)
        {
            return AttemptFailureCodes.Denied;
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            actor,
            EnrollmentAuthorizationActions.Discover,
            enrollmentId,
            EnrollmentResourceTypes.Assignment,
            cancellationToken);
        return admission.IsPermitted ? null : AttemptFailureCodes.Denied;
    }

    private async Task<AttemptSnapshot> LoadSnapshotAsync(
        EnrollmentActorContext actor,
        Enrollment enrollment,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var binding = await cohorts.FindActivatedAsync(
            enrollment.OrganizationId,
            enrollment.ActivityId,
            enrollment.CohortId,
            cancellationToken);
        var history = await attempts.ListForEnrollmentAsync(
            enrollment.OrganizationId,
            enrollment.EnrollmentId,
            transaction,
            cancellationToken);
        var retries = await retryEntitlements.ListUnusedAsync(
            enrollment.OrganizationId,
            enrollment.EnrollmentId,
            _clock.UtcNow,
            transaction,
            cancellationToken);
        IReadOnlyList<AcceptedVersionSummary> versionHistory = [];
        var submissionId = await versions.FindSubmissionIdByEnrollmentAsync(
            enrollment.OrganizationId,
            enrollment.EnrollmentId,
            transaction,
            cancellationToken);
        if (submissionId is Guid id)
        {
            versionHistory = [.. await versions.ListVersionsAsync(
                enrollment.OrganizationId,
                id,
                transaction,
                cancellationToken)];
        }

        var requiredMaterial = versionHistory.Count > 0;
        var requiredNotices = await noticePort.ListRequiredAsync(
            enrollment.OrganizationId,
            enrollment.ActivityId,
            enrollment.CohortId,
            enrollment.BaselineId,
            transaction,
            cancellationToken);
        var timingResult = await timing.GetMyWorkTimingAsync(actor, enrollment.EnrollmentId, cancellationToken);
        var timingState = timingResult.Succeeded && timingResult.Value?.Timing is { } effective
            ? effective.EligibilityState
            : TimingEligibilityStates.Unavailable;
        var configurationReady = binding is not null
            && !binding.VerificationDegraded
            && requiredNotices is not null
            && sessionStarts.CanCommit;
        var facts = new AttemptReadinessFacts(
            enrollment.Status,
            timingState,
            binding?.AttemptLimit ?? 1,
            history,
            retries,
            requiredMaterial,
            AgentInspectionRequired: true,
            requiredMaterial,
            configurationReady,
            requiredNotices is not null,
            _clock.UtcNow);
        return new AttemptSnapshot(
            binding,
            history,
            versionHistory,
            versionHistory.Select(item => item.VersionId).ToArray(),
            requiredNotices ?? [],
            facts);
    }

    private static AttemptReadinessProjection ToProjection(
        Enrollment enrollment,
        AttemptSnapshot snapshot,
        AttemptReadiness readiness) =>
        new(
            enrollment.EnrollmentId,
            readiness.State,
            readiness.NextOrdinal,
            readiness.RemainingEntitlement,
            readiness.EntitlementSource,
            snapshot.Binding?.AttemptLimit ?? 1,
            readiness.ActiveAttemptId,
            readiness.ActiveSessionId,
            AttemptCommandDigest.Compute(
                enrollment.OrganizationId,
                enrollment.EnrollmentId,
                enrollment.ParticipantActorId,
                readiness.NextOrdinal,
                readiness.EntitlementSource,
                snapshot.AcceptedVersionIds,
                snapshot.Notices.Select(notice => notice.SourceVersionId).ToArray()),
            snapshot.VersionHistory,
            snapshot.History.Select(item => new AttemptHistoryItem(
                item.AttemptId,
                item.Ordinal,
                item.Status,
                item.Consumed,
                item.Binding.SessionId,
                item.StartedAtUtc,
                item.TerminalAtUtc,
                item.TerminalReasonCategory)).ToArray(),
            snapshot.Notices,
            readiness.PermittedActions);

    private static StartAttemptOutcome Reconciled(
        StartOperation operation,
        IReadOnlyList<Attempt> history,
        AttemptReadiness readiness) =>
        new(
            true,
            AttemptOutcomes.Reconciled,
            AttemptReadinessStates.ActiveConflict,
            operation.AttemptId,
            history.FirstOrDefault(item => item.AttemptId == operation.AttemptId)?.Ordinal,
            operation.SessionId,
            Math.Max(0, readiness.RemainingEntitlement),
            [AttemptClientActions.ContinueAttempt, AttemptClientActions.ReturnToMyWork]);

    private static StartAttemptOutcome Fail(string outcome, string? readiness = null) =>
        new(false, outcome, readiness, null, null, null, 0, [AttemptClientActions.ReturnToMyWork]);

    private static SubmissionParentScope ScopeFrom(Enrollment enrollment) =>
        new(
            enrollment.OrganizationId,
            enrollment.ActivityId,
            enrollment.CohortId,
            enrollment.BaselineId,
            enrollment.EnrollmentId,
            enrollment.ParticipantActorId,
            enrollment.TaskSourceId,
            enrollment.TaskVersionId,
            enrollment.TaskContentDigest);

    private sealed record AttemptSnapshot(
        ActivatedCohortBinding? Binding,
        IReadOnlyList<Attempt> History,
        IReadOnlyList<AcceptedVersionSummary> VersionHistory,
        IReadOnlyList<Guid> AcceptedVersionIds,
        IReadOnlyList<RequiredNoticeProjection> Notices,
        AttemptReadinessFacts Facts);
}
