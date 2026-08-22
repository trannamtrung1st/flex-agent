using System.Data;
using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.Postgres;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class PostgresHumanDisplayProfileDirectory(PostgresConnectionAccessor connections)
    : IHumanDisplayProfileDirectory
{
    public async Task<HumanDisplayCandidatePage> ListEligibleAsync(
        Guid organizationId,
        string requiredAction,
        string? prefix,
        Guid? afterActorId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", organizationId, DbType.Guid);
        parameters.Add("RequiredAction", requiredAction, DbType.String);
        parameters.Add("Prefix", string.IsNullOrWhiteSpace(prefix) ? null : prefix, DbType.String);
        parameters.Add("PrefixPattern", string.IsNullOrWhiteSpace(prefix) ? null : prefix + "%", DbType.String);
        parameters.Add("AfterId", afterActorId, DbType.Guid);
        parameters.Add("Limit", limit + 1, DbType.Int32);
        var rows = (await connection.QueryAsync<HumanDisplayCandidate>(
            new CommandDefinition(
                """
                SELECT profile.actor_id AS ActorId, profile.display_label AS DisplayLabel
                FROM identity_human_display_profiles AS profile
                INNER JOIN actors AS actor ON actor.id = profile.actor_id AND actor.disabled_at IS NULL
                INNER JOIN human_identity_bindings AS binding
                    ON binding.actor_id = profile.actor_id AND binding.disabled_at IS NULL
                INNER JOIN actor_organization_grants AS grant_row
                    ON grant_row.organization_id = profile.organization_id
                   AND grant_row.actor_id = profile.actor_id
                   AND grant_row.revoked_at IS NULL
                   AND grant_row.granted_action = @RequiredAction
                WHERE profile.organization_id = @OrganizationId
                  AND (@Prefix IS NULL OR profile.display_label ILIKE @PrefixPattern)
                  AND (@AfterId IS NULL OR profile.actor_id > @AfterId)
                ORDER BY profile.actor_id
                LIMIT @Limit
                """,
                parameters,
                cancellationToken: cancellationToken))).ToArray();
        var hasMore = rows.Length > limit;
        var taken = rows.Take(limit).ToArray();
        return new HumanDisplayCandidatePage(taken, hasMore);
    }

    public async Task<HumanDisplayCandidate?> RevalidateEligibleAsync(
        Guid organizationId,
        Guid actorId,
        string requiredAction,
        object? commitTransaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT profile.actor_id AS ActorId, profile.display_label AS DisplayLabel
            FROM identity_human_display_profiles AS profile
            INNER JOIN actors AS actor ON actor.id = profile.actor_id AND actor.disabled_at IS NULL
            INNER JOIN human_identity_bindings AS binding
                ON binding.actor_id = profile.actor_id AND binding.disabled_at IS NULL
            INNER JOIN actor_organization_grants AS grant_row
                ON grant_row.organization_id = profile.organization_id
               AND grant_row.actor_id = profile.actor_id
               AND grant_row.revoked_at IS NULL
               AND grant_row.granted_action = @RequiredAction
            WHERE profile.organization_id = @OrganizationId
              AND profile.actor_id = @ActorId
            """;
        var lockedSql = sql + """
            
            FOR SHARE OF profile
            FOR SHARE OF actor
            FOR SHARE OF binding
            FOR SHARE OF grant_row
            """;
        var parameters = new { OrganizationId = organizationId, ActorId = actorId, RequiredAction = requiredAction };
        var transaction = PostgresCommitTransaction.Optional(commitTransaction);
        if (transaction is not null)
        {
            return await transaction.Connection!.QuerySingleOrDefaultAsync<HumanDisplayCandidate>(
                new CommandDefinition(lockedSql, parameters, transaction, cancellationToken: cancellationToken));
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<HumanDisplayCandidate>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    public async Task<string?> FindDisplayLabelAsync(
        Guid organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                """
                SELECT display_label
                FROM identity_human_display_profiles
                WHERE organization_id = @OrganizationId AND actor_id = @ActorId
                """,
                new { OrganizationId = organizationId, ActorId = actorId },
                cancellationToken: cancellationToken));
    }
}
