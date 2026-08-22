-- Additive Enrollment assignment, lifecycle history, command idempotency,
-- exact Cohort/baseline binding target, and IdentityAccess display profiles.
-- Do not edit 0001-0042.

ALTER TABLE assessment_cohort_baseline_bindings
    ADD CONSTRAINT uq_assessment_cohort_baseline_bindings_exact
        UNIQUE (organization_id, activity_id, cohort_id, baseline_id);

CREATE TABLE identity_human_display_profiles (
    organization_id UUID NOT NULL,
    actor_id UUID NOT NULL,
    display_label TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, actor_id),
    CONSTRAINT fk_identity_human_display_profiles_organization
        FOREIGN KEY (organization_id) REFERENCES organizations (id),
    CONSTRAINT fk_identity_human_display_profiles_actor
        FOREIGN KEY (actor_id) REFERENCES actors (id),
    CONSTRAINT chk_identity_human_display_profiles_label
        CHECK (char_length(btrim(display_label)) BETWEEN 1 AND 80)
);

CREATE TABLE submissions_enrollments (
    organization_id UUID NOT NULL,
    enrollment_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    cohort_id UUID NOT NULL,
    baseline_id UUID NOT NULL,
    task_source_id UUID NOT NULL,
    task_version_id UUID NOT NULL,
    task_content_digest CHAR(64) NOT NULL,
    lifecycle_policy_id UUID NOT NULL,
    lifecycle_policy_version INTEGER NOT NULL,
    participant_actor_id UUID NOT NULL,
    status TEXT NOT NULL,
    revision BIGINT NOT NULL,
    assigned_by_actor_id UUID NOT NULL,
    assigned_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, enrollment_id),
    CONSTRAINT fk_submissions_enrollments_organization
        FOREIGN KEY (organization_id) REFERENCES organizations (id),
    CONSTRAINT fk_submissions_enrollments_binding
        FOREIGN KEY (organization_id, activity_id, cohort_id, baseline_id)
        REFERENCES assessment_cohort_baseline_bindings (organization_id, activity_id, cohort_id, baseline_id),
    CONSTRAINT fk_submissions_enrollments_participant
        FOREIGN KEY (participant_actor_id) REFERENCES actors (id),
    CONSTRAINT fk_submissions_enrollments_assigned_by
        FOREIGN KEY (assigned_by_actor_id) REFERENCES actors (id),
    CONSTRAINT chk_submissions_enrollments_status
        CHECK (status IN ('active', 'suspended', 'closed', 'revoked')),
    CONSTRAINT chk_submissions_enrollments_revision
        CHECK (revision >= 1),
    CONSTRAINT chk_submissions_enrollments_lifecycle_version
        CHECK (lifecycle_policy_version >= 1),
    CONSTRAINT chk_submissions_enrollments_digest
        CHECK (task_content_digest = lower(task_content_digest) AND char_length(task_content_digest) = 64)
);

CREATE UNIQUE INDEX uq_submissions_enrollments_live_participant
    ON submissions_enrollments (organization_id, activity_id, participant_actor_id)
    WHERE status IN ('active', 'suspended');

CREATE INDEX ix_submissions_enrollments_cohort
    ON submissions_enrollments (organization_id, activity_id, cohort_id, updated_at, enrollment_id);

CREATE INDEX ix_submissions_enrollments_participant_current
    ON submissions_enrollments (organization_id, participant_actor_id, updated_at, enrollment_id)
    WHERE status IN ('active', 'suspended');

