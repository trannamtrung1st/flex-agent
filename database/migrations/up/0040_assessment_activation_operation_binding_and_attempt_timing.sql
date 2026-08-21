-- Bind each idempotency key to the first command digest independently
-- of append-only execution attempts, and reject inverted attempt times.
-- Additive after 0039.

CREATE TABLE assessment_activation_operations (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    requested_cohort_id UUID NOT NULL,
    idempotency_key TEXT NOT NULL,
    command_digest CHAR(64) NOT NULL,
    bound_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT pk_assessment_activation_operations
        PRIMARY KEY (organization_id, activity_id, requested_cohort_id, idempotency_key),
    CONSTRAINT chk_assessment_activation_operations_key
        CHECK (char_length(btrim(idempotency_key)) BETWEEN 1 AND 128),
    CONSTRAINT chk_assessment_activation_operations_digest
        CHECK (command_digest = lower(command_digest) AND char_length(command_digest) = 64)
);

INSERT INTO assessment_activation_operations (
    organization_id,
    activity_id,
    requested_cohort_id,
    idempotency_key,
    command_digest,
    bound_at)
SELECT DISTINCT ON (organization_id, activity_id, requested_cohort_id, idempotency_key)
    organization_id,
    activity_id,
    requested_cohort_id,
    idempotency_key,
    command_digest,
    started_at
FROM assessment_activation_attempts
ORDER BY organization_id, activity_id, requested_cohort_id, idempotency_key, attempt_id;

ALTER TABLE assessment_activation_attempts
    ADD CONSTRAINT chk_assessment_activation_attempts_timing
    CHECK (finished_at >= started_at);
