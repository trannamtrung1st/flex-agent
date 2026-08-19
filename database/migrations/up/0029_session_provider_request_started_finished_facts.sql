-- Append-only provider-request facts: reservation (started) and completion (finished).
-- Do not rewrite frozen 0001-0028. Temporarily disable the append-only UPDATE
-- trigger so this migration-owned backfill can stamp existing rows as finished.

ALTER TABLE session_invocation_provider_attempts
    ADD COLUMN fact_kind TEXT;

ALTER TABLE session_invocation_provider_attempts
    DISABLE TRIGGER trg_session_invocation_provider_attempts_no_update;

UPDATE session_invocation_provider_attempts
SET fact_kind = 'finished'
WHERE fact_kind IS NULL;

ALTER TABLE session_invocation_provider_attempts
    ENABLE TRIGGER trg_session_invocation_provider_attempts_no_update;

ALTER TABLE session_invocation_provider_attempts
    ALTER COLUMN fact_kind SET NOT NULL;

ALTER TABLE session_invocation_provider_attempts
    DROP CONSTRAINT session_invocation_provider_attempts_pkey;

ALTER TABLE session_invocation_provider_attempts
    ADD PRIMARY KEY (organization_id, session_id, provider_request_id, fact_kind);

ALTER TABLE session_invocation_provider_attempts
    ADD CONSTRAINT chk_session_invocation_provider_attempts_fact_kind
        CHECK (fact_kind IN ('started', 'finished'));
