using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class AccommodationCoordinatorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-22T06:00:00Z");
    private static readonly Guid OrganizationId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid ActivityId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid CohortId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2");
    private static readonly Guid ParticipantId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
    private static readonly Guid AdministratorId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2");
    private static readonly Guid ApproverId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb8");

    [Fact]
    public async Task Grant_inside_routine_bounds_changes_effective_deadline_without_changing_baseline()
    {
        var harness = await AssignedHarnessAsync();
        var enrollmentId = harness.EnrollmentId;
        var requested = Now.AddDays(22);
        var granted = await harness.Accommodations.GrantAsync(
            GrantCommand(enrollmentId, 1, "grant-1", Format(requested), fairness: false),
            TestContext.Current.CancellationToken);

        Assert.True(granted.Succeeded, granted.OutcomeCode);
        Assert.Equal(AccommodationStates.Granted, granted.Status);

        var detail = await harness.Timing.GetEnrollmentTimingAsync(
            Administrator(),
            ActivityId,
            CohortId,
            enrollmentId,
            TestContext.Current.CancellationToken);
        Assert.True(detail.Succeeded);
        Assert.Equal(requested, detail.Value!.Timing.EffectiveSubmissionExclusiveEndUtc);
        Assert.Equal(Now.AddDays(20), detail.Value.Timing.Baseline.DeadlineUtc);
        Assert.Equal(granted.AccommodationId, Assert.Single(detail.Value!.Timing.CurrentAccommodations).AccommodationId);

        var replayed = await harness.Accommodations.GrantAsync(
            GrantCommand(enrollmentId, 1, "grant-1", Format(requested), fairness: false),
            TestContext.Current.CancellationToken);
        Assert.True(replayed.Succeeded, replayed.OutcomeCode);
        Assert.Equal(granted.AccommodationId, replayed.AccommodationId);
    }

    [Fact]
    public async Task Fairness_exception_stays_pending_until_a_distinct_approver_decides()
    {
        var harness = await AssignedHarnessAsync();
        var enrollmentId = harness.EnrollmentId;
        var requested = Now.AddDays(40);
        var pending = await harness.Accommodations.GrantAsync(
            GrantCommand(enrollmentId, 1, "grant-fair", Format(requested), fairness: true),
            TestContext.Current.CancellationToken);
        Assert.True(pending.Succeeded, pending.OutcomeCode);
        Assert.Equal(AccommodationStates.PendingApproval, pending.Status);

        var self = await harness.Accommodations.DecideAsync(
            DecideCommand(enrollmentId, pending.AccommodationId!.Value, 1, "decide-self", true, Administrator()),
            TestContext.Current.CancellationToken);
        Assert.False(self.Succeeded);
        Assert.Equal(AccommodationFailureCodes.DistinctApproverRequired, self.OutcomeCode);

        var approved = await harness.Accommodations.DecideAsync(
            DecideCommand(enrollmentId, pending.AccommodationId.Value, 1, "decide-ok", true, Approver()),
            TestContext.Current.CancellationToken);
        Assert.True(approved.Succeeded, approved.OutcomeCode);
        Assert.Equal(AccommodationStates.Granted, approved.Status);
    }

    [Fact]
    public async Task Enrollment_read_without_accommodation_read_omits_accommodation_history()
    {
        var harness = await AssignedHarnessAsync();
        var requested = Now.AddDays(22);
        var granted = await harness.Accommodations.GrantAsync(
            GrantCommand(harness.EnrollmentId, 1, "grant-history", Format(requested), fairness: false),
            TestContext.Current.CancellationToken);
        Assert.True(granted.Succeeded, granted.OutcomeCode);

        harness.Authorization.DeniedActions.Add(EnrollmentAuthorizationActions.ReadAccommodation);
        var detail = await harness.Timing.GetEnrollmentTimingAsync(
            Administrator(),
            ActivityId,
            CohortId,
            harness.EnrollmentId,
            TestContext.Current.CancellationToken);
        Assert.True(detail.Succeeded);
        Assert.Empty(detail.Value!.History);
        Assert.Empty(detail.Value.Timing.CurrentAccommodations);
        Assert.Equal(requested, detail.Value.Timing.EffectiveSubmissionExclusiveEndUtc);
    }

    [Fact]
    public async Task My_work_timing_uses_none_consequence_when_eligibility_is_not_authoritative()
    {
        var harness = await AssignedHarnessAsync();
        var requested = Now.AddDays(22);
        var granted = await harness.Accommodations.GrantAsync(
            GrantCommand(harness.EnrollmentId, 1, "grant-my-work", Format(requested), fairness: false),
            TestContext.Current.CancellationToken);
        Assert.True(granted.Succeeded, granted.OutcomeCode);

        var participant = Participant();
        var accommodated = await harness.Timing.GetMyWorkTimingAsync(
            participant,
            harness.EnrollmentId,
            TestContext.Current.CancellationToken);
        Assert.True(accommodated.Succeeded);
        Assert.Equal(AccommodationConsequenceCodes.DeadlineReplacement, accommodated.Value!.ParticipantConsequenceCode);
        Assert.Equal(AccommodationConsequenceCodes.DeadlineReplacement, accommodated.Value.Timing!.ParticipantConsequenceCode);

        var baseline = TimingMapper.BaselineFrom(Binding());
        harness.Policies.Policy = DevelopmentAccommodationPolicy.Create(OrganizationId, baseline, "development") with
        {
            EnvironmentEligible = false,
        };
        var closed = await harness.Timing.GetMyWorkTimingAsync(
            participant,
            harness.EnrollmentId,
            TestContext.Current.CancellationToken);
        Assert.True(closed.Succeeded);
        Assert.False(closed.Value!.Timing!.IsAuthoritativeEligibility);
        Assert.Equal(AccommodationConsequenceCodes.None, closed.Value.ParticipantConsequenceCode);
        Assert.Equal(AccommodationConsequenceCodes.None, closed.Value.Timing.ParticipantConsequenceCode);
    }

    [Fact]
    public async Task Same_idempotency_key_with_a_different_expiry_conflicts()
    {
        var harness = await AssignedHarnessAsync();
        var requested = Now.AddDays(22);
        var first = await harness.Accommodations.GrantAsync(
            GrantCommand(harness.EnrollmentId, 1, "grant-expiry", Format(requested), fairness: false, Now.AddDays(5)),
            TestContext.Current.CancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);

        var second = await harness.Accommodations.GrantAsync(
            GrantCommand(harness.EnrollmentId, 1, "grant-expiry", Format(requested), fairness: false, Now.AddDays(10)),
            TestContext.Current.CancellationToken);
        Assert.False(second.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.IdempotencyConflict, second.OutcomeCode);
    }

    [Fact]
    public async Task Current_policy_widening_cannot_exceed_frozen_baseline_bounds()
    {
        var harness = await AssignedHarnessAsync();
        var baseline = TimingMapper.BaselineFrom(harness.Cohorts.Binding!);
        var frozen = DevelopmentAccommodationPolicy.Create(OrganizationId, baseline, "development");
        var tightDeadline = AccommodationPolicyNormalizer.FormatInstant(Now.AddDays(22));
        harness.Cohorts.Binding = Binding() with
        {
            FrozenAccommodationPolicy = frozen with
            {
                Dimensions = new Dictionary<string, AccommodationDimensionBounds>(frozen.Dimensions, StringComparer.Ordinal)
                {
                    [AccommodationDimensions.SubmissionDeadlineUtc] = frozen.Dimensions[AccommodationDimensions.SubmissionDeadlineUtc] with
                    {
                        RoutineMax = tightDeadline,
                        HardMax = tightDeadline,
                    },
                },
            },
        };
        var denied = await harness.Accommodations.GrantAsync(
            GrantCommand(harness.EnrollmentId, 1, "grant-wide", Format(Now.AddDays(30)), fairness: false),
            TestContext.Current.CancellationToken);
        Assert.False(denied.Succeeded);
        Assert.Equal(AccommodationFailureCodes.OutsideBounds, denied.OutcomeCode);
    }

    [Fact]
    public async Task Frozen_snapshot_with_wrong_version_or_digest_fails_closed()
    {
        var harness = await AssignedHarnessAsync();
        var matchedBaseline = TimingMapper.BaselineFrom(Binding());
        var snapshot = DevelopmentAccommodationPolicy.Create(OrganizationId, matchedBaseline, "development");
        harness.Cohorts.Binding = Binding() with
        {
            FrozenPolicySourceId = snapshot.Identity.PolicyId,
            FrozenPolicyVersionId = Guid.Parse("99999999-9999-4999-8999-999999999901"),
            FrozenPolicyDigest = snapshot.Identity.Digest,
            FrozenAccommodationPolicy = snapshot,
        };
        var wrongVersion = TimingMapper.BaselineFrom(harness.Cohorts.Binding);
        Assert.True(wrongVersion.VerificationDegraded);
        Assert.Null(wrongVersion.FrozenPolicySnapshot);
        var deniedVersion = await harness.Accommodations.GrantAsync(
            GrantCommand(harness.EnrollmentId, 1, "grant-id-version", Format(Now.AddDays(22)), fairness: false),
            TestContext.Current.CancellationToken);
        Assert.False(deniedVersion.Succeeded);
        Assert.Equal(AccommodationFailureCodes.PolicyUnavailable, deniedVersion.OutcomeCode);

        var timing = await harness.Timing.GetEnrollmentTimingAsync(
            Administrator(),
            ActivityId,
            CohortId,
            harness.EnrollmentId,
            TestContext.Current.CancellationToken);
        Assert.True(timing.Succeeded);
        Assert.False(timing.Value!.PolicyAvailable);
        Assert.DoesNotContain(EnrollmentClientActions.RequestAccommodation, timing.Value.Summary.PermittedActions);
        Assert.DoesNotContain(EnrollmentClientActions.ApproveException, timing.Value.Summary.PermittedActions);
        Assert.DoesNotContain(EnrollmentClientActions.RejectException, timing.Value.Summary.PermittedActions);
        Assert.Contains(EnrollmentClientActions.RevokeAccommodation, timing.Value.Summary.PermittedActions);
        Assert.Empty(timing.Value.PermittedAccommodationDimensions);
        Assert.Empty(timing.Value.PermittedReasonCategories);

        harness.Cohorts.Binding = Binding() with
        {
            FrozenPolicySourceId = snapshot.Identity.PolicyId,
            FrozenPolicyVersionId = snapshot.Identity.VersionId,
            FrozenPolicyDigest = new string('c', 64),
            FrozenAccommodationPolicy = snapshot,
        };
        var wrongDigest = TimingMapper.BaselineFrom(harness.Cohorts.Binding);
        Assert.True(wrongDigest.VerificationDegraded);
        Assert.Null(wrongDigest.FrozenPolicySnapshot);
        var deniedDigest = await harness.Accommodations.GrantAsync(
            GrantCommand(harness.EnrollmentId, 1, "grant-id-digest", Format(Now.AddDays(22)), fairness: false),
            TestContext.Current.CancellationToken);
        Assert.False(deniedDigest.Succeeded);
        Assert.Equal(AccommodationFailureCodes.PolicyUnavailable, deniedDigest.OutcomeCode);
    }

    [Fact]
    public async Task Revoke_while_policy_is_unavailable_omits_policy_dependent_actions()
    {
        var harness = await AssignedHarnessAsync();
        var granted = await harness.Accommodations.GrantAsync(
            GrantCommand(harness.EnrollmentId, 1, "grant-then-revoke", Format(Now.AddDays(22)), fairness: false),
            TestContext.Current.CancellationToken);
        Assert.True(granted.Succeeded, granted.OutcomeCode);
        Assert.Contains(EnrollmentClientActions.RequestAccommodation, granted.PermittedActions);

        var baseline = TimingMapper.BaselineFrom(Binding());
        var unavailable = DevelopmentAccommodationPolicy.Create(OrganizationId, baseline, "development") with
        {
            EnvironmentEligible = false,
        };
        harness.Policies.Policy = unavailable;
        var deniedGrant = await harness.Accommodations.GrantAsync(
            GrantCommand(harness.EnrollmentId, 1, "grant-while-unavailable", Format(Now.AddDays(23)), fairness: false),
            TestContext.Current.CancellationToken);
        Assert.False(deniedGrant.Succeeded);
        Assert.Equal(AccommodationFailureCodes.PolicyUnavailable, deniedGrant.OutcomeCode);

        var revoked = await harness.Accommodations.RevokeAsync(
            RevokeCommand(harness.EnrollmentId, granted.AccommodationId!.Value, granted.Revision!.Value, "revoke-unavailable"),
            TestContext.Current.CancellationToken);
        Assert.True(revoked.Succeeded, revoked.OutcomeCode);
        Assert.Contains(EnrollmentClientActions.RevokeAccommodation, revoked.PermittedActions);
        Assert.DoesNotContain(EnrollmentClientActions.RequestAccommodation, revoked.PermittedActions);
        Assert.DoesNotContain(EnrollmentClientActions.ApproveException, revoked.PermittedActions);
        Assert.DoesNotContain(EnrollmentClientActions.RejectException, revoked.PermittedActions);
    }

    private async Task<TimingHarness> AssignedHarnessAsync()
    {
        var store = new InMemoryEnrollmentStore();
        var operations = new InMemoryEnrollmentOperationStore();
        var accommodations = new InMemoryAccommodationStore();
        var authorization = new AllowEnrollmentAuthorizationPort();
        var cohorts = new FixedActivatedCohortPort { Binding = Binding() };
        var candidates = new InMemoryCandidatePort();
        candidates.Candidates.Add(new EnrollmentCandidate(ParticipantId, "Synthetic Participant"));
        var audit = new RecordingEnrollmentAuditPort();
        var sessions = new AllowEnrollmentSessionPort();
        var unitOfWork = new InMemoryEnrollmentUnitOfWork(sessions, store, operations, audit);
        var enrollmentCoordinator = new EnrollmentCoordinator(
            authorization,
            cohorts,
            candidates,
            store,
            operations,
            audit,
            unitOfWork,
            sessions,
            new FixedEnrollmentClock(Now));
        var assigned = await enrollmentCoordinator.AssignAsync(
            new AssignEnrollmentCommand(
                Administrator(),
                ActivityId,
                CohortId,
                ParticipantId,
                "assign-1",
                EnrollmentCommandDigest.Compute(
                    EnrollmentOperationKinds.Assign,
                    OrganizationId,
                    ActivityId,
                    CohortId,
                    null,
                    ParticipantId,
                    null,
                    null)),
            TestContext.Current.CancellationToken);
        Assert.True(assigned.Succeeded, assigned.OutcomeCode);
        var policies = new FixedAccommodationPolicyPort();
        var accommodationCoordinator = new AccommodationCoordinator(
            authorization,
            cohorts,
            store,
            accommodations,
            operations,
            audit,
            unitOfWork,
            sessions,
            policies,
            new FixedEnrollmentClock(Now));
        var timing = new EnrollmentTimingQueryService(
            authorization,
            cohorts,
            store,
            accommodations,
            policies,
            new FixedEnrollmentClock(Now));
        return new TimingHarness(assigned.EnrollmentId!.Value, accommodationCoordinator, timing, authorization, policies, cohorts);
    }

    private static GrantAccommodationCommand GrantCommand(
        Guid enrollmentId,
        long revision,
        string key,
        string value,
        bool fairness,
        DateTimeOffset? expiresAtUtc = null) =>
        new(
            Administrator(),
            ActivityId,
            CohortId,
            enrollmentId,
            AccommodationDimensions.SubmissionDeadlineUtc,
            value,
            AccommodationReasonCategories.DevelopmentSynthetic,
            expiresAtUtc,
            fairness,
            revision,
            key,
            AccommodationCommandDigest.Compute(
                AccommodationOperationKinds.Grant,
                OrganizationId,
                ActivityId,
                CohortId,
                enrollmentId,
                null,
                AccommodationDimensions.SubmissionDeadlineUtc,
                value,
                AccommodationReasonCategories.DevelopmentSynthetic,
                fairness,
                revision,
                expiresAtUtc));

    private static DecideAccommodationCommand DecideCommand(
        Guid enrollmentId,
        Guid accommodationId,
        long revision,
        string key,
        bool approve,
        EnrollmentActorContext actor) =>
        new(
            actor,
            ActivityId,
            CohortId,
            enrollmentId,
            accommodationId,
            approve,
            revision,
            key,
            AccommodationCommandDigest.Compute(
                AccommodationOperationKinds.Decide,
                OrganizationId,
                ActivityId,
                CohortId,
                enrollmentId,
                accommodationId,
                null,
                null,
                null,
                approve,
                revision));

    private static RevokeAccommodationCommand RevokeCommand(
        Guid enrollmentId,
        Guid accommodationId,
        long revision,
        string key) =>
        new(
            Administrator(),
            ActivityId,
            CohortId,
            enrollmentId,
            accommodationId,
            revision,
            key,
            AccommodationCommandDigest.Compute(
                AccommodationOperationKinds.Revoke,
                OrganizationId,
                ActivityId,
                CohortId,
                enrollmentId,
                accommodationId,
                null,
                null,
                null,
                false,
                revision));

    private static EnrollmentActorContext Participant() =>
        Actor(ParticipantId, "organization.member", [
            EnrollmentAuthorizationActions.Discover,
        ]);

    private static EnrollmentActorContext Administrator() =>
        Actor(AdministratorId, AuthenticationStrengthEvaluator.AdministratorRelationship, [
            EnrollmentAuthorizationActions.Assign,
            EnrollmentAuthorizationActions.Read,
            EnrollmentAuthorizationActions.GrantAccommodation,
            EnrollmentAuthorizationActions.DecideAccommodation,
            EnrollmentAuthorizationActions.RevokeAccommodation,
            EnrollmentAuthorizationActions.ReadAccommodation,
        ]);

    private static EnrollmentActorContext Approver() =>
        Actor(ApproverId, AuthenticationStrengthEvaluator.AdministratorRelationship, [
            EnrollmentAuthorizationActions.DecideAccommodation,
            EnrollmentAuthorizationActions.Read,
        ]);

    private static EnrollmentActorContext Actor(Guid actorId, string relationship, IReadOnlyList<string> actions) =>
        new(
            new TrustedActor(actorId, HumanInteractiveActorTypes.Interactive),
            new OrganizationScope(OrganizationId),
            relationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.CreateVersion7(),
            "https",
            actions,
            Guid.CreateVersion7());

    private static ActivatedCohortBinding Binding() =>
        new(
            OrganizationId,
            ActivityId,
            CohortId,
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa4"),
            new string('a', 64),
            "activated",
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa5"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa6"),
            new string('a', 64),
            "Campaign",
            "Task",
            "America/New_York",
            Now,
            Now.AddDays(30),
            Now.AddDays(20),
            EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId,
            EnrollmentLifecyclePolicy.RestrictedPreservationVersion,
            false,
            2,
            3600);

    private static string Format(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

    private sealed record TimingHarness(
        Guid EnrollmentId,
        IAccommodationCoordinator Accommodations,
        IEnrollmentTimingQueryService Timing,
        AllowEnrollmentAuthorizationPort Authorization,
        FixedAccommodationPolicyPort Policies,
        FixedActivatedCohortPort Cohorts);
}
