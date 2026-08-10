-- Immutable one-time migration: authorization and configuration foundation schema.
-- ADR-010 artifact 4; UTC-ordered; do not edit after merge.

CREATE TABLE organizations (
    id UUID PRIMARY KEY,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE actors (
    id UUID PRIMARY KEY,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE actor_organization_grants (
    organization_id UUID NOT NULL REFERENCES organizations (id),
    actor_id UUID NOT NULL REFERENCES actors (id),
    relationship_version BIGINT NOT NULL,
    granted_action TEXT NOT NULL,
    revoked_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, actor_id, granted_action)
);

CREATE INDEX ix_actor_organization_grants_actor
    ON actor_organization_grants (actor_id, organization_id)
    WHERE revoked_at IS NULL;

CREATE TABLE configuration_sources (
    id UUID NOT NULL,
    organization_id UUID NOT NULL REFERENCES organizations (id),
    source_kind TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, id)
);

CREATE TABLE configuration_source_versions (
    id UUID NOT NULL,
    organization_id UUID NOT NULL,
    configuration_source_id UUID NOT NULL,
    schema_version TEXT NOT NULL,
    procedure_id TEXT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    idempotency_key TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, id),
    CONSTRAINT fk_configuration_source_versions_source
        FOREIGN KEY (organization_id, configuration_source_id)
        REFERENCES configuration_sources (organization_id, id),
    CONSTRAINT uq_configuration_source_versions_idempotency
        UNIQUE (organization_id, configuration_source_id, idempotency_key),
    CONSTRAINT uq_configuration_source_versions_digest
        UNIQUE (organization_id, configuration_source_id, content_digest),
    CONSTRAINT chk_configuration_source_versions_digest_lowercase
        CHECK (content_digest = lower(content_digest))
);

CREATE TABLE audit_events (
    event_id UUID PRIMARY KEY,
    organization_id UUID NOT NULL REFERENCES organizations (id),
    event_schema_version TEXT NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    sequence_number BIGSERIAL NOT NULL,
    correlation_id UUID NOT NULL,
    actor_type TEXT NOT NULL,
    actor_id UUID NOT NULL,
    action TEXT NOT NULL,
    resource_type TEXT NOT NULL,
    resource_id UUID NOT NULL,
    outcome TEXT NOT NULL,
    reason_code TEXT NULL,
    relationship_version BIGINT NULL,
    source_channel TEXT NOT NULL,
    payload_digest CHAR(64) NULL
);

CREATE INDEX ix_audit_events_organization_sequence
    ON audit_events (organization_id, sequence_number);

CREATE OR REPLACE FUNCTION reject_audit_event_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'audit_events are append-only';
END;
$$;

CREATE TRIGGER trg_audit_events_no_update
    BEFORE UPDATE ON audit_events
    FOR EACH ROW
    EXECUTE FUNCTION reject_audit_event_mutation();

CREATE TRIGGER trg_audit_events_no_delete
    BEFORE DELETE ON audit_events
    FOR EACH ROW
    EXECUTE FUNCTION reject_audit_event_mutation();

CREATE TABLE outbox_items (
    id UUID PRIMARY KEY,
    organization_id UUID NOT NULL REFERENCES organizations (id),
    event_type TEXT NOT NULL,
    aggregate_type TEXT NOT NULL,
    aggregate_id UUID NOT NULL,
    correlation_id UUID NOT NULL,
    payload_digest CHAR(64) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    processed_at TIMESTAMPTZ NULL
);

CREATE INDEX ix_outbox_items_unprocessed
    ON outbox_items (organization_id, created_at)
    WHERE processed_at IS NULL;
