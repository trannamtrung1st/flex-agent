-- Append-only manifest runtime records, terminal seal columns, and Evaluation
-- handoff eligibility. Do not rewrite frozen 0005-0016.
-- UTC-ordered; additive after frozen 0016.

ALTER TABLE session_terminal_records
    ADD COLUMN IF NOT EXISTS reason_category TEXT NULL;

ALTER TABLE session_terminal_records
    ADD COLUMN IF NOT EXISTS attempt_mapping TEXT NULL;

ALTER TABLE session_terminal_records
    ADD COLUMN IF NOT EXISTS procedure_id TEXT NULL;

ALTER TABLE session_terminal_records
    ADD COLUMN IF NOT EXISTS seal_digest CHAR(64) NULL;

ALTER TABLE session_terminal_records
    DROP CONSTRAINT IF EXISTS chk_session_terminal_records_lifecycle;

ALTER TABLE session_terminal_records
    ADD CONSTRAINT chk_session_terminal_records_lifecycle
        CHECK (lifecycle_state IN ('completed', 'terminated', 'aborted'));

ALTER TABLE session_terminal_records
    DROP CONSTRAINT IF EXISTS chk_session_terminal_records_attempt_mapping;

ALTER TABLE session_terminal_records
    ADD CONSTRAINT chk_session_terminal_records_attempt_mapping
        CHECK (attempt_mapping IS NULL OR attempt_mapping IN ('completed', 'aborted'));

ALTER TABLE session_terminal_records
    DROP CONSTRAINT IF EXISTS chk_session_terminal_records_seal;

-- Pre-0017 rows stay NULL on the new columns. Those tables are append-only, so
-- this migration must not fabricate a seal or rewrite historical terminal rows.
-- NULL procedure_id + NULL seal fields is the legacy-unsealed representation.
ALTER TABLE session_terminal_records
    ADD CONSTRAINT chk_session_terminal_records_seal
        CHECK (
            (
                procedure_id IS NULL
                AND seal_digest IS NULL
                AND reason_category IS NULL
                AND attempt_mapping IS NULL
            )
            OR (
                procedure_id = 'manifest-jcs-sha256-v1'
                AND seal_digest = lower(seal_digest)
                AND char_length(seal_digest) = 64
                AND reason_category IS NOT NULL
                AND attempt_mapping IS NOT NULL
            )
            OR (
                procedure_id = 'manifest-jcs-sha256-v2'
                AND cutoff_sequence IS NOT NULL
                AND seal_digest = lower(seal_digest)
                AND char_length(seal_digest) = 64
                AND reason_category IS NOT NULL
                AND attempt_mapping IS NOT NULL
            ));

CREATE TABLE session_manifest_runtime_records (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    manifest_sequence BIGINT NOT NULL,
    record_type TEXT NOT NULL,
    service_actor TEXT NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    protected_ref TEXT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    session_sequence BIGINT NOT NULL,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, manifest_sequence),
    CONSTRAINT fk_session_manifest_runtime_records_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT uq_session_manifest_runtime_records_payload
        UNIQUE (organization_id, session_id, record_type, protected_ref),
    CONSTRAINT chk_session_manifest_runtime_records_type
        CHECK (record_type IN ('model.invocation.v1', 'transcript.append.v1', 'timer.event.v1')),
    CONSTRAINT chk_session_manifest_runtime_records_sequence
        CHECK (manifest_sequence >= 1),
    CONSTRAINT chk_session_manifest_runtime_records_digest
        CHECK (content_digest = lower(content_digest) AND char_length(content_digest) = 64)
);

CREATE TRIGGER trg_session_manifest_runtime_records_stamp_committed
    BEFORE INSERT ON session_manifest_runtime_records
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_manifest_runtime_records_no_update
    BEFORE UPDATE ON session_manifest_runtime_records
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_manifest_runtime_records_no_delete
    BEFORE DELETE ON session_manifest_runtime_records
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_evaluation_handoffs (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    handoff_id TEXT NOT NULL,
    eligibility TEXT NOT NULL,
    terminal_state TEXT NOT NULL,
    cutoff_sequence BIGINT NULL,
    configuration_id TEXT NOT NULL,
    configuration_digest CHAR(64) NOT NULL,
    manifest_id TEXT NOT NULL,
    seal_digest CHAR(64) NOT NULL,
    committed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id),
    CONSTRAINT fk_session_evaluation_handoffs_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT uq_session_evaluation_handoffs_id
        UNIQUE (organization_id, session_id, handoff_id),
    CONSTRAINT chk_session_evaluation_handoffs_eligibility
        CHECK (eligibility IN ('eligible', 'ineligible')),
    CONSTRAINT chk_session_evaluation_handoffs_terminal
        CHECK (terminal_state IN ('completed', 'terminated', 'aborted')),
    CONSTRAINT chk_session_evaluation_handoffs_eligible_completed
        CHECK (eligibility <> 'eligible' OR terminal_state = 'completed'),
    CONSTRAINT chk_session_evaluation_handoffs_digest
        CHECK (
            configuration_digest = lower(configuration_digest)
            AND seal_digest = lower(seal_digest)
            AND char_length(configuration_digest) = 64
            AND char_length(seal_digest) = 64)
);

CREATE TRIGGER trg_session_evaluation_handoffs_stamp_committed
    BEFORE INSERT ON session_evaluation_handoffs
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_committed_at();

CREATE TRIGGER trg_session_evaluation_handoffs_no_update
    BEFORE UPDATE ON session_evaluation_handoffs
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_evaluation_handoffs_no_delete
    BEFORE DELETE ON session_evaluation_handoffs
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();
