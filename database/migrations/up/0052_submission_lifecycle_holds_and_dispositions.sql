-- Additive Submission lifecycle holds, artifact dispositions, and short-lived
-- protected-access capabilities. Do not edit 0001-0051.

CREATE TABLE submissions_lifecycle_holds (
    organization_id UUID NOT NULL,
    hold_id UUID NOT NULL,
    artifact_object_key TEXT NOT NULL,
    reason_code TEXT NOT NULL,
    active BOOLEAN NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, hold_id),
    CONSTRAINT chk_submissions_lifecycle_holds_reason
        CHECK (reason_code = 'legal_hold')
);

CREATE INDEX ix_submissions_lifecycle_holds_artifact
    ON submissions_lifecycle_holds (organization_id, artifact_object_key)
    WHERE active;

CREATE TABLE submissions_artifact_dispositions (
    organization_id UUID NOT NULL,
    disposition_id UUID NOT NULL,
    work_kind TEXT NOT NULL,
    artifact_object_key TEXT NOT NULL,
    disposed_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, disposition_id),
    CONSTRAINT chk_submissions_artifact_dispositions_kind
        CHECK (work_kind IN ('cleanup_incomplete', 'cleanup_rejected', 'cleanup_orphan'))
);

CREATE TABLE submissions_protected_capabilities (
    organization_id UUID NOT NULL,
    capability_id UUID NOT NULL,
    actor_id UUID NOT NULL,
    enrollment_id UUID NOT NULL,
    version_id UUID NOT NULL,
    item_id UUID NOT NULL,
    action TEXT NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    redeemed_at TIMESTAMPTZ NULL,
    PRIMARY KEY (organization_id, capability_id),
    CONSTRAINT chk_submissions_protected_capabilities_action
        CHECK (action IN ('preview_item', 'download_item'))
);

CREATE INDEX ix_submissions_protected_capabilities_expiry
    ON submissions_protected_capabilities (expires_at)
    WHERE redeemed_at IS NULL;
