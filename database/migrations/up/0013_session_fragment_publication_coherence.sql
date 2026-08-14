-- Bind fragment audit columns to the parent Agent Message publication
-- identity, require Agent turn/slot, and keep 0012 frozen. Same-Session
-- cross-link to another Invocation/Decision/attempt/turn is rejected.
-- UTC-ordered; additive after applied 0012.

ALTER TABLE session_messages
    ADD COLUMN IF NOT EXISTS response_slot_id TEXT NULL;

UPDATE session_messages AS messages
SET response_slot_id = fragments.response_slot_id
FROM session_message_fragments AS fragments
WHERE messages.organization_id = fragments.organization_id
  AND messages.session_id = fragments.session_id
  AND messages.message_id = fragments.message_id
  AND messages.author_type = 'agent'
  AND messages.response_slot_id IS NULL
  AND fragments.fragment_ordinal = 1;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM session_messages
        WHERE author_type = 'agent'
          AND (turn_id IS NULL OR response_slot_id IS NULL)
    ) THEN
        RAISE EXCEPTION 'agent session_messages require turn_id and response_slot_id before 0013';
    END IF;
END
$$;

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
                AND assembled_content_digest IS NULL
                AND response_slot_id IS NULL)
            OR (author_type = 'agent'
                AND generation_attempt_id IS NOT NULL
                AND driving_invocation_id IS NOT NULL
                AND driving_decision_id IS NOT NULL
                AND turn_id IS NOT NULL
                AND response_slot_id IS NOT NULL
                AND (accepted_agent_output_id IS NULL
                    OR accepted_agent_output_id = message_id)));

ALTER TABLE session_messages
    DROP CONSTRAINT IF EXISTS uq_session_messages_publication_identity;

ALTER TABLE session_messages
    ADD CONSTRAINT uq_session_messages_publication_identity
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            message_id,
            driving_invocation_id,
            driving_decision_id,
            generation_attempt_id,
            turn_id,
            response_slot_id);

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
    ALTER COLUMN generation_attempt_id SET NOT NULL;

ALTER TABLE session_message_fragments
    ALTER COLUMN turn_id SET NOT NULL;

ALTER TABLE session_message_fragments
    ALTER COLUMN response_slot_id SET NOT NULL;

ALTER TABLE session_message_fragments
    DROP CONSTRAINT IF EXISTS fk_session_message_fragments_publication;

ALTER TABLE session_message_fragments
    ADD CONSTRAINT fk_session_message_fragments_publication
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            message_id,
            driving_invocation_id,
            driving_decision_id,
            generation_attempt_id,
            turn_id,
            response_slot_id)
        REFERENCES session_messages (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            message_id,
            driving_invocation_id,
            driving_decision_id,
            generation_attempt_id,
            turn_id,
            response_slot_id);
