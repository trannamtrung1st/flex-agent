-- Repair migration for upgrades that applied 0002 before idempotency backfill shipped.
-- ADR-010 artifact 4 review follow-up; UTC-ordered; do not edit after merge.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uq_configuration_source_versions_org_source_version')
    THEN
        ALTER TABLE configuration_source_versions
            ADD CONSTRAINT uq_configuration_source_versions_org_source_version
            UNIQUE (organization_id, configuration_source_id, id);
    END IF;
END;
$$;

INSERT INTO configuration_source_version_idempotency (
    organization_id,
    configuration_source_id,
    action,
    idempotency_key,
    version_id,
    payload_fingerprint,
    created_at)
SELECT
    organization_id,
    configuration_source_id,
    'configuration_source_version.register',
    idempotency_key,
    id,
    encode(
        digest(
            procedure_id || '|' || schema_version || '|' || content_digest,
            'sha256'),
        'hex'),
    created_at
FROM configuration_source_versions
ON CONFLICT DO NOTHING;

ALTER TABLE configuration_source_version_idempotency
    DROP CONSTRAINT IF EXISTS fk_configuration_source_version_idempotency_version;

ALTER TABLE configuration_source_version_idempotency
    DROP CONSTRAINT IF EXISTS fk_configuration_source_version_idempotency_source_version;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_configuration_source_version_idempotency_source_version')
    THEN
        ALTER TABLE configuration_source_version_idempotency
            ADD CONSTRAINT fk_configuration_source_version_idempotency_source_version
                FOREIGN KEY (organization_id, configuration_source_id, version_id)
                REFERENCES configuration_source_versions (organization_id, configuration_source_id, id);
    END IF;
END;
$$;
