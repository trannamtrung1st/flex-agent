-- Additive human identity bindings, opaque application sessions, OIDC
-- transaction state, Data Protection key ring, and pre-Organization
-- authentication-security events. Do not rewrite frozen 0001-0029.

ALTER TABLE actors
    ADD COLUMN disabled_at TIMESTAMPTZ NULL;

CREATE TABLE human_identity_bindings (
    binding_id UUID PRIMARY KEY,
    issuer TEXT NOT NULL,
    subject TEXT NOT NULL,
    actor_id UUID NOT NULL REFERENCES actors (id),
    created_at TIMESTAMPTZ NOT NULL,
    disabled_at TIMESTAMPTZ NULL,
    CONSTRAINT chk_human_identity_bindings_issuer
        CHECK (char_length(issuer) BETWEEN 1 AND 512),
    CONSTRAINT chk_human_identity_bindings_subject
        CHECK (char_length(subject) BETWEEN 1 AND 256)
);

CREATE UNIQUE INDEX uq_human_identity_bindings_issuer_subject
    ON human_identity_bindings (issuer, subject);

CREATE INDEX ix_human_identity_bindings_actor
    ON human_identity_bindings (actor_id)
    WHERE disabled_at IS NULL;

CREATE TABLE application_sessions (
    application_session_id UUID PRIMARY KEY,
    actor_id UUID NOT NULL REFERENCES actors (id),
    organization_id UUID NOT NULL REFERENCES organizations (id),
    issuer TEXT NOT NULL,
    subject TEXT NOT NULL,
    credential_digest CHAR(64) NULL,
    authentication_strength TEXT NOT NULL,
    provider_session_digest CHAR(64) NULL,
    created_at TIMESTAMPTZ NOT NULL,
    last_seen_at TIMESTAMPTZ NOT NULL,
    idle_expires_at TIMESTAMPTZ NOT NULL,
    absolute_expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ NULL,
    rotated_at TIMESTAMPTZ NULL,
    predecessor_session_id UUID NULL REFERENCES application_sessions (application_session_id),
    terminal_reason TEXT NULL,
    CONSTRAINT chk_application_sessions_digest_lowercase
        CHECK (credential_digest IS NULL OR credential_digest = lower(credential_digest)),
    CONSTRAINT chk_application_sessions_provider_digest_lowercase
        CHECK (provider_session_digest IS NULL OR provider_session_digest = lower(provider_session_digest)),
    CONSTRAINT chk_application_sessions_strength
        CHECK (char_length(authentication_strength) BETWEEN 1 AND 256),
    CONSTRAINT chk_application_sessions_idle_order
        CHECK (idle_expires_at >= created_at),
    CONSTRAINT chk_application_sessions_absolute_order
        CHECK (absolute_expires_at >= created_at)
);

CREATE UNIQUE INDEX uq_application_sessions_live_digest
    ON application_sessions (credential_digest)
    WHERE credential_digest IS NOT NULL
      AND revoked_at IS NULL
      AND rotated_at IS NULL;

CREATE INDEX ix_application_sessions_identity_live
    ON application_sessions (issuer, subject)
    WHERE revoked_at IS NULL
      AND rotated_at IS NULL;

CREATE INDEX ix_application_sessions_provider_session
    ON application_sessions (provider_session_digest)
    WHERE provider_session_digest IS NOT NULL
      AND revoked_at IS NULL
      AND rotated_at IS NULL;

CREATE TABLE oidc_login_transactions (
    transaction_id UUID PRIMARY KEY,
    state_digest CHAR(64) NOT NULL,
    nonce_ciphertext BYTEA NOT NULL,
    code_verifier_ciphertext BYTEA NOT NULL,
    return_path TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    consumed_at TIMESTAMPTZ NULL,
    correlation_id UUID NOT NULL,
    CONSTRAINT uq_oidc_login_transactions_state UNIQUE (state_digest),
    CONSTRAINT chk_oidc_login_transactions_state_digest
        CHECK (state_digest = lower(state_digest)),
    CONSTRAINT chk_oidc_login_transactions_return_path
        CHECK (char_length(return_path) BETWEEN 1 AND 256),
    CONSTRAINT chk_oidc_login_transactions_expiry
        CHECK (expires_at > created_at)
);

CREATE INDEX ix_oidc_login_transactions_expiry
    ON oidc_login_transactions (expires_at)
    WHERE consumed_at IS NULL;

CREATE TABLE data_protection_keys (
    id BIGSERIAL PRIMARY KEY,
    friendly_name TEXT NULL,
    xml_ciphertext BYTEA NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE TABLE authentication_security_events (
    event_id UUID PRIMARY KEY,
    occurred_at TIMESTAMPTZ NOT NULL,
    event_type TEXT NOT NULL,
    outcome TEXT NOT NULL,
    reason_code TEXT NOT NULL,
    correlation_id UUID NOT NULL,
    actor_id UUID NULL,
    organization_id UUID NULL,
    application_session_id UUID NULL,
    CONSTRAINT chk_authentication_security_events_type
        CHECK (char_length(event_type) BETWEEN 1 AND 64),
    CONSTRAINT chk_authentication_security_events_reason
        CHECK (char_length(reason_code) BETWEEN 1 AND 128)
);

CREATE INDEX ix_authentication_security_events_occurred
    ON authentication_security_events (occurred_at);

CREATE OR REPLACE FUNCTION reject_authentication_security_event_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'authentication_security_events are append-only';
END;
$$;

CREATE TRIGGER trg_authentication_security_events_no_update
    BEFORE UPDATE ON authentication_security_events
    FOR EACH ROW
    EXECUTE FUNCTION reject_authentication_security_event_mutation();

CREATE TRIGGER trg_authentication_security_events_no_delete
    BEFORE DELETE ON authentication_security_events
    FOR EACH ROW
    EXECUTE FUNCTION reject_authentication_security_event_mutation();
