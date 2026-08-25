-- Additive repair after shipped 0057/0058: drop the unique index on disposition
-- audit facts if present, and give accepted-version reconstruction precedence
-- over intake-derived incomplete rows (including accepted intakes that still
-- retain intake items). Do not edit 0001-0058.

DROP INDEX IF EXISTS uq_submissions_artifact_dispositions_artifact;

UPDATE submissions_durable_work AS work
SET work_kind = 'cleanup_accepted',
    enrollment_id = COALESCE(work.enrollment_id, version.enrollment_id),
    version_id = COALESCE(work.version_id, item.version_id),
    failure_reason = 'exact_artifact_version_unavailable'
FROM submissions_accepted_version_items AS item
INNER JOIN submissions_accepted_versions AS version
    ON version.organization_id = item.organization_id
   AND version.version_id = item.version_id
WHERE work.organization_id = item.organization_id
  AND work.artifact_object_key = item.artifact_object_key
  AND work.status = 'failed'
  AND work.failure_reason IN (
      'exact_artifact_version_unavailable',
      'legacy_unversioned_reconstruction');
