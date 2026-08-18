-- Additive service-delegation records for delayed Session timer work (ADR-002 /
-- proposed ADR-015). Do not rewrite frozen 0001-0021. Historical timer rows
-- keep a null envelope reference and remain fail-closed until an authorized
-- repair path supplies one; this script does not backfill authority.

CREATE TABLE service_delegations (
    delegation_id UUID PRIMARY KEY,
    organization_id UUID NOT NULL REFERENCES organizations (id),
    activity_id UUID NOT NULL,
    participant_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    session_id UUID NOT NULL,
    service_actor_id UUID NOT NULL REFERENCES actors (id),
    allowed_action TEXT NOT NULL,
    system_purpose TEXT NOT NULL,
    initiating_authority TEXT NOT NULL,
    effective_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ NULL,
    revoked_at TIMESTAMPTZ NULL,
    delegation_version BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT fk_service_delegations_runtime
        FOREIGN KEY (organization_id, activity_id, participant_id, attempt_id, session_id)
        REFERENCES session_runtimes (organization_id, activity_id, participant_id, attempt_id, session_id),
    CONSTRAINT chk_service_delegations_action
        CHECK (allowed_action ~ '^[a-z][a-z0-9._]*$'),
    CONSTRAINT chk_service_delegations_purpose
        CHECK (char_length(system_purpose) BETWEEN 1 AND 128),
    CONSTRAINT chk_service_delegations_authority
        CHECK (char_length(initiating_authority) BETWEEN 1 AND 128),
    CONSTRAINT chk_service_delegations_version
        CHECK (delegation_version >= 1),
    CONSTRAINT chk_service_delegations_expiry
        CHECK (expires_at IS NULL OR expires_at > effective_at)
);

CREATE UNIQUE INDEX uq_service_delegations_active_session_action
    ON service_delegations (organization_id, session_id, allowed_action)
    WHERE revoked_at IS NULL;

CREATE INDEX ix_service_delegations_service_actor
    ON service_delegations (service_actor_id, organization_id)
    WHERE revoked_at IS NULL;

ALTER TABLE session_timer_schedules
    ADD COLUMN IF NOT EXISTS timer_lane_delegation_id UUID NULL;

ALTER TABLE session_timer_schedules
    DROP CONSTRAINT IF EXISTS fk_session_timer_schedules_timer_lane_delegation;

ALTER TABLE session_timer_schedules
    ADD CONSTRAINT fk_session_timer_schedules_timer_lane_delegation
        FOREIGN KEY (timer_lane_delegation_id)
        REFERENCES service_delegations (delegation_id);
