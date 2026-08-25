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

        var rejectedRecent = stale with { Status = IntakeStates.Rejected, UpdatedAtUtc = now.AddDays(-1) };
        var rejectedStale = stale with { Status = IntakeStates.Rejected, UpdatedAtUtc = now.AddDays(-8) };
        Assert.False(SubmissionLifecycle.RejectedBytesEligibleForCleanup(rejectedRecent, now));
        Assert.True(SubmissionLifecycle.RejectedBytesEligibleForCleanup(rejectedStale, now));
        Assert.False(SubmissionLifecycle.MayDeleteArtifact(acceptedReferenceExists: true, legalHoldActive: false));
        Assert.False(SubmissionLifecycle.MayDeleteArtifact(acceptedReferenceExists: false, legalHoldActive: true));
        Assert.True(SubmissionLifecycle.MayDeleteArtifact(acceptedReferenceExists: false, legalHoldActive: false));
        Assert.False(SubmissionLifecycle.AcceptedPayloadEligibleForCleanup(null, now, legalHoldActive: false));
        Assert.False(SubmissionLifecycle.AcceptedPayloadEligibleForCleanup(now.AddDays(-364), now, legalHoldActive: false));
        Assert.False(SubmissionLifecycle.AcceptedPayloadEligibleForCleanup(now.AddDays(-366), now, legalHoldActive: true));
        Assert.True(SubmissionLifecycle.AcceptedPayloadEligibleForCleanup(now.AddDays(-366), now, legalHoldActive: false));
        Assert.False(SubmissionLifecycle.MayDeleteAcceptedPayload(legalHoldActive: true));
        Assert.True(SubmissionLifecycle.MayDeleteAcceptedPayload(legalHoldActive: false));
    }

    [Fact]
    public void Protected_capability_rejects_expiry_replay_and_substitution()
    {
        var now = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        var capability = new ProtectedArtifactCapability(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"),
            Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee"),
            SubmissionPermittedActions.DownloadItem,
            now.AddMinutes(5),
            null);

        Assert.Null(ProtectedArtifactCapabilityRules.Redeem(
            capability,
            capability.OrganizationId,
            capability.ActorId,
            capability.EnrollmentId,
            capability.VersionId,
            capability.ItemId,
            SubmissionPermittedActions.DownloadItem,
            now));
        Assert.Equal(
            SubmissionFailureCodes.CapabilityExpired,
            ProtectedArtifactCapabilityRules.Redeem(
                capability,
                capability.OrganizationId,
                capability.ActorId,
                capability.EnrollmentId,
                capability.VersionId,
                capability.ItemId,
                SubmissionPermittedActions.DownloadItem,
                now.AddMinutes(5)));
        Assert.Equal(
            SubmissionFailureCodes.CapabilityMismatch,
            ProtectedArtifactCapabilityRules.Redeem(
                capability,
                Guid.CreateVersion7(),
                capability.ActorId,
                capability.EnrollmentId,
                capability.VersionId,
                capability.ItemId,
                SubmissionPermittedActions.DownloadItem,
                now));
        Assert.Equal(
            SubmissionFailureCodes.CapabilityMismatch,
            ProtectedArtifactCapabilityRules.Redeem(
                capability with { RedeemedAtUtc = now },
                capability.OrganizationId,
                capability.ActorId,
                capability.EnrollmentId,
                capability.VersionId,
                capability.ItemId,
                SubmissionPermittedActions.DownloadItem,
                now));
    }

    [Fact]
    public void Telemetry_bands_exclude_raw_sizes_and_counts()
    {
        Assert.Equal("1kib_1mib", SubmissionTelemetryBands.ByteBand(2048));
        Assert.Equal("6_10", SubmissionTelemetryBands.CountBand(10));
        Assert.Equal("over_10", SubmissionTelemetryBands.CountBand(11));
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

    [Fact]
    public async Task Preview_writes_audit_before_disclosing_content_and_fails_closed_when_audit_unavailable()
    {
        var organizationId = OrganizationId;
        var enrollmentId = EnrollmentId;
        var versionId = Guid.Parse("33333333-3333-4333-8333-333333333333");
        var itemId = Guid.Parse("44444444-4444-4444-8444-444444444444");
        var content = "Synthetic preview text."u8.ToArray();
        var artifacts = new InMemoryArtifactStore();
        var put = await artifacts.PutAsync(
            new ArtifactPutRequest(
                organizationId,
                ArtifactObjectKey.Create(organizationId, itemId),
                content,
                "text/plain"),
            TestContext.Current.CancellationToken);
        Assert.True(put.Succeeded);

        var enrollments = new InMemoryEnrollmentStore();
        var binding = Binding();
        var enrollment = Enrollment.Create(
            enrollmentId,
            organizationId,
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
        var versions = new InMemorySubmissionVersionStore();
        await versions.InsertAcceptedVersionAsync(
            new AcceptedSubmissionVersion(
                Guid.CreateVersion7(),
                versionId,
                1,
                new SubmissionParentScope(
                    organizationId,
                    ActivityId,
                    CohortId,
                    binding.BaselineId,
                    enrollmentId,
                    ParticipantId,
                    binding.TaskSourceId,
                    binding.TaskVersionId,
                    binding.TaskContentDigest),
                Digest,
                null,
                DateTimeOffset.UtcNow,
                [
                    new AcceptedVersionItem(
                        itemId,
                        MaterialCategories.DirectText,
                        null,
                        content.Length,
                        put.Reference!.Digest.Sha256Hex,
                        put.Reference.ObjectKey.Value,
                        put.Reference.VersionId.Value),
                ]),
            ParticipantId,
            new InMemoryEnrollmentTransaction(),
            TestContext.Current.CancellationToken);

        var audit = new RecordingEnrollmentAuditPort();
        var queries = new SubmissionQueryService(
            new AllowEnrollmentAuthorizationPort(),
            enrollments,
            new InMemoryIntakeStore(),
            versions,
            new FixedFrozenSubmissionRequirementPort(),
            new FixedMaterialPolicyPort(),
            new FixedActivatedCohortPort { Binding = binding },
            artifacts,
            null,
            audit,
            null,
            new InMemoryProtectedArtifactCapabilityStore());

        var preview = await queries.GetAcceptedItemPreviewAsync(
            Participant(),
            enrollmentId,
            versionId,
            itemId,
            TestContext.Current.CancellationToken);

        Assert.True(preview.Found);
        Assert.Equal("Synthetic preview text.", preview.Value!.Text);
        Assert.Equal(1, audit.RequiredWrites);

        audit.FailRequired = true;
        var denied = await queries.GetAcceptedItemPreviewAsync(
            Participant(),
            enrollmentId,
            versionId,
            itemId,
            TestContext.Current.CancellationToken);
        Assert.False(denied.Found);
        Assert.Equal(SubmissionFailureCodes.AuditUnavailable, denied.OutcomeCode);
        Assert.Null(denied.Value);
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
