-- Additive accepted-payload cleanup work kind after Activity-closure retention.
-- Do not edit 0001-0052.

ALTER TABLE submissions_durable_work
    DROP CONSTRAINT chk_submissions_durable_work_kind;
ALTER TABLE submissions_durable_work
    ADD CONSTRAINT chk_submissions_durable_work_kind
        CHECK (work_kind IN ('cleanup_incomplete', 'cleanup_rejected', 'cleanup_orphan', 'cleanup_accepted'));

ALTER TABLE submissions_artifact_dispositions
    DROP CONSTRAINT chk_submissions_artifact_dispositions_kind;
ALTER TABLE submissions_artifact_dispositions
    ADD CONSTRAINT chk_submissions_artifact_dispositions_kind
        CHECK (work_kind IN ('cleanup_incomplete', 'cleanup_rejected', 'cleanup_orphan', 'cleanup_accepted'));

CREATE UNIQUE INDEX uq_submissions_durable_work_pending_accepted_artifact
    ON submissions_durable_work (organization_id, artifact_object_key)
    WHERE status IN ('pending', 'leased')
      AND work_kind = 'cleanup_accepted'
      AND artifact_object_key IS NOT NULL;
