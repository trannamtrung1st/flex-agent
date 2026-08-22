using System.Data;
using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using Npgsql;

namespace FlexAgent.Submissions.Infrastructure;

public sealed class PostgresEnrollmentTransaction(PostgresTransactionScope scope) : IEnrollmentTransaction
{
    public bool AuditAccepted { get; set; } = true;

    public bool OutboxAccepted { get; set; } = true;

    public object CommitHandle => scope.Transaction;

    public PostgresTransactionScope Scope => scope;
}

public sealed class PostgresEnrollmentUnitOfWork(
    PostgresConnectionAccessor connections,
    IEnrollmentSessionPort sessions) : IEnrollmentUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(
        EnrollmentActorContext actor,
        Func<IEnrollmentTransaction, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var scope = await PostgresTransactionScope.BeginAsync(connections, cancellationToken);
        var transaction = new PostgresEnrollmentTransaction(scope);
        var result = await action(transaction);
        if (!await sessions.ConfirmLiveAsync(actor, transaction, cancellationToken))
        {
            throw new EnrollmentSessionExpiredException();
        }

        await scope.CommitAsync(cancellationToken);
        return result;
    }
}

public sealed class KernelEnrollmentAuthorizationPort(
    IAuthorizationKernel kernel,
    ICommitAuthorizationKernel commitKernel) : IEnrollmentAuthorizationPort
{
    public Task<AuthorizationDecision> AuthorizeAdmissionAsync(
        EnrollmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        CancellationToken cancellationToken = default) =>
        kernel.AuthorizeAsync(
            new AuthorizationRequest(
                actor.Actor,
                actor.Organization,
                action,
                new ResourceScope(actor.Organization, resourceType, resourceId),
                actor.SourceChannel,
                actor.CorrelationId),
            cancellationToken);

    public Task<AuthorizationDecision> ReauthorizeAsync(
        EnrollmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default) =>
        commitKernel.ReauthorizeInTransactionAsync(
            new AuthorizationRequest(
                actor.Actor,
                actor.Organization,
                action,
                new ResourceScope(actor.Organization, resourceType, resourceId),
                actor.SourceChannel,
                actor.CorrelationId),
            (NpgsqlTransaction)transaction.CommitHandle,
            cancellationToken);
}

public sealed class PostgresEnrollmentAuditPort(
    IAuditEventWriter auditEventWriter,
    IOutboxItemWriter outboxItemWriter) : IEnrollmentAuditPort
{
    public async Task WriteRequiredDurableAsync(
        EnrollmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        string outcome,
        string? reasonCode,
        AuthorizationDecision? authorization,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var npgsql = (NpgsqlTransaction)transaction.CommitHandle;
        try
        {
            await auditEventWriter.InsertAsync(
                new AuditEventWriteModel(
                    Guid.CreateVersion7(),
                    actor.Organization.OrganizationId,
                    "v1",
                    DateTimeOffset.UtcNow,
                    actor.CorrelationId,
                    actor.Actor.ActorType,
                    actor.Actor.ActorId,
                    action,
                    resourceType,
                    resourceId,
                    outcome,
                    reasonCode,
                    authorization?.RelationshipVersion,
                    actor.SourceChannel,
                    null,
                    authorization?.AuthorizationReferenceType,
                    authorization?.AuthorizationReferenceId),
                npgsql,
                cancellationToken);
        }
        catch
        {
            transaction.AuditAccepted = false;
            throw new EnrollmentAuditUnavailableException();
        }
    }

    public async Task WriteAvailabilityAsync(
        Enrollment enrollment,
        EnrollmentActorContext actor,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var npgsql = (NpgsqlTransaction)transaction.CommitHandle;
        try
        {
            await outboxItemWriter.InsertAsync(
                new OutboxItemWriteModel(
                    Guid.CreateVersion7(),
                    enrollment.OrganizationId,
                    "enrollment.assigned",
                    EnrollmentResourceTypes.Enrollment,
                    enrollment.EnrollmentId,
                    actor.CorrelationId,
                    enrollment.TaskContentDigest,
                    enrollment.AssignedAtUtc),
                npgsql,
                cancellationToken);
        }
        catch
        {
            transaction.OutboxAccepted = false;
            throw new EnrollmentAuditUnavailableException();
        }
    }
}

