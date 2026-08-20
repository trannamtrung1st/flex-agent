-- Identity-level back-channel logout watermarks so a late callback cannot
-- remint a live session after a sub-only logout. Do not rewrite frozen 0001-0032.

CREATE TABLE identity_logout_watermarks (
    issuer TEXT NOT NULL,
    subject TEXT NOT NULL,
    logged_out_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT pk_identity_logout_watermarks PRIMARY KEY (issuer, subject),
    CONSTRAINT chk_identity_logout_watermarks_issuer
        CHECK (char_length(issuer) BETWEEN 1 AND 512),
    CONSTRAINT chk_identity_logout_watermarks_subject
        CHECK (char_length(subject) BETWEEN 1 AND 256)
);
