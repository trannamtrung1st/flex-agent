using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using Npgsql;

namespace FlexAgent.Submissions.Infrastructure;

public sealed class PostgresIntakeStore(PostgresConnectionAccessor connections) : IIntakeStore
{
    public async Task<SubmissionIntakeRecord?> FindIntakeAsync(
        Guid organizationId,
        Guid enrollmentId,
        Guid intakeId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        const string where = """
             WHERE organization_id = @OrganizationId
               AND enrollment_id = @EnrollmentId
               AND intake_id = @IntakeId
             """;

        if (transaction is PostgresEnrollmentTransaction postgres)
        {
            var row = await postgres.Scope.Connection.QuerySingleOrDefaultAsync<IntakeRow>(
                new CommandDefinition(
                    SelectIntakeSql + where,
                    new { OrganizationId = organizationId, EnrollmentId = enrollmentId, IntakeId = intakeId },
                    postgres.Scope.Transaction,
                    cancellationToken: cancellationToken));
            return row is null ? null : await HydrateAsync(postgres.Scope.Connection, postgres.Scope.Transaction, row, cancellationToken);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var outside = await connection.QuerySingleOrDefaultAsync<IntakeRow>(
            new CommandDefinition(
                SelectIntakeSql + where,
                new { OrganizationId = organizationId, EnrollmentId = enrollmentId, IntakeId = intakeId },
                cancellationToken: cancellationToken));
        return outside is null ? null : await HydrateAsync(connection, null, outside, cancellationToken);
    }

    public async Task<SubmissionIntakeRecord?> FindActiveIntakeAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = SelectIntakeSql + """
             WHERE organization_id = @OrganizationId
               AND enrollment_id = @EnrollmentId
               AND status IN ('receiving', 'received', 'validating', 'cancelling', 'reconciling')
             ORDER BY created_at DESC
             LIMIT 1
             """;

        if (transaction is PostgresEnrollmentTransaction postgres)
        {
            var row = await postgres.Scope.Connection.QuerySingleOrDefaultAsync<IntakeRow>(
                new CommandDefinition(
                    sql,
                    new { OrganizationId = organizationId, EnrollmentId = enrollmentId },
                    postgres.Scope.Transaction,
                    cancellationToken: cancellationToken));
            return row is null ? null : await HydrateAsync(postgres.Scope.Connection, postgres.Scope.Transaction, row, cancellationToken);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var outside = await connection.QuerySingleOrDefaultAsync<IntakeRow>(
            new CommandDefinition(
                sql,
                new { OrganizationId = organizationId, EnrollmentId = enrollmentId },
                cancellationToken: cancellationToken));
        return outside is null ? null : await HydrateAsync(connection, null, outside, cancellationToken);
    }

    public async Task InsertIntakeAsync(
        SubmissionIntakeRecord intake,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO submissions_submissions (
                    organization_id, submission_id, activity_id, cohort_id, baseline_id,
                    enrollment_id, participant_actor_id, task_source_id, task_version_id,
                    task_content_digest, created_at)
                VALUES (
                    @OrganizationId, @SubmissionId, @ActivityId, @CohortId, @BaselineId,
                    @EnrollmentId, @ParticipantActorId, @TaskSourceId, @TaskVersionId,
                    @TaskContentDigest, @CreatedAtUtc)
                ON CONFLICT (organization_id, enrollment_id) DO NOTHING;
                INSERT INTO submissions_intakes (
                    organization_id, intake_id, submission_id, activity_id, cohort_id, baseline_id,
                    enrollment_id, participant_actor_id, task_source_id, task_version_id,
                    task_content_digest, status, revision, policy_digest,
                    frozen_requirement_source_id, frozen_requirement_version_id, frozen_requirement_digest,
                    organization_policy_source_id, organization_policy_version_id, organization_policy_digest,
                    created_at, updated_at, complete_receipt_at)
                VALUES (
                    @OrganizationId, @IntakeId, @SubmissionId, @ActivityId, @CohortId, @BaselineId,
                    @EnrollmentId, @ParticipantActorId, @TaskSourceId, @TaskVersionId,
                    @TaskContentDigest, @Status, @Revision, @PolicyDigest,
                    @FrozenRequirementSourceId, @FrozenRequirementVersionId, @FrozenRequirementDigest,
                    @OrganizationPolicySourceId, @OrganizationPolicyVersionId, @OrganizationPolicyDigest,
                    @CreatedAtUtc, @UpdatedAtUtc, @CompleteReceiptAtUtc);
                """,
                MapIntake(intake),
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task UpdateIntakeAsync(
        SubmissionIntakeRecord intake,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        var updated = await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_intakes
                SET status = @Status,
                    revision = @Revision,
                    updated_at = @UpdatedAtUtc,
                    complete_receipt_at = @CompleteReceiptAtUtc
                WHERE organization_id = @OrganizationId
                  AND intake_id = @IntakeId
                  AND revision = @Revision - 1
                """,
                MapIntake(intake),
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
        if (updated != 1)
        {
            throw new InvalidOperationException(SubmissionFailureCodes.StaleRevision);
        }
    }

    private static async Task<SubmissionIntakeRecord> HydrateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? dbTransaction,
        IntakeRow row,
        CancellationToken cancellationToken)
    {
        var items = await connection.QueryAsync<IntakeItemRow>(
            new CommandDefinition(
                """
                SELECT item_id AS ItemId, category AS Category, filename AS Filename,
                       declared_mime_type AS DeclaredMimeType, byte_count AS ByteCount,
                       content_digest AS ContentDigest, artifact_object_key AS ArtifactObjectKey,
                       artifact_version_id AS ArtifactVersionId, received_at AS ReceivedAtUtc
                FROM submissions_intake_items
                WHERE organization_id = @OrganizationId AND intake_id = @IntakeId
                ORDER BY item_id
                """,
                new { row.OrganizationId, row.IntakeId },
                dbTransaction,
                cancellationToken: cancellationToken));

        return new SubmissionIntakeRecord(
            row.IntakeId,
            row.SubmissionId,
            new SubmissionParentScope(
                row.OrganizationId,
                row.ActivityId,
                row.CohortId,
                row.BaselineId,
                row.EnrollmentId,
                row.ParticipantActorId,
                row.TaskSourceId,
                row.TaskVersionId,
                row.TaskContentDigest),
            row.Status,
            row.Revision,
            row.PolicyDigest,
            row.FrozenRequirementSourceId,
            row.FrozenRequirementVersionId,
            row.FrozenRequirementDigest,
            row.OrganizationPolicySourceId,
            row.OrganizationPolicyVersionId,
            row.OrganizationPolicyDigest,
            row.CreatedAtUtc,
            row.UpdatedAtUtc,
            row.CompleteReceiptAtUtc,
            items.Select(item => new IntakeItem(
                item.ItemId,
                item.Category,
                item.Filename,
                item.DeclaredMimeType,
                item.ByteCount,
                item.ContentDigest,
                item.ArtifactObjectKey,
                item.ArtifactVersionId,
                item.ReceivedAtUtc)).ToArray());
    }

