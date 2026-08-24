-- Additive Enrollment Task binding on the complete Submission parent tuple.
-- Do not edit 0001-0049. 0049 omitted task_source_id, task_version_id, and
-- task_content_digest from the Submission unique key and child FKs.

ALTER TABLE submissions_enrollments
    ADD CONSTRAINT uq_submissions_enrollments_complete_binding
        UNIQUE (
            organization_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id,
            task_source_id,
            task_version_id,
            task_content_digest);

ALTER TABLE submissions_submissions
    DROP CONSTRAINT fk_submissions_submissions_enrollment;

ALTER TABLE submissions_submissions
    ADD CONSTRAINT fk_submissions_submissions_enrollment_parent
        FOREIGN KEY (
            organization_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id,
            task_source_id,
            task_version_id,
            task_content_digest)
        REFERENCES submissions_enrollments (
            organization_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id,
            task_source_id,
            task_version_id,
            task_content_digest);

ALTER TABLE submissions_intakes
    DROP CONSTRAINT fk_submissions_intakes_enrollment_parent;

ALTER TABLE submissions_intakes
    ADD CONSTRAINT fk_submissions_intakes_enrollment_parent
        FOREIGN KEY (
            organization_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id,
            task_source_id,
            task_version_id,
            task_content_digest)
        REFERENCES submissions_enrollments (
            organization_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id,
            task_source_id,
            task_version_id,
            task_content_digest);

ALTER TABLE submissions_intakes
    DROP CONSTRAINT fk_submissions_intakes_submission_parent;

ALTER TABLE submissions_accepted_versions
    DROP CONSTRAINT fk_submissions_accepted_versions_submission_parent;

ALTER TABLE submissions_submissions
    DROP CONSTRAINT uq_submissions_submissions_complete_scope;

ALTER TABLE submissions_submissions
    ADD CONSTRAINT uq_submissions_submissions_complete_scope
        UNIQUE (
            organization_id,
            submission_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id,
            task_source_id,
            task_version_id,
            task_content_digest);

ALTER TABLE submissions_intakes
    ADD CONSTRAINT fk_submissions_intakes_submission_parent
        FOREIGN KEY (
            organization_id,
            submission_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id,
            task_source_id,
            task_version_id,
            task_content_digest)
        REFERENCES submissions_submissions (
            organization_id,
            submission_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id,
            task_source_id,
            task_version_id,
            task_content_digest);

ALTER TABLE submissions_accepted_versions
    ADD CONSTRAINT fk_submissions_accepted_versions_submission_parent
        FOREIGN KEY (
            organization_id,
            submission_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id,
            task_source_id,
            task_version_id,
            task_content_digest)
        REFERENCES submissions_submissions (
            organization_id,
            submission_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id,
            task_source_id,
            task_version_id,
            task_content_digest);
