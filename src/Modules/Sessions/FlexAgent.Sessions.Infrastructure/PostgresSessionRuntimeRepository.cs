using System.Text;
using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using Npgsql;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresSessionRuntimeRepository
{
    internal static Func<NpgsqlTransaction, Task>? AfterHeadLoadedAsync { get; set; }
    internal static int FragmentInsertAttempts { get; set; }

    internal static int PublicationMessagesTouched { get; set; }

    internal static int TranscriptInsertAttempts { get; set; }

    internal static int TurnUpsertAttempts { get; set; }
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

    private const string LoadAgentMessagesSql = """
        SELECT message_id, generation_attempt_id, driving_invocation_id, driving_decision_id,
               turn_id, response_slot_id, completion_state, assembled_content_digest,
               accepted_agent_output_id, sealed_session_sequence, sealed_at
        FROM session_messages
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
          AND author_type = 'agent'
        ORDER BY committed_at, message_id;
        """;

    private const string LoadAgentFragmentsSql = """
        SELECT message_id, fragment_ordinal, session_sequence, exact_utf8_text, content_digest, committed_at
        FROM session_message_fragments
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY message_id, fragment_ordinal;
        """;

    private const string LoadTimerSchedulesSql = """
        SELECT
            schedule_revision,
            schedule_revision_ordinal,
            state,
            lane_state,
            relative_delay,
            remaining_active_seconds,
            remaining_since,
            fire_at,
            requested_by_category,
            source_decision_id,
            fired_invocation_id,
            created_at
        FROM session_timer_schedules
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY schedule_revision_ordinal, schedule_revision;
        """;

    private const string LoadManifestRuntimeSql = """
        SELECT
            manifest_sequence,
            record_type,
            service_actor,
            occurred_at,
            protected_ref,
            content_digest,
            session_sequence
        FROM session_manifest_runtime_records
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY manifest_sequence;
        """;

    private const string LoadTerminalRecordSql = """
        SELECT
            terminal_record_id,
            lifecycle_state,
            reason_category,
            attempt_mapping,
            cutoff_sequence,
            procedure_id,
            seal_digest
        FROM session_terminal_records
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId;
        """;

    private const string LoadEvaluationHandoffSql = """
        SELECT
            handoff_id,
            terminal_record_id,
            procedure_id,
            eligibility,
            terminal_state,
            cutoff_sequence,
            configuration_id,
            configuration_digest,
            manifest_id,
            seal_digest
        FROM session_evaluation_handoffs
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId;
        """;

    private const string InsertManifestRuntimeSql = """
        INSERT INTO session_manifest_runtime_records (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            manifest_sequence, record_type, service_actor, occurred_at,
            protected_ref, content_digest, session_sequence)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @ManifestSequence, @RecordType, @ServiceActor, @OccurredAt,
            @ProtectedRef, @ContentDigest, @SessionSequence)
        ON CONFLICT (organization_id, session_id, record_type, protected_ref) DO NOTHING;
        """;

    private const string InsertManifestRefSql = """
        INSERT INTO session_manifest_refs (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            ref_kind, protected_ref, content_digest)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @RefKind, @ProtectedRef, @ContentDigest)
        ON CONFLICT (organization_id, session_id, ref_kind, protected_ref) DO NOTHING;
        """;

    private const string InsertFrozenPolicySnapshotSql = """
        INSERT INTO session_frozen_policy_snapshots (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            configuration_id, configuration_digest, manifest_id, policy_digest, policy_payload)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @ConfigurationId, @ConfigurationDigest, @ManifestId, @PolicyDigest,
            CAST(@PolicyPayload AS jsonb));
        """;

    private const string InsertStartingParticipantRelationshipSql = """
        INSERT INTO session_actor_relationships (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            actor_id, actor_type, relationship, relationship_version)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @ActorId, @ActorType, @Relationship, 1);
        """;

    private const string InsertTerminalRecordSql = """
        INSERT INTO session_terminal_records (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            terminal_record_id, lifecycle_state, reason_category, attempt_mapping,
            cutoff_sequence, procedure_id, seal_digest)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @TerminalRecordId, @LifecycleState, @ReasonCategory, @AttemptMapping,
            @CutoffSequence, @ProcedureId, @SealDigest)
        ON CONFLICT (organization_id, session_id) DO NOTHING;
        """;

    private const string InsertEvaluationHandoffSql = """
        INSERT INTO session_evaluation_handoffs (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            handoff_id, terminal_record_id, procedure_id, eligibility, terminal_state,
            cutoff_sequence, configuration_id, configuration_digest, manifest_id, seal_digest)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @HandoffId, @TerminalRecordId, @ProcedureId, @Eligibility, @TerminalState,
            @CutoffSequence, @ConfigurationId, @ConfigurationDigest, @ManifestId, @SealDigest)
        ON CONFLICT (organization_id, session_id) DO NOTHING;
        """;

    private const string InsertTimerScheduleSql = """
        INSERT INTO session_timer_schedules (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            schedule_revision, schedule_revision_ordinal, state, lane_state, relative_delay,
            remaining_active_seconds, remaining_since, fire_at, requested_by_category,
            source_decision_id, fired_invocation_id, created_at, timer_lane_delegation_id)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @ScheduleRevisionId, @ScheduleRevisionOrdinal, @State, @LaneState, @RelativeDelay,
            @RemainingActiveSeconds, @RemainingSince, @FireAt, @RequestedByCategory,
            @SourceDecisionId, @FiredInvocationId, @CreatedAt, @TimerLaneDelegationId);
        """;

    private const string UpdateTimerScheduleSql = """
        UPDATE session_timer_schedules
        SET
            state = @State,
            lane_state = @LaneState,
            remaining_active_seconds = @RemainingActiveSeconds,
            remaining_since = @RemainingSince,
            fire_at = @FireAt,
            fired_invocation_id = @FiredInvocationId
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
          AND schedule_revision = @ScheduleRevisionId;
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

    private const string LoadOutputEffectsSql = """
        SELECT agent_invocation_id, revision_ordinal, item_ordinal, effect_outcome
        FROM session_decision_output_effects
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
        ORDER BY agent_invocation_id, revision_ordinal, item_ordinal;
        """;

    private const string LoadActionEffectsSql = """
        SELECT agent_invocation_id, revision_ordinal, item_ordinal, effect_outcome
        FROM session_decision_requested_action_effects
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

    private const string InsertAgentMessageSql = """
        INSERT INTO session_messages (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            message_id, author_type, turn_id, protected_ref, content_digest, completion_state,
            generation_attempt_id, driving_invocation_id, driving_decision_id,
            accepted_agent_output_id, assembled_content_digest, response_slot_id,
            sealed_session_sequence, sealed_at)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @MessageId, 'agent', @TurnId, @ProtectedRef, @ContentDigest, @CompletionState,
            @GenerationAttemptId, @DrivingInvocationId, @DrivingDecisionId,
            @AcceptedAgentOutputId, @AssembledContentDigest, @ResponseSlotId,
            @SealedSessionSequence, @SealedAt)
        ON CONFLICT (organization_id, session_id, message_id) DO NOTHING;
        """;

    private const string UpdateAgentMessageSealSql = """
        UPDATE session_messages
        SET
            completion_state = @CompletionState,
            assembled_content_digest = @AssembledContentDigest,
            sealed_session_sequence = @SealedSessionSequence,
            sealed_at = @SealedAt
        WHERE organization_id = @OrganizationId
          AND activity_id = @ActivityId
          AND participant_id = @ParticipantId
          AND attempt_id = @AttemptId
          AND session_id = @SessionId
          AND message_id = @MessageId
          AND (
              completion_state IS DISTINCT FROM @CompletionState
              OR assembled_content_digest IS DISTINCT FROM @AssembledContentDigest
              OR sealed_session_sequence IS DISTINCT FROM @SealedSessionSequence
              OR sealed_at IS DISTINCT FROM @SealedAt);
        """;

    private const string InsertAgentFragmentSql = """
        INSERT INTO session_message_fragments (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            message_id, fragment_ordinal, session_sequence, turn_id, response_slot_id,
            generation_attempt_id, protected_ref, content_digest, exact_utf8_text,
            driving_invocation_id, driving_decision_id)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @MessageId, @FragmentOrdinal, @SessionSequence, @TurnId, @ResponseSlotId,
            @GenerationAttemptId, @ProtectedRef, @ContentDigest, @ExactUtf8Text,
            @DrivingInvocationId, @DrivingDecisionId)
        ON CONFLICT (organization_id, session_id, message_id, fragment_ordinal) DO NOTHING;
        """;

    private const string LoadFragmentDigestSql = """
        SELECT content_digest
        FROM session_message_fragments
        WHERE organization_id = @OrganizationId
          AND session_id = @SessionId
          AND message_id = @MessageId
          AND fragment_ordinal = @FragmentOrdinal;
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

    private const string InsertDurableWorkSql = """
        INSERT INTO session_durable_work (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            work_id, work_type, business_key, state)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @WorkId, @WorkType, @BusinessKey, @State)
        ON CONFLICT (organization_id, session_id, work_type, business_key) DO NOTHING;
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
        SELECT
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @DecisionId, @DecisionType, @ProducedAt,
            @NextTimerExpectedScheduleRevision, @NextTimerRelativeDelay,
            @ReasonCategory, @CommunicationPurpose, @TurnId, @ResponseSlotId,
            @PayloadDigest, @DecisionPayloadDigestVersion,
            @EnvelopeSchemaVersion, CAST(@EnvelopeJson AS jsonb),
            @CommittedSessionVersion, @CommittedSessionSequence
        WHERE NOT EXISTS (
            SELECT 1
            FROM session_decisions
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND agent_invocation_id = @AgentInvocationId);
        """;

    private const string InsertOutcomeSql = """
        INSERT INTO session_execution_outcomes (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, execution_outcome_id, outcome_category, reason_category,
            committed_session_version, committed_session_sequence)
        SELECT
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @ExecutionOutcomeId, @OutcomeCategory, @ReasonCategory,
            @CommittedSessionVersion, @CommittedSessionSequence
        WHERE NOT EXISTS (
            SELECT 1
            FROM session_execution_outcomes
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND agent_invocation_id = @AgentInvocationId);
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

    private const string InsertOutputEffectSql = """
        INSERT INTO session_decision_output_effects (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, revision_ordinal, item_ordinal, effect_outcome)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @RevisionOrdinal, @ItemOrdinal, @EffectOutcome)
        ON CONFLICT (
            organization_id, session_id, agent_invocation_id, revision_ordinal, item_ordinal)
        DO NOTHING;
        """;

    private const string InsertActionEffectSql = """
        INSERT INTO session_decision_requested_action_effects (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, revision_ordinal, item_ordinal, effect_outcome)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @RevisionOrdinal, @ItemOrdinal, @EffectOutcome)
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

    private const string LoadTimerLaneDelegationIdSql = """
        SELECT delegation_id
        FROM service_delegations
        WHERE organization_id = @OrganizationId
          AND session_id = @SessionId
          AND allowed_action = @AllowedAction
          AND revoked_at IS NULL;
        """;

    public async Task InsertActiveAsync(
        SessionOwnership ownership,
        SessionRuntime session,
        TrustedRuntimeActor participantActor,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default,
        ServiceDelegationIssue? timerLaneDelegation = null)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(participantActor);
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureOwnership(ownership, session.Ownership);
        if (participantActor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(participantActor.ActorType))
        {
            throw new ArgumentOutOfRangeException(nameof(participantActor));
        }

        var lastCommittedAt = ToUtc(await RequireConnection(transaction).ExecuteScalarAsync<DateTime>(
            new CommandDefinition(
                InsertActiveSql,
                HeadParameters(ownership, session, expectedSessionVersion: null),
                transaction,
                cancellationToken: cancellationToken)));

        session.ReplaceLastCommittedAtFromDatabase(lastCommittedAt);
        if (timerLaneDelegation is not null)
        {
            await PostgresServiceDelegationRepository.InsertInTransactionAsync(
                new SessionScopedDelegationTarget(
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId),
                timerLaneDelegation,
                transaction,
                cancellationToken);
        }

        await PersistTimerSchedulesAsync(ownership, session, transaction, cancellationToken);
        await PersistBindingManifestRefsAsync(ownership, session, transaction, cancellationToken);
        await PersistFrozenPolicySnapshotAsync(ownership, session, transaction, cancellationToken);
        await PersistStartingParticipantRelationshipAsync(
            ownership,
            participantActor,
            transaction,
            cancellationToken);
        await PersistManifestAndTerminalAsync(ownership, session, transaction, cancellationToken);
    }

    public Task<SessionRuntime?> LoadForUpdateAsync(
        SessionOwnership ownership,
        TrustedSessionBinding binding,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default) =>
        LoadAsync(ownership, binding, transaction, forUpdate: true, cancellationToken);

    public Task<SessionRuntime?> LoadSnapshotAsync(
        SessionOwnership ownership,
        TrustedSessionBinding binding,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default) =>
        LoadAsync(ownership, binding, transaction, forUpdate: false, cancellationToken);

    private async Task<SessionRuntime?> LoadAsync(
        SessionOwnership ownership,
        TrustedSessionBinding binding,
        NpgsqlTransaction transaction,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureOwnership(ownership, binding.Ownership);

        var connection = RequireConnection(transaction);
        var commandArgs = OwnershipParameters(ownership);
        var row = await connection.QuerySingleOrDefaultAsync<SessionRuntimeRow>(
            new CommandDefinition(
                forUpdate ? LoadForUpdateSql + " FOR UPDATE" : LoadForUpdateSql,
                commandArgs,
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

        if (!forUpdate && AfterHeadLoadedAsync is not null)
        {
            await AfterHeadLoadedAsync(transaction);
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
        var outputEffectRows = (await connection.QueryAsync<SessionItemEffectRow>(
            new CommandDefinition(LoadOutputEffectsSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var actionEffectRows = (await connection.QueryAsync<SessionItemEffectRow>(
            new CommandDefinition(LoadActionEffectsSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var agentMessageRows = (await connection.QueryAsync<SessionAgentMessageRow>(
            new CommandDefinition(LoadAgentMessagesSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var agentFragmentRows = (await connection.QueryAsync<SessionAgentFragmentRow>(
            new CommandDefinition(LoadAgentFragmentsSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var timerRows = (await connection.QueryAsync<SessionTimerScheduleRow>(
            new CommandDefinition(LoadTimerSchedulesSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var manifestRows = (await connection.QueryAsync<SessionManifestRuntimeRow>(
            new CommandDefinition(LoadManifestRuntimeSql, commandArgs, transaction, cancellationToken: cancellationToken))).AsList();
        var terminalRow = await connection.QuerySingleOrDefaultAsync<SessionTerminalRow>(
            new CommandDefinition(LoadTerminalRecordSql, commandArgs, transaction, cancellationToken: cancellationToken));
        var handoffRow = await connection.QuerySingleOrDefaultAsync<SessionEvaluationHandoffRow>(
            new CommandDefinition(LoadEvaluationHandoffSql, commandArgs, transaction, cancellationToken: cancellationToken));

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
                            .ToArray(),
                        outputEffectRows
                            .Where(effect =>
                                string.Equals(effect.agent_invocation_id, item.agent_invocation_id, StringComparison.Ordinal)
                                && effect.revision_ordinal == validation.revision_ordinal)
                            .ToArray(),
                        actionEffectRows
                            .Where(effect =>
                                string.Equals(effect.agent_invocation_id, item.agent_invocation_id, StringComparison.Ordinal)
                                && effect.revision_ordinal == validation.revision_ordinal)
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

        var agentMessages = agentMessageRows
            .Select(item =>
            {
                var fragments = agentFragmentRows
                    .Where(fragment => string.Equals(fragment.message_id, item.message_id, StringComparison.Ordinal))
                    .Select(fragment => new AgentResponseFragment(
                        fragment.fragment_ordinal,
                        fragment.session_sequence,
                        fragment.exact_utf8_text,
                        fragment.content_digest,
                        ToUtc(fragment.committed_at)))
                    .ToArray();
                return AgentResponseMessage.Rehydrate(
                    item.message_id,
                    item.generation_attempt_id,
                    item.driving_invocation_id,
                    item.driving_decision_id,
                    item.turn_id,
                    item.response_slot_id,
                    item.completion_state,
                    item.assembled_content_digest,
                    fragments,
                    item.accepted_agent_output_id,
                    item.sealed_session_sequence,
                    item.sealed_at is null ? null : ToUtc(item.sealed_at.Value));
            })
            .ToArray();

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
            lastAdmittedAtByFamily,
            agentMessages,
            timerRows.Select(ToTimerSchedule).ToArray(),
            manifestRows.Select(ToManifestRecord).ToArray(),
            terminalRow is null ? null : ToTerminalRecord(terminalRow),
            handoffRow is null ? null : ToEvaluationHandoff(handoffRow));
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
        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertDurableWorkSql,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    WorkId = Guid.NewGuid(),
                    WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                    BusinessKey = invocation.AgentInvocationId,
                    State = DurableSessionWorkStates.Pending,
                },
                transaction,
                cancellationToken: cancellationToken));
        await PersistTimerSchedulesAsync(ownership, session, transaction, cancellationToken);
        await PersistManifestAndTerminalAsync(ownership, session, transaction, cancellationToken);
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
        await PersistTimerSchedulesAsync(ownership, session, transaction, cancellationToken);
        await PersistManifestAndTerminalAsync(ownership, session, transaction, cancellationToken);
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
            string? envelopeJson = null;
            if (recommendation is EnvelopeRecommendation envelope)
            {
                envelopeJson = Encoding.UTF8.GetString(AgentDecisionEnvelopeSerializer.ToUtf8Json(envelope));
            }

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
                            EnvelopeRecommendation envelopeReason => envelopeReason.NoActionReasonCategory,
                            _ => null,
                        },
                        CommunicationPurpose = recommendation switch
                        {
                            EmitMessageRecommendation emit => emit.CommunicationPurpose,
                            EnvelopeRecommendation envelopeMessage => envelopeMessage.Outputs
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
                        EnvelopeSchemaVersion = envelopeJson is null ? "v1" : "v2",
                        EnvelopeJson = envelopeJson,
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
                            EffectOutcome = DecisionEffectOutcomes.NotAttempted,
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                if (IsTerminalItemEffect(item.EffectOutcome))
                {
                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            InsertOutputEffectSql,
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
                                item.EffectOutcome,
                            },
                            transaction,
                            cancellationToken: cancellationToken));
                }
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
                            EffectOutcome = DecisionEffectOutcomes.NotAttempted,
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                if (IsTerminalItemEffect(item.EffectOutcome))
                {
                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            InsertActionEffectSql,
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
                                item.EffectOutcome,
                            },
                            transaction,
                            cancellationToken: cancellationToken));
                }
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

    public async Task<bool> TrySaveAgentResponsePublicationAsync(
        SessionOwnership ownership,
        long expectedSessionVersion,
        SessionRuntime session,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureOwnership(ownership, session.Ownership);
        // Callers must discard this SessionRuntime if the surrounding transaction
        // rolls back; retry by reloading PostgreSQL. Dirty flags are cleared here
        // before commit.

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
        await PersistTimerSchedulesAsync(ownership, session, transaction, cancellationToken);
        await PersistAgentMessagesAsync(ownership, session, transaction, cancellationToken);
        await PersistManifestAndTerminalAsync(ownership, session, transaction, cancellationToken);
        return true;
    }

    public async Task<bool> TrySaveLifecycleAsync(
        SessionOwnership ownership,
        long expectedSessionVersion,
        SessionRuntime session,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureOwnership(ownership, session.Ownership);

        var updated = await RequireConnection(transaction).ExecuteAsync(
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
        await PersistTimerSchedulesAsync(ownership, session, transaction, cancellationToken);
        await PersistAgentMessagesAsync(ownership, session, transaction, cancellationToken);
        await PersistManifestAndTerminalAsync(ownership, session, transaction, cancellationToken);
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
        foreach (var turn in session.DirtyTurns)
        {
            if (!turn.IsDirty)
            {
                continue;
            }

            TurnUpsertAttempts++;
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

        session.RemoveCleanTurns();

        foreach (var item in session.PendingTranscript)
        {
            TranscriptInsertAttempts++;
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

        session.ClearPendingTranscript();
    }

    private async Task PersistTimerSchedulesAsync(
        SessionOwnership ownership,
        SessionRuntime session,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = RequireConnection(transaction);
        var timerLaneDelegationId = await connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                LoadTimerLaneDelegationIdSql,
                new
                {
                    ownership.OrganizationId,
                    ownership.SessionId,
                    AllowedAction = SessionRuntimeAuditActions.FireDueTimer,
                },
                transaction,
                cancellationToken: cancellationToken));
        foreach (var revision in session.DirtyTimerSchedules)
        {
            var parameters = TimerScheduleParameters(ownership, revision, timerLaneDelegationId);
            if (revision.PendingInsert)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        InsertTimerScheduleSql,
                        parameters,
                        transaction,
                        cancellationToken: cancellationToken));
            }
            else
            {
                var updated = await connection.ExecuteAsync(
                    new CommandDefinition(
                        UpdateTimerScheduleSql,
                        parameters,
                        transaction,
                        cancellationToken: cancellationToken));
                if (updated != 1)
                {
                    throw new InvalidOperationException(
                        $"Timer schedule '{revision.ScheduleRevisionId}' was dirty but not updated.");
                }
            }

            revision.MarkPersisted();
        }
    }

    private async Task PersistBindingManifestRefsAsync(
        SessionOwnership ownership,
        SessionRuntime session,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = RequireConnection(transaction);
        await InsertManifestRefAsync(
            connection,
            transaction,
            cancellationToken,
            ownership,
            "configuration",
            session.Binding.ConfigurationId,
            session.Binding.ConfigurationDigest);
        await InsertManifestRefAsync(
            connection,
            transaction,
            cancellationToken,
            ownership,
            "manifest",
            session.Binding.ManifestId,
            ProtectedContentRef.DigestForReference(session.Binding.ManifestId));
        foreach (var reference in session.Binding.PermittedSubmissionRefs)
        {
            await InsertManifestRefAsync(
                connection,
                transaction,
                cancellationToken,
                ownership,
                "submission",
                reference.ProtectedRef,
                reference.ContentDigest);
        }

        foreach (var reference in session.Binding.PermittedKnowledgeRefs)
        {
            await InsertManifestRefAsync(
                connection,
                transaction,
                cancellationToken,
                ownership,
                "knowledge",
                reference.ProtectedRef,
                reference.ContentDigest);
        }

        foreach (var reference in session.Binding.PermittedMemoryReadRefs)
        {
            await InsertManifestRefAsync(
                connection,
                transaction,
                cancellationToken,
                ownership,
                "memory_read",
                reference.ProtectedRef,
                reference.ContentDigest);
        }
    }

    private async Task PersistFrozenPolicySnapshotAsync(
        SessionOwnership ownership,
        SessionRuntime session,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var binding = session.Binding;
        if (!string.Equals(binding.ConfigurationDigest, binding.Policy.PolicyDigest, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Frozen policy digest must match the Session configuration digest.");
        }

        await RequireConnection(transaction).ExecuteAsync(
            new CommandDefinition(
                InsertFrozenPolicySnapshotSql,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    binding.ConfigurationId,
                    binding.ConfigurationDigest,
                    binding.ManifestId,
                    PolicyDigest = binding.Policy.PolicyDigest,
                    PolicyPayload = FrozenRuntimePolicySnapshot.ToCanonicalJson(binding.Policy),
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private async Task PersistStartingParticipantRelationshipAsync(
        SessionOwnership ownership,
        TrustedRuntimeActor participantActor,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await RequireConnection(transaction).ExecuteAsync(
            new CommandDefinition(
                InsertStartingParticipantRelationshipSql,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    participantActor.ActorId,
                    participantActor.ActorType,
                    Relationship = SessionEventSubscriptionRelationships.Participant,
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private async Task InsertManifestRefAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken,
        SessionOwnership ownership,
        string refKind,
        string protectedRef,
        string contentDigest)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertManifestRefSql,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    RefKind = refKind,
                    ProtectedRef = protectedRef,
                    ContentDigest = contentDigest,
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private async Task PersistManifestAndTerminalAsync(
        SessionOwnership ownership,
        SessionRuntime session,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = RequireConnection(transaction);
        foreach (var record in session.PendingManifestRecords)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertManifestRuntimeSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        record.ManifestSequence,
                        record.RecordType,
                        record.ServiceActor,
                        OccurredAt = record.OccurredAt.UtcDateTime,
                        record.PayloadRef.ProtectedRef,
                        record.PayloadRef.ContentDigest,
                        record.SessionSequence,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            record.MarkPersisted();
        }

        if (session.TerminalRecordPendingInsert && session.TerminalRecord is { } terminal)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertTerminalRecordSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        terminal.TerminalRecordId,
                        LifecycleState = ToDbLifecycle(terminal.LifecycleState),
                        terminal.ReasonCategory,
                        terminal.AttemptMapping,
                        terminal.CutoffSequence,
                        terminal.ProcedureId,
                        terminal.SealDigest,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        if (session.EvaluationHandoffPendingInsert && session.EvaluationHandoff is { } handoff)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertEvaluationHandoffSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        handoff.HandoffId,
                        handoff.TerminalRecordId,
                        handoff.ProcedureId,
                        handoff.Eligibility,
                        TerminalState = ToDbLifecycle(handoff.TerminalState),
                        handoff.CutoffSequence,
                        handoff.ConfigurationId,
                        handoff.ConfigurationDigest,
                        handoff.ManifestId,
                        handoff.SealDigest,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        session.MarkTerminalArtifactsPersisted();
    }

    private static ManifestRuntimeRecord ToManifestRecord(SessionManifestRuntimeRow row) =>
        ManifestRuntimeRecord.Rehydrate(
            row.manifest_sequence,
            row.record_type,
            row.service_actor,
            ToUtc(row.occurred_at),
            new ProtectedContentRef(row.protected_ref, row.content_digest),
            row.session_sequence);

    private SessionTerminalRecord ToTerminalRecord(SessionTerminalRow row) =>
        new(
            row.terminal_record_id,
            FromDbLifecycle(row.lifecycle_state),
            row.reason_category,
            row.attempt_mapping,
            row.cutoff_sequence,
            row.procedure_id ?? ManifestSealProcedures.LegacyUnsealed,
            row.seal_digest);

    private EvaluationHandoff ToEvaluationHandoff(SessionEvaluationHandoffRow row) =>
        new(
            row.handoff_id,
            row.terminal_record_id,
            row.procedure_id,
            row.eligibility,
            FromDbLifecycle(row.terminal_state),
            row.cutoff_sequence,
            row.configuration_id,
            row.configuration_digest,
            row.manifest_id,
            row.seal_digest);

    private static object TimerScheduleParameters(
        SessionOwnership ownership,
        TimerScheduleRevision revision,
        Guid? timerLaneDelegationId) => new
    {
        ownership.OrganizationId,
        ownership.ActivityId,
        ownership.ParticipantId,
        ownership.AttemptId,
        ownership.SessionId,
        ScheduleRevisionId = revision.ScheduleRevisionId,
        ScheduleRevisionOrdinal = revision.ScheduleRevision,
        State = ToDbTimerState(revision.LaneState),
        LaneState = revision.LaneState,
        revision.RelativeDelay,
        revision.RemainingActiveSeconds,
        RemainingSince = revision.RemainingSince.UtcDateTime,
        FireAt = revision.DueAt?.UtcDateTime,
        revision.RequestedByCategory,
        SourceDecisionId = revision.DrivingDecisionId,
        revision.FiredInvocationId,
        CreatedAt = revision.CreatedAt.UtcDateTime,
        TimerLaneDelegationId = timerLaneDelegationId,
    };

    private static string ToDbTimerState(string laneState) => laneState switch
    {
        TimerLaneStates.Superseded => "replaced",
        TimerLaneStates.Expired => "cancelled",
        _ => laneState,
    };

    private static string FromDbTimerLaneState(string? laneState, string state)
    {
        if (!string.IsNullOrEmpty(laneState))
        {
            return laneState;
        }

        return state switch
        {
            "replaced" => TimerLaneStates.Superseded,
            _ => state,
        };
    }

    private TimerScheduleRevision ToTimerSchedule(SessionTimerScheduleRow row)
    {
        var remainingSince = row.remaining_since is null ? ToUtc(row.created_at ?? DateTime.UnixEpoch) : ToUtc(row.remaining_since.Value);
        var createdAt = row.created_at is null ? remainingSince : ToUtc(row.created_at.Value);
        return TimerScheduleRevision.Rehydrate(
            row.schedule_revision,
            row.schedule_revision_ordinal ?? 1,
            FromDbTimerLaneState(row.lane_state, row.state),
            row.relative_delay,
            row.remaining_active_seconds ?? 0,
            row.fire_at is null ? null : ToUtc(row.fire_at.Value),
            remainingSince,
            row.requested_by_category ?? TimerRequestedByCategories.DefaultCadence,
            row.source_decision_id,
            row.fired_invocation_id,
            createdAt);
    }

    private async Task PersistAgentMessagesAsync(
        SessionOwnership ownership,
        SessionRuntime session,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = RequireConnection(transaction);
        foreach (var message in session.PendingPublicationWork)
        {
            PublicationMessagesTouched++;
            var protectedRef = $"msg:{message.MessageId}";
            if (message.PendingInsert)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        InsertAgentMessageSql,
                        new
                        {
                            ownership.OrganizationId,
                            ownership.ActivityId,
                            ownership.ParticipantId,
                            ownership.AttemptId,
                            ownership.SessionId,
                            message.MessageId,
                            message.TurnId,
                            ProtectedRef = protectedRef,
                            ContentDigest = ProtectedContentRef.DigestForReference(protectedRef),
                            message.CompletionState,
                            message.GenerationAttemptId,
                            DrivingInvocationId = message.DrivingInvocationId,
                            DrivingDecisionId = message.DrivingDecisionId,
                            AcceptedAgentOutputId = message.AcceptedAgentOutputId,
                            message.AssembledContentDigest,
                            message.ResponseSlotId,
                            message.SealedSessionSequence,
                            SealedAt = message.SealedAt?.UtcDateTime,
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                message.MarkMessagePersisted();
            }

            if (message.SealDirty)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        UpdateAgentMessageSealSql,
                        new
                        {
                            ownership.OrganizationId,
                            ownership.ActivityId,
                            ownership.ParticipantId,
                            ownership.AttemptId,
                            ownership.SessionId,
                            message.MessageId,
                            message.CompletionState,
                            message.AssembledContentDigest,
                            message.SealedSessionSequence,
                            SealedAt = message.SealedAt?.UtcDateTime,
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                message.MarkSealPersisted();
            }

            foreach (var fragment in message.PendingInserts)
            {
                FragmentInsertAttempts++;
                var inserted = await connection.ExecuteAsync(
                    new CommandDefinition(
                        InsertAgentFragmentSql,
                        new
                        {
                            ownership.OrganizationId,
                            ownership.ActivityId,
                            ownership.ParticipantId,
                            ownership.AttemptId,
                            ownership.SessionId,
                            message.MessageId,
                            fragment.FragmentOrdinal,
                            fragment.SessionSequence,
                            message.TurnId,
                            ResponseSlotId = message.ResponseSlotId,
                            message.GenerationAttemptId,
                            ProtectedRef = $"frag:{message.MessageId}:{fragment.FragmentOrdinal}",
                            fragment.ContentDigest,
                            ExactUtf8Text = fragment.ExactUtf8Text,
                            DrivingInvocationId = message.DrivingInvocationId,
                            DrivingDecisionId = message.DrivingDecisionId,
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                if (inserted == 0)
                {
                    var storedDigest = await connection.ExecuteScalarAsync<string>(
                        new CommandDefinition(
                            LoadFragmentDigestSql,
                            new
                            {
                                ownership.OrganizationId,
                                ownership.SessionId,
                                message.MessageId,
                                fragment.FragmentOrdinal,
                            },
                            transaction,
                            cancellationToken: cancellationToken));
                    if (!string.Equals(storedDigest, fragment.ContentDigest, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "Persisted fragment digest does not match the committed ordinal.");
                    }
                }

                fragment.MarkPersisted();
            }

            message.ClearPersistedPendingInserts();
        }

        session.RemoveCleanPublicationWork();
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
        IReadOnlyList<SessionActionValidationRow> actionRows,
        IReadOnlyList<SessionItemEffectRow> outputEffectRows,
        IReadOnlyList<SessionItemEffectRow> actionEffectRows)
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
                    ResolveItemEffect(item.effect_outcome, outputEffectRows, item.item_ordinal)))
                .ToArray(),
            actionRows
                .Select(item => new RequestedActionItemValidation(
                    item.local_ref,
                    item.kind,
                    item.validation_outcome,
                    item.rejection_reason_category,
                    ResolveItemEffect(item.effect_outcome, actionEffectRows, item.item_ordinal)))
                .ToArray());
        record.BindAuthoritativeState(
            row.revision_ordinal,
            row.validation_commit_session_version,
            row.validation_commit_session_sequence);
        if (row.effect_outcome is DecisionEffectOutcomes.Applied
            or DecisionEffectOutcomes.NoDomainEffect
            or DecisionEffectOutcomes.EffectFailed)
        {
            record.RestorePersistedEffect(row.effect_outcome, row.applied_turn_id, row.applied_response_slot_id);
            if (row.effect_commit_session_version is not null && row.effect_commit_session_sequence is not null)
            {
                record.BindEffectCommitState(
                    row.effect_commit_session_version.Value,
                    row.effect_commit_session_sequence.Value);
            }
        }

        return record;
    }

    private static string ResolveItemEffect(
        string validationRowEffect,
        IReadOnlyList<SessionItemEffectRow> effectRows,
        int itemOrdinal)
    {
        var fact = effectRows.FirstOrDefault(row => row.item_ordinal == itemOrdinal);
        return fact is not null && IsTerminalItemEffect(fact.effect_outcome)
            ? fact.effect_outcome
            : validationRowEffect;
    }

    private static bool IsTerminalItemEffect(string effectOutcome) =>
        effectOutcome is DecisionEffectOutcomes.Applied
            or DecisionEffectOutcomes.NoDomainEffect
            or DecisionEffectOutcomes.EffectFailed;

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

    private sealed record SessionAgentMessageRow(
        string message_id,
        string generation_attempt_id,
        string driving_invocation_id,
        string driving_decision_id,
        string turn_id,
        string response_slot_id,
        string completion_state,
        string? assembled_content_digest,
        string? accepted_agent_output_id,
        long? sealed_session_sequence,
        DateTime? sealed_at);

    private sealed record SessionAgentFragmentRow(
        string message_id,
        int fragment_ordinal,
        long session_sequence,
        string exact_utf8_text,
        string content_digest,
        DateTime committed_at);

    private sealed record SessionTimerScheduleRow(
        string schedule_revision,
        long? schedule_revision_ordinal,
        string state,
        string? lane_state,
        string relative_delay,
        int? remaining_active_seconds,
        DateTime? remaining_since,
        DateTime? fire_at,
        string? requested_by_category,
        string? source_decision_id,
        string? fired_invocation_id,
        DateTime? created_at);

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

    private sealed record SessionItemEffectRow(
        string agent_invocation_id,
        int revision_ordinal,
        int item_ordinal,
        string effect_outcome);

    private sealed record SessionManifestRuntimeRow(
        long manifest_sequence,
        string record_type,
        string service_actor,
        DateTime occurred_at,
        string protected_ref,
        string content_digest,
        long session_sequence);

    private sealed record SessionTerminalRow(
        Guid terminal_record_id,
        string lifecycle_state,
        string? reason_category,
        string? attempt_mapping,
        long? cutoff_sequence,
        string? procedure_id,
        string? seal_digest);

    private sealed record SessionEvaluationHandoffRow(
        string handoff_id,
        Guid terminal_record_id,
        string procedure_id,
        string eligibility,
        string terminal_state,
        long? cutoff_sequence,
        string configuration_id,
        string configuration_digest,
        string manifest_id,
        string seal_digest);
}
