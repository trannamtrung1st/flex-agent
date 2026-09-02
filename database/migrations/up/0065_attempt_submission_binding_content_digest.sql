-- Persist the version-level Submission provenance digest on Attempt bindings.
-- Temporarily drop the append-only UPDATE trigger for this controlled backfill
-- only; restore it before leaving the migration. Do not edit 0001-0064.

ALTER TABLE submissions_attempt_submission_bindings
    ADD COLUMN content_digest CHAR(64);

DROP TRIGGER trg_submissions_attempt_submission_bindings_no_update
    ON submissions_attempt_submission_bindings;

UPDATE submissions_attempt_submission_bindings AS binding
SET content_digest = encode(
    sha256(
        convert_to(
            binding.version_id::text
                || E'\n'
                || binding.version_number::text
                || E'\n'
                || coalesce(
                    (
                        SELECT string_agg(items.item_id::text || ':' || items.content_digest, E'\n' ORDER BY items.item_id)
                        FROM submissions_accepted_version_items AS items
                        WHERE items.organization_id = binding.organization_id
                          AND items.version_id = binding.version_id
                    ),
                    'policy:' || (
                        SELECT versions.policy_digest
                        FROM submissions_accepted_versions AS versions
                        WHERE versions.organization_id = binding.organization_id
                          AND versions.version_id = binding.version_id
                    )),
            'UTF8')),
    'hex')
WHERE binding.content_digest IS NULL;

CREATE TRIGGER trg_submissions_attempt_submission_bindings_no_update
    BEFORE UPDATE ON submissions_attempt_submission_bindings
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

ALTER TABLE submissions_attempt_submission_bindings
    ALTER COLUMN content_digest SET NOT NULL;

ALTER TABLE submissions_attempt_submission_bindings
    ADD CONSTRAINT chk_submissions_attempt_submission_bindings_digest
        CHECK (content_digest = lower(content_digest) AND char_length(content_digest) = 64);
