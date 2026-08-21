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
        if (result is ActivationOutcome { Succeeded: true })
        {
            await scope.CommitAsync(cancellationToken);
        }
        else
        {
            await scope.RollbackAsync(cancellationToken);
        }

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
                    schema_version, canonicalization_version, document, created_at)
                VALUES (
                    @OrganizationId, @ActivityId, @BaselineId, @ContentDigest, @ProcedureId,
                    @SchemaVersion, @CanonicalizationVersion, @Document::jsonb, CLOCK_TIMESTAMP())
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
                },
                npgsql,
                cancellationToken: cancellationToken));

        await npgsql.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO assessment_cohort_baseline_bindings (
                    organization_id, cohort_id, baseline_id, bound_at)
                VALUES (
                    @OrganizationId, @CohortId, @BaselineId, CLOCK_TIMESTAMP())
                """,
                new { OrganizationId = organizationId, CohortId = cohortId, BaselineId = baselineId },
                npgsql,
                cancellationToken: cancellationToken));

        var occurredAt = DateTimeOffset.UtcNow;
        await auditEventWriter.InsertAsync(
            new AuditEventWriteModel(
                Guid.CreateVersion7(),
                organizationId,
                "v1",
                occurredAt,
                Guid.CreateVersion7(),
                "human.interactive",
                Guid.Empty,
                AssessmentAuthorizationActions.ActivateCohort,
                AssessmentResourceTypes.Baseline,
                baselineId,
                "permit",
                null,
                1,
                "https",
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
                Guid.CreateVersion7(),
                contentDigest,
                occurredAt),
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

        return rows.Select(row => new TrustedSourceDescriptor(
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
            row.ProductionEligible)).ToArray();
    }

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
