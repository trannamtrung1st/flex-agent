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
            snapshot.configuration_digest AS snapshot_configuration_digest,
            snapshot.policy_digest,
            snapshot.policy_payload::text AS policy_payload,
            frozen.profile_id,
            frozen.profile_version,
            frozen.profile_digest,
            frozen.provider_id,
            frozen.credential_mode,
            frozen.credential_binding_reference,
            frozen.credential_binding_version
        FROM session_runtimes AS runtime
        INNER JOIN session_frozen_policy_snapshots AS snapshot
            ON snapshot.organization_id = runtime.organization_id
           AND snapshot.activity_id = runtime.activity_id
           AND snapshot.participant_id = runtime.participant_id
           AND snapshot.attempt_id = runtime.attempt_id
           AND snapshot.session_id = runtime.session_id
        LEFT JOIN session_frozen_model_deployments AS frozen
            ON frozen.organization_id = runtime.organization_id
           AND frozen.activity_id = runtime.activity_id
           AND frozen.participant_id = runtime.participant_id
           AND frozen.attempt_id = runtime.attempt_id
           AND frozen.session_id = runtime.session_id
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
            || !string.Equals(row.configuration_digest, row.snapshot_configuration_digest, StringComparison.Ordinal))
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
            Refs(refs, "memory_read"),
            ToFrozenDeployment(row));
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
        string snapshot_configuration_digest,
        string policy_digest,
        string policy_payload,
        string? profile_id,
        string? profile_version,
        string? profile_digest,
        string? provider_id,
        string? credential_mode,
        string? credential_binding_reference,
        string? credential_binding_version);

    private static FrozenModelDeploymentBinding? ToFrozenDeployment(SnapshotRow row)
    {
        if (string.IsNullOrWhiteSpace(row.profile_id)
            || string.IsNullOrWhiteSpace(row.profile_version)
            || string.IsNullOrWhiteSpace(row.profile_digest)
            || string.IsNullOrWhiteSpace(row.provider_id)
            || string.IsNullOrWhiteSpace(row.credential_mode)
            || string.IsNullOrWhiteSpace(row.credential_binding_reference)
            || string.IsNullOrWhiteSpace(row.credential_binding_version))
        {
            return null;
        }

        return new FrozenModelDeploymentBinding(
            row.profile_id,
            row.profile_version,
            row.profile_digest,
            row.provider_id,
            row.credential_mode,
            row.credential_binding_reference,
            row.credential_binding_version);
    }

    private sealed record ManifestRefRow(string ref_kind, string protected_ref, string content_digest);
}
