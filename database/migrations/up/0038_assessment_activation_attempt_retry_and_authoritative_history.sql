-- Failed activation attempts are immutable evidence, not sticky idempotent
-- completions. Keep one successful outcome per client key and stop treating
-- historical expected-revision values as authoritative. Additive after 0037.

UPDATE assessment_activation_attempts
SET authoritative_revision_id = NULL,
    authoritative_revision_number = NULL
WHERE outcome_code <> 'assessment.activated';

ALTER TABLE assessment_activation_attempts
    DROP CONSTRAINT uq_assessment_activation_attempts_idempotency;

CREATE UNIQUE INDEX uq_assessment_activation_attempts_successful_idempotency
    ON assessment_activation_attempts (organization_id, activity_id, cohort_id, idempotency_key)
    WHERE outcome_code = 'assessment.activated';
