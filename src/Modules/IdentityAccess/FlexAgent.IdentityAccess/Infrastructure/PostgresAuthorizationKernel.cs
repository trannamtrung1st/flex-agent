using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using Npgsql;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class PostgresAuthorizationKernel(PostgresConnectionAccessor connectionAccessor)
    : ICommitAuthorizationKernel
{
    private const string ActiveGrantSql = """
        SELECT grant_id, relationship_version
        FROM actor_organization_grants
        WHERE organization_id = @OrganizationId
          AND actor_id = @ActorId
          AND granted_action = @GrantedAction
          AND revoked_at IS NULL;
        """;

    private const string ActiveGrantForCommitSql = """
        SELECT grant_id, relationship_version
        FROM actor_organization_grants
        WHERE organization_id = @OrganizationId
          AND actor_id = @ActorId
          AND granted_action = @GrantedAction
          AND revoked_at IS NULL
        FOR SHARE;
        """;

    private const string ActorExistsSql = """
        SELECT 1
        FROM actors
        WHERE id = @ActorId;
        """;

    private const string LoadDelegationSql = """
        SELECT
            delegation_id,
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            service_actor_id,
            allowed_action,
            effective_at,
            expires_at,
            revoked_at,
            delegation_version
        FROM service_delegations
        WHERE delegation_id = @DelegationId;
        """;

    private const string LoadDelegationForCommitSql = """
        SELECT
            delegation_id,
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            service_actor_id,
            allowed_action,
            effective_at,
            expires_at,
            revoked_at,
            delegation_version
        FROM service_delegations
        WHERE delegation_id = @DelegationId
        FOR SHARE;
        """;

    public Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default) =>
        EvaluateAsync(request, useCommitLock: false, connection: null, transaction: null, cancellationToken);

    public Task<AuthorizationDecision> AuthorizeInTransactionAsync(
        AuthorizationRequest request,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default) =>
        EvaluateAsync(
            request,
            useCommitLock: false,
            RequireTransactionConnection(transaction),
            transaction,
            cancellationToken);

    public Task<AuthorizationDecision> ReauthorizeInTransactionAsync(
        AuthorizationRequest request,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default) =>
        EvaluateAsync(
            request,
            useCommitLock: true,
            RequireTransactionConnection(transaction),
            transaction,
            cancellationToken);

    private static NpgsqlConnection RequireTransactionConnection(NpgsqlTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return transaction.Connection
            ?? throw new InvalidOperationException("Authorization requires an open transaction connection.");
    }

    private async Task<AuthorizationDecision> EvaluateAsync(
        AuthorizationRequest request,
        bool useCommitLock,
        NpgsqlConnection? connection,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var ownsConnection = connection is null;
        connection ??= await connectionAccessor.OpenConnectionAsync(cancellationToken);

        try
        {
            if (request.Actor is null)
            {
                return AuthorizationDecision.Deny(AuthorizationReasonCodes.MissingActor);
            }

            var actorExists = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    ActorExistsSql,
                    new { ActorId = request.Actor.ActorId },
                    transaction,
                    cancellationToken: cancellationToken));

            if (actorExists is null)
            {
                return AuthorizationDecision.Deny(AuthorizationReasonCodes.UnknownActor);
            }

            if (request.DelegationId is { } delegationId)
            {
                return await EvaluateDelegationAsync(
                    request,
                    delegationId,
                    useCommitLock,
                    connection,
                    transaction,
                    cancellationToken);
            }

            var grantSql = useCommitLock ? ActiveGrantForCommitSql : ActiveGrantSql;
            var grant = await connection.QuerySingleOrDefaultAsync<GrantRow>(
                new CommandDefinition(
                    grantSql,
                    new
                    {
                        OrganizationId = request.Organization.OrganizationId,
                        ActorId = request.Actor.ActorId,
                        GrantedAction = request.Action,
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (grant is null)
            {
                return AuthorizationDecision.Deny(AuthorizationReasonCodes.DeniedNoGrant);
            }

            var scopeDecision = await SessionHumanGrantScopeValidation.ValidateAsync(
                request,
                connection,
                transaction,
                cancellationToken);
            if (scopeDecision is not null)
            {
                return scopeDecision;
            }

            return AuthorizationDecision.Permit(
                grant.relationship_version,
                authorizationReferenceType: AuthorizationReferenceTypes.ActorOrganizationGrant,
                authorizationReferenceId: grant.grant_id);
        }
        finally
        {
            if (ownsConnection)
            {
                await connection.DisposeAsync();
            }
        }
    }

    private static async Task<AuthorizationDecision> EvaluateDelegationAsync(
        AuthorizationRequest request,
        Guid delegationId,
        bool useCommitLock,
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (delegationId == Guid.Empty)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.MissingDelegation);
        }

        var row = await connection.QuerySingleOrDefaultAsync<DelegationRow>(
            new CommandDefinition(
                useCommitLock ? LoadDelegationForCommitSql : LoadDelegationSql,
                new { DelegationId = delegationId },
                transaction,
                cancellationToken: cancellationToken));
        if (row is null)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.MissingDelegation);
        }

        var now = DateTime.SpecifyKind(
            await connection.ExecuteScalarAsync<DateTime>(
                new CommandDefinition(
                    "SELECT clock_timestamp();",
                    transaction: transaction,
                    cancellationToken: cancellationToken)),
            DateTimeKind.Utc);
        var effectiveAt = DateTime.SpecifyKind(row.effective_at, DateTimeKind.Utc);
        var expiresAt = row.expires_at is { } expires
            ? DateTime.SpecifyKind(expires, DateTimeKind.Utc)
            : (DateTime?)null;

        if (row.revoked_at is not null)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.RevokedDelegation);
        }

        if (effectiveAt > now)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.DelegationNotEffective);
        }

        if (expiresAt is { } expiry && expiry <= now)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.ExpiredDelegation);
        }

        if (row.service_actor_id != request.Actor!.ActorId)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.DelegationActorMismatch);
        }

        if (!string.Equals(row.allowed_action, request.Action, StringComparison.Ordinal))
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.DelegationActionMismatch);
        }

        if (row.organization_id != request.Organization.OrganizationId
            || row.organization_id != request.Resource.Organization.OrganizationId
            || row.session_id != request.Resource.ResourceId
            || (request.ActivityId is { } activityId && row.activity_id != activityId)
            || (request.ParticipantId is { } participantId && row.participant_id != participantId)
            || (request.AttemptId is { } attemptId && row.attempt_id != attemptId)
            || !string.Equals(request.Resource.ResourceType, AuthorizationResourceTypes.Session, StringComparison.Ordinal))
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.ScopeMismatch);
        }

        return AuthorizationDecision.Permit(
            row.delegation_version,
            authorizationReferenceType: AuthorizationReferenceTypes.ServiceDelegation,
            authorizationReferenceId: row.delegation_id);
    }

    private sealed record GrantRow(Guid grant_id, long relationship_version);

    private sealed record DelegationRow(
        Guid delegation_id,
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        Guid service_actor_id,
        string allowed_action,
        DateTime effective_at,
        DateTime? expires_at,
        DateTime? revoked_at,
        long delegation_version);
}

public sealed class PostgresGrantRepository(PostgresConnectionAccessor connectionAccessor)
{
    public async Task RevokeAsync(
        Guid organizationId,
        Guid actorId,
        string grantedAction,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE actor_organization_grants
            SET revoked_at = NOW() AT TIME ZONE 'UTC'
            WHERE organization_id = @OrganizationId
              AND actor_id = @ActorId
              AND granted_action = @GrantedAction
              AND revoked_at IS NULL;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { OrganizationId = organizationId, ActorId = actorId, GrantedAction = grantedAction },
            cancellationToken: cancellationToken));
    }
}
