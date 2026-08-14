using System.Text;
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

    private const string LoadTurnsSql = """
        SELECT turn_id, kind, state, trigger_invocation_id, response_slot_id,
               response_slot_state, claimed_by_invocation_id, created_session_sequence
        FROM session_turns
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY created_session_sequence, turn_id;
        """;

    private const string LoadTranscriptSql = """
        SELECT message_id, author_type, turn_id, protected_ref, content_digest
        FROM session_visible_transcript_items
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY committed_at, message_id;
        """;

    private const string LoadAttemptsSql = """
        SELECT agent_invocation_id, attempt_ordinal, outcome_category, agent_decision_id
        FROM session_invocation_attempts
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY agent_invocation_id, attempt_ordinal;
        """;

    private const string LoadDecisionsSql = """
        SELECT agent_invocation_id, decision_id, decision_type, produced_at,
               next_timer_expected_schedule_revision, next_timer_relative_delay,
               reason_category, communication_purpose, turn_id, response_slot_id,
               payload_digest, decision_payload_digest_version,
               envelope_schema_version, envelope_json,
               committed_session_version, committed_session_sequence
        FROM session_decisions
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId;
        """;

    private const string LoadOutcomesSql = """
        SELECT agent_invocation_id, execution_outcome_id, outcome_category, reason_category,
               committed_session_version, committed_session_sequence
        FROM session_execution_outcomes
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId;
        """;

    private const string LoadValidationsSql = """
        SELECT agent_invocation_id, revision_ordinal,
               validated_against_session_version, validated_against_session_sequence,
               validation_commit_session_version, validation_commit_session_sequence,
               validation_outcome, effect_outcome, timer_validation_outcome,
               rejection_reason_category, applied_turn_id, applied_response_slot_id,
               effect_commit_session_version, effect_commit_session_sequence
        FROM session_decision_validations
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY agent_invocation_id, revision_ordinal;
        """;

    private const string LoadOutputValidationsSql = """
        SELECT agent_invocation_id, revision_ordinal, item_ordinal, local_ref, kind,
               validation_outcome, rejection_reason_category, agent_output_id, effect_outcome
        FROM session_decision_output_validations
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY agent_invocation_id, revision_ordinal, item_ordinal;
        """;

    private const string LoadActionValidationsSql = """
        SELECT agent_invocation_id, revision_ordinal, item_ordinal, local_ref, kind,
               validation_outcome, rejection_reason_category, effect_outcome
        FROM session_decision_requested_action_validations
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY agent_invocation_id, revision_ordinal, item_ordinal;
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

    private const string UpsertTurnSql = """
        INSERT INTO session_turns (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            turn_id, kind, state, trigger_invocation_id, response_slot_id,
            response_slot_state, claimed_by_invocation_id, created_session_sequence)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @TurnId, @Kind, @State, @TriggerInvocationId, @ResponseSlotId,
            @ResponseSlotState, @ClaimedByInvocationId, @CreatedSessionSequence)
        ON CONFLICT (organization_id, session_id, turn_id) DO UPDATE
        SET
            state = EXCLUDED.state,
            response_slot_state = EXCLUDED.response_slot_state,
            claimed_by_invocation_id = EXCLUDED.claimed_by_invocation_id
        WHERE session_turns.state IS DISTINCT FROM EXCLUDED.state
           OR session_turns.response_slot_state IS DISTINCT FROM EXCLUDED.response_slot_state
           OR session_turns.claimed_by_invocation_id IS DISTINCT FROM EXCLUDED.claimed_by_invocation_id;
        """;

    private const string InsertTranscriptSql = """
        INSERT INTO session_visible_transcript_items (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            message_id, author_type, turn_id, protected_ref, content_digest)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @MessageId, @AuthorType, @TurnId, @ProtectedRef, @ContentDigest)
        ON CONFLICT (organization_id, session_id, message_id) DO NOTHING;
        """;

    private const string InsertAttemptSql = """
        INSERT INTO session_invocation_attempts (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, attempt_ordinal, outcome_category, agent_decision_id)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @AttemptOrdinal, @OutcomeCategory, @AgentDecisionId)
        ON CONFLICT (organization_id, session_id, agent_invocation_id, attempt_ordinal) DO NOTHING;
        """;

    private const string InsertDecisionSql = """
        INSERT INTO session_decisions (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, decision_id, decision_type, produced_at,
            next_timer_expected_schedule_revision, next_timer_relative_delay,
            reason_category, communication_purpose, turn_id, response_slot_id,
            payload_digest, decision_payload_digest_version,
            envelope_schema_version, envelope_json,
            committed_session_version, committed_session_sequence)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @DecisionId, @DecisionType, @ProducedAt,
            @NextTimerExpectedScheduleRevision, @NextTimerRelativeDelay,
            @ReasonCategory, @CommunicationPurpose, @TurnId, @ResponseSlotId,
            @PayloadDigest, @DecisionPayloadDigestVersion,
            @EnvelopeSchemaVersion, CAST(@EnvelopeJson AS jsonb),
            @CommittedSessionVersion, @CommittedSessionSequence)
        ON CONFLICT (
            organization_id, activity_id, participant_id, attempt_id, session_id, agent_invocation_id)
        DO NOTHING;
        """;

    private const string InsertOutcomeSql = """
        INSERT INTO session_execution_outcomes (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, execution_outcome_id, outcome_category, reason_category,
            committed_session_version, committed_session_sequence)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @ExecutionOutcomeId, @OutcomeCategory, @ReasonCategory,
            @CommittedSessionVersion, @CommittedSessionSequence)
        ON CONFLICT (organization_id, session_id, agent_invocation_id)
        DO NOTHING;
        """;

    private const string InsertValidationSql = """
        INSERT INTO session_decision_validations (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, revision_ordinal,
            validated_against_session_version, validated_against_session_sequence,
            validation_commit_session_version, validation_commit_session_sequence,
            validation_outcome, effect_outcome, timer_validation_outcome,
            rejection_reason_category)
        SELECT
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @RevisionOrdinal,
            @ValidatedAgainstSessionVersion, @ValidatedAgainstSessionSequence,
            @ValidationCommitSessionVersion, @ValidationCommitSessionSequence,
            @ValidationOutcome, 'not_attempted', @TimerValidationOutcome,
            @RejectionReasonCategory
        WHERE NOT EXISTS (
            SELECT 1
            FROM session_decision_validations
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND agent_invocation_id = @AgentInvocationId
              AND revision_ordinal = @RevisionOrdinal);
        """;

    private const string InsertOutputValidationSql = """
        INSERT INTO session_decision_output_validations (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, revision_ordinal, item_ordinal, local_ref, kind,
            validation_outcome, rejection_reason_category, agent_output_id, effect_outcome)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @RevisionOrdinal, @ItemOrdinal, @LocalRef, @Kind,
            @ValidationOutcome, @RejectionReasonCategory, @AgentOutputId, @EffectOutcome)
        ON CONFLICT (
            organization_id, session_id, agent_invocation_id, revision_ordinal, item_ordinal)
        DO NOTHING;
        """;

    private const string InsertActionValidationSql = """
        INSERT INTO session_decision_requested_action_validations (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, revision_ordinal, item_ordinal, local_ref, kind,
            validation_outcome, rejection_reason_category, effect_outcome)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @RevisionOrdinal, @ItemOrdinal, @LocalRef, @Kind,
            @ValidationOutcome, @RejectionReasonCategory, @EffectOutcome)
        ON CONFLICT (
            organization_id, session_id, agent_invocation_id, revision_ordinal, item_ordinal)
        DO NOTHING;
        """;

    private const string UpdateValidationEffectSql = """
        UPDATE session_decision_validations
        SET
            effect_outcome = @EffectOutcome,
            applied_turn_id = @AppliedTurnId,
            applied_response_slot_id = @AppliedResponseSlotId,
            effect_commit_session_version = @EffectCommitSessionVersion,
            effect_commit_session_sequence = @EffectCommitSessionSequence
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
          AND agent_invocation_id = @AgentInvocationId
          AND revision_ordinal = @RevisionOrdinal
          AND effect_outcome = 'not_attempted';
        """;

    private const string UpdateInvocationSql = """
        UPDATE session_invocations
        SET
            status = @Status,
            agent_decision_id = @AgentDecisionId,
            execution_outcome_id = @ExecutionOutcomeId
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
          AND agent_invocation_id = @AgentInvocationId;
        """;

    private const string CountInvocationsSql = """
        SELECT COUNT(*)::int
        FROM session_invocations
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId;
        """;

    private const string ListInvocationIdsSql = """
        SELECT agent_invocation_id
        FROM session_invocations
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY admitted_session_sequence;
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
        var commandArgs = OwnershipParameters(ownership);
        var row = await connection.QuerySingleOrDefaultAsync<SessionRuntimeRow>(
            new CommandDefinition(LoadForUpdateSql, commandArgs, transaction, cancellationToken: cancellationToken));
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
            new CommandDefinition(LoadInvocationsSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var turnRows = (await connection.QueryAsync<SessionTurnRow>(
            new CommandDefinition(LoadTurnsSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var transcriptRows = (await connection.QueryAsync<SessionTranscriptRow>(
            new CommandDefinition(LoadTranscriptSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var attemptRows = (await connection.QueryAsync<SessionAttemptRow>(
            new CommandDefinition(LoadAttemptsSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var decisionRows = (await connection.QueryAsync<SessionDecisionRow>(
            new CommandDefinition(LoadDecisionsSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var outcomeRows = (await connection.QueryAsync<SessionOutcomeRow>(
            new CommandDefinition(LoadOutcomesSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var validationRows = (await connection.QueryAsync<SessionValidationRow>(
            new CommandDefinition(LoadValidationsSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var outputValidationRows = (await connection.QueryAsync<SessionOutputValidationRow>(
            new CommandDefinition(LoadOutputValidationsSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var actionValidationRows = (await connection.QueryAsync<SessionActionValidationRow>(
            new CommandDefinition(LoadActionValidationsSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();

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
                item.status,
                ToDecision(decisionRows.SingleOrDefault(decision =>
                    string.Equals(decision.agent_invocation_id, item.agent_invocation_id, StringComparison.Ordinal))),
                ToOutcome(outcomeRows.SingleOrDefault(outcome =>
                    string.Equals(outcome.agent_invocation_id, item.agent_invocation_id, StringComparison.Ordinal))),
                attemptRows
                    .Where(attempt => string.Equals(attempt.agent_invocation_id, item.agent_invocation_id, StringComparison.Ordinal))
                    .Select(attempt => new InvocationExecutionAttempt(
                        attempt.attempt_ordinal,
                        attempt.outcome_category,
                        attempt.agent_decision_id))
                    .ToArray(),
                validationRows
                    .Where(validation => string.Equals(validation.agent_invocation_id, item.agent_invocation_id, StringComparison.Ordinal))
                    .Select(validation => ToValidation(
                        validation,
                        outputValidationRows
                            .Where(output =>
                                string.Equals(output.agent_invocation_id, item.agent_invocation_id, StringComparison.Ordinal)
                                && output.revision_ordinal == validation.revision_ordinal)
                            .ToArray(),
                        actionValidationRows
                            .Where(action =>
                                string.Equals(action.agent_invocation_id, item.agent_invocation_id, StringComparison.Ordinal)
                                && action.revision_ordinal == validation.revision_ordinal)
                            .ToArray()))
                    .ToArray()))
            .ToArray();

        var turns = turnRows
            .Select(item => Turn.Rehydrate(
                item.turn_id,
                item.kind,
                item.state,
                item.trigger_invocation_id,
                ResponseSlot.Rehydrate(item.response_slot_id, item.response_slot_state, item.claimed_by_invocation_id),
                item.created_session_sequence))
            .ToArray();

        var transcript = transcriptRows
            .Select(item => new VisibleTranscriptItemRef(
                item.message_id,
                item.author_type,
                item.turn_id,
                new ProtectedContentRef(item.protected_ref, item.content_digest)))
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
            turns,
            transcript,
            lastAdmittedAtByFamily);
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

        await PersistTurnsAndTranscriptAsync(ownership, session, transaction, cancellationToken);
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

    public async Task<bool> TrySaveCompletionAsync(
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

        await PersistTurnsAndTranscriptAsync(ownership, session, transaction, cancellationToken);
        foreach (var attempt in invocation.Attempts)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertAttemptSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        invocation.AgentInvocationId,
                        AttemptOrdinal = attempt.AttemptOrdinal,
                        OutcomeCategory = attempt.OutcomeCategory,
                        AgentDecisionId = attempt.AgentDecisionId,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        if (invocation.Decision is not null)
        {
            var recommendation = invocation.Decision.Recommendation;
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertDecisionSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        invocation.AgentInvocationId,
                        invocation.Decision.DecisionId,
                        invocation.Decision.DecisionType,
                        ProducedAt = invocation.Decision.ProducedAt,
                        NextTimerExpectedScheduleRevision = recommendation.NextTimer?.ExpectedScheduleRevision,
                        NextTimerRelativeDelay = recommendation.NextTimer?.RelativeDelay,
                        ReasonCategory = recommendation switch
                        {
                            NoActionRecommendation noAction => noAction.ReasonCategory,
                            EnvelopeRecommendation envelope => envelope.NoActionReasonCategory,
                            _ => null,
                        },
                        CommunicationPurpose = recommendation switch
                        {
                            EmitMessageRecommendation emit => emit.CommunicationPurpose,
                            EnvelopeRecommendation envelope => envelope.Outputs
                                .FirstOrDefault(output =>
                                    string.Equals(output.Kind, AgentOutputKinds.Message, StringComparison.Ordinal))
                                ?.CommunicationPurpose,
                            _ => null,
                        },
                        TurnId = recommendation switch
                        {
                            EmitMessageRecommendation emitTurn => emitTurn.TurnId,
                            EnvelopeRecommendation envelopeTurn => envelopeTurn.Outputs
                                .FirstOrDefault(output =>
                                    string.Equals(output.Kind, AgentOutputKinds.Message, StringComparison.Ordinal))
                                ?.TurnId,
                            _ => null,
                        },
                        ResponseSlotId = recommendation switch
                        {
                            EmitMessageRecommendation emitSlot => emitSlot.ResponseSlotId,
                            EnvelopeRecommendation envelopeSlot => envelopeSlot.Outputs
                                .FirstOrDefault(output =>
                                    string.Equals(output.Kind, AgentOutputKinds.Message, StringComparison.Ordinal))
                                ?.ResponseSlotId,
                            _ => null,
                        },
                        PayloadDigest = invocation.Decision.PayloadDigest,
                        DecisionPayloadDigestVersion = DecisionPayloadDigest.FormatVersionV1,
                        EnvelopeSchemaVersion = recommendation is EnvelopeRecommendation ? "v2" : "v1",
                        EnvelopeJson = recommendation is EnvelopeRecommendation envelopeJson
                            ? Encoding.UTF8.GetString(AgentDecisionEnvelopeSerializer.ToUtf8Json(envelopeJson))
                            : null,
                        CommittedSessionVersion = invocation.Decision.CommittedSessionVersion,
                        CommittedSessionSequence = invocation.Decision.CommittedSessionSequence,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        if (invocation.ExecutionOutcome is not null)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertOutcomeSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        invocation.AgentInvocationId,
                        invocation.ExecutionOutcome.ExecutionOutcomeId,
                        OutcomeCategory = invocation.ExecutionOutcome.OutcomeCategory,
                        ReasonCategory = invocation.ExecutionOutcome.ReasonCategory,
                        CommittedSessionVersion = invocation.ExecutionOutcome.CommittedSessionVersion,
                        CommittedSessionSequence = invocation.ExecutionOutcome.CommittedSessionSequence,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        foreach (var validation in invocation.ValidationHistory)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertValidationSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        invocation.AgentInvocationId,
                        RevisionOrdinal = validation.RevisionOrdinal,
                        ValidatedAgainstSessionVersion = Math.Max(0, validation.ValidatedAtSessionVersion - 1),
                        ValidatedAgainstSessionSequence = validation.ValidatedAtSessionSequence,
                        ValidationCommitSessionVersion = validation.ValidatedAtSessionVersion,
                        ValidationCommitSessionSequence = validation.ValidatedAtSessionSequence,
                        ValidationOutcome = validation.ValidationOutcome,
                        TimerValidationOutcome = validation.TimerValidationOutcome,
                        RejectionReasonCategory = validation.RejectionReasonCategory,
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (validation.EffectOutcome is DecisionEffectOutcomes.Applied
                or DecisionEffectOutcomes.NoDomainEffect
                or DecisionEffectOutcomes.EffectFailed)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        UpdateValidationEffectSql,
                        new
                        {
                            ownership.OrganizationId,
                            ownership.ActivityId,
                            ownership.ParticipantId,
                            ownership.AttemptId,
                            ownership.SessionId,
                            invocation.AgentInvocationId,
                            RevisionOrdinal = validation.RevisionOrdinal,
                            EffectOutcome = validation.EffectOutcome,
                            AppliedTurnId = validation.AppliedTurnId,
                            AppliedResponseSlotId = validation.AppliedResponseSlotId,
                            EffectCommitSessionVersion = validation.EffectCommitSessionVersion,
                            EffectCommitSessionSequence = validation.EffectCommitSessionSequence,
                        },
                        transaction,
                        cancellationToken: cancellationToken));
            }

            for (var index = 0; index < validation.OutputValidations.Count; index++)
            {
                var item = validation.OutputValidations[index];
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        InsertOutputValidationSql,
                        new
                        {
                            ownership.OrganizationId,
                            ownership.ActivityId,
                            ownership.ParticipantId,
                            ownership.AttemptId,
                            ownership.SessionId,
                            invocation.AgentInvocationId,
                            RevisionOrdinal = validation.RevisionOrdinal,
                            ItemOrdinal = index,
                            item.LocalRef,
                            item.Kind,
                            item.ValidationOutcome,
                            item.RejectionReasonCategory,
                            item.AgentOutputId,
                            item.EffectOutcome,
                        },
                        transaction,
                        cancellationToken: cancellationToken));
            }

            for (var index = 0; index < validation.RequestedActionValidations.Count; index++)
            {
                var item = validation.RequestedActionValidations[index];
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        InsertActionValidationSql,
                        new
                        {
                            ownership.OrganizationId,
                            ownership.ActivityId,
                            ownership.ParticipantId,
                            ownership.AttemptId,
                            ownership.SessionId,
                            invocation.AgentInvocationId,
                            RevisionOrdinal = validation.RevisionOrdinal,
                            ItemOrdinal = index,
                            item.LocalRef,
                            item.Kind,
                            item.ValidationOutcome,
                            item.RejectionReasonCategory,
                            item.EffectOutcome,
                        },
                        transaction,
                        cancellationToken: cancellationToken));
            }
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateInvocationSql,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    invocation.AgentInvocationId,
                    invocation.Status,
                    invocation.AgentDecisionId,
                    invocation.ExecutionOutcomeId,
                },
                transaction,
                cancellationToken: cancellationToken));
        return true;
    }

    public async Task<int> CountInvocationsAsync(
        SessionOwnership ownership,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(transaction);
        return await RequireConnection(transaction).ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountInvocationsSql,
                OwnershipParameters(ownership),
                transaction,
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<string>> ListInvocationIdsAsync(
        SessionOwnership ownership,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(transaction);
        var ids = await RequireConnection(transaction).QueryAsync<string>(
            new CommandDefinition(
                ListInvocationIdsSql,
                OwnershipParameters(ownership),
                transaction,
                cancellationToken: cancellationToken));
        return ids.AsList();
    }

    private async Task PersistTurnsAndTranscriptAsync(
        SessionOwnership ownership,
        SessionRuntime session,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = RequireConnection(transaction);
        foreach (var turn in session.Turns.Where(item => item.IsDirty))
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    UpsertTurnSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        turn.TurnId,
                        turn.Kind,
                        turn.State,
                        turn.TriggerInvocationId,
                        turn.ResponseSlot.ResponseSlotId,
                        ResponseSlotState = turn.ResponseSlot.State,
                        turn.ResponseSlot.ClaimedByInvocationId,
                        turn.CreatedSessionSequence,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            turn.MarkClean();
        }

        foreach (var item in session.VisibleTranscript)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertTranscriptSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        item.MessageId,
                        item.AuthorType,
                        item.TurnId,
                        item.ContentRef.ProtectedRef,
                        item.ContentRef.ContentDigest,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }
    }

    private static AgentDecisionRecord? ToDecision(SessionDecisionRow? row)
    {
        if (row is null)
        {
            return null;
        }

        DecisionRecommendation recommendation;
        if (string.Equals(row.envelope_schema_version, "v2", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(row.envelope_json))
        {
            var parsed = AgentDecisionEnvelopeParser.Parse(Encoding.UTF8.GetBytes(row.envelope_json));
            if (!parsed.Succeeded || parsed.Envelope is null)
            {
                throw new InvalidOperationException("Stored v2 Decision envelope could not be reconstructed.");
            }

            recommendation = parsed.Envelope;
        }
        else
        {
            NextTimerRecommendation? nextTimer = row.next_timer_relative_delay is null
                ? null
                : new NextTimerRecommendation(
                    row.next_timer_relative_delay,
                    row.next_timer_expected_schedule_revision ?? string.Empty);
            recommendation = row.decision_type switch
            {
                RuntimeDecisionTypes.NoAction => new NoActionRecommendation(
                    row.decision_id,
                    row.agent_invocation_id,
                    ToUtc(row.produced_at),
                    row.reason_category ?? NoActionReasonCategories.IntentionalSilence,
                    nextTimer),
                RuntimeDecisionTypes.EmitMessage => new EmitMessageRecommendation(
                    row.decision_id,
                    row.agent_invocation_id,
                    ToUtc(row.produced_at),
                    row.communication_purpose ?? string.Empty,
                    row.turn_id,
                    row.response_slot_id,
                    nextTimer),
                _ => new ProhibitedDecisionRecommendation(
                    row.decision_id,
                    row.agent_invocation_id,
                    ToUtc(row.produced_at),
                    row.decision_type,
                    nextTimer),
            };
        }

        var decision = new AgentDecisionRecord(recommendation, row.payload_digest);
        decision.BindCommitState(row.committed_session_version, row.committed_session_sequence);
        return decision;
    }

    private static ExecutionOutcomeRecord? ToOutcome(SessionOutcomeRow? row)
    {
        if (row is null)
        {
            return null;
        }

        var outcome = new ExecutionOutcomeRecord(row.execution_outcome_id, row.outcome_category, row.reason_category);
        outcome.BindCommitState(row.committed_session_version, row.committed_session_sequence);
        return outcome;
    }

    private static DecisionValidationEffectRecord ToValidation(
        SessionValidationRow row,
        IReadOnlyList<SessionOutputValidationRow> outputRows,
        IReadOnlyList<SessionActionValidationRow> actionRows)
    {
        var record = new DecisionValidationEffectRecord(
            row.validation_outcome,
            DecisionEffectOutcomes.NotAttempted,
            row.timer_validation_outcome,
            row.rejection_reason_category,
            outputRows
                .Select(item => new OutputItemValidation(
                    item.local_ref,
                    item.kind,
                    item.validation_outcome,
                    item.rejection_reason_category,
                    item.agent_output_id,
                    item.effect_outcome))
                .ToArray(),
            actionRows
                .Select(item => new RequestedActionItemValidation(
                    item.local_ref,
                    item.kind,
                    item.validation_outcome,
                    item.rejection_reason_category,
                    item.effect_outcome))
                .ToArray());
        record.BindAuthoritativeState(
            row.revision_ordinal,
            row.validation_commit_session_version,
            row.validation_commit_session_sequence);
        if (row.effect_outcome is DecisionEffectOutcomes.Applied
            or DecisionEffectOutcomes.NoDomainEffect
            or DecisionEffectOutcomes.EffectFailed)
        {
            record.SetEffectOutcome(row.effect_outcome, row.applied_turn_id, row.applied_response_slot_id);
            if (row.effect_commit_session_version is not null && row.effect_commit_session_sequence is not null)
            {
                record.BindEffectCommitState(
                    row.effect_commit_session_version.Value,
                    row.effect_commit_session_sequence.Value);
            }
        }

        return record;
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

    private sealed record SessionTurnRow(
        string turn_id,
        string kind,
        string state,
        string? trigger_invocation_id,
        string response_slot_id,
        string response_slot_state,
        string? claimed_by_invocation_id,
        long created_session_sequence);

    private sealed record SessionTranscriptRow(
        string message_id,
        string author_type,
        string? turn_id,
        string protected_ref,
        string content_digest);

    private sealed record SessionAttemptRow(
        string agent_invocation_id,
        int attempt_ordinal,
        string outcome_category,
        string? agent_decision_id);

    private sealed record SessionDecisionRow(
        string agent_invocation_id,
        string decision_id,
        string decision_type,
        DateTime produced_at,
        string? next_timer_expected_schedule_revision,
        string? next_timer_relative_delay,
        string? reason_category,
        string? communication_purpose,
        string? turn_id,
        string? response_slot_id,
        string payload_digest,
        string decision_payload_digest_version,
        string envelope_schema_version,
        string? envelope_json,
        long committed_session_version,
        long committed_session_sequence);

    private sealed record SessionOutcomeRow(
        string agent_invocation_id,
        string execution_outcome_id,
        string outcome_category,
        string reason_category,
        long committed_session_version,
        long committed_session_sequence);

    private sealed record SessionValidationRow(
        string agent_invocation_id,
        int revision_ordinal,
        long validated_against_session_version,
        long validated_against_session_sequence,
        long validation_commit_session_version,
        long validation_commit_session_sequence,
        string validation_outcome,
        string effect_outcome,
        string timer_validation_outcome,
        string? rejection_reason_category,
        string? applied_turn_id,
        string? applied_response_slot_id,
        long? effect_commit_session_version,
        long? effect_commit_session_sequence);

    private sealed record SessionOutputValidationRow(
        string agent_invocation_id,
        int revision_ordinal,
        int item_ordinal,
        string local_ref,
        string kind,
        string validation_outcome,
        string? rejection_reason_category,
        string? agent_output_id,
        string effect_outcome);

    private sealed record SessionActionValidationRow(
        string agent_invocation_id,
        int revision_ordinal,
        int item_ordinal,
        string local_ref,
        string kind,
        string validation_outcome,
        string? rejection_reason_category,
        string effect_outcome);
}
