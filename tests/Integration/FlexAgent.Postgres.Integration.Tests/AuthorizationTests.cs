using System.Text;
using Dapper;
using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class AuthorizationDomainTests
{
    [Fact]
    public void AuthorizationDecision_deny_has_stable_reason()
    {
        var decision = AuthorizationDecision.Deny(AuthorizationReasonCodes.DeniedNoGrant);

        Assert.False(decision.IsPermitted);
        Assert.Equal(AuthorizationOutcomes.Deny, decision.Outcome);
        Assert.Equal(AuthorizationReasonCodes.DeniedNoGrant, decision.ReasonCode);
    }
}

public sealed class RegisterConfigurationSourceVersionAuthorizationTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Deny_by_default_for_unknown_actor()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var unknownActor = new TrustedActor(Guid.NewGuid(), "synthetic.test_actor");

        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, unknownActor, PostgresIntegrationFixture.MinimalStableDomainDigest),
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.Denied, result.OutcomeCode);
        Assert.Null(result.Identity);
    }

    [Fact]
    public async Task Deny_when_grant_revoked_before_commit()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        await Fixture.Services.GrantRepository.RevokeAsync(
            seeded.OrganizationId,
            seeded.ActorId,
            AuthorizationActions.RegisterConfigurationSourceVersion,
            CancellationToken);

        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, seeded.Actor, digest, content),
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.Denied, result.OutcomeCode);
    }

    [Fact]
    public async Task Successful_registration_persists_version_audit_and_outbox()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        var correlationId = Guid.NewGuid();

        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, seeded.Actor, digest, content, correlationId: correlationId),
            CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Identity);
        Assert.Equal(digest, result.Identity!.ContentDigest);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var auditCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM audit_events WHERE correlation_id = @CorrelationId;",
                new { CorrelationId = correlationId },
                cancellationToken: CancellationToken));

        var outboxCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM outbox_items WHERE correlation_id = @CorrelationId;",
                new { CorrelationId = correlationId },
                cancellationToken: CancellationToken));

        Assert.Equal(1, auditCount);
        Assert.Equal(1, outboxCount);

        await using var auditConnection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var relationshipVersion = await auditConnection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                "SELECT relationship_version FROM audit_events WHERE correlation_id = @CorrelationId;",
                new { CorrelationId = correlationId },
                cancellationToken: CancellationToken));

        Assert.NotNull(relationshipVersion);
        Assert.Equal(1, relationshipVersion);
    }

    [Fact]
    public async Task Idempotent_registration_returns_same_identity()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        const string idempotencyKey = "idem-001";

        var first = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, seeded.Actor, digest, content, idempotencyKey: idempotencyKey),
            CancellationToken);

        var second = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, seeded.Actor, digest, content, idempotencyKey: idempotencyKey),
            CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.Identity!.VersionId, second.Identity!.VersionId);
    }

    [Fact]
    public async Task Conflicting_idempotency_key_fails_without_second_version()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        const string idempotencyKey = "idem-conflict";

        Assert.True((await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, seeded.Actor, digest, content, idempotencyKey: idempotencyKey),
            CancellationToken)).Succeeded);

        var alternateContent = Encoding.UTF8.GetBytes(
            """
            {"canonicalization_version":"rfc8785","effective_configuration":{"domains":[{"domain_key":"memory_mode","effective_value":{"mode":"strict"},"provenance_classification":"inherited"}]},"procedure_id":"rsc-jcs-sha256-v1","resolution_decisions":[{"decision_key":"memory_mode","outcome":"stable_required"}],"schema_version":"v1","source_references":[{"content_digest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","source_id":"agent.synth.02","source_key":"agent","source_version":"rev.0002"}]}
            """);
        var alternateDigest = FlexAgent.CanonicalJson.CanonicalJsonProcessor.CanonicalizeSha256Hex(
            alternateContent,
            new FlexAgent.CanonicalJson.CanonicalJsonLimits(65_536, 64, 4_096, 4_096));

        var conflict = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, seeded.Actor, alternateDigest, alternateContent, idempotencyKey: idempotencyKey),
            CancellationToken);

        Assert.False(conflict.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.IdempotencyConflict, conflict.OutcomeCode);

        var count = await Fixture.Services.VersionRepository.CountForSourceAsync(
            seeded.OrganizationId,
            seeded.ConfigurationSourceId,
            CancellationToken);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Invalid_digest_is_rejected_before_persistence()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();

        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, seeded.Actor, "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff", content),
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.InvalidDigest, result.OutcomeCode);

        var count = await Fixture.Services.VersionRepository.CountForSourceAsync(
            seeded.OrganizationId,
            seeded.ConfigurationSourceId,
            CancellationToken);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Invalid_procedure_is_rejected_before_persistence()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;

        var command = new RegisterConfigurationSourceVersionCommand(
            seeded.Actor,
            seeded.Scope,
            seeded.ConfigurationSourceId,
            "unsupported-procedure",
            ConfigurationSchemaVersions.V1,
            content,
            digest,
            "invalid-procedure",
            Guid.NewGuid(),
            "integration.test");

        var result = await Fixture.Services.RegisterHandler.HandleAsync(command, CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.InvalidProcedure, result.OutcomeCode);
    }

    private static RegisterConfigurationSourceVersionCommand CreateCommand(
        SeededOrganization seeded,
        TrustedActor actor,
        string digest,
        byte[]? content = null,
        string idempotencyKey = "idem-default",
        Guid? correlationId = null) =>
        new(
            actor,
            seeded.Scope,
            seeded.ConfigurationSourceId,
            ConfigurationProcedureIds.RscJcsSha256V1,
            ConfigurationSchemaVersions.V1,
            content ?? PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8(),
            digest,
            idempotencyKey,
            correlationId ?? Guid.NewGuid(),
            "integration.test");
}
