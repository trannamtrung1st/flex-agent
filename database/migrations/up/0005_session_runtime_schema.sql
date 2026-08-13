-- Immutable one-time migration: Session runtime persistence for the frozen
-- in-memory Sessions aggregate. ADR-010/012/013; UTC-ordered; do not edit after merge.
-- P2: validated_against_* is the Session state observed during validation;
--     validation_commit_* is the Session state after the validation row commits.
--     In-memory ValidatedAtSessionVersion is the post-Touch stand-in; hydrate it
--     from validation_commit_* and persist against from pre-Touch state.
-- P3: decision_payload_digest_version identifies the canonical digest format.

CREATE OR REPLACE FUNCTION reject_session_append_only_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION '% are append-only', TG_TABLE_NAME;
END;
$$;

CREATE OR REPLACE FUNCTION stamp_session_committed_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.committed_at := clock_timestamp();
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION stamp_session_last_committed_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.last_committed_at := clock_timestamp();
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION stamp_session_created_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.created_at := clock_timestamp();
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION protect_session_decision_validation_identity()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.organization_id IS DISTINCT FROM OLD.organization_id
        OR NEW.activity_id IS DISTINCT FROM OLD.activity_id
        OR NEW.participant_id IS DISTINCT FROM OLD.participant_id
        OR NEW.attempt_id IS DISTINCT FROM OLD.attempt_id
        OR NEW.session_id IS DISTINCT FROM OLD.session_id
        OR NEW.agent_invocation_id IS DISTINCT FROM OLD.agent_invocation_id
        OR NEW.revision_ordinal IS DISTINCT FROM OLD.revision_ordinal
        OR NEW.validated_against_session_version IS DISTINCT FROM OLD.validated_against_session_version
        OR NEW.validated_against_session_sequence IS DISTINCT FROM OLD.validated_against_session_sequence
        OR NEW.validation_commit_session_version IS DISTINCT FROM OLD.validation_commit_session_version
        OR NEW.validation_commit_session_sequence IS DISTINCT FROM OLD.validation_commit_session_sequence
        OR NEW.validation_committed_at IS DISTINCT FROM OLD.validation_committed_at
        OR NEW.validation_outcome IS DISTINCT FROM OLD.validation_outcome
        OR NEW.timer_validation_outcome IS DISTINCT FROM OLD.timer_validation_outcome
        OR NEW.rejection_reason_category IS DISTINCT FROM OLD.rejection_reason_category
    THEN
        RAISE EXCEPTION 'session_decision_validations validation identity is append-only';
    END IF;

    NEW.effect_committed_at := clock_timestamp();
    RETURN NEW;
END;
$$;

CREATE TABLE session_runtimes (
    organization_id UUID NOT NULL REFERENCES organizations (id),
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    configuration_id TEXT NOT NULL,
    configuration_digest CHAR(64) NOT NULL,
    manifest_id TEXT NOT NULL,
    lifecycle_state TEXT NOT NULL,
    session_version BIGINT NOT NULL DEFAULT 0,
    session_sequence BIGINT NOT NULL DEFAULT 0,
    cutoff_sequence BIGINT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    last_committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id),
    CONSTRAINT uq_session_runtimes_ownership
        UNIQUE (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_session_runtimes_digest_lowercase
        CHECK (configuration_digest = lower(configuration_digest)),
    CONSTRAINT chk_session_runtimes_lifecycle
        CHECK (lifecycle_state IN (
            'ready', 'active', 'paused', 'completing', 'completed', 'terminated', 'aborted')),
    CONSTRAINT chk_session_runtimes_version_nonnegative
        CHECK (session_version >= 0 AND session_sequence >= 0)
);

CREATE TRIGGER trg_session_runtimes_stamp_created
    BEFORE INSERT ON session_runtimes
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_created_at();

CREATE TRIGGER trg_session_runtimes_stamp_committed
    BEFORE INSERT OR UPDATE ON session_runtimes
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_last_committed_at();

CREATE TRIGGER trg_session_runtimes_no_delete
    BEFORE DELETE ON session_runtimes
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_events (
    event_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    session_sequence BIGINT NOT NULL,
    event_type TEXT NOT NULL,
    payload_digest CHAR(64) NOT NULL,
    correlation_id UUID NULL,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, event_id),
    CONSTRAINT fk_session_events_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT uq_session_events_sequence
        UNIQUE (organization_id, session_id, session_sequence),
    CONSTRAINT chk_session_events_digest_lowercase
        CHECK (payload_digest = lower(payload_digest)),
    CONSTRAINT chk_session_events_sequence_positive
        CHECK (session_sequence > 0)
);

