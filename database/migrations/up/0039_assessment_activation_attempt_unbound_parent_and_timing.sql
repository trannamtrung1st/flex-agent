-- Record requested Cohort identity separately from an optional parent
-- binding, and persist attempt start/end times. Do not restore
-- authoritative revision columns nulled by 0038: distinct 0037-runtime
-- stale-failure evidence cannot be reconstructed safely. Additive after 0038.

ALTER TABLE assessment_activation_attempts
    ADD COLUMN requested_cohort_id UUID,
    ADD COLUMN started_at TIMESTAMPTZ,
    ADD COLUMN finished_at TIMESTAMPTZ;

UPDATE assessment_activation_attempts
SET requested_cohort_id = cohort_id,
    started_at = created_at,
    finished_at = created_at;

ALTER TABLE assessment_activation_attempts
    ALTER COLUMN requested_cohort_id SET NOT NULL;

ALTER TABLE assessment_activation_attempts
    ALTER COLUMN started_at SET NOT NULL;

ALTER TABLE assessment_activation_attempts
    ALTER COLUMN finished_at SET NOT NULL;

ALTER TABLE assessment_activation_attempts
    ALTER COLUMN cohort_id DROP NOT NULL;

DROP INDEX uq_assessment_activation_attempts_successful_idempotency;

CREATE UNIQUE INDEX uq_assessment_activation_attempts_successful_idempotency
    ON assessment_activation_attempts (organization_id, activity_id, requested_cohort_id, idempotency_key)
    WHERE outcome_code = 'assessment.activated';
