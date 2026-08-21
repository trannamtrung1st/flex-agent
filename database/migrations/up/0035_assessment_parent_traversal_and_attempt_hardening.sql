-- Strengthen Assessment parent traversal so revision, baseline, and attempt
-- FKs prove the referenced row belongs to the same activity_id.
-- Additive after 0034. Do not edit 0001-0034.

ALTER TABLE assessment_activity_revisions
    ADD CONSTRAINT uq_assessment_activity_revisions_identity
        UNIQUE (organization_id, activity_id, revision_id);

ALTER TABLE assessment_cohorts
    ADD CONSTRAINT uq_assessment_cohorts_activity
        UNIQUE (organization_id, activity_id, cohort_id);

ALTER TABLE assessment_activation_baselines
    ADD CONSTRAINT uq_assessment_activation_baselines_activity
        UNIQUE (organization_id, activity_id, baseline_id);

ALTER TABLE assessment_activities
    DROP CONSTRAINT fk_assessment_activities_current_revision;

ALTER TABLE assessment_activities
    ADD CONSTRAINT fk_assessment_activities_current_revision
        FOREIGN KEY (organization_id, activity_id, current_revision_id)
        REFERENCES assessment_activity_revisions (organization_id, activity_id, revision_id)
        DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE assessment_cohorts
    DROP CONSTRAINT fk_assessment_cohorts_revision;

ALTER TABLE assessment_cohorts
    ADD CONSTRAINT fk_assessment_cohorts_revision
        FOREIGN KEY (organization_id, activity_id, bound_revision_id)
        REFERENCES assessment_activity_revisions (organization_id, activity_id, revision_id);

ALTER TABLE assessment_cohort_baseline_bindings
    ADD COLUMN activity_id UUID;

UPDATE assessment_cohort_baseline_bindings AS binding
SET activity_id = cohort.activity_id
FROM assessment_cohorts AS cohort
WHERE cohort.organization_id = binding.organization_id
  AND cohort.cohort_id = binding.cohort_id;

ALTER TABLE assessment_cohort_baseline_bindings
    ALTER COLUMN activity_id SET NOT NULL;

ALTER TABLE assessment_cohort_baseline_bindings
    DROP CONSTRAINT fk_assessment_cohort_baseline_bindings_cohort;

ALTER TABLE assessment_cohort_baseline_bindings
    DROP CONSTRAINT fk_assessment_cohort_baseline_bindings_baseline;

ALTER TABLE assessment_cohort_baseline_bindings
    DROP CONSTRAINT assessment_cohort_baseline_bindings_pkey;

ALTER TABLE assessment_cohort_baseline_bindings
    ADD PRIMARY KEY (organization_id, activity_id, cohort_id);

ALTER TABLE assessment_cohort_baseline_bindings
    ADD CONSTRAINT fk_assessment_cohort_baseline_bindings_cohort
        FOREIGN KEY (organization_id, activity_id, cohort_id)
        REFERENCES assessment_cohorts (organization_id, activity_id, cohort_id);

ALTER TABLE assessment_cohort_baseline_bindings
    ADD CONSTRAINT fk_assessment_cohort_baseline_bindings_baseline
        FOREIGN KEY (organization_id, activity_id, baseline_id)
        REFERENCES assessment_activation_baselines (organization_id, activity_id, baseline_id);

ALTER TABLE assessment_activation_attempts
    DROP CONSTRAINT fk_assessment_activation_attempts_cohort;

ALTER TABLE assessment_activation_attempts
    ADD CONSTRAINT fk_assessment_activation_attempts_cohort
        FOREIGN KEY (organization_id, activity_id, cohort_id)
        REFERENCES assessment_cohorts (organization_id, activity_id, cohort_id);

ALTER TABLE assessment_activation_attempts
    ADD CONSTRAINT fk_assessment_activation_attempts_revision
        FOREIGN KEY (organization_id, activity_id, expected_revision_id)
        REFERENCES assessment_activity_revisions (organization_id, activity_id, revision_id);

ALTER TABLE assessment_activation_attempts
    ADD CONSTRAINT fk_assessment_activation_attempts_baseline
        FOREIGN KEY (organization_id, activity_id, baseline_id)
        REFERENCES assessment_activation_baselines (organization_id, activity_id, baseline_id);
