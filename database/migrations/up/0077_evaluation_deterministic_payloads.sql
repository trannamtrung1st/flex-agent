-- Additive protected deterministic-evaluator output payload store.
-- Additive after frozen 0072/0076. Do not edit prior migrations.

CREATE TABLE evaluation_deterministic_payloads (
    organization_id UUID NOT NULL,
    deterministic_attempt_id UUID NOT NULL,
    request_id UUID NOT NULL,
    protected_ref TEXT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    output_utf8 BYTEA NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, deterministic_attempt_id),
    CONSTRAINT fk_evaluation_deterministic_payloads_attempt
        FOREIGN KEY (organization_id, deterministic_attempt_id)
        REFERENCES evaluation_deterministic_attempts (organization_id, deterministic_attempt_id),
    CONSTRAINT fk_evaluation_deterministic_payloads_request
        FOREIGN KEY (organization_id, request_id)
        REFERENCES evaluation_requests (organization_id, request_id),
    CONSTRAINT uq_evaluation_deterministic_payloads_digest
        UNIQUE (organization_id, deterministic_attempt_id, content_digest),
    CONSTRAINT chk_evaluation_deterministic_payloads_digest
        CHECK (
            content_digest = lower(content_digest)
            AND char_length(content_digest) = 64),
    CONSTRAINT chk_evaluation_deterministic_payloads_ref
        CHECK (char_length(protected_ref) BETWEEN 1 AND 256),
    CONSTRAINT chk_evaluation_deterministic_payloads_bytes
        CHECK (octet_length(output_utf8) BETWEEN 1 AND 1048576)
);

CREATE OR REPLACE FUNCTION reject_evaluation_deterministic_payload_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'UPDATE'
        OR current_setting('flex_agent.allow_payload_disposition', true) IS DISTINCT FROM 'true' THEN
        RAISE EXCEPTION 'evaluation_deterministic_payloads are immutable';
    END IF;
    RETURN OLD;
END;
$$;

CREATE TRIGGER trg_evaluation_deterministic_payloads_no_update
    BEFORE UPDATE ON evaluation_deterministic_payloads
    FOR EACH ROW
    EXECUTE FUNCTION reject_evaluation_deterministic_payload_mutation();

CREATE TRIGGER trg_evaluation_deterministic_payloads_no_delete
    BEFORE DELETE ON evaluation_deterministic_payloads
    FOR EACH ROW
    EXECUTE FUNCTION reject_evaluation_deterministic_payload_mutation();
