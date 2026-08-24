using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class AccommodationDomainTests
{
    private static readonly DateTimeOffset Start = AccommodationDomainTestsSupport.Start;
    private static readonly DateTimeOffset Deadline = AccommodationDomainTestsSupport.Deadline;
    private static readonly DateTimeOffset Ends = AccommodationDomainTestsSupport.Ends;

    [Fact]
    public void Routine_grant_stores_one_normalized_replacement_inside_intersected_bounds()
    {
        var requested = Deadline.AddDays(2);
        var created = Accommodation.Request(
            AccommodationDomainTestsSupport.Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(requested),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(14), Ends.AddDays(7), 7200),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            null,
            Guid.CreateVersion7(),
            1);

        Assert.True(created.Succeeded);
        Assert.Equal(AccommodationStates.Granted, created.Value!.Status);
        Assert.Equal(Format(requested), created.Value.NormalizedValue);
        Assert.Equal(AccommodationOutcomes.Granted, created.OutcomeCode);
        Assert.Null(created.Value.ApproverActorId);
    }

    [Fact]
    public void Unknown_dimension_free_text_reason_or_production_synthetic_reason_is_rejected()
    {
        Assert.Equal(
            AccommodationFailureCodes.UnsupportedDimension,
            Accommodation.Request(
                AccommodationDomainTestsSupport.Parent(),
                "attempt_limit",
                "3",
                AccommodationDomainTestsSupport.Frozen(),
                AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(14), Ends.AddDays(7), 7200),
                AccommodationReasonCategories.DevelopmentSynthetic,
                Start,
                null,
                Guid.CreateVersion7(),
                1).OutcomeCode);

        Assert.Equal(
            AccommodationFailureCodes.InvalidReason,
            Accommodation.Request(
                AccommodationDomainTestsSupport.Parent(),
                AccommodationDimensions.SubmissionDeadlineUtc,
                Format(Deadline.AddDays(1)),
                AccommodationDomainTestsSupport.Frozen(),
                AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(14), Ends.AddDays(7), 7200),
                "medical.diagnosis",
                Start,
                null,
                Guid.CreateVersion7(),
                1).OutcomeCode);

        var production = AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(14), Ends.AddDays(7), 7200) with
        {
            Environment = "production",
            SyntheticDevelopmentOnly = true,
        };
        Assert.Equal(
            AccommodationFailureCodes.InvalidReason,
            Accommodation.Request(
                AccommodationDomainTestsSupport.Parent(),
                AccommodationDimensions.SubmissionDeadlineUtc,
                Format(Deadline.AddDays(1)),
                AccommodationDomainTestsSupport.Frozen(),
                production,
                AccommodationReasonCategories.DevelopmentSynthetic,
                Start,
                null,
                Guid.CreateVersion7(),
                1).OutcomeCode);
    }

    [Fact]
    public void Value_outside_routine_bounds_requires_a_distinct_approver_and_cannot_self_activate()
    {
        var outsideRoutine = Deadline.AddDays(10);
        var requested = Accommodation.Request(
            AccommodationDomainTestsSupport.Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(outsideRoutine),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(2), Ends.AddDays(14), 7200),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            null,
            Guid.CreateVersion7(),
            1,
            fairnessException: true);

        Assert.True(requested.Succeeded);
        Assert.Equal(AccommodationStates.PendingApproval, requested.Value!.Status);
        Assert.Equal(AccommodationOutcomes.ApprovalRequired, requested.OutcomeCode);

        var self = requested.Value.Decide(
            requested.Value.RequesterActorId,
            approve: true,
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(2), Ends.AddDays(14), 7200),
            requested.Value.Revision,
            Start.AddMinutes(1));
        Assert.False(self.Succeeded);
        Assert.Equal(AccommodationFailureCodes.DistinctApproverRequired, self.OutcomeCode);

        var approved = requested.Value.Decide(
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb9"),
            approve: true,
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(2), Ends.AddDays(14), 7200),
            requested.Value.Revision,
            Start.AddMinutes(1));
        Assert.True(approved.Succeeded);
        Assert.Equal(AccommodationStates.Granted, approved.Value!.Status);
        Assert.Equal(Format(outsideRoutine), approved.Value.NormalizedValue);
        Assert.NotEqual(approved.Value.RequesterActorId, approved.Value.ApproverActorId);
    }

    [Fact]
    public void Approval_cannot_edit_or_widen_the_request_and_stale_revision_is_rejected()
    {
        var requested = Accommodation.Request(
            AccommodationDomainTestsSupport.Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(10)),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(2), Ends.AddDays(14), 7200),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            null,
            Guid.CreateVersion7(),
            1,
            fairnessException: true).Value!;

        var stale = requested.Decide(
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb9"),
            approve: true,
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(2), Ends.AddDays(14), 7200),
            requested.Revision + 1,
            Start.AddMinutes(1));
        Assert.Equal(AccommodationFailureCodes.StaleRevision, stale.OutcomeCode);

        var currentRejects = requested.Decide(
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb9"),
            approve: true,
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(2), Ends.AddDays(3), 7200),
            requested.Revision,
            Start.AddMinutes(1));
        Assert.Equal(AccommodationFailureCodes.OutsideBounds, currentRejects.OutcomeCode);
        Assert.Equal(AccommodationStates.PendingApproval, requested.Status);
    }

    [Fact]
    public void Later_grant_supersedes_rather_than_editing_or_composing_the_prior_value()
    {
        var first = Accommodation.CreateGranted(
            AccommodationDomainTestsSupport.Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(1)),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            null,
            Guid.CreateVersion7(),
            1).Value!;
        var second = Accommodation.CreateGranted(
            AccommodationDomainTestsSupport.Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(3)),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start.AddHours(1),
            null,
            Guid.CreateVersion7(),
            1).Value!;

        var superseded = first.Supersede(second.AccommodationId, Start.AddHours(1));
        Assert.Equal(AccommodationStates.Superseded, superseded.Status);
        Assert.Equal(Format(Deadline.AddDays(1)), superseded.NormalizedValue);
        Assert.Equal(second.AccommodationId, superseded.SupersededByAccommodationId);
        Assert.Equal(Format(Deadline.AddDays(3)), second.NormalizedValue);
    }

    [Fact]
    public void Revocation_preserves_history_and_lifecycle_class_is_independent_of_business_expiry()
    {
        var granted = Accommodation.CreateGranted(
            AccommodationDomainTestsSupport.Parent(),
            AccommodationDimensions.PerAttemptDurationSeconds,
            "5400",
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            Start.AddDays(2),
            Guid.CreateVersion7(),
            1).Value!;
        var revoked = granted.Revoke(Guid.CreateVersion7(), Start.AddHours(3));
        Assert.True(revoked.Succeeded);
        Assert.Equal(AccommodationStates.Revoked, revoked.Value!.Status);
        Assert.Equal("5400", revoked.Value.NormalizedValue);
        Assert.Equal(AccommodationLifecyclePolicy.HistoryRetentionPolicyId, revoked.Value.LifecyclePolicyId);
        Assert.Equal(AccommodationLifecyclePolicy.HistoryRetentionVersion, revoked.Value.LifecyclePolicyVersion);
        Assert.NotEqual(EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId, revoked.Value.LifecyclePolicyId);
        Assert.Equal(Start.AddDays(2), revoked.Value.ExpiresAtUtc);
        Assert.Null(revoked.Value.ApproverActorId);
    }

    [Fact]
    public void Relative_bounds_normalize_against_the_verified_baseline_before_comparison()
    {
        var policy = AccommodationPolicyNormalizer.FromRelativeAllowances(
            AccommodationDomainTestsSupport.Parent().OrganizationId,
            AccommodationDomainTestsSupport.Frozen(),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            null,
            "development",
            true,
            new BaselineTiming(
                Start,
                Ends,
                Deadline,
                "UTC",
                2,
                3600,
                AccommodationDomainTestsSupport.Frozen(),
                false),
            new Dictionary<string, RelativeAccommodationAllowance>(StringComparer.Ordinal)
            {
                [AccommodationDimensions.SubmissionDeadlineUtc] = new(true, 0, 14, 0, 30, AccommodationValueKinds.UtcInstant),
                [AccommodationDimensions.AttemptStartNotBeforeUtc] = new(true, -7, 0, -14, 0, AccommodationValueKinds.UtcInstant),
                [AccommodationDimensions.AttemptStartBeforeUtc] = new(true, 0, 7, 0, 30, AccommodationValueKinds.UtcInstant),
                [AccommodationDimensions.PerAttemptDurationSeconds] = new(true, 0, 3600, 0, 10800, AccommodationValueKinds.PositiveSeconds),
            },
            [AccommodationReasonCategories.DevelopmentSynthetic],
            Guid.Parse("44444444-4444-4444-8444-444444444401"),
            false,
            true);

        Assert.Equal(Format(Deadline), policy.Dimensions[AccommodationDimensions.SubmissionDeadlineUtc].RoutineMin);
        Assert.Equal(Format(Deadline.AddDays(14)), policy.Dimensions[AccommodationDimensions.SubmissionDeadlineUtc].RoutineMax);

        var created = Accommodation.Request(
            AccommodationDomainTestsSupport.Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(3)),
            AccommodationDomainTestsSupport.Frozen(),
            policy,
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            null,
            Guid.CreateVersion7(),
            1);
        Assert.True(created.Succeeded);
        Assert.Equal(AccommodationStates.Granted, created.Value!.Status);
    }

    [Fact]
    public void Frozen_and_current_policy_intersection_rejects_a_later_widened_current_bound()
    {
        var frozen = AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(2), Ends.AddDays(7), 7200);
        var current = AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(14), Ends.AddDays(7), 7200);
        var effective = AccommodationPolicyNormalizer.EffectiveBounds(frozen, current)!;
        Assert.Equal(
            Format(Deadline.AddDays(2)),
            effective.Dimensions[AccommodationDimensions.SubmissionDeadlineUtc].RoutineMax);

        Assert.Equal(
            AccommodationFailureCodes.OutsideBounds,
            Accommodation.Request(
                AccommodationDomainTestsSupport.Parent(),
                AccommodationDimensions.SubmissionDeadlineUtc,
                Format(Deadline.AddDays(10)),
                AccommodationDomainTestsSupport.Frozen(),
                effective,
                AccommodationReasonCategories.DevelopmentSynthetic,
                Start,
                null,
                Guid.CreateVersion7(),
                1).OutcomeCode);
    }

    [Fact]
    public void Digest_binds_canonical_expiry_so_offset_equivalents_match_and_different_instants_conflict()
    {
        var first = DateTimeOffset.Parse("2026-09-25T17:00:00+00:00");
        var offsetEquivalent = new DateTimeOffset(2026, 9, 25, 13, 0, 0, TimeSpan.FromHours(-4));
        var later = DateTimeOffset.Parse("2026-10-10T17:00:00Z");
        var left = AccommodationCommandDigest.Compute(
            AccommodationOperationKinds.Grant,
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb0"),
            null,
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(1)),
            AccommodationReasonCategories.DevelopmentSynthetic,
            false,
            1,
            first);
        var right = AccommodationCommandDigest.Compute(
            AccommodationOperationKinds.Grant,
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb0"),
            null,
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(1)),
            AccommodationReasonCategories.DevelopmentSynthetic,
            false,
            1,
            offsetEquivalent);
        var other = AccommodationCommandDigest.Compute(
            AccommodationOperationKinds.Grant,
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb0"),
            null,
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(1)),
            AccommodationReasonCategories.DevelopmentSynthetic,
            false,
            1,
            later);
        Assert.Equal(left, right);
        Assert.NotEqual(left, other);
    }

    [Fact]
    public void Approval_of_an_expired_pending_request_is_rejected_and_revoke_requires_granted()
    {
        var pending = Accommodation.Request(
            AccommodationDomainTestsSupport.Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(10)),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(2), Ends.AddDays(14), 7200),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            Start.AddMinutes(5),
            Guid.CreateVersion7(),
            1,
            fairnessException: true).Value!;

        Assert.Equal(
            AccommodationFailureCodes.InvalidValue,
            pending.Decide(
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb9"),
                approve: true,
                AccommodationDomainTestsSupport.Frozen(),
                AccommodationDomainTestsSupport.CurrentPolicy(Deadline.AddDays(2), Ends.AddDays(14), 7200),
                pending.Revision,
                Start.AddMinutes(6)).OutcomeCode);

        Assert.Equal(
            AccommodationFailureCodes.Denied,
            pending.Revoke(Guid.CreateVersion7(), Start.AddMinutes(1)).OutcomeCode);

        var granted = Accommodation.CreateGranted(
            AccommodationDomainTestsSupport.Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(1)),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationDomainTestsSupport.Frozen(),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            null,
            Guid.CreateVersion7(),
            1).Value!;
        var revoked = granted.Revoke(Guid.CreateVersion7(), Start.AddHours(1));
        Assert.True(revoked.Succeeded);
        Assert.Equal(
            AccommodationFailureCodes.Denied,
            revoked.Value!.Revoke(Guid.CreateVersion7(), Start.AddHours(2)).OutcomeCode);
        Assert.Equal(
            AccommodationFailureCodes.Denied,
            granted.Supersede(Guid.CreateVersion7(), Start.AddHours(1)).Revoke(Guid.CreateVersion7(), Start.AddHours(2)).OutcomeCode);
    }

    private static string Format(DateTimeOffset value) => AccommodationDomainTestsSupport.FormatUtc(value);
}
