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
        const string sql = $"""
            {SelectIntakeSql}
            WHERE organization_id = @OrganizationId
              AND enrollment_id = @EnrollmentId
              AND intake_id = @IntakeId
            """;

        if (transaction is PostgresEnrollmentTransaction postgres)
        {
            var row = await postgres.Scope.Connection.QuerySingleOrDefaultAsync<IntakeRow>(
                new CommandDefinition(
                    sql,
                    new { OrganizationId = organizationId, EnrollmentId = enrollmentId, IntakeId = intakeId },
                    postgres.Scope.Transaction,
                    cancellationToken: cancellationToken));
            return row is null ? null : await HydrateAsync(postgres.Scope.Connection, postgres.Scope.Transaction, row, cancellationToken);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var outside = await connection.QuerySingleOrDefaultAsync<IntakeRow>(
            new CommandDefinition(
                sql,
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
        const string sql = $"""
            {SelectIntakeSql}
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

        await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                DELETE FROM submissions_intake_items
                WHERE organization_id = @OrganizationId AND intake_id = @IntakeId
                """,
                new { intake.Scope.OrganizationId, intake.IntakeId },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));

        foreach (var item in intake.Items)
        {
            await postgres.Scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO submissions_intake_items (
                        organization_id, intake_id, item_id, category, filename, declared_mime_type,
                        byte_count, content_digest, artifact_object_key, artifact_version_id, received_at)
                    VALUES (
                        @OrganizationId, @IntakeId, @ItemId, @Category, @Filename, @DeclaredMimeType,
                        @ByteCount, @ContentDigest, @ArtifactObjectKey, @ArtifactVersionId, @ReceivedAtUtc);
                    """,
                    new
                    {
                        intake.Scope.OrganizationId,
                        intake.IntakeId,
                        item.ItemId,
                        item.Category,
                        item.Filename,
                        item.DeclaredMimeType,
                        item.ByteCount,
                        item.ContentDigest,
                        item.ArtifactObjectKey,
                        item.ArtifactVersionId,
                        item.ReceivedAtUtc,
                    },
                    postgres.Scope.Transaction,
                    cancellationToken: cancellationToken));
        }
    }

    public async Task<IReadOnlyList<SubmissionIntakeRecord>> ListIncompleteCreatedBeforeAsync(
        DateTimeOffset cutoffUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<IntakeRow>(
            new CommandDefinition(
                $"""
                {SelectIntakeSql}
                WHERE status IN ('receiving', 'received', 'validating', 'cancelling', 'reconciling')
                  AND created_at <= @CutoffUtc
                ORDER BY created_at
                LIMIT @Limit
                """,
                new { CutoffUtc = cutoffUtc, Limit = limit },
                cancellationToken: cancellationToken));
        var results = new List<SubmissionIntakeRecord>();
        foreach (var row in rows)
        {
            results.Add(await HydrateAsync(connection, null, row, cancellationToken));
        }

        return results;
    }

    public async Task<IReadOnlyList<SubmissionIntakeRecord>> ListRejectedUpdatedBeforeAsync(
        DateTimeOffset cutoffUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<IntakeRow>(
            new CommandDefinition(
                $"""
                {SelectIntakeSql}
                WHERE status IN ('cancelled', 'rejected', 'failed')
                  AND updated_at <= @CutoffUtc
                ORDER BY updated_at
                LIMIT @Limit
                """,
                new { CutoffUtc = cutoffUtc, Limit = limit },
                cancellationToken: cancellationToken));
        var results = new List<SubmissionIntakeRecord>();
        foreach (var row in rows)
        {
            results.Add(await HydrateAsync(connection, null, row, cancellationToken));
        }

        return results;
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
            var rows = await postgres.Scope.Connection.QueryAsync<VersionSummaryRow>(
                new CommandDefinition(sql, new { OrganizationId = organizationId, SubmissionId = submissionId }, postgres.Scope.Transaction, cancellationToken: cancellationToken));
            return MapVersionSummaries(rows);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var outside = await connection.QueryAsync<VersionSummaryRow>(
            new CommandDefinition(sql, new { OrganizationId = organizationId, SubmissionId = submissionId }, cancellationToken: cancellationToken));
        return MapVersionSummaries(outside);
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
            ToUtcOffset(row.AcceptedAtUtc),
            items.Select(item => new AcceptedVersionItem(
                item.ItemId,
                item.Category,
                item.Filename,
                item.ByteCount,
                item.ContentDigest,
                item.ArtifactObjectKey,
                item.ArtifactVersionId)).ToArray());
    }

    public async Task<SubmissionVersionAllocation> AllocateNextVersionAsync(
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

        var latest = await postgres.Scope.Connection.QuerySingleOrDefaultAsync<LatestVersionRow>(
            new CommandDefinition(
                """
                SELECT version_id AS VersionId, version_number AS VersionNumber
                FROM submissions_accepted_versions
                WHERE organization_id = @OrganizationId AND submission_id = @SubmissionId
                ORDER BY version_number DESC
                LIMIT 1
                """,
                new { OrganizationId = organizationId, SubmissionId = submissionId },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));

        var nextNumber = (latest?.VersionNumber ?? 0) + 1;
        return new SubmissionVersionAllocation(nextNumber, latest?.VersionId);
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

    public async Task<bool> HasAcceptedArtifactKeyAsync(
        Guid organizationId,
        string artifactObjectKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var found = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM submissions_accepted_version_items
                    WHERE organization_id = @OrganizationId AND artifact_object_key = @ArtifactObjectKey
                )
                """,
                new { OrganizationId = organizationId, ArtifactObjectKey = artifactObjectKey },
                cancellationToken: cancellationToken));
        return found;
    }

    public async Task<IReadOnlyList<AcceptedArtifactCleanupCandidate>> ListAcceptedArtifactCandidatesAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AcceptedArtifactCleanupCandidate>(
            new CommandDefinition(
                """
                SELECT v.organization_id AS OrganizationId, v.activity_id AS ActivityId,
                       v.enrollment_id AS EnrollmentId, v.version_id AS VersionId,
                       i.artifact_object_key AS ArtifactObjectKey
                FROM submissions_accepted_version_items i
                INNER JOIN submissions_accepted_versions v
                    ON v.organization_id = i.organization_id AND v.version_id = i.version_id
                ORDER BY v.accepted_at
                LIMIT @Limit
                """,
                new { Limit = limit },
                cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    private static IReadOnlyList<AcceptedVersionSummary> MapVersionSummaries(IEnumerable<VersionSummaryRow> rows) =>
        rows.Select(row => new AcceptedVersionSummary(
            row.VersionId,
            row.VersionNumber,
            ToUtcOffset(row.AcceptedAtUtc),
            checked((int)row.ItemCount))).ToArray();

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime());

    private sealed record LatestVersionRow(Guid VersionId, int VersionNumber);

    private sealed class VersionSummaryRow
    {
        public Guid VersionId { get; init; }
        public int VersionNumber { get; init; }
        public DateTime AcceptedAtUtc { get; init; }
        public long ItemCount { get; init; }
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
        DateTime AcceptedAtUtc);

    private sealed record VersionItemRow(
        Guid ItemId,
        string Category,
        string? Filename,
        long ByteCount,
        string ContentDigest,
        string ArtifactObjectKey,
        string ArtifactVersionId);
}

