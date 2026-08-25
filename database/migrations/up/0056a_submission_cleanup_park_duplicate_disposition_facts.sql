-- Additive pre-0057 repair: park extra disposition audit facts for one artifact
-- so the shipped 0057 unique index can apply. 0060 restores the parked rows
-- after 0059 drops that index. Do not edit 0001-0056.

CREATE TABLE submissions_artifact_disposition_upgrade_overflow (
    organization_id UUID NOT NULL,
    disposition_id UUID NOT NULL,
    work_kind TEXT NOT NULL,
    artifact_object_key TEXT NOT NULL,
    disposed_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, disposition_id)
);

INSERT INTO submissions_artifact_disposition_upgrade_overflow (
    organization_id, disposition_id, work_kind, artifact_object_key, disposed_at)
SELECT d.organization_id,
    d.disposition_id,
    d.work_kind,
    d.artifact_object_key,
    d.disposed_at
FROM submissions_artifact_dispositions AS d
WHERE NOT EXISTS (
    SELECT 1
    FROM (
        SELECT DISTINCT ON (organization_id, artifact_object_key)
            organization_id,
            disposition_id
        FROM submissions_artifact_dispositions
        ORDER BY organization_id, artifact_object_key, disposed_at, disposition_id
    ) AS keeper
    WHERE keeper.organization_id = d.organization_id
      AND keeper.disposition_id = d.disposition_id);

DELETE FROM submissions_artifact_dispositions AS d
    USING submissions_artifact_disposition_upgrade_overflow AS overflow
WHERE d.organization_id = overflow.organization_id
  AND d.disposition_id = overflow.disposition_id;