CREATE TRIGGER trg_session_events_stamp_committed
    BEFORE INSERT ON session_events
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_events_no_update
    BEFORE UPDATE ON session_events
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_events_no_delete
    BEFORE DELETE ON session_events
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_turns (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    turn_id TEXT NOT NULL,
    kind TEXT NOT NULL,
    state TEXT NOT NULL,
    trigger_invocation_id TEXT NULL,
    response_slot_id TEXT NOT NULL,
    response_slot_state TEXT NOT NULL,
    claimed_by_invocation_id TEXT NULL,
    last_committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, turn_id),
    CONSTRAINT fk_session_turns_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT uq_session_turns_response_slot
        UNIQUE (organization_id, session_id, response_slot_id),
    CONSTRAINT chk_session_turns_kind
        CHECK (kind IN ('participant', 'agent_opening', 'agent_closing', 'agent_timer')),
    CONSTRAINT chk_session_turns_state
        CHECK (state IN ('accepted', 'work_queued', 'complete', 'cancelled')),
    CONSTRAINT chk_session_turns_slot_state
        CHECK (response_slot_state IN (
            'open', 'claimed_for_publication', 'intentional_no_action', 'cancelled'))
);

CREATE TRIGGER trg_session_turns_stamp_committed
    BEFORE INSERT OR UPDATE ON session_turns
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_last_committed_at();

CREATE TRIGGER trg_session_turns_no_delete
    BEFORE DELETE ON session_turns
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_visible_transcript_items (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    message_id TEXT NOT NULL,
    author_type TEXT NOT NULL,
    turn_id TEXT NULL,
    protected_ref TEXT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, message_id),
    CONSTRAINT fk_session_visible_transcript_items_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_session_visible_transcript_author
        CHECK (author_type IN ('participant', 'agent')),
    CONSTRAINT chk_session_visible_transcript_digest_lowercase
        CHECK (content_digest = lower(content_digest))
);

CREATE TRIGGER trg_session_visible_transcript_items_stamp_committed
    BEFORE INSERT ON session_visible_transcript_items
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_visible_transcript_items_no_update
    BEFORE UPDATE ON session_visible_transcript_items
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_visible_transcript_items_no_delete
    BEFORE DELETE ON session_visible_transcript_items
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_invocations (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    agent_invocation_id TEXT NOT NULL,
    trigger_family TEXT NOT NULL,
    trigger_type TEXT NOT NULL,
    trigger_id TEXT NOT NULL,
    purpose TEXT NOT NULL,
    turn_id TEXT NULL,
    response_slot_id TEXT NULL,
    idempotency_key TEXT NOT NULL,
    policy_digest CHAR(64) NOT NULL,
    admitted_session_sequence BIGINT NOT NULL,
    status TEXT NOT NULL,
    agent_decision_id TEXT NULL,
    execution_outcome_id TEXT NULL,
    last_committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, agent_invocation_id),
    CONSTRAINT fk_session_invocations_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT uq_session_invocations_idempotency
        UNIQUE (organization_id, session_id, idempotency_key),
    CONSTRAINT uq_session_invocations_identity
        UNIQUE (
            organization_id, session_id, trigger_family, trigger_type, trigger_id, purpose, policy_digest),
    CONSTRAINT chk_session_invocations_digest_lowercase
        CHECK (policy_digest = lower(policy_digest)),
    CONSTRAINT chk_session_invocations_status
        CHECK (status IN (
            'admitted', 'executing', 'decision_recorded', 'decided', 'execution_failed', 'cancelled')),
    CONSTRAINT chk_session_invocations_exclusive_outcome
        CHECK (agent_decision_id IS NULL OR execution_outcome_id IS NULL)
);

CREATE TRIGGER trg_session_invocations_stamp_committed
    BEFORE INSERT OR UPDATE ON session_invocations
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_last_committed_at();

CREATE TRIGGER trg_session_invocations_no_delete
    BEFORE DELETE ON session_invocations
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_invocation_attempts (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    agent_invocation_id TEXT NOT NULL,
    attempt_ordinal INT NOT NULL,
    outcome_category TEXT NOT NULL,
    agent_decision_id TEXT NULL,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, agent_invocation_id, attempt_ordinal),
    CONSTRAINT fk_session_invocation_attempts_invocation
        FOREIGN KEY (organization_id, session_id, agent_invocation_id)
        REFERENCES session_invocations (organization_id, session_id, agent_invocation_id),
    CONSTRAINT chk_session_invocation_attempts_ordinal
        CHECK (attempt_ordinal >= 1)
);

