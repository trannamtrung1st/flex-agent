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
        var (intakes, versions) = InMemorySubmissionStores.CreatePaired();
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
        var (intakes, versions) = InMemorySubmissionStores.CreatePaired();
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

    [Fact]
    public async Task Cleanup_does_not_record_disposition_when_artifact_delete_returns_false()
    {
        var organizationId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var now = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        var clock = new FixedEnrollmentClock(now);
        var inner = new InMemoryArtifactStore();
        var artifacts = new RecordingArtifactStore(inner) { DeleteSucceeds = false };
        var (intakes, versions) = InMemorySubmissionStores.CreatePaired();
        var holds = new InMemoryLifecycleHoldStore();
        var dispositions = new InMemoryArtifactDispositionStore();
        var work = new InMemorySubmissionWorkStore();
        var transaction = new InMemoryEnrollmentTransaction();
        var activityId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var put = await inner.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, itemId), "keep"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);
        var scope = ClosedScope(organizationId, activityId);
        await InsertAcceptedAsync(versions, transaction, scope, itemId, put.Reference!, now);

        var processor = new SubmissionCleanupProcessor(
            work,
            intakes,
            artifacts,
            versions,
            holds,
            dispositions,
            clock,
            new StubActivityClosurePort(activityId, now.AddDays(-366)));
        var outcome = await processor.TryProcessNextAsync(TestContext.Current.CancellationToken);

        Assert.Equal("failed", outcome);
        Assert.Empty(dispositions.Records);
        var stillPresent = await inner.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, put.Reference!),
            TestContext.Current.CancellationToken);
        Assert.True(stillPresent.Succeeded);
    }

    [Fact]
    public async Task Accepted_cleanup_deletes_the_exact_stored_artifact_version()
    {
        var organizationId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var now = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        var clock = new FixedEnrollmentClock(now);
        var inner = new InMemoryArtifactStore();
        var artifacts = new RecordingArtifactStore(inner);
        var (intakes, versions) = InMemorySubmissionStores.CreatePaired();
        var holds = new InMemoryLifecycleHoldStore();
        var dispositions = new InMemoryArtifactDispositionStore();
        var work = new InMemorySubmissionWorkStore();
        var transaction = new InMemoryEnrollmentTransaction();
        var activityId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var put = await inner.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, itemId), "old"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);
        var scope = ClosedScope(organizationId, activityId);
        await InsertAcceptedAsync(versions, transaction, scope, itemId, put.Reference!, now);

        var processor = new SubmissionCleanupProcessor(
            work,
            intakes,
            artifacts,
            versions,
            holds,
            dispositions,
            clock,
            new StubActivityClosurePort(activityId, now.AddDays(-366)));
        for (var i = 0; i < 4; i++)
        {
            if (await processor.TryProcessNextAsync(TestContext.Current.CancellationToken) == "idle")
            {
                break;
            }
        }

        Assert.Contains(
            artifacts.Deleted,
            deleted => deleted.ObjectKey.Value == put.Reference!.ObjectKey.Value
                && deleted.VersionId.Value == put.Reference.VersionId.Value
                && deleted.VersionId.Value.Length > 0);
        var exactGone = await inner.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, put.Reference!),
            TestContext.Current.CancellationToken);
        Assert.False(exactGone.Succeeded);
    }

    [Fact]
    public async Task Accepted_cleanup_reaches_eligible_artifact_after_a_full_page_of_disposed_rows()
    {
        var organizationId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var now = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        var clock = new FixedEnrollmentClock(now);
        var artifacts = new InMemoryArtifactStore();
        var (intakes, versions) = InMemorySubmissionStores.CreatePaired();
        var holds = new InMemoryLifecycleHoldStore();
        var dispositions = new InMemoryArtifactDispositionStore();
        var work = new InMemorySubmissionWorkStore();
        var transaction = new InMemoryEnrollmentTransaction();
        var activityId = Guid.CreateVersion7();
        var scope = ClosedScope(organizationId, activityId);

        for (var i = 0; i < 20; i++)
        {
            var itemId = Guid.CreateVersion7();
            var put = await artifacts.PutAsync(
                new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, itemId), "old"u8.ToArray(), "text/plain"),
                TestContext.Current.CancellationToken);
            await InsertAcceptedAsync(
                versions,
                transaction,
                scope with { EnrollmentId = Guid.CreateVersion7() },
                itemId,
                put.Reference!,
                now.AddMinutes(i));
            await dispositions.RecordAsync(
                organizationId,
                Guid.CreateVersion7(),
                SubmissionWorkKinds.CleanupAccepted,
                put.Reference!.ObjectKey.Value,
                now,
                TestContext.Current.CancellationToken);
        }

        var eligibleItemId = Guid.CreateVersion7();
        var eligiblePut = await artifacts.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, eligibleItemId), "keep-until-cleanup"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);
        await InsertAcceptedAsync(
            versions,
            transaction,
            scope with { EnrollmentId = Guid.CreateVersion7() },
            eligibleItemId,
            eligiblePut.Reference!,
            now.AddMinutes(20));

        var processor = new SubmissionCleanupProcessor(
            work,
            intakes,
            artifacts,
            versions,
            holds,
            dispositions,
            clock,
            new StubActivityClosurePort(activityId, now.AddDays(-366)));
        for (var i = 0; i < 8; i++)
        {
            await processor.TryProcessNextAsync(TestContext.Current.CancellationToken);
        }

        var eligibleGone = await artifacts.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, eligiblePut.Reference!),
            TestContext.Current.CancellationToken);
        Assert.False(eligibleGone.Succeeded);
        Assert.Contains(
            dispositions.Records,
            record => record.ArtifactObjectKey == eligiblePut.Reference!.ObjectKey.Value);
    }

    [Fact]
    public async Task Cleanup_fails_closed_when_claimed_work_has_no_exact_artifact_version()
    {
        var organizationId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var now = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        var clock = new FixedEnrollmentClock(now);
        var inner = new InMemoryArtifactStore();
        var artifacts = new RecordingArtifactStore(inner);
        var (intakes, versions) = InMemorySubmissionStores.CreatePaired();
        var holds = new InMemoryLifecycleHoldStore();
        var dispositions = new InMemoryArtifactDispositionStore();
        var work = new InMemorySubmissionWorkStore();
        var itemId = Guid.CreateVersion7();
        var put = await inner.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, itemId), "legacy"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);

        await work.EnqueueAsync(
            new SubmissionWorkItem(
                organizationId,
                Guid.CreateVersion7(),
                SubmissionWorkKinds.CleanupIncomplete,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                null,
                SubmissionWorkStates.Pending,
                0,
                now,
                null,
                put.Reference!.ObjectKey.Value,
                null),
            new InMemoryEnrollmentTransaction(),
            TestContext.Current.CancellationToken);

        var processor = new SubmissionCleanupProcessor(work, intakes, artifacts, versions, holds, dispositions, clock);
        var outcome = await processor.TryProcessNextAsync(TestContext.Current.CancellationToken);

        Assert.Equal("failed", outcome);
        Assert.Empty(artifacts.Deleted);
        Assert.Empty(dispositions.Records);
        Assert.Contains(
            work.Items,
            item => item.Status == SubmissionWorkStates.Failed
                && item.FailureReason == SubmissionWorkFailureReasons.ExactArtifactVersionUnavailable);
        var stillPresent = await inner.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, put.Reference),
            TestContext.Current.CancellationToken);
        Assert.True(stillPresent.Succeeded);
    }

    [Fact]
    public async Task Accepted_cleanup_advances_persisted_scan_cursor_past_a_page_of_held_artifacts()
    {
        var organizationId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var now = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        var clock = new FixedEnrollmentClock(now);
        var artifacts = new InMemoryArtifactStore();
        var (intakes, versions) = InMemorySubmissionStores.CreatePaired();
        var holds = new InMemoryLifecycleHoldStore();
        var dispositions = new InMemoryArtifactDispositionStore();
        var work = new InMemorySubmissionWorkStore();
        var scan = new InMemoryAcceptedCleanupScanStore();
        var transaction = new InMemoryEnrollmentTransaction();
        var activityId = Guid.CreateVersion7();
        var scope = ClosedScope(organizationId, activityId);

        for (var i = 0; i < 20; i++)
        {
            var itemId = Guid.CreateVersion7();
            var put = await artifacts.PutAsync(
                new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, itemId), "held"u8.ToArray(), "text/plain"),
                TestContext.Current.CancellationToken);
            await InsertAcceptedAsync(
                versions,
                transaction,
                scope with { EnrollmentId = Guid.CreateVersion7() },
                itemId,
                put.Reference!,
                now.AddMinutes(i));
            await holds.InsertHoldAsync(
                organizationId,
                Guid.CreateVersion7(),
                put.Reference!.ObjectKey.Value,
                TestContext.Current.CancellationToken);
        }

        var eligibleItemId = Guid.CreateVersion7();
        var eligiblePut = await artifacts.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, eligibleItemId), "eligible"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);
        await InsertAcceptedAsync(
            versions,
            transaction,
            scope with { EnrollmentId = Guid.CreateVersion7() },
            eligibleItemId,
            eligiblePut.Reference!,
            now.AddMinutes(20));

        var processor = new SubmissionCleanupProcessor(
            work,
            intakes,
            artifacts,
            versions,
            holds,
            dispositions,
            clock,
            new StubActivityClosurePort(activityId, now.AddDays(-366)),
            acceptedScan: scan);

        Assert.Equal("idle", await processor.TryProcessNextAsync(TestContext.Current.CancellationToken));
        Assert.NotNull((await scan.GetSnapshotAsync(TestContext.Current.CancellationToken)).Cursor);

        string outcome = "idle";
        for (var i = 0; i < 6; i++)
        {
            outcome = await processor.TryProcessNextAsync(TestContext.Current.CancellationToken);
            if (outcome == "idle")
            {
                break;
            }
        }

        var eligibleGone = await artifacts.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, eligiblePut.Reference!),
            TestContext.Current.CancellationToken);
        Assert.False(eligibleGone.Succeeded);
        Assert.Contains(
            dispositions.Records,
            record => record.ArtifactObjectKey == eligiblePut.Reference!.ObjectKey.Value);
    }

    [Fact]
    public async Task Accepted_cleanup_scan_cas_rejects_stale_generation()
    {
        var scan = new InMemoryAcceptedCleanupScanStore();
        var first = new AcceptedArtifactCleanupCursor(
            DateTimeOffset.Parse("2026-08-25T12:00:00Z"),
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Guid.Parse("22222222-2222-4222-8222-222222222222"));
        var stale = new AcceptedArtifactCleanupCursor(
            DateTimeOffset.Parse("2026-08-25T11:00:00Z"),
            Guid.Parse("33333333-3333-4333-8333-333333333333"),
            Guid.Parse("44444444-4444-4444-8444-444444444444"));
        var snapshot = await scan.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.True(await scan.TryAdvanceAsync(snapshot.Generation, first, TestContext.Current.CancellationToken));
        Assert.False(await scan.TryAdvanceAsync(snapshot.Generation, stale, TestContext.Current.CancellationToken));

        var after = await scan.GetSnapshotAsync(TestContext.Current.CancellationToken);
        Assert.Equal(first, after.Cursor);
        Assert.Equal(snapshot.Generation + 1, after.Generation);
    }

    [Fact]
    public async Task Accepted_cleanup_completes_duplicate_work_when_peer_already_disposed()
    {
        var organizationId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var now = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        var clock = new FixedEnrollmentClock(now);
        var inner = new InMemoryArtifactStore();
        var artifacts = new RecordingArtifactStore(inner);
        var (intakes, versions) = InMemorySubmissionStores.CreatePaired();
        var holds = new InMemoryLifecycleHoldStore();
        var dispositions = new InMemoryArtifactDispositionStore();
        var work = new InMemorySubmissionWorkStore();
        var scan = new InMemoryAcceptedCleanupScanStore();
        var transaction = new InMemoryEnrollmentTransaction();
        var activityId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var put = await inner.PutAsync(
            new ArtifactPutRequest(organizationId, ArtifactObjectKey.Create(organizationId, itemId), "bytes"u8.ToArray(), "text/plain"),
            TestContext.Current.CancellationToken);
        await InsertAcceptedAsync(
            versions,
            transaction,
            ClosedScope(organizationId, activityId),
            itemId,
            put.Reference!,
            now.AddDays(-366));

        var replicaBSawEnqueueCheck = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var replicaAFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var replicaBDispositions = new StaleEnqueueDispositionStore(
            dispositions,
            replicaBSawEnqueueCheck,
            replicaAFinished);
        var closures = new StubActivityClosurePort(activityId, now.AddDays(-366));
        var replicaA = new SubmissionCleanupProcessor(
            work,
            intakes,
            artifacts,
            versions,
            holds,
            dispositions,
            clock,
            closures,
            acceptedScan: scan);
        var replicaB = new SubmissionCleanupProcessor(
            work,
            intakes,
            artifacts,
            versions,
            holds,
            replicaBDispositions,
            clock,
            closures,
            acceptedScan: scan);

        var replicaBTask = replicaB.TryProcessNextAsync(TestContext.Current.CancellationToken);
        await replicaBSawEnqueueCheck.Task.WaitAsync(TestContext.Current.CancellationToken);
        var replicaAOutcome = await replicaA.TryProcessNextAsync(TestContext.Current.CancellationToken);
        replicaAFinished.SetResult();
        var replicaBOutcome = await replicaBTask.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal("completed", replicaAOutcome);
        Assert.Equal("completed", replicaBOutcome);
        Assert.Single(artifacts.Deleted);
        Assert.Single(dispositions.Records);
        Assert.DoesNotContain(work.Items, item => item.Status == SubmissionWorkStates.Pending);
        Assert.Equal(2, work.Items.Count(item => item.Status == SubmissionWorkStates.Completed));
        var gone = await inner.GetExactVersionAsync(
            new ArtifactGetRequest(organizationId, put.Reference!),
            TestContext.Current.CancellationToken);
        Assert.False(gone.Succeeded);
    }

    private static SubmissionParentScope ClosedScope(Guid organizationId, Guid activityId) =>
        new(
            organizationId,
            activityId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new string('a', 64));

    private static Task InsertAcceptedAsync(
        InMemorySubmissionVersionStore versions,
        InMemoryEnrollmentTransaction transaction,
        SubmissionParentScope scope,
        Guid itemId,
        StoredArtifactReference artifact,
        DateTimeOffset acceptedAtUtc) =>
        versions.InsertAcceptedVersionAsync(
            new AcceptedSubmissionVersion(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                1,
                scope,
                new string('a', 64),
                null,
                acceptedAtUtc,
                [
                    new AcceptedVersionItem(
                        itemId,
                        MaterialCategories.DirectText,
                        null,
                        artifact.ByteCount,
                        artifact.Digest.Sha256Hex,
                        artifact.ObjectKey.Value,
                        artifact.VersionId.Value),
                ]),
            scope.ParticipantActorId,
            transaction,
            TestContext.Current.CancellationToken);

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

    private sealed class StaleEnqueueDispositionStore(
        IArtifactDispositionStore inner,
        TaskCompletionSource enqueueCheckStarted,
        TaskCompletionSource peerFinished) : IArtifactDispositionStore
    {
        private int _existsCalls;

        public Task RecordAsync(
            Guid organizationId,
            Guid dispositionId,
            string workKind,
            string artifactObjectKey,
            DateTimeOffset disposedAtUtc,
            CancellationToken cancellationToken = default) =>
            inner.RecordAsync(organizationId, dispositionId, workKind, artifactObjectKey, disposedAtUtc, cancellationToken);

        public async Task<bool> ExistsAsync(
            Guid organizationId,
            string artifactObjectKey,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _existsCalls) == 1)
            {
                enqueueCheckStarted.TrySetResult();
                await peerFinished.Task.WaitAsync(cancellationToken);
                return false;
            }

            return await inner.ExistsAsync(organizationId, artifactObjectKey, cancellationToken);
        }
    }

    private sealed class RecordingArtifactStore(IArtifactStore inner) : IArtifactStore
    {
        public List<StoredArtifactReference> Deleted { get; } = [];

        public bool DeleteSucceeds { get; set; } = true;

        public Task<ArtifactPutResult> PutAsync(ArtifactPutRequest request, CancellationToken cancellationToken = default) =>
            inner.PutAsync(request, cancellationToken);

        public Task<ArtifactGetResult> GetExactVersionAsync(ArtifactGetRequest request, CancellationToken cancellationToken = default) =>
            inner.GetExactVersionAsync(request, cancellationToken);

        public Task<ArtifactPresignResult> IssueUploadCapabilityAsync(
            ArtifactPresignRequest request,
            CancellationToken cancellationToken = default) =>
            inner.IssueUploadCapabilityAsync(request, cancellationToken);

        public Task<ArtifactPresignResult> IssueDownloadCapabilityAsync(
            ArtifactPresignRequest request,
            CancellationToken cancellationToken = default) =>
            inner.IssueDownloadCapabilityAsync(request, cancellationToken);

        public async Task<bool> DeleteAsync(
            Guid organizationId,
            StoredArtifactReference reference,
            CancellationToken cancellationToken = default)
        {
            Deleted.Add(reference);
            if (!DeleteSucceeds)
            {
                return false;
            }

            return await inner.DeleteAsync(organizationId, reference, cancellationToken);
        }
    }
}
