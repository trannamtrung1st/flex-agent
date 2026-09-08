-- Evaluation request, durable work, immutable completion, and recovery schema.
-- Additive after frozen 0071. Evaluation hosts and provider lanes remain disabled.

ALTER TABLE service_delegations
    ADD CONSTRAINT uq_service_delegations_evaluation_scope
    UNIQUE (
        delegation_id,
        organization_id,
        activity_id,
        participant_id,
        attempt_id,
        session_id);

ALTER TABLE configuration_source_payloads
    ADD CONSTRAINT uq_configuration_source_payloads_evaluation_scope
    UNIQUE (
        organization_id,
        configuration_source_id,
        source_version_id,
        content_digest);

ALTER TABLE submissions_accepted_versions
    ADD CONSTRAINT uq_submissions_accepted_versions_evaluation_scope
    UNIQUE (organization_id, submission_id, version_id);

ALTER TABLE submissions_attempt_submission_bindings
    ADD CONSTRAINT uq_submissions_attempt_submission_evaluation_scope
    UNIQUE (organization_id, attempt_id, version_id, content_digest);

ALTER TABLE submissions_attempts
    ADD CONSTRAINT uq_submissions_attempts_evaluation_scope
    UNIQUE (
        organization_id,
        activity_id,
        participant_actor_id,
        attempt_id,
        session_id,
        resolved_configuration_id,
        initial_manifest_id,
        configuration_digest,
        manifest_digest);

ALTER TABLE session_resolved_configurations
    ADD CONSTRAINT uq_session_resolved_configurations_evaluation_scope
    UNIQUE (organization_id, configuration_id, configuration_digest);

ALTER TABLE session_initial_manifests
    ADD CONSTRAINT uq_session_initial_manifests_evaluation_scope
    UNIQUE (
        organization_id,
        manifest_id,
        configuration_id,
        manifest_digest);

ALTER TABLE session_frozen_model_deployments
    ADD CONSTRAINT uq_session_frozen_model_deployments_evaluation_scope
    UNIQUE (
        organization_id,
        activity_id,
        participant_id,
        attempt_id,
        session_id,
        profile_id,
        profile_version,
        profile_digest,
        provider_id,
        credential_mode,
        credential_binding_reference,
        credential_binding_version);

ALTER TABLE session_evaluation_handoffs
    ADD CONSTRAINT uq_session_evaluation_handoffs_evaluation_authority
    UNIQUE (
        organization_id,
        activity_id,
        participant_id,
        attempt_id,
        session_id,
        handoff_id,
        terminal_record_id,
        eligibility,
        terminal_state,
        cutoff_sequence,
        configuration_id,
        configuration_digest,
        manifest_id,
        seal_digest,
        procedure_id);

CREATE TABLE evaluation_handoff_inbox (
    organization_id UUID NOT NULL,
    delivery_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    handoff_id TEXT NOT NULL,
    event_schema TEXT NOT NULL,
    state TEXT NOT NULL,
    received_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    processed_at TIMESTAMPTZ NULL,
    failure_category TEXT NULL,
    PRIMARY KEY (organization_id, delivery_id),
    CONSTRAINT fk_evaluation_handoff_inbox_handoff
        FOREIGN KEY (organization_id, session_id, handoff_id)
        REFERENCES session_evaluation_handoffs (organization_id, session_id, handoff_id),
    CONSTRAINT fk_evaluation_handoff_inbox_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT uq_evaluation_handoff_inbox_handoff
        UNIQUE (organization_id, session_id, handoff_id),
    CONSTRAINT chk_evaluation_handoff_inbox_schema
        CHECK (event_schema = 'session.evaluation_handoff.recorded.v1'),
    CONSTRAINT chk_evaluation_handoff_inbox_state
        CHECK (state IN ('pending', 'processed', 'rejected')),
    CONSTRAINT chk_evaluation_handoff_inbox_terminal
        CHECK (
            (state = 'pending' AND processed_at IS NULL AND failure_category IS NULL)
            OR (state = 'processed' AND processed_at IS NOT NULL AND failure_category IS NULL)
            OR (state = 'rejected' AND processed_at IS NOT NULL AND failure_category IS NOT NULL))
);

CREATE INDEX ix_evaluation_handoff_inbox_pending
    ON evaluation_handoff_inbox (received_at, organization_id, delivery_id)
    WHERE state = 'pending';

