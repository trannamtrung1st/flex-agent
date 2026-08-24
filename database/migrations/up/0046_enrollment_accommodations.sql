-- Additive Participant accommodation records, append-only facts, and
-- enrollment-operation kinds for grant/decide/revoke. Do not edit 0001-0045.

ALTER TABLE submissions_enrollment_operations
    DROP CONSTRAINT chk_submissions_enrollment_operations_kind;

ALTER TABLE submissions_enrollment_operations
    ADD CONSTRAINT chk_submissions_enrollment_operations_kind
        CHECK (operation_kind IN (
            'assign',
            'suspend',
            'restore',
            'close',
            'revoke',
            'accommodation_grant',
            'accommodation_decide',
            'accommodation_revoke'));

CREATE TABLE submissions_accommodations (
    organization_id UUID NOT NULL,
    accommodation_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    cohort_id UUID NOT NULL,
    baseline_id UUID NOT NULL,
    enrollment_id UUID NOT NULL,
    participant_actor_id UUID NOT NULL,
    dimension TEXT NOT NULL,
    normalized_value TEXT NOT NULL,
    frozen_policy_id UUID NOT NULL,
    frozen_policy_version_id UUID NOT NULL,
    frozen_policy_digest CHAR(64) NOT NULL,
    decision_policy_id UUID NOT NULL,
    decision_policy_version_id UUID NOT NULL,
    decision_policy_digest CHAR(64) NOT NULL,
    reason_category TEXT NOT NULL,
    status TEXT NOT NULL,
    revision BIGINT NOT NULL,
    requester_actor_id UUID NOT NULL,
    approver_actor_id UUID NULL,
    created_at TIMESTAMPTZ NOT NULL,
    decided_at TIMESTAMPTZ NULL,
    expires_at TIMESTAMPTZ NULL,
    revoked_at TIMESTAMPTZ NULL,
    superseded_by_accommodation_id UUID NULL,
    fairness_exception BOOLEAN NOT NULL,
    lifecycle_policy_id UUID NOT NULL,
    lifecycle_policy_version INTEGER NOT NULL,
    PRIMARY KEY (organization_id, accommodation_id),
    CONSTRAINT fk_submissions_accommodations_organization
        FOREIGN KEY (organization_id) REFERENCES organizations (id),
    CONSTRAINT fk_submissions_accommodations_enrollment
        FOREIGN KEY (organization_id, enrollment_id)
        REFERENCES submissions_enrollments (organization_id, enrollment_id),
    CONSTRAINT chk_submissions_accommodations_dimension
        CHECK (dimension IN (
            'submission_deadline_utc',
            'attempt_start_not_before_utc',
            'attempt_start_before_utc',
            'per_attempt_duration_seconds')),
    CONSTRAINT chk_submissions_accommodations_status
        CHECK (status IN ('pending_approval', 'granted', 'rejected', 'revoked', 'superseded')),
    CONSTRAINT chk_submissions_accommodations_revision
        CHECK (revision >= 1),
    CONSTRAINT chk_submissions_accommodations_lifecycle_version
        CHECK (lifecycle_policy_version >= 1),
    CONSTRAINT chk_submissions_accommodations_frozen_digest
        CHECK (frozen_policy_digest = lower(frozen_policy_digest) AND char_length(frozen_policy_digest) = 64),
    CONSTRAINT chk_submissions_accommodations_decision_digest
        CHECK (decision_policy_digest = lower(decision_policy_digest) AND char_length(decision_policy_digest) = 64),
    CONSTRAINT chk_submissions_accommodations_value
        CHECK (char_length(btrim(normalized_value)) BETWEEN 1 AND 64),
    CONSTRAINT chk_submissions_accommodations_reason
        CHECK (char_length(btrim(reason_category)) BETWEEN 1 AND 128)
);

CREATE UNIQUE INDEX uq_submissions_accommodations_current_dimension
    ON submissions_accommodations (organization_id, enrollment_id, dimension)
    WHERE status = 'granted';

CREATE INDEX ix_submissions_accommodations_enrollment
    ON submissions_accommodations (organization_id, enrollment_id, created_at, accommodation_id);

CREATE TABLE submissions_accommodation_facts (
    organization_id UUID NOT NULL,
    accommodation_id UUID NOT NULL,
    fact_id UUID NOT NULL,
    sequence BIGINT NOT NULL,
    prior_status TEXT NULL,
    new_status TEXT NOT NULL,
    reason_category TEXT NOT NULL,
    actor_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, accommodation_id, fact_id),
    CONSTRAINT fk_submissions_accommodation_facts_parent
        FOREIGN KEY (organization_id, accommodation_id)
        REFERENCES submissions_accommodations (organization_id, accommodation_id),
    CONSTRAINT uq_submissions_accommodation_facts_sequence
        UNIQUE (organization_id, accommodation_id, sequence),
    CONSTRAINT chk_submissions_accommodation_facts_sequence
        CHECK (sequence >= 1)
);

CREATE OR REPLACE FUNCTION reject_submissions_accommodation_identity_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.activity_id IS DISTINCT FROM OLD.activity_id
        OR NEW.cohort_id IS DISTINCT FROM OLD.cohort_id
        OR NEW.baseline_id IS DISTINCT FROM OLD.baseline_id
        OR NEW.enrollment_id IS DISTINCT FROM OLD.enrollment_id
        OR NEW.participant_actor_id IS DISTINCT FROM OLD.participant_actor_id
        OR NEW.dimension IS DISTINCT FROM OLD.dimension
        OR NEW.requester_actor_id IS DISTINCT FROM OLD.requester_actor_id
        OR NEW.frozen_policy_id IS DISTINCT FROM OLD.frozen_policy_id
        OR NEW.frozen_policy_version_id IS DISTINCT FROM OLD.frozen_policy_version_id
        OR NEW.frozen_policy_digest IS DISTINCT FROM OLD.frozen_policy_digest
        OR NEW.created_at IS DISTINCT FROM OLD.created_at
        OR NEW.fairness_exception IS DISTINCT FROM OLD.fairness_exception
        OR NEW.lifecycle_policy_id IS DISTINCT FROM OLD.lifecycle_policy_id
        OR NEW.lifecycle_policy_version IS DISTINCT FROM OLD.lifecycle_policy_version
    THEN
        RAISE EXCEPTION 'submissions accommodation identity is immutable';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_submissions_accommodations_identity
    BEFORE UPDATE ON submissions_accommodations
    FOR EACH ROW
    EXECUTE FUNCTION reject_submissions_accommodation_identity_mutation();

CREATE OR REPLACE FUNCTION reject_submissions_accommodation_fact_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'submissions accommodation facts are append-only';
END;
$$;

CREATE TRIGGER trg_submissions_accommodation_facts_no_update
    BEFORE UPDATE ON submissions_accommodation_facts
    FOR EACH ROW
    EXECUTE FUNCTION reject_submissions_accommodation_fact_mutation();

CREATE TRIGGER trg_submissions_accommodation_facts_no_delete
    BEFORE DELETE ON submissions_accommodation_facts
    FOR EACH ROW
    EXECUTE FUNCTION reject_submissions_accommodation_fact_mutation();
