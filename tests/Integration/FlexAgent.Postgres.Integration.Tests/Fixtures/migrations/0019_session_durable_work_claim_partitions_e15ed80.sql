-- Compact Organization/Activity claim-fairness state. Do not rewrite frozen 0005-0018.
-- last_claimed_at advances when a partition receives a claim, including claims
-- performed by older workers, because the scheduler row is maintained at the
-- table boundary. Seed currently claimed work so an in-flight partition is not
-- treated as never-served after upgrade. Databases that recorded c861da6's
-- earlier 0019 hash cannot apply this script in place and must be rebuilt.

CREATE TABLE session_durable_work_claim_partitions (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    last_claimed_at TIMESTAMPTZ NOT NULL,
    last_claimed_work_id UUID NOT NULL,
    PRIMARY KEY (organization_id, activity_id)
);

COMMENT ON TABLE session_durable_work_claim_partitions IS
    'Least-recently-claimed Organization/Activity scheduler state for durable invocation work.';

INSERT INTO session_durable_work_claim_partitions (
    organization_id, activity_id, last_claimed_at, last_claimed_work_id)
SELECT DISTINCT ON (organization_id, activity_id)
    organization_id,
    activity_id,
    last_committed_at,
    work_id
FROM session_durable_work
WHERE work_type = 'invocation.execute'
  AND state = 'claimed'
ORDER BY organization_id, activity_id, last_committed_at DESC, work_id DESC;

CREATE OR REPLACE FUNCTION stamp_session_durable_work_claim_partition()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO session_durable_work_claim_partitions (
        organization_id, activity_id, last_claimed_at, last_claimed_work_id)
    VALUES (
        NEW.organization_id,
        NEW.activity_id,
        clock_timestamp(),
        NEW.work_id)
    ON CONFLICT (organization_id, activity_id) DO UPDATE
    SET last_claimed_at = EXCLUDED.last_claimed_at,
        last_claimed_work_id = EXCLUDED.last_claimed_work_id;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_session_durable_work_claim_partition
    AFTER INSERT OR UPDATE ON session_durable_work
    FOR EACH ROW
    WHEN (NEW.work_type = 'invocation.execute' AND NEW.state = 'claimed')
    EXECUTE FUNCTION stamp_session_durable_work_claim_partition();