public sealed class PostgresEnrollmentStore(PostgresConnectionAccessor connections) : IEnrollmentStore
{
    public async Task<Enrollment?> FindAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is PostgresEnrollmentTransaction postgres)
        {
            var locked = await postgres.Scope.Connection.QuerySingleOrDefaultAsync<EnrollmentRow>(
                new CommandDefinition(
                    SelectSql + " WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId FOR UPDATE",
                    new { OrganizationId = organizationId, EnrollmentId = enrollmentId },
                    postgres.Scope.Transaction,
                    cancellationToken: cancellationToken));
            return locked?.ToEnrollment();
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<EnrollmentRow>(
            new CommandDefinition(
                SelectSql + " WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId",
                new { OrganizationId = organizationId, EnrollmentId = enrollmentId },
                cancellationToken: cancellationToken));
        return row?.ToEnrollment();
    }

    public async Task<Enrollment?> FindLiveForParticipantAsync(
        Guid organizationId,
        Guid activityId,
        Guid participantActorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        var row = await postgres.Scope.Connection.QuerySingleOrDefaultAsync<EnrollmentRow>(
            new CommandDefinition(
                SelectSql + """
                     WHERE organization_id = @OrganizationId
                       AND activity_id = @ActivityId
                       AND participant_actor_id = @ParticipantActorId
                       AND status IN ('active', 'suspended')
                    FOR UPDATE
                    """,
                new { OrganizationId = organizationId, ActivityId = activityId, ParticipantActorId = participantActorId },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
        return row?.ToEnrollment();
    }

    public async Task InsertAsync(
        Enrollment enrollment,
        EnrollmentEvent enrollmentEvent,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        var inserted = await postgres.Scope.Connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                """
                INSERT INTO submissions_enrollments (
                    organization_id, enrollment_id, activity_id, cohort_id, baseline_id,
                    task_source_id, task_version_id, task_content_digest, lifecycle_policy_id,
                    lifecycle_policy_version, participant_actor_id, status, revision,
                    assigned_by_actor_id, assigned_at, updated_at)
                VALUES (
                    @OrganizationId, @EnrollmentId, @ActivityId, @CohortId, @BaselineId,
                    @TaskSourceId, @TaskVersionId, @TaskContentDigest, @LifecyclePolicyId,
                    @LifecyclePolicyVersion, @ParticipantActorId, @Status, @Revision,
                    @AssignedByActorId, @AssignedAtUtc, @UpdatedAtUtc)
                ON CONFLICT (organization_id, activity_id, participant_actor_id)
                    WHERE status IN ('active', 'suspended')
                DO NOTHING
                RETURNING enrollment_id
                """,
                enrollment,
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
        if (inserted is null)
        {
            throw new EnrollmentLiveUniquenessException();
        }

        await InsertEventAsync(enrollmentEvent, postgres, cancellationToken);
    }

    public async Task UpdateAsync(
        Enrollment enrollment,
        EnrollmentEvent enrollmentEvent,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        var updated = await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_enrollments
                SET status = @Status, revision = @Revision, updated_at = @UpdatedAtUtc
                WHERE organization_id = @OrganizationId
                  AND enrollment_id = @EnrollmentId
                  AND revision = @ExpectedRevision
                """,
                new
                {
                    enrollment.OrganizationId,
                    enrollment.EnrollmentId,
                    enrollment.Status,
                    enrollment.Revision,
                    enrollment.UpdatedAtUtc,
                    ExpectedRevision = enrollment.Revision - 1,
                },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
        if (updated != 1)
        {
            throw new EnrollmentStaleRevisionException();
        }

        await InsertEventAsync(enrollmentEvent, postgres, cancellationToken);
    }

    public async Task<IReadOnlyList<EnrollmentHistoryItem>> ListHistoryAsync(
        Guid organizationId,
        Guid enrollmentId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<EnrollmentHistoryItem>(
            new CommandDefinition(
                """
                SELECT sequence, prior_status AS PriorStatus, new_status AS NewStatus,
                       reason_code AS ReasonCode, occurred_at AS OccurredAtUtc
                FROM submissions_enrollment_events
                WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
                ORDER BY sequence
                """,
                new { OrganizationId = organizationId, EnrollmentId = enrollmentId },
                cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public Task<CursorPage<Enrollment>> ListForCohortAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken) =>
        ListAsync(
            """
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId
              AND activity_id = @ActivityId
              AND cohort_id = @CohortId
            """,
            new { OrganizationId = organizationId, ActivityId = activityId, CohortId = cohortId },
            cursor,
            limit,
            cancellationToken);

    public Task<CursorPage<Enrollment>> ListCurrentForParticipantAsync(
        Guid organizationId,
        Guid participantActorId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken) =>
        ListAsync(
            """
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId
              AND participant_actor_id = @ParticipantActorId
              AND status IN ('active', 'suspended')
            """,
            new { OrganizationId = organizationId, ParticipantActorId = participantActorId },
            cursor,
            limit,
            cancellationToken);

    private async Task<CursorPage<Enrollment>> ListAsync(
        string fromWhere,
        object parameters,
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        EnrollmentCursor.TryParse(cursor, out var afterTime, out var afterId);
        var dynamicParameters = new DynamicParameters(parameters);
        dynamicParameters.Add("AfterTime", afterTime, DbType.DateTimeOffset);
        dynamicParameters.Add("AfterId", afterId, DbType.Guid);
        dynamicParameters.Add("Limit", limit + 1, DbType.Int32);
        var rows = (await connection.QueryAsync<EnrollmentRow>(
            new CommandDefinition(
                SelectList + $"""
                 {fromWhere}
                   AND (@AfterTime IS NULL
                        OR updated_at > @AfterTime
                        OR (updated_at = @AfterTime AND enrollment_id > @AfterId))
                 ORDER BY updated_at, enrollment_id
                 LIMIT @Limit
                """,
                dynamicParameters,
                cancellationToken: cancellationToken))).ToArray();
        var hasMore = rows.Length > limit;
        var taken = rows.Take(limit).Select(row => row.ToEnrollment()).ToArray();
        return new CursorPage<Enrollment>(
            taken,
            hasMore ? EnrollmentCursor.Format(taken[^1].UpdatedAtUtc, taken[^1].EnrollmentId) : null,
            hasMore);
    }

    private static async Task InsertEventAsync(
        EnrollmentEvent enrollmentEvent,
        PostgresEnrollmentTransaction transaction,
        CancellationToken cancellationToken) =>
        await transaction.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO submissions_enrollment_events (
                    organization_id, enrollment_id, event_id, sequence, prior_status, new_status,
                    reason_code, actor_id, occurred_at, correlation_id, authorization_reference_id,
                    enrollment_revision)
                VALUES (
                    @OrganizationId, @EnrollmentId, @EventId, @Sequence, @PriorStatus, @NewStatus,
                    @ReasonCode, @ActorId, @OccurredAtUtc, @CorrelationId, @AuthorizationReferenceId,
                    @EnrollmentRevision)
                """,
                enrollmentEvent,
                transaction.Scope.Transaction,
                cancellationToken: cancellationToken));

    private const string SelectList = """
        SELECT organization_id AS OrganizationId, enrollment_id AS EnrollmentId, activity_id AS ActivityId,
               cohort_id AS CohortId, baseline_id AS BaselineId, task_source_id AS TaskSourceId,
               task_version_id AS TaskVersionId, task_content_digest AS TaskContentDigest,
               lifecycle_policy_id AS LifecyclePolicyId, lifecycle_policy_version AS LifecyclePolicyVersion,
               participant_actor_id AS ParticipantActorId, status AS Status, revision AS Revision,
               assigned_by_actor_id AS AssignedByActorId, assigned_at AS AssignedAtUtc, updated_at AS UpdatedAtUtc
        """;

    private const string SelectSql = SelectList + " FROM submissions_enrollments";

    private sealed record EnrollmentRow(
        Guid OrganizationId,
        Guid EnrollmentId,
        Guid ActivityId,
        Guid CohortId,
        Guid BaselineId,
        Guid TaskSourceId,
        Guid TaskVersionId,
        string TaskContentDigest,
        Guid LifecyclePolicyId,
        int LifecyclePolicyVersion,
        Guid ParticipantActorId,
        string Status,
        long Revision,
        Guid AssignedByActorId,
        DateTimeOffset AssignedAtUtc,
        DateTimeOffset UpdatedAtUtc)
    {
        public Enrollment ToEnrollment() => new(
            EnrollmentId,
            OrganizationId,
            ActivityId,
            CohortId,
            BaselineId,
            TaskSourceId,
            TaskVersionId,
            TaskContentDigest,
            LifecyclePolicyId,
            LifecyclePolicyVersion,
            ParticipantActorId,
            Status,
            Revision,
            AssignedByActorId,
            AssignedAtUtc,
            UpdatedAtUtc);
    }
}

