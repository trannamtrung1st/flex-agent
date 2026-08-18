-- Additive frozen model-deployment binding and provider-attempt provenance.
-- Do not rewrite frozen 0001-0025. Existing session_runtimes may remain without
-- a frozen model-deployment row; execution then fails closed.

CREATE TABLE session_frozen_model_deployments (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    profile_id TEXT NOT NULL,
    profile_version TEXT NOT NULL,
    profile_digest CHAR(64) NOT NULL,
    provider_id TEXT NOT NULL,
    credential_mode TEXT NOT NULL,
    credential_binding_reference TEXT NOT NULL,
    credential_binding_version TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id),
    CONSTRAINT fk_session_frozen_model_deployments_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_session_frozen_model_deployments_digest_lowercase
        CHECK (profile_digest = lower(profile_digest)),
    CONSTRAINT chk_session_frozen_model_deployments_credential_mode
        CHECK (credential_mode IN ('deployment_default', 'organization_byok')),
    CONSTRAINT chk_session_frozen_model_deployments_nonempty
        CHECK (
            btrim(profile_id) <> ''
            AND btrim(profile_version) <> ''
            AND btrim(provider_id) <> ''
            AND btrim(credential_binding_reference) <> ''
            AND btrim(credential_binding_version) <> ''
        )
);

CREATE TRIGGER trg_session_frozen_model_deployments_stamp_created
    BEFORE INSERT ON session_frozen_model_deployments
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_created_at();

CREATE TRIGGER trg_session_frozen_model_deployments_no_update
    BEFORE UPDATE ON session_frozen_model_deployments
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_frozen_model_deployments_no_delete
    BEFORE DELETE ON session_frozen_model_deployments
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_invocation_provider_attempts (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    agent_invocation_id TEXT NOT NULL,
    attempt_ordinal INT NOT NULL,
    adapter_kind TEXT NOT NULL,
    adapter_contract_version TEXT NOT NULL,
    profile_id TEXT NOT NULL,
    profile_version TEXT NOT NULL,
    profile_digest CHAR(64) NOT NULL,
    requested_model TEXT NOT NULL,
    resolved_model_version TEXT NOT NULL,
    outcome_category TEXT NOT NULL,
    input_token_count INT NULL,
    output_token_count INT NULL,
    provider_request_ref TEXT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    completed_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, agent_invocation_id, attempt_ordinal),
    CONSTRAINT fk_session_invocation_provider_attempts_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_session_invocation_provider_attempts_digest_lowercase
        CHECK (profile_digest = lower(profile_digest)),
    CONSTRAINT chk_session_invocation_provider_attempts_ordinal
        CHECK (attempt_ordinal >= 1),
    CONSTRAINT chk_session_invocation_provider_attempts_tokens
        CHECK (
            (input_token_count IS NULL OR input_token_count >= 0)
            AND (output_token_count IS NULL OR output_token_count >= 0)
        )
);

CREATE TRIGGER trg_session_invocation_provider_attempts_stamp_created
    BEFORE INSERT ON session_invocation_provider_attempts
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_created_at();

CREATE TRIGGER trg_session_invocation_provider_attempts_no_update
    BEFORE UPDATE ON session_invocation_provider_attempts
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_invocation_provider_attempts_no_delete
    BEFORE DELETE ON session_invocation_provider_attempts
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();
