-- Allow only NULL → exact Attempt binding on acknowledgment records.
-- Identity, version, digest, outcome, and timestamp remain immutable.
-- Do not edit 0001-0063.

CREATE OR REPLACE FUNCTION reject_acknowledgment_identity_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.organization_id IS DISTINCT FROM OLD.organization_id
        OR NEW.record_id IS DISTINCT FROM OLD.record_id
        OR NEW.enrollment_id IS DISTINCT FROM OLD.enrollment_id
        OR NEW.participant_actor_id IS DISTINCT FROM OLD.participant_actor_id
        OR NEW.notice_id IS DISTINCT FROM OLD.notice_id
        OR NEW.source_id IS DISTINCT FROM OLD.source_id
        OR NEW.source_version_id IS DISTINCT FROM OLD.source_version_id
        OR NEW.source_content_digest IS DISTINCT FROM OLD.source_content_digest
        OR NEW.notice_content_digest IS DISTINCT FROM OLD.notice_content_digest
        OR NEW.outcome IS DISTINCT FROM OLD.outcome
        OR NEW.recorded_at IS DISTINCT FROM OLD.recorded_at
        OR NEW.idempotency_key IS DISTINCT FROM OLD.idempotency_key
        OR NEW.command_digest IS DISTINCT FROM OLD.command_digest
    THEN
        RAISE EXCEPTION 'session_acknowledgment_records identity is immutable';
    END IF;
    IF OLD.bound_attempt_id IS NOT NULL
        OR NEW.bound_attempt_id IS NULL
        OR NEW.bound_attempt_id IS NOT DISTINCT FROM OLD.bound_attempt_id
    THEN
        RAISE EXCEPTION 'session_acknowledgment_records may only bind once from unbound to an Attempt';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_session_acknowledgment_records_protect_identity
    BEFORE UPDATE ON session_acknowledgment_records
    FOR EACH ROW
    EXECUTE FUNCTION reject_acknowledgment_identity_mutation();

CREATE TRIGGER trg_session_acknowledgment_records_no_delete
    BEFORE DELETE ON session_acknowledgment_records
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();