public sealed class PostgresEnrollmentOperationStore : IEnrollmentOperationStore
{
    public async Task AcquireLockAsync(
        Guid organizationId,
        Guid actorId,
        string operationKind,
        Guid resourceId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                "SELECT pg_advisory_xact_lock(hashtextextended(@Key, 0))",
                new { Key = $"{organizationId:D}:{actorId:D}:{operationKind}:{resourceId:D}:{idempotencyKey}" },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task AcquireLiveParticipantLockAsync(
        Guid organizationId,
        Guid activityId,
        Guid participantActorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                "SELECT pg_advisory_xact_lock(hashtextextended(@Key, 0))",
                new { Key = $"enrollment.live:{organizationId:D}:{activityId:D}:{participantActorId:D}" },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task<EnrollmentOperation?> FindAsync(
        Guid organizationId,
        Guid actorId,
        string operationKind,
        Guid resourceId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        return await postgres.Scope.Connection.QuerySingleOrDefaultAsync<EnrollmentOperation>(
            new CommandDefinition(
                """
                SELECT organization_id AS OrganizationId, actor_id AS ActorId, operation_kind AS OperationKind,
                       resource_id AS ResourceId, idempotency_key AS IdempotencyKey, command_digest AS CommandDigest,
                       outcome_code AS OutcomeCode, enrollment_id AS EnrollmentId, created_at AS CreatedAtUtc,
                       expires_at AS ExpiresAtUtc
                FROM submissions_enrollment_operations
                WHERE organization_id = @OrganizationId
                  AND actor_id = @ActorId
                  AND operation_kind = @OperationKind
                  AND resource_id = @ResourceId
                  AND idempotency_key = @IdempotencyKey
                """,
                new
                {
                    OrganizationId = organizationId,
                    ActorId = actorId,
                    OperationKind = operationKind,
                    ResourceId = resourceId,
                    IdempotencyKey = idempotencyKey,
                },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(
        EnrollmentOperation operation,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO submissions_enrollment_operations (
                    organization_id, actor_id, operation_kind, resource_id, idempotency_key,
                    command_digest, outcome_code, enrollment_id, created_at, expires_at)
                VALUES (
                    @OrganizationId, @ActorId, @OperationKind, @ResourceId, @IdempotencyKey,
                    @CommandDigest, @OutcomeCode, @EnrollmentId, @CreatedAtUtc, @ExpiresAtUtc)
                """,
                operation,
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
    }
}

internal static class EnrollmentCursor
{
    public static string Format(DateTimeOffset updatedAt, Guid enrollmentId) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{updatedAt.UtcTicks}:{enrollmentId:D}"));

    public static bool TryParse(string? cursor, out DateTimeOffset? updatedAt, out Guid? enrollmentId)
    {
        updatedAt = null;
        enrollmentId = null;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = decoded.Split(':', 2);
            if (parts.Length == 2
                && long.TryParse(parts[0], out var ticks)
                && Guid.TryParse(parts[1], out var id))
            {
                updatedAt = new DateTimeOffset(ticks, TimeSpan.Zero);
                enrollmentId = id;
                return true;
            }
        }
        catch (FormatException)
        {
        }

        return false;
    }
}
