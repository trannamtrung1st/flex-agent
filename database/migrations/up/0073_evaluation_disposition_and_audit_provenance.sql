-- Bind Evaluation disposition and lifecycle-audit provenance that 0072 left
-- independently keyed. Additive after frozen 0072. Do not edit 0072.

ALTER TABLE evaluation_annotations
    ADD CONSTRAINT uq_evaluation_annotations_owned
    UNIQUE (organization_id, evaluation_id, annotation_id);

ALTER TABLE evaluation_dispositions
    DROP CONSTRAINT fk_evaluation_dispositions_annotation;

ALTER TABLE evaluation_dispositions
    ADD CONSTRAINT fk_evaluation_dispositions_annotation
    FOREIGN KEY (organization_id, evaluation_id, last_annotation_id)
    REFERENCES evaluation_annotations (organization_id, evaluation_id, annotation_id);

ALTER TABLE audit_events
    ADD CONSTRAINT uq_audit_events_organization_event
    UNIQUE (organization_id, event_id);

ALTER TABLE evaluation_lifecycle_disposition_events
    DROP CONSTRAINT fk_evaluation_lifecycle_dispositions_audit;

ALTER TABLE evaluation_lifecycle_disposition_events
    ADD CONSTRAINT fk_evaluation_lifecycle_dispositions_audit
    FOREIGN KEY (organization_id, audit_event_id)
    REFERENCES audit_events (organization_id, event_id);

CREATE OR REPLACE FUNCTION enforce_evaluation_lifecycle_disposition_audit()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM audit_events
        WHERE event_id = NEW.audit_event_id
          AND organization_id = NEW.organization_id
          AND actor_id = NEW.actor_id
          AND action = 'evaluation.lifecycle.dispose'
          AND resource_type = 'evaluation'
          AND resource_id = NEW.evaluation_id
          AND outcome = 'succeeded') THEN
        RAISE EXCEPTION 'evaluation_lifecycle_disposition_events require a matching succeeded evaluation.lifecycle.dispose audit event';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_evaluation_lifecycle_disposition_audit
    BEFORE INSERT ON evaluation_lifecycle_disposition_events
    FOR EACH ROW
    EXECUTE FUNCTION enforce_evaluation_lifecycle_disposition_audit();
