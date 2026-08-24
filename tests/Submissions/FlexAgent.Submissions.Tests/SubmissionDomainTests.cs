using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class MaterialPolicyTests
{
    [Fact]
    public void Intersect_applies_most_restrictive_category_limits()
    {
        var frozenRef = new PolicySourceRef(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64));
        var orgRef = new PolicySourceRef(Guid.NewGuid(), Guid.NewGuid(), new string('b', 64));
        var frozen = DevelopmentMaterialPolicy.FrozenRequirement(frozenRef) with
        {
            Categories =
            [
                new MaterialCategoryLimit(MaterialCategories.DirectText, true, 1_048_576, 1, [], ["text/plain"]),
                new MaterialCategoryLimit(MaterialCategories.PlainTextAttachment, true, 10_485_760, 10, [".txt"], ["text/plain"]),
            ],
        };
        var organization = DevelopmentMaterialPolicy.OrganizationPolicy(orgRef) with
        {
            MaxAttachmentCount = 5,
            Categories =
            [
                new MaterialCategoryLimit(MaterialCategories.DirectText, true, 512_000, 1, [], ["text/plain"]),
                new MaterialCategoryLimit(MaterialCategories.PlainTextAttachment, true, 5_242_880, 10, [".txt"], ["text/plain"]),
            ],
        };

        var effective = MaterialPolicyResolver.Intersect(frozen, organization);

        Assert.NotNull(effective);
        Assert.Equal(512_000, effective.Categories.Single(c => c.Category == MaterialCategories.DirectText).MaxBytes);
        Assert.Equal(5_242_880, effective.Categories.Single(c => c.Category == MaterialCategories.PlainTextAttachment).MaxBytes);
        Assert.Equal(5, effective.MaxAttachmentCount);
    }

    [Fact]
    public void Intersect_fails_closed_when_environment_ineligible()
    {
        var frozenRef = new PolicySourceRef(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64));
        var orgRef = new PolicySourceRef(Guid.NewGuid(), Guid.NewGuid(), new string('b', 64));
        var frozen = DevelopmentMaterialPolicy.FrozenRequirement(frozenRef);
        var organization = DevelopmentMaterialPolicy.OrganizationPolicy(orgRef) with { EnvironmentEligible = false };

        Assert.Null(MaterialPolicyResolver.Intersect(frozen, organization));
    }
}

public sealed class MaterialContentValidatorTests
{
    [Fact]
    public void ValidateDirectText_rejects_invalid_utf8()
    {
        var policy = DevelopmentMaterialPolicy.FrozenRequirement(
            new PolicySourceRef(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64)));
        var invalid = new byte[] { 0xFF, 0xFE, 0xFD };

        var result = MaterialContentValidator.ValidateDirectText(invalid, policy);

        Assert.False(result.Succeeded);
        Assert.Equal(SubmissionFailureCodes.InvalidEncoding, result.OutcomeCode);
    }

    [Fact]
    public void ValidateDirectText_rejects_oversized_content()
    {
        var policy = DevelopmentMaterialPolicy.FrozenRequirement(
            new PolicySourceRef(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64)));
        var oversized = new byte[1_048_577];

        var result = MaterialContentValidator.ValidateDirectText(oversized, policy);

        Assert.False(result.Succeeded);
        Assert.Equal(SubmissionFailureCodes.Oversized, result.OutcomeCode);
    }

    [Fact]
    public void EvaluateScanner_required_mode_fails_closed_without_clean_result()
    {
        var outcome = MaterialContentValidator.EvaluateScanner(
            MaterialScannerMode.Required,
            MaterialScanOutcome.Unavailable);

        Assert.Equal(MaterialScanOutcome.Unavailable, outcome);
    }

    [Fact]
    public void EvaluateScanner_disabled_mode_does_not_require_scan_result()
    {
        var outcome = MaterialContentValidator.EvaluateScanner(
            MaterialScannerMode.DisabledByApprovedPolicy,
            null);

        Assert.Equal(MaterialScanOutcome.Clean, outcome);
    }
}

public sealed class IntakeStateMachineTests
{
    [Fact]
    public void ReceiptBeforeCutoff_requires_complete_receipt_at_or_before_cutoff()
    {
        var cutoff = DateTimeOffset.Parse("2026-08-24T12:00:00Z");
        Assert.True(IntakeStateMachine.ReceiptBeforeCutoff(cutoff.AddMinutes(-1), cutoff));
        Assert.True(IntakeStateMachine.ReceiptBeforeCutoff(cutoff, cutoff));
        Assert.False(IntakeStateMachine.ReceiptBeforeCutoff(cutoff.AddMinutes(1), cutoff));
        Assert.False(IntakeStateMachine.ReceiptBeforeCutoff(null, cutoff));
    }

