using Dapper;
using FlexAgent.Configuration.Domain;
using FlexAgent.Postgres;
using Npgsql;

namespace FlexAgent.Configuration.Infrastructure;

public sealed record ConfigurationSourceVersionRow(
    Guid Id,
    Guid OrganizationId,
    Guid ConfigurationSourceId,
    string SchemaVersion,
    string ProcedureId,
    string ContentDigest,
    string IdempotencyKey,
    DateTime CreatedAt);

public sealed class PostgresConfigurationSourceVersionRepository(PostgresConnectionAccessor connectionAccessor)
{
    private const string SelectByDigestSql = """
        SELECT
            id AS Id,
            organization_id AS OrganizationId,
            configuration_source_id AS ConfigurationSourceId,
            schema_version AS SchemaVersion,
            procedure_id AS ProcedureId,
            content_digest AS ContentDigest,
            idempotency_key AS IdempotencyKey,
            created_at AS CreatedAt
        FROM configuration_source_versions
        WHERE organization_id = @OrganizationId
          AND configuration_source_id = @ConfigurationSourceId
          AND content_digest = @ContentDigest;
        """;

    private const string SelectByIdSql = """
        SELECT
            id AS Id,
            organization_id AS OrganizationId,
            configuration_source_id AS ConfigurationSourceId,
            schema_version AS SchemaVersion,
            procedure_id AS ProcedureId,
            content_digest AS ContentDigest,
            idempotency_key AS IdempotencyKey,
            created_at AS CreatedAt
        FROM configuration_source_versions
        WHERE organization_id = @OrganizationId
          AND id = @VersionId;
        """;

    private const string InsertSql = """
        INSERT INTO configuration_source_versions (
            id,
            organization_id,
            configuration_source_id,
            schema_version,
            procedure_id,
            content_digest,
            idempotency_key,
            created_at)
        VALUES (
            @Id,
            @OrganizationId,
            @ConfigurationSourceId,
            @SchemaVersion,
            @ProcedureId,
            @ContentDigest,
            @IdempotencyKey,
            @CreatedAt)
        ON CONFLICT ON CONSTRAINT uq_configuration_source_versions_digest DO NOTHING
        RETURNING
            id AS Id,
            organization_id AS OrganizationId,
            configuration_source_id AS ConfigurationSourceId,
            schema_version AS SchemaVersion,
            procedure_id AS ProcedureId,
            content_digest AS ContentDigest,
            idempotency_key AS IdempotencyKey,
            created_at AS CreatedAt;
        """;

    private const string ListSql = """
        SELECT
            id AS Id,
            organization_id AS OrganizationId,
            configuration_source_id AS ConfigurationSourceId,
            schema_version AS SchemaVersion,
            procedure_id AS ProcedureId,
            content_digest AS ContentDigest,
            idempotency_key AS IdempotencyKey,
            created_at AS CreatedAt
        FROM configuration_source_versions
        WHERE organization_id = @OrganizationId
          AND configuration_source_id = @ConfigurationSourceId
        ORDER BY created_at;
        """;

    private const string CountSql = """
        SELECT COUNT(*)
        FROM configuration_source_versions
        WHERE organization_id = @OrganizationId
          AND configuration_source_id = @ConfigurationSourceId;
        """;

    private const string SourceExistsSql = """
        SELECT 1
        FROM configuration_sources
        WHERE organization_id = @OrganizationId
          AND id = @ConfigurationSourceId;
        """;

    public async Task<bool> SourceExistsInOrganizationAsync(
        Guid organizationId,
        Guid configurationSourceId,
        NpgsqlTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var connection = transaction?.Connection ?? await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var ownsConnection = transaction is null;

        try
        {
            var exists = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    SourceExistsSql,
                    new { OrganizationId = organizationId, ConfigurationSourceId = configurationSourceId },
                    transaction,
                    cancellationToken: cancellationToken));

            return exists is not null;
        }
        finally
        {
            if (ownsConnection)
            {
                await connection.DisposeAsync();
            }
        }
    }

    public async Task<ConfigurationSourceVersionRow?> GetByDigestAsync(
        Guid organizationId,
        Guid configurationSourceId,
        string contentDigest,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return await transaction.Connection!.QuerySingleOrDefaultAsync<ConfigurationSourceVersionRow>(
            new CommandDefinition(
                SelectByDigestSql,
                new { OrganizationId = organizationId, ConfigurationSourceId = configurationSourceId, ContentDigest = contentDigest },
                transaction,
                cancellationToken: cancellationToken));
    }

    public async Task<ConfigurationSourceVersionRow?> GetByIdAsync(
        Guid organizationId,
        Guid versionId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return await transaction.Connection!.QuerySingleOrDefaultAsync<ConfigurationSourceVersionRow>(
            new CommandDefinition(
                SelectByIdSql,
                new { OrganizationId = organizationId, VersionId = versionId },
                transaction,
                cancellationToken: cancellationToken));
    }

    public async Task<ConfigurationSourceVersionRow?> TryInsertAsync(
        ConfigurationSourceVersionRow row,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return await transaction.Connection!.QuerySingleOrDefaultAsync<ConfigurationSourceVersionRow>(
            new CommandDefinition(InsertSql, row, transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ConfigurationSourceVersionRow>> ListForSourceAsync(
        Guid organizationId,
        Guid configurationSourceId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ConfigurationSourceVersionRow>(
            new CommandDefinition(
                ListSql,
                new { OrganizationId = organizationId, ConfigurationSourceId = configurationSourceId },
                cancellationToken: cancellationToken));

        return rows.AsList();
    }

    public async Task<int> CountForSourceAsync(
        Guid organizationId,
        Guid configurationSourceId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountSql,
                new { OrganizationId = organizationId, ConfigurationSourceId = configurationSourceId },
                cancellationToken: cancellationToken));
    }
}
