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

    private const string SelectByIdForSourceSql = """
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

    public async Task<ConfigurationSourceVersionRow?> GetByIdForSourceAsync(
        Guid organizationId,
        Guid configurationSourceId,
        Guid versionId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return await transaction.Connection!.QuerySingleOrDefaultAsync<ConfigurationSourceVersionRow>(
            new CommandDefinition(
                SelectByIdForSourceSql,
                new
                {
                    OrganizationId = organizationId,
                    ConfigurationSourceId = configurationSourceId,
                    VersionId = versionId,
                },
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

    public async Task<bool> TryInsertNoticeProjectionSetAsync(
        Guid organizationId,
        Guid sourceId,
        Guid sourceVersionId,
        string sourceContentDigest,
        int noticeCount,
        DateTime createdAt,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var inserted = await transaction.Connection!.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                """
                INSERT INTO configuration_participant_notice_projection_sets (
                    organization_id, source_id, source_version_id, source_content_digest, notice_count, created_at)
                VALUES (
                    @OrganizationId, @SourceId, @SourceVersionId, @SourceContentDigest, @NoticeCount, @CreatedAt)
                ON CONFLICT DO NOTHING
                RETURNING source_version_id;
                """,
                new
                {
                    OrganizationId = organizationId,
                    SourceId = sourceId,
                    SourceVersionId = sourceVersionId,
                    SourceContentDigest = sourceContentDigest,
                    NoticeCount = noticeCount,
                    CreatedAt = createdAt,
                },
                transaction,
                cancellationToken: cancellationToken));
        return inserted is not null;
    }

    public Task InsertNoticeProjectionAsync(
        Guid organizationId,
        Guid sourceId,
        Guid sourceVersionId,
        string sourceContentDigest,
        ParticipantNoticeProjection notice,
        DateTime createdAt,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default) =>
        transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO configuration_participant_notice_projections (
                    organization_id, source_id, source_version_id, notice_id, notice_type, required_outcome,
                    protected_content_ref, content_digest, source_content_digest, created_at)
                VALUES (
                    @OrganizationId, @SourceId, @SourceVersionId, @NoticeId, @NoticeType, @RequiredOutcome,
                    @ProtectedContentRef, @ContentDigest, @SourceContentDigest, @CreatedAt);
                """,
                new
                {
                    OrganizationId = organizationId,
                    SourceId = sourceId,
                    SourceVersionId = sourceVersionId,
                    notice.NoticeId,
                    notice.NoticeType,
                    notice.RequiredOutcome,
                    notice.ProtectedContentRef,
                    notice.ContentDigest,
                    SourceContentDigest = sourceContentDigest,
                    CreatedAt = createdAt,
                },
                transaction,
                cancellationToken: cancellationToken));
}
