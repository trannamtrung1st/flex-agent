-- Harden Session runtime invariants after 0005.
-- Do not edit 0005; this additive script is the repair path for already-applied
-- 0005 databases. Capability/policy acceptance stays on validation rows.
-- UTC-ordered; do not edit after merge.

ALTER TABLE session_decisions
    DROP CONSTRAINT IF EXISTS chk_session_decisions_type;

ALTER TABLE session_invocations
    ADD COLUMN IF NOT EXISTS last_session_sequence BIGINT;

UPDATE session_invocations
SET last_session_sequence = admitted_session_sequence
WHERE last_session_sequence IS NULL;

ALTER TABLE session_invocations
    ALTER COLUMN last_session_sequence SET NOT NULL;

ALTER TABLE session_invocations
    DROP CONSTRAINT IF EXISTS chk_session_invocations_last_sequence;

ALTER TABLE session_invocations
    ADD CONSTRAINT chk_session_invocations_last_sequence
        CHECK (last_session_sequence >= admitted_session_sequence);

CREATE OR REPLACE FUNCTION stamp_session_invocation_last_sequence()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.last_session_sequence IS NULL THEN
        NEW.last_session_sequence := NEW.admitted_session_sequence;
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_session_invocations_stamp_last_sequence ON session_invocations;

CREATE TRIGGER trg_session_invocations_stamp_last_sequence
    BEFORE INSERT ON session_invocations
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_invocation_last_sequence();

ALTER TABLE session_invocations
    DROP CONSTRAINT IF EXISTS uq_session_invocations_ownership_identity;

ALTER TABLE session_invocations
    ADD CONSTRAINT uq_session_invocations_ownership_identity
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id);

ALTER TABLE session_invocation_attempts
    DROP CONSTRAINT IF EXISTS fk_session_invocation_attempts_invocation;

ALTER TABLE session_invocation_attempts
    ADD CONSTRAINT fk_session_invocation_attempts_invocation
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id)
        REFERENCES session_invocations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id);

ALTER TABLE session_decisions
    DROP CONSTRAINT IF EXISTS fk_session_decisions_invocation;

ALTER TABLE session_decisions
    ADD CONSTRAINT fk_session_decisions_invocation
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id)
        REFERENCES session_invocations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id);

ALTER TABLE session_decisions
    ADD COLUMN IF NOT EXISTS committed_session_sequence BIGINT NOT NULL DEFAULT 0;

ALTER TABLE session_decision_validations
    DROP CONSTRAINT IF EXISTS fk_session_decision_validations_invocation;

ALTER TABLE session_decision_validations
    ADD CONSTRAINT fk_session_decision_validations_invocation
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id)
        REFERENCES session_invocations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id);

ALTER TABLE session_execution_outcomes
    DROP CONSTRAINT IF EXISTS fk_session_execution_outcomes_invocation;

ALTER TABLE session_execution_outcomes
    ADD CONSTRAINT fk_session_execution_outcomes_invocation
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id)
        REFERENCES session_invocations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id);

ALTER TABLE session_execution_outcomes
    ADD COLUMN IF NOT EXISTS committed_session_version BIGINT NOT NULL DEFAULT 0;

ALTER TABLE session_execution_outcomes
    ADD COLUMN IF NOT EXISTS committed_session_sequence BIGINT NOT NULL DEFAULT 0;

ALTER TABLE session_decision_validations
    DROP CONSTRAINT IF EXISTS chk_session_decision_validations_effect_commit_state;

ALTER TABLE session_decision_validations
    ADD CONSTRAINT chk_session_decision_validations_effect_commit_state
        CHECK (
            (
                effect_outcome = 'not_attempted'
                AND effect_commit_session_version IS NULL
                AND effect_commit_session_sequence IS NULL
                AND effect_committed_at IS NULL
                AND applied_turn_id IS NULL
                AND applied_response_slot_id IS NULL
            )
            OR (
                effect_outcome IN ('applied', 'no_domain_effect', 'effect_failed')
                AND effect_commit_session_version IS NOT NULL
                AND effect_commit_session_sequence IS NOT NULL
                AND effect_committed_at IS NOT NULL
            ));