CREATE TRIGGER trg_session_invocation_attempts_stamp_committed
    BEFORE INSERT ON session_invocation_attempts
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_invocation_attempts_no_update
    BEFORE UPDATE ON session_invocation_attempts
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_invocation_attempts_no_delete
    BEFORE DELETE ON session_invocation_attempts
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_decisions (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    agent_invocation_id TEXT NOT NULL,
    decision_id TEXT NOT NULL,
    decision_type TEXT NOT NULL,
    produced_at TIMESTAMPTZ NOT NULL,
    next_timer_expected_schedule_revision TEXT NULL,
    next_timer_relative_delay TEXT NULL,
    reason_category TEXT NULL,
    communication_purpose TEXT NULL,
    turn_id TEXT NULL,
    response_slot_id TEXT NULL,
    payload_digest CHAR(64) NOT NULL,
    decision_payload_digest_version TEXT NOT NULL DEFAULT 'v1',
    committed_session_version BIGINT NOT NULL DEFAULT 0,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, decision_id),
    CONSTRAINT fk_session_decisions_invocation
        FOREIGN KEY (organization_id, session_id, agent_invocation_id)
        REFERENCES session_invocations (organization_id, session_id, agent_invocation_id),
    CONSTRAINT uq_session_decisions_invocation
        UNIQUE (organization_id, session_id, agent_invocation_id),
    CONSTRAINT chk_session_decisions_digest_lowercase
        CHECK (payload_digest = lower(payload_digest)),
    CONSTRAINT chk_session_decisions_digest_version
        CHECK (decision_payload_digest_version ~ '^v[0-9]+$'),
    CONSTRAINT chk_session_decisions_type
        CHECK (decision_type IN ('no_action', 'emit_message'))
);

CREATE TRIGGER trg_session_decisions_stamp_committed
    BEFORE INSERT ON session_decisions
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_decisions_no_update
    BEFORE UPDATE ON session_decisions
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_decisions_no_delete
    BEFORE DELETE ON session_decisions
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_decision_validations (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    agent_invocation_id TEXT NOT NULL,
    revision_ordinal INT NOT NULL,
    validated_against_session_version BIGINT NOT NULL,
    validated_against_session_sequence BIGINT NOT NULL,
    validation_commit_session_version BIGINT NOT NULL,
    validation_commit_session_sequence BIGINT NOT NULL,
    validation_committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    validation_outcome TEXT NOT NULL,
    effect_outcome TEXT NOT NULL,
    timer_validation_outcome TEXT NOT NULL,
    rejection_reason_category TEXT NULL,
    applied_turn_id TEXT NULL,
    applied_response_slot_id TEXT NULL,
    effect_commit_session_version BIGINT NULL,
    effect_commit_session_sequence BIGINT NULL,
    effect_committed_at TIMESTAMPTZ NULL,
    PRIMARY KEY (organization_id, session_id, agent_invocation_id, revision_ordinal),
    CONSTRAINT fk_session_decision_validations_invocation
        FOREIGN KEY (organization_id, session_id, agent_invocation_id)
        REFERENCES session_invocations (organization_id, session_id, agent_invocation_id),
    CONSTRAINT chk_session_decision_validations_revision
        CHECK (revision_ordinal >= 1),
    CONSTRAINT chk_session_decision_validations_against_vs_commit
        CHECK (
            validation_commit_session_version >= validated_against_session_version
            AND validation_commit_session_sequence >= validated_against_session_sequence),
    CONSTRAINT chk_session_decision_validations_outcome
        CHECK (validation_outcome IN ('accepted', 'rejected', 'suppressed')),
    CONSTRAINT chk_session_decision_validations_effect
        CHECK (effect_outcome IN ('applied', 'no_domain_effect', 'effect_failed', 'not_attempted')),
    CONSTRAINT chk_session_decision_validations_timer
        CHECK (timer_validation_outcome IN ('accepted', 'rejected', 'omitted', 'not_present'))
);

CREATE UNIQUE INDEX uq_session_decision_validations_terminal_effect
    ON session_decision_validations (organization_id, session_id, agent_invocation_id)
    WHERE effect_outcome IN ('applied', 'no_domain_effect', 'effect_failed');

CREATE OR REPLACE FUNCTION stamp_session_validation_committed_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.validation_committed_at := clock_timestamp();
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_session_decision_validations_stamp_committed
    BEFORE INSERT ON session_decision_validations
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_validation_committed_at();

