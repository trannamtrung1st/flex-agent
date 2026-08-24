using System.Diagnostics;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class IntakeCoordinator(
    IEnrollmentAuthorizationPort authorization,
    IEnrollmentStore enrollments,
    IActivatedCohortPort cohorts,
    IFrozenSubmissionRequirementPort frozenRequirements,
    IMaterialPolicyPort materialPolicies,
    IIntakeStore intakes,
    ISubmissionVersionStore versions,
    IEnrollmentOperationStore operations,
    IEnrollmentAuditPort audit,
    IEnrollmentUnitOfWork unitOfWork,
    IEnrollmentSessionPort sessions,
    IArtifactSafetyScanner scanner,
    IEnrollmentTimingQueryService timing,
    IEnrollmentClock? clock = null,
    IEnrollmentTelemetry? telemetry = null) : IIntakeCoordinator
{
    private readonly IEnrollmentClock _clock = clock ?? new SystemEnrollmentClock();
    private readonly IEnrollmentTelemetry _telemetry = telemetry ?? NullEnrollmentTelemetry.Instance;

    public Task<IntakeMutationOutcome> BeginAsync(
        BeginIntakeCommand command,
        CancellationToken cancellationToken = default)
    {
        var digest = SubmissionCommandDigest.Compute(
            IntakeOperationKinds.Begin,
            command.Actor.Organization.OrganizationId.ToString("D"),
            command.EnrollmentId.ToString("D"));
        return ExecuteAsync(
            command.Actor,
            SubmissionAuthorizationActions.BeginIntake,
            IntakeOperationKinds.Begin,
            command.EnrollmentId,
            command.IdempotencyKey,
            command.TrustedCommandDigest,
            digest,
            async (transaction, enrollment, binding, effectivePolicy) =>
            {
                var existing = await intakes.FindActiveIntakeAsync(
                    enrollment.OrganizationId,
                    enrollment.EnrollmentId,
                    transaction,
                    cancellationToken);
                if (existing is not null && !IntakeStateMachine.IsTerminal(existing.Status))
                {
                    return Success(existing, command.Actor.GrantedActions);
                }

                var submissionId = await versions.FindSubmissionIdByEnrollmentAsync(
                    enrollment.OrganizationId,
                    enrollment.EnrollmentId,
                    transaction,
                    cancellationToken) ?? Guid.NewGuid();
                var intakeId = Guid.NewGuid();
                var now = _clock.UtcNow;
                var scope = ScopeFrom(enrollment, binding);
                var intake = new SubmissionIntakeRecord(
                    intakeId,
                    submissionId,
                    scope,
                    IntakeStates.Receiving,
                    1,
                    effectivePolicy.EffectiveDigest,
                    effectivePolicy.FrozenRequirement.SourceId,
                    effectivePolicy.FrozenRequirement.VersionId,
                    effectivePolicy.FrozenRequirement.ContentDigest,
                    effectivePolicy.OrganizationPolicy.SourceId,
                    effectivePolicy.OrganizationPolicy.VersionId,
                    effectivePolicy.OrganizationPolicy.ContentDigest,
                    now,
                    now,
                    null,
                    []);

                await intakes.InsertIntakeAsync(intake, command.Actor.Actor.ActorId, transaction, cancellationToken);
                return Success(intake, command.Actor.GrantedActions);
            },
            cancellationToken);
    }

    public Task<IntakeMutationOutcome> FinalizeAsync(
        FinalizeIntakeCommand command,
        CancellationToken cancellationToken = default)
    {
        var digest = SubmissionCommandDigest.Compute(
            IntakeOperationKinds.Finalize,
            command.Actor.Organization.OrganizationId.ToString("D"),
            command.EnrollmentId.ToString("D"),
            command.IntakeId.ToString("D"),
            command.ExpectedRevision.ToString());
        return ExecuteAsync(
            command.Actor,
            SubmissionAuthorizationActions.FinalizeIntake,
            IntakeOperationKinds.Finalize,
            command.EnrollmentId,
            command.IdempotencyKey,
            command.TrustedCommandDigest,
            digest,
            async (transaction, enrollment, binding, effectivePolicy) =>
            {
                var intake = await intakes.FindIntakeAsync(
                    enrollment.OrganizationId,
                    command.EnrollmentId,
                    command.IntakeId,
                    transaction,
                    cancellationToken);
                if (intake is null || !IntakeBelongsToEnrollment(intake, enrollment))
                {
                    return Fail(SubmissionFailureCodes.NotFound);
                }

                if (intake.Revision != command.ExpectedRevision)
                {
                    return Fail(SubmissionFailureCodes.StaleRevision);
                }

                if (intake.Status == IntakeStates.Accepted)
                {
                    return Fail(SubmissionFailureCodes.AlreadyAccepted);
                }

                if (!IntakeStateMachine.CanTransition(intake.Status, IntakeStates.Validating)
                    && intake.Status != IntakeStates.Validating
                    && intake.Status != IntakeStates.Received)
                {
                    return Fail(SubmissionFailureCodes.CancellationRace);
                }

                var timingResult = await timing.GetMyWorkTimingAsync(
                    command.Actor,
                    command.EnrollmentId,
                    cancellationToken);
                var cutoff = timingResult.Succeeded && timingResult.Value?.Timing is EffectiveTiming effective
                    ? effective.EffectiveSubmissionExclusiveEndUtc
                    : binding.DeadlineUtc;
                if (!IntakeStateMachine.ReceiptBeforeCutoff(intake.CompleteReceiptAtUtc, cutoff))
                {
                    return Fail(SubmissionFailureCodes.CutoffPassed);
                }

                foreach (var item in intake.Items)
                {
                    if (item.ArtifactObjectKey is null || item.ArtifactVersionId is null)
                    {
                        return Fail(SubmissionFailureCodes.UploadIncomplete);
                    }

                    var scanResult = await scanner.ScanAsync(
                            new ArtifactScanRequest(
                                enrollment.OrganizationId,
                                new StoredArtifactReference(
                                    new ArtifactObjectKey(item.ArtifactObjectKey),
                                    new ArtifactVersionId(item.ArtifactVersionId),
                                    ArtifactDigest.FromHex(item.ContentDigest),
                                    item.ByteCount),
                                item.Category),
                            cancellationToken);
                    var scan = MaterialContentValidator.EvaluateScanner(
                        effectivePolicy.ScannerMode,
                        scanResult.Succeeded
                            ? ArtifactScanOutcomeMapper.ToDomain(scanResult.Outcome)
                            : null);
                    if (scan != MaterialScanOutcome.Clean)
                    {
                        return Fail(scan switch
                        {
                            MaterialScanOutcome.Rejected => SubmissionFailureCodes.ScannerRejected,
                            _ => SubmissionFailureCodes.ScannerRequiredUnavailable,
                        });
                    }
                }

                var allocation = await versions.AllocateNextVersionAsync(
                    enrollment.OrganizationId,
                    intake.SubmissionId,
                    transaction,
                    cancellationToken);
                var versionId = Guid.NewGuid();
                var acceptedItems = intake.Items.Select(item => new AcceptedVersionItem(
                    item.ItemId,
                    item.Category,
                    item.Filename,
                    item.ByteCount,
                    item.ContentDigest,
                    item.ArtifactObjectKey!,
                    item.ArtifactVersionId!)).ToArray();

                var accepted = new AcceptedSubmissionVersion(
                    intake.SubmissionId,
                    versionId,
                    allocation.VersionNumber,
                    intake.Scope,
                    intake.PolicyDigest,
                    allocation.PredecessorVersionId,
                    _clock.UtcNow,
                    acceptedItems);

                await versions.InsertAcceptedVersionAsync(
                    accepted,
                    command.Actor.Actor.ActorId,
                    transaction,
                    cancellationToken);

                var acceptedIntake = intake with
                {
                    Status = IntakeStates.Accepted,
                    Revision = intake.Revision + 1,
                    UpdatedAtUtc = _clock.UtcNow,
                };
                await intakes.UpdateIntakeAsync(
                    acceptedIntake,
                    command.Actor.Actor.ActorId,
                    transaction,
                    cancellationToken);

                return new IntakeMutationOutcome(
                    true,
                    "accepted",
                    intake.IntakeId,
                    intake.SubmissionId,
                    IntakeStates.Accepted,
                    acceptedIntake.Revision,
                    versionId,
                    allocation.VersionNumber,
                    command.Actor.GrantedActions);
            },
            cancellationToken);
    }

    public Task<IntakeMutationOutcome> CancelAsync(
        CancelIntakeCommand command,
        CancellationToken cancellationToken = default)
    {
        var digest = SubmissionCommandDigest.Compute(
            IntakeOperationKinds.Cancel,
            command.Actor.Organization.OrganizationId.ToString("D"),
            command.EnrollmentId.ToString("D"),
            command.IntakeId.ToString("D"),
            command.ExpectedRevision.ToString());
        return ExecuteAsync(
            command.Actor,
            SubmissionAuthorizationActions.CancelIntake,
            IntakeOperationKinds.Cancel,
            command.EnrollmentId,
            command.IdempotencyKey,
            command.TrustedCommandDigest,
            digest,
            async (transaction, enrollment, binding, effectivePolicy) =>
            {
                var intake = await intakes.FindIntakeAsync(
                    enrollment.OrganizationId,
                    command.EnrollmentId,
                    command.IntakeId,
                    transaction,
                    cancellationToken);
                if (intake is null || !IntakeBelongsToEnrollment(intake, enrollment))
                {
                    return Fail(SubmissionFailureCodes.NotFound);
                }

                if (intake.Revision != command.ExpectedRevision)
                {
                    return Fail(SubmissionFailureCodes.StaleRevision);
                }

                if (IntakeStateMachine.IsTerminal(intake.Status))
                {
                    return Success(intake, command.Actor.GrantedActions);
                }

                var cancelled = intake with
                {
                    Status = IntakeStates.Cancelled,
                    Revision = intake.Revision + 1,
                    UpdatedAtUtc = _clock.UtcNow,
                };
                await intakes.UpdateIntakeAsync(
                    cancelled,
                    command.Actor.Actor.ActorId,
                    transaction,
                    cancellationToken);
                return Success(cancelled, command.Actor.GrantedActions);
            },
            cancellationToken);
    }

    public Task<IntakeMutationOutcome> CompleteItemAsync(
        CompleteIntakeItemCommand command,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CompleteItemAsync is implemented in Postgres-backed intake item receipt path.");

    private async Task<IntakeMutationOutcome> ExecuteAsync(
        EnrollmentActorContext actor,
        string action,
        string operationKind,
        Guid enrollmentId,
        string idempotencyKey,
        string trustedDigest,
        string expectedDigest,
        Func<IEnrollmentTransaction, Enrollment, ActivatedCohortBinding, NormalizedMaterialPolicy, Task<IntakeMutationOutcome>> commit,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            if (EnrollmentIdempotencyKey.Validate(idempotencyKey) is { } invalid)
            {
                return Fail(invalid);
            }

            if (EnrollmentAuthenticationPolicy.Evaluate(actor, EnrollmentAuthorizationActions.Discover) is not null)
            {
                return Fail(SubmissionFailureCodes.Unauthorized);
            }

            if (!string.Equals(expectedDigest, trustedDigest, StringComparison.Ordinal))
            {
                return Fail(SubmissionFailureCodes.IdempotencyConflict);
            }

            var admission = await authorization.AuthorizeAdmissionAsync(
                actor,
                EnrollmentAuthorizationActions.Discover,
                enrollmentId,
                EnrollmentResourceTypes.Assignment,
                cancellationToken);
            if (!admission.IsPermitted)
            {
                return Fail(SubmissionFailureCodes.Unauthorized);
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
                if (!await sessions.ConfirmLiveAsync(actor, transaction, cancellationToken))
                {
                    return Fail(SubmissionFailureCodes.Unauthorized);
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
                        return Fail(SubmissionFailureCodes.IdempotencyConflict);
                    }

                    var replayed = await ReplayStoredOperationAsync(
                        existing,
                        operationKind,
                        enrollmentId,
                        actor,
                        transaction,
                        cancellationToken);
                    if (replayed is not null)
                    {
                        return replayed;
                    }
                }

                var enrollment = await enrollments.FindAsync(
                    actor.Organization.OrganizationId,
                    enrollmentId,
                    transaction,
                    cancellationToken);
                if (enrollment is null || enrollment.ParticipantActorId != actor.Actor.ActorId)
                {
                    return Fail(SubmissionFailureCodes.EnrollmentUnavailable);
                }

                if (!string.Equals(enrollment.Status, EnrollmentStates.Active, StringComparison.Ordinal))
                {
                    return Fail(SubmissionFailureCodes.EnrollmentNotActive);
                }

                var binding = await cohorts.RevalidateAsync(
                    actor.Organization.OrganizationId,
                    enrollment.ActivityId,
                    enrollment.CohortId,
                    transaction,
                    cancellationToken);
                if (binding is null)
                {
                    return Fail(SubmissionFailureCodes.PolicyUnavailable);
                }

                var frozen = await frozenRequirements.ResolveFrozenAsync(
                    actor.Organization.OrganizationId,
                    binding.TaskSourceId,
                    binding.TaskVersionId,
                    binding.TaskContentDigest,
                    transaction,
                    cancellationToken);
                var organization = await materialPolicies.ResolveCurrentAsync(
                    actor.Organization.OrganizationId,
                    new PolicySourceRef(
                        binding.FrozenPolicySourceId,
                        binding.FrozenPolicyVersionId,
                        binding.FrozenPolicyDigest),
                    _clock.UtcNow,
                    transaction,
                    cancellationToken);
                var effective = MaterialPolicyResolver.Intersect(frozen, organization);
                if (effective is null)
                {
                    return Fail(SubmissionFailureCodes.PolicyUnavailable);
                }

                var reauthorized = await authorization.ReauthorizeAsync(
                    actor,
                    EnrollmentAuthorizationActions.Discover,
                    enrollmentId,
                    EnrollmentResourceTypes.Assignment,
                    transaction,
                    cancellationToken);
                if (!reauthorized.IsPermitted)
                {
                    return Fail(SubmissionFailureCodes.Unauthorized);
                }

                var committed = await commit(transaction, enrollment, binding, effective);
                await operations.InsertAsync(
                    new EnrollmentOperation(
                        actor.Organization.OrganizationId,
                        actor.Actor.ActorId,
                        operationKind,
                        enrollmentId,
                        idempotencyKey,
                        expectedDigest,
                        committed.OutcomeCode,
                        ResolveOperationResourceId(operationKind, committed),
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
                    return Fail(SubmissionFailureCodes.AuditUnavailable);
                }

                return committed;
            }, cancellationToken);

            _telemetry.RecordMutation(operationKind, outcome.Succeeded ? "success" : "failure", Stopwatch.GetElapsedTime(started));
            return outcome;
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, SubmissionFailureCodes.StaleRevision, StringComparison.Ordinal))
        {
            _telemetry.RecordMutation(operationKind, "failure", Stopwatch.GetElapsedTime(started));
            return Fail(SubmissionFailureCodes.StaleRevision);
        }
        catch
        {
            _telemetry.RecordMutation(operationKind, "failure", Stopwatch.GetElapsedTime(started));
            throw;
        }
    }

    private async Task<IntakeMutationOutcome?> ReplayStoredOperationAsync(
        EnrollmentOperation existing,
        string operationKind,
        Guid enrollmentId,
        EnrollmentActorContext actor,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (existing.EnrollmentId is not Guid relatedId)
        {
            return ReplayFromStoredOutcome(existing, actor.GrantedActions);
        }

        if (operationKind == IntakeOperationKinds.Finalize && IsSuccessfulIntakeOutcome(existing.OutcomeCode))
        {
            var version = await versions.FindVersionAsync(
                actor.Organization.OrganizationId,
                relatedId,
                transaction,
                cancellationToken);
            if (version is null || version.Scope.EnrollmentId != enrollmentId)
            {
                return ReplayFromStoredOutcome(existing, actor.GrantedActions);
            }

            return new IntakeMutationOutcome(
                true,
                existing.OutcomeCode,
                null,
                version.SubmissionId,
                IntakeStates.Accepted,
                null,
                version.VersionId,
                version.VersionNumber,
                actor.GrantedActions);
        }

        var replayIntake = await intakes.FindIntakeAsync(
            actor.Organization.OrganizationId,
            enrollmentId,
            relatedId,
            transaction,
            cancellationToken);
        if (replayIntake is not null)
        {
            return IsSuccessfulIntakeOutcome(existing.OutcomeCode)
                ? Success(replayIntake, actor.GrantedActions)
                : Fail(existing.OutcomeCode);
        }

        return ReplayFromStoredOutcome(existing, actor.GrantedActions);
    }

    private static Guid? ResolveOperationResourceId(string operationKind, IntakeMutationOutcome committed) =>
        operationKind == IntakeOperationKinds.Finalize
            ? committed.VersionId ?? committed.IntakeId
            : committed.IntakeId ?? committed.VersionId;

    private static bool IntakeBelongsToEnrollment(SubmissionIntakeRecord intake, Enrollment enrollment) =>
        intake.Scope.EnrollmentId == enrollment.EnrollmentId
        && intake.Scope.ParticipantActorId == enrollment.ParticipantActorId;

    private static IntakeMutationOutcome ReplayFromStoredOutcome(
        EnrollmentOperation existing,
        IReadOnlyList<string> actions) =>
        new(
            IsSuccessfulIntakeOutcome(existing.OutcomeCode),
            existing.OutcomeCode,
            existing.EnrollmentId,
            null,
            IsSuccessfulIntakeOutcome(existing.OutcomeCode) ? existing.OutcomeCode : null,
            null,
            null,
            null,
            actions);

    private static bool IsSuccessfulIntakeOutcome(string outcomeCode) =>
        outcomeCode is "accepted"
            or IntakeStates.Receiving
            or IntakeStates.Received
            or IntakeStates.Validating
            or IntakeStates.Cancelling
            or IntakeStates.Reconciling
            or IntakeStates.Accepted
            or IntakeStates.Cancelled;

    private static SubmissionParentScope ScopeFrom(Enrollment enrollment, ActivatedCohortBinding binding) =>
        new(
            enrollment.OrganizationId,
            enrollment.ActivityId,
            enrollment.CohortId,
            enrollment.BaselineId,
            enrollment.EnrollmentId,
            enrollment.ParticipantActorId,
            binding.TaskSourceId,
            binding.TaskVersionId,
            binding.TaskContentDigest);

    private static IntakeMutationOutcome Success(SubmissionIntakeRecord intake, IReadOnlyList<string> actions) =>
        new(
            true,
            intake.Status,
            intake.IntakeId,
            intake.SubmissionId,
            intake.Status,
            intake.Revision,
            null,
            null,
            actions);

    private static IntakeMutationOutcome Fail(string code) =>
        new(false, code, null, null, null, null, null, null, []);
}
