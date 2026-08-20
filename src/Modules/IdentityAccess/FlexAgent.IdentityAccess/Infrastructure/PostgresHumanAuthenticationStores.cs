using System.Security.Cryptography;
using System.Text;
using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using Npgsql;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class PostgresDatabaseClock(PostgresConnectionAccessor connectionAccessor) : IDatabaseClock
{
    public async Task<DateTimeOffset> GetUtcNowAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<DateTimeOffset>(
            new CommandDefinition(
                "SELECT clock_timestamp();",
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}

public sealed class PostgresHumanIdentityBindingStore(PostgresConnectionAccessor connectionAccessor)
    : IHumanIdentityBindingStore
{
    public async Task<HumanIdentityBinding?> FindByIdentityAsync(
        ExactIssuerSubject identity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<BindingRow>(
            new CommandDefinition(
                """
                SELECT binding_id, issuer, subject, actor_id, created_at, disabled_at
                FROM human_identity_bindings
                WHERE issuer = @Issuer AND subject = @Subject;
                """,
                new { identity.Issuer, identity.Subject },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row is null ? null : row.ToBinding();
    }

    public async Task<IReadOnlyList<Guid>> ListEligibleOrganizationIdsAsync(
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        var ids = await connection.QueryAsync<Guid>(
            new CommandDefinition(
                """
                SELECT DISTINCT organization_id
                FROM actor_organization_grants
                WHERE actor_id = @ActorId
                  AND revoked_at IS NULL;
                """,
                new { ActorId = actorId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return ids.ToArray();
    }

    public async Task<(bool Exists, bool Disabled)> GetActorStateAsync(
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        var disabledAt = await connection.ExecuteScalarAsync<DateTimeOffset?>(
            new CommandDefinition(
                """
                SELECT disabled_at
                FROM actors
                WHERE id = @ActorId;
                """,
                new { ActorId = actorId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT CASE WHEN EXISTS(SELECT 1 FROM actors WHERE id = @ActorId) THEN 1 ELSE 0 END;",
                new { ActorId = actorId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return (exists == 1, disabledAt is not null);
    }

    public async Task<string?> TryProvisionAsync(
        HumanIdentityBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binding);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO human_identity_bindings (
                        binding_id, issuer, subject, actor_id, created_at, disabled_at)
                    VALUES (
                        @BindingId, @Issuer, @Subject, @ActorId, @CreatedAt, @DisabledAt);
                    """,
                    new
                    {
                        binding.BindingId,
                        binding.Identity.Issuer,
                        binding.Identity.Subject,
                        binding.ActorId,
                        binding.CreatedAt,
                        binding.DisabledAt,
                    },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            return null;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var existing = await FindByIdentityAsync(binding.Identity, cancellationToken).ConfigureAwait(false);
            if (existing is not null && existing.ActorId != binding.ActorId)
            {
                return HumanAuthenticationReasonCodes.ReboundIdentity;
            }

            return HumanAuthenticationReasonCodes.UnknownSubject;
        }
    }

    public async Task DisableByIdentityAsync(
        ExactIssuerSubject identity,
        DateTimeOffset disabledAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE human_identity_bindings
                SET disabled_at = COALESCE(disabled_at, @DisabledAt)
                WHERE issuer = @Issuer AND subject = @Subject;
                """,
                new { identity.Issuer, identity.Subject, DisabledAt = disabledAt },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE actors
                SET disabled_at = COALESCE(disabled_at, @DisabledAt)
                WHERE id = (
                    SELECT actor_id
                    FROM human_identity_bindings
                    WHERE issuer = @Issuer AND subject = @Subject);
                """,
                new { identity.Issuer, identity.Subject, DisabledAt = disabledAt },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed record BindingRow(
        Guid BindingId,
        string Issuer,
        string Subject,
        Guid ActorId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? DisabledAt)
    {
        public HumanIdentityBinding ToBinding() =>
            new(BindingId, new ExactIssuerSubject(Issuer, Subject), ActorId, CreatedAt, DisabledAt);
    }
}

public sealed class PostgresApplicationSessionStore(PostgresConnectionAccessor connectionAccessor)
    : IApplicationSessionStore
{
    public async Task InsertAsync(ApplicationSessionRecord session, CancellationToken cancellationToken = default)
    {
        if (!await TryInsertLiveSessionAsync(session, session.Lifetime.CreatedAt, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("Application session could not be inserted.");
        }
    }

    public async Task<bool> TryInsertLiveSessionAsync(
        ApplicationSessionRecord session,
        DateTimeOffset authenticatedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await AcquireIdentityLogoutLockAsync(connection, transaction, session.Identity, cancellationToken)
            .ConfigureAwait(false);
        await AcquireProviderLogoutLockAsync(connection, transaction, session.ProviderSessionDigest, cancellationToken)
            .ConfigureAwait(false);
        if (await ProviderSessionIsRevokedAsync(connection, transaction, session.ProviderSessionDigest, cancellationToken)
                .ConfigureAwait(false)
            || await IdentityLogoutWatermarkBlocksAsync(
                connection,
                transaction,
                session.Identity,
                authenticatedAt,
                cancellationToken).ConfigureAwait(false))
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO application_sessions (
                        application_session_id,
                        actor_id,
                        organization_id,
                        issuer,
                        subject,
                        credential_digest,
                        authentication_strength,
                        provider_session_digest,
                        created_at,
                        last_seen_at,
                        idle_expires_at,
                        absolute_expires_at,
                        revoked_at,
                        rotated_at,
                        predecessor_session_id,
                        terminal_reason)
                    VALUES (
                        @ApplicationSessionId,
                        @ActorId,
                        @OrganizationId,
                        @Issuer,
                        @Subject,
                        @CredentialDigest,
                        @AuthenticationStrength,
                        @ProviderSessionDigest,
                        @CreatedAt,
                        @LastSeenAt,
                        @IdleExpiresAt,
                        @AbsoluteExpiresAt,
                        @RevokedAt,
                        @RotatedAt,
                        @PredecessorSessionId,
                        @TerminalReason);
                    """,
                    ToRow(session),
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        if (await ProviderSessionIsRevokedAsync(connection, transaction, session.ProviderSessionDigest, cancellationToken)
                .ConfigureAwait(false)
            || await IdentityLogoutWatermarkBlocksAsync(
                connection,
                transaction,
                session.Identity,
                authenticatedAt,
                cancellationToken).ConfigureAwait(false))
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<ApplicationSessionRecord?> FindLiveByCredentialDigestAsync(
        string credentialDigest,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<SessionRow>(
            new CommandDefinition(
                """
                SELECT *
                FROM application_sessions
                WHERE credential_digest = @CredentialDigest
                  AND revoked_at IS NULL
                  AND rotated_at IS NULL;
                """,
                new { CredentialDigest = credentialDigest },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row?.ToRecord();
    }

    public async Task<ApplicationSessionRecord?> GetByIdAsync(
        Guid applicationSessionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<SessionRow>(
            new CommandDefinition(
                """
                SELECT *
                FROM application_sessions
                WHERE application_session_id = @ApplicationSessionId;
                """,
                new { ApplicationSessionId = applicationSessionId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row?.ToRecord();
    }

    public async Task TerminateLiveAsync(
        Guid applicationSessionId,
        DateTimeOffset terminatedAt,
        string terminalReason,
        bool rotated,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE application_sessions
                SET credential_digest = NULL,
                    rotated_at = CASE WHEN @Rotated THEN @TerminatedAt ELSE rotated_at END,
                    revoked_at = CASE WHEN @Rotated THEN revoked_at ELSE @TerminatedAt END,
                    terminal_reason = @TerminalReason
                WHERE application_session_id = @ApplicationSessionId
                  AND revoked_at IS NULL
                  AND rotated_at IS NULL;
                """,
                new
                {
                    ApplicationSessionId = applicationSessionId,
                    TerminatedAt = terminatedAt,
                    TerminalReason = terminalReason,
                    Rotated = rotated,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> TryRotateAsync(
        Guid predecessorSessionId,
        DateTimeOffset terminatedAt,
        string terminalReason,
        ApplicationSessionRecord successor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(successor);
        if (successor.PredecessorSessionId != predecessorSessionId)
        {
            return false;
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await AcquireIdentityLogoutLockAsync(connection, transaction, successor.Identity, cancellationToken)
            .ConfigureAwait(false);
        await AcquireProviderLogoutLockAsync(connection, transaction, successor.ProviderSessionDigest, cancellationToken)
            .ConfigureAwait(false);
        if (await ProviderSessionIsRevokedAsync(connection, transaction, successor.ProviderSessionDigest, cancellationToken)
                .ConfigureAwait(false)
            || await IdentityLogoutWatermarkBlocksAsync(
                connection,
                transaction,
                successor.Identity,
                successor.Lifetime.CreatedAt,
                cancellationToken).ConfigureAwait(false))
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE application_sessions
                SET credential_digest = NULL,
                    rotated_at = @TerminatedAt,
                    terminal_reason = @TerminalReason
                WHERE application_session_id = @PredecessorSessionId
                  AND revoked_at IS NULL
                  AND rotated_at IS NULL;
                """,
                new
                {
                    PredecessorSessionId = predecessorSessionId,
                    TerminatedAt = terminatedAt,
                    TerminalReason = terminalReason,
                },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO application_sessions (
                        application_session_id,
                        actor_id,
                        organization_id,
                        issuer,
                        subject,
                        credential_digest,
                        authentication_strength,
                        provider_session_digest,
                        created_at,
                        last_seen_at,
                        idle_expires_at,
                        absolute_expires_at,
                        revoked_at,
                        rotated_at,
                        predecessor_session_id,
                        terminal_reason)
                    VALUES (
                        @ApplicationSessionId,
                        @ActorId,
                        @OrganizationId,
                        @Issuer,
                        @Subject,
                        @CredentialDigest,
                        @AuthenticationStrength,
                        @ProviderSessionDigest,
                        @CreatedAt,
                        @LastSeenAt,
                        @IdleExpiresAt,
                        @AbsoluteExpiresAt,
                        @RevokedAt,
                        @RotatedAt,
                        @PredecessorSessionId,
                        @TerminalReason);
                    """,
                    ToRow(successor),
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (await ProviderSessionIsRevokedAsync(
                    connection,
                    transaction,
                    successor.ProviderSessionDigest,
                    cancellationToken).ConfigureAwait(false)
                || await IdentityLogoutWatermarkBlocksAsync(
                    connection,
                    transaction,
                    successor.Identity,
                    successor.Lifetime.CreatedAt,
                    cancellationToken).ConfigureAwait(false))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return false;
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    public async Task TouchActivityAsync(
        Guid applicationSessionId,
        ApplicationSessionLifetime lifetime,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE application_sessions
                SET last_seen_at = @LastSeenAt,
                    idle_expires_at = @IdleExpiresAt
                WHERE application_session_id = @ApplicationSessionId
                  AND revoked_at IS NULL
                  AND rotated_at IS NULL
                  AND last_seen_at < @LastSeenAt;
                """,
                new
                {
                    ApplicationSessionId = applicationSessionId,
                    lifetime.LastSeenAt,
                    lifetime.IdleExpiresAt,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<int> RevokeLiveByIdentityAsync(
        ExactIssuerSubject identity,
        DateTimeOffset revokedAt,
        string terminalReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        return await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE application_sessions
                SET credential_digest = NULL,
                    revoked_at = @RevokedAt,
                    terminal_reason = @TerminalReason
                WHERE issuer = @Issuer
                  AND subject = @Subject
                  AND revoked_at IS NULL
                  AND rotated_at IS NULL;
                """,
                new
                {
                    identity.Issuer,
                    identity.Subject,
                    RevokedAt = revokedAt,
                    TerminalReason = terminalReason,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<int> RevokeLiveByProviderSessionDigestAsync(
        string providerSessionDigest,
        DateTimeOffset revokedAt,
        string terminalReason,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await AcquireProviderLogoutLockAsync(connection, transaction, providerSessionDigest, cancellationToken)
            .ConfigureAwait(false);
        await TombstoneProviderSessionAsync(connection, transaction, providerSessionDigest, revokedAt, cancellationToken)
            .ConfigureAwait(false);
        var count = await RevokeLiveByProviderSessionDigestCoreAsync(
            connection,
            transaction,
            providerSessionDigest,
            revokedAt,
            terminalReason,
            cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return count;
    }

    public async Task<ForcedLogoutApplyResult> TryApplyForcedLogoutAsync(
        string issuer,
        string jwtId,
        string? providerSessionDigest,
        ExactIssuerSubject? identity,
        DateTimeOffset revokedAt,
        DateTimeOffset logoutIssuedAt,
        string terminalReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(jwtId);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO consumed_logout_tokens (issuer, jti, consumed_at)
                    VALUES (@Issuer, @JwtId, @RevokedAt);
                    """,
                    new { Issuer = issuer, JwtId = jwtId, RevokedAt = revokedAt },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return ForcedLogoutApplyResult.Duplicate();
        }

        var count = 0;
        if (identity is not null)
        {
            await AcquireIdentityLogoutLockAsync(connection, transaction, identity, cancellationToken)
                .ConfigureAwait(false);
            await UpsertIdentityLogoutWatermarkAsync(
                connection,
                transaction,
                identity,
                logoutIssuedAt,
                cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(providerSessionDigest))
        {
            await AcquireProviderLogoutLockAsync(connection, transaction, providerSessionDigest, cancellationToken)
                .ConfigureAwait(false);
            await TombstoneProviderSessionAsync(
                connection,
                transaction,
                providerSessionDigest,
                revokedAt,
                cancellationToken).ConfigureAwait(false);
            count += await RevokeLiveByProviderSessionDigestCoreAsync(
                connection,
                transaction,
                providerSessionDigest,
                revokedAt,
                terminalReason,
                cancellationToken).ConfigureAwait(false);
        }

        if (identity is not null)
        {
            count += await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE application_sessions
                    SET credential_digest = NULL,
                        revoked_at = @RevokedAt,
                        terminal_reason = @TerminalReason
                    WHERE issuer = @Issuer
                      AND subject = @Subject
                      AND revoked_at IS NULL
                      AND rotated_at IS NULL;
                    """,
                    new
                    {
                        identity.Issuer,
                        identity.Subject,
                        RevokedAt = revokedAt,
                        TerminalReason = terminalReason,
                    },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return ForcedLogoutApplyResult.Applied(count);
    }

    private static async Task UpsertIdentityLogoutWatermarkAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ExactIssuerSubject identity,
        DateTimeOffset logoutIssuedAt,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO identity_logout_watermarks (issuer, subject, logged_out_at)
                VALUES (@Issuer, @Subject, @LoggedOutAt)
                ON CONFLICT (issuer, subject)
                DO UPDATE SET logged_out_at = GREATEST(identity_logout_watermarks.logged_out_at, EXCLUDED.logged_out_at);
                """,
                new
                {
                    identity.Issuer,
                    identity.Subject,
                    LoggedOutAt = logoutIssuedAt,
                },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task<bool> IdentityLogoutWatermarkBlocksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ExactIssuerSubject identity,
        DateTimeOffset authenticatedAt,
        CancellationToken cancellationToken)
    {
        var loggedOutAt = await connection.ExecuteScalarAsync<DateTimeOffset?>(
            new CommandDefinition(
                """
                SELECT logged_out_at
                FROM identity_logout_watermarks
                WHERE issuer = @Issuer
                  AND subject = @Subject;
                """,
                new { identity.Issuer, identity.Subject },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return loggedOutAt is DateTimeOffset watermark && authenticatedAt <= watermark;
    }

    private static async Task AcquireProviderLogoutLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string? providerSessionDigest,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerSessionDigest))
        {
            return;
        }

        var (k1, k2) = PostgresAdvisoryKeys.Create("provider", providerSessionDigest);
        await connection.ExecuteAsync(
            new CommandDefinition(
                "SELECT pg_advisory_xact_lock(@K1, @K2);",
                new { K1 = k1, K2 = k2 },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task AcquireIdentityLogoutLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ExactIssuerSubject identity,
        CancellationToken cancellationToken)
    {
        var (k1, k2) = PostgresAdvisoryKeys.Create("identity", $"{identity.Issuer}\n{identity.Subject}");
        await connection.ExecuteAsync(
            new CommandDefinition(
                "SELECT pg_advisory_xact_lock(@K1, @K2);",
                new { K1 = k1, K2 = k2 },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task TombstoneProviderSessionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string providerSessionDigest,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO revoked_provider_sessions (provider_session_digest, revoked_at)
                VALUES (@ProviderSessionDigest, @RevokedAt)
                ON CONFLICT (provider_session_digest) DO NOTHING;
                """,
                new { ProviderSessionDigest = providerSessionDigest, RevokedAt = revokedAt },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

    private static async Task<bool> ProviderSessionIsRevokedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string? providerSessionDigest,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerSessionDigest))
        {
            return false;
        }

        var found = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS(
                    SELECT 1
                    FROM revoked_provider_sessions
                    WHERE provider_session_digest = @ProviderSessionDigest)
                THEN 1 ELSE 0 END;
                """,
                new { ProviderSessionDigest = providerSessionDigest },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return found == 1;
    }

    private static Task<int> RevokeLiveByProviderSessionDigestCoreAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string providerSessionDigest,
        DateTimeOffset revokedAt,
        string terminalReason,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE application_sessions
                SET credential_digest = NULL,
                    revoked_at = @RevokedAt,
                    terminal_reason = @TerminalReason
                WHERE provider_session_digest = @ProviderSessionDigest
                  AND revoked_at IS NULL
                  AND rotated_at IS NULL;
                """,
                new
                {
                    ProviderSessionDigest = providerSessionDigest,
                    RevokedAt = revokedAt,
                    TerminalReason = terminalReason,
                },
                transaction,
                cancellationToken: cancellationToken));

    private static object ToRow(ApplicationSessionRecord session) =>
        new
        {
            session.ApplicationSessionId,
            session.ActorId,
            session.OrganizationId,
            session.Identity.Issuer,
            session.Identity.Subject,
            session.CredentialDigest,
            AuthenticationStrength = AuthenticationStrengthCodec.Encode(session.Strength),
            session.ProviderSessionDigest,
            session.Lifetime.CreatedAt,
            session.Lifetime.LastSeenAt,
            session.Lifetime.IdleExpiresAt,
            session.Lifetime.AbsoluteExpiresAt,
            session.RevokedAt,
            session.RotatedAt,
            session.PredecessorSessionId,
            session.TerminalReason,
        };

    private sealed class SessionRow
    {
        public Guid ApplicationSessionId { get; init; }
        public Guid ActorId { get; init; }
        public Guid OrganizationId { get; init; }
        public string Issuer { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string? CredentialDigest { get; init; }
        public string AuthenticationStrength { get; init; } = string.Empty;
        public string? ProviderSessionDigest { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset LastSeenAt { get; init; }
        public DateTimeOffset IdleExpiresAt { get; init; }
        public DateTimeOffset AbsoluteExpiresAt { get; init; }
        public DateTimeOffset? RevokedAt { get; init; }
        public DateTimeOffset? RotatedAt { get; init; }
        public Guid? PredecessorSessionId { get; init; }
        public string? TerminalReason { get; init; }

        public ApplicationSessionRecord ToRecord() =>
            new(
                ApplicationSessionId,
                ActorId,
                OrganizationId,
                new ExactIssuerSubject(Issuer, Subject),
                CredentialDigest,
                AuthenticationStrengthCodec.Decode(AuthenticationStrength),
                ProviderSessionDigest,
                new ApplicationSessionLifetime(CreatedAt, LastSeenAt, IdleExpiresAt, AbsoluteExpiresAt),
                RevokedAt,
                RotatedAt,
                PredecessorSessionId,
                TerminalReason);
    }
}

public sealed class PostgresOidcLoginTransactionStore(
    PostgresConnectionAccessor connectionAccessor,
    ISymmetricPayloadProtector protector) : IOidcLoginTransactionStore
{
    public async Task CreateAsync(OidcLoginTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO oidc_login_transactions (
                    transaction_id,
                    state_digest,
                    nonce_ciphertext,
                    code_verifier_ciphertext,
                    return_path,
                    created_at,
                    expires_at,
                    consumed_at,
                    correlation_id,
                    correlation_digest)
                VALUES (
                    @TransactionId,
                    @StateDigest,
                    @NonceCiphertext,
                    @CodeVerifierCiphertext,
                    @ReturnPath,
                    clock_timestamp(),
                    @ExpiresAt,
                    NULL,
                    @CorrelationId,
                    @CorrelationDigest);
                """,
                new
                {
                    transaction.TransactionId,
                    transaction.StateDigest,
                    NonceCiphertext = protector.Protect(transaction.Nonce),
                    CodeVerifierCiphertext = protector.Protect(transaction.CodeVerifier),
                    transaction.ReturnPath,
                    transaction.ExpiresAt,
                    transaction.CorrelationId,
                    transaction.CorrelationDigest,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<OidcLoginTransaction?> ConsumeAsync(
        string stateDigest,
        string correlationDigest,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<TransactionRow>(
            new CommandDefinition(
                """
                UPDATE oidc_login_transactions
                SET consumed_at = @Now
                WHERE state_digest = @StateDigest
                  AND correlation_digest = @CorrelationDigest
                  AND consumed_at IS NULL
                  AND expires_at > @Now
                RETURNING transaction_id, state_digest, correlation_digest, nonce_ciphertext,
                          code_verifier_ciphertext, return_path, expires_at, correlation_id;
                """,
                new { StateDigest = stateDigest, CorrelationDigest = correlationDigest, Now = now },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row is null)
        {
            return null;
        }

        return new OidcLoginTransaction(
            row.TransactionId,
            row.StateDigest,
            row.CorrelationDigest,
            protector.Unprotect(row.NonceCiphertext),
            protector.Unprotect(row.CodeVerifierCiphertext),
            row.ReturnPath,
            row.ExpiresAt,
            row.CorrelationId);
    }

    private sealed record TransactionRow(
        Guid TransactionId,
        string StateDigest,
        string CorrelationDigest,
        byte[] NonceCiphertext,
        byte[] CodeVerifierCiphertext,
        string ReturnPath,
        DateTimeOffset ExpiresAt,
        Guid CorrelationId);
}

public sealed class PostgresAuthenticationSecurityEventWriter(PostgresConnectionAccessor connectionAccessor)
    : IAuthenticationSecurityEventWriter
{
    public async Task WriteAsync(
        AuthenticationSecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO authentication_security_events (
                    event_id,
                    occurred_at,
                    event_type,
                    outcome,
                    reason_code,
                    correlation_id,
                    actor_id,
                    organization_id,
                    application_session_id)
                VALUES (
                    @EventId,
                    @OccurredAt,
                    @EventType,
                    @Outcome,
                    @ReasonCode,
                    @CorrelationId,
                    @ActorId,
                    @OrganizationId,
                    @ApplicationSessionId);
                """,
                securityEvent,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}

public sealed class PostgresLogoutTokenReplayStore(PostgresConnectionAccessor connectionAccessor)
    : ILogoutTokenReplayStore
{
    public async Task<bool> TryConsumeAsync(
        string issuer,
        string jwtId,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(jwtId);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO consumed_logout_tokens (issuer, jti, consumed_at)
                    VALUES (@Issuer, @JwtId, @ConsumedAt);
                    """,
                    new { Issuer = issuer, JwtId = jwtId, ConsumedAt = consumedAt },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            return true;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }
    }
}

internal static class AuthenticationStrengthCodec
{
    public static string Encode(AuthenticationStrength strength)
    {
        ArgumentNullException.ThrowIfNull(strength);
        var acr = strength.Acr ?? string.Empty;
        var amr = string.Join(',', strength.Amr.Where(static value => !string.IsNullOrWhiteSpace(value)));
        if (acr.Length + amr.Length > 256)
        {
            throw new InvalidOperationException("Authentication strength exceeded the bounded store size.");
        }

        return $"acr={acr};amr={amr}";
    }

    public static AuthenticationStrength Decode(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return AuthenticationStrength.Empty;
        }

        var acr = string.Empty;
        var amr = Array.Empty<string>();
        foreach (var part in encoded.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2)
            {
                continue;
            }

            if (pair[0] == "acr")
            {
                acr = pair[1];
            }
            else if (pair[0] == "amr")
            {
                amr = string.IsNullOrEmpty(pair[1])
                    ? []
                    : pair[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
        }

        return new AuthenticationStrength(string.IsNullOrEmpty(acr) ? null : acr, amr);
    }
}

internal static class PostgresAdvisoryKeys
{
    public static (int K1, int K2) Create(string kind, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(kind + "\n" + value));
        return (BitConverter.ToInt32(hash, 0), BitConverter.ToInt32(hash, 4));
    }
}
