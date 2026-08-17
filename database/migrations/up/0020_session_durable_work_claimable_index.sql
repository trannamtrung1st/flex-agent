-- Partial index of claimable invocation.execute work. Do not rewrite frozen 0005-0019.
-- Completed, failed, and cancelled history stays out of the claim scan.

CREATE INDEX ix_session_durable_work_claimable
    ON session_durable_work (
        work_type,
        organization_id,
        activity_id,
        last_committed_at,
        work_id)
    WHERE work_type = 'invocation.execute'
      AND (
            state = 'pending'
            OR (state = 'claimed' AND claim_lease_until IS NOT NULL)
          );

COMMENT ON INDEX ix_session_durable_work_claimable IS
    'Claimable invocation.execute rows only, so completed history is not scanned during fair claim.';