    private const string SelectIntakeSql = """
        SELECT organization_id AS OrganizationId, intake_id AS IntakeId, submission_id AS SubmissionId,
               activity_id AS ActivityId, cohort_id AS CohortId, baseline_id AS BaselineId,
               enrollment_id AS EnrollmentId, participant_actor_id AS ParticipantActorId,
               task_source_id AS TaskSourceId, task_version_id AS TaskVersionId,
               task_content_digest AS TaskContentDigest, status AS Status, revision AS Revision,
               policy_digest AS PolicyDigest, frozen_requirement_source_id AS FrozenRequirementSourceId,
               frozen_requirement_version_id AS FrozenRequirementVersionId,
               frozen_requirement_digest AS FrozenRequirementDigest,
               organization_policy_source_id AS OrganizationPolicySourceId,
               organization_policy_version_id AS OrganizationPolicyVersionId,
               organization_policy_digest AS OrganizationPolicyDigest,
               created_at AS CreatedAtUtc, updated_at AS UpdatedAtUtc,
               complete_receipt_at AS CompleteReceiptAtUtc
        FROM submissions_intakes
        """;

    private static object MapIntake(SubmissionIntakeRecord intake) => new
    {
        intake.Scope.OrganizationId,
        intake.IntakeId,
        intake.SubmissionId,
        intake.Scope.ActivityId,
        intake.Scope.CohortId,
        intake.Scope.BaselineId,
        intake.Scope.EnrollmentId,
        intake.Scope.ParticipantActorId,
        intake.Scope.TaskSourceId,
        intake.Scope.TaskVersionId,
        TaskContentDigest = intake.Scope.TaskContentDigest,
        intake.Status,
        intake.Revision,
        intake.PolicyDigest,
        FrozenRequirementSourceId = intake.FrozenRequirementSourceId,
        FrozenRequirementVersionId = intake.FrozenRequirementVersionId,
        FrozenRequirementDigest = intake.FrozenRequirementDigest,
        OrganizationPolicySourceId = intake.OrganizationPolicySourceId,
        OrganizationPolicyVersionId = intake.OrganizationPolicyVersionId,
        OrganizationPolicyDigest = intake.OrganizationPolicyDigest,
        intake.CreatedAtUtc,
        intake.UpdatedAtUtc,
        intake.CompleteReceiptAtUtc,
    };