CREATE TABLE evaluation_requests (
    organization_id UUID NOT NULL,
    request_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    handoff_id TEXT NOT NULL,
    terminal_record_id UUID NOT NULL,
    handoff_eligibility TEXT NOT NULL,
    handoff_terminal_state TEXT NOT NULL,
    cutoff_sequence BIGINT NOT NULL,
    request_kind TEXT NOT NULL,
    frozen_input_digest CHAR(64) NOT NULL,
    idempotency_key TEXT NOT NULL,
    delegation_id UUID NOT NULL,
    state TEXT NOT NULL,
    predecessor_evaluation_id UUID NULL,
    replacement_reason TEXT NULL,
    rubric_source_id UUID NOT NULL,
    rubric_source_version_id UUID NOT NULL,
    rubric_content_digest CHAR(64) NOT NULL,
    submission_source_id UUID NOT NULL,
    submission_version_id UUID NOT NULL,
    submission_content_digest CHAR(64) NOT NULL,
    configuration_id TEXT NOT NULL,
    configuration_record_id UUID NOT NULL,
    configuration_digest CHAR(64) NOT NULL,
    manifest_id TEXT NOT NULL,
    manifest_record_id UUID NOT NULL,
    manifest_digest CHAR(64) NOT NULL,
    manifest_seal_procedure_id TEXT NOT NULL,
    terminal_seal_digest CHAR(64) NOT NULL,
    model_profile_id TEXT NOT NULL,
    model_profile_version TEXT NOT NULL,
    model_profile_digest CHAR(64) NOT NULL,
    provider_id TEXT NOT NULL,
    credential_mode TEXT NOT NULL,
    credential_binding_reference TEXT NOT NULL,
    credential_binding_version TEXT NOT NULL,
    evaluator_registry_version TEXT NOT NULL,
    lifecycle_policy_ref TEXT NOT NULL,
    correlation_id UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    completed_at TIMESTAMPTZ NULL,
    failure_category TEXT NULL,
    PRIMARY KEY (organization_id, request_id),
    CONSTRAINT uq_evaluation_requests_owned
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id),
    CONSTRAINT uq_evaluation_requests_predecessor_binding
        UNIQUE NULLS NOT DISTINCT (
            organization_id,
            request_id,
            predecessor_evaluation_id),
    CONSTRAINT fk_evaluation_requests_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT fk_evaluation_requests_handoff
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            handoff_id,
            terminal_record_id,
            handoff_eligibility,
            handoff_terminal_state,
            cutoff_sequence,
            configuration_id,
            configuration_digest,
            manifest_id,
            terminal_seal_digest,
            manifest_seal_procedure_id)
        REFERENCES session_evaluation_handoffs (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            handoff_id,
            terminal_record_id,
            eligibility,
            terminal_state,
            cutoff_sequence,
            configuration_id,
            configuration_digest,
            manifest_id,
            seal_digest,
            procedure_id),
    CONSTRAINT fk_evaluation_requests_delegation
        FOREIGN KEY (
            delegation_id,
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id)
        REFERENCES service_delegations (
            delegation_id,
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id),
    CONSTRAINT fk_evaluation_requests_rubric_payload
        FOREIGN KEY (
            organization_id,
            rubric_source_id,
            rubric_source_version_id,
            rubric_content_digest)
        REFERENCES configuration_source_payloads (
            organization_id,
            configuration_source_id,
            source_version_id,
            content_digest),
    CONSTRAINT fk_evaluation_requests_submission
        FOREIGN KEY (
            organization_id,
            attempt_id,
            submission_version_id,
            submission_content_digest)
        REFERENCES submissions_attempt_submission_bindings (
            organization_id,
            attempt_id,
            version_id,
            content_digest),
    CONSTRAINT fk_evaluation_requests_submission_version
        FOREIGN KEY (organization_id, submission_source_id, submission_version_id)
        REFERENCES submissions_accepted_versions (organization_id, submission_id, version_id),
    CONSTRAINT fk_evaluation_requests_attempt_frozen_input
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            configuration_record_id,
            manifest_record_id,
            configuration_digest,
            manifest_digest)
        REFERENCES submissions_attempts (
            organization_id,
            activity_id,
            participant_actor_id,
            attempt_id,
            session_id,
            resolved_configuration_id,
            initial_manifest_id,
            configuration_digest,
            manifest_digest),
    CONSTRAINT fk_evaluation_requests_resolved_configuration
        FOREIGN KEY (
            organization_id,
            configuration_record_id,
            configuration_digest)
        REFERENCES session_resolved_configurations (
            organization_id,
            configuration_id,
            configuration_digest),
    CONSTRAINT fk_evaluation_requests_initial_manifest
        FOREIGN KEY (
            organization_id,
            manifest_record_id,
            configuration_record_id,
            manifest_digest)
        REFERENCES session_initial_manifests (
            organization_id,
            manifest_id,
            configuration_id,
            manifest_digest),
    CONSTRAINT fk_evaluation_requests_frozen_model
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            model_profile_id,
            model_profile_version,
            model_profile_digest,
            provider_id,
            credential_mode,
            credential_binding_reference,
            credential_binding_version)
        REFERENCES session_frozen_model_deployments (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            profile_id,
            profile_version,
            profile_digest,
            provider_id,
            credential_mode,
            credential_binding_reference,
            credential_binding_version),
    CONSTRAINT chk_evaluation_requests_kind
        CHECK (request_kind IN ('initial', 'replacement')),
    CONSTRAINT chk_evaluation_requests_state
        CHECK (state IN (
            'queued', 'running', 'validating', 'completing', 'completed',
            'failed_retryable', 'failed_review_required', 'cancelled')),
    CONSTRAINT chk_evaluation_requests_identity
        CHECK (
            char_length(idempotency_key) BETWEEN 8 AND 128
            AND frozen_input_digest = lower(frozen_input_digest)
            AND char_length(frozen_input_digest) = 64),
    CONSTRAINT chk_evaluation_requests_digests
        CHECK (
            rubric_content_digest = lower(rubric_content_digest)
            AND char_length(rubric_content_digest) = 64
            AND submission_content_digest = lower(submission_content_digest)
            AND char_length(submission_content_digest) = 64
            AND configuration_digest = lower(configuration_digest)
            AND char_length(configuration_digest) = 64
            AND manifest_digest = lower(manifest_digest)
            AND char_length(manifest_digest) = 64
            AND terminal_seal_digest = lower(terminal_seal_digest)
            AND char_length(terminal_seal_digest) = 64
            AND model_profile_digest = lower(model_profile_digest)
            AND char_length(model_profile_digest) = 64),
    CONSTRAINT chk_evaluation_requests_completed_handoff
        CHECK (
            handoff_eligibility = 'eligible'
            AND handoff_terminal_state = 'completed'
            AND cutoff_sequence >= 0),
    CONSTRAINT chk_evaluation_requests_record_ids
        CHECK (
            configuration_id = configuration_record_id::text
            AND manifest_id = manifest_record_id::text),
    CONSTRAINT chk_evaluation_requests_manifest_procedure
        CHECK (manifest_seal_procedure_id IN ('manifest-jcs-sha256-v1', 'manifest-jcs-sha256-v2')),
    CONSTRAINT chk_evaluation_requests_replacement
        CHECK (
            (request_kind = 'initial'
                AND predecessor_evaluation_id IS NULL
                AND replacement_reason IS NULL)
            OR (request_kind = 'replacement'
                AND predecessor_evaluation_id IS NOT NULL
                AND char_length(replacement_reason) BETWEEN 8 AND 128)),
    CONSTRAINT chk_evaluation_requests_terminal
        CHECK (
            (state = 'completed' AND completed_at IS NOT NULL AND failure_category IS NULL)
            OR (state IN ('failed_retryable', 'failed_review_required', 'cancelled')
                AND completed_at IS NULL AND failure_category IS NOT NULL)
            OR (state IN ('queued', 'running', 'validating', 'completing')
                AND completed_at IS NULL AND failure_category IS NULL))
);

