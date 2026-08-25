-- Additive exact artifact version on durable cleanup work.
-- Do not edit 0001-0053.

ALTER TABLE submissions_durable_work
    ADD COLUMN artifact_version_id TEXT NULL;
