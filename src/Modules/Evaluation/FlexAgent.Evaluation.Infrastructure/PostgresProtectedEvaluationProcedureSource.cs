using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresProtectedEvaluationProcedureSource(
    PostgresConnectionAccessor connectionAccessor) : IProtectedEvaluationProcedureSource
{
    public async Task<ProtectedCanonicalUtf8?> GetCanonicalUtf8Async(
        Guid organizationId,
        Guid sourceId,
        Guid sourceVersionId,
        string expectedDigest,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty
            || sourceId == Guid.Empty
            || sourceVersionId == Guid.Empty
            || !EvaluationIdentity.IsSha256Hex(expectedDigest))
        {
            return null;
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PayloadRow>(
            new CommandDefinition(
                """
                SELECT configuration_source_id, source_version_id, content_digest, canonical_utf8
                FROM configuration_source_payloads
                WHERE organization_id = @OrganizationId
                  AND configuration_source_id = @SourceId
                  AND source_version_id = @SourceVersionId
                  AND content_digest = @ExpectedDigest;
                """,
                new
                {
                    OrganizationId = organizationId,
                    SourceId = sourceId,
                    SourceVersionId = sourceVersionId,
                    ExpectedDigest = expectedDigest,
                },
                cancellationToken: cancellationToken));

        return row is null
            ? null
            : new ProtectedCanonicalUtf8(
                row.configuration_source_id,
                row.source_version_id,
                row.canonical_utf8,
                row.content_digest);
    }

    private sealed record PayloadRow(
        Guid configuration_source_id,
        Guid source_version_id,
        string content_digest,
        byte[] canonical_utf8);
}