CREATE UNIQUE INDEX uq_evaluation_requests_initial_input
    ON evaluation_requests (organization_id, session_id, handoff_id, frozen_input_digest)
    WHERE request_kind = 'initial';

CREATE UNIQUE INDEX uq_evaluation_requests_idempotency
    ON evaluation_requests (organization_id, session_id, idempotency_key);

CREATE TABLE evaluation_invocation_attempts (
    organization_id UUID NOT NULL,
    request_id UUID NOT NULL,
    invocation_attempt_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    attempt_ordinal INTEGER NOT NULL,
    state TEXT NOT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    finished_at TIMESTAMPTZ NULL,
    failure_category TEXT NULL,
    protected_request_ref TEXT NULL,
    protected_response_ref TEXT NULL,
    PRIMARY KEY (organization_id, invocation_attempt_id),
    CONSTRAINT uq_evaluation_invocation_attempts_request
        UNIQUE (organization_id, request_id, invocation_attempt_id),
    CONSTRAINT fk_evaluation_invocation_attempts_request
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id)
        REFERENCES evaluation_requests (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id),
    CONSTRAINT chk_evaluation_invocation_attempts_ordinal
        CHECK (attempt_ordinal >= 1),
    CONSTRAINT chk_evaluation_invocation_attempts_state
        CHECK (state IN (
            'running', 'validating', 'completing', 'completed',
            'failed_retryable', 'failed_review_required', 'cancelled')),
    CONSTRAINT chk_evaluation_invocation_attempts_terminal
        CHECK (
            (state IN ('running', 'validating', 'completing')
                AND finished_at IS NULL AND failure_category IS NULL)
            OR (state = 'completed' AND finished_at IS NOT NULL AND failure_category IS NULL)
            OR (state IN ('failed_retryable', 'failed_review_required', 'cancelled')
                AND finished_at IS NOT NULL AND failure_category IS NOT NULL))
);

CREATE UNIQUE INDEX uq_evaluation_attempts_ordinal
    ON evaluation_invocation_attempts (organization_id, request_id, attempt_ordinal);

CREATE TABLE evaluation_deterministic_attempts (
    organization_id UUID NOT NULL,
    deterministic_attempt_id UUID NOT NULL,
    request_id UUID NOT NULL,
    invocation_attempt_id UUID NOT NULL,
    criterion_id TEXT NOT NULL,
    criterion_version TEXT NOT NULL,
    evaluator_id TEXT NOT NULL,
    evaluator_version TEXT NOT NULL,
    evaluator_digest CHAR(64) NOT NULL,
    canonical_input_digest CHAR(64) NOT NULL,
    dependency_digest CHAR(64) NOT NULL,
    configuration_digest CHAR(64) NOT NULL,
    outcome TEXT NOT NULL,
    protected_input_ref TEXT NULL,
    protected_output_ref TEXT NULL,
    failure_category TEXT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    finished_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, deterministic_attempt_id),
    CONSTRAINT fk_evaluation_deterministic_attempts_invocation
        FOREIGN KEY (organization_id, request_id, invocation_attempt_id)
        REFERENCES evaluation_invocation_attempts (
            organization_id,
            request_id,
            invocation_attempt_id),
    CONSTRAINT uq_evaluation_deterministic_attempts_request
        UNIQUE (organization_id, request_id, deterministic_attempt_id),
    CONSTRAINT chk_evaluation_deterministic_attempts_outcome
        CHECK (outcome IN ('succeeded', 'failed', 'timed_out', 'bound_exhausted', 'invalid_output')),
    CONSTRAINT chk_evaluation_deterministic_attempts_digests
        CHECK (
            evaluator_digest = lower(evaluator_digest)
            AND canonical_input_digest = lower(canonical_input_digest)
            AND dependency_digest = lower(dependency_digest)
            AND configuration_digest = lower(configuration_digest)
            AND char_length(evaluator_digest) = 64
            AND char_length(canonical_input_digest) = 64
            AND char_length(dependency_digest) = 64
            AND char_length(configuration_digest) = 64),
    CONSTRAINT chk_evaluation_deterministic_attempts_time
        CHECK (finished_at >= started_at)
);

