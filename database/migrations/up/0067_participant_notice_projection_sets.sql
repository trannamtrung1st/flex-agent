-- Verifiable empty-or-populated notice projection receipts, coupled to source
-- version registration. Existing versions without a receipt fail closed.
-- Do not edit 0001-0066.

CREATE TABLE configuration_participant_notice_projection_sets (
    organization_id UUID NOT NULL,
    source_id UUID NOT NULL,
    source_version_id UUID NOT NULL,
    source_content_digest CHAR(64) NOT NULL,
    notice_count INTEGER NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, source_version_id),
    CONSTRAINT fk_configuration_participant_notice_projection_sets_version
        FOREIGN KEY (organization_id, source_version_id)
        REFERENCES configuration_source_versions (organization_id, id),
    CONSTRAINT chk_configuration_participant_notice_projection_sets_digest
        CHECK (
            source_content_digest = lower(source_content_digest)
            AND char_length(source_content_digest) = 64
        ),
    CONSTRAINT chk_configuration_participant_notice_projection_sets_count
        CHECK (notice_count >= 0)
);

CREATE TRIGGER trg_configuration_participant_notice_projection_sets_no_update
    BEFORE UPDATE ON configuration_participant_notice_projection_sets
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_configuration_participant_notice_projection_sets_no_delete
    BEFORE DELETE ON configuration_participant_notice_projection_sets
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();
