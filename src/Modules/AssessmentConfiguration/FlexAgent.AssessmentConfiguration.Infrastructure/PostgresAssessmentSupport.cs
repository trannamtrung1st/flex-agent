using System.Text.Json;
using Dapper;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using Npgsql;

namespace FlexAgent.AssessmentConfiguration.Infrastructure;

public sealed class PostgresAssessmentTransaction(PostgresTransactionScope scope) : IAssessmentActivationTransaction
{
    public bool AuditAccepted { get; set; } = true;

    public bool OutboxAccepted { get; set; } = true;

    public object? PersistenceContext => scope.Transaction;

    public PostgresTransactionScope Scope => scope;
}

public sealed class PostgresAssessmentUnitOfWork(PostgresConnectionAccessor connections) : IAssessmentActivationUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(
        Func<IAssessmentActivationTransaction, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var scope = await PostgresTransactionScope.BeginAsync(connections, cancellationToken);
        var transaction = new PostgresAssessmentTransaction(scope);
        var result = await action(transaction);
        await scope.CommitAsync(cancellationToken);
        return result;
    }
}

public sealed class PostgresAssessmentBaselineStore(
    IAuditEventWriter auditEventWriter,
    IOutboxItemWriter outboxItemWriter) : IAssessmentBaselineStore
{
    public async Task InsertAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid baselineId,
        ActivationBaselineDocument document,
        string contentDigest,
        IAssessmentActivationTransaction transaction,
        AssessmentActorContext actor,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        if (transaction.PersistenceContext is not NpgsqlTransaction npgsql)
        {
            throw new InvalidOperationException("Assessment baseline persistence requires the PostgreSQL activation transaction.");
        }

        var payload = JsonSerializer.Serialize(document, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await npgsql.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO assessment_activation_baselines (
                    organization_id, activity_id, baseline_id, content_digest, procedure_id,
                    schema_version, canonicalization_version, document, created_at,
                    actor_id, correlation_id)
                VALUES (
                    @OrganizationId, @ActivityId, @BaselineId, @ContentDigest, @ProcedureId,
                    @SchemaVersion, @CanonicalizationVersion, @Document::jsonb, @OccurredAtUtc,
                    @ActorId, @CorrelationId)
                """,
                new
                {
                    OrganizationId = organizationId,
                    ActivityId = activityId,
                    BaselineId = baselineId,
                    ContentDigest = contentDigest,
                    ProcedureId = document.ProcedureId,
                    SchemaVersion = document.SchemaVersion,
                    CanonicalizationVersion = document.CanonicalizationVersion,
                    Document = payload,
                    OccurredAtUtc = occurredAtUtc,
                    ActorId = actor.Actor.ActorId,
                    CorrelationId = actor.CorrelationId,
                },
                npgsql,
                cancellationToken: cancellationToken));

        await npgsql.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO assessment_cohort_baseline_bindings (
                    organization_id, activity_id, cohort_id, baseline_id, bound_at)
                VALUES (
                    @OrganizationId, @ActivityId, @CohortId, @BaselineId, CLOCK_TIMESTAMP())
                """,
                new { OrganizationId = organizationId, ActivityId = activityId, CohortId = cohortId, BaselineId = baselineId },
                npgsql,
                cancellationToken: cancellationToken));

        await auditEventWriter.InsertAsync(
            new AuditEventWriteModel(
                Guid.CreateVersion7(),
                organizationId,
                "v1",
                occurredAtUtc,
                actor.CorrelationId,
                actor.Actor.ActorType,
                actor.Actor.ActorId,
                AssessmentAuthorizationActions.ActivateCohort,
                AssessmentResourceTypes.Baseline,
                baselineId,
                "permit",
                null,
                1,
                actor.SourceChannel,
                contentDigest),
            npgsql,
            cancellationToken);
        await outboxItemWriter.InsertAsync(
            new OutboxItemWriteModel(
                Guid.CreateVersion7(),
                organizationId,
                "assessment.cohort.activated",
                AssessmentResourceTypes.Cohort,
                activityId,
                actor.CorrelationId,
                contentDigest,
                occurredAtUtc),
            npgsql,
            cancellationToken);
    }
}