CREATE TABLE evaluation_provider_artifacts (
    organization_id UUID NOT NULL,
    provider_artifact_id UUID NOT NULL,
    request_id UUID NOT NULL,
    invocation_attempt_id UUID NOT NULL,
    model_profile_id TEXT NOT NULL,
    model_profile_version TEXT NOT NULL,
    model_profile_digest CHAR(64) NOT NULL,
    credential_binding_reference TEXT NOT NULL,
    protected_request_ref TEXT NOT NULL,
    protected_response_ref TEXT NULL,
    outcome TEXT NOT NULL,
    failure_category TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, provider_artifact_id),
    CONSTRAINT fk_evaluation_provider_artifacts_invocation
        FOREIGN KEY (organization_id, request_id, invocation_attempt_id)
        REFERENCES evaluation_invocation_attempts (
            organization_id,
            request_id,
            invocation_attempt_id),
    CONSTRAINT chk_evaluation_provider_artifacts_digest
        CHECK (
            model_profile_digest = lower(model_profile_digest)
            AND char_length(model_profile_digest) = 64),
    CONSTRAINT chk_evaluation_provider_artifacts_outcome
        CHECK (outcome IN ('succeeded', 'failed', 'timed_out', 'invalid_output'))
);

CREATE TABLE evaluation_durable_work (
    organization_id UUID NOT NULL,
    work_id UUID NOT NULL,
    request_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    state TEXT NOT NULL,
    available_at TIMESTAMPTZ NOT NULL,
    attempt_count INTEGER NOT NULL DEFAULT 0,
    max_attempts INTEGER NOT NULL,
    attempt_timeout_seconds INTEGER NOT NULL,
    backoff_seconds INTEGER NOT NULL,
    claim_owner UUID NULL,
    claim_lease_until TIMESTAMPTZ NULL,
    failure_category TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    last_committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, work_id),
    CONSTRAINT fk_evaluation_durable_work_request
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id)
        REFERENCES evaluation_requests (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id),
    CONSTRAINT uq_evaluation_durable_work_request
        UNIQUE (organization_id, request_id),
    CONSTRAINT chk_evaluation_durable_work_state
        CHECK (state IN ('pending', 'claimed', 'completed', 'failed', 'cancelled')),
    CONSTRAINT chk_evaluation_durable_work_bounds
        CHECK (
            attempt_count >= 0
            AND max_attempts BETWEEN 1 AND 10
            AND attempt_count <= max_attempts
            AND attempt_timeout_seconds BETWEEN 1 AND 3600
            AND backoff_seconds BETWEEN 1 AND 3600),
    CONSTRAINT chk_evaluation_durable_work_claim
        CHECK (
            (state = 'claimed' AND claim_owner IS NOT NULL AND claim_lease_until IS NOT NULL)
            OR (state <> 'claimed' AND claim_owner IS NULL AND claim_lease_until IS NULL))
);

CREATE INDEX ix_evaluation_durable_work_claimable
    ON evaluation_durable_work (available_at, organization_id, activity_id, work_id)
    WHERE state IN ('pending', 'claimed');

CREATE TABLE evaluation_work_claim_partitions (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    last_claimed_at TIMESTAMPTZ NOT NULL,
    last_claimed_work_id UUID NOT NULL,
    PRIMARY KEY (organization_id, activity_id)
);

CREATE TABLE evaluation_evidence_items (
    organization_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    evidence_id UUID NOT NULL,
    request_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    source_type TEXT NOT NULL,
    source_id UUID NOT NULL,
    source_version_id UUID NOT NULL,
    source_content_digest CHAR(64) NOT NULL,
    locator_schema TEXT NOT NULL,
    locator_digest CHAR(64) NOT NULL,
    precision TEXT NOT NULL,
    integrity_state TEXT NOT NULL,
    created_by_service TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, evaluation_id, evidence_id),
    CONSTRAINT fk_evaluation_evidence_items_request
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id)
        REFERENCES evaluation_requests (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id),
    CONSTRAINT chk_evaluation_evidence_items_source
        CHECK (source_type IN (
            'submission.direct_text', 'submission.text_attachment',
            'session.transcript_item', 'session.work_trace',
            'configuration.fact', 'manifest.fact', 'deterministic.fact')),
    CONSTRAINT chk_evaluation_evidence_items_precision
        CHECK (precision IN ('exact_range', 'stable_segment', 'whole_item')),
    CONSTRAINT chk_evaluation_evidence_items_integrity
        CHECK (integrity_state IN ('verified', 'lower_precision', 'unavailable', 'integrity_changed')),
    CONSTRAINT chk_evaluation_evidence_items_digests
        CHECK (
            source_content_digest = lower(source_content_digest)
            AND locator_digest = lower(locator_digest)
            AND char_length(source_content_digest) = 64
            AND char_length(locator_digest) = 64)
);

