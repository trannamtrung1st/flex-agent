-- Persist an immutable Turn creation order so reload does not sort by turn_id.
-- created_session_sequence is stamped once and preserved on UPDATE.
-- UTC-ordered; additive after frozen 0007.

ALTER TABLE session_turns
    ADD COLUMN IF NOT EXISTS created_session_sequence BIGINT;

UPDATE session_turns AS turn
SET created_session_sequence = ranked.created_session_sequence
FROM (
    SELECT
        organization_id,
        session_id,
        turn_id,
        (ROW_NUMBER() OVER (
            PARTITION BY organization_id, session_id
            ORDER BY last_committed_at, turn_id) - 1) AS created_session_sequence
    FROM session_turns
) AS ranked
WHERE turn.organization_id = ranked.organization_id
  AND turn.session_id = ranked.session_id
  AND turn.turn_id = ranked.turn_id
  AND turn.created_session_sequence IS NULL;

ALTER TABLE session_turns
    ALTER COLUMN created_session_sequence SET NOT NULL;

ALTER TABLE session_turns
    DROP CONSTRAINT IF EXISTS chk_session_turns_created_sequence;

ALTER TABLE session_turns
    ADD CONSTRAINT chk_session_turns_created_sequence
        CHECK (created_session_sequence >= 0);

ALTER TABLE session_turns
    DROP CONSTRAINT IF EXISTS uq_session_turns_created_sequence;

ALTER TABLE session_turns
    ADD CONSTRAINT uq_session_turns_created_sequence
        UNIQUE (organization_id, session_id, created_session_sequence);

CREATE OR REPLACE FUNCTION stamp_session_turn_created_sequence()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        IF NEW.created_session_sequence IS NULL THEN
            RAISE EXCEPTION 'session_turns.created_session_sequence is required';
        END IF;
    ELSE
        NEW.created_session_sequence := OLD.created_session_sequence;
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_session_turns_stamp_created_sequence ON session_turns;

CREATE TRIGGER trg_session_turns_stamp_created_sequence
    BEFORE INSERT OR UPDATE ON session_turns
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_turn_created_sequence();
