-- Additive Submission intake, accepted versions, and immutable item metadata.
-- Do not edit 0001-0047.

CREATE TABLE submissions_submissions (
    organization_id UUID NOT NULL,
    submission_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    cohort_id UUID NOT NULL,
    baseline_id UUID NOT NULL,
    enrollment_id UUID NOT NULL,
    participant_actor_id UUID NOT NULL,
    task_source_id UUID NOT NULL,
    task_version_id UUID NOT NULL,
    task_content_digest CHAR(64) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, submission_id),
    CONSTRAINT fk_submissions_submissions_enrollment
        FOREIGN KEY (organization_id, enrollment_id)
        REFERENCES submissions_enrollments (organization_id, enrollment_id),
    CONSTRAINT uq_submissions_submissions_enrollment
        UNIQUE (organization_id, enrollment_id),
    CONSTRAINT chk_submissions_submissions_digest
        CHECK (task_content_digest = lower(task_content_digest) AND char_length(task_content_digest) = 64)
);

CREATE TABLE submissions_intakes (
    organization_id UUID NOT NULL,
    intake_id UUID NOT NULL,
    submission_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    cohort_id UUID NOT NULL,
    baseline_id UUID NOT NULL,
    enrollment_id UUID NOT NULL,
    participant_actor_id UUID NOT NULL,
    task_source_id UUID NOT NULL,
    task_version_id UUID NOT NULL,
    task_content_digest CHAR(64) NOT NULL,
    status TEXT NOT NULL,
    revision BIGINT NOT NULL,
    policy_digest CHAR(64) NOT NULL,
    frozen_requirement_source_id UUID NOT NULL,
    frozen_requirement_version_id UUID NOT NULL,
    frozen_requirement_digest CHAR(64) NOT NULL,
    organization_policy_source_id UUID NOT NULL,
    organization_policy_version_id UUID NOT NULL,
    organization_policy_digest CHAR(64) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    complete_receipt_at TIMESTAMPTZ NULL,
    PRIMARY KEY (organization_id, intake_id),
    CONSTRAINT fk_submissions_intakes_submission
        FOREIGN KEY (organization_id, submission_id)
        REFERENCES submissions_submissions (organization_id, submission_id),
    CONSTRAINT fk_submissions_intakes_enrollment_parent
        FOREIGN KEY (
            organization_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id)
        REFERENCES submissions_enrollments (
            organization_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id),
    CONSTRAINT chk_submissions_intakes_status
        CHECK (status IN (
            'receiving', 'received', 'validating', 'cancelling', 'cancelled',
            'rejected', 'failed', 'reconciling', 'accepted')),
    CONSTRAINT chk_submissions_intakes_revision
        CHECK (revision >= 1),
    CONSTRAINT chk_submissions_intakes_policy_digest
        CHECK (policy_digest = lower(policy_digest) AND char_length(policy_digest) = 64)
);

CREATE UNIQUE INDEX uq_submissions_intakes_active_enrollment
    ON submissions_intakes (organization_id, enrollment_id)
    WHERE status IN ('receiving', 'received', 'validating', 'cancelling', 'reconciling');

CREATE TABLE submissions_intake_items (
    organization_id UUID NOT NULL,
    intake_id UUID NOT NULL,
    item_id UUID NOT NULL,
    category TEXT NOT NULL,
    filename TEXT NULL,
    declared_mime_type TEXT NULL,
    byte_count BIGINT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    artifact_object_key TEXT NULL,
    artifact_version_id TEXT NULL,
    received_at TIMESTAMPTZ NULL,
    PRIMARY KEY (organization_id, intake_id, item_id),
    CONSTRAINT fk_submissions_intake_items_intake
        FOREIGN KEY (organization_id, intake_id)
        REFERENCES submissions_intakes (organization_id, intake_id),
    CONSTRAINT chk_submissions_intake_items_byte_count
        CHECK (byte_count >= 0),
    CONSTRAINT chk_submissions_intake_items_digest
        CHECK (content_digest = lower(content_digest) AND char_length(content_digest) = 64)
);

CREATE TABLE submissions_accepted_versions (
    organization_id UUID NOT NULL,
    submission_id UUID NOT NULL,
    version_id UUID NOT NULL,
    version_number INTEGER NOT NULL,
    activity_id UUID NOT NULL,
    cohort_id UUID NOT NULL,
    baseline_id UUID NOT NULL,
    enrollment_id UUID NOT NULL,
    participant_actor_id UUID NOT NULL,
    task_source_id UUID NOT NULL,
    task_version_id UUID NOT NULL,
    task_content_digest CHAR(64) NOT NULL,
    policy_digest CHAR(64) NOT NULL,
    predecessor_version_id UUID NULL,
    accepted_at TIMESTAMPTZ NOT NULL,
    accepted_by_actor_id UUID NOT NULL,
    PRIMARY KEY (organization_id, version_id),
    CONSTRAINT fk_submissions_accepted_versions_submission
        FOREIGN KEY (organization_id, submission_id)
        REFERENCES submissions_submissions (organization_id, submission_id),
    CONSTRAINT uq_submissions_accepted_versions_number
        UNIQUE (organization_id, submission_id, version_number),
    CONSTRAINT chk_submissions_accepted_versions_number
        CHECK (version_number >= 1)
);

CREATE TABLE submissions_accepted_version_items (
    organization_id UUID NOT NULL,
    version_id UUID NOT NULL,
    item_id UUID NOT NULL,
    category TEXT NOT NULL,
    filename TEXT NULL,
    byte_count BIGINT NOT NULL,
    content_digest CHAR(64) NOT NULL,
    artifact_object_key TEXT NOT NULL,
    artifact_version_id TEXT NOT NULL,
    PRIMARY KEY (organization_id, version_id, item_id),
    CONSTRAINT fk_submissions_accepted_version_items_version
        FOREIGN KEY (organization_id, version_id)
        REFERENCES submissions_accepted_versions (organization_id, version_id),
    CONSTRAINT chk_submissions_accepted_version_items_byte_count
        CHECK (byte_count >= 0)
);

CREATE OR REPLACE FUNCTION reject_submissions_accepted_version_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'submissions accepted versions are immutable';
END;
$$;

CREATE TRIGGER trg_submissions_accepted_versions_immutable
    BEFORE UPDATE OR DELETE ON submissions_accepted_versions
    FOR EACH ROW
    EXECUTE FUNCTION reject_submissions_accepted_version_mutation();

CREATE TRIGGER trg_submissions_accepted_version_items_immutable
    BEFORE UPDATE OR DELETE ON submissions_accepted_version_items
    FOR EACH ROW
    EXECUTE FUNCTION reject_submissions_accepted_version_mutation();

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
            'accommodation_revoke',
            'intake_begin',
            'intake_complete_item',
            'intake_cancel',
            'intake_finalize'));
