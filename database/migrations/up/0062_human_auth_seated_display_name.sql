-- Persist the seated operator display name captured from the validated ID
-- token at login. Used for chrome identity; not authorization.

ALTER TABLE application_sessions
    ADD COLUMN seated_display_name TEXT NULL;

ALTER TABLE application_sessions
    ADD CONSTRAINT chk_application_sessions_seated_display_name
        CHECK (seated_display_name IS NULL OR char_length(seated_display_name) BETWEEN 1 AND 120);
