using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresHostedFrozenTimingDocumentSource(
    PostgresConnectionAccessor connections) : IHostedSessionFrozenTimingSource
{
    public async Task<HostedFrozenTimingPolicy> LoadAsync(
        Guid organizationId,
        Guid sessionId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        _ = asOfUtc;
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var document = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                """
                SELECT document::text
                FROM session_frozen_timing
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                """,
                new
                {
                    OrganizationId = organizationId,
                    SessionId = sessionId,
                },
                cancellationToken: cancellationToken));
        return HostedSessionFrozenTiming.FromDocumentJson(document);
    }
}
