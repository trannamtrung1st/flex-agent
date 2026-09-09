-- Persist deterministic output content digest for provenance reconciliation.
-- Additive after frozen 0072. Do not edit 0072.

ALTER TABLE evaluation_deterministic_attempts
    ADD COLUMN output_content_digest CHAR(64) NULL;

ALTER TABLE evaluation_deterministic_attempts
    ADD CONSTRAINT chk_evaluation_deterministic_attempts_output_digest
    CHECK (
        output_content_digest IS NULL
        OR (
            output_content_digest = lower(output_content_digest)
            AND char_length(output_content_digest) = 64));
