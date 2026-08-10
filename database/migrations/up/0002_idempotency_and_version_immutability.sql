-- Immutable one-time migration: idempotency records, version immutability enforcement.
-- ADR-010 artifact 4 follow-up; UTC-ordered; do not edit after merge.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE configuration_source_version_idempotency (
    organization_id UUID NOT NULL,
    configuration_source_id UUID NOT NULL,
    action TEXT NOT NULL,
    idempotency_key TEXT NOT NULL,
    version_id UUID NOT NULL,
    payload_fingerprint CHAR(64) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, configuration_source_id, action, idempotency_key),
    CONSTRAINT fk_configuration_source_version_idempotency_source
        FOREIGN KEY (organization_id, configuration_source_id)
        REFERENCES configuration_sources (organization_id, id)
);

ALTER TABLE configuration_source_versions
    ADD CONSTRAINT uq_configuration_source_versions_org_source_version
    UNIQUE (organization_id, configuration_source_id, id);

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
    ADD CONSTRAINT fk_configuration_source_version_idempotency_source_version
        FOREIGN KEY (organization_id, configuration_source_id, version_id)
        REFERENCES configuration_source_versions (organization_id, configuration_source_id, id);

ALTER TABLE configuration_source_versions
    DROP CONSTRAINT IF EXISTS uq_configuration_source_versions_idempotency;

CREATE OR REPLACE FUNCTION reject_configuration_source_version_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'configuration_source_versions are immutable';
END;
$$;

CREATE TRIGGER trg_configuration_source_versions_no_update
    BEFORE UPDATE ON configuration_source_versions
    FOR EACH ROW
    EXECUTE FUNCTION reject_configuration_source_version_mutation();

CREATE TRIGGER trg_configuration_source_versions_no_delete
    BEFORE DELETE ON configuration_source_versions
    FOR EACH ROW
    EXECUTE FUNCTION reject_configuration_source_version_mutation();
