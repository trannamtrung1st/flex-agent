-- Record per-item Decision effect as append-only facts, separate from
-- item validation. Frozen 0009 child rows keep a dual-read effect_outcome
-- column; new writes leave that column at not_attempted and append a
-- terminal effect row when an effect is attempted. Absence of an effect
-- row is explicit not_attempted unless a pre-0010 child column already
-- stored a terminal outcome. Do not rewrite 0005-0009.
-- UTC-ordered; additive after frozen 0009.

CREATE TABLE session_decision_output_effects (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    agent_invocation_id TEXT NOT NULL,
    revision_ordinal INT NOT NULL,
    item_ordinal INT NOT NULL,
    effect_outcome TEXT NOT NULL,
    PRIMARY KEY (organization_id, session_id, agent_invocation_id, revision_ordinal, item_ordinal),
    CONSTRAINT fk_session_decision_output_effects_item
        FOREIGN KEY (
            organization_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal)
        REFERENCES session_decision_output_validations (
            organization_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal),
    CONSTRAINT chk_session_decision_output_effects_outcome
        CHECK (effect_outcome IN ('applied', 'no_domain_effect', 'effect_failed'))
);

CREATE TRIGGER trg_session_decision_output_effects_no_update
    BEFORE UPDATE ON session_decision_output_effects
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_decision_output_effects_no_delete
    BEFORE DELETE ON session_decision_output_effects
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_decision_requested_action_effects (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    agent_invocation_id TEXT NOT NULL,
    revision_ordinal INT NOT NULL,
    item_ordinal INT NOT NULL,
    effect_outcome TEXT NOT NULL,
    PRIMARY KEY (organization_id, session_id, agent_invocation_id, revision_ordinal, item_ordinal),
    CONSTRAINT fk_session_decision_action_effects_item
        FOREIGN KEY (
            organization_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal)
        REFERENCES session_decision_requested_action_validations (
            organization_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal),
    CONSTRAINT chk_session_decision_action_effects_outcome
        CHECK (effect_outcome IN ('applied', 'no_domain_effect', 'effect_failed'))
);

CREATE TRIGGER trg_session_decision_action_effects_no_update
    BEFORE UPDATE ON session_decision_requested_action_effects
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_decision_action_effects_no_delete
    BEFORE DELETE ON session_decision_requested_action_effects
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();
