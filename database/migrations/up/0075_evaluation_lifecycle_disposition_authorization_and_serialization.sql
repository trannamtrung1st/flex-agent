-- Serialize legal-hold mutation versus provider-artifact disposal and restrict
-- physical disposal to a dedicated lifecycle executor with delegation proof.
-- Additive after frozen 0074.

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'flexagent_lifecycle_executor') THEN
        CREATE ROLE flexagent_lifecycle_executor NOLOGIN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'flexagent_application') THEN
        CREATE ROLE flexagent_application NOLOGIN;
    END IF;
END $$;

CREATE OR REPLACE FUNCTION serialize_evaluation_lifecycle_hold_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    PERFORM 1
    FROM evaluations
    WHERE organization_id = NEW.organization_id
      AND evaluation_id = NEW.evaluation_id
    FOR UPDATE;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_evaluation_lifecycle_holds_serialize ON evaluation_lifecycle_holds;

CREATE TRIGGER trg_evaluation_lifecycle_holds_serialize
    BEFORE INSERT OR UPDATE ON evaluation_lifecycle_holds
    FOR EACH ROW
    EXECUTE FUNCTION serialize_evaluation_lifecycle_hold_mutation();

DROP FUNCTION IF EXISTS dispose_evaluation_provider_artifact(
    UUID,
    UUID,
    UUID,
    UUID,
    TEXT,
    UUID);

CREATE OR REPLACE FUNCTION dispose_evaluation_provider_artifact(
    p_organization_id UUID,
    p_evaluation_id UUID,
    p_provider_artifact_id UUID,
    p_audit_event_id UUID,
    p_delegation_id UUID,
    p_reason_code TEXT,
    p_actor_id UUID)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_disposition_event_id UUID := gen_random_uuid();
    v_request evaluation_requests%ROWTYPE;
BEGIN
    IF p_reason_code NOT IN ('retention_expired', 'authorized_erasure') THEN
        RAISE EXCEPTION 'invalid lifecycle disposition reason';
    END IF;

    SELECT request.*
    INTO v_request
    FROM evaluations AS evaluation
    INNER JOIN evaluation_requests AS request
      ON request.organization_id = evaluation.organization_id
     AND request.request_id = evaluation.request_id
    WHERE evaluation.organization_id = p_organization_id
      AND evaluation.evaluation_id = p_evaluation_id
    FOR UPDATE OF evaluation;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'evaluation not found';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM evaluation_provider_artifacts AS artifact
        WHERE artifact.organization_id = p_organization_id
          AND artifact.provider_artifact_id = p_provider_artifact_id
          AND artifact.request_id = v_request.request_id)
    THEN
        RAISE EXCEPTION 'provider artifact not found for evaluation';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM service_delegations AS delegation
        WHERE delegation.delegation_id = p_delegation_id
          AND delegation.organization_id = p_organization_id
          AND delegation.activity_id = v_request.activity_id
          AND delegation.participant_id = v_request.participant_id
          AND delegation.attempt_id = v_request.attempt_id
          AND delegation.session_id = v_request.session_id
          AND delegation.service_actor_id = p_actor_id
          AND delegation.allowed_action = 'evaluation.lifecycle.dispose'
          AND delegation.revoked_at IS NULL
          AND delegation.effective_at <= clock_timestamp()
          AND (delegation.expires_at IS NULL OR delegation.expires_at > clock_timestamp()))
    THEN
        RAISE EXCEPTION 'evaluation.lifecycle.dispose delegation required';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM evaluation_lifecycle_holds
        WHERE organization_id = p_organization_id
          AND evaluation_id = p_evaluation_id
          AND active)
    THEN
        RAISE EXCEPTION 'evaluations have an active legal hold';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM audit_events
        WHERE event_id = p_audit_event_id
          AND organization_id = p_organization_id
          AND actor_id = p_actor_id
          AND action = 'evaluation.lifecycle.dispose'
          AND resource_type = 'evaluation'
          AND resource_id = p_evaluation_id
          AND outcome = 'succeeded')
    THEN
        RAISE EXCEPTION 'evaluation.lifecycle.dispose audit event required';
    END IF;

    INSERT INTO evaluation_lifecycle_disposition_events (
        organization_id,
        disposition_event_id,
        evaluation_id,
        object_kind,
        object_id,
        reason_code,
        actor_id,
        audit_event_id,
        disposed_at)
    VALUES (
        p_organization_id,
        v_disposition_event_id,
        p_evaluation_id,
        'provider_artifact',
        p_provider_artifact_id,
        p_reason_code,
        p_actor_id,
        p_audit_event_id,
        clock_timestamp());

    DELETE FROM evaluation_provider_artifacts
    WHERE organization_id = p_organization_id
      AND provider_artifact_id = p_provider_artifact_id;
END;
$$;

ALTER FUNCTION dispose_evaluation_provider_artifact(
    UUID,
    UUID,
    UUID,
    UUID,
    UUID,
    TEXT,
    UUID) OWNER TO flexagent_lifecycle_executor;

REVOKE ALL ON FUNCTION dispose_evaluation_provider_artifact(
    UUID,
    UUID,
    UUID,
    UUID,
    UUID,
    TEXT,
    UUID) FROM PUBLIC;

REVOKE ALL ON FUNCTION dispose_evaluation_provider_artifact(
    UUID,
    UUID,
    UUID,
    UUID,
    UUID,
    TEXT,
    UUID) FROM flexagent;

REVOKE ALL ON FUNCTION dispose_evaluation_provider_artifact(
    UUID,
    UUID,
    UUID,
    UUID,
    UUID,
    TEXT,
    UUID) FROM flexagent_application;

GRANT EXECUTE ON FUNCTION dispose_evaluation_provider_artifact(
    UUID,
    UUID,
    UUID,
    UUID,
    UUID,
    TEXT,
    UUID) TO flexagent_lifecycle_executor;

GRANT SELECT, DELETE ON evaluation_provider_artifacts TO flexagent_lifecycle_executor;
GRANT SELECT, INSERT ON evaluation_lifecycle_disposition_events TO flexagent_lifecycle_executor;
GRANT SELECT, UPDATE ON evaluations TO flexagent_lifecycle_executor;
GRANT SELECT ON evaluation_requests TO flexagent_lifecycle_executor;
GRANT SELECT ON audit_events TO flexagent_lifecycle_executor;
GRANT SELECT ON evaluation_lifecycle_holds TO flexagent_lifecycle_executor;
GRANT SELECT ON service_delegations TO flexagent_lifecycle_executor;
