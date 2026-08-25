using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class SubmissionCleanupProcessor(
    ISubmissionWorkStore work,
    IIntakeStore intakes,
    IArtifactStore artifacts,
    ISubmissionVersionStore versions,
    ISubmissionLifecycleHoldStore holds,
    IArtifactDispositionStore dispositions,
    IEnrollmentClock? clock = null) : ISubmissionCleanupProcessor
{
    private readonly IEnrollmentClock _clock = clock ?? new SystemEnrollmentClock();

    public async Task<string> TryProcessNextAsync(CancellationToken cancellationToken = default)
    {
        await EnqueueEligibleAsync(
            await intakes.ListIncompleteCreatedBeforeAsync(
                _clock.UtcNow - SubmissionLifecycleClocks.IncompleteRetention,
                20,
                cancellationToken),
            SubmissionWorkKinds.CleanupIncomplete,
            cancellationToken);
        await EnqueueEligibleAsync(
            await intakes.ListRejectedUpdatedBeforeAsync(
                _clock.UtcNow - SubmissionLifecycleClocks.RejectedByteRetention,
                20,
                cancellationToken),
            SubmissionWorkKinds.CleanupRejected,
            cancellationToken);

        var claimed = await work.ClaimNextAsync(_clock.UtcNow, SubmissionLifecycleClocks.WorkLease, cancellationToken);
        if (claimed is null)
        {
            return "idle";
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(claimed.ArtifactObjectKey))
            {
                var accepted = await versions.HasAcceptedArtifactKeyAsync(
                    claimed.OrganizationId,
                    claimed.ArtifactObjectKey,
                    cancellationToken);
                var held = await holds.IsHeldAsync(
                    claimed.OrganizationId,
                    claimed.ArtifactObjectKey,
                    cancellationToken);
                if (!SubmissionLifecycle.MayDeleteArtifact(accepted, held))
                {
                    await work.CompleteAsync(claimed.OrganizationId, claimed.WorkId, cancellationToken);
                    return "skipped";
                }

                await artifacts.DeleteAsync(
                    claimed.OrganizationId,
                    new StoredArtifactReference(
                        new ArtifactObjectKey(claimed.ArtifactObjectKey),
                        new ArtifactVersionId(string.Empty),
                        ArtifactDigest.FromHex(new string('0', 64)),
                        0),
                    cancellationToken);
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

    private async Task EnqueueEligibleAsync(
        IReadOnlyList<SubmissionIntakeRecord> intakes,
        string workKind,
        CancellationToken cancellationToken)
    {
        foreach (var intake in intakes)
        {
            foreach (var item in intake.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ArtifactObjectKey))
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
                        item.ArtifactObjectKey),
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