CREATE TABLE evaluation_evidence_sets (
    organization_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    evidence_set_id UUID NOT NULL,
    request_id UUID NOT NULL,
    invocation_attempt_id UUID NOT NULL,
    seal_schema TEXT NOT NULL,
    seal_digest CHAR(64) NOT NULL,
    sealed_at TIMESTAMPTZ NOT NULL,
    sealed_by_service TEXT NOT NULL,
    PRIMARY KEY (organization_id, evaluation_id, evidence_set_id),
    CONSTRAINT fk_evaluation_evidence_sets_request
        FOREIGN KEY (organization_id, request_id)
        REFERENCES evaluation_requests (organization_id, request_id),
    CONSTRAINT fk_evaluation_evidence_sets_invocation
        FOREIGN KEY (organization_id, request_id, invocation_attempt_id)
        REFERENCES evaluation_invocation_attempts (
            organization_id,
            request_id,
            invocation_attempt_id),
    CONSTRAINT uq_evaluation_evidence_sets_evaluation
        UNIQUE (organization_id, evaluation_id),
    CONSTRAINT uq_evaluation_evidence_sets_request
        UNIQUE (organization_id, evaluation_id, evidence_set_id, request_id),
    CONSTRAINT chk_evaluation_evidence_sets_schema
        CHECK (seal_schema = 'evidence-set-jcs-sha256-v1'),
    CONSTRAINT chk_evaluation_evidence_sets_digest
        CHECK (seal_digest = lower(seal_digest) AND char_length(seal_digest) = 64)
);

CREATE TABLE evaluation_evidence_set_items (
    organization_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    evidence_set_id UUID NOT NULL,
    evidence_id UUID NOT NULL,
    item_ordinal INTEGER NOT NULL,
    PRIMARY KEY (organization_id, evaluation_id, evidence_set_id, evidence_id),
    CONSTRAINT fk_evaluation_evidence_set_items_set
        FOREIGN KEY (organization_id, evaluation_id, evidence_set_id)
        REFERENCES evaluation_evidence_sets (organization_id, evaluation_id, evidence_set_id),
    CONSTRAINT fk_evaluation_evidence_set_items_item
        FOREIGN KEY (organization_id, evaluation_id, evidence_id)
        REFERENCES evaluation_evidence_items (organization_id, evaluation_id, evidence_id),
    CONSTRAINT uq_evaluation_evidence_set_items_ordinal
        UNIQUE (organization_id, evaluation_id, evidence_set_id, item_ordinal),
    CONSTRAINT chk_evaluation_evidence_set_items_ordinal
        CHECK (item_ordinal >= 1)
);

CREATE TABLE evaluation_criterion_judgments (
    organization_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    judgment_id UUID NOT NULL,
    request_id UUID NOT NULL,
    criterion_id TEXT NOT NULL,
    criterion_version TEXT NOT NULL,
    evaluator_mode TEXT NOT NULL,
    status TEXT NOT NULL,
    confidence TEXT NOT NULL,
    uncertainty_json JSONB NOT NULL,
    rationale TEXT NOT NULL,
    score_json JSONB NULL,
    provisional_feedback TEXT NULL,
    deterministic_attempt_id UUID NULL,
    PRIMARY KEY (organization_id, evaluation_id, judgment_id),
    CONSTRAINT fk_evaluation_criterion_judgments_request
        FOREIGN KEY (organization_id, request_id)
        REFERENCES evaluation_requests (organization_id, request_id),
    CONSTRAINT fk_evaluation_criterion_judgments_deterministic
        FOREIGN KEY (organization_id, request_id, deterministic_attempt_id)
        REFERENCES evaluation_deterministic_attempts (
            organization_id,
            request_id,
            deterministic_attempt_id),
    CONSTRAINT uq_evaluation_criterion_judgments_criterion
        UNIQUE (organization_id, evaluation_id, criterion_id, criterion_version),
    CONSTRAINT chk_evaluation_criterion_judgments_mode
        CHECK (evaluator_mode IN ('deterministic', 'agent_assisted', 'agent_judgment')),
    CONSTRAINT chk_evaluation_criterion_judgments_status
        CHECK (status IN (
            'satisfied', 'not_satisfied', 'insufficient_evidence',
            'not_applicable', 'conflict')),
    CONSTRAINT chk_evaluation_criterion_judgments_rationale
        CHECK (char_length(rationale) BETWEEN 1 AND 4000)
);

CREATE TABLE evaluations (
    organization_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    request_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    evidence_set_id UUID NOT NULL,
    procedure_source_id UUID NOT NULL,
    procedure_source_version_id UUID NOT NULL,
    procedure_digest CHAR(64) NOT NULL,
    aggregate_status TEXT NOT NULL,
    creation_service_id TEXT NOT NULL,
    completed_at TIMESTAMPTZ NOT NULL,
    predecessor_evaluation_id UUID NULL,
    PRIMARY KEY (organization_id, evaluation_id),
    CONSTRAINT uq_evaluations_request_binding
        UNIQUE (organization_id, evaluation_id, request_id),
    CONSTRAINT uq_evaluations_owned_identity
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            evaluation_id),
    CONSTRAINT fk_evaluations_request
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id)
        REFERENCES evaluation_requests (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id),
    CONSTRAINT fk_evaluations_request_predecessor
        FOREIGN KEY (organization_id, request_id, predecessor_evaluation_id)
        REFERENCES evaluation_requests (
            organization_id,
            request_id,
            predecessor_evaluation_id),
    CONSTRAINT fk_evaluations_evidence_set
        FOREIGN KEY (organization_id, evaluation_id, evidence_set_id, request_id)
        REFERENCES evaluation_evidence_sets (
            organization_id,
            evaluation_id,
            evidence_set_id,
            request_id)
        DEFERRABLE INITIALLY DEFERRED,
    CONSTRAINT uq_evaluations_request
        UNIQUE (organization_id, request_id),
    CONSTRAINT chk_evaluations_digest
        CHECK (procedure_digest = lower(procedure_digest) AND char_length(procedure_digest) = 64),
    CONSTRAINT chk_evaluations_aggregate
        CHECK (aggregate_status IN (
            'complete', 'requirements_not_satisfied', 'insufficient_evidence',
            'conflict_review_required', 'not_applicable_excluded'))
);