public sealed class PostgresExactAcceptedVersionReader(PostgresSubmissionVersionStore versions) : IExactAcceptedVersionReader
{
    public async Task<AcceptedSubmissionVersion?> GetExactAsync(
        SubmissionParentScope scope,
        Guid versionId,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        var version = await versions.FindVersionAsync(scope.OrganizationId, versionId, null, cancellationToken);
        if (version is null
            || version.Scope.EnrollmentId != scope.EnrollmentId
            || version.Scope.ParticipantActorId != scope.ParticipantActorId
            || version.Scope.ActivityId != scope.ActivityId
            || version.Scope.CohortId != scope.CohortId
            || version.Scope.TaskSourceId != scope.TaskSourceId)
        {
            return null;
        }

        return version;
    }
}

public sealed class PostgresSubmissionWorkStore(PostgresConnectionAccessor connections) : ISubmissionWorkStore
{
    public async Task EnqueueAsync(SubmissionWorkItem work, IEnrollmentTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO submissions_durable_work (
                organization_id, work_id, work_kind, enrollment_id, intake_id, version_id,
                status, attempt_count, available_at, lease_until, artifact_object_key, created_at)
            VALUES (
                @OrganizationId, @WorkId, @WorkKind, @EnrollmentId, @IntakeId, @VersionId,
                @Status, @AttemptCount, @AvailableAtUtc, @LeaseUntilUtc, @ArtifactObjectKey, CLOCK_TIMESTAMP())
            ON CONFLICT DO NOTHING
            """;
        var parameters = new
        {
            work.OrganizationId,
            work.WorkId,
            work.WorkKind,
            work.EnrollmentId,
            work.IntakeId,
            work.VersionId,
            work.Status,
            work.AttemptCount,
            work.AvailableAtUtc,
            work.LeaseUntilUtc,
            work.ArtifactObjectKey,
        };

        if (transaction is PostgresEnrollmentTransaction postgres)
        {
            await postgres.Scope.Connection.ExecuteAsync(
                new CommandDefinition(sql, parameters, postgres.Scope.Transaction, cancellationToken: cancellationToken));
            return;
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    public async Task<SubmissionWorkItem?> ClaimNextAsync(DateTimeOffset nowUtc, TimeSpan lease, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var claimed = await connection.QuerySingleOrDefaultAsync<WorkRow>(
            new CommandDefinition(
                """
                SELECT organization_id AS OrganizationId, work_id AS WorkId, work_kind AS WorkKind,
                       enrollment_id AS EnrollmentId, intake_id AS IntakeId, version_id AS VersionId,
                       status AS Status, attempt_count AS AttemptCount, available_at AS AvailableAtUtc,
                       lease_until AS LeaseUntilUtc, artifact_object_key AS ArtifactObjectKey
                FROM submissions_durable_work
                WHERE status = 'pending' AND available_at <= @NowUtc
                   OR (status = 'leased' AND lease_until IS NOT NULL AND lease_until < @NowUtc)
                ORDER BY created_at
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """,
                new { NowUtc = nowUtc },
                transaction,
                cancellationToken: cancellationToken));
        if (claimed is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var leaseUntil = nowUtc.Add(lease);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_durable_work
                SET status = 'leased',
                    attempt_count = attempt_count + 1,
                    lease_until = @LeaseUntil
                WHERE organization_id = @OrganizationId AND work_id = @WorkId
                """,
                new { claimed.OrganizationId, claimed.WorkId, LeaseUntil = leaseUntil },
                transaction,
                cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return new SubmissionWorkItem(
            claimed.OrganizationId,
            claimed.WorkId,
            claimed.WorkKind,
            claimed.EnrollmentId,
            claimed.IntakeId,
            claimed.VersionId,
            SubmissionWorkStates.Leased,
            claimed.AttemptCount + 1,
            claimed.AvailableAtUtc,
            leaseUntil,
            claimed.ArtifactObjectKey);
    }

