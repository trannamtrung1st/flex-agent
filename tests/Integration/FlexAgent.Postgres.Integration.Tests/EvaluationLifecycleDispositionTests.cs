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

        var holdException = await Assert.ThrowsAsync<PostgresException>(() =>
            EvaluationLifecycleDispositionSupport.RunAsLifecycleExecutorAsync(
                context.Connection,
                connection => EvaluationLifecycleDispositionSupport.ExecuteDisposeAsync(
                    connection,
                    context.OrganizationId,
                    context.EvaluationId,
                    context.ProviderArtifactId,
                    context.AuditEventId,
                    context.DelegationId,
                    "retention_expired",
                    context.LifecycleActorId)));
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

        await EvaluationLifecycleDispositionSupport.RunAsLifecycleExecutorAsync(
            context.Connection,
            connection => EvaluationLifecycleDispositionSupport.ExecuteDisposeAsync(
                connection,
                context.OrganizationId,
                context.EvaluationId,
                context.ProviderArtifactId,
                context.AuditEventId,
                context.DelegationId,
                "authorized_erasure",
                context.LifecycleActorId));

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

        var auditException = await Assert.ThrowsAsync<PostgresException>(() =>
            EvaluationLifecycleDispositionSupport.RunAsLifecycleExecutorAsync(
                context.Connection,
                connection => EvaluationLifecycleDispositionSupport.ExecuteDisposeAsync(
                    connection,
                    context.OrganizationId,
                    context.EvaluationId,
                    context.ProviderArtifactId,
                    Guid.CreateVersion7(),
                    context.DelegationId,
                    "retention_expired",
                    context.LifecycleActorId)));
        Assert.Contains("audit event required", auditException.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Provider_artifact_disposition_requires_lifecycle_executor_role()
    {
        var context = await SeedProviderArtifactAsync();

        var denied = await Assert.ThrowsAsync<PostgresException>(() =>
            EvaluationLifecycleDispositionSupport.RunAsApplicationRoleAsync(
                context.Connection,
                connection => EvaluationLifecycleDispositionSupport.ExecuteDisposeAsync(
                    connection,
                    context.OrganizationId,
                    context.EvaluationId,
                    context.ProviderArtifactId,
                    context.AuditEventId,
                    context.DelegationId,
                    "retention_expired",
                    context.LifecycleActorId)));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
    }

    [Fact]
    public async Task Provider_artifact_disposition_requires_current_lifecycle_delegation()
    {
        var context = await SeedProviderArtifactAsync();
        await context.Connection.ExecuteAsync(
            "UPDATE service_delegations SET revoked_at = clock_timestamp() WHERE delegation_id = @DelegationId;",
            new { context.DelegationId });

        var delegationException = await Assert.ThrowsAsync<PostgresException>(() =>
            EvaluationLifecycleDispositionSupport.RunAsLifecycleExecutorAsync(
                context.Connection,
                connection => EvaluationLifecycleDispositionSupport.ExecuteDisposeAsync(
                    connection,
                    context.OrganizationId,
                    context.EvaluationId,
                    context.ProviderArtifactId,
                    context.AuditEventId,
                    context.DelegationId,
                    "retention_expired",
                    context.LifecycleActorId)));
        Assert.Contains("delegation required", delegationException.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Concurrent_hold_establishment_blocks_provider_artifact_disposition()
    {
        var context = await SeedProviderArtifactAsync();
        var holdMayCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? disposeException = null;

        var holdTask = Task.Run(async () =>
        {
            await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(CancellationToken);
            await connection.ExecuteAsync(
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
                },
                transaction);
            await holdMayCommit.Task;
            await transaction.CommitAsync(CancellationToken);
        },
        CancellationToken);

        var disposeTask = Task.Run(async () =>
        {
            try
            {
                await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
                await EvaluationLifecycleDispositionSupport.RunAsLifecycleExecutorAsync(
                    connection,
                    executorConnection => EvaluationLifecycleDispositionSupport.ExecuteDisposeAsync(
                        executorConnection,
                        context.OrganizationId,
                        context.EvaluationId,
                        context.ProviderArtifactId,
                        context.AuditEventId,
                        context.DelegationId,
                        "retention_expired",
                        context.LifecycleActorId));
            }
            catch (Exception exception)
            {
                disposeException = exception;
            }
        },
        CancellationToken);

        await Task.Delay(TimeSpan.FromMilliseconds(200), CancellationToken);
        holdMayCommit.SetResult();
        await Task.WhenAll(holdTask, disposeTask);

        Assert.NotNull(disposeException);
        Assert.IsType<PostgresException>(disposeException);
        Assert.Contains("legal hold", disposeException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await context.Connection.ExecuteScalarAsync<int>(
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
        var lifecycleActorId = await Fixture.SeedWorkerActorAsync();
        var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var evaluationId = await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
            connection,
            claimed!,
            CancellationToken);
        var providerArtifactId = await EvaluationPersistenceTestSeed.InsertProviderArtifactAsync(
            connection,
            claimed!,
            CancellationToken);
        var delegationId = await EvaluationLifecycleDispositionSupport.InsertLifecycleDisposeDelegationAsync(
            connection,
            claimed!,
            lifecycleActorId,
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
                @CorrelationId, 'service', @LifecycleActorId, 'evaluation.lifecycle.dispose',
                'evaluation', @EvaluationId, 'succeeded', 'integration.test');
            """,
            new
            {
                AuditEventId = auditEventId,
                claimed!.Ownership.OrganizationId,
                CorrelationId = Guid.CreateVersion7(),
                LifecycleActorId = lifecycleActorId,
                EvaluationId = evaluationId,
            });
        return new DispositionContext(
            connection,
            claimed.Ownership.OrganizationId,
            evaluationId,
            providerArtifactId,
            auditEventId,
            delegationId,
            lifecycleActorId);
    }

    private sealed record DispositionContext(
        NpgsqlConnection Connection,
        Guid OrganizationId,
        Guid EvaluationId,
        Guid ProviderArtifactId,
        Guid AuditEventId,
        Guid DelegationId,
        Guid LifecycleActorId);
}
