CREATE TABLE session_warning_occurrences (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    warning_threshold_id TEXT NOT NULL,
    warning_code TEXT NOT NULL,
    remaining_seconds_threshold INTEGER NOT NULL,
    due_at TIMESTAMPTZ NOT NULL,
    committed_at TIMESTAMPTZ NOT NULL,
    session_sequence BIGINT NOT NULL,
    remaining_seconds_at_commit INTEGER NOT NULL,
    delivery_status TEXT NOT NULL,
    PRIMARY KEY (organization_id, session_id, warning_threshold_id),
    CONSTRAINT fk_session_warning_occurrences_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT uq_session_warning_occurrences_sequence
        UNIQUE (organization_id, session_id, session_sequence),
    CONSTRAINT chk_session_warning_occurrences_threshold_id
        CHECK (length(btrim(warning_threshold_id)) BETWEEN 1 AND 128),
    CONSTRAINT chk_session_warning_occurrences_code
        CHECK (warning_code IN ('approaching', 'imminent')),
    CONSTRAINT chk_session_warning_occurrences_threshold
        CHECK (remaining_seconds_threshold > 0),
    CONSTRAINT chk_session_warning_occurrences_sequence
        CHECK (session_sequence > 0),
    CONSTRAINT chk_session_warning_occurrences_remaining
        CHECK (remaining_seconds_at_commit >= 0),
    CONSTRAINT chk_session_warning_occurrences_delivery
        CHECK (delivery_status IN ('issued', 'late'))
);

CREATE TRIGGER trg_session_warning_occurrences_no_update
    BEFORE UPDATE ON session_warning_occurrences
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_warning_occurrences_no_delete
    BEFORE DELETE ON session_warning_occurrences
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();
