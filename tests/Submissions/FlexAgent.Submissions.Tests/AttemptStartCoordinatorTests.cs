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
    public async Task Binding_failure_after_session_commit_aborts_without_an_attempt()
    {
        var harness = await CreateHarnessAsync(acknowledgments: new FailBindAcknowledgmentPort());
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
        Assert.Equal(AttemptFailureCodes.AcknowledgmentInvalid, started.OutcomeCode);
        Assert.Empty(harness.Attempts.Items);
        Assert.Equal(0, harness.Sessions.CommitCount);
        Assert.Equal(StartOperationStates.Failed, Assert.Single(harness.StartOperations.Items).Status);
        Assert.Equal(AttemptFailureCodes.AcknowledgmentInvalid, harness.StartOperations.Items[0].OutcomeCode);
        var readiness = await harness.Coordinator.GetAsync(
            ParticipantContext(),
            EnrollmentId,
            TestContext.Current.CancellationToken);
        Assert.Equal(AttemptReadinessStates.Eligible, readiness.Value!.ReadinessState);
        Assert.Equal(1, readiness.Value.RemainingEntitlement);
    }

    [Fact]
    public async Task Unavailable_session_after_bind_records_failed_start_operation()
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
        Assert.Equal(StartOperationStates.Failed, Assert.Single(harness.StartOperations.Items).Status);
        Assert.Equal(1, RemainingEntitlement(harness));
    }

    private static int RemainingEntitlement(Harness harness) =>
        AttemptEntitlementCalculator.Remaining(1, harness.Attempts.Items, [], Now);

    [Fact]
    public async Task Second_start_binds_a_new_unbound_acknowledgment_not_the_historical_row()
    {
        var notice = new RequiredNoticeProjection(
            Guid.CreateVersion7(),
            "instructions",
            "affirmed",
            "notice:1",
            Guid.CreateVersion7(),
            Digest,
            Guid.CreateVersion7());
        var acknowledgments = new InMemoryAcknowledgmentLifecyclePort();
        var harness = await CreateHarnessAsync(
            attemptLimit: 2,
            notices: new FixedNoticePort(notice),
            acknowledgments: acknowledgments);
        var firstDigest = AttemptCommandDigest.Compute(
            OrganizationId,
            EnrollmentId,
            ParticipantId,
            1,
            AttemptEntitlementSources.Baseline,
            harness.VersionIds,
            [notice.SourceVersionId]);
        await acknowledgments.RecordAsync(
            new AcknowledgeAttemptNoticeCommand(
                ParticipantContext(),
                EnrollmentId,
                notice.NoticeId,
                notice.SourceVersionId,
                "affirmed",
                "ack-key-0001",
                AcknowledgmentCommandDigest.Compute(
                    OrganizationId,
                    EnrollmentId,
                    ParticipantId,
                    notice.NoticeId,
                    notice.SourceVersionId,
                    "affirmed")),
            notice,
            new InMemoryEnrollmentTransaction(),
            TestContext.Current.CancellationToken);
        var first = await harness.Coordinator.StartAsync(
            new StartAttemptCommand(ParticipantContext(), EnrollmentId, "start-key-0001", firstDigest),
            TestContext.Current.CancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.Equal(Digest, harness.Attempts.Items[0].Binding.ConfigurationDigest);

        var completed = harness.Attempts.Items[0].Complete(Now.AddMinutes(1), "completed");
        Assert.True(completed.Succeeded);
        await harness.Attempts.UpdateTerminalAsync(
            completed.Value!,
            new InMemoryEnrollmentTransaction(),
            TestContext.Current.CancellationToken);

        await acknowledgments.RecordAsync(
            new AcknowledgeAttemptNoticeCommand(
                ParticipantContext(),
                EnrollmentId,
                notice.NoticeId,
                notice.SourceVersionId,
                "affirmed",
                "ack-key-0002",
                AcknowledgmentCommandDigest.Compute(
                    OrganizationId,
                    EnrollmentId,
                    ParticipantId,
                    notice.NoticeId,
                    notice.SourceVersionId,
                    "affirmed")),
            notice,
            new InMemoryEnrollmentTransaction(),
            TestContext.Current.CancellationToken);
        var secondDigest = AttemptCommandDigest.Compute(
            OrganizationId,
            EnrollmentId,
            ParticipantId,
            2,
            AttemptEntitlementSources.Baseline,
            harness.VersionIds,
            [notice.SourceVersionId]);
        var second = await harness.Coordinator.StartAsync(
            new StartAttemptCommand(ParticipantContext(), EnrollmentId, "start-key-0002", secondDigest),
            TestContext.Current.CancellationToken);

        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(2, second.Ordinal);
        Assert.Equal(2, harness.Attempts.Items.Count);
        Assert.Equal(2, acknowledgments.Items.Count(item => item.Record.BoundAttemptId is not null));
        Assert.NotEqual(acknowledgments.Items[0].Record.BoundAttemptId, acknowledgments.Items[1].Record.BoundAttemptId);
    }

    [Fact]
    public async Task Unavailable_session_commit_gate_is_visible_in_readiness()
    {
        var harness = await CreateHarnessAsync();
        harness.Sessions.CanCommit = false;
        var readiness = await harness.Coordinator.GetAsync(
            ParticipantContext(),
            EnrollmentId,
            TestContext.Current.CancellationToken);
        Assert.Equal(AttemptReadinessStates.ConfigurationUnavailable, readiness.Value!.ReadinessState);
        Assert.DoesNotContain(AttemptClientActions.StartAttempt, readiness.Value.PermittedActions);
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

    private static async Task<Harness> CreateHarnessAsync(
        int attemptLimit = 1,
        IParticipantNoticePort? notices = null,
        IAcknowledgmentLifecyclePort? acknowledgments = null)
    {
        var binding = Binding(attemptLimit);
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
            notices ?? new EmptyNoticePort(),
            acknowledgments ?? new EmptyAcknowledgmentPort(),
            sessionStarts,
            audit,
            unitOfWork,
            sessions,
            new FixedEnrollmentClock(Now));
        return new Harness(
            coordinator,
            attempts,
            startOperations,
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

    private static ActivatedCohortBinding Binding(int attemptLimit = 1) =>
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
            AttemptLimit: attemptLimit);

    private sealed record Harness(
        AttemptStartCoordinator Coordinator,
        InMemoryAttemptStore Attempts,
        InMemoryStartOperationStore StartOperations,
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

        public bool CanCommit { get; set; } = true;

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

    private sealed class FixedNoticePort(RequiredNoticeProjection notice) : IParticipantNoticePort
    {
        public Task<IReadOnlyList<RequiredNoticeProjection>?> ListRequiredAsync(
            Guid organizationId,
            Guid activityId,
            Guid cohortId,
            Guid baselineId,
            IEnrollmentTransaction? transaction,
            CancellationToken cancellationToken = default)
        {
            _ = (organizationId, activityId, cohortId, baselineId, transaction, cancellationToken);
            return Task.FromResult<IReadOnlyList<RequiredNoticeProjection>?>([notice]);
        }
    }

    private sealed class FailBindAcknowledgmentPort : IAcknowledgmentLifecyclePort
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
            Task.FromResult<string?>(AttemptFailureCodes.AcknowledgmentInvalid);
    }
}
