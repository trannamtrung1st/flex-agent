using Dapper;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Domain;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionRuntimeSchemaTests(PostgresIntegrationFixture fixture) : PostgresIntegrationTest(fixture)
{
    private static readonly string[] RequiredSessionTables =
    [
        "session_actor_relationships",
        "session_runtimes",
        "session_events",
        "session_turns",
        "session_visible_transcript_items",
        "session_invocations",
        "session_invocation_attempts",
        "session_decisions",
        "session_decision_output_validations",
        "session_decision_output_effects",
        "session_decision_requested_action_validations",
        "session_decision_requested_action_effects",
        "session_decision_validations",
        "session_execution_outcomes",
        "session_messages",
        "session_message_fragments",
        "session_timer_schedules",
        "session_pause_intervals",
        "session_terminal_intents",
        "session_terminal_records",
        "session_durable_work",
        "session_durable_work_claim_partitions",
        "session_evaluation_handoffs",
        "session_frozen_policy_snapshots",
        "session_manifest_refs",
        "session_manifest_runtime_records",
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
    public async Task Timer_schedules_store_contract_lane_state_and_remaining_delay()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var columns = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_timer_schedules';
                """,
                cancellationToken: CancellationToken))).AsList();

        Assert.Contains("lane_state", columns);
        Assert.Contains("schedule_revision_ordinal", columns);
        Assert.Contains("remaining_active_seconds", columns);
        Assert.Contains("remaining_since", columns);
        Assert.Contains("requested_by_category", columns);
        Assert.Contains("created_at", columns);
        Assert.Contains("fired_invocation_id", columns);
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
    public async Task Decisions_store_v2_envelope_json_and_reject_v1_rows_with_json()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);

        var columns = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_decisions'
                  AND column_name IN ('envelope_schema_version', 'envelope_json');
                """,
                cancellationToken: CancellationToken))).AsList();
        Assert.Contains("envelope_schema_version", columns);
        Assert.Contains("envelope_json", columns);

        var v1Json = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_decisions (
                        organization_id, activity_id, participant_id, attempt_id, session_id,
                        agent_invocation_id, decision_id, decision_type, produced_at,
                        payload_digest, decision_payload_digest_version,
                        envelope_schema_version, envelope_json,
                        committed_session_version, committed_session_sequence)
                    VALUES (
                        @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                        @InvocationId, @DecisionId, 'no_action',
                        TIMESTAMPTZ '2026-08-13T00:00:02Z',
                        @PayloadDigest, @DigestVersion,
                        'v1', '{}'::jsonb,
                        1, 2);
                    """,
                    new
                    {
                        seeded.OrganizationId,
                        seeded.ActivityId,
                        seeded.ParticipantId,
                        seeded.AttemptId,
                        seeded.SessionId,
                        seeded.InvocationId,
                        seeded.DecisionId,
                        PayloadDigest = LowercaseDigest('e'),
                        DigestVersion = DecisionPayloadDigest.FormatVersionV1,
                    },
                    cancellationToken: CancellationToken)));
        Assert.Contains("envelope_presence", v1Json.ConstraintName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Output_and_action_validation_rows_are_append_only_and_bound_to_a_revision()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decision_output_validations (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, revision_ordinal, item_ordinal, local_ref, kind,
                    validation_outcome, rejection_reason_category, agent_output_id, effect_outcome)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 1, 0, 'out.message.primary', 'message',
                    'accepted', NULL, 'aout.roundtrip.0001', 'applied');
                """,
                seeded,
                cancellationToken: CancellationToken));

        var update = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_decision_output_validations
                    SET effect_outcome = 'not_attempted'
                    WHERE session_id = @SessionId
                      AND agent_invocation_id = @InvocationId;
                    """,
                    seeded,
                    cancellationToken: CancellationToken)));
        Assert.Contains("append-only", update.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Output_and_action_effect_rows_are_append_only_and_terminal()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decision_output_validations (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, revision_ordinal, item_ordinal, local_ref, kind,
                    validation_outcome, rejection_reason_category, agent_output_id, effect_outcome)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 1, 0, 'out.message.primary', 'message',
                    'accepted', NULL, 'aout.roundtrip.0001', 'not_attempted');
                """,
                seeded,
                cancellationToken: CancellationToken));

        var notAttempted = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_decision_output_effects (
                        organization_id, activity_id, participant_id, attempt_id, session_id,
                        agent_invocation_id, revision_ordinal, item_ordinal, effect_outcome)
                    VALUES (
                        @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                        @InvocationId, 1, 0, 'not_attempted');
                    """,
                    seeded,
                    cancellationToken: CancellationToken)));
        Assert.Contains("chk_session_decision_output_effects_outcome", notAttempted.ConstraintName, StringComparison.OrdinalIgnoreCase);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decision_output_effects (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, revision_ordinal, item_ordinal, effect_outcome)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 1, 0, 'applied');
                """,
                seeded,
                cancellationToken: CancellationToken));

        var update = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_decision_output_effects
                    SET effect_outcome = 'effect_failed'
                    WHERE session_id = @SessionId
                      AND agent_invocation_id = @InvocationId;
                    """,
                    seeded,
                    cancellationToken: CancellationToken)));
        Assert.Contains("append-only", update.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Item_effect_rows_must_match_the_validation_item_ownership_tuple()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");
        await InsertOutputValidationAsync(connection, seeded, itemOrdinal: 0, validationOutcome: "accepted");
        await InsertActionValidationAsync(connection, seeded, itemOrdinal: 0, validationOutcome: "accepted");

        foreach (var mismatch in new[]
                 {
                     seeded with { ActivityId = Guid.NewGuid() },
                     seeded with { ParticipantId = Guid.NewGuid() },
                     seeded with { AttemptId = Guid.NewGuid() },
                 })
        {
            var outputMismatch = await Assert.ThrowsAsync<PostgresException>(
                async () => await InsertOutputEffectAsync(connection, mismatch, itemOrdinal: 0, effectOutcome: "applied"));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, outputMismatch.SqlState);

            var actionMismatch = await Assert.ThrowsAsync<PostgresException>(
                async () => await InsertActionEffectAsync(connection, mismatch, itemOrdinal: 0, effectOutcome: "applied"));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, actionMismatch.SqlState);
        }
    }

    [Fact]
    public async Task Item_effect_rows_cannot_reference_a_rejected_validation_item()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");
        await InsertOutputValidationAsync(connection, seeded, itemOrdinal: 0, validationOutcome: "rejected");
        await InsertActionValidationAsync(connection, seeded, itemOrdinal: 0, validationOutcome: "rejected");

        var rejectedOutput = await Assert.ThrowsAsync<PostgresException>(
            async () => await InsertOutputEffectAsync(connection, seeded, itemOrdinal: 0, effectOutcome: "applied"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, rejectedOutput.SqlState);

        var rejectedAction = await Assert.ThrowsAsync<PostgresException>(
            async () => await InsertActionEffectAsync(connection, seeded, itemOrdinal: 0, effectOutcome: "applied"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, rejectedAction.SqlState);
    }

    [Fact]
    public async Task Agent_messages_and_fragments_link_to_decision_and_accepted_output()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var messageColumns = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_messages';
                """,
                cancellationToken: CancellationToken))).AsList();
        var fragmentColumns = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_message_fragments';
                """,
                cancellationToken: CancellationToken))).AsList();

        Assert.Contains("generation_attempt_id", messageColumns);
        Assert.Contains("response_slot_id", messageColumns);
        Assert.Contains("driving_invocation_id", messageColumns);
        Assert.Contains("driving_decision_id", messageColumns);
        Assert.Contains("accepted_agent_output_id", messageColumns);
        Assert.Contains("assembled_content_digest", messageColumns);
        Assert.Contains("exact_utf8_text", fragmentColumns);
        Assert.Contains("driving_invocation_id", fragmentColumns);
        Assert.Contains("driving_decision_id", fragmentColumns);
        Assert.DoesNotContain("accepted_agent_output_id", fragmentColumns);

        var seeded = await SeedRuntimeWithInvocationAsync();
        await InsertDecisionAsync(connection, seeded);
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");
        await InsertOutputValidationAsync(connection, seeded, itemOrdinal: 0, validationOutcome: "accepted");
        await InsertOutputValidationAsync(connection, seeded, itemOrdinal: 1, validationOutcome: "rejected");

        await InsertAgentMessageAsync(connection, seeded, "aout.roundtrip.0001", acceptedAgentOutputId: "aout.roundtrip.0001");
        await InsertAgentFragmentAsync(
            connection,
            seeded,
            "aout.roundtrip.0001",
            fragmentOrdinal: 1,
            sessionSequence: 3,
            exactUtf8Text: "Hel",
            acceptedAgentOutputId: "aout.roundtrip.0001");

        var wrongActivity = await Assert.ThrowsAsync<PostgresException>(
            async () => await InsertAgentFragmentAsync(
                connection,
                seeded with { ActivityId = Guid.NewGuid() },
                "aout.roundtrip.0001",
                fragmentOrdinal: 2,
                sessionSequence: 4,
                exactUtf8Text: "lo",
                acceptedAgentOutputId: "aout.roundtrip.0001"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, wrongActivity.SqlState);

        var unknownOutput = await Assert.ThrowsAsync<PostgresException>(
            async () => await InsertAgentMessageAsync(
                connection,
                seeded,
                "aout.unknown.0001",
                acceptedAgentOutputId: "aout.unknown.0001"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, unknownOutput.SqlState);
    }

    [Fact]
    public async Task Agent_fragments_reject_empty_or_digest_mismatched_text_and_allow_incomplete()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");
        await InsertOutputValidationAsync(connection, seeded, itemOrdinal: 0, validationOutcome: "accepted");
        await InsertAgentMessageAsync(
            connection,
            seeded,
            "aout.roundtrip.0001",
            acceptedAgentOutputId: "aout.roundtrip.0001",
            completionState: "incomplete",
            assembledContentDigest: ProtectedContentRef.DigestUtf8("Hel"));

        var empty = await Assert.ThrowsAsync<PostgresException>(
            async () => await InsertAgentFragmentAsync(
                connection,
                seeded,
                "aout.roundtrip.0001",
                fragmentOrdinal: 1,
                sessionSequence: 3,
                exactUtf8Text: string.Empty,
                acceptedAgentOutputId: "aout.roundtrip.0001"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, empty.SqlState);

        var mismatch = await Assert.ThrowsAsync<PostgresException>(
            async () => await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_message_fragments (
                        organization_id, activity_id, participant_id, attempt_id, session_id,
                        message_id, fragment_ordinal, session_sequence, turn_id, response_slot_id,
                        generation_attempt_id, protected_ref, content_digest, exact_utf8_text,
                        driving_invocation_id, driving_decision_id)
                    VALUES (
                        @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                        @MessageId, 1, 3, 'turn.1', 'slot.1',
                        'agen.1', 'frag:aout.roundtrip.0001:1', @ContentDigest, 'Hel',
                        @InvocationId, @DecisionId);
                    """,
                    new
                    {
                        seeded.OrganizationId,
                        seeded.ActivityId,
                        seeded.ParticipantId,
                        seeded.AttemptId,
                        seeded.SessionId,
                        MessageId = "aout.roundtrip.0001",
                        ContentDigest = ProtectedContentRef.DigestUtf8("HELLO"),
                        seeded.InvocationId,
                        seeded.DecisionId,
                    },
                    cancellationToken: CancellationToken)));
        Assert.Equal(PostgresErrorCodes.CheckViolation, mismatch.SqlState);
    }

    [Fact]
    public async Task Agent_fragments_cannot_cross_link_another_invocation_or_attempt_in_the_same_session()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");
        await InsertOutputValidationAsync(connection, seeded, itemOrdinal: 0, validationOutcome: "accepted");
        await InsertAgentMessageAsync(connection, seeded, "aout.roundtrip.0001", acceptedAgentOutputId: "aout.roundtrip.0001");

        var other = seeded with { InvocationId = "inv-2", DecisionId = "dec-2" };
        await InsertInvocationAsync(connection, other, other.InvocationId, "idem-2", "trigger-2");
        await InsertDecisionAsync(connection, other);

        var otherInvocation = await Assert.ThrowsAsync<PostgresException>(
            async () => await InsertAgentFragmentAsync(
                connection,
                other,
                "aout.roundtrip.0001",
                fragmentOrdinal: 1,
                sessionSequence: 3,
                exactUtf8Text: "Hel",
                acceptedAgentOutputId: "aout.roundtrip.0001"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, otherInvocation.SqlState);

        var otherAttempt = await Assert.ThrowsAsync<PostgresException>(
            async () => await InsertAgentFragmentAsync(
                connection,
                seeded,
                "aout.roundtrip.0001",
                fragmentOrdinal: 1,
                sessionSequence: 3,
                exactUtf8Text: "Hel",
                acceptedAgentOutputId: "aout.roundtrip.0001",
                generationAttemptId: "agen.other"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, otherAttempt.SqlState);

        var otherTurn = await Assert.ThrowsAsync<PostgresException>(
            async () => await InsertAgentFragmentAsync(
                connection,
                seeded,
                "aout.roundtrip.0001",
                fragmentOrdinal: 1,
                sessionSequence: 3,
                exactUtf8Text: "Hel",
                acceptedAgentOutputId: "aout.roundtrip.0001",
                turnId: "turn.other",
                responseSlotId: "slot.other"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, otherTurn.SqlState);
    }

    [Fact]
    public async Task Agent_fragments_cannot_attach_to_a_participant_message_and_agent_messages_require_a_turn()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_messages (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    message_id, author_type, turn_id, protected_ref, content_digest, completion_state)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    'msg.p.1', 'participant', 'turn.1', @ProtectedRef, @ContentDigest, 'complete');
                """,
                new
                {
                    seeded.OrganizationId,
                    seeded.ActivityId,
                    seeded.ParticipantId,
                    seeded.AttemptId,
                    seeded.SessionId,
                    ProtectedRef = "msg:msg.p.1",
                    ContentDigest = ProtectedContentRef.DigestForReference("msg:msg.p.1"),
                },
                cancellationToken: CancellationToken));

        var participantParent = await Assert.ThrowsAsync<PostgresException>(
            async () => await InsertAgentFragmentAsync(
                connection,
                seeded,
                "msg.p.1",
                fragmentOrdinal: 1,
                sessionSequence: 3,
                exactUtf8Text: "Hel",
                acceptedAgentOutputId: null));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, participantParent.SqlState);

        var missingTurn = await Assert.ThrowsAsync<PostgresException>(
            async () => await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_messages (
                        organization_id, activity_id, participant_id, attempt_id, session_id,
                        message_id, author_type, turn_id, protected_ref, content_digest,
                        completion_state, generation_attempt_id, driving_invocation_id, driving_decision_id)
                    VALUES (
                        @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                        'aout.missing.turn', 'agent', NULL, @ProtectedRef, @ContentDigest,
                        'open', 'agen.1', @InvocationId, @DecisionId);
                    """,
                    new
                    {
                        seeded.OrganizationId,
                        seeded.ActivityId,
                        seeded.ParticipantId,
                        seeded.AttemptId,
                        seeded.SessionId,
                        ProtectedRef = "msg:aout.missing.turn",
                        ContentDigest = ProtectedContentRef.DigestForReference("msg:aout.missing.turn"),
                        seeded.InvocationId,
                        seeded.DecisionId,
                    },
                    cancellationToken: CancellationToken)));
        Assert.Equal(PostgresErrorCodes.CheckViolation, missingTurn.SqlState);
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
                    applied_response_slot_id = 'slot.agent.inv-1',
                    effect_commit_session_version = 2,
                    effect_commit_session_sequence = 3
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
            await InsertDecisionAsync(connection, seeded with { DecisionId = "dec-other" }, committedSessionSequence: 3));
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
    public async Task Recorded_prohibited_decision_types_are_persistable()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);

        var inserted = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decisions (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, decision_id, decision_type, produced_at,
                    payload_digest, decision_payload_digest_version,
                    committed_session_version, committed_session_sequence)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, @DecisionId, @DecisionType, TIMESTAMPTZ '2026-08-13T00:00:00Z',
                    @PayloadDigest, @DigestVersion,
                    1, 2);
                """,
                new
                {
                    seeded.OrganizationId,
                    seeded.ActivityId,
                    seeded.ParticipantId,
                    seeded.AttemptId,
                    seeded.SessionId,
                    seeded.InvocationId,
                    DecisionId = "dec-tool",
                    DecisionType = RuntimeDecisionTypes.RequestTool,
                    PayloadDigest = LowercaseDigest('e'),
                    DigestVersion = DecisionPayloadDigest.FormatVersionV1,
                },
                cancellationToken: CancellationToken));

        Assert.Equal(1, inserted);
    }

    [Fact]
    public async Task Invocation_descendants_must_match_invocation_ownership_tuple()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);

        var mismatch = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_decisions (
                        organization_id, activity_id, participant_id, attempt_id, session_id,
                        agent_invocation_id, decision_id, decision_type, produced_at,
                        payload_digest, decision_payload_digest_version,
                        committed_session_version, committed_session_sequence)
                    VALUES (
                        @OrganizationId, @ActivityId, @ForgedParticipantId, @AttemptId, @SessionId,
                        @InvocationId, @DecisionId, 'no_action', TIMESTAMPTZ '2026-08-13T00:00:00Z',
                        @PayloadDigest, @DigestVersion,
                        1, 2);
                    """,
                    new
                    {
                        seeded.OrganizationId,
                        seeded.ActivityId,
                        ForgedParticipantId = Guid.NewGuid(),
                        seeded.AttemptId,
                        seeded.SessionId,
                        seeded.InvocationId,
                        seeded.DecisionId,
                        PayloadDigest = LowercaseDigest('d'),
                        DigestVersion = DecisionPayloadDigest.FormatVersionV1,
                    },
                    cancellationToken: CancellationToken)));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, mismatch.SqlState);
    }

    [Fact]
    public async Task Terminal_effects_are_one_way_on_the_latest_accepted_revision()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 2, validationOutcome: "rejected");

        var staleRevision = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_decision_validations
                    SET effect_outcome = 'applied',
                        applied_turn_id = 'turn.agent.inv-1',
                        applied_response_slot_id = 'slot.agent.inv-1',
                        effect_commit_session_version = 3,
                        effect_commit_session_sequence = 4
                    WHERE session_id = @SessionId
                      AND agent_invocation_id = @InvocationId
                      AND revision_ordinal = 1;
                    """,
                    seeded,
                    cancellationToken: CancellationToken)));
        Assert.Contains("latest revision", staleRevision.MessageText, StringComparison.OrdinalIgnoreCase);

        var rejectedLatest = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_decision_validations
                    SET effect_outcome = 'applied',
                        effect_commit_session_version = 3,
                        effect_commit_session_sequence = 4
                    WHERE session_id = @SessionId
                      AND agent_invocation_id = @InvocationId
                      AND revision_ordinal = 2;
                    """,
                    seeded,
                    cancellationToken: CancellationToken)));
        Assert.Contains("accepted", rejectedLatest.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Terminal_effects_cannot_be_rewritten_or_inserted()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);

        var terminalInsert = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_decision_validations (
                        organization_id, activity_id, participant_id, attempt_id, session_id,
                        agent_invocation_id, revision_ordinal,
                        validated_against_session_version, validated_against_session_sequence,
                        validation_commit_session_version, validation_commit_session_sequence,
                        validation_outcome, effect_outcome, timer_validation_outcome,
                        effect_commit_session_version, effect_commit_session_sequence)
                    VALUES (
                        @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                        @InvocationId, 1,
                        0, 1,
                        1, 2,
                        'accepted', 'applied', 'not_present',
                        1, 2);
                    """,
                    seeded,
                    cancellationToken: CancellationToken)));
        Assert.Contains("not_attempted", terminalInsert.MessageText, StringComparison.OrdinalIgnoreCase);

        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE session_decision_validations
                SET effect_outcome = 'applied',
                    applied_turn_id = 'turn.agent.inv-1',
                    applied_response_slot_id = 'slot.agent.inv-1',
                    effect_commit_session_version = 2,
                    effect_commit_session_sequence = 3
                WHERE session_id = @SessionId
                  AND agent_invocation_id = @InvocationId
                  AND revision_ordinal = 1;
                """,
                seeded,
                cancellationToken: CancellationToken));

        var rewrite = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_decision_validations
                    SET effect_outcome = 'effect_failed'
                    WHERE session_id = @SessionId
                      AND agent_invocation_id = @InvocationId
                      AND revision_ordinal = 1;
                    """,
                    seeded,
                    cancellationToken: CancellationToken)));
        Assert.Contains("immutable", rewrite.MessageText, StringComparison.OrdinalIgnoreCase);

        var afterTerminal = await Assert.ThrowsAsync<PostgresException>(async () =>
            await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 2, validationOutcome: "rejected"));
        Assert.Contains("terminal effect", afterTerminal.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Decision_and_outcome_rows_store_commit_session_sequence()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var decisionColumns = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_decisions';
                """,
                cancellationToken: CancellationToken))).AsList();
        var outcomeColumns = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_execution_outcomes';
                """,
                cancellationToken: CancellationToken))).AsList();
        var invocationColumns = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_invocations';
                """,
                cancellationToken: CancellationToken))).AsList();

        Assert.Contains("committed_session_sequence", decisionColumns);
        Assert.Contains("committed_session_version", outcomeColumns);
        Assert.Contains("committed_session_sequence", outcomeColumns);
        Assert.Contains("last_session_sequence", invocationColumns);
        Assert.Contains("admitted_at", invocationColumns);
        Assert.Contains("last_committed_at", invocationColumns);
    }

    [Fact]
    public async Task Invocation_admitted_at_is_immutable_across_lifecycle_updates()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var before = await connection.QuerySingleAsync<(DateTime AdmittedAt, DateTime LastCommittedAt)>(
            new CommandDefinition(
                """
                SELECT admitted_at, last_committed_at
                FROM session_invocations
                WHERE session_id = @SessionId
                  AND agent_invocation_id = @InvocationId;
                """,
                seeded,
                cancellationToken: CancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                "SELECT pg_sleep(0.05);",
                cancellationToken: CancellationToken));
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE session_invocations
                SET status = 'executing'
                WHERE session_id = @SessionId
                  AND agent_invocation_id = @InvocationId;
                """,
                seeded,
                cancellationToken: CancellationToken));

        var after = await connection.QuerySingleAsync<(DateTime AdmittedAt, DateTime LastCommittedAt)>(
            new CommandDefinition(
                """
                SELECT admitted_at, last_committed_at
                FROM session_invocations
                WHERE session_id = @SessionId
                  AND agent_invocation_id = @InvocationId;
                """,
                seeded,
                cancellationToken: CancellationToken));

        Assert.Equal(before.AdmittedAt, after.AdmittedAt);
        Assert.True(after.LastCommittedAt > before.LastCommittedAt);
    }

    [Fact]
    public async Task Decision_commit_sequence_is_required_and_bumps_invocation_sequence()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);

        var missingSequence = await Assert.ThrowsAsync<PostgresException>(async () =>
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
                        seeded.OrganizationId,
                        seeded.ActivityId,
                        seeded.ParticipantId,
                        seeded.AttemptId,
                        seeded.SessionId,
                        seeded.InvocationId,
                        seeded.DecisionId,
                        PayloadDigest = LowercaseDigest('d'),
                        DigestVersion = DecisionPayloadDigest.FormatVersionV1,
                    },
                    cancellationToken: CancellationToken)));
        Assert.Equal(PostgresErrorCodes.NotNullViolation, missingSequence.SqlState);

        await InsertDecisionAsync(connection, seeded);
        var lastSequence = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                """
                SELECT last_session_sequence
                FROM session_invocations
                WHERE session_id = @SessionId
                  AND agent_invocation_id = @InvocationId;
                """,
                seeded,
                cancellationToken: CancellationToken));
        Assert.Equal(2, lastSequence);

        var outcomeRuntime = seeded with { InvocationId = "inv-2", DecisionId = "dec-2" };
        await InsertInvocationAsync(connection, outcomeRuntime, outcomeRuntime.InvocationId, "idem-2", "trigger-2");

        var missingOutcomeSequence = await Assert.ThrowsAsync<PostgresException>(async () =>
            await InsertExecutionOutcomeAsync(connection, outcomeRuntime, committedSessionSequence: null));
        Assert.Equal(PostgresErrorCodes.NotNullViolation, missingOutcomeSequence.SqlState);

        await InsertExecutionOutcomeAsync(connection, outcomeRuntime, committedSessionSequence: 2);
        var outcomeLastSequence = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                """
                SELECT last_session_sequence
                FROM session_invocations
                WHERE session_id = @SessionId
                  AND agent_invocation_id = @InvocationId;
                """,
                outcomeRuntime,
                cancellationToken: CancellationToken));
        Assert.Equal(2, outcomeLastSequence);
    }

    [Fact]
    public async Task Invocation_rejects_both_a_decision_and_an_execution_outcome()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);

        var outcomeAfterDecision = await Assert.ThrowsAsync<PostgresException>(async () =>
            await InsertExecutionOutcomeAsync(connection, seeded, committedSessionSequence: 3));
        Assert.Contains("ExecutionOutcome", outcomeAfterDecision.MessageText, StringComparison.OrdinalIgnoreCase);

        var outcomeOnly = seeded with { InvocationId = "inv-2", DecisionId = "dec-2" };
        await InsertInvocationAsync(connection, outcomeOnly, outcomeOnly.InvocationId, "idem-2", "trigger-2");
        await InsertExecutionOutcomeAsync(connection, outcomeOnly, committedSessionSequence: 2);

        var decisionAfterOutcome = await Assert.ThrowsAsync<PostgresException>(async () =>
            await InsertDecisionAsync(connection, outcomeOnly, committedSessionSequence: 3));
        Assert.Contains("Decision", decisionAfterOutcome.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Validation_revisions_require_the_invocation_decision()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);

        var missingDecision = await Assert.ThrowsAsync<PostgresException>(async () =>
            await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, missingDecision.SqlState);

        await InsertDecisionAsync(connection, seeded);
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");
    }

    [Fact]
    public async Task Effect_commit_state_cannot_precede_validation_commit_state()
    {
        var seeded = await SeedRuntimeWithInvocationAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await InsertDecisionAsync(connection, seeded);
        await InsertValidationRevisionAsync(connection, seeded, revisionOrdinal: 1, validationOutcome: "accepted");

        var olderCommit = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_decision_validations
                    SET effect_outcome = 'applied',
                        effect_commit_session_version = 0,
                        effect_commit_session_sequence = 1
                    WHERE session_id = @SessionId
                      AND agent_invocation_id = @InvocationId
                      AND revision_ordinal = 1;
                    """,
                    seeded,
                    cancellationToken: CancellationToken)));
        Assert.Equal(PostgresErrorCodes.CheckViolation, olderCommit.SqlState);
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

    [Fact]
    public async Task Agent_messages_store_seal_session_sequence()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var columns = (await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'session_messages';
                """,
                cancellationToken: CancellationToken))).AsList();

        Assert.Contains("sealed_session_sequence", columns);
        Assert.Contains("sealed_at", columns);
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

    private async Task InsertDecisionAsync(
        NpgsqlConnection connection,
        SeededRuntime runtime,
        long committedSessionSequence = 2)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decisions (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, decision_id, decision_type, produced_at,
                    payload_digest, decision_payload_digest_version,
                    committed_session_version, committed_session_sequence)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, @DecisionId, 'no_action', TIMESTAMPTZ '2026-08-13T00:00:00Z',
                    @PayloadDigest, @DigestVersion,
                    1, @CommittedSessionSequence);
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
                    CommittedSessionSequence = committedSessionSequence,
                },
                cancellationToken: CancellationToken));
    }

    private async Task InsertExecutionOutcomeAsync(
        NpgsqlConnection connection,
        SeededRuntime runtime,
        long? committedSessionSequence)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                committedSessionSequence is null
                    ? """
                      INSERT INTO session_execution_outcomes (
                          organization_id, activity_id, participant_id, attempt_id, session_id,
                          agent_invocation_id, execution_outcome_id, outcome_category, reason_category)
                      VALUES (
                          @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                          @InvocationId, 'out-1', 'provider_unavailable', 'timeout');
                      """
                    : """
                      INSERT INTO session_execution_outcomes (
                          organization_id, activity_id, participant_id, attempt_id, session_id,
                          agent_invocation_id, execution_outcome_id, outcome_category, reason_category,
                          committed_session_version, committed_session_sequence)
                      VALUES (
                          @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                          @InvocationId, 'out-1', 'provider_unavailable', 'timeout',
                          1, @CommittedSessionSequence);
                      """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    runtime.InvocationId,
                    CommittedSessionSequence = committedSessionSequence,
                },
                cancellationToken: CancellationToken));
    }

    private async Task InsertValidationRevisionAsync(
        NpgsqlConnection connection,
        SeededRuntime runtime,
        int revisionOrdinal,
        string validationOutcome)
    {
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
                    @InvocationId, @RevisionOrdinal,
                    @AgainstVersion, @AgainstSequence,
                    @CommitVersion, @CommitSequence,
                    @ValidationOutcome, 'not_attempted', 'not_present');
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    runtime.InvocationId,
                    RevisionOrdinal = revisionOrdinal,
                    AgainstVersion = revisionOrdinal - 1,
                    AgainstSequence = revisionOrdinal,
                    CommitVersion = revisionOrdinal,
                    CommitSequence = revisionOrdinal + 1,
                    ValidationOutcome = validationOutcome,
                },
                cancellationToken: CancellationToken));
    }

    private async Task InsertOutputValidationAsync(
        NpgsqlConnection connection,
        SeededRuntime runtime,
        int itemOrdinal,
        string validationOutcome)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decision_output_validations (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, revision_ordinal, item_ordinal, local_ref, kind,
                    validation_outcome, rejection_reason_category, agent_output_id, effect_outcome)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 1, @ItemOrdinal, @LocalRef, @Kind,
                    @ValidationOutcome, @RejectionReason, @AgentOutputId, 'not_attempted');
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    runtime.InvocationId,
                    ItemOrdinal = itemOrdinal,
                    LocalRef = validationOutcome == "accepted" ? "out.message.primary" : "out.voice.primary",
                    Kind = validationOutcome == "accepted" ? "message" : "voice",
                    ValidationOutcome = validationOutcome,
                    RejectionReason = validationOutcome == "accepted" ? null : "capability_disabled",
                    AgentOutputId = validationOutcome == "accepted" ? "aout.roundtrip.0001" : null,
                },
                cancellationToken: CancellationToken));
    }

    private async Task InsertActionValidationAsync(
        NpgsqlConnection connection,
        SeededRuntime runtime,
        int itemOrdinal,
        string validationOutcome)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decision_requested_action_validations (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, revision_ordinal, item_ordinal, local_ref, kind,
                    validation_outcome, rejection_reason_category, effect_outcome)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 1, @ItemOrdinal, 'act.timer.replace', 'replace_timer',
                    @ValidationOutcome, @RejectionReason, 'not_attempted');
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    runtime.InvocationId,
                    ItemOrdinal = itemOrdinal,
                    ValidationOutcome = validationOutcome,
                    RejectionReason = validationOutcome == "accepted" ? null : "capability_disabled",
                },
                cancellationToken: CancellationToken));
    }

    private async Task InsertOutputEffectAsync(
        NpgsqlConnection connection,
        SeededRuntime runtime,
        int itemOrdinal,
        string effectOutcome)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decision_output_effects (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, revision_ordinal, item_ordinal, effect_outcome)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 1, @ItemOrdinal, @EffectOutcome);
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    runtime.InvocationId,
                    ItemOrdinal = itemOrdinal,
                    EffectOutcome = effectOutcome,
                },
                cancellationToken: CancellationToken));
    }

    private async Task InsertActionEffectAsync(
        NpgsqlConnection connection,
        SeededRuntime runtime,
        int itemOrdinal,
        string effectOutcome)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_decision_requested_action_effects (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, revision_ordinal, item_ordinal, effect_outcome)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 1, @ItemOrdinal, @EffectOutcome);
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    runtime.InvocationId,
                    ItemOrdinal = itemOrdinal,
                    EffectOutcome = effectOutcome,
                },
                cancellationToken: CancellationToken));
    }

    private async Task InsertAgentMessageAsync(
        NpgsqlConnection connection,
        SeededRuntime runtime,
        string messageId,
        string? acceptedAgentOutputId,
        string completionState = "open",
        string? assembledContentDigest = null)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_messages (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    message_id, author_type, turn_id, protected_ref, content_digest,
                    completion_state, generation_attempt_id, driving_invocation_id, driving_decision_id,
                    accepted_agent_output_id, assembled_content_digest, response_slot_id)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @MessageId, 'agent', 'turn.1', @ProtectedRef, @ContentDigest,
                    @CompletionState, 'agen.1', @InvocationId, @DecisionId,
                    @AcceptedAgentOutputId, @AssembledContentDigest, 'slot.1');
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    MessageId = messageId,
                    ProtectedRef = $"msg:{messageId}",
                    ContentDigest = ProtectedContentRef.DigestForReference($"msg:{messageId}"),
                    CompletionState = completionState,
                    runtime.InvocationId,
                    runtime.DecisionId,
                    AcceptedAgentOutputId = acceptedAgentOutputId,
                    AssembledContentDigest = assembledContentDigest,
                },
                cancellationToken: CancellationToken));
    }

    private async Task InsertAgentFragmentAsync(
        NpgsqlConnection connection,
        SeededRuntime runtime,
        string messageId,
        int fragmentOrdinal,
        long sessionSequence,
        string exactUtf8Text,
        string? acceptedAgentOutputId,
        string generationAttemptId = "agen.1",
        string turnId = "turn.1",
        string responseSlotId = "slot.1")
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_message_fragments (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    message_id, fragment_ordinal, session_sequence, turn_id, response_slot_id,
                    generation_attempt_id, protected_ref, content_digest, exact_utf8_text,
                    driving_invocation_id, driving_decision_id)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @MessageId, @FragmentOrdinal, @SessionSequence, @TurnId, @ResponseSlotId,
                    @GenerationAttemptId, @ProtectedRef, @ContentDigest, @ExactUtf8Text,
                    @InvocationId, @DecisionId);
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    MessageId = messageId,
                    FragmentOrdinal = fragmentOrdinal,
                    SessionSequence = sessionSequence,
                    TurnId = turnId,
                    ResponseSlotId = responseSlotId,
                    GenerationAttemptId = generationAttemptId,
                    ProtectedRef = $"frag:{messageId}:{fragmentOrdinal}",
                    ContentDigest = ProtectedContentRef.DigestUtf8(exactUtf8Text),
                    ExactUtf8Text = exactUtf8Text,
                    runtime.InvocationId,
                    runtime.DecisionId,
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
