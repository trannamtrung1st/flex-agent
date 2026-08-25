-- Additive replica-safe generation for the accepted-cleanup scan cursor.
-- Do not edit 0001-0055.

ALTER TABLE submissions_accepted_cleanup_scan
    ADD COLUMN generation BIGINT NOT NULL DEFAULT 0;

ALTER TABLE submissions_accepted_cleanup_scan
    ADD CONSTRAINT chk_submissions_accepted_cleanup_scan_generation
        CHECK (generation >= 0);
