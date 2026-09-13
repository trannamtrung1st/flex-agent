-- Immutable canonical-input authority per invocation criterion scope.
-- Additive after 0080. Do not edit frozen prior migrations.

CREATE TABLE evaluation_deterministic_input_authority (
    organization_id UUID NOT NULL,
    request_id UUID NOT NULL,
    invocation_attempt_id UUID NOT NULL,
    criterion_id TEXT NOT NULL,
    criterion_version TEXT NOT NULL,
    canonical_input_digest CHAR(64) NOT NULL,
    protected_input_ref TEXT NOT NULL,
    input_utf8 BYTEA NOT NULL,
    established_by_attempt_id UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (
        organization_id,
        request_id,
        invocation_attempt_id,
        criterion_id,
        criterion_version),
    CONSTRAINT fk_evaluation_deterministic_input_authority_request
        FOREIGN KEY (organization_id, request_id)
        REFERENCES evaluation_requests (organization_id, request_id),
    CONSTRAINT fk_evaluation_deterministic_input_authority_invocation
        FOREIGN KEY (organization_id, request_id, invocation_attempt_id)
        REFERENCES evaluation_invocation_attempts (
            organization_id,
            request_id,
            invocation_attempt_id),
    CONSTRAINT fk_evaluation_deterministic_input_authority_attempt
        FOREIGN KEY (organization_id, request_id, established_by_attempt_id)
        REFERENCES evaluation_deterministic_attempts (
            organization_id,
            request_id,
            deterministic_attempt_id),
    CONSTRAINT chk_evaluation_deterministic_input_authority_digest
        CHECK (
            canonical_input_digest = lower(canonical_input_digest)
            AND char_length(canonical_input_digest) = 64),
    CONSTRAINT chk_evaluation_deterministic_input_authority_ref
        CHECK (char_length(protected_input_ref) BETWEEN 1 AND 256),
    CONSTRAINT chk_evaluation_deterministic_input_authority_bytes
        CHECK (octet_length(input_utf8) BETWEEN 1 AND 1048576)
);

CREATE OR REPLACE FUNCTION reject_evaluation_deterministic_input_authority_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'evaluation_deterministic_input_authority is immutable';
END;
$$;

CREATE TRIGGER trg_evaluation_deterministic_input_authority_no_update
    BEFORE UPDATE ON evaluation_deterministic_input_authority
    FOR EACH ROW
    EXECUTE FUNCTION reject_evaluation_deterministic_input_authority_mutation();

CREATE TRIGGER trg_evaluation_deterministic_input_authority_no_delete
    BEFORE DELETE ON evaluation_deterministic_input_authority
    FOR EACH ROW
    EXECUTE FUNCTION reject_evaluation_deterministic_input_authority_mutation();
