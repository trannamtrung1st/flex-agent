-- Additive repair after 0059: restore disposition audit facts parked by 0056a
-- so historical duplicates survive the immutable 0057 unique index. Do not
-- edit 0001-0059.

INSERT INTO submissions_artifact_dispositions (
    organization_id, disposition_id, work_kind, artifact_object_key, disposed_at)
SELECT organization_id,
    disposition_id,
    work_kind,
    artifact_object_key,
    disposed_at
FROM submissions_artifact_disposition_upgrade_overflow;

DROP TABLE submissions_artifact_disposition_upgrade_overflow;
