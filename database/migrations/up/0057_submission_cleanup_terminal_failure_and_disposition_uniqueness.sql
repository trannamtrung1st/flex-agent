-- Additive repair after shipped 0055: keep unbackfillable cleanup as terminal
-- provenance, reconstruct that intent from remaining unversioned items when the
-- work row was deleted, and make artifact dispositions unique. Do not edit 0001-0056.

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

INSERT INTO submissions_durable_work (
    organization_id, work_id, work_kind, enrollment_id, intake_id, version_id,
    status, attempt_count, available_at, lease_until, artifact_object_key,
    artifact_version_id, created_at, failure_reason)
SELECT DISTINCT ON (item.organization_id, item.artifact_object_key)
    item.organization_id,
    gen_random_uuid(),
    'cleanup_incomplete',
    NULL,
    item.intake_id,
    NULL,
    'failed',
    0,
    CLOCK_TIMESTAMP(),
    NULL,
    item.artifact_object_key,
    NULL,
    CLOCK_TIMESTAMP(),
    'exact_artifact_version_unavailable'
FROM submissions_intake_items AS item
WHERE item.artifact_object_key IS NOT NULL
  AND btrim(item.artifact_object_key) <> ''
  AND (item.artifact_version_id IS NULL OR btrim(item.artifact_version_id) = '')
  AND NOT EXISTS (
      SELECT 1
      FROM submissions_durable_work AS work
      WHERE work.organization_id = item.organization_id
        AND work.artifact_object_key = item.artifact_object_key)
  AND NOT EXISTS (
      SELECT 1
      FROM submissions_artifact_dispositions AS disposed
      WHERE disposed.organization_id = item.organization_id
        AND disposed.artifact_object_key = item.artifact_object_key)
ORDER BY item.organization_id, item.artifact_object_key, item.item_id;

INSERT INTO submissions_durable_work (
    organization_id, work_id, work_kind, enrollment_id, intake_id, version_id,
    status, attempt_count, available_at, lease_until, artifact_object_key,
    artifact_version_id, created_at, failure_reason)
SELECT DISTINCT ON (item.organization_id, item.artifact_object_key)
    item.organization_id,
    gen_random_uuid(),
    'cleanup_accepted',
    NULL,
    NULL,
    item.version_id,
    'failed',
    0,
    CLOCK_TIMESTAMP(),
    NULL,
    item.artifact_object_key,
    NULL,
    CLOCK_TIMESTAMP(),
    'exact_artifact_version_unavailable'
FROM submissions_accepted_version_items AS item
WHERE item.artifact_object_key IS NOT NULL
  AND btrim(item.artifact_object_key) <> ''
  AND (item.artifact_version_id IS NULL OR btrim(item.artifact_version_id) = '')
  AND NOT EXISTS (
      SELECT 1
      FROM submissions_durable_work AS work
      WHERE work.organization_id = item.organization_id
        AND work.artifact_object_key = item.artifact_object_key)
  AND NOT EXISTS (
      SELECT 1
      FROM submissions_artifact_dispositions AS disposed
      WHERE disposed.organization_id = item.organization_id
        AND disposed.artifact_object_key = item.artifact_object_key)
ORDER BY item.organization_id, item.artifact_object_key, item.item_id;

CREATE UNIQUE INDEX uq_submissions_artifact_dispositions_artifact
    ON submissions_artifact_dispositions (organization_id, artifact_object_key);
