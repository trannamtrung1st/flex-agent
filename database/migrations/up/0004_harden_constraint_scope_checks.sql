-- Harden constraint repair checks to target-table relations after original 0003.
-- ADR-010 artifact 4 review follow-up; UTC-ordered; do not edit after merge.

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uq_configuration_source_versions_org_source_version'
          AND conrelid = 'configuration_source_versions'::regclass)
    THEN
        ALTER TABLE configuration_source_versions
            ADD CONSTRAINT uq_configuration_source_versions_org_source_version
            UNIQUE (organization_id, configuration_source_id, id);
    END IF;
END;
$$;

ALTER TABLE configuration_source_version_idempotency
    DROP CONSTRAINT IF EXISTS fk_configuration_source_version_idempotency_version;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_configuration_source_version_idempotency_source_version'
          AND conrelid = 'configuration_source_version_idempotency'::regclass)
    THEN
        ALTER TABLE configuration_source_version_idempotency
            DROP CONSTRAINT IF EXISTS fk_configuration_source_version_idempotency_source_version;

        ALTER TABLE configuration_source_version_idempotency
            ADD CONSTRAINT fk_configuration_source_version_idempotency_source_version
                FOREIGN KEY (organization_id, configuration_source_id, version_id)
                REFERENCES configuration_source_versions (organization_id, configuration_source_id, id);
    END IF;
END;
$$;
