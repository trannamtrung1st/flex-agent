using Dapper;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Domain;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionRuntimeSchemaTests(PostgresIntegrationFixture fixture) : PostgresIntegrationTest(fixture)
{
    private static readonly string[] RequiredSessionTables =
    [
        "session_runtimes",
        "session_events",
        "session_turns",
        "session_visible_transcript_items",
        "session_invocations",
        "session_invocation_attempts",
        "session_decisions",
        "session_decision_validations",
        "session_execution_outcomes",
        "session_messages",
        "session_message_fragments",
        "session_timer_schedules",
        "session_pause_intervals",
        "session_terminal_intents",
        "session_terminal_records",
        "session_durable_work",
        "session_manifest_refs",
    ];

    [Fact]
    public async Task Session_runtime_tables_exist_with_session_prefix()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var tables = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE'
                  AND table_name LIKE 'session_%'
                ORDER BY table_name;
                """,
                cancellationToken: CancellationToken))).AsList();

        Assert.Equal(RequiredSessionTables.OrderBy(name => name, StringComparer.Ordinal), tables);
    }

    [Fact]
    public async Task Decision_validation_distinguishes_observed_state_from_commit_state()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var columns = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_decision_validations';
                """,
                cancellationToken: CancellationToken))).AsList();

        Assert.Contains("validated_against_session_version", columns);
        Assert.Contains("validated_against_session_sequence", columns);
        Assert.Contains("validation_commit_session_version", columns);
        Assert.Contains("validation_commit_session_sequence", columns);
        Assert.Contains("validation_committed_at", columns);
        Assert.DoesNotContain("validated_at_session_version", columns);
    }

    [Fact]
    public async Task Decisions_store_payload_digest_and_explicit_format_version()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var columns = (await connection.QueryAsync<(string ColumnName, string? ColumnDefault)>(
            new CommandDefinition(
                """
                SELECT column_name, column_default
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_decisions'
                  AND column_name IN ('payload_digest', 'decision_payload_digest_version');
                """,
                cancellationToken: CancellationToken))).AsList();

        Assert.Contains(columns, column => column.ColumnName == "payload_digest");
        var version = Assert.Single(columns, column => column.ColumnName == "decision_payload_digest_version");
        Assert.Contains(DecisionPayloadDigest.FormatVersionV1, version.ColumnDefault, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Database_stamps_commit_time_instead_of_client_supplied_timestamp()
    {
        var seeded = await SeedRuntimeAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var clientTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var committedAt = await connection.ExecuteScalarAsync<DateTime>(
            new CommandDefinition(
                """
                INSERT INTO session_events (
                    event_id, organization_id, activity_id, participant_id, attempt_id, session_id,
                    session_sequence, event_type, payload_digest, committed_at)
                VALUES (
                    @EventId, @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    1, 'session.runtime.started', @PayloadDigest, @ClientTime)
                RETURNING committed_at;
                """,
                new
                {
                    EventId = Guid.NewGuid(),
                    seeded.OrganizationId,
                    seeded.ActivityId,
                    seeded.ParticipantId,
                    seeded.AttemptId,
                    seeded.SessionId,
                    PayloadDigest = LowercaseDigest('a'),
                    ClientTime = clientTime,
                },
                cancellationToken: CancellationToken));

        Assert.NotEqual(clientTime, committedAt);
        Assert.True(committedAt > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Validation_revisions_are_append_only_for_observed_state()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decision_validations (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, revision_ordinal,
                    validated_against_session_version, validated_against_session_sequence,
                    validation_commit_session_version, validation_commit_session_sequence,
                    validation_outcome, effect_outcome, timer_validation_outcome)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 1,
                    0, 1,
                    1, 2,
                    'accepted', 'not_attempted', 'not_present');
                """,
                seeded,
                cancellationToken: CancellationToken));

        var updateException = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_decision_validations
                    SET validated_against_session_version = 99
                    WHERE session_id = @SessionId
                      AND agent_invocation_id = @InvocationId
                      AND revision_ordinal = 1;
                    """,
                    seeded,
                    cancellationToken: CancellationToken)));
        Assert.Contains("append-only", updateException.MessageText, StringComparison.OrdinalIgnoreCase);

        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE session_decision_validations
                SET effect_outcome = 'applied',
                    applied_turn_id = 'turn.agent.inv-1',
                    applied_response_slot_id = 'slot.agent.inv-1'
                WHERE session_id = @SessionId
                  AND agent_invocation_id = @InvocationId
                  AND revision_ordinal = 1;
                """,
                seeded,
                cancellationToken: CancellationToken));
        Assert.Equal(1, updated);
    }

    [Fact]
    public async Task Duplicate_decision_identity_and_pending_timer_are_rejected()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);

        var duplicateDecision = await Assert.ThrowsAsync<PostgresException>(async () =>
            await InsertDecisionAsync(connection, seeded with { DecisionId = "dec-other" }));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicateDecision.SqlState);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_timer_schedules (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    schedule_revision, state, relative_delay)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    'rev-1', 'pending', 'PT5S');
                """,
                seeded,
                cancellationToken: CancellationToken));

        var duplicateTimer = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_timer_schedules (
                        organization_id, activity_id, participant_id, attempt_id, session_id,
                        schedule_revision, state, relative_delay)
                    VALUES (
                        @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                        'rev-2', 'claimed', 'PT5S');
                    """,
                    seeded,
                    cancellationToken: CancellationToken)));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicateTimer.SqlState);
    }

    [Fact]
    public async Task Invocation_idempotency_conflicts_across_distinct_identity()
    {
        var seeded = await SeedRuntimeAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertInvocationAsync(connection, seeded, "inv-1", "idem-1", "trigger-a");

        var conflict = await Assert.ThrowsAsync<PostgresException>(async () =>
            await InsertInvocationAsync(connection, seeded, "inv-2", "idem-1", "trigger-b"));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, conflict.SqlState);
    }

    [Fact]
    public async Task Child_rows_must_match_session_ownership_tuple()
    {
        var seeded = await SeedRuntimeAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);

        var mismatch = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_events (
                        event_id, organization_id, activity_id, participant_id, attempt_id, session_id,
                        session_sequence, event_type, payload_digest)
                    VALUES (
                        @EventId, @OrganizationId, @WrongActivityId, @ParticipantId, @AttemptId, @SessionId,
                        1, 'session.runtime.started', @PayloadDigest);
                    """,
                    new
                    {
                        EventId = Guid.NewGuid(),
                        seeded.OrganizationId,
                        WrongActivityId = Guid.NewGuid(),
                        seeded.ParticipantId,
                        seeded.AttemptId,
                        seeded.SessionId,
                        PayloadDigest = LowercaseDigest('a'),
                    },
                    cancellationToken: CancellationToken)));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, mismatch.SqlState);
    }

    [Fact]
    public async Task Session_runtime_rows_reject_delete()
    {
        var seeded = await SeedRuntimeAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);

        var deleteException = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM session_runtimes WHERE session_id = @SessionId;",
                    seeded,
                    cancellationToken: CancellationToken)));

        Assert.Contains("append-only", deleteException.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Message_fragments_require_session_sequence()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var columns = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_message_fragments';
                """,
                cancellationToken: CancellationToken))).AsList();

        Assert.Contains("session_sequence", columns);
        Assert.Contains("turn_id", columns);
        Assert.Contains("response_slot_id", columns);
        Assert.Contains("generation_attempt_id", columns);
    }

    private async Task<SeededRuntime> SeedRuntimeAsync()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var runtime = new SeededRuntime(
            organization.OrganizationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "inv-1",
            "dec-1");

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_runtimes (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    configuration_id, configuration_digest, manifest_id, lifecycle_state)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    'cfg-1', @ConfigurationDigest, 'man-1', 'active');
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    ConfigurationDigest = LowercaseDigest('b'),
                },
                cancellationToken: CancellationToken));

        return runtime;
    }

    private async Task<SeededRuntime> SeedRuntimeWithInvocationAsync()
    {
        var runtime = await SeedRuntimeAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertInvocationAsync(connection, runtime, runtime.InvocationId, "idem-1", "trigger-1");
        return runtime;
    }

    private async Task InsertInvocationAsync(
        NpgsqlConnection connection,
        SeededRuntime runtime,
        string invocationId,
        string idempotencyKey,
        string triggerId)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_invocations (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, trigger_family, trigger_type, trigger_id, purpose,
                    idempotency_key, policy_digest, admitted_session_sequence, status)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 'participant_input', 'participant_message', @TriggerId,
                    'participant_turn.respond', @IdempotencyKey, @PolicyDigest, 1, 'admitted');
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    InvocationId = invocationId,
                    TriggerId = triggerId,
                    IdempotencyKey = idempotencyKey,
                    PolicyDigest = LowercaseDigest('c'),
                },
                cancellationToken: CancellationToken));
    }

    private async Task InsertDecisionAsync(NpgsqlConnection connection, SeededRuntime runtime)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decisions (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, decision_id, decision_type, produced_at,
                    payload_digest, decision_payload_digest_version)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, @DecisionId, 'no_action', TIMESTAMPTZ '2026-08-13T00:00:00Z',
                    @PayloadDigest, @DigestVersion);
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    runtime.InvocationId,
                    runtime.DecisionId,
                    PayloadDigest = LowercaseDigest('d'),
                    DigestVersion = DecisionPayloadDigest.FormatVersionV1,
                },
                cancellationToken: CancellationToken));
    }

    private static string LowercaseDigest(char fill) => new(fill, 64);

    private sealed record SeededRuntime(
        Guid OrganizationId,
        Guid ActivityId,
        Guid ParticipantId,
        Guid AttemptId,
        Guid SessionId,
        string InvocationId,
        string DecisionId);
}
