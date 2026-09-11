-- Persist criterion identity on provider artifacts for complete provenance reconciliation.

ALTER TABLE evaluation_provider_artifacts
    ADD COLUMN criterion_id TEXT,
    ADD COLUMN criterion_version TEXT;

UPDATE evaluation_provider_artifacts
SET criterion_id = COALESCE(criterion_id, 'crit.legacy.placeholder'),
    criterion_version = COALESCE(criterion_version, 'crit.legacy.placeholder.v1')
WHERE criterion_id IS NULL
   OR criterion_version IS NULL;

ALTER TABLE evaluation_provider_artifacts
    ALTER COLUMN criterion_id SET NOT NULL,
    ALTER COLUMN criterion_version SET NOT NULL;

ALTER TABLE evaluation_provider_artifacts
    ADD CONSTRAINT chk_evaluation_provider_artifacts_criterion_id
        CHECK (
            criterion_id ~ '^[a-z][a-z0-9._-]{7,127}$'
            AND criterion_id !~ '(^|[._-])(latest|current)([._-]|$)'),
    ADD CONSTRAINT chk_evaluation_provider_artifacts_criterion_version
        CHECK (
            criterion_version ~ '^[a-z][a-z0-9._-]{7,127}$'
            AND criterion_version !~ '(^|[._-])(latest|current)([._-]|$)');
