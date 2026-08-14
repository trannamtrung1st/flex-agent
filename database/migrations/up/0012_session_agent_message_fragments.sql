-- Persist Agent Message fragments with exact UTF-8 deltas and bind them
-- to the driving Decision and accepted aout.* output. Frozen 0005 tables
-- stored digest/protected_ref only and had no Decision linkage. Do not
-- rewrite 0005-0011.
-- UTC-ordered; additive after frozen 0011.

ALTER TABLE session_decisions
    DROP CONSTRAINT IF EXISTS uq_session_decisions_ownership_decision;

ALTER TABLE session_decisions
    ADD CONSTRAINT uq_session_decisions_ownership_decision
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            decision_id);

ALTER TABLE session_decision_output_validations
    DROP CONSTRAINT IF EXISTS uq_session_decision_output_validations_accepted_output_id;

ALTER TABLE session_decision_output_validations
    ADD CONSTRAINT uq_session_decision_output_validations_accepted_output_id
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_output_id);

ALTER TABLE session_messages
    ADD COLUMN IF NOT EXISTS generation_attempt_id TEXT NULL;

ALTER TABLE session_messages
    ADD COLUMN IF NOT EXISTS driving_invocation_id TEXT NULL;

ALTER TABLE session_messages
    ADD COLUMN IF NOT EXISTS driving_decision_id TEXT NULL;

ALTER TABLE session_messages
    ADD COLUMN IF NOT EXISTS accepted_agent_output_id TEXT NULL;

ALTER TABLE session_messages
    ADD COLUMN IF NOT EXISTS assembled_content_digest CHAR(64) NULL;

ALTER TABLE session_messages
    DROP CONSTRAINT IF EXISTS chk_session_messages_completion;

ALTER TABLE session_messages
    ADD CONSTRAINT chk_session_messages_completion
        CHECK (completion_state IN ('open', 'complete', 'incomplete', 'cancelled'));

ALTER TABLE session_messages
    DROP CONSTRAINT IF EXISTS chk_session_messages_agent_decision;

ALTER TABLE session_messages
    ADD CONSTRAINT chk_session_messages_agent_decision
        CHECK (
            (author_type = 'participant'
                AND generation_attempt_id IS NULL
                AND driving_invocation_id IS NULL
                AND driving_decision_id IS NULL
                AND accepted_agent_output_id IS NULL
                AND assembled_content_digest IS NULL)
            OR (author_type = 'agent'
                AND generation_attempt_id IS NOT NULL
                AND driving_invocation_id IS NOT NULL
                AND driving_decision_id IS NOT NULL
                AND (accepted_agent_output_id IS NULL
                    OR accepted_agent_output_id = message_id)));

ALTER TABLE session_messages
    DROP CONSTRAINT IF EXISTS chk_session_messages_assembled_digest_lowercase;

ALTER TABLE session_messages
    ADD CONSTRAINT chk_session_messages_assembled_digest_lowercase
        CHECK (
            assembled_content_digest IS NULL
            OR assembled_content_digest = lower(assembled_content_digest));

ALTER TABLE session_messages
    DROP CONSTRAINT IF EXISTS uq_session_messages_ownership_message;

ALTER TABLE session_messages
    ADD CONSTRAINT uq_session_messages_ownership_message
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            message_id);

ALTER TABLE session_messages
    DROP CONSTRAINT IF EXISTS fk_session_messages_decision;

ALTER TABLE session_messages
    ADD CONSTRAINT fk_session_messages_decision
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            driving_decision_id)
        REFERENCES session_decisions (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            decision_id);

ALTER TABLE session_messages
    DROP CONSTRAINT IF EXISTS fk_session_messages_invocation;

ALTER TABLE session_messages
    ADD CONSTRAINT fk_session_messages_invocation
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            driving_invocation_id)
        REFERENCES session_invocations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id);

ALTER TABLE session_messages
    DROP CONSTRAINT IF EXISTS fk_session_messages_accepted_output;

