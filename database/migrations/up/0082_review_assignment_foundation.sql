-- Active Review case assignment foundation for assigned-inspection reads.
-- Additive after 0081. Development/Testing seeds may insert rows; production
-- assignment policy is out of scope for this migration.

CREATE TABLE review_case_assignments (
    organization_id UUID NOT NULL,
    assignment_id UUID NOT NULL,
    review_case_id UUID NOT NULL,
    reviewer_actor_id UUID NOT NULL,
    assignment_state TEXT NOT NULL,
    content_capability TEXT NOT NULL,
    assigned_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ NULL,
    PRIMARY KEY (organization_id, assignment_id),
    CONSTRAINT fk_review_case_assignments_case
        FOREIGN KEY (organization_id, review_case_id)
        REFERENCES review_cases (organization_id, review_case_id),
    CONSTRAINT chk_review_case_assignments_state
        CHECK (assignment_state IN ('assigned', 'revoked')),
    CONSTRAINT chk_review_case_assignments_capability
        CHECK (content_capability IN ('review.content.read')),
    CONSTRAINT chk_review_case_assignments_revocation
        CHECK (
            (assignment_state = 'assigned' AND revoked_at IS NULL)
            OR (assignment_state = 'revoked' AND revoked_at IS NOT NULL))
);

CREATE UNIQUE INDEX uq_review_case_assignments_active_reviewer
    ON review_case_assignments (organization_id, review_case_id, reviewer_actor_id)
    WHERE assignment_state = 'assigned' AND revoked_at IS NULL;

CREATE INDEX ix_review_case_assignments_reviewer_active
    ON review_case_assignments (organization_id, reviewer_actor_id, assigned_at DESC)
    WHERE assignment_state = 'assigned' AND revoked_at IS NULL;

CREATE TABLE review_case_assignment_events (
    organization_id UUID NOT NULL,
    event_id UUID NOT NULL,
    assignment_id UUID NOT NULL,
    review_case_id UUID NOT NULL,
    reviewer_actor_id UUID NOT NULL,
    event_kind TEXT NOT NULL,
    content_capability TEXT NOT NULL,
    reason TEXT NULL,
    actor_type TEXT NOT NULL,
    actor_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, event_id),
    CONSTRAINT fk_review_case_assignment_events_assignment
        FOREIGN KEY (organization_id, assignment_id)
        REFERENCES review_case_assignments (organization_id, assignment_id),
    CONSTRAINT fk_review_case_assignment_events_case
        FOREIGN KEY (organization_id, review_case_id)
        REFERENCES review_cases (organization_id, review_case_id),
    CONSTRAINT chk_review_case_assignment_events_kind
        CHECK (event_kind IN ('assigned', 'revoked')),
    CONSTRAINT chk_review_case_assignment_events_capability
        CHECK (content_capability IN ('review.content.read')),
    CONSTRAINT chk_review_case_assignment_events_actor
        CHECK (actor_type IN ('human', 'service', 'system')),
    CONSTRAINT chk_review_case_assignment_events_reason
        CHECK (reason IS NULL OR char_length(reason) BETWEEN 8 AND 128)
);

CREATE INDEX ix_review_case_assignment_events_case_occurred
    ON review_case_assignment_events (organization_id, review_case_id, occurred_at);

CREATE OR REPLACE FUNCTION prevent_review_case_assignment_event_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'append-only: review_case_assignment_events cannot be updated or deleted';
END;
$$;

CREATE TRIGGER trg_review_case_assignment_events_append_only
    BEFORE UPDATE OR DELETE ON review_case_assignment_events
    FOR EACH ROW
    EXECUTE FUNCTION prevent_review_case_assignment_event_mutation();
