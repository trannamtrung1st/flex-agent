-- Backfill an open pause interval for Sessions already paused before the
-- hosted coordinator started writing session_pause_intervals. Additive
-- after 0067. Do not edit 0001-0067.

INSERT INTO session_pause_intervals (
    organization_id,
    activity_id,
    participant_id,
    attempt_id,
    session_id,
    pause_id,
    started_at,
    ended_at,
    last_committed_at)
SELECT
    runtime.organization_id,
    runtime.activity_id,
    runtime.participant_id,
    runtime.attempt_id,
    runtime.session_id,
    gen_random_uuid(),
    runtime.last_committed_at,
    NULL,
    runtime.last_committed_at
FROM session_runtimes AS runtime
WHERE runtime.lifecycle_state = 'paused'
  AND NOT EXISTS (
        SELECT 1
        FROM session_pause_intervals AS interval
        WHERE interval.organization_id = runtime.organization_id
          AND interval.session_id = runtime.session_id
          AND interval.ended_at IS NULL);