CREATE OR REPLACE FUNCTION reject_submissions_enrollment_binding_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.activity_id IS DISTINCT FROM OLD.activity_id
        OR NEW.cohort_id IS DISTINCT FROM OLD.cohort_id
        OR NEW.baseline_id IS DISTINCT FROM OLD.baseline_id
        OR NEW.task_source_id IS DISTINCT FROM OLD.task_source_id
        OR NEW.task_version_id IS DISTINCT FROM OLD.task_version_id
        OR NEW.task_content_digest IS DISTINCT FROM OLD.task_content_digest
        OR NEW.participant_actor_id IS DISTINCT FROM OLD.participant_actor_id
        OR NEW.organization_id IS DISTINCT FROM OLD.organization_id
        OR NEW.assigned_by_actor_id IS DISTINCT FROM OLD.assigned_by_actor_id
        OR NEW.assigned_at IS DISTINCT FROM OLD.assigned_at
        OR NEW.lifecycle_policy_id IS DISTINCT FROM OLD.lifecycle_policy_id
        OR NEW.lifecycle_policy_version IS DISTINCT FROM OLD.lifecycle_policy_version THEN
        RAISE EXCEPTION 'submissions_enrollments binding fields are immutable';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_submissions_enrollments_immutable_binding
    BEFORE UPDATE ON submissions_enrollments
    FOR EACH ROW
    EXECUTE FUNCTION reject_submissions_enrollment_binding_mutation();

CREATE TABLE submissions_enrollment_events (
    organization_id UUID NOT NULL,
    enrollment_id UUID NOT NULL,
    event_id UUID NOT NULL,
    sequence BIGINT NOT NULL,
    prior_status TEXT NOT NULL,
    new_status TEXT NOT NULL,
    reason_code TEXT NOT NULL,
    actor_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    correlation_id UUID NOT NULL,
    authorization_reference_id UUID NULL,
    enrollment_revision BIGINT NOT NULL,
    PRIMARY KEY (organization_id, event_id),
    CONSTRAINT fk_submissions_enrollment_events_enrollment
        FOREIGN KEY (organization_id, enrollment_id)
        REFERENCES submissions_enrollments (organization_id, enrollment_id),
    CONSTRAINT uq_submissions_enrollment_events_sequence
        UNIQUE (organization_id, enrollment_id, sequence),
    CONSTRAINT chk_submissions_enrollment_events_reason
        CHECK (reason_code IN (
            'temporary_restriction',
            'restriction_removed',
            'activity_or_enrollment_end',
            'access_revoked')),
    CONSTRAINT chk_submissions_enrollment_events_revision
        CHECK (enrollment_revision >= 1)
);

CREATE OR REPLACE FUNCTION reject_submissions_enrollment_event_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'submissions_enrollment_events is append-only';
END;
$$;

CREATE TRIGGER trg_submissions_enrollment_events_no_update
    BEFORE UPDATE ON submissions_enrollment_events
    FOR EACH ROW
    EXECUTE FUNCTION reject_submissions_enrollment_event_mutation();

CREATE TRIGGER trg_submissions_enrollment_events_no_delete
    BEFORE DELETE ON submissions_enrollment_events
    FOR EACH ROW
    EXECUTE FUNCTION reject_submissions_enrollment_event_mutation();

CREATE TABLE submissions_enrollment_operations (
    organization_id UUID NOT NULL,
    actor_id UUID NOT NULL,
    operation_kind TEXT NOT NULL,
    resource_id UUID NOT NULL,
    idempotency_key TEXT NOT NULL,
    command_digest CHAR(64) NOT NULL,
    outcome_code TEXT NOT NULL,
    enrollment_id UUID NULL,
    created_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, actor_id, operation_kind, resource_id, idempotency_key),
    CONSTRAINT fk_submissions_enrollment_operations_organization
        FOREIGN KEY (organization_id) REFERENCES organizations (id),
    CONSTRAINT chk_submissions_enrollment_operations_kind
        CHECK (operation_kind IN ('assign', 'suspend', 'restore', 'close', 'revoke')),
    CONSTRAINT chk_submissions_enrollment_operations_key
        CHECK (char_length(idempotency_key) BETWEEN 1 AND 128),
    CONSTRAINT chk_submissions_enrollment_operations_digest
        CHECK (command_digest = lower(command_digest) AND char_length(command_digest) = 64),
    CONSTRAINT chk_submissions_enrollment_operations_expiry
        CHECK (expires_at > created_at)
);
