-- Separate requested activation-revision claims from trusted draft head
-- identity. Requested values are request evidence and must not FK-bind a
-- parent that a denial cannot safely establish. Additive after 0036.

ALTER TABLE assessment_activation_attempts
    ADD COLUMN requested_revision_id UUID,
    ADD COLUMN requested_revision_number BIGINT,
    ADD COLUMN authoritative_revision_id UUID,
    ADD COLUMN authoritative_revision_number BIGINT;

UPDATE assessment_activation_attempts
SET requested_revision_id = expected_revision_id,
    requested_revision_number = expected_revision_number,
    authoritative_revision_id = expected_revision_id,
    authoritative_revision_number = expected_revision_number;

ALTER TABLE assessment_activation_attempts
    ALTER COLUMN requested_revision_id SET NOT NULL;

ALTER TABLE assessment_activation_attempts
    ALTER COLUMN requested_revision_number SET NOT NULL;

ALTER TABLE assessment_activation_attempts
    DROP CONSTRAINT fk_assessment_activation_attempts_revision;

ALTER TABLE assessment_activation_attempts
    DROP COLUMN expected_revision_id;

ALTER TABLE assessment_activation_attempts
    DROP COLUMN expected_revision_number;

ALTER TABLE assessment_activation_attempts
    ADD CONSTRAINT fk_assessment_activation_attempts_authoritative_revision
        FOREIGN KEY (organization_id, activity_id, authoritative_revision_id)
        REFERENCES assessment_activity_revisions (organization_id, activity_id, revision_id);