ALTER TABLE session_messages
    ADD CONSTRAINT fk_session_messages_accepted_output
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            accepted_agent_output_id)
        REFERENCES session_decision_output_validations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_output_id);

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
        OR NEW.protected_ref IS DISTINCT FROM OLD.protected_ref
        OR NEW.content_digest IS DISTINCT FROM OLD.content_digest
        OR NEW.committed_at IS DISTINCT FROM OLD.committed_at
        OR NEW.generation_attempt_id IS DISTINCT FROM OLD.generation_attempt_id
        OR NEW.driving_invocation_id IS DISTINCT FROM OLD.driving_invocation_id
        OR NEW.driving_decision_id IS DISTINCT FROM OLD.driving_decision_id
        OR NEW.accepted_agent_output_id IS DISTINCT FROM OLD.accepted_agent_output_id
    THEN
        RAISE EXCEPTION 'session_messages identity is append-only';
    END IF;

    RETURN NEW;
END;
$$;

ALTER TABLE session_message_fragments
    ADD COLUMN IF NOT EXISTS exact_utf8_text TEXT NULL;

ALTER TABLE session_message_fragments
    ADD COLUMN IF NOT EXISTS driving_invocation_id TEXT NULL;

ALTER TABLE session_message_fragments
    ADD COLUMN IF NOT EXISTS driving_decision_id TEXT NULL;

ALTER TABLE session_message_fragments
    ADD COLUMN IF NOT EXISTS accepted_agent_output_id TEXT NULL;

ALTER TABLE session_message_fragments
    ALTER COLUMN exact_utf8_text SET NOT NULL;

ALTER TABLE session_message_fragments
    ALTER COLUMN driving_invocation_id SET NOT NULL;

ALTER TABLE session_message_fragments
    ALTER COLUMN driving_decision_id SET NOT NULL;

ALTER TABLE session_message_fragments
    DROP CONSTRAINT IF EXISTS chk_session_message_fragments_exact_text;

ALTER TABLE session_message_fragments
    ADD CONSTRAINT chk_session_message_fragments_exact_text
        CHECK (octet_length(convert_to(exact_utf8_text, 'UTF8')) > 0);

ALTER TABLE session_message_fragments
    DROP CONSTRAINT IF EXISTS chk_session_message_fragments_digest_matches_text;

ALTER TABLE session_message_fragments
    ADD CONSTRAINT chk_session_message_fragments_digest_matches_text
        CHECK (content_digest = encode(sha256(convert_to(exact_utf8_text, 'UTF8')), 'hex'));

ALTER TABLE session_message_fragments
    DROP CONSTRAINT IF EXISTS chk_session_message_fragments_accepted_output;

ALTER TABLE session_message_fragments
    ADD CONSTRAINT chk_session_message_fragments_accepted_output
        CHECK (
            accepted_agent_output_id IS NULL
            OR accepted_agent_output_id = message_id);

ALTER TABLE session_message_fragments
    DROP CONSTRAINT IF EXISTS fk_session_message_fragments_message_ownership;

ALTER TABLE session_message_fragments
    ADD CONSTRAINT fk_session_message_fragments_message_ownership
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            message_id)
        REFERENCES session_messages (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            message_id);

ALTER TABLE session_message_fragments
    DROP CONSTRAINT IF EXISTS fk_session_message_fragments_decision;

ALTER TABLE session_message_fragments
    ADD CONSTRAINT fk_session_message_fragments_decision
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            driving_decision_id)
        REFERENCES session_decisions (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            decision_id);

ALTER TABLE session_message_fragments
    DROP CONSTRAINT IF EXISTS fk_session_message_fragments_invocation;

ALTER TABLE session_message_fragments
    ADD CONSTRAINT fk_session_message_fragments_invocation
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            driving_invocation_id)
        REFERENCES session_invocations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id);

ALTER TABLE session_message_fragments
    DROP CONSTRAINT IF EXISTS fk_session_message_fragments_accepted_output;

ALTER TABLE session_message_fragments
    ADD CONSTRAINT fk_session_message_fragments_accepted_output
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            accepted_agent_output_id)
        REFERENCES session_decision_output_validations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_output_id);
