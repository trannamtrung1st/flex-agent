-- Additive durable validation/reconciliation/cleanup work for Submission intake.
-- Do not edit 0001-0050.

CREATE TABLE submissions_durable_work (
    organization_id UUID NOT NULL,
    work_id UUID NOT NULL,
    work_kind TEXT NOT NULL,
    enrollment_id UUID NULL,
    intake_id UUID NULL,
    version_id UUID NULL,
    status TEXT NOT NULL,
    attempt_count INTEGER NOT NULL,
    available_at TIMESTAMPTZ NOT NULL,
    lease_until TIMESTAMPTZ NULL,
    artifact_object_key TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, work_id),
    CONSTRAINT chk_submissions_durable_work_kind
        CHECK (work_kind IN ('cleanup_incomplete', 'cleanup_rejected', 'cleanup_orphan')),
    CONSTRAINT chk_submissions_durable_work_status
        CHECK (status IN ('pending', 'leased', 'completed', 'failed')),
    CONSTRAINT chk_submissions_durable_work_attempts
        CHECK (attempt_count >= 0)
);

CREATE INDEX ix_submissions_durable_work_claimable
    ON submissions_durable_work (available_at, created_at)
    WHERE status IN ('pending', 'leased');

CREATE UNIQUE INDEX uq_submissions_durable_work_pending_artifact
    ON submissions_durable_work (organization_id, work_kind, intake_id, artifact_object_key)
    WHERE status IN ('pending', 'leased') AND artifact_object_key IS NOT NULL;