ALTER TABLE evaluation_evidence_items
    ADD CONSTRAINT fk_evaluation_evidence_items_evaluation
    FOREIGN KEY (organization_id, evaluation_id, request_id)
    REFERENCES evaluations (organization_id, evaluation_id, request_id)
    DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE evaluation_criterion_judgments
    ADD CONSTRAINT fk_evaluation_criterion_judgments_evaluation
    FOREIGN KEY (organization_id, evaluation_id, request_id)
    REFERENCES evaluations (organization_id, evaluation_id, request_id)
    DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE evaluation_requests
    ADD CONSTRAINT fk_evaluation_requests_predecessor
    FOREIGN KEY (
        organization_id,
        activity_id,
        participant_id,
        attempt_id,
        session_id,
        predecessor_evaluation_id)
    REFERENCES evaluations (
        organization_id,
        activity_id,
        participant_id,
        attempt_id,
        session_id,
        evaluation_id)
    DEFERRABLE INITIALLY DEFERRED;

CREATE TABLE evaluation_lineage (
    organization_id UUID NOT NULL,
    lineage_id UUID NOT NULL,
    predecessor_evaluation_id UUID NOT NULL,
    successor_request_id UUID NOT NULL,
    successor_evaluation_id UUID NOT NULL,
    reason TEXT NOT NULL,
    actor_type TEXT NOT NULL,
    actor_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, lineage_id),
    CONSTRAINT fk_evaluation_lineage_predecessor
        FOREIGN KEY (organization_id, predecessor_evaluation_id)
        REFERENCES evaluations (organization_id, evaluation_id),
    CONSTRAINT fk_evaluation_lineage_request_predecessor
        FOREIGN KEY (
            organization_id,
            successor_request_id,
            predecessor_evaluation_id)
        REFERENCES evaluation_requests (
            organization_id,
            request_id,
            predecessor_evaluation_id),
    CONSTRAINT fk_evaluation_lineage_successor
        FOREIGN KEY (organization_id, successor_evaluation_id, successor_request_id)
        REFERENCES evaluations (organization_id, evaluation_id, request_id),
    CONSTRAINT uq_evaluation_lineage_predecessor_successor
        UNIQUE (organization_id, predecessor_evaluation_id, successor_evaluation_id),
    CONSTRAINT chk_evaluation_lineage_actor
        CHECK (actor_type IN ('human', 'service', 'system')),
    CONSTRAINT chk_evaluation_lineage_reason
        CHECK (char_length(reason) BETWEEN 8 AND 128),
    CONSTRAINT chk_evaluation_lineage_distinct
        CHECK (predecessor_evaluation_id <> successor_evaluation_id)
);

CREATE TABLE evaluation_annotations (
    organization_id UUID NOT NULL,
    annotation_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    evidence_id UUID NULL,
    kind TEXT NOT NULL,
    disposition TEXT NOT NULL,
    reason TEXT NOT NULL,
    actor_type TEXT NOT NULL,
    actor_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, annotation_id),
    CONSTRAINT fk_evaluation_annotations_evaluation
        FOREIGN KEY (organization_id, evaluation_id)
        REFERENCES evaluations (organization_id, evaluation_id),
    CONSTRAINT chk_evaluation_annotations_kind
        CHECK (kind IN (
            'source_integrity_changed', 'source_lawfully_unavailable',
            'lower_precision_recorded', 'lifecycle_hold', 'lifecycle_expiry')),
    CONSTRAINT chk_evaluation_annotations_disposition
        CHECK (disposition IN (
            'attention_required', 'lawfully_unavailable', 'lower_precision',
            'held', 'expired_minimum_provenance')),
    CONSTRAINT chk_evaluation_annotations_actor
        CHECK (actor_type IN ('human', 'service', 'system')),
    CONSTRAINT chk_evaluation_annotations_reason
        CHECK (char_length(reason) BETWEEN 8 AND 128)
);

CREATE TABLE evaluation_dispositions (
    organization_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    current_disposition TEXT NOT NULL,
    last_annotation_id UUID NOT NULL,
    version BIGINT NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, evaluation_id),
    CONSTRAINT fk_evaluation_dispositions_evaluation
        FOREIGN KEY (organization_id, evaluation_id)
        REFERENCES evaluations (organization_id, evaluation_id),
    CONSTRAINT fk_evaluation_dispositions_annotation
        FOREIGN KEY (organization_id, last_annotation_id)
        REFERENCES evaluation_annotations (organization_id, annotation_id),
    CONSTRAINT chk_evaluation_dispositions_value
        CHECK (current_disposition IN (
            'attention_required', 'lawfully_unavailable', 'lower_precision',
            'held', 'expired_minimum_provenance')),
    CONSTRAINT chk_evaluation_dispositions_version
        CHECK (version >= 1)
);

