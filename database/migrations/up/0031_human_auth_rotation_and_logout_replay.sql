-- One successor per predecessor and single-use logout-token replay
-- protection. Do not rewrite frozen 0001-0030.

CREATE UNIQUE INDEX uq_application_sessions_predecessor
    ON application_sessions (predecessor_session_id)
    WHERE predecessor_session_id IS NOT NULL;

CREATE TABLE consumed_logout_tokens (
    issuer TEXT NOT NULL,
    jti TEXT NOT NULL,
    consumed_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT pk_consumed_logout_tokens PRIMARY KEY (issuer, jti),
    CONSTRAINT chk_consumed_logout_tokens_issuer
        CHECK (char_length(issuer) BETWEEN 1 AND 512),
    CONSTRAINT chk_consumed_logout_tokens_jti
        CHECK (char_length(jti) BETWEEN 1 AND 256)
);