    public async Task CompleteAsync(Guid organizationId, Guid workId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_durable_work
                SET status = 'completed', lease_until = NULL
                WHERE organization_id = @OrganizationId AND work_id = @WorkId
                """,
                new { OrganizationId = organizationId, WorkId = workId },
                cancellationToken: cancellationToken));
    }

    public async Task FailAsync(Guid organizationId, Guid workId, DateTimeOffset retryAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_durable_work
                SET status = 'pending', available_at = @RetryAtUtc, lease_until = NULL
                WHERE organization_id = @OrganizationId AND work_id = @WorkId
                """,
                new { OrganizationId = organizationId, WorkId = workId, RetryAtUtc = retryAtUtc },
                cancellationToken: cancellationToken));
    }

    private sealed record WorkRow(
        Guid OrganizationId,
        Guid WorkId,
        string WorkKind,
        Guid? EnrollmentId,
        Guid? IntakeId,
        Guid? VersionId,
        string Status,
        int AttemptCount,
        DateTimeOffset AvailableAtUtc,
        DateTimeOffset? LeaseUntilUtc,
        string? ArtifactObjectKey);
}

public sealed class PostgresLifecycleHoldStore(PostgresConnectionAccessor connections) : ISubmissionLifecycleHoldStore
{
    public async Task<bool> IsHeldAsync(Guid organizationId, string artifactObjectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var found = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM submissions_lifecycle_holds
                    WHERE organization_id = @OrganizationId
                      AND artifact_object_key = @ArtifactObjectKey
                      AND active
                )
                """,
                new { OrganizationId = organizationId, ArtifactObjectKey = artifactObjectKey },
                cancellationToken: cancellationToken));
        return found;
    }

    public async Task InsertHoldAsync(Guid organizationId, Guid holdId, string artifactObjectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO submissions_lifecycle_holds (
                    organization_id, hold_id, artifact_object_key, reason_code, active, created_at)
                VALUES (@OrganizationId, @HoldId, @ArtifactObjectKey, 'legal_hold', TRUE, CLOCK_TIMESTAMP())
                """,
                new { OrganizationId = organizationId, HoldId = holdId, ArtifactObjectKey = artifactObjectKey },
                cancellationToken: cancellationToken));
    }
}