CREATE OR REPLACE FUNCTION reject_session_decision_validation_terminal_insert()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.effect_outcome IS DISTINCT FROM 'not_attempted'
        OR NEW.effect_commit_session_version IS NOT NULL
        OR NEW.effect_commit_session_sequence IS NOT NULL
        OR NEW.effect_committed_at IS NOT NULL
        OR NEW.applied_turn_id IS NOT NULL
        OR NEW.applied_response_slot_id IS NOT NULL
    THEN
        RAISE EXCEPTION 'session_decision_validations insert must start as not_attempted';
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_session_decision_validations_reject_terminal_insert
    ON session_decision_validations;

CREATE TRIGGER trg_session_decision_validations_reject_terminal_insert
    BEFORE INSERT ON session_decision_validations
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_decision_validation_terminal_insert();

CREATE OR REPLACE FUNCTION enforce_session_decision_effect_transition()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    latest_ordinal INT;
BEGIN
    IF NEW.organization_id IS DISTINCT FROM OLD.organization_id
        OR NEW.activity_id IS DISTINCT FROM OLD.activity_id
        OR NEW.participant_id IS DISTINCT FROM OLD.participant_id
        OR NEW.attempt_id IS DISTINCT FROM OLD.attempt_id
        OR NEW.session_id IS DISTINCT FROM OLD.session_id
        OR NEW.agent_invocation_id IS DISTINCT FROM OLD.agent_invocation_id
        OR NEW.revision_ordinal IS DISTINCT FROM OLD.revision_ordinal
        OR NEW.validated_against_session_version IS DISTINCT FROM OLD.validated_against_session_version
        OR NEW.validated_against_session_sequence IS DISTINCT FROM OLD.validated_against_session_sequence
        OR NEW.validation_commit_session_version IS DISTINCT FROM OLD.validation_commit_session_version
        OR NEW.validation_commit_session_sequence IS DISTINCT FROM OLD.validation_commit_session_sequence
        OR NEW.validation_committed_at IS DISTINCT FROM OLD.validation_committed_at
        OR NEW.validation_outcome IS DISTINCT FROM OLD.validation_outcome
        OR NEW.timer_validation_outcome IS DISTINCT FROM OLD.timer_validation_outcome
        OR NEW.rejection_reason_category IS DISTINCT FROM OLD.rejection_reason_category
    THEN
        RAISE EXCEPTION 'session_decision_validations validation identity is append-only';
    END IF;

    IF OLD.effect_outcome IN ('applied', 'no_domain_effect', 'effect_failed') THEN
        RAISE EXCEPTION 'session_decision_validations terminal effect is immutable';
    END IF;

    SELECT MAX(revision_ordinal)
    INTO latest_ordinal
    FROM session_decision_validations
    WHERE organization_id = OLD.organization_id
      AND session_id = OLD.session_id
      AND agent_invocation_id = OLD.agent_invocation_id;

    IF OLD.revision_ordinal IS DISTINCT FROM latest_ordinal THEN
        RAISE EXCEPTION 'session_decision_validations effect can only be applied on the latest revision';
    END IF;

    IF OLD.validation_outcome IS DISTINCT FROM 'accepted' THEN
        RAISE EXCEPTION 'session_decision_validations effect requires accepted validation';
    END IF;

    IF OLD.effect_outcome IS DISTINCT FROM 'not_attempted'
        OR NEW.effect_outcome NOT IN ('applied', 'no_domain_effect', 'effect_failed')
    THEN
        RAISE EXCEPTION 'session_decision_validations effect must transition not_attempted to terminal';
    END IF;

    NEW.effect_committed_at := clock_timestamp();
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_session_decision_validations_protect_identity
    ON session_decision_validations;

CREATE TRIGGER trg_session_decision_validations_protect_identity
    BEFORE UPDATE ON session_decision_validations
    FOR EACH ROW
    EXECUTE FUNCTION enforce_session_decision_effect_transition();
