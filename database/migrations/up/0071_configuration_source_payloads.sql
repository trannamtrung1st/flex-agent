-- Additive protected configuration-source payload store for new source versions.
-- Historical digest-only versions remain evaluation-ineligible. Do not edit 0001-0070.

CREATE TABLE configuration_source_payloads (
    organization_id UUID NOT NULL,
    configuration_source_id UUID NOT NULL,
    source_version_id UUID NOT NULL,
    content_digest CHAR(64) NOT NULL,
    canonical_utf8 BYTEA NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, source_version_id),
    CONSTRAINT fk_configuration_source_payloads_version
        FOREIGN KEY (organization_id, configuration_source_id, source_version_id)
        REFERENCES configuration_source_versions (organization_id, configuration_source_id, id),
    CONSTRAINT fk_configuration_source_payloads_source
        FOREIGN KEY (organization_id, configuration_source_id)
        REFERENCES configuration_sources (organization_id, id),
    CONSTRAINT uq_configuration_source_payloads_digest
        UNIQUE (organization_id, configuration_source_id, content_digest),
    CONSTRAINT chk_configuration_source_payloads_digest_lowercase
        CHECK (content_digest = lower(content_digest)),
    CONSTRAINT chk_configuration_source_payloads_bytes
        CHECK (octet_length(canonical_utf8) BETWEEN 1 AND 1048576)
);

CREATE TABLE configuration_source_payload_holds (
    organization_id UUID NOT NULL,
    hold_id UUID NOT NULL,
    source_version_id UUID NOT NULL,
    reason_code TEXT NOT NULL,
    active BOOLEAN NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, hold_id),
    CONSTRAINT fk_configuration_source_payload_holds_payload
        FOREIGN KEY (organization_id, source_version_id)
        REFERENCES configuration_source_payloads (organization_id, source_version_id)
        ON DELETE CASCADE,
    CONSTRAINT chk_configuration_source_payload_holds_reason
        CHECK (reason_code = 'legal_hold')
);

CREATE INDEX ix_configuration_source_payload_holds_version
    ON configuration_source_payload_holds (organization_id, source_version_id)
    WHERE active;

CREATE OR REPLACE FUNCTION reject_configuration_source_payload_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'UPDATE'
        OR current_setting('flex_agent.allow_payload_disposition', true) IS DISTINCT FROM 'true' THEN
        RAISE EXCEPTION 'configuration_source_payloads are immutable';
    END IF;
    RETURN OLD;
END;
$$;

CREATE TRIGGER trg_configuration_source_payloads_no_update
    BEFORE UPDATE ON configuration_source_payloads
    FOR EACH ROW
    EXECUTE FUNCTION reject_configuration_source_payload_mutation();

CREATE TRIGGER trg_configuration_source_payloads_no_delete
    BEFORE DELETE ON configuration_source_payloads
    FOR EACH ROW
    EXECUTE FUNCTION reject_configuration_source_payload_mutation();

CREATE OR REPLACE FUNCTION dispose_configuration_source_payload(
    p_organization_id UUID,
    p_source_version_id UUID)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM configuration_source_payload_holds
        WHERE organization_id = p_organization_id
          AND source_version_id = p_source_version_id
          AND active)
    THEN
        RAISE EXCEPTION 'configuration_source_payloads have an active legal hold';
    END IF;

    PERFORM set_config('flex_agent.allow_payload_disposition', 'true', true);
    DELETE FROM configuration_source_payloads
    WHERE organization_id = p_organization_id
      AND source_version_id = p_source_version_id;
    PERFORM set_config('flex_agent.allow_payload_disposition', 'false', true);
END;
$$;