    private sealed record IntakeRow(
        Guid OrganizationId,
        Guid IntakeId,
        Guid SubmissionId,
        Guid ActivityId,
        Guid CohortId,
        Guid BaselineId,
        Guid EnrollmentId,
        Guid ParticipantActorId,
        Guid TaskSourceId,
        Guid TaskVersionId,
        string TaskContentDigest,
        string Status,
        long Revision,
        string PolicyDigest,
        Guid FrozenRequirementSourceId,
        Guid FrozenRequirementVersionId,
        string FrozenRequirementDigest,
        Guid OrganizationPolicySourceId,
        Guid OrganizationPolicyVersionId,
        string OrganizationPolicyDigest,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        DateTimeOffset? CompleteReceiptAtUtc);

    private sealed record IntakeItemRow(
        Guid ItemId,
        string Category,
        string? Filename,
        string? DeclaredMimeType,
        long ByteCount,
        string ContentDigest,
        string? ArtifactObjectKey,
        string? ArtifactVersionId,
        DateTimeOffset? ReceivedAtUtc);
}

public sealed class PostgresSubmissionVersionStore(PostgresConnectionAccessor connections) : ISubmissionVersionStore
{
    public async Task<Guid?> FindSubmissionIdByEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT submission_id
            FROM submissions_submissions
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            """;

        if (transaction is PostgresEnrollmentTransaction postgres)
        {
            return await postgres.Scope.Connection.ExecuteScalarAsync<Guid?>(
                new CommandDefinition(
                    sql,
                    new { OrganizationId = organizationId, EnrollmentId = enrollmentId },
                    postgres.Scope.Transaction,
                    cancellationToken: cancellationToken));
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                sql,
                new { OrganizationId = organizationId, EnrollmentId = enrollmentId },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<AcceptedVersionSummary>> ListVersionsAsync(
        Guid organizationId,
        Guid submissionId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                version_id AS VersionId,
                version_number AS VersionNumber,
                accepted_at AS AcceptedAtUtc,
                (SELECT COUNT(*) FROM submissions_accepted_version_items items
                 WHERE items.organization_id = versions.organization_id
                   AND items.version_id = versions.version_id) AS ItemCount
            FROM submissions_accepted_versions versions
            WHERE organization_id = @OrganizationId AND submission_id = @SubmissionId
            ORDER BY version_number DESC
            """;

