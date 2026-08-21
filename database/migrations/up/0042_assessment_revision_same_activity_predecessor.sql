-- Bind previous-revision provenance to the same Activity identity and
-- require created/saved predecessor shape for new rows. Additive after 0041.

ALTER TABLE assessment_activity_revisions
    DROP CONSTRAINT fk_assessment_activity_revisions_previous;

ALTER TABLE assessment_activity_revisions
    ADD CONSTRAINT fk_assessment_activity_revisions_previous
    FOREIGN KEY (organization_id, activity_id, previous_revision_id)
    REFERENCES assessment_activity_revisions (organization_id, activity_id, revision_id);

ALTER TABLE assessment_activity_revisions
    DROP CONSTRAINT chk_assessment_activity_revisions_change_category;

ALTER TABLE assessment_activity_revisions
    ADD CONSTRAINT chk_assessment_activity_revisions_change_category
    CHECK (
        change_category IS NULL
        OR (change_category = 'created' AND previous_revision_id IS NULL)
        OR (change_category = 'saved' AND previous_revision_id IS NOT NULL)
    );
