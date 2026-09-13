-- Minimal Review case foundation for Evaluation completion handoff.
-- Additive after 0079. Evaluation completion invokes Review writes through
-- infrastructure adapter; Evaluation application/domain do not own Review tables.

CREATE TABLE review_cases (
    organization_id UUID NOT NULL,
    review_case_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    case_state TEXT NOT NULL,
    candidate_state TEXT NOT NULL,
    current_candidate_evaluation_id UUID NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, review_case_id),
    CONSTRAINT uq_review_cases_session_scope
        UNIQUE (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_review_cases_case_state
        CHECK (case_state IN ('evaluation_available', 'candidate_stale')),
    CONSTRAINT chk_review_cases_candidate_state
        CHECK (candidate_state IN ('none', 'selected', 'replacement_available', 'stale'))
);

CREATE TABLE review_case_events (
    organization_id UUID NOT NULL,
    event_id UUID NOT NULL,
    review_case_id UUID NOT NULL,
    event_kind TEXT NOT NULL,
    evaluation_id UUID NULL,
    reason TEXT NULL,
    actor_type TEXT NOT NULL,
    actor_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, event_id),
    CONSTRAINT fk_review_case_events_case
        FOREIGN KEY (organization_id, review_case_id)
        REFERENCES review_cases (organization_id, review_case_id),
    CONSTRAINT chk_review_case_events_kind
        CHECK (event_kind IN (
            'evaluation_available',
            'initial_candidate_selected',
            'replacement_available')),
    CONSTRAINT chk_review_case_events_actor
        CHECK (actor_type IN ('human', 'service', 'system')),
    CONSTRAINT chk_review_case_events_reason
        CHECK (reason IS NULL OR char_length(reason) BETWEEN 8 AND 128)
);

CREATE INDEX ix_review_case_events_case_occurred
    ON review_case_events (organization_id, review_case_id, occurred_at);

CREATE OR REPLACE FUNCTION prevent_review_case_event_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'append-only: review_case_events cannot be updated or deleted';
END;
$$;

CREATE TRIGGER trg_review_case_events_append_only
    BEFORE UPDATE OR DELETE ON review_case_events
    FOR EACH ROW
    EXECUTE FUNCTION prevent_review_case_event_mutation();
