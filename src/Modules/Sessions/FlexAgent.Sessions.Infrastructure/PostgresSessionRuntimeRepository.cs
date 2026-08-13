using Dapper;
using FlexAgent.Sessions.Domain;
using Npgsql;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresSessionRuntimeRepository
{
    private const string InsertActiveSql = """
        INSERT INTO session_runtimes (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            configuration_id, configuration_digest, manifest_id, lifecycle_state,
            session_version, session_sequence, cutoff_sequence)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @ConfigurationId, @ConfigurationDigest, @ManifestId, @LifecycleState,
            @SessionVersion, @SessionSequence, @CutoffSequence)
        RETURNING last_committed_at;
        """;

    private const string LoadForUpdateSql = """
        SELECT
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            configuration_id,
            configuration_digest,
            manifest_id,
            lifecycle_state,
            session_version,
            session_sequence,
            cutoff_sequence,
            last_committed_at
        FROM session_runtimes
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        FOR UPDATE;
        """;

    private const string LoadInvocationsSql = """
        SELECT
            agent_invocation_id,
            trigger_family,
            trigger_type,
            trigger_id,
            purpose,
            turn_id,
            response_slot_id,
            idempotency_key,
            policy_digest,
            admitted_session_sequence,
            last_session_sequence,
            status,
            admitted_at,
            last_committed_at
        FROM session_invocations
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY admitted_session_sequence;
        """;

    private const string UpdateHeadSql = """
        UPDATE session_runtimes
        SET
            lifecycle_state = @LifecycleState,
            session_version = @SessionVersion,
            session_sequence = @SessionSequence,
            cutoff_sequence = @CutoffSequence
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
          AND session_version = @ExpectedSessionVersion;
        """;

    private const string InsertInvocationSql = """
        INSERT INTO session_invocations (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, trigger_family, trigger_type, trigger_id, purpose,
            turn_id, response_slot_id, idempotency_key, policy_digest,
            admitted_session_sequence, status)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @TriggerFamily, @TriggerType, @TriggerId, @Purpose,
            @TurnId, @ResponseSlotId, @IdempotencyKey, @PolicyDigest,
            @AdmittedSessionSequence, @Status);
        """;

    private const string AuthoritativeUtcSql = "SELECT clock_timestamp();";

    public async Task<DateTimeOffset> ReadAuthoritativeUtcAsync(
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return ToUtc(await RequireConnection(transaction).ExecuteScalarAsync<DateTime>(
            new CommandDefinition(AuthoritativeUtcSql, transaction: transaction, cancellationToken: cancellationToken)));
    }

    public async Task InsertActiveAsync(
        SessionOwnership ownership,
        SessionRuntime session,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureOwnership(ownership, session.Ownership);

        var lastCommittedAt = ToUtc(await RequireConnection(transaction).ExecuteScalarAsync<DateTime>(
            new CommandDefinition(
                InsertActiveSql,
                HeadParameters(ownership, session, expectedSessionVersion: null),
                transaction,
                cancellationToken: cancellationToken)));

        session.ReplaceLastCommittedAtFromDatabase(lastCommittedAt);
    }

    public async Task<SessionRuntime?> LoadForUpdateAsync(
        SessionOwnership ownership,
        TrustedSessionBinding binding,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureOwnership(ownership, binding.Ownership);

        var connection = RequireConnection(transaction);
        var row = await connection.QuerySingleOrDefaultAsync<SessionRuntimeRow>(
            new CommandDefinition(
                LoadForUpdateSql,
                OwnershipParameters(ownership),
                transaction,
                cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        if (!string.Equals(row.configuration_id, binding.ConfigurationId, StringComparison.Ordinal)
            || !string.Equals(row.configuration_digest, binding.ConfigurationDigest, StringComparison.Ordinal)
            || !string.Equals(row.manifest_id, binding.ManifestId, StringComparison.Ordinal))
        {
            return null;
        }

        var invocationRows = (await connection.QueryAsync<SessionInvocationRow>(
            new CommandDefinition(
                LoadInvocationsSql,
                OwnershipParameters(ownership),
                transaction,
                cancellationToken: cancellationToken))).AsList();

        var invocations = invocationRows
            .Select(item => AgentInvocation.Rehydrate(
                item.agent_invocation_id,
                ownership,
                new TrustedTrigger(
                    item.trigger_family,
                    item.trigger_type,
                    item.trigger_id,
                    item.purpose,
                    item.turn_id,
                    item.response_slot_id),
                item.idempotency_key,
                item.policy_digest,
                item.last_session_sequence,
                item.status))
            .ToArray();

        var lastAdmittedAtByFamily = invocationRows
            .GroupBy(item => item.trigger_family, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => ToUtc(group.Max(item => item.admitted_at)),
                StringComparer.Ordinal);

        return SessionRuntime.Rehydrate(
            binding,
            FromDbLifecycle(row.lifecycle_state),
            row.session_version,
            row.session_sequence,
            row.cutoff_sequence,
            ToUtc(row.last_committed_at),
            invocations,
            lastAdmittedAtByFamily: lastAdmittedAtByFamily);
    }

    public async Task<bool> TrySaveAdmissionAsync(
        SessionOwnership ownership,
        long expectedSessionVersion,
        SessionRuntime session,
        AgentInvocation invocation,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(invocation);
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureOwnership(ownership, session.Ownership);
        EnsureOwnership(ownership, invocation.Ownership);

        var connection = RequireConnection(transaction);
        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateHeadSql,
                HeadParameters(ownership, session, expectedSessionVersion),
                transaction,
                cancellationToken: cancellationToken));
        if (updated != 1)
        {
            return false;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertInvocationSql,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    invocation.AgentInvocationId,
                    TriggerFamily = invocation.Trigger.TriggerFamily,
                    TriggerType = invocation.Trigger.TriggerType,
                    TriggerId = invocation.Trigger.TriggerId,
                    Purpose = invocation.Trigger.Purpose,
                    TurnId = invocation.Trigger.TurnId,
                    ResponseSlotId = invocation.Trigger.ResponseSlotId,
                    invocation.IdempotencyKey,
                    PolicyDigest = invocation.PolicyDigest,
                    AdmittedSessionSequence = invocation.SessionSequence,
                    invocation.Status,
                },
                transaction,
                cancellationToken: cancellationToken));
        return true;
    }

    private static DateTimeOffset ToUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static NpgsqlConnection RequireConnection(NpgsqlTransaction transaction) =>
        transaction.Connection ?? throw new InvalidOperationException("Transaction has no connection.");

    private static void EnsureOwnership(SessionOwnership left, SessionOwnership right)
    {
        if (left != right)
        {
            throw new InvalidOperationException("Session ownership tuple mismatch.");
        }
    }

    private static object OwnershipParameters(SessionOwnership ownership) => new
    {
        ownership.OrganizationId,
        ownership.ActivityId,
        ownership.ParticipantId,
        ownership.AttemptId,
        ownership.SessionId,
    };

    private static object HeadParameters(
        SessionOwnership ownership,
        SessionRuntime session,
        long? expectedSessionVersion) => new
    {
        ownership.OrganizationId,
        ownership.ActivityId,
        ownership.ParticipantId,
        ownership.AttemptId,
        ownership.SessionId,
        session.Binding.ConfigurationId,
        session.Binding.ConfigurationDigest,
        session.Binding.ManifestId,
        LifecycleState = ToDbLifecycle(session.LifecycleState),
        session.SessionVersion,
        session.SessionSequence,
        session.CutoffSequence,
        ExpectedSessionVersion = expectedSessionVersion,
    };

    private static string ToDbLifecycle(SessionLifecycleState state) => state switch
    {
        SessionLifecycleState.Ready => "ready",
        SessionLifecycleState.Active => "active",
        SessionLifecycleState.Paused => "paused",
        SessionLifecycleState.Completing => "completing",
        SessionLifecycleState.Completed => "completed",
        SessionLifecycleState.Terminated => "terminated",
        SessionLifecycleState.Aborted => "aborted",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown Session lifecycle."),
    };

    private static SessionLifecycleState FromDbLifecycle(string state) => state switch
    {
        "ready" => SessionLifecycleState.Ready,
        "active" => SessionLifecycleState.Active,
        "paused" => SessionLifecycleState.Paused,
        "completing" => SessionLifecycleState.Completing,
        "completed" => SessionLifecycleState.Completed,
        "terminated" => SessionLifecycleState.Terminated,
        "aborted" => SessionLifecycleState.Aborted,
        _ => throw new InvalidOperationException($"Unknown Session lifecycle '{state}'."),
    };

    private sealed record SessionRuntimeRow(
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        string configuration_id,
        string configuration_digest,
        string manifest_id,
        string lifecycle_state,
        long session_version,
        long session_sequence,
        long? cutoff_sequence,
        DateTime last_committed_at);

    private sealed record SessionInvocationRow(
        string agent_invocation_id,
        string trigger_family,
        string trigger_type,
        string trigger_id,
        string purpose,
        string? turn_id,
        string? response_slot_id,
        string idempotency_key,
        string policy_digest,
        long admitted_session_sequence,
        long last_session_sequence,
        string status,
        DateTime admitted_at,
        DateTime last_committed_at);
}
