-- Persist the Session sequence and UTC time allocated at Agent Message
-- seal so reconnect replay can emit a distinct complete-event cursor.
-- Frozen 0005-0014 unchanged.

ALTER TABLE session_messages
    ADD COLUMN IF NOT EXISTS sealed_session_sequence BIGINT NULL;

ALTER TABLE session_messages
    ADD COLUMN IF NOT EXISTS sealed_at TIMESTAMPTZ NULL;

ALTER TABLE session_messages
    DROP CONSTRAINT IF EXISTS chk_session_messages_sealed_sequence;

ALTER TABLE session_messages
    ADD CONSTRAINT chk_session_messages_sealed_sequence
        CHECK (
            (sealed_session_sequence IS NULL AND sealed_at IS NULL)
            OR (sealed_session_sequence > 0 AND sealed_at IS NOT NULL));
