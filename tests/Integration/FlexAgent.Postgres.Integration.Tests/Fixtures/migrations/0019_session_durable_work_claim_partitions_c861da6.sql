-- Compact Organization/Activity claim-fairness state. Do not rewrite frozen 0005-0018.
-- last_claimed_at advances when a partition receives a claim, not only when
-- work completes, so in-flight work cannot starve another partition.

CREATE TABLE session_durable_work_claim_partitions (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    last_claimed_at TIMESTAMPTZ NOT NULL,
    last_claimed_work_id UUID NOT NULL,
    PRIMARY KEY (organization_id, activity_id)
);

COMMENT ON TABLE session_durable_work_claim_partitions IS
    'Least-recently-claimed Organization/Activity scheduler state for durable invocation work.';
