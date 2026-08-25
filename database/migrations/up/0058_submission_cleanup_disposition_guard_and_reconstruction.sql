-- Additive repair after 0057: preserve duplicate disposition audit facts, add
-- a unique acquisition guard for future records, and correct reconstructed
-- cleanup provenance from joinable intake/accepted parents. Do not edit 0001-0057.

ALTER TABLE submissions_durable_work
    DROP CONSTRAINT chk_submissions_durable_work_kind;
ALTER TABLE submissions_durable_work
    ADD CONSTRAINT chk_submissions_durable_work_kind
        CHECK (work_kind IN (
            'cleanup_incomplete',
            'cleanup_rejected',
            'cleanup_orphan',
            'cleanup_accepted',
            'cleanup_legacy_reconstruction'));

UPDATE submissions_durable_work AS work
SET work_kind = CASE
        WHEN intake.status IN ('cancelled', 'rejected', 'failed') THEN 'cleanup_rejected'
        WHEN intake.intake_id IS NOT NULL THEN 'cleanup_incomplete'
        ELSE 'cleanup_legacy_reconstruction'
    END,
    enrollment_id = COALESCE(work.enrollment_id, intake.enrollment_id),
    failure_reason = CASE
        WHEN intake.intake_id IS NULL THEN 'legacy_unversioned_reconstruction'
        ELSE 'exact_artifact_version_unavailable'
    END
FROM submissions_intake_items AS item
LEFT JOIN submissions_intakes AS intake
    ON intake.organization_id = item.organization_id
   AND intake.intake_id = item.intake_id
WHERE work.organization_id = item.organization_id
  AND work.intake_id = item.intake_id
  AND work.artifact_object_key = item.artifact_object_key
  AND work.status = 'failed'
  AND work.failure_reason IN (
      'exact_artifact_version_unavailable',
      'legacy_unversioned_reconstruction');

UPDATE submissions_durable_work AS work
SET work_kind = CASE
        WHEN version.version_id IS NOT NULL THEN 'cleanup_accepted'
        ELSE 'cleanup_legacy_reconstruction'
    END,
    enrollment_id = COALESCE(work.enrollment_id, version.enrollment_id),
    failure_reason = CASE
        WHEN version.version_id IS NULL THEN 'legacy_unversioned_reconstruction'
        ELSE 'exact_artifact_version_unavailable'
    END
FROM submissions_accepted_version_items AS item
LEFT JOIN submissions_accepted_versions AS version
    ON version.organization_id = item.organization_id
   AND version.version_id = item.version_id
WHERE work.organization_id = item.organization_id
  AND work.artifact_object_key = item.artifact_object_key
  AND work.intake_id IS NULL
  AND (work.version_id IS NULL OR work.version_id = item.version_id)
  AND work.status = 'failed'
  AND work.failure_reason IN (
      'exact_artifact_version_unavailable',
      'legacy_unversioned_reconstruction');

CREATE TABLE submissions_artifact_disposition_guards (
    organization_id UUID NOT NULL,
    artifact_object_key TEXT NOT NULL,
    first_disposition_id UUID NOT NULL,
    acquired_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, artifact_object_key)
);

INSERT INTO submissions_artifact_disposition_guards (
    organization_id, artifact_object_key, first_disposition_id, acquired_at)
SELECT DISTINCT ON (organization_id, artifact_object_key)
    organization_id,
    artifact_object_key,
    disposition_id,
    disposed_at
FROM submissions_artifact_dispositions
ORDER BY organization_id, artifact_object_key, disposed_at, disposition_id;
