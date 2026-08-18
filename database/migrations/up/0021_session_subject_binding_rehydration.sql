-- Session-scoped actor relationships and frozen-policy binding snapshots.
-- Do not rewrite frozen 0005-0019. Production SSE rehydrates (actor, session)
-- current relationship and TrustedSessionBinding from these records.
-- Frozen policy payloads cannot be reconstructed from 0020 session_runtimes,
-- so a populated upgrade is refused rather than stranding Sessions.

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM session_runtimes) THEN
        RAISE EXCEPTION '0021 requires empty session_runtimes because frozen-policy snapshots cannot be backfilled from 0020; refusing a populated upgrade';
    END IF;
END;
$$;

CREATE TABLE session_frozen_policy_snapshots (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    configuration_id TEXT NOT NULL,
    configuration_digest CHAR(64) NOT NULL,
    manifest_id TEXT NOT NULL,
    policy_digest CHAR(64) NOT NULL,
    policy_payload JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id),
    CONSTRAINT fk_session_frozen_policy_snapshots_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_session_frozen_policy_snapshots_digest_lowercase
        CHECK (
            configuration_digest = lower(configuration_digest)
            AND policy_digest = lower(policy_digest)
            AND configuration_digest = policy_digest
        )
);

CREATE TRIGGER trg_session_frozen_policy_snapshots_stamp_created
    BEFORE INSERT ON session_frozen_policy_snapshots
    FOR EACH ROW
    EXECUTE FUNCTION stamp_session_created_at();

CREATE TRIGGER trg_session_frozen_policy_snapshots_no_update
    BEFORE UPDATE ON session_frozen_policy_snapshots
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TRIGGER trg_session_frozen_policy_snapshots_no_delete
    BEFORE DELETE ON session_frozen_policy_snapshots
    FOR EACH ROW
    EXECUTE FUNCTION reject_session_append_only_mutation();

CREATE TABLE session_actor_relationships (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    actor_id UUID NOT NULL REFERENCES actors (id),
    actor_type TEXT NOT NULL,
    relationship TEXT NOT NULL,
    relationship_version BIGINT NOT NULL,
    revoked_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (organization_id, session_id, actor_id),
    CONSTRAINT fk_session_actor_relationships_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_session_actor_relationships_relationship
        CHECK (relationship IN ('participant', 'reviewer', 'administrator')),
    CONSTRAINT chk_session_actor_relationships_actor_type
        CHECK (btrim(actor_type) <> ''),
    CONSTRAINT chk_session_actor_relationships_version
        CHECK (relationship_version >= 1)
);

CREATE UNIQUE INDEX ix_session_actor_relationships_current
    ON session_actor_relationships (actor_id, session_id)
    WHERE revoked_at IS NULL;

CREATE INDEX ix_session_actor_relationships_session
    ON session_actor_relationships (organization_id, session_id)
    WHERE revoked_at IS NULL;

COMMENT ON TABLE session_frozen_policy_snapshots IS
    'Immutable frozen runtime policy used to rehydrate TrustedSessionBinding.';

COMMENT ON TABLE session_actor_relationships IS
    'Current actor relationship to one Session; revoke independently of org grants.';
