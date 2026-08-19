-- Distinguish Invocation attempt ordinal from each external provider request.
-- Do not rewrite frozen 0001-0026.
-- Temporarily disable the table's append-only UPDATE trigger so this
-- migration-owned backfill can populate new columns on existing 0026 rows.

ALTER TABLE session_invocation_provider_attempts
    ADD COLUMN provider_request_id TEXT,
    ADD COLUMN phase TEXT,
    ADD COLUMN provider_request_ordinal INT;

ALTER TABLE session_invocation_provider_attempts
    DISABLE TRIGGER trg_session_invocation_provider_attempts_no_update;

UPDATE session_invocation_provider_attempts
SET
    provider_request_id = 'prat.migrated.' || agent_invocation_id || '.' || attempt_ordinal::text,
    phase = 'control',
    provider_request_ordinal = attempt_ordinal
WHERE provider_request_id IS NULL;

ALTER TABLE session_invocation_provider_attempts
    ENABLE TRIGGER trg_session_invocation_provider_attempts_no_update;

ALTER TABLE session_invocation_provider_attempts
    ALTER COLUMN provider_request_id SET NOT NULL,
    ALTER COLUMN phase SET NOT NULL,
    ALTER COLUMN provider_request_ordinal SET NOT NULL;

ALTER TABLE session_invocation_provider_attempts
    DROP CONSTRAINT session_invocation_provider_attempts_pkey;

ALTER TABLE session_invocation_provider_attempts
    ADD PRIMARY KEY (organization_id, session_id, provider_request_id);

ALTER TABLE session_invocation_provider_attempts
    ADD CONSTRAINT chk_session_invocation_provider_attempts_phase
        CHECK (phase IN ('control', 'content'));

ALTER TABLE session_invocation_provider_attempts
    ADD CONSTRAINT chk_session_invocation_provider_attempts_request_id
        CHECK (btrim(provider_request_id) <> '');

ALTER TABLE session_invocation_provider_attempts
    ADD CONSTRAINT chk_session_invocation_provider_attempts_request_ordinal
        CHECK (provider_request_ordinal >= 1);
