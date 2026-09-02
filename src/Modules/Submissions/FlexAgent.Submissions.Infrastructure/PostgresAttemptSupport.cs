using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using Npgsql;

namespace FlexAgent.Submissions.Infrastructure;

public sealed class PostgresAttemptStore(PostgresConnectionAccessor connections) : IAttemptStore
{
    public async Task<IReadOnlyList<Attempt>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT organization_id AS OrganizationId, attempt_id AS AttemptId, activity_id AS ActivityId,
                   cohort_id AS CohortId, baseline_id AS BaselineId, enrollment_id AS EnrollmentId,
                   participant_actor_id AS ParticipantActorId, task_source_id AS TaskSourceId,
                   ordinal AS Ordinal, entitlement_source AS EntitlementSource,
                   retry_entitlement_id AS RetryEntitlementId, status AS Status, consumed AS Consumed,
                   requested_at AS RequestedAtUtc, started_at AS StartedAtUtc, terminal_at AS TerminalAtUtc,
                   terminal_reason_category AS TerminalReasonCategory, session_id AS SessionId,
                   resolved_configuration_id AS ResolvedConfigurationId, initial_manifest_id AS InitialManifestId,
                   configuration_digest AS ConfigurationDigest, manifest_digest AS ManifestDigest
            FROM submissions_attempts
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            ORDER BY ordinal
            """;
        var (connection, dbTransaction, dispose) = await OpenAsync(transaction, cancellationToken);
        try
        {
            var heads = (await connection.QueryAsync<AttemptHeadRow>(
                new CommandDefinition(
                    sql,
                    new { OrganizationId = organizationId, EnrollmentId = enrollmentId },
                    dbTransaction,
                    cancellationToken: cancellationToken))).ToArray();
            if (heads.Length == 0)
            {
                return [];
            }

            var bindings = (await connection.QueryAsync<BindingRow>(
                new CommandDefinition(
                    """
                    SELECT attempt_id AS AttemptId, version_id AS VersionId, version_number AS VersionNumber,
                           binding_order AS BindingOrder, content_digest AS ContentDigest
                    FROM submissions_attempt_submission_bindings
                    WHERE organization_id = @OrganizationId AND attempt_id = ANY(@AttemptIds)
                    ORDER BY attempt_id, binding_order
                    """,
                    new { OrganizationId = organizationId, AttemptIds = heads.Select(item => item.AttemptId).ToArray() },
                    dbTransaction,
                    cancellationToken: cancellationToken))).ToArray();
            return heads.Select(head => ToAttempt(head, bindings.Where(item => item.AttemptId == head.AttemptId).ToArray())).ToArray();
        }
        finally
        {
            if (dispose)
            {
                await connection.DisposeAsync();
            }
        }
    }

    public async Task InsertAsync(
        Attempt attempt,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, dbTransaction) = EnrollmentTransactionConnection.Required(transaction);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO submissions_attempts (
                    organization_id, attempt_id, activity_id, cohort_id, baseline_id, enrollment_id,
                    participant_actor_id, task_source_id, ordinal, entitlement_source, retry_entitlement_id,
                    status, consumed, requested_at, started_at, terminal_at, terminal_reason_category,
                    session_id, resolved_configuration_id, initial_manifest_id, configuration_digest, manifest_digest)
                VALUES (
                    @OrganizationId, @AttemptId, @ActivityId, @CohortId, @BaselineId, @EnrollmentId,
                    @ParticipantActorId, @TaskSourceId, @Ordinal, @EntitlementSource, @RetryEntitlementId,
                    @Status, @Consumed, @RequestedAtUtc, @StartedAtUtc, @TerminalAtUtc, @TerminalReasonCategory,
                    @SessionId, @ResolvedConfigurationId, @InitialManifestId, @ConfigurationDigest, @ManifestDigest)
                """,
                new
                {
                    attempt.OrganizationId,
                    attempt.AttemptId,
                    attempt.ActivityId,
                    attempt.CohortId,
                    attempt.BaselineId,
                    attempt.EnrollmentId,
                    attempt.ParticipantActorId,
                    attempt.TaskSourceId,
                    attempt.Ordinal,
                    attempt.EntitlementSource,
                    attempt.RetryEntitlementId,
                    attempt.Status,
                    attempt.Consumed,
                    attempt.RequestedAtUtc,
                    attempt.StartedAtUtc,
                    attempt.TerminalAtUtc,
                    attempt.TerminalReasonCategory,
                    SessionId = attempt.Binding.SessionId,
                    ResolvedConfigurationId = attempt.Binding.ResolvedConfigurationId,
                    InitialManifestId = attempt.Binding.InitialManifestId,
                    ConfigurationDigest = attempt.Binding.ConfigurationDigest,
                    ManifestDigest = attempt.Binding.ManifestDigest,
                },
                dbTransaction,
                cancellationToken: cancellationToken));
        foreach (var binding in attempt.SubmissionBindings)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO submissions_attempt_submission_bindings (
                        organization_id, attempt_id, version_id, version_number, binding_order, content_digest)
                    VALUES (@OrganizationId, @AttemptId, @VersionId, @VersionNumber, @BindingOrder, @ContentDigest)
                    """,
                    new
                    {
                        attempt.OrganizationId,
                        attempt.AttemptId,
                        binding.VersionId,
                        binding.VersionNumber,
                        binding.BindingOrder,
                        binding.ContentDigest,
                    },
                    dbTransaction,
                    cancellationToken: cancellationToken));
        }
    }

    public async Task<Attempt?> FindByIdAsync(
        Guid organizationId,
        Guid attemptId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, dbTransaction) = EnrollmentTransactionConnection.Required(transaction);
        var head = await connection.QuerySingleOrDefaultAsync<AttemptHeadRow>(
            new CommandDefinition(
                """
                SELECT organization_id AS OrganizationId, attempt_id AS AttemptId, activity_id AS ActivityId,
                       cohort_id AS CohortId, baseline_id AS BaselineId, enrollment_id AS EnrollmentId,
                       participant_actor_id AS ParticipantActorId, task_source_id AS TaskSourceId,
                       ordinal AS Ordinal, entitlement_source AS EntitlementSource,
                       retry_entitlement_id AS RetryEntitlementId, status AS Status, consumed AS Consumed,
                       requested_at AS RequestedAtUtc, started_at AS StartedAtUtc, terminal_at AS TerminalAtUtc,
                       terminal_reason_category AS TerminalReasonCategory, session_id AS SessionId,
                       resolved_configuration_id AS ResolvedConfigurationId, initial_manifest_id AS InitialManifestId,
                       configuration_digest AS ConfigurationDigest, manifest_digest AS ManifestDigest
                FROM submissions_attempts
                WHERE organization_id = @OrganizationId AND attempt_id = @AttemptId
                """,
                new { OrganizationId = organizationId, AttemptId = attemptId },
                dbTransaction,
                cancellationToken: cancellationToken));
        if (head is null)
        {
            return null;
        }

        var bindings = (await connection.QueryAsync<BindingRow>(
            new CommandDefinition(
                """
                SELECT attempt_id AS AttemptId, version_id AS VersionId, version_number AS VersionNumber,
                       binding_order AS BindingOrder, content_digest AS ContentDigest
                FROM submissions_attempt_submission_bindings
                WHERE organization_id = @OrganizationId AND attempt_id = @AttemptId
                ORDER BY binding_order
                """,
                new { OrganizationId = organizationId, AttemptId = attemptId },
                dbTransaction,
                cancellationToken: cancellationToken))).ToArray();
        return ToAttempt(head, bindings);
    }

    public async Task UpdateTerminalAsync(
        Attempt attempt,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, dbTransaction) = EnrollmentTransactionConnection.Required(transaction);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_attempts
                SET status = @Status, terminal_at = @TerminalAtUtc, terminal_reason_category = @TerminalReasonCategory
                WHERE organization_id = @OrganizationId AND attempt_id = @AttemptId
                """,
                new
                {
                    attempt.OrganizationId,
                    attempt.AttemptId,
                    attempt.Status,
                    attempt.TerminalAtUtc,
                    attempt.TerminalReasonCategory,
                },
                dbTransaction,
                cancellationToken: cancellationToken));
    }

    private static Attempt ToAttempt(AttemptHeadRow head, IReadOnlyList<BindingRow> bindings) =>
        new(
            head.AttemptId,
            head.OrganizationId,
            head.ActivityId,
            head.CohortId,
            head.BaselineId,
            head.EnrollmentId,
            head.ParticipantActorId,
            head.TaskSourceId,
            head.Ordinal,
            head.EntitlementSource,
            head.RetryEntitlementId,
            head.Status,
            head.Consumed,
            head.RequestedAtUtc,
            head.StartedAtUtc,
            head.TerminalAtUtc,
            head.TerminalReasonCategory,
            new AttemptBinding(
                head.SessionId,
                head.ResolvedConfigurationId,
                head.InitialManifestId,
                head.ConfigurationDigest,
                head.ManifestDigest),
            bindings.Select(item => new AttemptSubmissionBinding(
                item.VersionId,
                item.VersionNumber,
                item.BindingOrder,
                item.ContentDigest)).ToArray());

    private async Task<(NpgsqlConnection Connection, NpgsqlTransaction? Transaction, bool Dispose)> OpenAsync(
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            var (connection, dbTransaction) = EnrollmentTransactionConnection.Required(transaction);
            return (connection, dbTransaction, false);
        }

        var opened = await connections.OpenConnectionAsync(cancellationToken);
        return (opened, null, true);
    }

    private sealed record AttemptHeadRow(
        Guid OrganizationId,
        Guid AttemptId,
        Guid ActivityId,
        Guid CohortId,
        Guid BaselineId,
        Guid EnrollmentId,
        Guid ParticipantActorId,
        Guid TaskSourceId,
        int Ordinal,
        string EntitlementSource,
        Guid? RetryEntitlementId,
        string Status,
        bool Consumed,
        DateTimeOffset RequestedAtUtc,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset? TerminalAtUtc,
        string? TerminalReasonCategory,
        Guid SessionId,
        Guid ResolvedConfigurationId,
        Guid InitialManifestId,
        string ConfigurationDigest,
        string ManifestDigest);

    private sealed record BindingRow(
        Guid AttemptId,
        Guid VersionId,
        int VersionNumber,
        int BindingOrder,
        string ContentDigest);
}

