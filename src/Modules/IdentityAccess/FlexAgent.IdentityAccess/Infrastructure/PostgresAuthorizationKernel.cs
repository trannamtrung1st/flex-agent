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
        SELECT relationship_version
        FROM actor_organization_grants
        WHERE organization_id = @OrganizationId
          AND actor_id = @ActorId
          AND granted_action = @GrantedAction
          AND revoked_at IS NULL;
        """;

    private const string ActiveGrantForCommitSql = """
        SELECT relationship_version
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

    public Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default) =>
        EvaluateAsync(request, useCommitLock: false, connection: null, transaction: null, cancellationToken);

    public Task<AuthorizationDecision> ReauthorizeInTransactionAsync(
        AuthorizationRequest request,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default) =>
        EvaluateAsync(request, useCommitLock: true, transaction.Connection, transaction, cancellationToken);

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

            var grantSql = useCommitLock ? ActiveGrantForCommitSql : ActiveGrantSql;
            var relationshipVersion = await connection.ExecuteScalarAsync<long?>(
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

            if (relationshipVersion is null)
            {
                return AuthorizationDecision.Deny(AuthorizationReasonCodes.DeniedNoGrant);
            }

            return AuthorizationDecision.Permit(relationshipVersion.Value);
        }
        finally
        {
            if (ownsConnection)
            {
                await connection.DisposeAsync();
            }
        }
    }
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
