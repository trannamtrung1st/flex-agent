using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class AttemptStartCoordinatorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-02T12:00:00Z");
    private static readonly Guid OrganizationId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid ActivityId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid CohortId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2");
    private static readonly Guid ParticipantId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
    private static readonly Guid EnrollmentId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-ddddddddddda");
    private static readonly string Digest = new('a', 64);

    [Fact]
    public async Task Eligible_start_consumes_one_entitlement_and_returns_session_locator()
    {
        var harness = await CreateHarnessAsync();
        var digest = AttemptCommandDigest.Compute(
            OrganizationId,
            EnrollmentId,
            ParticipantId,
            1,
            AttemptEntitlementSources.Baseline,
            harness.VersionIds,
            []);
        var started = await harness.Coordinator.StartAsync(
            new StartAttemptCommand(ParticipantContext(), EnrollmentId, "start-key-0001", digest),
            TestContext.Current.CancellationToken);

        Assert.True(started.Succeeded, started.OutcomeCode);
        Assert.NotNull(started.AttemptId);
        Assert.NotNull(started.SessionId);
        Assert.Equal(1, started.Ordinal);
        Assert.Equal(0, started.RemainingEntitlement);
        Assert.Contains(AttemptClientActions.ContinueAttempt, started.PermittedActions);
        Assert.Single(harness.Attempts.Items);
        Assert.True(harness.Attempts.Items[0].Consumed);
        Assert.Equal(1, harness.Sessions.CommitCount);
    }

    [Fact]
    public async Task Equivalent_retry_reconciles_without_a_second_consumption()
    {
        var harness = await CreateHarnessAsync();
        var digest = AttemptCommandDigest.Compute(
            OrganizationId,
            EnrollmentId,
            ParticipantId,
            1,
            AttemptEntitlementSources.Baseline,
            harness.VersionIds,
            []);
        var command = new StartAttemptCommand(ParticipantContext(), EnrollmentId, "start-key-0001", digest);
        var first = await harness.Coordinator.StartAsync(command, TestContext.Current.CancellationToken);
        var second = await harness.Coordinator.StartAsync(command, TestContext.Current.CancellationToken);

        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(first.AttemptId, second.AttemptId);
        Assert.Equal(first.SessionId, second.SessionId);
        Assert.Equal(AttemptOutcomes.Reconciled, second.OutcomeCode);
        Assert.Single(harness.Attempts.Items);
        Assert.Equal(1, harness.Sessions.CommitCount);
    }

    [Fact]
    public async Task Precommit_session_failure_does_not_consume_entitlement()
    {
        var harness = await CreateHarnessAsync();
        harness.Sessions.Fail = true;
        var digest = AttemptCommandDigest.Compute(
            OrganizationId,
            EnrollmentId,
            ParticipantId,
            1,
            AttemptEntitlementSources.Baseline,
            harness.VersionIds,
            []);
        var started = await harness.Coordinator.StartAsync(
            new StartAttemptCommand(ParticipantContext(), EnrollmentId, "start-key-0001", digest),
            TestContext.Current.CancellationToken);

        Assert.False(started.Succeeded);
        Assert.Empty(harness.Attempts.Items);
        Assert.Equal(0, started.RemainingEntitlement == 0 ? 0 : started.RemainingEntitlement);
        var readiness = await harness.Coordinator.GetAsync(
            ParticipantContext(),
            EnrollmentId,
            TestContext.Current.CancellationToken);
        Assert.Equal(AttemptReadinessStates.Eligible, readiness.Value!.ReadinessState);
        Assert.Equal(1, readiness.Value.RemainingEntitlement);
    }

    [Fact]
    public async Task Audit_unavailable_rolls_back_without_consuming_entitlement()
    {
        var harness = await CreateHarnessAsync();
        harness.UnitOfWork.AuditAccepted = false;
        var digest = AttemptCommandDigest.Compute(
            OrganizationId,
            EnrollmentId,
            ParticipantId,
            1,
            AttemptEntitlementSources.Baseline,
            harness.VersionIds,
            []);
        var started = await harness.Coordinator.StartAsync(
            new StartAttemptCommand(ParticipantContext(), EnrollmentId, "start-key-0001", digest),
            TestContext.Current.CancellationToken);

        Assert.False(started.Succeeded);
        Assert.Equal(AttemptFailureCodes.AuditUnavailable, started.OutcomeCode);
        Assert.Empty(harness.Attempts.Items);
        var readiness = await harness.Coordinator.GetAsync(
            ParticipantContext(),
            EnrollmentId,
            TestContext.Current.CancellationToken);
        Assert.Equal(AttemptReadinessStates.Eligible, readiness.Value!.ReadinessState);
        Assert.Equal(1, readiness.Value.RemainingEntitlement);
    }

    [Fact]
    public async Task Exact_version_reader_requires_the_commit_transaction()
    {
        var harness = await CreateHarnessAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.ExactVersions.GetExactAsync(
                harness.Scope,
                harness.VersionIds[0],
                null!,
                TestContext.Current.CancellationToken));
    }

    private static async Task<Harness> CreateHarnessAsync()
    {
        var binding = Binding();
        var enrollments = new InMemoryEnrollmentStore();
        enrollments.Restore(
        [
            Enrollment.Create(
                EnrollmentId,
                OrganizationId,
                ActivityId,
                CohortId,
                binding.BaselineId,
                binding.TaskSourceId,
                binding.TaskVersionId,
                binding.TaskContentDigest,
                EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId,
                EnrollmentLifecyclePolicy.RestrictedPreservationVersion,
                ParticipantId,
                ParticipantId,
                Now).Value!,
        ], []);
        var operations = new InMemoryEnrollmentOperationStore();
        var attempts = new InMemoryAttemptStore();
        var startOperations = new InMemoryStartOperationStore();
        var audit = new RecordingEnrollmentAuditPort();
        var sessions = new AllowEnrollmentSessionPort();
        var unitOfWork = new InMemoryEnrollmentUnitOfWork(
            sessions,
            enrollments,
            operations,
            audit,
            attempts: attempts,
            startOperations: startOperations);
        var versions = new InMemorySubmissionVersionStore();
        var version = new AcceptedSubmissionVersion(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            new SubmissionParentScope(
                OrganizationId,
                ActivityId,
                CohortId,
                binding.BaselineId,
                EnrollmentId,
                ParticipantId,
                binding.TaskSourceId,
                binding.TaskVersionId,
                binding.TaskContentDigest),
            Digest,
            null,
            Now,
            [new AcceptedVersionItem(Guid.CreateVersion7(), MaterialCategories.DirectText, null, 12, Digest, "obj", "v1")]);
        await versions.InsertAcceptedVersionAsync(version, ParticipantId, new InMemoryEnrollmentTransaction(), CancellationToken.None);
        var exact = new TransactionBoundExactReader(versions);
        var sessionStarts = new RecordingSessionStartPort();
        var coordinator = new AttemptStartCoordinator(
            new AllowEnrollmentAuthorizationPort(),
            enrollments,
            new FixedActivatedCohortPort { Binding = binding },
            new OpenTimingPort(),
            versions,
            exact,
            attempts,
            startOperations,
            EmptyRetryEntitlementReader.Instance,
            new EmptyNoticePort(),
            new EmptyAcknowledgmentPort(),
            sessionStarts,
            audit,
            unitOfWork,
            sessions,
            new FixedEnrollmentClock(Now));
        return new Harness(
            coordinator,
            attempts,
            sessionStarts,
            exact,
            unitOfWork,
            version.Scope,
            [version.VersionId]);
    }

    private static EnrollmentActorContext ParticipantContext() =>
        new(
            new TrustedActor(ParticipantId, HumanInteractiveActorTypes.Interactive),
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
            false,
            AttemptLimit: 1);

    private sealed record Harness(
        AttemptStartCoordinator Coordinator,
        InMemoryAttemptStore Attempts,
        RecordingSessionStartPort Sessions,
        TransactionBoundExactReader ExactVersions,
        InMemoryEnrollmentUnitOfWork UnitOfWork,
        SubmissionParentScope Scope,
        IReadOnlyList<Guid> VersionIds);

    private sealed class TransactionBoundExactReader(ISubmissionVersionStore versions) : IExactAcceptedVersionReader
    {
        public Task<AcceptedSubmissionVersion?> GetExactAsync(
            SubmissionParentScope scope,
            Guid versionId,
            object commitTransaction,
            CancellationToken cancellationToken = default)
        {
            if (commitTransaction is null)
            {
                throw new InvalidOperationException("commit.transaction.required");
            }

            return versions.FindVersionAsync(scope.OrganizationId, versionId, null, cancellationToken);
        }
    }

    private sealed class RecordingSessionStartPort : ISessionStartCommitPort
    {
        public int CommitCount { get; private set; }

        public bool Fail { get; set; }

        public Task<SessionStartCommitResult> CommitActiveAsync(
            SessionStartCommitRequest request,
            object commitTransaction,
            CancellationToken cancellationToken = default)
        {
            _ = commitTransaction;
            if (Fail)
            {
                return Task.FromResult(new SessionStartCommitResult(false, AttemptFailureCodes.Unavailable, null, null));
            }

            CommitCount++;
            return Task.FromResult(new SessionStartCommitResult(true, "session.started", Digest, Digest));
        }
    }

    private sealed class EmptyNoticePort : IParticipantNoticePort
    {
        public Task<IReadOnlyList<RequiredNoticeProjection>?> ListRequiredAsync(
            Guid organizationId,
            Guid activityId,
            Guid cohortId,
            Guid baselineId,
            IEnrollmentTransaction? transaction,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RequiredNoticeProjection>?>([]);
    }

    private sealed class EmptyAcknowledgmentPort : IAcknowledgmentLifecyclePort
    {
        public Task<AcknowledgmentMutationOutcome> RecordAsync(
            AcknowledgeAttemptNoticeCommand command,
            RequiredNoticeProjection notice,
            object commitTransaction,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AcknowledgmentMutationOutcome(true, "acknowledgment.recorded", Guid.CreateVersion7(), "affirmed"));

        public Task<IReadOnlyList<CurrentAcknowledgmentFact>> ListCurrentAsync(
            Guid organizationId,
            Guid enrollmentId,
            Guid participantActorId,
            IReadOnlyList<RequiredNoticeProjection> notices,
            object commitTransaction,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CurrentAcknowledgmentFact>>([]);

        public Task<string?> BindToAttemptAsync(
            IReadOnlyList<CurrentAcknowledgmentFact> records,
            Guid attemptId,
            Guid enrollmentId,
            Guid participantActorId,
            object commitTransaction,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class OpenTimingPort : IEnrollmentTimingQueryService
    {
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
                    new EffectiveTiming(
                        new BaselineTiming(
                            Now,
                            Now.AddDays(30),
                            Now.AddDays(20),
                            "UTC",
                            1,
                            3600,
                            new AccommodationPolicyIdentity(Guid.CreateVersion7(), Guid.CreateVersion7(), new string('c', 64)),
                            false),
                        Now,
                        Now.AddDays(20),
                        Now,
                        Now.AddDays(30),
                        3600,
                        Now,
                        TimingEligibilityStates.Open,
                        true,
                        [],
                        AccommodationConsequenceCodes.None,
                        "UTC"),
                    AccommodationConsequenceCodes.None),
                "enrollment.ok"));
    }
}
