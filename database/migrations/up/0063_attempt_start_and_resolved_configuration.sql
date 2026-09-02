-- Additive Attempt start, exact Submission bindings, start-operation claims,
-- participant-notice projections, and resolved-configuration/manifest records.
-- Do not edit 0001-0062.

CREATE TABLE submissions_attempts (
    organization_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    cohort_id UUID NOT NULL,
    baseline_id UUID NOT NULL,
    enrollment_id UUID NOT NULL,
    participant_actor_id UUID NOT NULL,
    task_source_id UUID NOT NULL,
    ordinal INTEGER NOT NULL,
    entitlement_source TEXT NOT NULL,
    retry_entitlement_id UUID NULL,
    status TEXT NOT NULL,
    consumed BOOLEAN NOT NULL,
    requested_at TIMESTAMPTZ NOT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    terminal_at TIMESTAMPTZ NULL,
    terminal_reason_category TEXT NULL,
    session_id UUID NOT NULL,
    resolved_configuration_id UUID NOT NULL,
    initial_manifest_id UUID NOT NULL,
    configuration_digest CHAR(64) NOT NULL,
    manifest_digest CHAR(64) NOT NULL,
    PRIMARY KEY (organization_id, attempt_id),
    CONSTRAINT fk_submissions_attempts_enrollment
        FOREIGN KEY (organization_id, enrollment_id)
        REFERENCES submissions_enrollments (organization_id, enrollment_id),
    CONSTRAINT chk_submissions_attempts_status
        CHECK (status IN ('active', 'completed', 'aborted')),
    CONSTRAINT chk_submissions_attempts_entitlement
        CHECK (entitlement_source IN ('baseline', 'retry')),
    CONSTRAINT chk_submissions_attempts_ordinal
        CHECK (ordinal >= 1),
    CONSTRAINT chk_submissions_attempts_consumed
        CHECK (consumed),
    CONSTRAINT chk_submissions_attempts_retry
        CHECK (
            (entitlement_source = 'baseline' AND retry_entitlement_id IS NULL)
            OR (entitlement_source = 'retry' AND retry_entitlement_id IS NOT NULL)
        ),
    CONSTRAINT chk_submissions_attempts_digests
        CHECK (
            configuration_digest = lower(configuration_digest)
            AND char_length(configuration_digest) = 64
            AND manifest_digest = lower(manifest_digest)
            AND char_length(manifest_digest) = 64
        ),
    CONSTRAINT chk_submissions_attempts_terminal
        CHECK (
            (status = 'active' AND terminal_at IS NULL AND terminal_reason_category IS NULL)
            OR (status IN ('completed', 'aborted') AND terminal_at IS NOT NULL AND terminal_reason_category IS NOT NULL)
        )
);

CREATE UNIQUE INDEX uq_submissions_attempts_enrollment_ordinal
    ON submissions_attempts (organization_id, enrollment_id, ordinal);

CREATE UNIQUE INDEX uq_submissions_attempts_active_enrollment
    ON submissions_attempts (organization_id, enrollment_id)
    WHERE status = 'active';

CREATE UNIQUE INDEX uq_submissions_attempts_session
    ON submissions_attempts (organization_id, session_id);

CREATE TABLE submissions_attempt_submission_bindings (
    organization_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    version_id UUID NOT NULL,
    version_number INTEGER NOT NULL,
    binding_order INTEGER NOT NULL,
    PRIMARY KEY (organization_id, attempt_id, version_id),
    CONSTRAINT fk_submissions_attempt_submission_bindings_attempt
        FOREIGN KEY (organization_id, attempt_id)
        REFERENCES submissions_attempts (organization_id, attempt_id),
    CONSTRAINT fk_submissions_attempt_submission_bindings_version
        FOREIGN KEY (organization_id, version_id)
        REFERENCES submissions_accepted_versions (organization_id, version_id),
    CONSTRAINT chk_submissions_attempt_submission_bindings_order
        CHECK (binding_order >= 1 AND version_number >= 1)
);

