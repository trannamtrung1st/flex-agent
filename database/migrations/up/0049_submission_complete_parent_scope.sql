-- Additive complete Submission parent scope and accepted-version lineage constraints.
-- Do not edit 0001-0048.

ALTER TABLE submissions_submissions
    ADD CONSTRAINT uq_submissions_submissions_complete_scope
        UNIQUE (
            organization_id,
            submission_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id);

ALTER TABLE submissions_intakes
    DROP CONSTRAINT fk_submissions_intakes_submission;

ALTER TABLE submissions_intakes
    ADD CONSTRAINT fk_submissions_intakes_submission_parent
        FOREIGN KEY (
            organization_id,
            submission_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id)
        REFERENCES submissions_submissions (
            organization_id,
            submission_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id);

ALTER TABLE submissions_accepted_versions
    ADD CONSTRAINT uq_submissions_accepted_versions_submission_version
        UNIQUE (organization_id, submission_id, version_id);

ALTER TABLE submissions_accepted_versions
    ADD CONSTRAINT fk_submissions_accepted_versions_submission_parent
        FOREIGN KEY (
            organization_id,
            submission_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id)
        REFERENCES submissions_submissions (
            organization_id,
            submission_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id);

ALTER TABLE submissions_accepted_versions
    ADD CONSTRAINT fk_submissions_accepted_versions_predecessor
        FOREIGN KEY (organization_id, submission_id, predecessor_version_id)
        REFERENCES submissions_accepted_versions (organization_id, submission_id, version_id);

ALTER TABLE submissions_accepted_versions
    ADD CONSTRAINT chk_submissions_accepted_versions_predecessor
        CHECK (
            (version_number = 1 AND predecessor_version_id IS NULL)
            OR (version_number > 1 AND predecessor_version_id IS NOT NULL));
