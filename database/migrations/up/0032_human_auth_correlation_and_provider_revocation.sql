-- Browser-bound OIDC login correlation and provider-session logout
-- tombstones. Do not rewrite frozen 0001-0031.

ALTER TABLE oidc_login_transactions
    ADD COLUMN correlation_digest CHAR(64);

UPDATE oidc_login_transactions
SET correlation_digest = state_digest
WHERE correlation_digest IS NULL;

ALTER TABLE oidc_login_transactions
    ALTER COLUMN correlation_digest SET NOT NULL;

ALTER TABLE oidc_login_transactions
    ADD CONSTRAINT chk_oidc_login_transactions_correlation_digest
        CHECK (correlation_digest = lower(correlation_digest));

CREATE TABLE revoked_provider_sessions (
    provider_session_digest CHAR(64) PRIMARY KEY,
    revoked_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_revoked_provider_sessions_digest_lowercase
        CHECK (provider_session_digest = lower(provider_session_digest))
);
