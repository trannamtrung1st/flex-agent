using Dapper;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SubmissionPersistenceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Raw_insert_rejects_substituted_submission_parent_on_intake()
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);
        var began = await harness.IntakeCoordinator.BeginAsync(harness.BeginCommand("begin-parent"), CancellationToken);
        Assert.True(began.Succeeded, began.OutcomeCode);

        var secondParticipant = await AddEligibleParticipantAsync(harness.OrganizationId);
        var secondAssigned = await harness.EnrollmentCoordinator.AssignAsync(
            harness.AssignCommand("assign-second", secondParticipant),
            CancellationToken);
        Assert.True(secondAssigned.Succeeded, secondAssigned.OutcomeCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var otherSubmissionId = Guid.CreateVersion7();
        var insertedParent = await connection.ExecuteAsync(
            """
            INSERT INTO submissions_submissions (
                organization_id, submission_id, activity_id, cohort_id, baseline_id,
                enrollment_id, participant_actor_id, task_source_id, task_version_id,
                task_content_digest, created_at)
            SELECT
                organization_id, @OtherSubmissionId, activity_id, cohort_id, baseline_id,
                enrollment_id, participant_actor_id, task_source_id, task_version_id,
                task_content_digest, CLOCK_TIMESTAMP()
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId AND enrollment_id = @OtherEnrollmentId
            """,
            new
            {
                harness.OrganizationId,
                OtherSubmissionId = otherSubmissionId,
                OtherEnrollmentId = secondAssigned.EnrollmentId,
            });
        Assert.Equal(1, insertedParent);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO submissions_intakes (
                organization_id, intake_id, submission_id, activity_id, cohort_id, baseline_id,
                enrollment_id, participant_actor_id, task_source_id, task_version_id,
                task_content_digest, status, revision, policy_digest,
                frozen_requirement_source_id, frozen_requirement_version_id, frozen_requirement_digest,
                organization_policy_source_id, organization_policy_version_id, organization_policy_digest,
                created_at, updated_at, complete_receipt_at)
            SELECT
                organization_id,
                @NewIntakeId,
                @OtherSubmissionId,
                activity_id,
                cohort_id,
                baseline_id,
                enrollment_id,
                participant_actor_id,
                task_source_id,
                task_version_id,
                task_content_digest,
                'cancelled',
                revision,
                policy_digest,
                frozen_requirement_source_id,
                frozen_requirement_version_id,
                frozen_requirement_digest,
                organization_policy_source_id,
                organization_policy_version_id,
                organization_policy_digest,
                created_at,
                updated_at,
                complete_receipt_at
            FROM submissions_intakes
            WHERE organization_id = @OrganizationId AND intake_id = @IntakeId
            """,
            new
            {
                harness.OrganizationId,
                IntakeId = began.IntakeId,
                NewIntakeId = Guid.CreateVersion7(),
                OtherSubmissionId = otherSubmissionId,
            }));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Contains("fk_submissions_intakes_submission_parent", exception.ConstraintName, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("task_source_id")]
    [InlineData("task_version_id")]
    [InlineData("task_content_digest")]
    public async Task Raw_insert_rejects_substituted_task_binding_on_intake(string substitutedColumn)
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);
        var began = await harness.IntakeCoordinator.BeginAsync(harness.BeginCommand("begin-task"), CancellationToken);
        Assert.True(began.Succeeded, began.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO submissions_intakes (
                organization_id, intake_id, submission_id, activity_id, cohort_id, baseline_id,
                enrollment_id, participant_actor_id, task_source_id, task_version_id,
                task_content_digest, status, revision, policy_digest,
                frozen_requirement_source_id, frozen_requirement_version_id, frozen_requirement_digest,
                organization_policy_source_id, organization_policy_version_id, organization_policy_digest,
                created_at, updated_at, complete_receipt_at)
            SELECT
                organization_id,
                @NewIntakeId,
                submission_id,
                activity_id,
                cohort_id,
                baseline_id,
                enrollment_id,
                participant_actor_id,
                CASE WHEN @SubstitutedColumn = 'task_source_id' THEN @WrongTaskSourceId ELSE task_source_id END,
                CASE WHEN @SubstitutedColumn = 'task_version_id' THEN @WrongTaskVersionId ELSE task_version_id END,
                CASE WHEN @SubstitutedColumn = 'task_content_digest' THEN @WrongTaskContentDigest ELSE task_content_digest END,
                'cancelled',
                revision,
                policy_digest,
                frozen_requirement_source_id,
                frozen_requirement_version_id,
                frozen_requirement_digest,
                organization_policy_source_id,
                organization_policy_version_id,
                organization_policy_digest,
                created_at,
                updated_at,
                complete_receipt_at
            FROM submissions_intakes
            WHERE organization_id = @OrganizationId AND intake_id = @IntakeId
            """,
            new
            {
                harness.OrganizationId,
                IntakeId = began.IntakeId,
                NewIntakeId = Guid.CreateVersion7(),
                SubstitutedColumn = substitutedColumn,
                WrongTaskSourceId = Guid.CreateVersion7(),
                WrongTaskVersionId = Guid.CreateVersion7(),
                WrongTaskContentDigest = new string('c', 64),
            }));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.True(
            exception.ConstraintName is
                "fk_submissions_intakes_submission_parent" or
                "fk_submissions_intakes_enrollment_parent",
            exception.ConstraintName);
    }

    [Theory]
    [InlineData("task_source_id")]
    [InlineData("task_version_id")]
    [InlineData("task_content_digest")]
    public async Task Raw_insert_rejects_substituted_task_binding_on_accepted_version(string substitutedColumn)
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);
        var accepted = await FinalizeCurrentIntakeAsync(harness, "begin-task-version", "finalize-task-version");

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO submissions_accepted_versions (
                organization_id, submission_id, version_id, version_number,
                activity_id, cohort_id, baseline_id, enrollment_id, participant_actor_id,
                task_source_id, task_version_id, task_content_digest, policy_digest,
                predecessor_version_id, accepted_at, accepted_by_actor_id)
            SELECT
                organization_id,
                submission_id,
                @NewVersionId,
                2,
                activity_id,
                cohort_id,
                baseline_id,
                enrollment_id,
                participant_actor_id,
                CASE WHEN @SubstitutedColumn = 'task_source_id' THEN @WrongTaskSourceId ELSE task_source_id END,
                CASE WHEN @SubstitutedColumn = 'task_version_id' THEN @WrongTaskVersionId ELSE task_version_id END,
                CASE WHEN @SubstitutedColumn = 'task_content_digest' THEN @WrongTaskContentDigest ELSE task_content_digest END,
                policy_digest,
                version_id,
                accepted_at,
                accepted_by_actor_id
            FROM submissions_accepted_versions
            WHERE organization_id = @OrganizationId AND version_id = @VersionId
            """,
            new
            {
                harness.OrganizationId,
                VersionId = accepted.VersionId,
                NewVersionId = Guid.CreateVersion7(),
                SubstitutedColumn = substitutedColumn,
                WrongTaskSourceId = Guid.CreateVersion7(),
                WrongTaskVersionId = Guid.CreateVersion7(),
                WrongTaskContentDigest = new string('c', 64),
            }));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Contains(
            "fk_submissions_accepted_versions_submission_parent",
            exception.ConstraintName,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("task_source_id")]
    [InlineData("task_version_id")]
    [InlineData("task_content_digest")]
    public async Task Raw_insert_rejects_substituted_task_binding_on_submission(string substitutedColumn)
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO submissions_submissions (
                organization_id, submission_id, activity_id, cohort_id, baseline_id,
                enrollment_id, participant_actor_id, task_source_id, task_version_id,
                task_content_digest, created_at)
            SELECT
                organization_id,
                @SubmissionId,
                activity_id,
                cohort_id,
                baseline_id,
                enrollment_id,
                participant_actor_id,
                CASE WHEN @SubstitutedColumn = 'task_source_id' THEN @WrongTaskSourceId ELSE task_source_id END,
                CASE WHEN @SubstitutedColumn = 'task_version_id' THEN @WrongTaskVersionId ELSE task_version_id END,
                CASE WHEN @SubstitutedColumn = 'task_content_digest' THEN @WrongTaskContentDigest ELSE task_content_digest END,
                CLOCK_TIMESTAMP()
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            """,
            new
            {
                harness.OrganizationId,
                harness.EnrollmentId,
                SubmissionId = Guid.CreateVersion7(),
                SubstitutedColumn = substitutedColumn,
                WrongTaskSourceId = Guid.CreateVersion7(),
                WrongTaskVersionId = Guid.CreateVersion7(),
                WrongTaskContentDigest = new string('c', 64),
            }));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Contains(
            "fk_submissions_submissions_enrollment_parent",
            exception.ConstraintName,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Begin_after_cancel_reuses_stable_submission_row()
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);
        var first = await harness.IntakeCoordinator.BeginAsync(harness.BeginCommand("begin-1"), CancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);

        var cancelled = await harness.IntakeCoordinator.CancelAsync(
            new CancelIntakeCommand(
                harness.ParticipantActor,
                harness.EnrollmentId,
                first.IntakeId!.Value,
                first.Revision!.Value,
                "cancel-1",
                SubmissionCommandDigest.Compute(
                    IntakeOperationKinds.Cancel,
                    harness.OrganizationId.ToString("D"),
                    harness.EnrollmentId.ToString("D"),
                    first.IntakeId.Value.ToString("D"),
                    first.Revision.Value.ToString())),
            CancellationToken);
        Assert.True(cancelled.Succeeded, cancelled.OutcomeCode);

        var second = await harness.IntakeCoordinator.BeginAsync(harness.BeginCommand("begin-2"), CancellationToken);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(first.SubmissionId, second.SubmissionId);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var submissionCount = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM submissions_submissions
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            """,
            new { harness.OrganizationId, harness.EnrollmentId });
        Assert.Equal(1, submissionCount);
    }

    [Fact]
    public async Task Second_finalize_persists_predecessor_version_id()
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);
        var first = await FinalizeCurrentIntakeAsync(harness, "begin-v1", "finalize-v1");
        var cancelled = await harness.IntakeCoordinator.CancelAsync(
            new CancelIntakeCommand(
                harness.ParticipantActor,
                harness.EnrollmentId,
                first.IntakeId!.Value,
                first.Revision!.Value,
                "cancel-v1",
                SubmissionCommandDigest.Compute(
                    IntakeOperationKinds.Cancel,
                    harness.OrganizationId.ToString("D"),
                    harness.EnrollmentId.ToString("D"),
                    first.IntakeId.Value.ToString("D"),
                    first.Revision.Value.ToString())),
            CancellationToken);
        Assert.True(cancelled.Succeeded, cancelled.OutcomeCode);

        var second = await FinalizeCurrentIntakeAsync(harness, "begin-v2", "finalize-v2");
        Assert.Equal(2, second.VersionNumber);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var predecessor = await connection.ExecuteScalarAsync<Guid?>(
            """
            SELECT predecessor_version_id
            FROM submissions_accepted_versions
            WHERE organization_id = @OrganizationId AND version_id = @VersionId
            """,
            new { harness.OrganizationId, VersionId = second.VersionId });
        Assert.Equal(first.VersionId, predecessor);
    }

    [Fact]
    public async Task Concurrent_finalize_accepts_exactly_one_version()
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);
        var began = await harness.IntakeCoordinator.BeginAsync(harness.BeginCommand("begin-race"), CancellationToken);
        Assert.True(began.Succeeded, began.OutcomeCode);
        var completed = await harness.IntakeCoordinator.CompleteItemAsync(
            harness.CompleteCommand(began.IntakeId!.Value, began.Revision!.Value, "Direct text answer.", "complete-race"),
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        var first = FinalizeCommand(harness, completed.IntakeId!.Value, completed.Revision!.Value, "finalize-race-a");
        var second = FinalizeCommand(harness, completed.IntakeId.Value, completed.Revision.Value, "finalize-race-b");
        var results = await Task.WhenAll(
            harness.IntakeCoordinator.FinalizeAsync(first, CancellationToken),
            harness.IntakeCoordinator.FinalizeAsync(second, CancellationToken));

        Assert.Equal(1, results.Count(result => result.Succeeded));
        Assert.Contains(results, result => !result.Succeeded
            && result.OutcomeCode is SubmissionFailureCodes.StaleRevision
                or SubmissionFailureCodes.AlreadyAccepted
                or SubmissionFailureCodes.CancellationRace
                or SubmissionFailureCodes.IdempotencyConflict);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var versionCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM submissions_accepted_versions
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            """,
            new { harness.OrganizationId, harness.EnrollmentId });
        Assert.Equal(1, versionCount);
    }

    private static FinalizeIntakeCommand FinalizeCommand(
        SubmissionIntakeTestSeed.SubmissionIntakeHarness harness,
        Guid intakeId,
        long revision,
        string key) =>
        new(
            harness.ParticipantActor,
            harness.EnrollmentId,
            intakeId,
            revision,
            key,
            SubmissionCommandDigest.Compute(
                IntakeOperationKinds.Finalize,
                harness.OrganizationId.ToString("D"),
                harness.EnrollmentId.ToString("D"),
                intakeId.ToString("D"),
                revision.ToString()));

    private async Task<IntakeMutationOutcome> FinalizeCurrentIntakeAsync(
        SubmissionIntakeTestSeed.SubmissionIntakeHarness harness,
        string beginKey,
        string finalizeKey)
    {
        var began = await harness.IntakeCoordinator.BeginAsync(harness.BeginCommand(beginKey), CancellationToken);
        Assert.True(began.Succeeded, began.OutcomeCode);
        await SeedReceivedItemAsync(harness, began.IntakeId!.Value, began.Revision!.Value);

        var finalize = await harness.IntakeCoordinator.FinalizeAsync(
            FinalizeCommand(harness, began.IntakeId.Value, began.Revision.Value + 1, finalizeKey),
            CancellationToken);
        Assert.True(finalize.Succeeded, finalize.OutcomeCode);
        return finalize;
    }

    private async Task SeedReceivedItemAsync(
        SubmissionIntakeTestSeed.SubmissionIntakeHarness harness,
        Guid intakeId,
        long revision)
    {
        var connections = Fixture.Services.ConnectionAccessor;
        var intakes = new PostgresIntakeStore(connections);
        var sessions = new IdentityEnrollmentSessionPort(new PostgresApplicationSessionStore(connections));
        var unitOfWork = new PostgresEnrollmentUnitOfWork(connections, sessions);
        var intake = await intakes.FindIntakeAsync(
            harness.OrganizationId,
            harness.EnrollmentId,
            intakeId,
            null,
            CancellationToken);
        Assert.NotNull(intake);
        var item = new IntakeItem(
            Guid.CreateVersion7(),
            MaterialCategories.DirectText,
            "answer.txt",
            "text/plain",
            12,
            new string('b', 64),
            ArtifactObjectKey.Create(harness.OrganizationId, Guid.CreateVersion7()).Value,
            "version-1",
            DateTimeOffset.Parse("2026-08-24T12:00:00Z"));
        await unitOfWork.ExecuteAsync(
            harness.ParticipantActor,
            async transaction =>
            {
                await intakes.UpdateIntakeAsync(
                    intake with
                    {
                        Revision = revision + 1,
                        Status = IntakeStates.Received,
                        UpdatedAtUtc = DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                        CompleteReceiptAtUtc = DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                        Items = [item],
                    },
                    harness.ParticipantId,
                    transaction,
                    CancellationToken);
                return true;
            },
            CancellationToken);
    }

    private async Task<Guid> AddEligibleParticipantAsync(Guid organizationId)
    {
        var participantId = Guid.CreateVersion7();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO actors (id, created_at) VALUES (@ActorId, CLOCK_TIMESTAMP());
            INSERT INTO human_identity_bindings (binding_id, issuer, subject, actor_id, created_at)
            VALUES (@BindingId, 'https://issuer.test', @Subject, @ActorId, CLOCK_TIMESTAMP());
            INSERT INTO identity_human_display_profiles (organization_id, actor_id, display_label, created_at, updated_at)
            VALUES (@OrganizationId, @ActorId, 'Synthetic Participant', CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP());
            """,
            new
            {
                ActorId = participantId,
                BindingId = Guid.CreateVersion7(),
                Subject = Guid.CreateVersion7().ToString("D"),
                OrganizationId = organizationId,
            });
        await Fixture.GrantOrganizationActionAsync(organizationId, participantId, EnrollmentAuthorizationActions.Receive);
        await Fixture.GrantOrganizationActionAsync(organizationId, participantId, EnrollmentAuthorizationActions.Discover);
        return participantId;
    }
}