public sealed class PostgresStartOperationStore : IStartOperationStore
{
    public Task AcquireLockAsync(
        Guid organizationId,
        Guid enrollmentId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, dbTransaction) = EnrollmentTransactionConnection.Required(transaction);
        return connection.ExecuteAsync(
            new CommandDefinition(
                "SELECT pg_advisory_xact_lock(hashtextextended(@Key, 0))",
                new { Key = $"attempt.start:{organizationId:D}:{enrollmentId:D}:{idempotencyKey}" },
                dbTransaction,
                cancellationToken: cancellationToken));
    }

    public Task<StartOperation?> FindAsync(
        Guid organizationId,
        Guid enrollmentId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, dbTransaction) = EnrollmentTransactionConnection.Required(transaction);
        return connection.QuerySingleOrDefaultAsync<StartOperation>(
            new CommandDefinition(
                """
                SELECT organization_id AS OrganizationId, participant_actor_id AS ParticipantActorId,
                       enrollment_id AS EnrollmentId, action AS Action, idempotency_key AS IdempotencyKey,
                       command_digest AS CommandDigest, status AS Status, claim_owner AS ClaimOwner,
                       claimed_at AS ClaimedAtUtc, lease_until AS LeaseUntilUtc, attempt_id AS AttemptId,
                       session_id AS SessionId, outcome_code AS OutcomeCode, finished_at AS FinishedAtUtc
                FROM submissions_attempt_start_operations
                WHERE organization_id = @OrganizationId
                  AND enrollment_id = @EnrollmentId
                  AND action = @Action
                  AND idempotency_key = @IdempotencyKey
                """,
                new
                {
                    OrganizationId = organizationId,
                    EnrollmentId = enrollmentId,
                    Action = AttemptOperationKinds.Start,
                    IdempotencyKey = idempotencyKey,
                },
                dbTransaction,
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<StartOperation>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, dbTransaction) = EnrollmentTransactionConnection.Required(transaction);
        var rows = await connection.QueryAsync<StartOperation>(
            new CommandDefinition(
                """
                SELECT organization_id AS OrganizationId, participant_actor_id AS ParticipantActorId,
                       enrollment_id AS EnrollmentId, action AS Action, idempotency_key AS IdempotencyKey,
                       command_digest AS CommandDigest, status AS Status, claim_owner AS ClaimOwner,
                       claimed_at AS ClaimedAtUtc, lease_until AS LeaseUntilUtc, attempt_id AS AttemptId,
                       session_id AS SessionId, outcome_code AS OutcomeCode, finished_at AS FinishedAtUtc
                FROM submissions_attempt_start_operations
                WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
                """,
                new { OrganizationId = organizationId, EnrollmentId = enrollmentId },
                dbTransaction,
                cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public Task UpsertAsync(
        StartOperation operation,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, dbTransaction) = EnrollmentTransactionConnection.Required(transaction);
        return connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO submissions_attempt_start_operations (
                    organization_id, enrollment_id, participant_actor_id, action, idempotency_key,
                    command_digest, status, claim_owner, claimed_at, lease_until, attempt_id,
                    session_id, outcome_code, finished_at)
                VALUES (
                    @OrganizationId, @EnrollmentId, @ParticipantActorId, @Action, @IdempotencyKey,
                    @CommandDigest, @Status, @ClaimOwner, @ClaimedAtUtc, @LeaseUntilUtc, @AttemptId,
                    @SessionId, @OutcomeCode, @FinishedAtUtc)
                ON CONFLICT (organization_id, enrollment_id, action, idempotency_key)
                DO UPDATE SET
                    command_digest = EXCLUDED.command_digest,
                    status = EXCLUDED.status,
                    claim_owner = EXCLUDED.claim_owner,
                    claimed_at = EXCLUDED.claimed_at,
                    lease_until = EXCLUDED.lease_until,
                    attempt_id = EXCLUDED.attempt_id,
                    session_id = EXCLUDED.session_id,
                    outcome_code = EXCLUDED.outcome_code,
                    finished_at = EXCLUDED.finished_at
                WHERE submissions_attempt_start_operations.status = 'claimed'
                """,
                operation,
                dbTransaction,
                cancellationToken: cancellationToken));
    }
}

public sealed class PostgresParticipantNoticePort(PostgresConnectionAccessor connections) : IParticipantNoticePort
{
    public async Task<IReadOnlyList<RequiredNoticeProjection>?> ListRequiredAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid baselineId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        const string frozenSql = """
            SELECT
                COALESCE(ref->>'sourceId', ref->>'source_id')::uuid AS SourceId,
                COALESCE(ref->>'sourceVersion', ref->>'source_version')::uuid AS SourceVersionId,
                COALESCE(ref->>'contentDigest', ref->>'content_digest') AS ContentDigest
            FROM assessment_activation_baselines b
            INNER JOIN assessment_cohort_baseline_bindings bind
                ON bind.organization_id = b.organization_id
               AND bind.activity_id = b.activity_id
               AND bind.baseline_id = b.baseline_id
               AND bind.cohort_id = @CohortId
            CROSS JOIN LATERAL jsonb_array_elements(
                COALESCE(b.document->'sourceReferences', b.document->'source_references', '[]'::jsonb)) ref
            WHERE b.organization_id = @OrganizationId
              AND b.activity_id = @ActivityId
              AND b.baseline_id = @BaselineId
              AND COALESCE(ref->>'sourceKey', ref->>'source_key') IN ('workflow', 'organization_policy')
            """;
        const string setSql = """
            SELECT source_id AS SourceId, source_version_id AS SourceVersionId, source_content_digest AS ContentDigest
            FROM configuration_participant_notice_projection_sets
            WHERE organization_id = @OrganizationId
              AND source_version_id = ANY(@SourceVersionIds)
            """;
        const string noticeSql = """
            SELECT notice_id AS NoticeId, notice_type AS NoticeType, required_outcome AS RequiredOutcome,
                   protected_content_ref AS ProtectedContentRef, source_version_id AS SourceVersionId,
                   content_digest AS ContentDigest, source_id AS SourceId
            FROM configuration_participant_notice_projections
            WHERE organization_id = @OrganizationId
              AND source_version_id = ANY(@SourceVersionIds)
            ORDER BY notice_id
            """;
        try
        {
            var (connection, dbTransaction, dispose) = await OpenAsync(transaction, cancellationToken);
            try
            {
                var frozen = (await connection.QueryAsync<FrozenNoticeSource>(
                    new CommandDefinition(
                        frozenSql,
                        new
                        {
                            OrganizationId = organizationId,
                            ActivityId = activityId,
                            CohortId = cohortId,
                            BaselineId = baselineId,
                        },
                        dbTransaction,
                        cancellationToken: cancellationToken))).ToArray();
                if (frozen.Length == 0)
                {
                    return null;
                }

                var versionIds = frozen.Select(item => item.SourceVersionId).ToArray();
                var registered = (await connection.QueryAsync<FrozenNoticeSource>(
                    new CommandDefinition(
                        setSql,
                        new
                        {
                            OrganizationId = organizationId,
                            SourceVersionIds = versionIds,
                        },
                        dbTransaction,
                        cancellationToken: cancellationToken))).ToArray();
                if (frozen.Any(item => !registered.Any(set =>
                        set.SourceId == item.SourceId
                        && set.SourceVersionId == item.SourceVersionId
                        && string.Equals(set.ContentDigest, item.ContentDigest, StringComparison.Ordinal))))
                {
                    return null;
                }

                var rows = await connection.QueryAsync<RequiredNoticeProjection>(
                    new CommandDefinition(
                        noticeSql,
                        new { OrganizationId = organizationId, SourceVersionIds = versionIds },
                        dbTransaction,
                        cancellationToken: cancellationToken));
                return rows.ToArray();
            }
            finally
            {
                if (dispose)
                {
                    await connection.DisposeAsync();
                }
            }
        }
        catch (PostgresException)
        {
            return null;
        }
    }

    private async Task<(NpgsqlConnection Connection, NpgsqlTransaction? Transaction, bool Dispose)> OpenAsync(
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            var (opened, dbTransaction) = EnrollmentTransactionConnection.Required(transaction);
            return (opened, dbTransaction, false);
        }

        var connection = await connections.OpenConnectionAsync(cancellationToken);
        return (connection, null, true);
    }

    private sealed record FrozenNoticeSource(Guid SourceId, Guid SourceVersionId, string ContentDigest);
}
