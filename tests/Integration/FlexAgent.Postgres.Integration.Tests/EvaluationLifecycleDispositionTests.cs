using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Postgres.Integration.Tests.Support;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationLifecycleDispositionTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Active_legal_hold_blocks_provider_artifact_disposition()
    {
        var context = await SeedProviderArtifactAsync();
        await context.Connection.ExecuteAsync(
            """
            INSERT INTO evaluation_lifecycle_holds (
                organization_id, hold_id, evaluation_id, reason_code, active, created_at)
            VALUES (@OrganizationId, @HoldId, @EvaluationId, 'legal_hold', TRUE, clock_timestamp());
            """,
            new
            {
                context.OrganizationId,
                HoldId = Guid.CreateVersion7(),
                context.EvaluationId,
            });

        var holdException = await Assert.ThrowsAsync<PostgresException>(() => context.Connection.ExecuteAsync(
            """
            SELECT dispose_evaluation_provider_artifact(
                @OrganizationId,
                @EvaluationId,
                @ProviderArtifactId,
                @AuditEventId,
                'retention_expired',
                @ActorId);
            """,
            new
            {
                context.OrganizationId,
                context.EvaluationId,
                context.ProviderArtifactId,
                context.AuditEventId,
                context.ActorId,
            }));
        Assert.Contains("legal hold", holdException.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inactive_hold_does_not_block_provider_artifact_disposition()
    {
        var context = await SeedProviderArtifactAsync();
        await context.Connection.ExecuteAsync(
            """
            INSERT INTO evaluation_lifecycle_holds (
                organization_id, hold_id, evaluation_id, reason_code, active, created_at, released_at)
            VALUES (
                @OrganizationId, @HoldId, @EvaluationId, 'legal_hold', FALSE,
                clock_timestamp(), clock_timestamp());
            """,
            new
            {
                context.OrganizationId,
                HoldId = Guid.CreateVersion7(),
                context.EvaluationId,
            });

        await context.Connection.ExecuteAsync(
            """
            SELECT dispose_evaluation_provider_artifact(
                @OrganizationId,
                @EvaluationId,
                @ProviderArtifactId,
                @AuditEventId,
                'authorized_erasure',
                @ActorId);
            """,
            new
            {
                context.OrganizationId,
                context.EvaluationId,
                context.ProviderArtifactId,
                context.AuditEventId,
                context.ActorId,
            });

        Assert.Equal(0, await context.Connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_provider_artifacts
            WHERE organization_id = @OrganizationId
              AND provider_artifact_id = @ProviderArtifactId;
            """,
            new
            {
                context.OrganizationId,
                context.ProviderArtifactId,
            }));
        Assert.Equal(1, await context.Connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_lifecycle_disposition_events
            WHERE organization_id = @OrganizationId
              AND evaluation_id = @EvaluationId
              AND object_id = @ProviderArtifactId;
            """,
            new
            {
                context.OrganizationId,
                context.EvaluationId,
                context.ProviderArtifactId,
            }));
    }

    [Fact]
    public async Task Direct_provider_artifact_delete_without_disposition_event_is_denied()
    {
        var context = await SeedProviderArtifactAsync();

        var delete = await Assert.ThrowsAsync<PostgresException>(() => context.Connection.ExecuteAsync(
            """
            DELETE FROM evaluation_provider_artifacts
            WHERE organization_id = @OrganizationId
              AND provider_artifact_id = @ProviderArtifactId;
            """,
            new
            {
                context.OrganizationId,
                context.ProviderArtifactId,
            }));
        Assert.Contains("append-only", delete.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Provider_artifact_disposition_requires_matching_audit_event()
    {
        var context = await SeedProviderArtifactAsync();

        var auditException = await Assert.ThrowsAsync<PostgresException>(() => context.Connection.ExecuteAsync(
            """
            SELECT dispose_evaluation_provider_artifact(
                @OrganizationId,
                @EvaluationId,
                @ProviderArtifactId,
                @MissingAuditEventId,
                'retention_expired',
                @ActorId);
            """,
            new
            {
                context.OrganizationId,
                context.EvaluationId,
                context.ProviderArtifactId,
                MissingAuditEventId = Guid.CreateVersion7(),
                context.ActorId,
            }));
        Assert.Contains("audit event required", auditException.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<DispositionContext> SeedProviderArtifactAsync()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var evaluationId = await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
            connection,
            claimed!,
            CancellationToken);
        var providerArtifactId = await EvaluationPersistenceTestSeed.InsertProviderArtifactAsync(
            connection,
            claimed!,
            CancellationToken);
        var auditEventId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            """
            INSERT INTO audit_events (
                event_id, organization_id, event_schema_version, occurred_at,
                correlation_id, actor_type, actor_id, action, resource_type, resource_id,
                outcome, source_channel)
            VALUES (
                @AuditEventId, @OrganizationId, 'audit-event.v1', clock_timestamp(),
                @CorrelationId, 'service', @ActorId, 'evaluation.lifecycle.dispose',
                'evaluation', @EvaluationId, 'succeeded', 'integration.test');
            """,
            new
            {
                AuditEventId = auditEventId,
                claimed!.Ownership.OrganizationId,
                CorrelationId = Guid.CreateVersion7(),
                ActorId = prepared.WorkerActorId,
                EvaluationId = evaluationId,
            });
        return new DispositionContext(
            connection,
            claimed.Ownership.OrganizationId,
            evaluationId,
            providerArtifactId,
            auditEventId,
            prepared.WorkerActorId);
    }

    private sealed record DispositionContext(
        NpgsqlConnection Connection,
        Guid OrganizationId,
        Guid EvaluationId,
        Guid ProviderArtifactId,
        Guid AuditEventId,
        Guid ActorId);
}