CREATE UNIQUE INDEX uq_submissions_attempt_submission_bindings_order
    ON submissions_attempt_submission_bindings (organization_id, attempt_id, binding_order);

CREATE TABLE submissions_attempt_start_operations (
    organization_id UUID NOT NULL,
    enrollment_id UUID NOT NULL,
    participant_actor_id UUID NOT NULL,
    action TEXT NOT NULL,
    idempotency_key TEXT NOT NULL,
    command_digest CHAR(64) NOT NULL,
    status TEXT NOT NULL,
    claim_owner UUID NOT NULL,
    claimed_at TIMESTAMPTZ NOT NULL,
    lease_until TIMESTAMPTZ NOT NULL,
    attempt_id UUID NULL,
    session_id UUID NULL,
    outcome_code TEXT NULL,
    finished_at TIMESTAMPTZ NULL,
    PRIMARY KEY (organization_id, enrollment_id, action, idempotency_key),
    CONSTRAINT fk_submissions_attempt_start_operations_enrollment
        FOREIGN KEY (organization_id, enrollment_id)
        REFERENCES submissions_enrollments (organization_id, enrollment_id),
    CONSTRAINT chk_submissions_attempt_start_operations_action
        CHECK (action = 'attempt_start'),
    CONSTRAINT chk_submissions_attempt_start_operations_status
        CHECK (status IN ('claimed', 'committed', 'failed')),
    CONSTRAINT chk_submissions_attempt_start_operations_digest
        CHECK (command_digest = lower(command_digest) AND char_length(command_digest) = 64),
    CONSTRAINT chk_submissions_attempt_start_operations_key
        CHECK (char_length(idempotency_key) BETWEEN 1 AND 128),
    CONSTRAINT chk_submissions_attempt_start_operations_commit
        CHECK (
            (status = 'claimed' AND attempt_id IS NULL AND session_id IS NULL)
            OR (status = 'committed' AND attempt_id IS NOT NULL AND session_id IS NOT NULL)
            OR (status = 'failed' AND attempt_id IS NULL)
        )
);

CREATE TABLE configuration_participant_notice_projections (
    organization_id UUID NOT NULL,
    source_id UUID NOT NULL,
    source_version_id UUID NOT NULL,
    notice_id UUID NOT NULL,
    notice_type TEXT NOT NULL,
    required_outcome TEXT NOT NULL,
    protected_content_ref TEXT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    source_content_digest CHAR(64) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, source_version_id, notice_id),
    CONSTRAINT fk_configuration_participant_notice_projections_version
        FOREIGN KEY (organization_id, source_version_id)
        REFERENCES configuration_source_versions (organization_id, id),
    CONSTRAINT chk_configuration_participant_notice_projections_type
        CHECK (notice_type IN ('instructions', 'consent', 'data_use')),
    CONSTRAINT chk_configuration_participant_notice_projections_outcome
        CHECK (required_outcome = 'affirmed'),
    CONSTRAINT chk_configuration_participant_notice_projections_digests
        CHECK (
            content_digest = lower(content_digest)
            AND char_length(content_digest) = 64
            AND source_content_digest = lower(source_content_digest)
            AND char_length(source_content_digest) = 64
        )
);

CREATE TABLE session_acknowledgment_records (
    organization_id UUID NOT NULL,
    record_id UUID NOT NULL,
    enrollment_id UUID NOT NULL,
    participant_actor_id UUID NOT NULL,
    notice_id UUID NOT NULL,
    source_id UUID NOT NULL,
    source_version_id UUID NOT NULL,
    source_content_digest CHAR(64) NOT NULL,
    notice_content_digest CHAR(64) NOT NULL,
    outcome TEXT NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL,
    bound_attempt_id UUID NULL,
    idempotency_key TEXT NOT NULL,
    command_digest CHAR(64) NOT NULL,
    PRIMARY KEY (organization_id, record_id),
    CONSTRAINT fk_session_acknowledgment_records_enrollment
        FOREIGN KEY (organization_id, enrollment_id)
        REFERENCES submissions_enrollments (organization_id, enrollment_id),
    CONSTRAINT chk_session_acknowledgment_records_outcome
        CHECK (outcome IN ('affirmed', 'declined', 'withdrawn')),
    CONSTRAINT chk_session_acknowledgment_records_key
        CHECK (char_length(idempotency_key) BETWEEN 1 AND 128)
);

