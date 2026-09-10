-- Harden deterministic payload immutability and attempt provenance binding.
-- Additive after 0077. Do not edit frozen prior migrations.

ALTER TABLE evaluation_deterministic_payloads
    DROP CONSTRAINT fk_evaluation_deterministic_payloads_attempt;

ALTER TABLE evaluation_deterministic_payloads
    DROP CONSTRAINT fk_evaluation_deterministic_payloads_request;

ALTER TABLE evaluation_deterministic_payloads
    ADD CONSTRAINT fk_evaluation_deterministic_payloads_attempt
        FOREIGN KEY (organization_id, request_id, deterministic_attempt_id)
        REFERENCES evaluation_deterministic_attempts (
            organization_id,
            request_id,
            deterministic_attempt_id);

CREATE OR REPLACE FUNCTION reject_evaluation_deterministic_payload_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'evaluation_deterministic_payloads are immutable';
END;
$$;
