using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class SubmissionCleanupProcessorTests
{
    [Fact]
    public async Task Cleanup_removes_stale_incomplete_bytes_and_skips_accepted_or_held_artifacts()
    {
        var organizationId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var now = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        var clock = new FixedEnrollmentClock(now);
        var artifacts = new InMemoryArtifactStore();
        var intakes = new InMemoryIntakeStore();
        var versions = new InMemorySubmissionVersionStore();
        var holds = new InMemoryLifecycleHoldStore();
        var dispositions = new InMemoryArtifactDispositionStore();
        var work = new InMemorySubmissionWorkStore();
        var transaction = new InMemoryEnrollmentTransaction();

        var staleItemId = Guid.CreateVersion7();
        var acceptedItemId = Guid.CreateVersion7();
        var heldItemId = Guid.CreateVersion7();
        var stalePut = await artifacts.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, staleItemId), "stale"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);
        var acceptedPut = await artifacts.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, acceptedItemId), "kept"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);
        var heldPut = await artifacts.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, heldItemId), "held"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);

        var scope = new SubmissionParentScope(
            organizationId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new string('a', 64));

        await intakes.InsertIntakeAsync(
            Intake(scope, IntakeStates.Receiving, now.AddHours(-25), stalePut.Reference!),
            scope.ParticipantActorId,
            transaction,
            TestContext.Current.CancellationToken);
        await versions.InsertAcceptedVersionAsync(
            new AcceptedSubmissionVersion(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                1,
                scope,
                new string('a', 64),
                null,
                now,
                [
                    new AcceptedVersionItem(
                        acceptedItemId,
                        MaterialCategories.DirectText,
                        null,
                        4,
                        acceptedPut.Reference!.Digest.Sha256Hex,
                        acceptedPut.Reference.ObjectKey.Value,
                        acceptedPut.Reference.VersionId.Value),
                ]),
            scope.ParticipantActorId,
            transaction,
            TestContext.Current.CancellationToken);
        await intakes.InsertIntakeAsync(
            Intake(scope with { EnrollmentId = Guid.CreateVersion7() }, IntakeStates.Receiving, now.AddHours(-25), acceptedPut.Reference!),
            scope.ParticipantActorId,
            transaction,
            TestContext.Current.CancellationToken);
        await holds.InsertHoldAsync(organizationId, Guid.CreateVersion7(), heldPut.Reference!.ObjectKey.Value, TestContext.Current.CancellationToken);
        await intakes.InsertIntakeAsync(
            Intake(scope with { EnrollmentId = Guid.CreateVersion7() }, IntakeStates.Cancelled, now.AddDays(-8), heldPut.Reference),
            scope.ParticipantActorId,
            transaction,
            TestContext.Current.CancellationToken);

        var processor = new SubmissionCleanupProcessor(work, intakes, artifacts, versions, holds, dispositions, clock);

        string outcome = "idle";
        for (var i = 0; i < 12; i++)
        {
            outcome = await processor.TryProcessNextAsync(TestContext.Current.CancellationToken);
            if (outcome == "idle")
            {
                break;
            }
        }

        var staleGone = await artifacts.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, stalePut.Reference!),
            TestContext.Current.CancellationToken);
        var acceptedKept = await artifacts.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, acceptedPut.Reference!),
            TestContext.Current.CancellationToken);
        var heldKept = await artifacts.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, heldPut.Reference),
            TestContext.Current.CancellationToken);

        Assert.False(staleGone.Succeeded);
        Assert.True(acceptedKept.Succeeded);
        Assert.True(heldKept.Succeeded);
        Assert.Contains(dispositions.Records, record => record.ArtifactObjectKey == stalePut.Reference!.ObjectKey.Value);
        Assert.DoesNotContain(dispositions.Records, record => record.ArtifactObjectKey == acceptedPut.Reference!.ObjectKey.Value);
        Assert.DoesNotContain(dispositions.Records, record => record.ArtifactObjectKey == heldPut.Reference.ObjectKey.Value);
    }

    [Fact]
    public async Task Cleanup_removes_accepted_bytes_only_after_activity_closure_retention_and_skips_holds()
    {
        var organizationId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var now = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        var clock = new FixedEnrollmentClock(now);
        var artifacts = new InMemoryArtifactStore();
        var intakes = new InMemoryIntakeStore();
        var versions = new InMemorySubmissionVersionStore();
        var holds = new InMemoryLifecycleHoldStore();
        var dispositions = new InMemoryArtifactDispositionStore();
        var work = new InMemorySubmissionWorkStore();
        var transaction = new InMemoryEnrollmentTransaction();
        var activityId = Guid.CreateVersion7();

        var eligibleItemId = Guid.CreateVersion7();
        var heldItemId = Guid.CreateVersion7();
        var openItemId = Guid.CreateVersion7();
        var eligiblePut = await artifacts.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, eligibleItemId), "old"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);
        var heldPut = await artifacts.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, heldItemId), "held"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);
        var openPut = await artifacts.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, openItemId), "open"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);

        var closedScope = new SubmissionParentScope(
            organizationId,
            activityId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new string('a', 64));
        var openScope = closedScope with { ActivityId = Guid.CreateVersion7(), EnrollmentId = Guid.CreateVersion7() };

        await versions.InsertAcceptedVersionAsync(
            new AcceptedSubmissionVersion(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                1,
                closedScope,
                new string('a', 64),
                null,
                now,
                [
                    new AcceptedVersionItem(
                        eligibleItemId,
                        MaterialCategories.DirectText,
                        null,
                        3,
                        eligiblePut.Reference!.Digest.Sha256Hex,
                        eligiblePut.Reference.ObjectKey.Value,
                        eligiblePut.Reference.VersionId.Value),
                ]),
            closedScope.ParticipantActorId,
            transaction,
            TestContext.Current.CancellationToken);
        await versions.InsertAcceptedVersionAsync(
            new AcceptedSubmissionVersion(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                1,
                closedScope with { EnrollmentId = Guid.CreateVersion7() },
                new string('a', 64),
                null,
                now,
                [
                    new AcceptedVersionItem(
                        heldItemId,
                        MaterialCategories.DirectText,
                        null,
                        4,
                        heldPut.Reference!.Digest.Sha256Hex,
                        heldPut.Reference.ObjectKey.Value,
                        heldPut.Reference.VersionId.Value),
                ]),
            closedScope.ParticipantActorId,
            transaction,
            TestContext.Current.CancellationToken);
        await versions.InsertAcceptedVersionAsync(
            new AcceptedSubmissionVersion(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                1,
                openScope,
                new string('a', 64),
                null,
                now,
                [
                    new AcceptedVersionItem(
                        openItemId,
                        MaterialCategories.DirectText,
                        null,
                        4,
                        openPut.Reference!.Digest.Sha256Hex,
                        openPut.Reference.ObjectKey.Value,
                        openPut.Reference.VersionId.Value),
                ]),
            openScope.ParticipantActorId,
            transaction,
            TestContext.Current.CancellationToken);
        await holds.InsertHoldAsync(organizationId, Guid.CreateVersion7(), heldPut.Reference!.ObjectKey.Value, TestContext.Current.CancellationToken);

        var closures = new StubActivityClosurePort(activityId, now.AddDays(-366));
        var processor = new SubmissionCleanupProcessor(work, intakes, artifacts, versions, holds, dispositions, clock, closures);
        for (var i = 0; i < 12; i++)
        {
            if (await processor.TryProcessNextAsync(TestContext.Current.CancellationToken) == "idle")
            {
                break;
            }
        }

        var eligibleGone = await artifacts.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, eligiblePut.Reference!),
            TestContext.Current.CancellationToken);
        var heldKept = await artifacts.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, heldPut.Reference),
            TestContext.Current.CancellationToken);
        var openKept = await artifacts.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, openPut.Reference!),
            TestContext.Current.CancellationToken);

        Assert.False(eligibleGone.Succeeded);
        Assert.True(heldKept.Succeeded);
        Assert.True(openKept.Succeeded);
        Assert.Contains(dispositions.Records, record => record.ArtifactObjectKey == eligiblePut.Reference.ObjectKey.Value);
        Assert.DoesNotContain(dispositions.Records, record => record.ArtifactObjectKey == heldPut.Reference.ObjectKey.Value);
        Assert.DoesNotContain(dispositions.Records, record => record.ArtifactObjectKey == openPut.Reference.ObjectKey.Value);
    }

    private static SubmissionIntakeRecord Intake(
        SubmissionParentScope scope,
        string status,
        DateTimeOffset created,
        StoredArtifactReference artifact) =>
        new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            scope,
            status,
            1,
            new string('a', 64),
            scope.TaskSourceId,
            scope.TaskVersionId,
            scope.TaskContentDigest,
            scope.TaskSourceId,
            scope.TaskVersionId,
            scope.TaskContentDigest,
            created,
            created,
            created,
            [
                new IntakeItem(
                    Guid.CreateVersion7(),
                    MaterialCategories.DirectText,
                    null,
                    "text/plain",
                    artifact.ByteCount,
                    artifact.Digest.Sha256Hex,
                    artifact.ObjectKey.Value,
                    artifact.VersionId.Value,
                    created),
            ]);

    private sealed class FixedEnrollmentClock(DateTimeOffset now) : IEnrollmentClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class StubActivityClosurePort(Guid closedActivityId, DateTimeOffset closedAtUtc) : IActivityClosurePort
    {
        public Task<DateTimeOffset?> FindClosedAtUtcAsync(
            Guid organizationId,
            Guid activityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(activityId == closedActivityId ? closedAtUtc : (DateTimeOffset?)null);
    }
}
