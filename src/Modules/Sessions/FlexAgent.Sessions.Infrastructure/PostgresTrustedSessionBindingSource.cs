using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresTrustedSessionBindingSource(PostgresConnectionAccessor connectionAccessor)
    : ITrustedSessionBindingSource
{
    private const string LoadSnapshotSql = """
        SELECT
            runtime.organization_id,
            runtime.activity_id,
            runtime.participant_id,
            runtime.attempt_id,
            runtime.session_id,
            runtime.configuration_id,
            runtime.configuration_digest,
            runtime.manifest_id,
            snapshot.policy_digest,
            snapshot.policy_payload::text AS policy_payload
        FROM session_runtimes AS runtime
        INNER JOIN session_frozen_policy_snapshots AS snapshot
            ON snapshot.organization_id = runtime.organization_id
           AND snapshot.activity_id = runtime.activity_id
           AND snapshot.participant_id = runtime.participant_id
           AND snapshot.attempt_id = runtime.attempt_id
           AND snapshot.session_id = runtime.session_id
        WHERE runtime.organization_id = @OrganizationId
          AND runtime.session_id = @SessionId
        """;

    private const string LoadRefsSql = """
        SELECT ref_kind, protected_ref, content_digest
        FROM session_manifest_refs
        WHERE organization_id = @OrganizationId
          AND session_id = @SessionId;
        """;

    public Task<TrustedSessionBinding?> GetAsync(
        SessionOwnership ownership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        return LoadAsync(
            ownership.OrganizationId,
            ownership.SessionId,
            ownership,
            cancellationToken);
    }

    public Task<TrustedSessionBinding?> GetForOrganizationSessionAsync(
        Guid organizationId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        LoadAsync(organizationId, sessionId, expectedOwnership: null, cancellationToken);

    private async Task<TrustedSessionBinding?> LoadAsync(
        Guid organizationId,
        Guid sessionId,
        SessionOwnership? expectedOwnership,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || sessionId == Guid.Empty)
        {
            return null;
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<SnapshotRow>(
            new CommandDefinition(
                LoadSnapshotSql,
                new { OrganizationId = organizationId, SessionId = sessionId },
                cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        var ownership = new SessionOwnership(
            row.organization_id,
            row.activity_id,
            row.participant_id,
            row.attempt_id,
            row.session_id);
        if (expectedOwnership is not null && expectedOwnership != ownership)
        {
            return null;
        }

        var policy = FrozenRuntimePolicySnapshot.TryParse(row.policy_payload, row.policy_digest);
        if (policy is null
            || string.IsNullOrWhiteSpace(row.configuration_id)
            || !string.Equals(row.configuration_digest, policy.PolicyDigest, StringComparison.Ordinal))
        {
            return null;
        }

        var refs = (await connection.QueryAsync<ManifestRefRow>(
            new CommandDefinition(
                LoadRefsSql,
                new { OrganizationId = organizationId, SessionId = sessionId },
                cancellationToken: cancellationToken))).AsList();

        return new TrustedSessionBinding(
            ownership,
            row.configuration_id,
            row.configuration_digest,
            row.manifest_id,
            policy,
            Refs(refs, "submission"),
            Refs(refs, "knowledge"),
            Refs(refs, "memory_read"));
    }

    private static IReadOnlyList<ProtectedContentRef> Refs(IReadOnlyList<ManifestRefRow> rows, string kind) =>
        rows.Where(row => string.Equals(row.ref_kind, kind, StringComparison.Ordinal))
            .Select(row => new ProtectedContentRef(row.protected_ref, row.content_digest))
            .ToArray();

    private sealed record SnapshotRow(
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        string configuration_id,
        string configuration_digest,
        string manifest_id,
        string policy_digest,
        string policy_payload);

    private sealed record ManifestRefRow(string ref_kind, string protected_ref, string content_digest);
}
