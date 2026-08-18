-- Additive Worker workload-principal bindings and per-Session Invocation
-- execution-delegation envelopes (ADR-016). Do not rewrite frozen 0001-0024.
-- Historical Invocation work and Sessions keep a null envelope reference and
-- remain fail-closed; this script does not backfill authority.

CREATE TABLE service_principal_bindings (
    binding_id UUID PRIMARY KEY,
    authentication_profile TEXT NOT NULL,
    authentication_method TEXT NOT NULL,
    issuer TEXT NOT NULL,
    external_subject TEXT NOT NULL,
    client_identity TEXT NULL,
    expected_audience TEXT NOT NULL,
    service_actor_id UUID NOT NULL REFERENCES actors (id),
    service_purpose TEXT NOT NULL,
    effective_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ NULL,
    binding_version BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT chk_service_principal_bindings_profile
        CHECK (authentication_profile ~ '^[a-z][a-z0-9._]*$'),
    CONSTRAINT chk_service_principal_bindings_method
        CHECK (authentication_method ~ '^[a-z][a-z0-9._]*$'),
    CONSTRAINT chk_service_principal_bindings_purpose
        CHECK (char_length(service_purpose) BETWEEN 1 AND 128),
    CONSTRAINT chk_service_principal_bindings_version
        CHECK (binding_version >= 1)
);

CREATE UNIQUE INDEX uq_service_principal_bindings_active_principal
    ON service_principal_bindings (
        authentication_profile,
        issuer,
        external_subject,
        expected_audience)
    WHERE revoked_at IS NULL;

CREATE INDEX ix_service_principal_bindings_actor
    ON service_principal_bindings (service_actor_id)
    WHERE revoked_at IS NULL;

CREATE TABLE service_principal_binding_transitions (
    transition_id UUID PRIMARY KEY,
    binding_id UUID NOT NULL REFERENCES service_principal_bindings (binding_id),
    mutation_kind TEXT NOT NULL,
    previous_actor_id UUID NULL,
    new_actor_id UUID NOT NULL,
    previous_revoked_at TIMESTAMPTZ NULL,
    new_revoked_at TIMESTAMPTZ NULL,
    binding_version BIGINT NOT NULL,
    actor_id UUID NOT NULL,
    actor_type TEXT NOT NULL,
    reason TEXT NOT NULL,
    correlation_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT chk_service_principal_binding_transitions_kind
        CHECK (mutation_kind IN ('provision', 'revoke', 'replace')),
    CONSTRAINT chk_service_principal_binding_transitions_reason
        CHECK (char_length(reason) BETWEEN 1 AND 128)
);

CREATE OR REPLACE FUNCTION reject_service_principal_binding_transition_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'service_principal_binding_transitions are append-only';
END;
$$;

CREATE TRIGGER trg_service_principal_binding_transitions_no_update
    BEFORE UPDATE ON service_principal_binding_transitions
    FOR EACH ROW
    EXECUTE FUNCTION reject_service_principal_binding_transition_mutation();

CREATE TRIGGER trg_service_principal_binding_transitions_no_delete
    BEFORE DELETE ON service_principal_binding_transitions
    FOR EACH ROW
    EXECUTE FUNCTION reject_service_principal_binding_transition_mutation();

ALTER TABLE service_delegations
    DROP CONSTRAINT IF EXISTS chk_service_delegations_invocation_execute_expiry;

ALTER TABLE service_delegations
    ADD CONSTRAINT chk_service_delegations_invocation_execute_expiry
        CHECK (
            allowed_action <> 'session.invocation.execute'
            OR revoked_at IS NOT NULL
            OR (
                expires_at IS NOT NULL
                AND expires_at <= effective_at + INTERVAL '24 hours'
            )
        );

ALTER TABLE session_runtimes
    ADD COLUMN IF NOT EXISTS invocation_execute_delegation_id UUID NULL;

ALTER TABLE session_runtimes
    DROP CONSTRAINT IF EXISTS fk_session_runtimes_invocation_execute_delegation;

ALTER TABLE session_runtimes
    ADD CONSTRAINT fk_session_runtimes_invocation_execute_delegation
        FOREIGN KEY (invocation_execute_delegation_id)
        REFERENCES service_delegations (delegation_id);

ALTER TABLE session_durable_work
    ADD COLUMN IF NOT EXISTS invocation_execute_delegation_id UUID NULL;

ALTER TABLE session_durable_work
    DROP CONSTRAINT IF EXISTS fk_session_durable_work_invocation_execute_delegation;

ALTER TABLE session_durable_work
    ADD CONSTRAINT fk_session_durable_work_invocation_execute_delegation
        FOREIGN KEY (invocation_execute_delegation_id)
        REFERENCES service_delegations (delegation_id);
