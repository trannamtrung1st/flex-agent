-- Record actor, prior revision, change category, and save time on
-- immutable Activity revisions so Create/Save mutations are auditable.
-- Additive after 0040. Historical rows remain nullable.

ALTER TABLE assessment_activity_revisions
    ADD COLUMN previous_revision_id UUID,
    ADD COLUMN actor_id UUID,
    ADD COLUMN actor_type TEXT,
    ADD COLUMN correlation_id UUID,
    ADD COLUMN change_category TEXT,
    ADD COLUMN saved_at TIMESTAMPTZ;

UPDATE assessment_activity_revisions
SET saved_at = created_at
WHERE saved_at IS NULL;

ALTER TABLE assessment_activity_revisions
    ADD CONSTRAINT chk_assessment_activity_revisions_change_category
    CHECK (change_category IS NULL OR change_category IN ('created', 'saved'));

ALTER TABLE assessment_activity_revisions
    ADD CONSTRAINT fk_assessment_activity_revisions_previous
    FOREIGN KEY (organization_id, previous_revision_id)
    REFERENCES assessment_activity_revisions (organization_id, revision_id);