    [Fact]
    public void CanTransition_allows_receiving_to_received()
    {
        Assert.True(IntakeStateMachine.CanTransition(IntakeStates.Receiving, IntakeStates.Received));
        Assert.False(IntakeStateMachine.CanTransition(IntakeStates.Accepted, IntakeStates.Received));
    }
}

public sealed class SubmissionLifecycleTests
{
    [Fact]
    public void Incomplete_cleanup_requires_retention_elapsed()
    {
        var now = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        var scope = new SubmissionParentScope(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new string('a', 64));
        var recent = new SubmissionIntakeRecord(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            scope,
            IntakeStates.Receiving,
            1,
            new string('b', 64),
            scope.TaskSourceId,
            scope.TaskVersionId,
            scope.TaskContentDigest,
            scope.TaskSourceId,
            scope.TaskVersionId,
            scope.TaskContentDigest,
            now.AddHours(-1),
            now.AddHours(-1),
            null,
            []);
        var stale = recent with { CreatedAtUtc = now.AddHours(-25) };

        Assert.False(SubmissionLifecycle.IncompleteEligibleForCleanup(recent, now));
        Assert.True(SubmissionLifecycle.IncompleteEligibleForCleanup(stale, now));
    }
}

public sealed class SubmissionApplicationTests
{
    private static readonly Guid OrganizationId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid ActivityId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid CohortId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2");
    private static readonly Guid ParticipantId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
    private static readonly Guid EnrollmentId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");
    private static readonly string Digest = new('a', 64);

    [Fact]
    public void Participant_submission_actions_bypass_administrator_authentication_policy()
    {
        var actor = new EnrollmentActorContext(
            new TrustedActor(ParticipantId, HumanInteractiveActorTypes.Interactive),
            new OrganizationScope(OrganizationId),
            string.Empty,
            new AuthenticationStrength(null, []),
            Guid.CreateVersion7(),
            "https",
            [EnrollmentAuthorizationActions.Discover],
            Guid.CreateVersion7());

        Assert.Null(EnrollmentAuthenticationPolicy.Evaluate(actor, SubmissionAuthorizationActions.ReadSubmission));
        Assert.Null(EnrollmentAuthenticationPolicy.Evaluate(actor, SubmissionAuthorizationActions.BeginIntake));
    }

    [Fact]
    public async Task My_work_submission_query_uses_assignment_discovery_authorization()
    {
        var authorization = new AllowEnrollmentAuthorizationPort();
        var enrollments = new InMemoryEnrollmentStore();
        var binding = Binding();
        var enrollment = Enrollment.Create(
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
            DateTimeOffset.UtcNow).Value!;
        enrollments.Restore([enrollment], []);
        var cohorts = new FixedActivatedCohortPort { Binding = binding };
        var queries = new SubmissionQueryService(
            authorization,
            enrollments,
            new InMemoryIntakeStore(),
            new InMemorySubmissionVersionStore(),
            new FixedFrozenSubmissionRequirementPort(),
            new FixedMaterialPolicyPort(),
            cohorts);

        var result = await queries.GetMyWorkSubmissionAsync(
            Participant(),
            EnrollmentId,
            TestContext.Current.CancellationToken);

        Assert.True(result.Found);
        Assert.True(result.Value!.IntakeAvailable);
        Assert.Equal(EnrollmentResourceTypes.Assignment, authorization.LastResourceType);
    }

    [Fact]
    public async Task My_work_submission_query_reports_policy_unavailable_when_binding_missing()
    {
        var enrollments = new InMemoryEnrollmentStore();
        var binding = Binding();
        var enrollment = Enrollment.Create(
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
            DateTimeOffset.UtcNow).Value!;
        enrollments.Restore([enrollment], []);
        var queries = new SubmissionQueryService(
            new AllowEnrollmentAuthorizationPort(),
            enrollments,
            new InMemoryIntakeStore(),
            new InMemorySubmissionVersionStore(),
            new FixedFrozenSubmissionRequirementPort(),
            new FixedMaterialPolicyPort(),
            new FixedActivatedCohortPort { Binding = null });

        var result = await queries.GetMyWorkSubmissionAsync(
            Participant(),
            EnrollmentId,
            TestContext.Current.CancellationToken);

        Assert.True(result.Found);
        Assert.False(result.Value!.IntakeAvailable);
        Assert.Equal(SubmissionFailureCodes.PolicyUnavailable, result.Value.UnavailableReason);
    }

    private static EnrollmentActorContext Participant() =>
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
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(20),
            EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId,
            EnrollmentLifecyclePolicy.RestrictedPreservationVersion,
            false);
}
