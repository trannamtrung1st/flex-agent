using Dapper;
using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres.Integration.Tests.Support;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class CommitTimeAuthorizationRaceTests(PostgresIntegrationFixture fixture) : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Grant_row_lock_blocks_concurrent_revocation_until_transaction_completes()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        await using var holdingConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await using var revokingConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await holdingConnection.OpenAsync(CancellationToken);
        await revokingConnection.OpenAsync(CancellationToken);

        await using var holdingTransaction = await holdingConnection.BeginTransactionAsync(CancellationToken);
        var lockedVersion = await holdingConnection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                """
                SELECT relationship_version
                FROM actor_organization_grants
                WHERE organization_id = @OrganizationId
                  AND actor_id = @ActorId
                  AND granted_action = @GrantedAction
                  AND revoked_at IS NULL
                FOR SHARE;
                """,
                new
                {
                    OrganizationId = seeded.OrganizationId,
                    ActorId = seeded.ActorId,
                    GrantedAction = AuthorizationActions.RegisterConfigurationSourceVersion,
                },
                holdingTransaction,
                cancellationToken: CancellationToken));

        Assert.Equal(1, lockedVersion);

        var revokeTask = Task.Run(async () =>
        {
            await revokingConnection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE actor_organization_grants
                    SET revoked_at = NOW() AT TIME ZONE 'UTC'
                    WHERE organization_id = @OrganizationId
                      AND actor_id = @ActorId
                      AND granted_action = @GrantedAction
                      AND revoked_at IS NULL;
                    """,
                    new
                    {
                        OrganizationId = seeded.OrganizationId,
                        ActorId = seeded.ActorId,
                        GrantedAction = AuthorizationActions.RegisterConfigurationSourceVersion,
                    },
                    cancellationToken: CancellationToken));
        }, CancellationToken);

        await Task.Delay(TimeSpan.FromMilliseconds(200), CancellationToken);
        Assert.False(revokeTask.IsCompleted);

        await holdingTransaction.CommitAsync(CancellationToken);
        await revokeTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded),
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.Denied, result.OutcomeCode);
    }

    [Fact]
    public async Task Concurrent_registration_requests_return_one_authoritative_version()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        var command = CreateCommand(
            seeded,
            digest,
            content,
            idempotencyKey: "concurrent-same-key");

        var results = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => Fixture.Services.RegisterHandler.HandleAsync(
                command with { CorrelationId = Guid.NewGuid() },
                CancellationToken)));

        Assert.All(results, result => Assert.True(result.Succeeded));
        Assert.Single(results.Select(result => result.Identity!.VersionId).Distinct());

        var count = await Fixture.Services.VersionRepository.CountForSourceAsync(
            seeded.OrganizationId,
            seeded.ConfigurationSourceId,
            CancellationToken);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Digest_deduplication_reserves_idempotency_key_for_later_conflicts()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        var first = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, digest, content, idempotencyKey: "digest-key-1"),
            CancellationToken);

        var second = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, digest, content, idempotencyKey: "digest-key-2"),
            CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.Identity!.VersionId, second.Identity!.VersionId);

        var alternateContent = System.Text.Encoding.UTF8.GetBytes(
            """
            {"canonicalization_version":"rfc8785","effective_configuration":{"domains":[{"domain_key":"memory_mode","effective_value":{"mode":"strict"},"provenance_classification":"inherited"}]},"procedure_id":"rsc-jcs-sha256-v1","resolution_decisions":[{"decision_key":"memory_mode","outcome":"stable_required"}],"schema_version":"v1","source_references":[{"content_digest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","source_id":"agent.synth.02","source_key":"agent","source_version":"rev.0002"}]}
            """);
        var alternateDigest = FlexAgent.CanonicalJson.CanonicalJsonProcessor.CanonicalizeSha256Hex(
            alternateContent,
            new FlexAgent.CanonicalJson.CanonicalJsonLimits(65_536, 64, 4_096, 4_096));

        var conflict = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, alternateDigest, alternateContent, idempotencyKey: "digest-key-2"),
            CancellationToken);

        Assert.False(conflict.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.IdempotencyConflict, conflict.OutcomeCode);
        Assert.Equal(1, await Fixture.Services.VersionRepository.CountForSourceAsync(
            seeded.OrganizationId,
            seeded.ConfigurationSourceId,
            CancellationToken));
    }

    private static RegisterConfigurationSourceVersionCommand CreateCommand(
        SeededOrganization seeded,
        string? digest = null,
        byte[]? content = null,
        string idempotencyKey = "race-default",
        Guid? correlationId = null) =>
        new(
            seeded.Actor,
            seeded.Scope,
            seeded.ConfigurationSourceId,
            ConfigurationProcedureIds.RscJcsSha256V1,
            ConfigurationSchemaVersions.V1,
            content ?? PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8(),
            digest ?? PostgresIntegrationFixture.MinimalStableDomainDigest,
            idempotencyKey,
            correlationId ?? Guid.NewGuid(),
            "integration.test");
}
