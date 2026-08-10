using Dapper;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using Npgsql;

namespace FlexAgent.Configuration.Infrastructure;

public sealed record ConfigurationSourceVersionIdempotencyRow(
    Guid OrganizationId,
    Guid ConfigurationSourceId,
    string Action,
    string IdempotencyKey,
    Guid VersionId,
    string PayloadFingerprint,
    DateTime CreatedAt);

public sealed class PostgresConfigurationSourceVersionIdempotencyRepository
{
    private const string SelectByKeySql = """
        SELECT
            organization_id AS OrganizationId,
            configuration_source_id AS ConfigurationSourceId,
            action AS Action,
            idempotency_key AS IdempotencyKey,
            version_id AS VersionId,
            payload_fingerprint AS PayloadFingerprint,
            created_at AS CreatedAt
        FROM configuration_source_version_idempotency
        WHERE organization_id = @OrganizationId
          AND configuration_source_id = @ConfigurationSourceId
          AND action = @Action
          AND idempotency_key = @IdempotencyKey;
        """;

    private const string InsertSql = """
        INSERT INTO configuration_source_version_idempotency (
            organization_id,
            configuration_source_id,
            action,
            idempotency_key,
            version_id,
            payload_fingerprint,
            created_at)
        VALUES (
            @OrganizationId,
            @ConfigurationSourceId,
            @Action,
            @IdempotencyKey,
            @VersionId,
            @PayloadFingerprint,
            @CreatedAt)
        ON CONFLICT (organization_id, configuration_source_id, action, idempotency_key) DO NOTHING
        RETURNING version_id;
        """;

    public async Task<ConfigurationSourceVersionIdempotencyRow?> GetByKeyAsync(
        Guid organizationId,
        Guid configurationSourceId,
        string action,
        string idempotencyKey,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return await transaction.Connection!.QuerySingleOrDefaultAsync<ConfigurationSourceVersionIdempotencyRow>(
            new CommandDefinition(
                SelectByKeySql,
                new
                {
                    OrganizationId = organizationId,
                    ConfigurationSourceId = configurationSourceId,
                    Action = action,
                    IdempotencyKey = idempotencyKey,
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    public async Task<Guid?> TryInsertAsync(
        ConfigurationSourceVersionIdempotencyRow row,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return await transaction.Connection!.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                InsertSql,
                row,
                transaction,
                cancellationToken: cancellationToken));
    }
}
