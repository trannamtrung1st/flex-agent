-- Bind per-item Decision effect facts to the complete ownership tuple
-- and only accepted validation items. Frozen 0010 FKs keyed only
-- organization/session/invocation/revision/item, so a mismatched
-- activity, participant, or attempt could still insert. Do not rewrite
-- 0005-0010.
-- UTC-ordered; additive after frozen 0010.

ALTER TABLE session_decision_output_validations
    DROP CONSTRAINT IF EXISTS uq_session_decision_output_validations_ownership_item;

ALTER TABLE session_decision_output_validations
    ADD CONSTRAINT uq_session_decision_output_validations_ownership_item
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal);

ALTER TABLE session_decision_output_validations
    DROP CONSTRAINT IF EXISTS uq_session_decision_output_validations_ownership_item_outcome;

ALTER TABLE session_decision_output_validations
    ADD CONSTRAINT uq_session_decision_output_validations_ownership_item_outcome
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal,
            validation_outcome);

ALTER TABLE session_decision_requested_action_validations
    DROP CONSTRAINT IF EXISTS uq_session_decision_action_validations_ownership_item;

ALTER TABLE session_decision_requested_action_validations
    ADD CONSTRAINT uq_session_decision_action_validations_ownership_item
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal);

ALTER TABLE session_decision_requested_action_validations
    DROP CONSTRAINT IF EXISTS uq_session_decision_action_validations_ownership_item_outcome;

ALTER TABLE session_decision_requested_action_validations
    ADD CONSTRAINT uq_session_decision_action_validations_ownership_item_outcome
        UNIQUE (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal,
            validation_outcome);

ALTER TABLE session_decision_output_effects
    DROP CONSTRAINT IF EXISTS fk_session_decision_output_effects_item;

ALTER TABLE session_decision_output_effects
    ADD COLUMN IF NOT EXISTS validation_outcome TEXT NOT NULL DEFAULT 'accepted';

ALTER TABLE session_decision_output_effects
    DROP CONSTRAINT IF EXISTS chk_session_decision_output_effects_accepted_item;

ALTER TABLE session_decision_output_effects
    ADD CONSTRAINT chk_session_decision_output_effects_accepted_item
        CHECK (validation_outcome = 'accepted');

ALTER TABLE session_decision_output_effects
    ADD CONSTRAINT fk_session_decision_output_effects_item
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal,
            validation_outcome)
        REFERENCES session_decision_output_validations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal,
            validation_outcome);

ALTER TABLE session_decision_requested_action_effects
    DROP CONSTRAINT IF EXISTS fk_session_decision_action_effects_item;

ALTER TABLE session_decision_requested_action_effects
    ADD COLUMN IF NOT EXISTS validation_outcome TEXT NOT NULL DEFAULT 'accepted';

ALTER TABLE session_decision_requested_action_effects
    DROP CONSTRAINT IF EXISTS chk_session_decision_action_effects_accepted_item;

ALTER TABLE session_decision_requested_action_effects
    ADD CONSTRAINT chk_session_decision_action_effects_accepted_item
        CHECK (validation_outcome = 'accepted');

ALTER TABLE session_decision_requested_action_effects
    ADD CONSTRAINT fk_session_decision_action_effects_item
        FOREIGN KEY (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal,
            validation_outcome)
        REFERENCES session_decision_requested_action_validations (
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            agent_invocation_id,
            revision_ordinal,
            item_ordinal,
            validation_outcome);
