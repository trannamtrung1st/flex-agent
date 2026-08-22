using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class EnrollmentDomainTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-22T06:00:00Z");
    private static readonly Guid OrganizationId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid ActivityId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid CohortId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2");
    private static readonly Guid OtherCohortId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3");
    private static readonly Guid BaselineId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa4");
    private static readonly Guid TaskSourceId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa5");
    private static readonly Guid TaskVersionId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa6");
    private static readonly Guid ParticipantId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
    private static readonly Guid AdministratorId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2");
    private static readonly string Digest = new string('a', 64);

    [Fact]
    public void Create_binds_immutable_ownership_without_changing_caller_supplied_baseline()
    {
        var created = Enrollment.Create(
            Guid.CreateVersion7(),
            OrganizationId,
            ActivityId,
            CohortId,
            BaselineId,
            TaskSourceId,
            TaskVersionId,
            Digest,
            EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId,
            EnrollmentLifecyclePolicy.RestrictedPreservationVersion,
            ParticipantId,
            AdministratorId,
            Now);

        Assert.True(created.Succeeded);
        Assert.Equal(EnrollmentStates.Active, created.Value!.Status);
        Assert.Equal(1, created.Value.Revision);
        Assert.Equal(BaselineId, created.Value.BaselineId);
        Assert.Equal(EnrollmentVisibilityStates.Current, created.Value.VisibilityForParticipant());
        Assert.True(created.Value.PermitsNewIntakeOrStart());
    }

    [Fact]
    public void Create_fails_for_unknown_lifecycle_policy()
    {
        var created = Enrollment.Create(
            Guid.CreateVersion7(),
            OrganizationId,
            ActivityId,
            CohortId,
            BaselineId,
            TaskSourceId,
            TaskVersionId,
            Digest,
            Guid.CreateVersion7(),
            1,
            ParticipantId,
            AdministratorId,
            Now);

        Assert.False(created.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.MissingLifecyclePolicy, created.OutcomeCode);
    }

    [Theory]
    [InlineData(EnrollmentStates.Active, EnrollmentStates.Suspended, EnrollmentReasonCodes.TemporaryRestriction, EnrollmentOutcomes.Suspended, false)]
    [InlineData(EnrollmentStates.Suspended, EnrollmentStates.Active, EnrollmentReasonCodes.RestrictionRemoved, EnrollmentOutcomes.Restored, true)]
    [InlineData(EnrollmentStates.Active, EnrollmentStates.Closed, EnrollmentReasonCodes.ActivityOrEnrollmentEnd, EnrollmentOutcomes.Closed, false)]
    [InlineData(EnrollmentStates.Active, EnrollmentStates.Revoked, EnrollmentReasonCodes.AccessRevoked, EnrollmentOutcomes.Revoked, false)]
    public void Allowed_transitions_update_state_visibility_and_intake_authority(
        string from,
        string to,
        string reason,
        string outcome,
        bool permitsIntake)
    {
        var enrollment = Seed(from);
        var result = enrollment.Transition(to, reason, 1, Now);
        Assert.True(result.Succeeded);
        Assert.Equal(outcome, result.OutcomeCode);
        Assert.Equal(2, result.Value!.Revision);
        Assert.Equal(permitsIntake, result.Value.PermitsNewIntakeOrStart());
        Assert.Equal(
            to is EnrollmentStates.Closed or EnrollmentStates.Revoked
                ? EnrollmentVisibilityStates.Unavailable
                : to == EnrollmentStates.Suspended
                    ? EnrollmentVisibilityStates.Restricted
                    : EnrollmentVisibilityStates.Current,
            result.Value.VisibilityForParticipant());
    }

    [Fact]
    public void Terminal_history_rejects_further_mutation()
    {
        var closed = Seed(EnrollmentStates.Closed);
        var result = closed.Transition(EnrollmentStates.Active, EnrollmentReasonCodes.RestrictionRemoved, 1, Now);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Terminal, result.OutcomeCode);
        Assert.Equal(EnrollmentStates.Closed, closed.Status);
    }

    [Fact]
    public void Stale_revision_does_not_overwrite_current_state()
    {
        var enrollment = Seed(EnrollmentStates.Active);
        var result = enrollment.Transition(
            EnrollmentStates.Suspended,
            EnrollmentReasonCodes.TemporaryRestriction,
            99,
            Now);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.StaleRevision, result.OutcomeCode);
    }

    [Fact]
    public void Reason_category_must_match_the_requested_transition()
    {
        var enrollment = Seed(EnrollmentStates.Active);
        var result = enrollment.Transition(
            EnrollmentStates.Suspended,
            EnrollmentReasonCodes.AccessRevoked,
            1,
            Now);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.InvalidReason, result.OutcomeCode);
    }

    [Fact]
    public async Task Equivalent_assignment_retries_return_the_same_enrollment()
    {
        var harness = CreateHarness();
        var first = await harness.Coordinator.AssignAsync(AssignCommand("key-1"), TestContext.Current.CancellationToken);
        var second = await harness.Coordinator.AssignAsync(AssignCommand("key-1"), TestContext.Current.CancellationToken);
        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.EnrollmentId, second.EnrollmentId);
        Assert.Equal(EnrollmentOutcomes.Assigned, second.OutcomeCode);
        Assert.Single(harness.Store.Items);
    }

    [Fact]
    public async Task Distinct_key_same_live_cohort_deduplicates_without_a_second_row()
    {
        var harness = CreateHarness();
        var first = await harness.Coordinator.AssignAsync(AssignCommand("key-a"), TestContext.Current.CancellationToken);
        var second = await harness.Coordinator.AssignAsync(AssignCommand("key-b"), TestContext.Current.CancellationToken);
        Assert.Equal(EnrollmentOutcomes.Assigned, first.OutcomeCode);
        Assert.Equal(EnrollmentOutcomes.Deduplicated, second.OutcomeCode);
        Assert.Equal(first.EnrollmentId, second.EnrollmentId);
        Assert.Single(harness.Store.Items);
    }

    [Fact]
    public async Task Digest_mismatch_is_rejected_without_creating_enrollment()
    {
        var harness = CreateHarness();
        var command = AssignCommand("key-mismatch") with { TrustedCommandDigest = new string('b', 64) };
        var result = await harness.Coordinator.AssignAsync(command, TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.IdempotencyConflict, result.OutcomeCode);
        Assert.Empty(harness.Store.Items);
    }

    [Fact]
    public async Task Live_enrollment_in_another_cohort_is_a_safe_conflict()
    {
        var harness = CreateHarness();
        await harness.Coordinator.AssignAsync(AssignCommand("key-1"), TestContext.Current.CancellationToken);
        harness.Cohorts.Binding = Binding() with { CohortId = OtherCohortId };
        var conflict = await harness.Coordinator.AssignAsync(
            AssignCommand("key-2", OtherCohortId),
            TestContext.Current.CancellationToken);
        Assert.False(conflict.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Conflict, conflict.OutcomeCode);
        Assert.Single(harness.Store.Items);
    }

    [Fact]
    public async Task New_identity_is_created_after_terminal_state()
    {
        var harness = CreateHarness();
        var assigned = await harness.Coordinator.AssignAsync(AssignCommand("key-1"), TestContext.Current.CancellationToken);
        var closed = await harness.Coordinator.MutateAsync(
            LifecycleCommand(
                EnrollmentOperationKinds.Close,
                EnrollmentReasonCodes.ActivityOrEnrollmentEnd,
                assigned.EnrollmentId!.Value,
                assigned.Revision!.Value,
                "close-1"),
            TestContext.Current.CancellationToken);
        Assert.True(closed.Succeeded);
        var next = await harness.Coordinator.AssignAsync(AssignCommand("key-2"), TestContext.Current.CancellationToken);
        Assert.True(next.Succeeded);
        Assert.Equal(EnrollmentOutcomes.Assigned, next.OutcomeCode);
        Assert.NotEqual(assigned.EnrollmentId, next.EnrollmentId);
        Assert.Equal(2, harness.Store.Items.Count);
    }

    [Fact]
    public async Task Replay_after_authorization_loss_denies_without_changing_the_committed_row()
    {
        var harness = CreateHarness();
        var assigned = await harness.Coordinator.AssignAsync(AssignCommand("key-1"), TestContext.Current.CancellationToken);
        harness.Authorization.Permit = false;
        var replay = await harness.Coordinator.AssignAsync(AssignCommand("key-1"), TestContext.Current.CancellationToken);
        Assert.False(replay.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Denied, replay.OutcomeCode);
        Assert.Equal(assigned.EnrollmentId, harness.Store.Items[0].EnrollmentId);
        Assert.Equal(EnrollmentStates.Active, harness.Store.Items[0].Status);
    }

    [Fact]
    public async Task Audit_failure_does_not_project_success()
    {
        var harness = CreateHarness();
        harness.UnitOfWork.AuditAccepted = false;
        var result = await harness.Coordinator.AssignAsync(AssignCommand("key-1"), TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.AuditUnavailable, result.OutcomeCode);
    }

    [Fact]
    public async Task Suspended_assignment_remains_visible_without_open_action()
    {
        var harness = CreateHarness();
        var assigned = await harness.Coordinator.AssignAsync(AssignCommand("key-1"), TestContext.Current.CancellationToken);
        await harness.Coordinator.MutateAsync(
            LifecycleCommand(
                EnrollmentOperationKinds.Suspend,
                EnrollmentReasonCodes.TemporaryRestriction,
                assigned.EnrollmentId!.Value,
                assigned.Revision!.Value,
                "suspend-1"),
            TestContext.Current.CancellationToken);

        var page = await harness.Queries.ListMyWorkAsync(
            ParticipantContext(),
            null,
            20,
            TestContext.Current.CancellationToken);
        Assert.True(page.Succeeded);
        var item = Assert.Single(page.Value!.Items);
        Assert.Equal(EnrollmentStates.Suspended, item.Status);
        Assert.Equal(EnrollmentVisibilityStates.Restricted, item.Visibility);
        Assert.Equal([EnrollmentClientActions.ReturnToMyWork], item.PermittedActions);
        Assert.False(harness.Store.Items[0].PermitsNewIntakeOrStart());
    }

    [Fact]
    public async Task Closed_assignment_is_unavailable_to_the_participant()
    {
        var harness = CreateHarness();
        var assigned = await harness.Coordinator.AssignAsync(AssignCommand("key-1"), TestContext.Current.CancellationToken);
        await harness.Coordinator.MutateAsync(
            LifecycleCommand(
                EnrollmentOperationKinds.Close,
                EnrollmentReasonCodes.ActivityOrEnrollmentEnd,
                assigned.EnrollmentId!.Value,
                assigned.Revision!.Value,
                "close-1"),
            TestContext.Current.CancellationToken);

        var page = await harness.Queries.ListMyWorkAsync(
            ParticipantContext(),
            null,
            20,
            TestContext.Current.CancellationToken);
        Assert.Empty(page.Value!.Items);
        var detail = await harness.Queries.GetMyWorkAsync(
            ParticipantContext(),
            assigned.EnrollmentId!.Value,
            TestContext.Current.CancellationToken);
        Assert.False(detail.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Denied, detail.OutcomeCode);
    }

    [Fact]
    public async Task Degraded_activation_verification_fails_assignment()
    {
        var harness = CreateHarness();
        harness.Cohorts.Binding = Binding() with { VerificationDegraded = true };
        var result = await harness.Coordinator.AssignAsync(AssignCommand("key-degraded"), TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Unavailable, result.OutcomeCode);
        Assert.Empty(harness.Store.Items);
    }

    [Fact]
    public async Task Successful_assignment_audits_the_created_enrollment()
    {
        var harness = CreateHarness();
        var assigned = await harness.Coordinator.AssignAsync(AssignCommand("key-audit"), TestContext.Current.CancellationToken);
        Assert.True(assigned.Succeeded);
        Assert.Equal(assigned.EnrollmentId, harness.Audit.LastResourceId);
        Assert.Equal(EnrollmentResourceTypes.Enrollment, harness.Audit.LastResourceType);
    }

    [Fact]
    public async Task Stale_application_session_denies_assignment()
    {
        var harness = CreateHarness();
        harness.Sessions.Permit = false;
        var result = await harness.Coordinator.AssignAsync(AssignCommand("key-session"), TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Denied, result.OutcomeCode);
        Assert.Empty(harness.Store.Items);
    }

    [Fact]
    public async Task Session_expiry_after_the_early_lock_denies_assignment()
    {
        var harness = CreateHarness();
        harness.Sessions.ConfirmPermit = false;
        var result = await harness.Coordinator.AssignAsync(AssignCommand("key-session-confirm"), TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Denied, result.OutcomeCode);
        Assert.Empty(harness.Store.Items);
        Assert.True(harness.Sessions.RevalidateCount >= 1);
        Assert.True(harness.Sessions.ConfirmCount >= 1);
    }

    [Fact]
    public async Task Session_expiry_after_the_early_lock_denies_lifecycle()
    {
        var harness = CreateHarness();
        var assigned = await harness.Coordinator.AssignAsync(AssignCommand("key-session-life"), TestContext.Current.CancellationToken);
        Assert.True(assigned.Succeeded);
        harness.Sessions.ConfirmPermit = false;
        var result = await harness.Coordinator.MutateAsync(
            LifecycleCommand(
                EnrollmentOperationKinds.Suspend,
                EnrollmentReasonCodes.TemporaryRestriction,
                assigned.EnrollmentId!.Value,
                assigned.Revision!.Value,
                "suspend-session-confirm"),
            TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Denied, result.OutcomeCode);
        Assert.Equal(EnrollmentStates.Active, harness.Store.Items[0].Status);
        Assert.True(harness.Sessions.ConfirmCount >= 2);
    }

    [Fact]
    public async Task Session_expiry_after_replay_enrollment_read_denies_assignment()
    {
        var harness = CreateHarness();
        var assigned = await harness.Coordinator.AssignAsync(AssignCommand("key-replay"), TestContext.Current.CancellationToken);
        Assert.True(assigned.Succeeded, assigned.OutcomeCode);
        harness.Sessions.ConfirmWhen = () => harness.Store.TransactionalFindCount == 0;
        var replayed = await harness.Coordinator.AssignAsync(AssignCommand("key-replay"), TestContext.Current.CancellationToken);
        Assert.False(replayed.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Denied, replayed.OutcomeCode);
        Assert.True(harness.Store.TransactionalFindCount >= 1);
    }

    [Fact]
    public async Task Session_expiry_at_pre_commit_denies_assignment()
    {
        var harness = CreateHarness();
        harness.Sessions.ConfirmWhen = () => harness.Sessions.ConfirmCount == 1;
        var result = await harness.Coordinator.AssignAsync(AssignCommand("key-pre-commit"), TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Denied, result.OutcomeCode);
        Assert.True(harness.Sessions.ConfirmCount >= 2);
        Assert.Empty(harness.Store.Items);
        Assert.Empty(harness.Store.Events);
        Assert.Empty(harness.Operations.Items);
        Assert.Equal(0, harness.Audit.RequiredWrites);
        Assert.Equal(0, harness.Audit.AvailabilityWrites);
    }

    [Fact]
    public async Task Lifecycle_authorization_uses_the_enrollment_resource_type()
    {
        var harness = CreateHarness();
        var assigned = await harness.Coordinator.AssignAsync(AssignCommand("key-auth"), TestContext.Current.CancellationToken);
        Assert.Equal(EnrollmentResourceTypes.Cohort, harness.Authorization.LastResourceType);
        await harness.Coordinator.MutateAsync(
            LifecycleCommand(
                EnrollmentOperationKinds.Suspend,
                EnrollmentReasonCodes.TemporaryRestriction,
                assigned.EnrollmentId!.Value,
                assigned.Revision!.Value,
                "suspend-auth"),
            TestContext.Current.CancellationToken);
        Assert.Equal(EnrollmentResourceTypes.Enrollment, harness.Authorization.LastResourceType);
    }

    [Fact]
    public async Task Zero_row_lifecycle_update_returns_stale_revision()
    {
        var harness = CreateHarness();
        var assigned = await harness.Coordinator.AssignAsync(AssignCommand("key-stale"), TestContext.Current.CancellationToken);
        harness.Store.ForceStaleUpdate = true;
        var result = await harness.Coordinator.MutateAsync(
            LifecycleCommand(
                EnrollmentOperationKinds.Suspend,
                EnrollmentReasonCodes.TemporaryRestriction,
                assigned.EnrollmentId!.Value,
                assigned.Revision!.Value,
                "suspend-stale"),
            TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.StaleRevision, result.OutcomeCode);
        Assert.Equal(EnrollmentStates.Active, harness.Store.Items[0].Status);
    }

    [Fact]
    public void Command_digest_changes_when_participant_or_reason_changes()
    {
        var first = EnrollmentCommandDigest.Compute(
            EnrollmentOperationKinds.Assign,
            OrganizationId,
            ActivityId,
            CohortId,
            null,
            ParticipantId,
            null,
            null);
        var second = EnrollmentCommandDigest.Compute(
            EnrollmentOperationKinds.Assign,
            OrganizationId,
            ActivityId,
            CohortId,
            null,
            Guid.CreateVersion7(),
            null,
            null);
        Assert.NotEqual(first, second);
    }

    private static Enrollment Seed(string status)
    {
        var created = Enrollment.Create(
            Guid.CreateVersion7(),
            OrganizationId,
            ActivityId,
            CohortId,
            BaselineId,
            TaskSourceId,
            TaskVersionId,
            Digest,
            EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId,
            EnrollmentLifecyclePolicy.RestrictedPreservationVersion,
            ParticipantId,
            AdministratorId,
            Now).Value!;
        return created with { Status = status };
    }

    private static Harness CreateHarness()
    {
        var store = new InMemoryEnrollmentStore();
        var operations = new InMemoryEnrollmentOperationStore();
        var authorization = new AllowEnrollmentAuthorizationPort();
        var cohorts = new FixedActivatedCohortPort { Binding = Binding() };
        var candidates = new InMemoryCandidatePort();
        candidates.Candidates.Add(new EnrollmentCandidate(ParticipantId, "Synthetic Participant"));
        var audit = new RecordingEnrollmentAuditPort();
        var sessions = new AllowEnrollmentSessionPort();
        var unitOfWork = new InMemoryEnrollmentUnitOfWork(sessions, store, operations, audit);
        var coordinator = new EnrollmentCoordinator(
            authorization,
            cohorts,
            candidates,
            store,
            operations,
            audit,
            unitOfWork,
            sessions,
            new FixedEnrollmentClock(Now));
        var queries = new EnrollmentQueryService(authorization, cohorts, candidates, store);
        return new Harness(coordinator, queries, store, authorization, cohorts, unitOfWork, audit, sessions, operations);
    }

    private static AssignEnrollmentCommand AssignCommand(string key, Guid? cohortId = null)
    {
        var cohort = cohortId ?? CohortId;
        return new AssignEnrollmentCommand(
            AdministratorContext(),
            ActivityId,
            cohort,
            ParticipantId,
            key,
            EnrollmentCommandDigest.Compute(
                EnrollmentOperationKinds.Assign,
                OrganizationId,
                ActivityId,
                cohort,
                null,
                ParticipantId,
                null,
                null));
    }

    private static EnrollmentLifecycleCommand LifecycleCommand(
        string operation,
        string reason,
        Guid enrollmentId,
        long revision,
        string key) =>
        new(
            AdministratorContext(),
            ActivityId,
            CohortId,
            enrollmentId,
            operation,
            reason,
            revision,
            key,
            EnrollmentCommandDigest.Compute(
                operation,
                OrganizationId,
                ActivityId,
                CohortId,
                enrollmentId,
                null,
                reason,
                revision));

    private static EnrollmentActorContext AdministratorContext() =>
        new(
            new TrustedActor(AdministratorId, HumanInteractiveActorTypes.Interactive),
            new OrganizationScope(OrganizationId),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.CreateVersion7(),
            "https",
            [
                EnrollmentAuthorizationActions.Assign,
                EnrollmentAuthorizationActions.List,
                EnrollmentAuthorizationActions.Read,
                EnrollmentAuthorizationActions.Suspend,
                EnrollmentAuthorizationActions.Restore,
                EnrollmentAuthorizationActions.Close,
                EnrollmentAuthorizationActions.Revoke,
            ],
            Guid.CreateVersion7());

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
            BaselineId,
            Digest,
            "activated",
            TaskSourceId,
            TaskVersionId,
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

    private sealed record Harness(
        EnrollmentCoordinator Coordinator,
        EnrollmentQueryService Queries,
        InMemoryEnrollmentStore Store,
        AllowEnrollmentAuthorizationPort Authorization,
        FixedActivatedCohortPort Cohorts,
        InMemoryEnrollmentUnitOfWork UnitOfWork,
        RecordingEnrollmentAuditPort Audit,
        AllowEnrollmentSessionPort Sessions,
        InMemoryEnrollmentOperationStore Operations);
}