public sealed class PostgresArtifactDispositionStore(PostgresConnectionAccessor connections) : IArtifactDispositionStore
{
    public async Task RecordAsync(
        Guid organizationId,
        Guid dispositionId,
        string workKind,
        string artifactObjectKey,
        DateTimeOffset disposedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO submissions_artifact_dispositions (
                    organization_id, disposition_id, work_kind, artifact_object_key, disposed_at)
                VALUES (@OrganizationId, @DispositionId, @WorkKind, @ArtifactObjectKey, @DisposedAtUtc)
                """,
                new
                {
                    OrganizationId = organizationId,
                    DispositionId = dispositionId,
                    WorkKind = workKind,
                    ArtifactObjectKey = artifactObjectKey,
                    DisposedAtUtc = disposedAtUtc,
                },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsAsync(
        Guid organizationId,
        string artifactObjectKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM submissions_artifact_dispositions
                    WHERE organization_id = @OrganizationId AND artifact_object_key = @ArtifactObjectKey
                )
                """,
                new { OrganizationId = organizationId, ArtifactObjectKey = artifactObjectKey },
                cancellationToken: cancellationToken));
    }
}

public sealed class PostgresProtectedArtifactCapabilityStore(PostgresConnectionAccessor connections)
    : IProtectedArtifactCapabilityStore
{
    public async Task<ProtectedArtifactCapability> IssueAsync(
        ProtectedArtifactCapability capability,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO submissions_protected_capabilities (
                    organization_id, capability_id, actor_id, enrollment_id, version_id, item_id,
                    action, expires_at, redeemed_at)
                VALUES (
                    @OrganizationId, @CapabilityId, @ActorId, @EnrollmentId, @VersionId, @ItemId,
                    @Action, @ExpiresAtUtc, @RedeemedAtUtc)
                """,
                capability,
                cancellationToken: cancellationToken));
        return capability;
    }

    public async Task<ProtectedArtifactCapability?> FindAsync(
        Guid organizationId,
        Guid capabilityId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ProtectedArtifactCapability>(
            new CommandDefinition(
                """
                SELECT organization_id AS OrganizationId, capability_id AS CapabilityId, actor_id AS ActorId,
                       enrollment_id AS EnrollmentId, version_id AS VersionId, item_id AS ItemId,
                       action AS Action, expires_at AS ExpiresAtUtc, redeemed_at AS RedeemedAtUtc
                FROM submissions_protected_capabilities
                WHERE organization_id = @OrganizationId AND capability_id = @CapabilityId
                """,
                new { OrganizationId = organizationId, CapabilityId = capabilityId },
                cancellationToken: cancellationToken));
    }

    public async Task MarkRedeemedAsync(
        Guid organizationId,
        Guid capabilityId,
        DateTimeOffset redeemedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_protected_capabilities
                SET redeemed_at = @RedeemedAtUtc
                WHERE organization_id = @OrganizationId AND capability_id = @CapabilityId
                """,
                new { OrganizationId = organizationId, CapabilityId = capabilityId, RedeemedAtUtc = redeemedAtUtc },
                cancellationToken: cancellationToken));
    }
}