public sealed class KernelAssessmentAuthorizationPort(
    IAuthorizationKernel kernel,
    ICommitAuthorizationKernel commitKernel) : IAssessmentAuthorizationPort
{
    public Task<AuthorizationDecision> AuthorizeAdmissionAsync(
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        CancellationToken cancellationToken) =>
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
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (transaction.PersistenceContext is not NpgsqlTransaction npgsql)
        {
            return AuthorizeAdmissionAsync(actor, action, resourceId, resourceType, cancellationToken);
        }

        return commitKernel.ReauthorizeInTransactionAsync(
            new AuthorizationRequest(
                actor.Actor,
                actor.Organization,
                action,
                new ResourceScope(actor.Organization, resourceType, resourceId),
                actor.SourceChannel,
                actor.CorrelationId),
            npgsql,
            cancellationToken);
    }
}

public sealed class PostgresAssessmentSourceCatalog(PostgresConnectionAccessor connections)
    : IAssessmentSourceCatalog, IAssessmentSourceTransactionPort
{
    public Task<IReadOnlyList<TrustedSourceDescriptor>> LoadExactAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
        CancellationToken cancellationToken) =>
        LoadAsync(organizationId, references, transaction: null, cancellationToken);

    public Task<IReadOnlyList<TrustedSourceDescriptor>> RevalidateExactAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken) =>
        LoadAsync(
            organizationId,
            references,
            transaction.PersistenceContext as NpgsqlTransaction,
            cancellationToken);

    public Task<IReadOnlyList<TrustedSourceDescriptor>> ListSelectableAsync(
        Guid organizationId,
        string environment,
        CancellationToken cancellationToken) =>
        LoadSelectableAsync(organizationId, environment, cancellationToken);

    private async Task<IReadOnlyList<TrustedSourceDescriptor>> LoadSelectableAsync(
        Guid organizationId,
        string environment,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                d.organization_id,
                d.configuration_source_id AS source_id,
                d.version_id,
                d.source_kind,
                d.category,
                v.content_digest,
                d.lifecycle_state,
                d.compatibility_key,
                d.capability_text_enabled,
                d.capability_voice_enabled,
                d.capability_tools_enabled,
                d.capability_dynamic_memory_writes_enabled,
                d.capability_shared_session_enabled,
                d.capability_direct_deployment_enabled,
                d.production_eligible,
                d.transactionally_revalidatable,
                d.effective_values
            FROM configuration_source_readiness_descriptors d
            INNER JOIN configuration_source_versions v
                ON v.organization_id = d.organization_id
               AND v.configuration_source_id = d.configuration_source_id
               AND v.id = d.version_id
            WHERE d.organization_id = @OrganizationId
              AND d.lifecycle_state = 'available'
              AND d.transactionally_revalidatable = TRUE
              AND (@RequireProductionEligible = FALSE OR d.production_eligible = TRUE)
            """;

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<DescriptorRow>(
            new CommandDefinition(
                sql,
                new
                {
                    OrganizationId = organizationId,
                    RequireProductionEligible = environment == DeploymentEnvironments.Production,
                },
                cancellationToken: cancellationToken));
        return rows.Select(ToDescriptor).ToArray();
    }

    private async Task<IReadOnlyList<TrustedSourceDescriptor>> LoadAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (references.Count == 0)
        {
            return [];
        }

        const string sql = """
            SELECT
                d.organization_id,
                d.configuration_source_id AS source_id,
                d.version_id,
                d.source_kind,
                d.category,
                v.content_digest,
                d.lifecycle_state,
                d.compatibility_key,
                d.capability_text_enabled,
                d.capability_voice_enabled,
                d.capability_tools_enabled,
                d.capability_dynamic_memory_writes_enabled,
                d.capability_shared_session_enabled,
                d.capability_direct_deployment_enabled,
                d.production_eligible,
                d.transactionally_revalidatable,
                d.effective_values
            FROM configuration_source_readiness_descriptors d
            INNER JOIN configuration_source_versions v
                ON v.organization_id = d.organization_id
               AND v.configuration_source_id = d.configuration_source_id
               AND v.id = d.version_id
            WHERE d.organization_id = @OrganizationId
              AND d.version_id = ANY(@VersionIds)
            FOR SHARE
            """;

        IEnumerable<DescriptorRow> rows;
        if (transaction is null)
        {
            await using var connection = await connections.OpenConnectionAsync(cancellationToken);
            rows = await connection.QueryAsync<DescriptorRow>(
                new CommandDefinition(
                    sql,
                    new { OrganizationId = organizationId, VersionIds = references.Select(item => item.VersionId).ToArray() },
                    cancellationToken: cancellationToken));
        }
        else
        {
            rows = await transaction.Connection!.QueryAsync<DescriptorRow>(
                new CommandDefinition(
                    sql,
                    new { OrganizationId = organizationId, VersionIds = references.Select(item => item.VersionId).ToArray() },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        return rows.Select(ToDescriptor).ToArray();
    }

    private static TrustedSourceDescriptor ToDescriptor(DescriptorRow row) =>
        new(
            row.OrganizationId,
            row.SourceId,
            row.VersionId,
            row.SourceKind,
            row.Category,
            row.ContentDigest,
            row.LifecycleState,
            row.CompatibilityKey,
            new CapabilityBounds(
                row.CapabilityTextEnabled,
                row.CapabilityVoiceEnabled,
                row.CapabilityToolsEnabled,
                row.CapabilityDynamicMemoryWritesEnabled,
                row.CapabilitySharedSessionEnabled,
                row.CapabilityDirectDeploymentEnabled,
                []),
            JsonSerializer.Deserialize<Dictionary<string, string>>(row.EffectiveValues)
                ?? new Dictionary<string, string>(),
            row.TransactionallyRevalidatable,
            row.ProductionEligible);

    private sealed record DescriptorRow(
        Guid OrganizationId,
        Guid SourceId,
        Guid VersionId,
        string SourceKind,
        string Category,
        string ContentDigest,
        string LifecycleState,
        string CompatibilityKey,
        bool CapabilityTextEnabled,
        bool CapabilityVoiceEnabled,
        bool CapabilityToolsEnabled,
        bool CapabilityDynamicMemoryWritesEnabled,
        bool CapabilitySharedSessionEnabled,
        bool CapabilityDirectDeploymentEnabled,
        bool ProductionEligible,
        bool TransactionallyRevalidatable,
        string EffectiveValues);
}

public sealed class PostgresAssessmentAttemptStore(IAuditEventWriter auditEventWriter)
    : IAssessmentActivationAttemptStore
{
    public async Task AcquireIdempotencyLockAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (transaction.PersistenceContext is not NpgsqlTransaction npgsql)
        {
            throw new InvalidOperationException("Assessment activation attempts require the PostgreSQL activation transaction.");
        }

        await npgsql.Connection!.ExecuteAsync(
            new CommandDefinition(
                "SELECT pg_advisory_xact_lock(hashtext(@LockKey));",
                new { LockKey = $"{organizationId:D}:{activityId:D}:{cohortId:D}:{idempotencyKey}" },
                npgsql,
                cancellationToken: cancellationToken));
    }

    public async Task<AssessmentActivationAttempt?> FindAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (transaction.PersistenceContext is not NpgsqlTransaction npgsql)
        {
            throw new InvalidOperationException("Assessment activation attempts require the PostgreSQL activation transaction.");
        }

        var row = await npgsql.Connection!.QuerySingleOrDefaultAsync<AttemptRow>(
            new CommandDefinition(
                """
                SELECT a.organization_id, a.activity_id, a.cohort_id, a.attempt_id, a.expected_revision_id,
                       a.expected_revision_number, a.idempotency_key, a.command_digest, a.outcome_code,
                       a.baseline_id, bl.content_digest, c.state, a.actor_id, a.correlation_id
                FROM assessment_activation_attempts a
                LEFT JOIN assessment_activation_baselines bl
                    ON bl.organization_id = a.organization_id
                   AND bl.activity_id = a.activity_id
                   AND bl.baseline_id = a.baseline_id
                LEFT JOIN assessment_cohorts c
                    ON c.organization_id = a.organization_id
                   AND c.activity_id = a.activity_id
                   AND c.cohort_id = a.cohort_id
                WHERE a.organization_id = @OrganizationId
                  AND a.activity_id = @ActivityId
                  AND a.cohort_id = @CohortId
                  AND a.idempotency_key = @IdempotencyKey
                FOR UPDATE OF a
                """,
                new
                {
                    OrganizationId = organizationId,
                    ActivityId = activityId,
                    CohortId = cohortId,
                    IdempotencyKey = idempotencyKey,
                },
                npgsql,
                cancellationToken: cancellationToken));
        return row is null
            ? null
            : new AssessmentActivationAttempt(
                row.OrganizationId,
                row.ActivityId,
                row.CohortId,
                row.AttemptId,
                row.ExpectedRevisionId,
                row.ExpectedRevisionNumber,
                row.IdempotencyKey,
                row.CommandDigest,
                row.OutcomeCode,
                row.BaselineId,
                row.ContentDigest,
                row.State,
                row.ActorId,
                row.CorrelationId);
    }

    public async Task InsertAsync(
        AssessmentActivationAttempt attempt,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (transaction.PersistenceContext is not NpgsqlTransaction npgsql)
        {
            throw new InvalidOperationException("Assessment activation attempts require the PostgreSQL activation transaction.");
        }

        try
        {
            await npgsql.Connection!.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO assessment_activation_attempts (
                        organization_id, activity_id, cohort_id, attempt_id, expected_revision_id,
                        expected_revision_number, idempotency_key, command_digest, outcome_code,
                        baseline_id, created_at, actor_id, correlation_id)
                    VALUES (
                        @OrganizationId, @ActivityId, @CohortId, @AttemptId, @ExpectedRevisionId,
                        @ExpectedRevisionNumber, @IdempotencyKey, @CommandDigest, @OutcomeCode,
                        @BaselineId, CLOCK_TIMESTAMP(), @ActorId, @CorrelationId)
                    """,
                    new
                    {
                        attempt.OrganizationId,
                        attempt.ActivityId,
                        attempt.CohortId,
                        attempt.AttemptId,
                        attempt.ExpectedRevisionId,
                        attempt.ExpectedRevisionNumber,
                        attempt.IdempotencyKey,
                        attempt.CommandDigest,
                        attempt.OutcomeCode,
                        attempt.BaselineId,
                        attempt.ActorId,
                        attempt.CorrelationId,
                    },
                    npgsql,
                    cancellationToken: cancellationToken));
        }
        catch (PostgresException exception) when (exception.SqlState is "23503")
        {
            throw new InvalidOperationException("Assessment activation attempt parent is missing.", exception);
        }

        var succeeded = string.Equals(attempt.OutcomeCode, "assessment.activated", StringComparison.Ordinal);
        await auditEventWriter.InsertAsync(
            new AuditEventWriteModel(
                Guid.CreateVersion7(),
                attempt.OrganizationId,
                "v1",
                DateTimeOffset.UtcNow,
                attempt.CorrelationId,
                "human.interactive",
                attempt.ActorId,
                AssessmentAuthorizationActions.ActivateCohort,
                AssessmentResourceTypes.Cohort,
                attempt.CohortId,
                succeeded ? "permit" : "deny",
                succeeded ? null : attempt.OutcomeCode,
                1,
                "https",
                attempt.CommandDigest),
            npgsql,
            cancellationToken);
    }

    private sealed record AttemptRow(
        Guid OrganizationId,
        Guid ActivityId,
        Guid CohortId,
        Guid AttemptId,
        Guid ExpectedRevisionId,
        long ExpectedRevisionNumber,
        string IdempotencyKey,
        string CommandDigest,
        string OutcomeCode,
        Guid? BaselineId,
        string? ContentDigest,
        string? State,
        Guid ActorId,
        Guid CorrelationId);
}

public sealed class PostgresAssessmentRelationshipResolver(PostgresConnectionAccessor connections)
    : IAssessmentRelationshipResolver
{
    public async Task<AssessmentActorAuthorization> ResolveAsync(
        Guid actorId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var actions = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT granted_action
                FROM actor_organization_grants
                WHERE organization_id = @OrganizationId
                  AND actor_id = @ActorId
                  AND revoked_at IS NULL
                  AND granted_action LIKE 'assessment.%'
                """,
                new { OrganizationId = organizationId, ActorId = actorId },
                cancellationToken: cancellationToken))).ToArray();

        var relationship = actions.Any(action =>
                action is AssessmentAuthorizationActions.CreateActivity
                    or AssessmentAuthorizationActions.SaveActivity
                    or AssessmentAuthorizationActions.CheckReadiness
                    or AssessmentAuthorizationActions.ActivateCohort
                    or AssessmentAuthorizationActions.SelectSources)
            ? AuthenticationStrengthEvaluator.AdministratorRelationship
            : actions.Length > 0
                ? AuthenticationStrengthEvaluator.ReviewerRelationship
                : string.Empty;
        return new AssessmentActorAuthorization(relationship, actions);
    }
}

