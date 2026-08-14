-- Persist the successor Decision envelope and per-item validation/effect
-- without rewriting frozen 0005-0008. Historical v1 rows keep flattened
-- columns and a null envelope_json. v2 rows store the control envelope
-- plus independent output and requested-action validation rows.
-- UTC-ordered; additive after frozen 0008.

ALTER TABLE session_decisions
    ADD COLUMN IF NOT EXISTS envelope_schema_version TEXT NOT NULL DEFAULT 'v1';

ALTER TABLE session_decisions
    ADD COLUMN IF NOT EXISTS envelope_json JSONB NULL;

ALTER TABLE session_decisions
    DROP CONSTRAINT IF EXISTS chk_session_decisions_envelope_version;

ALTER TABLE session_decisions
    ADD CONSTRAINT chk_session_decisions_envelope_version
        CHECK (envelope_schema_version ~ '^v[0-9]+$');

ALTER TABLE session_decisions
    DROP CONSTRAINT IF EXISTS chk_session_decisions_envelope_presence;

ALTER TABLE session_decisions
    ADD CONSTRAINT chk_session_decisions_envelope_presence
        CHECK (
            (envelope_schema_version = 'v1' AND envelope_json IS NULL)
            OR (envelope_schema_version = 'v2' AND envelope_json IS NOT NULL));

ALTER TABLE session_decision_validations
    DROP CONSTRAINT IF EXISTS uq_session_decision_validations_ownership_revision;

ALTER TABLE session_decision_validations
    ADD CONSTRAINT uq_session_decision_validations_ownership_revision
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal);

CREATE TABLE session_decision_output_validations (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    agent_invocation_id TEXT NOT NULL,
    revision_ordinal INT NOT NULL,
    item_ordinal INT NOT NULL,
    local_ref TEXT NOT NULL,
    kind TEXT NOT NULL,
    validation_outcome TEXT NOT NULL,
    rejection_reason_category TEXT NULL,
    agent_output_id TEXT NULL,
    effect_outcome TEXT NOT NULL,
    PRIMARY KEY (organization_id, session_id, agent_invocation_id, revision_ordinal, item_ordinal),
    CONSTRAINT fk_session_decision_output_validations_revision
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal)
        REFERENCES session_decision_validations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal),
    CONSTRAINT chk_session_decision_output_validations_ordinal
        CHECK (item_ordinal >= 0),
    CONSTRAINT chk_session_decision_output_validations_outcome
        CHECK (validation_outcome IN ('accepted', 'rejected')),
    CONSTRAINT chk_session_decision_output_validations_effect
        CHECK (effect_outcome IN ('applied', 'no_domain_effect', 'effect_failed', 'not_attempted')),
    CONSTRAINT chk_session_decision_output_validations_output_id
        CHECK (agent_output_id IS NULL OR validation_outcome = 'accepted')
);

CREATE TRIGGER trg_session_decision_output_validations_no_update
    BEFORE UPDATE ON session_decision_output_validations
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_decision_output_validations_no_delete
    BEFORE DELETE ON session_decision_output_validations
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_decision_requested_action_validations (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    agent_invocation_id TEXT NOT NULL,
    revision_ordinal INT NOT NULL,
    item_ordinal INT NOT NULL,
    local_ref TEXT NOT NULL,
    kind TEXT NOT NULL,
    validation_outcome TEXT NOT NULL,
    rejection_reason_category TEXT NULL,
    effect_outcome TEXT NOT NULL,
    PRIMARY KEY (organization_id, session_id, agent_invocation_id, revision_ordinal, item_ordinal),
    CONSTRAINT fk_session_decision_action_validations_revision
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal)
        REFERENCES session_decision_validations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal),
    CONSTRAINT chk_session_decision_action_validations_ordinal
        CHECK (item_ordinal >= 0),
    CONSTRAINT chk_session_decision_action_validations_outcome
        CHECK (validation_outcome IN ('accepted', 'rejected')),
    CONSTRAINT chk_session_decision_action_validations_effect
        CHECK (effect_outcome IN ('applied', 'no_domain_effect', 'effect_failed', 'not_attempted'))
);

CREATE TRIGGER trg_session_decision_action_validations_no_update
    BEFORE UPDATE ON session_decision_requested_action_validations
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_decision_action_validations_no_delete
    BEFORE DELETE ON session_decision_requested_action_validations
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();
