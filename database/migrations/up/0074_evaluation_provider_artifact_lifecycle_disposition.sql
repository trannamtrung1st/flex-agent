-- Authorized hold-aware provider-artifact lifecycle disposition without
-- caller-settable session bypass. Additive after frozen 0072/0073.

DROP TRIGGER IF EXISTS trg_evaluation_provider_artifacts_immutable ON evaluation_provider_artifacts;

CREATE OR REPLACE FUNCTION protect_evaluation_provider_artifact()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'UPDATE' THEN
        RAISE EXCEPTION 'evaluation_provider_artifacts is append-only';
    END IF;

    IF TG_OP = 'DELETE' THEN
        IF NOT EXISTS (
            SELECT 1
            FROM evaluation_lifecycle_disposition_events AS disposition
            WHERE disposition.organization_id = OLD.organization_id
              AND disposition.object_kind = 'provider_artifact'
              AND disposition.object_id = OLD.provider_artifact_id)
        THEN
            RAISE EXCEPTION 'evaluation_provider_artifacts is append-only';
        END IF;
        RETURN OLD;
    END IF;

    RAISE EXCEPTION 'evaluation_provider_artifacts is append-only';
END;
$$;

CREATE TRIGGER trg_evaluation_provider_artifacts_protect
    BEFORE UPDATE OR DELETE ON evaluation_provider_artifacts
    FOR EACH ROW
    EXECUTE FUNCTION protect_evaluation_provider_artifact();

CREATE OR REPLACE FUNCTION dispose_evaluation_provider_artifact(
    p_organization_id UUID,
    p_evaluation_id UUID,
    p_provider_artifact_id UUID,
    p_audit_event_id UUID,
    p_reason_code TEXT,
    p_actor_id UUID)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_disposition_event_id UUID := gen_random_uuid();
BEGIN
    IF p_reason_code NOT IN ('retention_expired', 'authorized_erasure') THEN
        RAISE EXCEPTION 'invalid lifecycle disposition reason';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM evaluations
        WHERE organization_id = p_organization_id
          AND evaluation_id = p_evaluation_id)
    THEN
        RAISE EXCEPTION 'evaluation not found';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM evaluation_provider_artifacts AS artifact
        INNER JOIN evaluation_requests AS request
          ON request.organization_id = artifact.organization_id
         AND request.request_id = artifact.request_id
        INNER JOIN evaluations AS evaluation
          ON evaluation.organization_id = request.organization_id
         AND evaluation.request_id = request.request_id
         AND evaluation.evaluation_id = p_evaluation_id
        WHERE artifact.organization_id = p_organization_id
          AND artifact.provider_artifact_id = p_provider_artifact_id)
    THEN
        RAISE EXCEPTION 'provider artifact not found for evaluation';
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
