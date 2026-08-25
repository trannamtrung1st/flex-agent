using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class SubmissionCleanupProcessor(
    ISubmissionWorkStore work,
    IIntakeStore intakes,
    IArtifactStore artifacts,
    ISubmissionVersionStore versions,
    ISubmissionLifecycleHoldStore holds,
    IArtifactDispositionStore dispositions,
    IEnrollmentClock? clock = null,
    IActivityClosurePort? closures = null,
    IAcceptedPayloadLifecyclePolicyPort? acceptedPayloadPolicy = null,
    IAcceptedCleanupScanStore? acceptedScan = null) : ISubmissionCleanupProcessor
{
    private const int CandidatePageSize = 20;

    private readonly IEnrollmentClock _clock = clock ?? new SystemEnrollmentClock();
    private readonly IAcceptedPayloadLifecyclePolicyPort _acceptedPayloadPolicy =
        acceptedPayloadPolicy ?? new ApprovedDefaultAcceptedPayloadLifecyclePolicyPort();
    private readonly IAcceptedCleanupScanStore _acceptedScan =
        acceptedScan ?? new InMemoryAcceptedCleanupScanStore();

    public async Task<string> TryProcessNextAsync(CancellationToken cancellationToken = default)
    {
        await EnqueueEligibleAsync(
            await intakes.ListIncompleteCreatedBeforeAsync(
                _clock.UtcNow - SubmissionLifecycleClocks.IncompleteRetention,
                CandidatePageSize,
                cancellationToken),
            SubmissionWorkKinds.CleanupIncomplete,
            cancellationToken);
        await EnqueueEligibleAsync(
            await intakes.ListRejectedUpdatedBeforeAsync(
                _clock.UtcNow - SubmissionLifecycleClocks.RejectedByteRetention,
                CandidatePageSize,
                cancellationToken),
            SubmissionWorkKinds.CleanupRejected,
            cancellationToken);
        await EnqueueAcceptedAsync(cancellationToken);

        var claimed = await work.ClaimNextAsync(_clock.UtcNow, SubmissionLifecycleClocks.WorkLease, cancellationToken);
        if (claimed is null)
        {
            return "idle";
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(claimed.ArtifactObjectKey))
            {
                var held = await holds.IsHeldAsync(
                    claimed.OrganizationId,
                    claimed.ArtifactObjectKey,
                    cancellationToken);
                var accepted = await versions.HasAcceptedArtifactKeyAsync(
                    claimed.OrganizationId,
                    claimed.ArtifactObjectKey,
                    cancellationToken);
                var mayDelete = claimed.WorkKind == SubmissionWorkKinds.CleanupAccepted
                    ? SubmissionLifecycle.MayDeleteAcceptedPayload(held)
                    : SubmissionLifecycle.MayDeleteArtifact(accepted, held);
                if (!mayDelete)
                {
                    await work.CompleteAsync(claimed.OrganizationId, claimed.WorkId, cancellationToken);
                    return "skipped";
                }

                if (string.IsNullOrWhiteSpace(claimed.ArtifactVersionId))
                {
                    await work.MarkTerminalFailureAsync(
                        claimed.OrganizationId,
                        claimed.WorkId,
                        SubmissionWorkFailureReasons.ExactArtifactVersionUnavailable,
                        cancellationToken);
                    return "failed";
                }

                var deleted = await artifacts.DeleteAsync(
                    claimed.OrganizationId,
                    new StoredArtifactReference(
                        new ArtifactObjectKey(claimed.ArtifactObjectKey),
                        new ArtifactVersionId(claimed.ArtifactVersionId),
                        ArtifactDigest.FromHex(new string('0', 64)),
                        0),
                    cancellationToken);
                if (!deleted)
                {
                    await work.FailAsync(
                        claimed.OrganizationId,
                        claimed.WorkId,
                        _clock.UtcNow.Add(SubmissionLifecycleClocks.WorkLease),
                        cancellationToken);
                    return "failed";
                }

                await dispositions.RecordAsync(
                    claimed.OrganizationId,
                    Guid.CreateVersion7(),
                    claimed.WorkKind,
                    claimed.ArtifactObjectKey,
                    _clock.UtcNow,
                    cancellationToken);
            }

            await work.CompleteAsync(claimed.OrganizationId, claimed.WorkId, cancellationToken);
            return "completed";
        }
        catch
        {
            await work.FailAsync(
                claimed.OrganizationId,
                claimed.WorkId,
                _clock.UtcNow.Add(SubmissionLifecycleClocks.WorkLease),
                cancellationToken);
            return "failed";
        }
    }

    private async Task EnqueueAcceptedAsync(CancellationToken cancellationToken)
    {
        if (closures is null)
        {
            return;
        }

        var snapshot = await _acceptedScan.GetSnapshotAsync(cancellationToken);
        var candidates = await versions.ListAcceptedArtifactCandidatesAsync(
            CandidatePageSize,
            snapshot.Cursor,
            cancellationToken);
        if (candidates.Count == 0 && snapshot.Cursor is not null)
        {
            await _acceptedScan.TryAdvanceAsync(snapshot.Generation, null, cancellationToken);
            snapshot = await _acceptedScan.GetSnapshotAsync(cancellationToken);
            candidates = await versions.ListAcceptedArtifactCandidatesAsync(
                CandidatePageSize,
                snapshot.Cursor,
                cancellationToken);
        }

        if (candidates.Count == 0)
        {
            await _acceptedScan.TryAdvanceAsync(snapshot.Generation, null, cancellationToken);
            return;
        }

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.ArtifactVersionId))
            {
                continue;
            }

            var policy = await _acceptedPayloadPolicy.ResolveAcceptedPayloadPolicyAsync(
                candidate.OrganizationId,
                cancellationToken);
            var held = await holds.IsHeldAsync(candidate.OrganizationId, candidate.ArtifactObjectKey, cancellationToken);
            var alreadyDisposed = await dispositions.ExistsAsync(
                candidate.OrganizationId,
                candidate.ArtifactObjectKey,
                cancellationToken);
            var closedAt = await closures.FindClosedAtUtcAsync(
                candidate.OrganizationId,
                candidate.ActivityId,
                cancellationToken);
            if (alreadyDisposed
                || !SubmissionLifecycle.AcceptedPayloadEligibleForCleanup(
                    closedAt,
                    _clock.UtcNow,
                    held,
                    policy.RetentionAfterActivityClosure))
            {
                continue;
            }

            await work.EnqueueAsync(
                new SubmissionWorkItem(
                    candidate.OrganizationId,
                    Guid.CreateVersion7(),
                    SubmissionWorkKinds.CleanupAccepted,
                    candidate.EnrollmentId,
                    null,
                    candidate.VersionId,
                    SubmissionWorkStates.Pending,
                    0,
                    _clock.UtcNow,
                    null,
                    candidate.ArtifactObjectKey,
                    candidate.ArtifactVersionId),
                new CleanupEnqueueTransaction(),
                cancellationToken);
        }

        var last = candidates[^1];
        await _acceptedScan.TryAdvanceAsync(
            snapshot.Generation,
            candidates.Count < CandidatePageSize
                ? null
                : new AcceptedArtifactCleanupCursor(last.AcceptedAtUtc, last.VersionId, last.ItemId),
            cancellationToken);
    }

    private async Task EnqueueEligibleAsync(
        IReadOnlyList<SubmissionIntakeRecord> intakes,
        string workKind,
        CancellationToken cancellationToken)
    {
        foreach (var intake in intakes)
        {
            foreach (var item in intake.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ArtifactObjectKey)
                    || string.IsNullOrWhiteSpace(item.ArtifactVersionId))
                {
                    continue;
                }

                var accepted = await versions.HasAcceptedArtifactKeyAsync(
                    intake.Scope.OrganizationId,
                    item.ArtifactObjectKey,
                    cancellationToken);
                var held = await holds.IsHeldAsync(intake.Scope.OrganizationId, item.ArtifactObjectKey, cancellationToken);
                var alreadyDisposed = await dispositions.ExistsAsync(
                    intake.Scope.OrganizationId,
                    item.ArtifactObjectKey,
                    cancellationToken);
                if (alreadyDisposed || !SubmissionLifecycle.MayDeleteArtifact(accepted, held))
                {
                    continue;
                }

                await work.EnqueueAsync(
                    new SubmissionWorkItem(
                        intake.Scope.OrganizationId,
                        Guid.CreateVersion7(),
                        workKind,
                        intake.Scope.EnrollmentId,
                        intake.IntakeId,
                        null,
                        SubmissionWorkStates.Pending,
                        0,
                        _clock.UtcNow,
                        null,
                        item.ArtifactObjectKey,
                        item.ArtifactVersionId),
                    new CleanupEnqueueTransaction(),
                    cancellationToken);
            }
        }
    }

    private sealed class CleanupEnqueueTransaction : IEnrollmentTransaction
    {
        public bool AuditAccepted { get; set; } = true;

        public bool OutboxAccepted { get; set; } = true;

        public object CommitHandle { get; } = new();
    }
}