CREATE TRIGGER trg_session_decision_validations_protect_identity
    BEFORE UPDATE ON session_decision_validations
    FOR EACH ROW
    EXECUTE FUNCTION protect_session_decision_validation_identity();

CREATE TRIGGER trg_session_decision_validations_no_delete
    BEFORE DELETE ON session_decision_validations
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_execution_outcomes (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    agent_invocation_id TEXT NOT NULL,
    execution_outcome_id TEXT NOT NULL,
    outcome_category TEXT NOT NULL,
    reason_category TEXT NOT NULL,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, execution_outcome_id),
    CONSTRAINT fk_session_execution_outcomes_invocation
        FOREIGN KEY (organization_id, session_id, agent_invocation_id)
        REFERENCES session_invocations (organization_id, session_id, agent_invocation_id),
    CONSTRAINT uq_session_execution_outcomes_invocation
        UNIQUE (organization_id, session_id, agent_invocation_id)
);

CREATE TRIGGER trg_session_execution_outcomes_stamp_committed
    BEFORE INSERT ON session_execution_outcomes
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_execution_outcomes_no_update
    BEFORE UPDATE ON session_execution_outcomes
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_execution_outcomes_no_delete
    BEFORE DELETE ON session_execution_outcomes
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_messages (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    message_id TEXT NOT NULL,
    author_type TEXT NOT NULL,
    turn_id TEXT NULL,
    response_slot_id TEXT NULL,
    protected_ref TEXT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    completion_state TEXT NOT NULL DEFAULT 'open',
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, message_id),
    CONSTRAINT fk_session_messages_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_session_messages_author
        CHECK (author_type IN ('participant', 'agent')),
    CONSTRAINT chk_session_messages_digest_lowercase
        CHECK (content_digest = lower(content_digest)),
    CONSTRAINT chk_session_messages_completion
        CHECK (completion_state IN ('open', 'complete', 'cancelled'))
);

CREATE OR REPLACE FUNCTION protect_session_message_identity()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.organization_id IS DISTINCT FROM OLD.organization_id
        OR NEW.activity_id IS DISTINCT FROM OLD.activity_id
        OR NEW.participant_id IS DISTINCT FROM OLD.participant_id
        OR NEW.attempt_id IS DISTINCT FROM OLD.attempt_id
        OR NEW.session_id IS DISTINCT FROM OLD.session_id
        OR NEW.message_id IS DISTINCT FROM OLD.message_id
        OR NEW.author_type IS DISTINCT FROM OLD.author_type
        OR NEW.turn_id IS DISTINCT FROM OLD.turn_id
        OR NEW.response_slot_id IS DISTINCT FROM OLD.response_slot_id
        OR NEW.protected_ref IS DISTINCT FROM OLD.protected_ref
        OR NEW.content_digest IS DISTINCT FROM OLD.content_digest
        OR NEW.committed_at IS DISTINCT FROM OLD.committed_at
    THEN
        RAISE EXCEPTION 'session_messages identity is append-only';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_session_messages_stamp_committed
    BEFORE INSERT ON session_messages
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_messages_protect_identity
    BEFORE UPDATE ON session_messages
    FOR EACH ROW
    EXECUTE FUNCTION protect_session_message_identity();

CREATE TRIGGER trg_session_messages_no_delete
    BEFORE DELETE ON session_messages
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_message_fragments (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    message_id TEXT NOT NULL,
    fragment_ordinal INT NOT NULL,
    session_sequence BIGINT NOT NULL,
    turn_id TEXT NULL,
    response_slot_id TEXT NULL,
    generation_attempt_id TEXT NULL,
    protected_ref TEXT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, message_id, fragment_ordinal),
    CONSTRAINT fk_session_message_fragments_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT fk_session_message_fragments_message
        FOREIGN KEY (organization_id, session_id, message_id)
        REFERENCES session_messages (organization_id, session_id, message_id),
    CONSTRAINT uq_session_message_fragments_sequence
        UNIQUE (organization_id, session_id, session_sequence),
    CONSTRAINT chk_session_message_fragments_ordinal
        CHECK (fragment_ordinal >= 1),
    CONSTRAINT chk_session_message_fragments_sequence
        CHECK (session_sequence > 0),
    CONSTRAINT chk_session_message_fragments_digest_lowercase
        CHECK (content_digest = lower(content_digest))
);

