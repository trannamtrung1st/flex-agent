-- Additive backfill of exact artifact versions on already-queued cleanup work,
-- terminal failed provenance for unbackfillable artifact jobs, and a persisted
-- accepted-cleanup scan cursor. Do not edit 0001-0054.

UPDATE submissions_durable_work AS work
SET artifact_version_id = item.artifact_version_id
FROM submissions_intake_items AS item
WHERE work.organization_id = item.organization_id
  AND work.intake_id = item.intake_id
  AND work.artifact_object_key = item.artifact_object_key
  AND work.status IN ('pending', 'leased')
  AND work.artifact_object_key IS NOT NULL
  AND (work.artifact_version_id IS NULL OR btrim(work.artifact_version_id) = '')
  AND item.artifact_version_id IS NOT NULL
  AND btrim(item.artifact_version_id) <> '';

UPDATE submissions_durable_work AS work
SET artifact_version_id = item.artifact_version_id
FROM submissions_accepted_version_items AS item
WHERE work.organization_id = item.organization_id
  AND work.artifact_object_key = item.artifact_object_key
  AND (work.version_id IS NULL OR work.version_id = item.version_id)
  AND work.status IN ('pending', 'leased')
  AND work.artifact_object_key IS NOT NULL
  AND (work.artifact_version_id IS NULL OR btrim(work.artifact_version_id) = '')
  AND btrim(item.artifact_version_id) <> '';

ALTER TABLE submissions_durable_work
    ADD COLUMN failure_reason TEXT NULL;

UPDATE submissions_durable_work
SET status = 'failed',
    lease_until = NULL,
    failure_reason = 'exact_artifact_version_unavailable'
WHERE status IN ('pending', 'leased')
  AND artifact_object_key IS NOT NULL
  AND work_kind IN ('cleanup_incomplete', 'cleanup_rejected', 'cleanup_orphan', 'cleanup_accepted')
  AND (artifact_version_id IS NULL OR btrim(artifact_version_id) = '');

ALTER TABLE submissions_durable_work
    ADD CONSTRAINT chk_submissions_durable_work_exact_artifact_version
        CHECK (
            status NOT IN ('pending', 'leased')
            OR artifact_object_key IS NULL
            OR char_length(btrim(coalesce(artifact_version_id, ''))) > 0
        );

CREATE TABLE submissions_accepted_cleanup_scan (
    singleton_key SMALLINT PRIMARY KEY,
    after_accepted_at TIMESTAMPTZ NULL,
    after_version_id UUID NULL,
    after_item_id UUID NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_submissions_accepted_cleanup_scan_singleton
        CHECK (singleton_key = 1),
    CONSTRAINT chk_submissions_accepted_cleanup_scan_cursor
        CHECK (
            (after_accepted_at IS NULL AND after_version_id IS NULL AND after_item_id IS NULL)
            OR (after_accepted_at IS NOT NULL AND after_version_id IS NOT NULL AND after_item_id IS NOT NULL)
        )
);

INSERT INTO submissions_accepted_cleanup_scan (singleton_key, updated_at)
VALUES (1, CLOCK_TIMESTAMP());