        if (transaction is PostgresEnrollmentTransaction postgres)
        {
            var rows = await postgres.Scope.Connection.QueryAsync<AcceptedVersionSummary>(
                new CommandDefinition(sql, new { OrganizationId = organizationId, SubmissionId = submissionId }, postgres.Scope.Transaction, cancellationToken: cancellationToken));
            return rows.ToArray();
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var outside = await connection.QueryAsync<AcceptedVersionSummary>(
            new CommandDefinition(sql, new { OrganizationId = organizationId, SubmissionId = submissionId }, cancellationToken: cancellationToken));
        return outside.ToArray();
    }

    public async Task<AcceptedSubmissionVersion?> FindVersionAsync(
        Guid organizationId,
        Guid versionId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT organization_id AS OrganizationId, submission_id AS SubmissionId, version_id AS VersionId,
                   version_number AS VersionNumber, activity_id AS ActivityId, cohort_id AS CohortId,
                   baseline_id AS BaselineId, enrollment_id AS EnrollmentId,
                   participant_actor_id AS ParticipantActorId, task_source_id AS TaskSourceId,
                   task_version_id AS TaskVersionId, task_content_digest AS TaskContentDigest,
                   policy_digest AS PolicyDigest, predecessor_version_id AS PredecessorVersionId,
                   accepted_at AS AcceptedAtUtc
            FROM submissions_accepted_versions
            WHERE organization_id = @OrganizationId AND version_id = @VersionId
            """;

        VersionRow? row;
        NpgsqlConnection connection;
        NpgsqlTransaction? dbTransaction = null;
        var dispose = false;
        if (transaction is PostgresEnrollmentTransaction postgres)
        {
            connection = postgres.Scope.Connection;
            dbTransaction = postgres.Scope.Transaction;
            row = await connection.QuerySingleOrDefaultAsync<VersionRow>(
                new CommandDefinition(sql, new { OrganizationId = organizationId, VersionId = versionId }, dbTransaction, cancellationToken: cancellationToken));
        }
        else
        {
            connection = await connections.OpenConnectionAsync(cancellationToken);
            dispose = true;
            row = await connection.QuerySingleOrDefaultAsync<VersionRow>(
                new CommandDefinition(sql, new { OrganizationId = organizationId, VersionId = versionId }, cancellationToken: cancellationToken));
        }

        if (row is null)
        {
            if (dispose)
            {
                await connection.DisposeAsync();
            }

            return null;
        }

        var items = await connection.QueryAsync<VersionItemRow>(
            new CommandDefinition(
                """
                SELECT item_id AS ItemId, category AS Category, filename AS Filename,
                       byte_count AS ByteCount, content_digest AS ContentDigest,
                       artifact_object_key AS ArtifactObjectKey, artifact_version_id AS ArtifactVersionId
                FROM submissions_accepted_version_items
                WHERE organization_id = @OrganizationId AND version_id = @VersionId
                ORDER BY item_id
                """,
                new { OrganizationId = organizationId, VersionId = versionId },
                dbTransaction,
                cancellationToken: cancellationToken));

        if (dispose)
        {
            await connection.DisposeAsync();
        }

        return new AcceptedSubmissionVersion(
            row.SubmissionId,
            row.VersionId,
            row.VersionNumber,
            new SubmissionParentScope(
                row.OrganizationId,
                row.ActivityId,
                row.CohortId,
                row.BaselineId,
                row.EnrollmentId,
                row.ParticipantActorId,
                row.TaskSourceId,
                row.TaskVersionId,
                row.TaskContentDigest),
            row.PolicyDigest,
            row.PredecessorVersionId,
            row.AcceptedAtUtc,
            items.Select(item => new AcceptedVersionItem(
                item.ItemId,
                item.Category,
                item.Filename,
                item.ByteCount,
                item.ContentDigest,
                item.ArtifactObjectKey,
                item.ArtifactVersionId)).ToArray());
    }

    public async Task<int> AllocateVersionNumberAsync(
        Guid organizationId,
        Guid submissionId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                SELECT submission_id
                FROM submissions_submissions
                WHERE organization_id = @OrganizationId AND submission_id = @SubmissionId
                FOR UPDATE
                """,
                new { OrganizationId = organizationId, SubmissionId = submissionId },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));

        return await postgres.Scope.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COALESCE(MAX(version_number), 0) + 1
                FROM submissions_accepted_versions
                WHERE organization_id = @OrganizationId AND submission_id = @SubmissionId
                """,
                new { OrganizationId = organizationId, SubmissionId = submissionId },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task InsertAcceptedVersionAsync(
        AcceptedSubmissionVersion version,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO submissions_accepted_versions (
                    organization_id, submission_id, version_id, version_number,
                    activity_id, cohort_id, baseline_id, enrollment_id, participant_actor_id,
                    task_source_id, task_version_id, task_content_digest, policy_digest,
                    predecessor_version_id, accepted_at, accepted_by_actor_id)
                VALUES (
                    @OrganizationId, @SubmissionId, @VersionId, @VersionNumber,
                    @ActivityId, @CohortId, @BaselineId, @EnrollmentId, @ParticipantActorId,
                    @TaskSourceId, @TaskVersionId, @TaskContentDigest, @PolicyDigest,
                    @PredecessorVersionId, @AcceptedAtUtc, @AcceptedByActorId);
                """,
                new
                {
                    version.Scope.OrganizationId,
                    version.SubmissionId,
                    version.VersionId,
                    version.VersionNumber,
                    version.Scope.ActivityId,
                    version.Scope.CohortId,
                    version.Scope.BaselineId,
                    version.Scope.EnrollmentId,
                    version.Scope.ParticipantActorId,
                    version.Scope.TaskSourceId,
                    version.Scope.TaskVersionId,
                    TaskContentDigest = version.Scope.TaskContentDigest,
                    version.PolicyDigest,
                    version.PredecessorVersionId,
                    version.AcceptedAtUtc,
                    AcceptedByActorId = actorId,
                },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));

        foreach (var item in version.Items)
        {
            await postgres.Scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO submissions_accepted_version_items (
                        organization_id, version_id, item_id, category, filename,
                        byte_count, content_digest, artifact_object_key, artifact_version_id)
                    VALUES (
                        @OrganizationId, @VersionId, @ItemId, @Category, @Filename,
                        @ByteCount, @ContentDigest, @ArtifactObjectKey, @ArtifactVersionId);
                    """,
                    new
                    {
                        version.Scope.OrganizationId,
                        version.VersionId,
                        item.ItemId,
                        item.Category,
                        item.Filename,
                        item.ByteCount,
                        item.ContentDigest,
                        item.ArtifactObjectKey,
                        item.ArtifactVersionId,
                    },
                    postgres.Scope.Transaction,
                    cancellationToken: cancellationToken));
        }
    }

    private sealed record VersionRow(
        Guid OrganizationId,
        Guid SubmissionId,
        Guid VersionId,
        int VersionNumber,
        Guid ActivityId,
        Guid CohortId,
        Guid BaselineId,
        Guid EnrollmentId,
        Guid ParticipantActorId,
        Guid TaskSourceId,
        Guid TaskVersionId,
        string TaskContentDigest,
        string PolicyDigest,
        Guid? PredecessorVersionId,
        DateTimeOffset AcceptedAtUtc);

    private sealed record VersionItemRow(
        Guid ItemId,
        string Category,
        string? Filename,
        long ByteCount,
        string ContentDigest,
        string ArtifactObjectKey,
        string ArtifactVersionId);
}