CREATE TRIGGER trg_session_message_fragments_stamp_committed
    BEFORE INSERT ON session_message_fragments
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_message_fragments_no_update
    BEFORE UPDATE ON session_message_fragments
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_message_fragments_no_delete
    BEFORE DELETE ON session_message_fragments
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_timer_schedules (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    schedule_revision TEXT NOT NULL,
    state TEXT NOT NULL,
    relative_delay TEXT NOT NULL,
    fire_at TIMESTAMPTZ NULL,
    source_invocation_id TEXT NULL,
    source_decision_id TEXT NULL,
    last_committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, schedule_revision),
    CONSTRAINT fk_session_timer_schedules_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_session_timer_schedules_state
        CHECK (state IN ('pending', 'claimed', 'fired', 'replaced', 'cancelled'))
);

CREATE UNIQUE INDEX uq_session_timer_schedules_one_current
    ON session_timer_schedules (organization_id, session_id)
    WHERE state IN ('pending', 'claimed');

CREATE TRIGGER trg_session_timer_schedules_stamp_committed
    BEFORE INSERT OR UPDATE ON session_timer_schedules
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_last_committed_at();

CREATE TRIGGER trg_session_timer_schedules_no_delete
    BEFORE DELETE ON session_timer_schedules
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_pause_intervals (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    pause_id UUID NOT NULL,
    started_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    ended_at TIMESTAMPTZ NULL,
    last_committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, pause_id),
    CONSTRAINT fk_session_pause_intervals_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id)
);

CREATE TRIGGER trg_session_pause_intervals_stamp_committed
    BEFORE INSERT OR UPDATE ON session_pause_intervals
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_last_committed_at();

CREATE TRIGGER trg_session_pause_intervals_no_delete
    BEFORE DELETE ON session_pause_intervals
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_terminal_intents (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    intent_id UUID NOT NULL,
    reason_category TEXT NOT NULL,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, intent_id),
    CONSTRAINT fk_session_terminal_intents_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id)
);

CREATE TRIGGER trg_session_terminal_intents_stamp_committed
    BEFORE INSERT ON session_terminal_intents
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_terminal_intents_no_update
    BEFORE UPDATE ON session_terminal_intents
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_terminal_intents_no_delete
    BEFORE DELETE ON session_terminal_intents
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_terminal_records (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    terminal_record_id UUID NOT NULL,
    lifecycle_state TEXT NOT NULL,
    intent_id UUID NULL,
    cutoff_sequence BIGINT NULL,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, terminal_record_id),
    CONSTRAINT fk_session_terminal_records_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT uq_session_terminal_records_session
        UNIQUE (organization_id, session_id)
);

CREATE TRIGGER trg_session_terminal_records_stamp_committed
    BEFORE INSERT ON session_terminal_records
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_terminal_records_no_update
    BEFORE UPDATE ON session_terminal_records
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_terminal_records_no_delete
    BEFORE DELETE ON session_terminal_records
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_durable_work (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    work_id UUID NOT NULL,
    work_type TEXT NOT NULL,
    business_key TEXT NOT NULL,
    state TEXT NOT NULL,
    claim_lease_until TIMESTAMPTZ NULL,
    last_committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, work_id),
    CONSTRAINT fk_session_durable_work_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT uq_session_durable_work_business_key
        UNIQUE (organization_id, session_id, work_type, business_key),
    CONSTRAINT chk_session_durable_work_state
        CHECK (state IN ('pending', 'claimed', 'completed', 'failed', 'cancelled'))
);

CREATE TRIGGER trg_session_durable_work_stamp_committed
    BEFORE INSERT OR UPDATE ON session_durable_work
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_last_committed_at();

CREATE TRIGGER trg_session_durable_work_no_delete
    BEFORE DELETE ON session_durable_work
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_manifest_refs (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    ref_kind TEXT NOT NULL,
    protected_ref TEXT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, ref_kind, protected_ref),
    CONSTRAINT fk_session_manifest_refs_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_session_manifest_refs_kind
        CHECK (ref_kind IN ('submission', 'knowledge', 'memory_read', 'configuration', 'manifest')),
    CONSTRAINT chk_session_manifest_refs_digest_lowercase
        CHECK (content_digest = lower(content_digest))
);

CREATE TRIGGER trg_session_manifest_refs_stamp_committed
    BEFORE INSERT ON session_manifest_refs
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_manifest_refs_no_update
    BEFORE UPDATE ON session_manifest_refs
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_manifest_refs_no_delete
    BEFORE DELETE ON session_manifest_refs
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();
