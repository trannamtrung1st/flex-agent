-- Persist an immutable admission timestamp for cooldown rehydration.
-- last_committed_at remains the last mutation and is restamped on UPDATE.
-- UTC-ordered; additive after frozen 0006.

ALTER TABLE session_invocations
    ADD COLUMN IF NOT EXISTS admitted_at TIMESTAMPTZ;

UPDATE session_invocations
SET admitted_at = last_committed_at
WHERE admitted_at IS NULL;

ALTER TABLE session_invocations
    ALTER COLUMN admitted_at SET NOT NULL;

CREATE OR REPLACE FUNCTION stamp_session_invocation_admitted_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        NEW.admitted_at := clock_timestamp();
    ELSE
        NEW.admitted_at := OLD.admitted_at;
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_session_invocations_stamp_admitted_at ON session_invocations;

CREATE TRIGGER trg_session_invocations_stamp_admitted_at
    BEFORE INSERT OR UPDATE ON session_invocations
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_invocation_admitted_at();
