using System.Text;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class IntakeCoordinatorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-24T12:00:00Z");
    private static readonly Guid OrganizationId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid ActivityId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid CohortId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2");
    private static readonly Guid ParticipantA = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
    private static readonly Guid ParticipantB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2");
    private static readonly Guid EnrollmentA = Guid.Parse("dddddddd-dddd-4ddd-8ddd-ddddddddddda");
    private static readonly Guid EnrollmentB = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddb");
    private static readonly string Digest = new('a', 64);

    [Fact]
    public async Task Finalize_does_not_scan_another_enrollment_intake()
    {
        var scanner = new RecordingArtifactSafetyScanner();
        var harness = CreateHarness(scanner);
        var began = await harness.Coordinator.BeginAsync(
            BeginCommand(EnrollmentB, ParticipantB, "begin-scan-b"),
            TestContext.Current.CancellationToken);
        Assert.True(began.Succeeded, began.OutcomeCode);
        var completed = await harness.Coordinator.CompleteItemAsync(
            CompleteCommand(EnrollmentB, ParticipantB, began.IntakeId!.Value, began.Revision!.Value, "Owned by B.", "item-b"),
            TestContext.Current.CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        var denied = await harness.Coordinator.FinalizeAsync(
            FinalizeCommand(EnrollmentA, ParticipantA, began.IntakeId.Value, completed.Revision!.Value, "finalize-a-on-b"),
            TestContext.Current.CancellationToken);

        Assert.False(denied.Succeeded);
        Assert.Equal(SubmissionFailureCodes.NotFound, denied.OutcomeCode);
        Assert.Equal(0, scanner.ScanCount);
    }

    [Fact]
    public async Task Cancel_from_another_enrollment_returns_not_found()
    {
        var harness = CreateHarness();
        var began = await harness.Coordinator.BeginAsync(BeginCommand(EnrollmentB, ParticipantB, "begin-b"), TestContext.Current.CancellationToken);
        Assert.True(began.Succeeded, began.OutcomeCode);

        var denied = await harness.Coordinator.CancelAsync(
            CancelCommand(EnrollmentA, ParticipantA, began.IntakeId!.Value, began.Revision!.Value, "cancel-a"),
            TestContext.Current.CancellationToken);
        Assert.False(denied.Succeeded);
        Assert.Equal(SubmissionFailureCodes.NotFound, denied.OutcomeCode);
    }

    [Fact]
    public async Task Second_begin_reuses_stable_submission_for_enrollment()
    {
        var harness = CreateHarness();
        var first = await harness.Coordinator.BeginAsync(BeginCommand(EnrollmentA, ParticipantA, "begin-1"), TestContext.Current.CancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);
        var cancelled = await harness.Coordinator.CancelAsync(
            CancelCommand(EnrollmentA, ParticipantA, first.IntakeId!.Value, first.Revision!.Value, "cancel-1"),
            TestContext.Current.CancellationToken);
        Assert.True(cancelled.Succeeded, cancelled.OutcomeCode);

        var second = await harness.Coordinator.BeginAsync(BeginCommand(EnrollmentA, ParticipantA, "begin-2"), TestContext.Current.CancellationToken);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(first.SubmissionId, second.SubmissionId);
        Assert.NotEqual(first.IntakeId, second.IntakeId);
    }

    [Fact]
    public async Task Finalize_idempotency_replay_returns_same_version_id()
    {
        var harness = CreateHarness();
        var began = await harness.Coordinator.BeginAsync(BeginCommand(EnrollmentA, ParticipantA, "begin-finalize"), TestContext.Current.CancellationToken);
        Assert.True(began.Succeeded, began.OutcomeCode);
        await SeedReceivedItemAsync(harness, EnrollmentA, began.IntakeId!.Value, began.Revision!.Value);

        var finalize = await harness.Coordinator.FinalizeAsync(
            FinalizeCommand(EnrollmentA, ParticipantA, began.IntakeId.Value, began.Revision.Value + 1, "finalize-1"),
            TestContext.Current.CancellationToken);
        Assert.True(finalize.Succeeded, finalize.OutcomeCode);
        Assert.NotNull(finalize.VersionId);

        var replay = await harness.Coordinator.FinalizeAsync(
            FinalizeCommand(EnrollmentA, ParticipantA, began.IntakeId.Value, began.Revision.Value + 1, "finalize-1"),
            TestContext.Current.CancellationToken);
        Assert.True(replay.Succeeded, replay.OutcomeCode);
        Assert.Equal(finalize.VersionId, replay.VersionId);
        Assert.Equal(finalize.VersionNumber, replay.VersionNumber);
    }

    [Fact]
    public async Task Second_finalize_sets_predecessor_version_lineage()
    {
        var harness = CreateHarness();
        var firstCycle = await FinalizeIntakeAsync(harness, EnrollmentA, ParticipantA, "begin-v1", "finalize-v1");
        var cancelled = await harness.Coordinator.CancelAsync(
            CancelCommand(EnrollmentA, ParticipantA, firstCycle.IntakeId!.Value, firstCycle.Revision!.Value, "cancel-v1"),
            TestContext.Current.CancellationToken);
        Assert.True(cancelled.Succeeded, cancelled.OutcomeCode);

        var secondCycle = await FinalizeIntakeAsync(harness, EnrollmentA, ParticipantA, "begin-v2", "finalize-v2");
        Assert.NotEqual(firstCycle.VersionId, secondCycle.VersionId);
        Assert.Equal(2, secondCycle.VersionNumber);

        var secondVersion = await harness.Versions.FindVersionAsync(
            OrganizationId,
            secondCycle.VersionId!.Value,
            null,
            CancellationToken.None);
        Assert.NotNull(secondVersion);
        Assert.Equal(firstCycle.VersionId, secondVersion.PredecessorVersionId);
    }

    [Fact]
    public async Task Complete_item_receives_direct_text_and_sets_received()
    {
        var harness = CreateHarness();
        var began = await harness.Coordinator.BeginAsync(BeginCommand(EnrollmentA, ParticipantA, "begin-complete"), TestContext.Current.CancellationToken);
        Assert.True(began.Succeeded, began.OutcomeCode);

        var completed = await harness.Coordinator.CompleteItemAsync(
            CompleteCommand(EnrollmentA, ParticipantA, began.IntakeId!.Value, began.Revision!.Value, "Direct text answer.", "complete-1"),
            TestContext.Current.CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);
        Assert.Equal(IntakeStates.Received, completed.Status);

        var intake = await harness.Intakes.FindIntakeAsync(OrganizationId, EnrollmentA, began.IntakeId.Value, null, CancellationToken.None);
        Assert.NotNull(intake);
        Assert.Single(intake.Items);
        Assert.Equal(MaterialCategories.DirectText, intake.Items[0].Category);
        Assert.NotNull(intake.CompleteReceiptAtUtc);
        Assert.NotNull(intake.Items[0].ArtifactObjectKey);
    }

    [Fact]
    public async Task Complete_item_rejects_invalid_utf8()
    {
        var harness = CreateHarness();
        var began = await harness.Coordinator.BeginAsync(BeginCommand(EnrollmentA, ParticipantA, "begin-utf8"), TestContext.Current.CancellationToken);
        var content = new byte[] { 0xFF, 0xFE, 0xFD };
        var digest = MaterialContentValidator.Sha256Hex(content);
        var denied = await harness.Coordinator.CompleteItemAsync(
            new CompleteIntakeItemCommand(
                ParticipantContext(ParticipantA),
                EnrollmentA,
                began.IntakeId!.Value,
                Guid.Empty,
                MaterialCategories.DirectText,
                null,
                "text/plain",
                content,
                digest,
                began.Revision!.Value,
                "complete-utf8",
                SubmissionCommandDigest.Compute(
                    IntakeOperationKinds.CompleteItem,
                    OrganizationId.ToString("D"),
                    EnrollmentA.ToString("D"),
                    began.IntakeId.Value.ToString("D"),
                    began.Revision.Value.ToString(),
                    digest)),
            TestContext.Current.CancellationToken);
        Assert.False(denied.Succeeded);
        Assert.Equal(SubmissionFailureCodes.InvalidEncoding, denied.OutcomeCode);
    }

    [Fact]
    public async Task Complete_item_idempotency_replay_returns_same_intake()
    {
        var harness = CreateHarness();
        var began = await harness.Coordinator.BeginAsync(BeginCommand(EnrollmentA, ParticipantA, "begin-replay-item"), TestContext.Current.CancellationToken);
        var first = await harness.Coordinator.CompleteItemAsync(
            CompleteCommand(EnrollmentA, ParticipantA, began.IntakeId!.Value, began.Revision!.Value, "Direct text answer.", "complete-replay"),
            TestContext.Current.CancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);

        var replay = await harness.Coordinator.CompleteItemAsync(
            CompleteCommand(EnrollmentA, ParticipantA, began.IntakeId.Value, began.Revision.Value, "Direct text answer.", "complete-replay"),
            TestContext.Current.CancellationToken);
        Assert.True(replay.Succeeded, replay.OutcomeCode);
        Assert.Equal(first.IntakeId, replay.IntakeId);
        Assert.Equal(first.Revision, replay.Revision);
    }

    private static async Task<IntakeMutationOutcome> FinalizeIntakeAsync(
        IntakeHarness harness,
        Guid enrollmentId,
        Guid participantId,
        string beginKey,
        string finalizeKey)
    {
        var began = await harness.Coordinator.BeginAsync(BeginCommand(enrollmentId, participantId, beginKey), TestContext.Current.CancellationToken);
        Assert.True(began.Succeeded, began.OutcomeCode);
        await SeedReceivedItemAsync(harness, enrollmentId, began.IntakeId!.Value, began.Revision!.Value);

        var finalize = await harness.Coordinator.FinalizeAsync(
            FinalizeCommand(enrollmentId, participantId, began.IntakeId.Value, began.Revision.Value + 1, finalizeKey),
            TestContext.Current.CancellationToken);
        Assert.True(finalize.Succeeded, finalize.OutcomeCode);
        return finalize;
    }

    [Fact]
    public async Task Cancel_with_stale_revision_returns_bounded_outcome()
    {
        var harness = CreateHarness();
        var began = await harness.Coordinator.BeginAsync(BeginCommand(EnrollmentA, ParticipantA, "begin-stale"), TestContext.Current.CancellationToken);
        Assert.True(began.Succeeded, began.OutcomeCode);

        var stale = await harness.Coordinator.CancelAsync(
            CancelCommand(EnrollmentA, ParticipantA, began.IntakeId!.Value, began.Revision!.Value + 99, "cancel-stale"),
            TestContext.Current.CancellationToken);
        Assert.False(stale.Succeeded);
        Assert.Equal(SubmissionFailureCodes.StaleRevision, stale.OutcomeCode);
    }

    private static async Task SeedReceivedItemAsync(
        IntakeHarness harness,
        Guid enrollmentId,
        Guid intakeId,
        long revision)
    {
        var intake = await harness.Intakes.FindIntakeAsync(
            OrganizationId,
            enrollmentId,
            intakeId,
            null,
            CancellationToken.None);
        Assert.NotNull(intake);
        var item = new IntakeItem(
            Guid.CreateVersion7(),
            MaterialCategories.DirectText,
            "answer.txt",
            "text/plain",
            12,
            new string('b', 64),
            ArtifactObjectKey.Create(OrganizationId, Guid.CreateVersion7()).Value,
            "version-1",
            Now);
        await harness.UnitOfWork.ExecuteAsync(
            ParticipantContext(ParticipantA),
            async transaction =>
            {
                await harness.Intakes.UpdateIntakeAsync(
                    intake with
                    {
                        Revision = revision + 1,
                        Status = IntakeStates.Received,
                        UpdatedAtUtc = Now,
                        CompleteReceiptAtUtc = Now,
                        Items = [item],
                    },
                    ParticipantA,
                    transaction,
                    CancellationToken.None);
                return true;
            },
            CancellationToken.None);
    }

    private static IntakeHarness CreateHarness(IArtifactSafetyScanner? scanner = null)
    {
        var binding = Binding();
        var enrollments = new InMemoryEnrollmentStore();
        enrollments.Restore(
        [
            CreateEnrollment(EnrollmentA, ParticipantA, binding),
            CreateEnrollment(EnrollmentB, ParticipantB, binding),
        ], []);
        var operations = new InMemoryEnrollmentOperationStore();
        var authorization = new AllowEnrollmentAuthorizationPort();
        var cohorts = new FixedActivatedCohortPort { Binding = binding };
        var audit = new RecordingEnrollmentAuditPort();
        var sessions = new AllowEnrollmentSessionPort();
        var unitOfWork = new InMemoryEnrollmentUnitOfWork(sessions, enrollments, operations, audit);
        var intakes = new InMemoryIntakeStore();
        var versions = new InMemorySubmissionVersionStore();
        var timing = new FixedMyWorkTimingPort
        {
            Timing = CreateEffectiveTiming(Now.AddDays(1)),
        };
        var coordinator = new IntakeCoordinator(
            authorization,
            enrollments,
            cohorts,
            new FixedFrozenSubmissionRequirementPort(),
            new FixedMaterialPolicyPort(),
            intakes,
            versions,
            operations,
            audit,
            unitOfWork,
            sessions,
            scanner ?? new DisabledArtifactSafetyScanner(),
            timing,
            new InMemoryArtifactStore(),
            new FixedEnrollmentClock(Now));
        return new IntakeHarness(coordinator, intakes, versions, unitOfWork);
    }

    private sealed class RecordingArtifactSafetyScanner : IArtifactSafetyScanner
    {
        public int ScanCount;

        public Task<ArtifactScanResult> ScanAsync(ArtifactScanRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref ScanCount);
            return Task.FromResult(new ArtifactScanResult(
                true,
                ArtifactScanOutcome.Clean,
                ArtifactOutcomeCodes.ScanDisabled));
        }
    }

    private static Enrollment CreateEnrollment(Guid enrollmentId, Guid participantId, ActivatedCohortBinding binding) =>
        Enrollment.Create(
            enrollmentId,
            OrganizationId,
            ActivityId,
            CohortId,
            binding.BaselineId,
            binding.TaskSourceId,
            binding.TaskVersionId,
            binding.TaskContentDigest,
            EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId,
            EnrollmentLifecyclePolicy.RestrictedPreservationVersion,
            participantId,
            participantId,
            Now).Value!;

    private static BeginIntakeCommand BeginCommand(Guid enrollmentId, Guid participantId, string key) =>
        new(
            ParticipantContext(participantId),
            enrollmentId,
            key,
            SubmissionCommandDigest.Compute(
                IntakeOperationKinds.Begin,
                OrganizationId.ToString("D"),
                enrollmentId.ToString("D")));

    private static CancelIntakeCommand CancelCommand(
        Guid enrollmentId,
        Guid participantId,
        Guid intakeId,
        long revision,
        string key) =>
        new(
            ParticipantContext(participantId),
            enrollmentId,
            intakeId,
            revision,
            key,
            SubmissionCommandDigest.Compute(
                IntakeOperationKinds.Cancel,
                OrganizationId.ToString("D"),
                enrollmentId.ToString("D"),
                intakeId.ToString("D"),
                revision.ToString()));

    private static CompleteIntakeItemCommand CompleteCommand(
        Guid enrollmentId,
        Guid participantId,
        Guid intakeId,
        long revision,
        string text,
        string key)
    {
        var content = Encoding.UTF8.GetBytes(text);
        var digest = MaterialContentValidator.Sha256Hex(content);
        return new CompleteIntakeItemCommand(
            ParticipantContext(participantId),
            enrollmentId,
            intakeId,
            Guid.Empty,
            MaterialCategories.DirectText,
            null,
            "text/plain",
            content,
            digest,
            revision,
            key,
            SubmissionCommandDigest.Compute(
                IntakeOperationKinds.CompleteItem,
                OrganizationId.ToString("D"),
                enrollmentId.ToString("D"),
                intakeId.ToString("D"),
                revision.ToString(),
                digest));
    }

    private static FinalizeIntakeCommand FinalizeCommand(
        Guid enrollmentId,
        Guid participantId,
        Guid intakeId,
        long revision,
        string key) =>
        new(
            ParticipantContext(participantId),
            enrollmentId,
            intakeId,
            revision,
            key,
            SubmissionCommandDigest.Compute(
                IntakeOperationKinds.Finalize,
                OrganizationId.ToString("D"),
                enrollmentId.ToString("D"),
                intakeId.ToString("D"),
                revision.ToString()));

    private static EnrollmentActorContext ParticipantContext(Guid participantId) =>
        new(
            new TrustedActor(participantId, HumanInteractiveActorTypes.Interactive),
            new OrganizationScope(OrganizationId),
            string.Empty,
            new AuthenticationStrength(null, []),
            Guid.CreateVersion7(),
            "https",
            [EnrollmentAuthorizationActions.Discover],
            Guid.CreateVersion7());

    private static ActivatedCohortBinding Binding() =>
        new(
            OrganizationId,
            ActivityId,
            CohortId,
            Guid.CreateVersion7(),
            Digest,
            "activated",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Digest,
            "Campaign",
            "Task",
            "UTC",
            Now,
            Now.AddDays(30),
            Now.AddDays(20),
            EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId,
            EnrollmentLifecyclePolicy.RestrictedPreservationVersion,
            false);

    private static EffectiveTiming CreateEffectiveTiming(DateTimeOffset cutoff) =>
        new(
            new BaselineTiming(
                Now,
                Now.AddDays(30),
                cutoff,
                "UTC",
                1,
                3600,
                new AccommodationPolicyIdentity(Guid.CreateVersion7(), Guid.CreateVersion7(), new string('c', 64)),
                false),
            Now,
            cutoff,
            Now,
            Now.AddDays(30),
            3600,
            Now,
            "open",
            true,
            [],
            AccommodationConsequenceCodes.None,
            "UTC");

    private sealed record IntakeHarness(
        IntakeCoordinator Coordinator,
        InMemoryIntakeStore Intakes,
        InMemorySubmissionVersionStore Versions,
        InMemoryEnrollmentUnitOfWork UnitOfWork);

    private sealed class FixedMyWorkTimingPort : IEnrollmentTimingQueryService
    {
        public EffectiveTiming? Timing { get; set; }

        public Task<EnrollmentDecision<EnrollmentTimingDetail>> GetEnrollmentTimingAsync(
            EnrollmentActorContext actor,
            Guid activityId,
            Guid cohortId,
            Guid enrollmentId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<EnrollmentDecision<AssignmentTimingSummary>> GetMyWorkTimingAsync(
            EnrollmentActorContext actor,
            Guid enrollmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(EnrollmentDecision<AssignmentTimingSummary>.Ok(
                new AssignmentTimingSummary(
                    new AssignmentSummary(
                        enrollmentId,
                        EnrollmentStates.Active,
                        EnrollmentVisibilityStates.Current,
                        "Campaign",
                        "Task",
                        "UTC",
                        Now,
                        Now.AddDays(30),
                        Now.AddDays(20),
                        true,
                        []),
                    Timing,
                    AccommodationConsequenceCodes.None),
                "enrollment.ok"));
    }
}