CREATE UNIQUE INDEX uq_session_acknowledgment_records_idempotency
    ON session_acknowledgment_records (organization_id, enrollment_id, idempotency_key);

CREATE TABLE session_resolved_configurations (
    organization_id UUID NOT NULL,
    configuration_id UUID NOT NULL,
    configuration_digest CHAR(64) NOT NULL,
    canonical_json TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, configuration_id),
    CONSTRAINT chk_session_resolved_configurations_digest
        CHECK (configuration_digest = lower(configuration_digest) AND char_length(configuration_digest) = 64)
);

CREATE TABLE session_initial_manifests (
    organization_id UUID NOT NULL,
    manifest_id UUID NOT NULL,
    configuration_id UUID NOT NULL,
    manifest_digest CHAR(64) NOT NULL,
    canonical_json TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, manifest_id),
    CONSTRAINT fk_session_initial_manifests_configuration
        FOREIGN KEY (organization_id, configuration_id)
        REFERENCES session_resolved_configurations (organization_id, configuration_id),
    CONSTRAINT chk_session_initial_manifests_digest
        CHECK (manifest_digest = lower(manifest_digest) AND char_length(manifest_digest) = 64)
);

CREATE OR REPLACE FUNCTION reject_attempt_identity_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.attempt_id IS DISTINCT FROM OLD.attempt_id
        OR NEW.enrollment_id IS DISTINCT FROM OLD.enrollment_id
        OR NEW.ordinal IS DISTINCT FROM OLD.ordinal
        OR NEW.session_id IS DISTINCT FROM OLD.session_id
        OR NEW.resolved_configuration_id IS DISTINCT FROM OLD.resolved_configuration_id
        OR NEW.initial_manifest_id IS DISTINCT FROM OLD.initial_manifest_id
        OR NEW.consumed IS DISTINCT FROM OLD.consumed
        OR NEW.entitlement_source IS DISTINCT FROM OLD.entitlement_source
    THEN
        RAISE EXCEPTION 'submissions_attempts identity is immutable';
    END IF;
    IF OLD.status IN ('completed', 'aborted') THEN
        RAISE EXCEPTION 'submissions_attempts terminal rows are immutable';
    END IF;
    IF NEW.status NOT IN ('completed', 'aborted') THEN
        RAISE EXCEPTION 'submissions_attempts may only terminalize from active';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_submissions_attempts_protect_identity
    BEFORE UPDATE ON submissions_attempts
    FOR EACH ROW
    EXECUTE FUNCTION reject_attempt_identity_mutation();

CREATE TRIGGER trg_submissions_attempts_no_delete
    BEFORE DELETE ON submissions_attempts
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_submissions_attempt_submission_bindings_no_update
    BEFORE UPDATE ON submissions_attempt_submission_bindings
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_submissions_attempt_submission_bindings_no_delete
    BEFORE DELETE ON submissions_attempt_submission_bindings
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_configuration_participant_notice_projections_no_update
    BEFORE UPDATE ON configuration_participant_notice_projections
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_configuration_participant_notice_projections_no_delete
    BEFORE DELETE ON configuration_participant_notice_projections
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_resolved_configurations_no_update
    BEFORE UPDATE ON session_resolved_configurations
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_resolved_configurations_no_delete
    BEFORE DELETE ON session_resolved_configurations
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_initial_manifests_no_update
    BEFORE UPDATE ON session_initial_manifests
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_initial_manifests_no_delete
    BEFORE DELETE ON session_initial_manifests
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();