CREATE TABLE evaluation_manifest_refs (
    organization_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    manifest_ref_id UUID NOT NULL,
    ref_kind TEXT NOT NULL,
    protected_ref TEXT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, evaluation_id, manifest_ref_id),
    CONSTRAINT fk_evaluation_manifest_refs_evaluation
        FOREIGN KEY (organization_id, evaluation_id)
        REFERENCES evaluations (organization_id, evaluation_id),
    CONSTRAINT uq_evaluation_manifest_refs_payload
        UNIQUE (organization_id, evaluation_id, ref_kind, protected_ref),
    CONSTRAINT chk_evaluation_manifest_refs_kind
        CHECK (ref_kind IN ('evaluation', 'evidence_set', 'deterministic_attempt', 'provider_artifact')),
    CONSTRAINT chk_evaluation_manifest_refs_digest
        CHECK (content_digest = lower(content_digest) AND char_length(content_digest) = 64)
);

CREATE TABLE evaluation_review_handoffs (
    organization_id UUID NOT NULL,
    handoff_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    request_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    candidate_eligible BOOLEAN NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, handoff_id),
    CONSTRAINT fk_evaluation_review_handoffs_evaluation
        FOREIGN KEY (organization_id, evaluation_id, request_id)
        REFERENCES evaluations (organization_id, evaluation_id, request_id),
    CONSTRAINT fk_evaluation_review_handoffs_request
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id)
        REFERENCES evaluation_requests (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            request_id),
    CONSTRAINT uq_evaluation_review_handoffs_evaluation
        UNIQUE (organization_id, evaluation_id)
);

CREATE TABLE evaluation_lifecycle_holds (
    organization_id UUID NOT NULL,
    hold_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    reason_code TEXT NOT NULL,
    active BOOLEAN NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    released_at TIMESTAMPTZ NULL,
    PRIMARY KEY (organization_id, hold_id),
    CONSTRAINT fk_evaluation_lifecycle_holds_evaluation
        FOREIGN KEY (organization_id, evaluation_id)
        REFERENCES evaluations (organization_id, evaluation_id),
    CONSTRAINT chk_evaluation_lifecycle_holds_reason
        CHECK (reason_code = 'legal_hold'),
    CONSTRAINT chk_evaluation_lifecycle_holds_release
        CHECK (
            (active AND released_at IS NULL)
            OR (NOT active AND released_at IS NOT NULL))
);

CREATE INDEX ix_evaluation_lifecycle_holds_active
    ON evaluation_lifecycle_holds (organization_id, evaluation_id)
    WHERE active;

CREATE TABLE evaluation_lifecycle_disposition_events (
    organization_id UUID NOT NULL,
    disposition_event_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    object_kind TEXT NOT NULL,
    object_id UUID NOT NULL,
    reason_code TEXT NOT NULL,
    actor_id UUID NOT NULL,
    audit_event_id UUID NOT NULL,
    disposed_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, disposition_event_id),
    CONSTRAINT fk_evaluation_lifecycle_dispositions_evaluation
        FOREIGN KEY (organization_id, evaluation_id)
        REFERENCES evaluations (organization_id, evaluation_id),
    CONSTRAINT fk_evaluation_lifecycle_dispositions_audit
        FOREIGN KEY (audit_event_id)
        REFERENCES audit_events (event_id),
    CONSTRAINT uq_evaluation_lifecycle_dispositions_object
        UNIQUE (organization_id, object_kind, object_id),
    CONSTRAINT chk_evaluation_lifecycle_dispositions_kind
        CHECK (object_kind = 'provider_artifact'),
    CONSTRAINT chk_evaluation_lifecycle_dispositions_reason
        CHECK (reason_code IN ('retention_expired', 'authorized_erasure'))
);

CREATE OR REPLACE FUNCTION reject_evaluation_append_only_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION '% is append-only', TG_TABLE_NAME;
END;
$$;

