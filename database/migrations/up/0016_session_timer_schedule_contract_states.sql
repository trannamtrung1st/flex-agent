-- Map contract timer-lane states onto 0005 schedule rows without rewriting
-- frozen 0005-0015. Persist remaining active delay, revision ordinal, and
-- lossless lane_state. 0005 state keeps pending|claimed|fired|replaced|cancelled.
-- superseded -> replaced; expired -> cancelled.
-- Pending remaining is reconstructed from fire_at - last_committed_at.
-- source_decision_id implies agent_recommendation; created_at uses
-- last_committed_at because 0005 has no original creation timestamp.

ALTER TABLE session_timer_schedules
    ADD COLUMN IF NOT EXISTS schedule_revision_ordinal BIGINT NULL;

ALTER TABLE session_timer_schedules
    ADD COLUMN IF NOT EXISTS remaining_active_seconds INTEGER NULL;

ALTER TABLE session_timer_schedules
    ADD COLUMN IF NOT EXISTS remaining_since TIMESTAMPTZ NULL;

ALTER TABLE session_timer_schedules
    ADD COLUMN IF NOT EXISTS requested_by_category TEXT NULL;

ALTER TABLE session_timer_schedules
    ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ NULL;

ALTER TABLE session_timer_schedules
    ADD COLUMN IF NOT EXISTS fired_invocation_id TEXT NULL;

ALTER TABLE session_timer_schedules
    ADD COLUMN IF NOT EXISTS lane_state TEXT NULL;

UPDATE session_timer_schedules AS schedule
SET schedule_revision_ordinal = ranked.ordinal
FROM (
    SELECT
        ctid,
        ROW_NUMBER() OVER (
            PARTITION BY organization_id, session_id
            ORDER BY last_committed_at, schedule_revision) AS ordinal
    FROM session_timer_schedules
    WHERE schedule_revision_ordinal IS NULL
) AS ranked
WHERE schedule.ctid = ranked.ctid;

UPDATE session_timer_schedules
SET
    remaining_active_seconds = COALESCE(
        remaining_active_seconds,
        CASE
            WHEN state IN ('pending', 'claimed') AND fire_at IS NOT NULL
                THEN GREATEST(
                    0,
                    FLOOR(EXTRACT(EPOCH FROM (fire_at - last_committed_at)))::INTEGER)
            ELSE 0
        END),
    remaining_since = COALESCE(remaining_since, last_committed_at),
    requested_by_category = COALESCE(
        requested_by_category,
        CASE
            WHEN source_decision_id IS NOT NULL THEN 'agent_recommendation'
            ELSE 'default_cadence'
        END),
    created_at = COALESCE(created_at, last_committed_at),
    lane_state = COALESCE(
        lane_state,
        CASE state
            WHEN 'replaced' THEN 'superseded'
            ELSE state
        END);

ALTER TABLE session_timer_schedules
    DROP CONSTRAINT IF EXISTS chk_session_timer_schedules_lane_state_map;

ALTER TABLE session_timer_schedules
    ADD CONSTRAINT chk_session_timer_schedules_lane_state_map
        CHECK (
            lane_state IS NULL
            OR (
                (lane_state IN ('pending', 'claimed', 'fired') AND state = lane_state)
                OR (lane_state = 'superseded' AND state = 'replaced')
                OR (lane_state = 'cancelled' AND state = 'cancelled')
                OR (lane_state = 'expired' AND state = 'cancelled')
            ));

ALTER TABLE session_timer_schedules
    DROP CONSTRAINT IF EXISTS chk_session_timer_schedules_remaining;

ALTER TABLE session_timer_schedules
    ADD CONSTRAINT chk_session_timer_schedules_remaining
        CHECK (remaining_active_seconds IS NULL OR remaining_active_seconds >= 0);

ALTER TABLE session_timer_schedules
    DROP CONSTRAINT IF EXISTS chk_session_timer_schedules_requested_by;

ALTER TABLE session_timer_schedules
    ADD CONSTRAINT chk_session_timer_schedules_requested_by
        CHECK (
            requested_by_category IS NULL
            OR requested_by_category IN (
                'default_cadence',
                'agent_recommendation',
                'successor_after_fire'));

ALTER TABLE session_timer_schedules
    DROP CONSTRAINT IF EXISTS uq_session_timer_schedules_ordinal;

ALTER TABLE session_timer_schedules
    ADD CONSTRAINT uq_session_timer_schedules_ordinal
        UNIQUE (organization_id, session_id, schedule_revision_ordinal);
