-- Additive audit/delegation history for REQ-AUTH-27/31 and bounded timer-lane
-- expiry (proposed ADR-015). Do not rewrite frozen 0001-0022.
-- d175099 applied this script without a populated-0022 expiry guard. Databases
-- that recorded that hash must rebuild. This revision refuses unbounded or
-- over-long session.timer_lane.fire rows with an operator-facing error instead
-- of a generic CHECK failure, and does not fabricate expiry timestamps.

-- 58f2595 applied a 0023 revision whose preflight and CHECK also rejected
-- revoked historically unbounded rows, so the documented revoke-then-upgrade
-- repair could not succeed. Databases that recorded that hash must rebuild.
-- This revision inspects only active timer-lane rows and preserves revoked
-- 0022 history without fabricating expiry.

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM service_delegations
        WHERE allowed_action = 'session.timer_lane.fire'
          AND revoked_at IS NULL
          AND (
                expires_at IS NULL
                OR expires_at > effective_at + INTERVAL '7 days'
              )
    ) THEN
        RAISE EXCEPTION '0023 refuses unbounded or over-long active session.timer_lane.fire delegations left by 0022; revoke those rows before upgrade; refusing fabricated expiry backfill';
    END IF;
END;
$$;

ALTER TABLE audit_events
    ADD COLUMN IF NOT EXISTS authorization_reference_type TEXT NULL;

ALTER TABLE audit_events
    ADD COLUMN IF NOT EXISTS authorization_reference_id UUID NULL;

ALTER TABLE audit_events
    DROP CONSTRAINT IF EXISTS chk_audit_events_authorization_reference;

ALTER TABLE audit_events
    ADD CONSTRAINT chk_audit_events_authorization_reference
        CHECK (
            (authorization_reference_type IS NULL AND authorization_reference_id IS NULL)
            OR (
                authorization_reference_type IS NOT NULL
                AND authorization_reference_id IS NOT NULL
                AND authorization_reference_type ~ '^[a-z][a-z0-9._]*$'
                AND char_length(authorization_reference_type) BETWEEN 1 AND 64
            )
        );

CREATE TABLE service_delegation_transitions (
    transition_id UUID PRIMARY KEY,
    delegation_id UUID NOT NULL REFERENCES service_delegations (delegation_id),
    organization_id UUID NOT NULL REFERENCES organizations (id),
    session_id UUID NOT NULL,
    mutation_kind TEXT NOT NULL,
    previous_allowed_action TEXT NULL,
    new_allowed_action TEXT NOT NULL,
    previous_revoked_at TIMESTAMPTZ NULL,
    new_revoked_at TIMESTAMPTZ NULL,
    previous_expires_at TIMESTAMPTZ NULL,
    new_expires_at TIMESTAMPTZ NULL,
    delegation_version BIGINT NOT NULL,
    actor_id UUID NOT NULL REFERENCES actors (id),
    actor_type TEXT NOT NULL,
    reason TEXT NOT NULL,
    correlation_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT chk_service_delegation_transitions_kind
        CHECK (mutation_kind IN ('issue', 'revoke', 'narrow')),
    CONSTRAINT chk_service_delegation_transitions_reason
        CHECK (char_length(reason) BETWEEN 1 AND 128),
    CONSTRAINT chk_service_delegation_transitions_actor_type
        CHECK (char_length(actor_type) BETWEEN 1 AND 64),
    CONSTRAINT chk_service_delegation_transitions_version
        CHECK (delegation_version >= 1)
);

CREATE INDEX ix_service_delegation_transitions_delegation
    ON service_delegation_transitions (organization_id, delegation_id, occurred_at);

CREATE OR REPLACE FUNCTION reject_service_delegation_transition_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'service_delegation_transitions are append-only';
END;
$$;

CREATE TRIGGER trg_service_delegation_transitions_no_update
    BEFORE UPDATE ON service_delegation_transitions
    FOR EACH ROW
    EXECUTE FUNCTION reject_service_delegation_transition_mutation();

CREATE TRIGGER trg_service_delegation_transitions_no_delete
    BEFORE DELETE ON service_delegation_transitions
    FOR EACH ROW
    EXECUTE FUNCTION reject_service_delegation_transition_mutation();

ALTER TABLE service_delegations
    DROP CONSTRAINT IF EXISTS chk_service_delegations_timer_lane_fire_expiry;

ALTER TABLE service_delegations
    ADD CONSTRAINT chk_service_delegations_timer_lane_fire_expiry
        CHECK (
            allowed_action <> 'session.timer_lane.fire'
            OR revoked_at IS NOT NULL
            OR (
                expires_at IS NOT NULL
                AND expires_at <= effective_at + INTERVAL '7 days'
            )
        );
