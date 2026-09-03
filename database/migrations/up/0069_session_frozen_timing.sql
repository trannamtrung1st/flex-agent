CREATE TABLE session_frozen_timing (
    organization_id UUID NOT NULL,
    session_id UUID NOT NULL,
    document JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id),
    CONSTRAINT fk_session_frozen_timing_runtime
        FOREIGN KEY (organization_id, session_id)
        REFERENCES session_runtimes (organization_id, session_id),
    CONSTRAINT chk_session_frozen_timing_object
        CHECK (jsonb_typeof(document) = 'object')
);

CREATE TRIGGER trg_session_frozen_timing_no_update
    BEFORE UPDATE ON session_frozen_timing
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_frozen_timing_no_delete
    BEFORE DELETE ON session_frozen_timing
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();
