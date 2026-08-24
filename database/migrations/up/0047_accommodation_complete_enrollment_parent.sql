-- Additive complete Enrollment parent identity for accommodation rows.
-- Do not edit 0001-0046. 0046 only referenced (organization_id, enrollment_id).

ALTER TABLE submissions_enrollments
    ADD CONSTRAINT uq_submissions_enrollments_complete_parent
        UNIQUE (
            organization_id,
            enrollment_id,
            activity_id,
            cohort_id,
            baseline_id,
            participant_actor_id);

ALTER TABLE submissions_accommodations
    DROP CONSTRAINT fk_submissions_accommodations_enrollment;

ALTER TABLE submissions_accommodations
    ADD CONSTRAINT fk_submissions_accommodations_enrollment_parent
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
            participant_actor_id);
