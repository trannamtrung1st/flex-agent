using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class HumanAuthenticationPersistenceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Migration_creates_human_authentication_tables_and_keeps_audit_append_only()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var tables = (await connection.QueryAsync<string>(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name IN (
                'human_identity_bindings',
                'application_sessions',
                'oidc_login_transactions',
                'data_protection_keys',
                'authentication_security_events',
                'consumed_logout_tokens',
                'revoked_provider_sessions',
                'identity_logout_watermarks');
            """)).ToArray();
        Assert.Equal(8, tables.Length);

        var writer = new PostgresAuthenticationSecurityEventWriter(Fixture.Services.ConnectionAccessor);
        var eventId = Guid.NewGuid();
        await writer.WriteAsync(
            new AuthenticationSecurityEvent(
                eventId,
                DateTimeOffset.UtcNow,
                AuthenticationSecurityEventTypes.LoginDenied,
                "deny",
                HumanAuthenticationReasonCodes.UnknownSubject,
                Guid.NewGuid(),
                null,
                null,
                null),
            CancellationToken);

        var update = async () => await connection.ExecuteAsync(
            "UPDATE authentication_security_events SET outcome = 'permit' WHERE event_id = @EventId;",
            new { EventId = eventId });
        await Assert.ThrowsAsync<PostgresException>(update);
    }

    [Fact]
    public async Task Coordinator_persists_digest_only_sessions_and_clears_them_on_rotation()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var issuer = "https://issuer.example/realms/flex";
        var subject = "subject-" + seeded.ActorId.ToString("N")[..8];
        var bindings = new PostgresHumanIdentityBindingStore(Fixture.Services.ConnectionAccessor);
        var sessions = new PostgresApplicationSessionStore(Fixture.Services.ConnectionAccessor);
        var audit = new PostgresAuthenticationSecurityEventWriter(Fixture.Services.ConnectionAccessor);
        var digests = new HmacLookupDigestCalculator("integration-lookup-key-32-bytes!!"u8.ToArray());
        var coordinator = new HumanAuthenticationCoordinator(
            bindings,
            sessions,
            audit,
            digests,
            new PostgresDatabaseClock(Fixture.Services.ConnectionAccessor),
            new HumanAuthenticationOptions { Issuer = issuer });

        Assert.Null(await bindings.TryProvisionAsync(
            new HumanIdentityBinding(
                Guid.NewGuid(),
                new ExactIssuerSubject(issuer, subject),
                seeded.ActorId,
                DateTimeOffset.UtcNow,
                null),
            CancellationToken));

        var login = await coordinator.CompleteLoginAsync(
            new ValidatedHumanLogin(
                new ExactIssuerSubject(issuer, subject),
                new AuthenticationStrength("acr:mfa", ["mfa"]),
                "sid-1",
                DateTimeOffset.UtcNow),
            null,
            Guid.NewGuid(),
            CancellationToken);
        Assert.True(login.Succeeded);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var storedSecret = await connection.ExecuteScalarAsync<string?>(
            "SELECT credential_digest FROM application_sessions WHERE application_session_id = @Id;",
            new { Id = login.ApplicationSessionId });
        Assert.NotEqual(login.RawCredential, storedSecret);
        Assert.DoesNotContain("sid-1", storedSecret ?? string.Empty, StringComparison.Ordinal);

        var rotated = await coordinator.RotateAsync(
            login.ApplicationSessionId!.Value,
            ApplicationSessionTerminalReasons.PrivilegeChange,
            Guid.NewGuid(),
            CancellationToken);
        Assert.True(rotated.Succeeded);
        Assert.Null(await coordinator.AuthenticateAsync(login.RawCredential!, false, CancellationToken));
        Assert.NotNull(await coordinator.AuthenticateAsync(rotated.RawCredential!, false, CancellationToken));

        var terminalDigest = await connection.ExecuteScalarAsync<string?>(
            "SELECT credential_digest FROM application_sessions WHERE application_session_id = @Id;",
            new { Id = login.ApplicationSessionId });
        Assert.Null(terminalDigest);
    }

    [Fact]
    public async Task Concurrent_rotations_create_exactly_one_successor()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var issuer = "https://issuer.example/realms/flex";
        var subject = "subject-" + seeded.ActorId.ToString("N")[..8];
        var bindings = new PostgresHumanIdentityBindingStore(Fixture.Services.ConnectionAccessor);
        var sessions = new PostgresApplicationSessionStore(Fixture.Services.ConnectionAccessor);
        var audit = new PostgresAuthenticationSecurityEventWriter(Fixture.Services.ConnectionAccessor);
        var coordinator = new HumanAuthenticationCoordinator(
            bindings,
            sessions,
            audit,
            new HmacLookupDigestCalculator("integration-lookup-key-32-bytes!!"u8.ToArray()),
            new PostgresDatabaseClock(Fixture.Services.ConnectionAccessor),
            new HumanAuthenticationOptions { Issuer = issuer });
        Assert.Null(await bindings.TryProvisionAsync(
            new HumanIdentityBinding(
                Guid.NewGuid(),
                new ExactIssuerSubject(issuer, subject),
                seeded.ActorId,
                DateTimeOffset.UtcNow,
                null),
            CancellationToken));
        var login = await coordinator.CompleteLoginAsync(
            new ValidatedHumanLogin(
                new ExactIssuerSubject(issuer, subject),
                new AuthenticationStrength("acr:mfa", ["mfa"]),
                "sid-1",
                DateTimeOffset.UtcNow),
            null,
            Guid.NewGuid(),
            CancellationToken);

        var results = await Task.WhenAll(
            coordinator.RotateAsync(
                login.ApplicationSessionId!.Value,
                ApplicationSessionTerminalReasons.PrivilegeChange,
                Guid.NewGuid(),
                CancellationToken),
            coordinator.RotateAsync(
                login.ApplicationSessionId.Value,
                ApplicationSessionTerminalReasons.SensitiveReauthentication,
                Guid.NewGuid(),
                CancellationToken));

        Assert.Equal(1, results.Count(result => result.Succeeded));
        Assert.Equal(1, results.Count(result => result.ReasonCode == HumanAuthenticationReasonCodes.MissingSession));
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var successors = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM application_sessions
            WHERE predecessor_session_id = @Id;
            """,
            new { Id = login.ApplicationSessionId });
        Assert.Equal(1, successors);
    }

    [Fact]
    public async Task Sid_and_sub_logout_revokes_only_the_identified_provider_session()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var issuer = "https://issuer.example/realms/flex";
        var subject = "subject-" + seeded.ActorId.ToString("N")[..8];
        var identity = new ExactIssuerSubject(issuer, subject);
        var bindings = new PostgresHumanIdentityBindingStore(Fixture.Services.ConnectionAccessor);
        var sessions = new PostgresApplicationSessionStore(Fixture.Services.ConnectionAccessor);
        var audit = new PostgresAuthenticationSecurityEventWriter(Fixture.Services.ConnectionAccessor);
        var coordinator = new HumanAuthenticationCoordinator(
            bindings,
            sessions,
            audit,
            new HmacLookupDigestCalculator("integration-lookup-key-32-bytes!!"u8.ToArray()),
            new PostgresDatabaseClock(Fixture.Services.ConnectionAccessor),
            new HumanAuthenticationOptions { Issuer = issuer });
        Assert.Null(await bindings.TryProvisionAsync(
            new HumanIdentityBinding(Guid.NewGuid(), identity, seeded.ActorId, DateTimeOffset.UtcNow, null),
            CancellationToken));
        var authenticatedAt = DateTimeOffset.UnixEpoch.AddHours(2);
        var first = await coordinator.CompleteLoginAsync(
            new ValidatedHumanLogin(identity, new AuthenticationStrength("acr:mfa", ["mfa"]), "sid-a", authenticatedAt),
            null,
            Guid.NewGuid(),
            CancellationToken);
        var second = await coordinator.CompleteLoginAsync(
            new ValidatedHumanLogin(identity, new AuthenticationStrength("acr:mfa", ["mfa"]), "sid-b", authenticatedAt),
            null,
            Guid.NewGuid(),
            CancellationToken);

        var applied = await coordinator.ApplyBackChannelLogoutAsync(
            new ValidatedLogoutToken(issuer, subject, "sid-a", "jti-sid-and-sub", authenticatedAt.AddMinutes(1)),
            Guid.NewGuid(),
            CancellationToken);
        var remintedTarget = await coordinator.CompleteLoginAsync(
            new ValidatedHumanLogin(identity, new AuthenticationStrength("acr:mfa", ["mfa"]), "sid-a", authenticatedAt),
            null,
            Guid.NewGuid(),
            CancellationToken);
        var remintedSibling = await coordinator.CompleteLoginAsync(
            new ValidatedHumanLogin(identity, new AuthenticationStrength("acr:mfa", ["mfa"]), "sid-c", authenticatedAt),
            null,
            Guid.NewGuid(),
            CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.True(applied.Accepted);
        Assert.Equal(1, applied.RevokedCount);
        Assert.Null(await coordinator.AuthenticateAsync(first.RawCredential!, false, CancellationToken));
        Assert.NotNull(await coordinator.AuthenticateAsync(second.RawCredential!, false, CancellationToken));
        Assert.False(remintedTarget.Succeeded);
        Assert.True(remintedSibling.Succeeded);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var watermarks = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM identity_logout_watermarks
            WHERE issuer = @Issuer
              AND subject = @Subject;
            """,
            new { Issuer = issuer, Subject = subject });
        Assert.Equal(0, watermarks);
    }
}
