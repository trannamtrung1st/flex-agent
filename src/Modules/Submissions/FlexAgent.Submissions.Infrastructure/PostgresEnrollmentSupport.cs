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
        DateTimeOffset? afterTime,
        Guid? afterId,
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
            afterTime,
            afterId,
            limit,
            cancellationToken);

    public Task<CursorPage<Enrollment>> ListCurrentForParticipantAsync(
        Guid organizationId,
        Guid participantActorId,
        DateTimeOffset? afterTime,
        Guid? afterId,
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
            afterTime,
            afterId,
            limit,
            cancellationToken);

    private async Task<CursorPage<Enrollment>> ListAsync(
        string fromWhere,
        object parameters,
        DateTimeOffset? afterTime,
        Guid? afterId,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
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
        return new CursorPage<Enrollment>(taken, null, hasMore);
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


public sealed class PostgresAccommodationStore(PostgresConnectionAccessor connections) : IAccommodationStore
{
    private const string SelectSql = """
        SELECT organization_id AS OrganizationId, accommodation_id AS AccommodationId, activity_id AS ActivityId,
               cohort_id AS CohortId, baseline_id AS BaselineId, enrollment_id AS EnrollmentId,
               participant_actor_id AS ParticipantActorId, dimension AS Dimension, normalized_value AS NormalizedValue,
               frozen_policy_id AS FrozenPolicyId, frozen_policy_version_id AS FrozenPolicyVersionId,
               frozen_policy_digest AS FrozenPolicyDigest, decision_policy_id AS DecisionPolicyId,
               decision_policy_version_id AS DecisionPolicyVersionId, decision_policy_digest AS DecisionPolicyDigest,
               reason_category AS ReasonCategory, status AS Status, revision AS Revision,
               requester_actor_id AS RequesterActorId, approver_actor_id AS ApproverActorId,
               created_at AS CreatedAtUtc, decided_at AS DecidedAtUtc, expires_at AS ExpiresAtUtc,
               revoked_at AS RevokedAtUtc, superseded_by_accommodation_id AS SupersededByAccommodationId,
               fairness_exception AS FairnessException, lifecycle_policy_id AS LifecyclePolicyId,
               lifecycle_policy_version AS LifecyclePolicyVersion
        FROM submissions_accommodations
        """;

    public async Task<Accommodation?> FindAsync(
        Guid organizationId,
        Guid accommodationId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is PostgresEnrollmentTransaction postgres)
        {
            var row = await postgres.Scope.Connection.QuerySingleOrDefaultAsync<AccommodationRow>(
                new CommandDefinition(
                    SelectSql + " WHERE organization_id = @OrganizationId AND accommodation_id = @AccommodationId FOR UPDATE",
                    new { OrganizationId = organizationId, AccommodationId = accommodationId },
                    postgres.Scope.Transaction,
                    cancellationToken: cancellationToken));
            return row?.ToAccommodation();
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var unlocked = await connection.QuerySingleOrDefaultAsync<AccommodationRow>(
            new CommandDefinition(
                SelectSql + " WHERE organization_id = @OrganizationId AND accommodation_id = @AccommodationId",
                new { OrganizationId = organizationId, AccommodationId = accommodationId },
                cancellationToken: cancellationToken));
        return unlocked?.ToAccommodation();
    }

    public async Task<IReadOnlyList<Accommodation>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken)
    {
        async Task<IReadOnlyList<Accommodation>> QueryAsync(System.Data.IDbConnection connection, System.Data.IDbTransaction? dbTransaction)
        {
            var rows = await connection.QueryAsync<AccommodationRow>(
                new CommandDefinition(
                    SelectSql + """
                         WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
                         ORDER BY created_at, accommodation_id
                        """,
                    new { OrganizationId = organizationId, EnrollmentId = enrollmentId },
                    dbTransaction,
                    cancellationToken: cancellationToken));
            return rows.Select(row => row.ToAccommodation()).ToArray();
        }

        if (transaction is PostgresEnrollmentTransaction postgres)
        {
            return await QueryAsync(postgres.Scope.Connection, postgres.Scope.Transaction);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        return await QueryAsync(connection, null);
    }

    public async Task InsertAsync(
        Accommodation accommodation,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        try
        {
            await postgres.Scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO submissions_accommodations (
                        organization_id, accommodation_id, activity_id, cohort_id, baseline_id, enrollment_id,
                        participant_actor_id, dimension, normalized_value, frozen_policy_id, frozen_policy_version_id,
                        frozen_policy_digest, decision_policy_id, decision_policy_version_id, decision_policy_digest,
                        reason_category, status, revision, requester_actor_id, approver_actor_id, created_at,
                        decided_at, expires_at, revoked_at, superseded_by_accommodation_id, fairness_exception,
                        lifecycle_policy_id, lifecycle_policy_version)
                    VALUES (
                        @OrganizationId, @AccommodationId, @ActivityId, @CohortId, @BaselineId, @EnrollmentId,
                        @ParticipantActorId, @Dimension, @NormalizedValue, @FrozenPolicyId, @FrozenPolicyVersionId,
                        @FrozenPolicyDigest, @DecisionPolicyId, @DecisionPolicyVersionId, @DecisionPolicyDigest,
                        @ReasonCategory, @Status, @Revision, @RequesterActorId, @ApproverActorId, @CreatedAtUtc,
                        @DecidedAtUtc, @ExpiresAtUtc, @RevokedAtUtc, @SupersededByAccommodationId, @FairnessException,
                        @LifecyclePolicyId, @LifecyclePolicyVersion)
                    """,
                    ToRow(accommodation),
                    postgres.Scope.Transaction,
                    cancellationToken: cancellationToken));
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new EnrollmentLiveUniquenessException();
        }
        await InsertFactAsync(accommodation, null, actorId, postgres, cancellationToken);
    }

    public async Task UpdateAsync(
        Accommodation accommodation,
        string? priorStatus,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var postgres = (PostgresEnrollmentTransaction)transaction;
        var updated = await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_accommodations
                SET normalized_value = @NormalizedValue,
                    decision_policy_id = @DecisionPolicyId,
                    decision_policy_version_id = @DecisionPolicyVersionId,
                    decision_policy_digest = @DecisionPolicyDigest,
                    status = @Status,
                    revision = @Revision,
                    approver_actor_id = @ApproverActorId,
                    decided_at = @DecidedAtUtc,
                    expires_at = @ExpiresAtUtc,
                    revoked_at = @RevokedAtUtc,
                    superseded_by_accommodation_id = @SupersededByAccommodationId
                WHERE organization_id = @OrganizationId
                  AND accommodation_id = @AccommodationId
                  AND revision = @ExpectedRevision
                """,
                ToRow(accommodation) with { ExpectedRevision = accommodation.Revision - 1 },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));
        if (updated != 1)
        {
            throw new EnrollmentStaleRevisionException();
        }

        await InsertFactAsync(accommodation, priorStatus, actorId, postgres, cancellationToken);
    }

    private static async Task InsertFactAsync(
        Accommodation accommodation,
        string? priorStatus,
        Guid actorId,
        PostgresEnrollmentTransaction postgres,
        CancellationToken cancellationToken) =>
        await postgres.Scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO submissions_accommodation_facts (
                    organization_id, accommodation_id, fact_id, sequence, prior_status, new_status,
                    reason_category, actor_id, occurred_at)
                VALUES (
                    @OrganizationId, @AccommodationId, @FactId, @Sequence, @PriorStatus, @NewStatus,
                    @ReasonCategory, @ActorId, @OccurredAtUtc)
                """,
                new
                {
                    OrganizationId = accommodation.Parent.OrganizationId,
                    AccommodationId = accommodation.AccommodationId,
                    FactId = Guid.CreateVersion7(),
                    Sequence = accommodation.Revision,
                    PriorStatus = priorStatus,
                    NewStatus = accommodation.Status,
                    ReasonCategory = accommodation.ReasonCategory,
                    ActorId = actorId,
                    OccurredAtUtc = accommodation.RevokedAtUtc ?? accommodation.DecidedAtUtc ?? accommodation.CreatedAtUtc,
                },
                postgres.Scope.Transaction,
                cancellationToken: cancellationToken));

    private static AccommodationRow ToRow(Accommodation accommodation) =>
        new(
            accommodation.Parent.OrganizationId,
            accommodation.AccommodationId,
            accommodation.Parent.ActivityId,
            accommodation.Parent.CohortId,
            accommodation.Parent.BaselineId,
            accommodation.Parent.EnrollmentId,
            accommodation.Parent.ParticipantActorId,
            accommodation.Dimension,
            accommodation.NormalizedValue,
            accommodation.FrozenPolicy.PolicyId,
            accommodation.FrozenPolicy.VersionId,
            accommodation.FrozenPolicy.Digest,
            accommodation.DecisionPolicy.PolicyId,
            accommodation.DecisionPolicy.VersionId,
            accommodation.DecisionPolicy.Digest,
            accommodation.ReasonCategory,
            accommodation.Status,
            accommodation.Revision,
            accommodation.RequesterActorId,
            accommodation.ApproverActorId,
            accommodation.CreatedAtUtc,
            accommodation.DecidedAtUtc,
            accommodation.ExpiresAtUtc,
            accommodation.RevokedAtUtc,
            accommodation.SupersededByAccommodationId,
            accommodation.FairnessException,
            accommodation.LifecyclePolicyId,
            accommodation.LifecyclePolicyVersion,
            0);

    private sealed record AccommodationRow(
        Guid OrganizationId,
        Guid AccommodationId,
        Guid ActivityId,
        Guid CohortId,
        Guid BaselineId,
        Guid EnrollmentId,
        Guid ParticipantActorId,
        string Dimension,
        string NormalizedValue,
        Guid FrozenPolicyId,
        Guid FrozenPolicyVersionId,
        string FrozenPolicyDigest,
        Guid DecisionPolicyId,
        Guid DecisionPolicyVersionId,
        string DecisionPolicyDigest,
        string ReasonCategory,
        string Status,
        long Revision,
        Guid RequesterActorId,
        Guid? ApproverActorId,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? DecidedAtUtc,
        DateTimeOffset? ExpiresAtUtc,
        DateTimeOffset? RevokedAtUtc,
        Guid? SupersededByAccommodationId,
        bool FairnessException,
        Guid LifecyclePolicyId,
        int LifecyclePolicyVersion,
        long ExpectedRevision = 0)
    {
        public Accommodation ToAccommodation() =>
            new(
                AccommodationId,
                new AccommodationParentBinding(
                    OrganizationId,
                    ActivityId,
                    CohortId,
                    BaselineId,
                    EnrollmentId,
                    ParticipantActorId),
                Dimension,
                NormalizedValue,
                new AccommodationPolicyIdentity(FrozenPolicyId, FrozenPolicyVersionId, FrozenPolicyDigest),
                new AccommodationPolicyIdentity(DecisionPolicyId, DecisionPolicyVersionId, DecisionPolicyDigest),
                ReasonCategory,
                Status,
                Revision,
                RequesterActorId,
                ApproverActorId,
                CreatedAtUtc,
                DecidedAtUtc,
                ExpiresAtUtc,
                RevokedAtUtc,
                SupersededByAccommodationId,
                FairnessException,
                LifecyclePolicyId,
                LifecyclePolicyVersion);
    }
}