CREATE OR REPLACE FUNCTION protect_evaluation_request_identity()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'evaluation_requests cannot be deleted';
    END IF;
    IF ROW(
        NEW.organization_id, NEW.request_id, NEW.activity_id, NEW.participant_id,
        NEW.attempt_id, NEW.session_id, NEW.handoff_id, NEW.terminal_record_id,
        NEW.handoff_eligibility, NEW.handoff_terminal_state, NEW.cutoff_sequence, NEW.request_kind,
        NEW.frozen_input_digest, NEW.idempotency_key, NEW.delegation_id,
        NEW.predecessor_evaluation_id, NEW.replacement_reason,
        NEW.rubric_source_id, NEW.rubric_source_version_id, NEW.rubric_content_digest,
        NEW.submission_source_id, NEW.submission_version_id, NEW.submission_content_digest,
        NEW.configuration_id, NEW.configuration_record_id, NEW.configuration_digest,
        NEW.manifest_id, NEW.manifest_record_id, NEW.manifest_digest,
        NEW.manifest_seal_procedure_id, NEW.terminal_seal_digest,
        NEW.model_profile_id, NEW.model_profile_version, NEW.model_profile_digest,
        NEW.provider_id, NEW.credential_mode, NEW.credential_binding_reference,
        NEW.credential_binding_version, NEW.evaluator_registry_version,
        NEW.lifecycle_policy_ref, NEW.correlation_id, NEW.created_at)
       IS DISTINCT FROM
       ROW(
        OLD.organization_id, OLD.request_id, OLD.activity_id, OLD.participant_id,
        OLD.attempt_id, OLD.session_id, OLD.handoff_id, OLD.terminal_record_id,
        OLD.handoff_eligibility, OLD.handoff_terminal_state, OLD.cutoff_sequence, OLD.request_kind,
        OLD.frozen_input_digest, OLD.idempotency_key, OLD.delegation_id,
        OLD.predecessor_evaluation_id, OLD.replacement_reason,
        OLD.rubric_source_id, OLD.rubric_source_version_id, OLD.rubric_content_digest,
        OLD.submission_source_id, OLD.submission_version_id, OLD.submission_content_digest,
        OLD.configuration_id, OLD.configuration_record_id, OLD.configuration_digest,
        OLD.manifest_id, OLD.manifest_record_id, OLD.manifest_digest,
        OLD.manifest_seal_procedure_id, OLD.terminal_seal_digest,
        OLD.model_profile_id, OLD.model_profile_version, OLD.model_profile_digest,
        OLD.provider_id, OLD.credential_mode, OLD.credential_binding_reference,
        OLD.credential_binding_version, OLD.evaluator_registry_version,
        OLD.lifecycle_policy_ref, OLD.correlation_id, OLD.created_at)
    THEN
        RAISE EXCEPTION 'evaluation_requests frozen identity is immutable';
    END IF;
    IF OLD.state IN ('completed', 'failed_review_required', 'cancelled') THEN
        RAISE EXCEPTION 'evaluation_requests terminal state is immutable';
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION verify_evaluation_request_frozen_authorities()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM session_resolved_configurations AS configuration
        WHERE configuration.organization_id = NEW.organization_id
          AND configuration.configuration_id = NEW.configuration_record_id
          AND configuration.configuration_digest = NEW.configuration_digest
          AND EXISTS (
                SELECT 1
                FROM jsonb_array_elements(
                    configuration.canonical_json::jsonb -> 'sources') AS source
                WHERE source ->> 'source_key' = 'rubric_evaluation'
                  AND source ->> 'source_id' = NEW.rubric_source_id::text
                  AND source ->> 'source_version_id' = NEW.rubric_source_version_id::text
                  AND source ->> 'content_digest' = NEW.rubric_content_digest)
          AND EXISTS (
                SELECT 1
                FROM jsonb_array_elements(
                    configuration.canonical_json::jsonb -> 'permitted_submissions') AS submission
                WHERE submission ->> 'content_digest' = NEW.submission_content_digest))
    THEN
        RAISE EXCEPTION 'evaluation request authorities are not frozen by the resolved configuration';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_evaluation_requests_verify_frozen_authorities
    BEFORE INSERT ON evaluation_requests
    FOR EACH ROW
    EXECUTE FUNCTION verify_evaluation_request_frozen_authorities();

CREATE TRIGGER trg_evaluation_requests_protect_identity
    BEFORE UPDATE OR DELETE ON evaluation_requests
    FOR EACH ROW
    EXECUTE FUNCTION protect_evaluation_request_identity();

CREATE OR REPLACE FUNCTION protect_evaluation_invocation_attempt()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'evaluation_invocation_attempts cannot be deleted';
    END IF;
    IF ROW(
        NEW.organization_id, NEW.request_id, NEW.invocation_attempt_id,
        NEW.activity_id, NEW.participant_id, NEW.attempt_id, NEW.session_id,
        NEW.attempt_ordinal, NEW.started_at)
       IS DISTINCT FROM
       ROW(
        OLD.organization_id, OLD.request_id, OLD.invocation_attempt_id,
        OLD.activity_id, OLD.participant_id, OLD.attempt_id, OLD.session_id,
        OLD.attempt_ordinal, OLD.started_at)
    THEN
        RAISE EXCEPTION 'evaluation_invocation_attempts identity is immutable';
    END IF;
    IF OLD.state IN ('completed', 'failed_retryable', 'failed_review_required', 'cancelled') THEN
        RAISE EXCEPTION 'evaluation_invocation_attempts terminal state is immutable';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_evaluation_invocation_attempts_protect
    BEFORE UPDATE OR DELETE ON evaluation_invocation_attempts
    FOR EACH ROW
    EXECUTE FUNCTION protect_evaluation_invocation_attempt();

DO $$
DECLARE
    table_name TEXT;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'evaluation_deterministic_attempts',
        'evaluation_provider_artifacts',
        'evaluation_evidence_items',
        'evaluation_evidence_sets',
        'evaluation_evidence_set_items',
        'evaluation_criterion_judgments',
        'evaluations',
        'evaluation_lineage',
        'evaluation_annotations',
        'evaluation_manifest_refs',
        'evaluation_review_handoffs',
        'evaluation_lifecycle_disposition_events'
    ]
    LOOP
        EXECUTE format(
            'CREATE TRIGGER trg_%I_immutable BEFORE UPDATE OR DELETE ON %I '
            'FOR EACH ROW EXECUTE FUNCTION reject_evaluation_append_only_mutation()',
            table_name,
            table_name);
    END LOOP;
END $$;

CREATE OR REPLACE FUNCTION stamp_evaluation_work_claim_partition()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO evaluation_work_claim_partitions (
        organization_id, activity_id, last_claimed_at, last_claimed_work_id)
    VALUES (
        NEW.organization_id, NEW.activity_id, clock_timestamp(), NEW.work_id)
    ON CONFLICT (organization_id, activity_id) DO UPDATE
    SET last_claimed_at = EXCLUDED.last_claimed_at,
        last_claimed_work_id = EXCLUDED.last_claimed_work_id;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_evaluation_durable_work_claim_partition
    AFTER INSERT OR UPDATE ON evaluation_durable_work
    FOR EACH ROW
    WHEN (NEW.state = 'claimed')
    EXECUTE FUNCTION stamp_evaluation_work_claim_partition();
