-- Additive Enrollment shared-admission hardening. Do not edit 0001-0044.
-- Freeze policy window_seconds so a mid-window duration change cannot
-- expire the live counter and issue a second budget. Store and index
-- expires_at so hot-path cleanup is a bounded expiry-range lookup.

CREATE OR REPLACE FUNCTION submissions_enrollment_request_policies_tighten_only()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.policy_revision <= OLD.policy_revision THEN
        RAISE EXCEPTION 'enrollment request policy revision must increase';
    END IF;

    IF NEW.window_seconds IS DISTINCT FROM OLD.window_seconds THEN
        RAISE EXCEPTION 'enrollment request policy window cannot change';
    END IF;

    IF NEW.read_permit_limit > OLD.read_permit_limit
        OR NEW.mutation_permit_limit > OLD.mutation_permit_limit THEN
        RAISE EXCEPTION 'enrollment request policy may only tighten';
    END IF;

    RETURN NEW;
END;
$$;

ALTER TABLE submissions_enrollment_request_policies
    DROP CONSTRAINT chk_submissions_enrollment_request_policies_window;

ALTER TABLE submissions_enrollment_request_policies
    ADD CONSTRAINT chk_submissions_enrollment_request_policies_window
        CHECK (window_seconds = 10);

ALTER TABLE submissions_enrollment_request_counters
    ADD COLUMN expires_at timestamptz;

UPDATE submissions_enrollment_request_counters
SET expires_at = window_start + make_interval(secs => window_seconds)
WHERE expires_at IS NULL;

ALTER TABLE submissions_enrollment_request_counters
    ALTER COLUMN expires_at SET NOT NULL;

CREATE OR REPLACE FUNCTION submissions_enrollment_request_counters_set_expires_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.expires_at := NEW.window_start + make_interval(secs => NEW.window_seconds);
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_submissions_enrollment_request_counters_set_expires_at
    BEFORE INSERT OR UPDATE OF window_start, window_seconds, expires_at
    ON submissions_enrollment_request_counters
    FOR EACH ROW
    EXECUTE FUNCTION submissions_enrollment_request_counters_set_expires_at();

DROP INDEX ix_submissions_enrollment_request_counters_cleanup;

CREATE INDEX ix_submissions_enrollment_request_counters_expires_at
    ON submissions_enrollment_request_counters (expires_at);

CREATE OR REPLACE FUNCTION submissions_try_acquire_enrollment_request_permit(
    p_organization_id uuid,
    p_actor_id uuid,
    p_surface text,
    p_expected_revision integer,
    p_expected_read_limit integer,
    p_expected_mutation_limit integer,
    p_expected_window_seconds integer,
    p_cleanup_limit integer)
RETURNS TABLE(decision text, retry_after_seconds integer, permit_count integer)
LANGUAGE plpgsql
AS $$
DECLARE
    v_policy submissions_enrollment_request_policies%ROWTYPE;
    v_now timestamptz;
    v_window_start timestamptz;
    v_limit integer;
    v_retry integer;
    v_count integer := NULL;
BEGIN
    v_now := clock_timestamp();

    WITH expired AS (
        SELECT ctid
        FROM submissions_enrollment_request_counters
        WHERE expires_at <= v_now
        ORDER BY expires_at
        FOR UPDATE SKIP LOCKED
        LIMIT GREATEST(COALESCE(p_cleanup_limit, 0), 0)
    )
    DELETE FROM submissions_enrollment_request_counters AS counters
    USING expired
    WHERE counters.ctid = expired.ctid;

    SELECT *
    INTO v_policy
    FROM submissions_enrollment_request_policies
    WHERE singleton_key = 1;

    IF NOT FOUND
        OR v_policy.policy_revision IS DISTINCT FROM p_expected_revision
        OR v_policy.read_permit_limit IS DISTINCT FROM p_expected_read_limit
        OR v_policy.mutation_permit_limit IS DISTINCT FROM p_expected_mutation_limit
        OR v_policy.window_seconds IS DISTINCT FROM p_expected_window_seconds
        OR p_surface NOT IN ('read', 'mutation') THEN
        decision := 'unavailable';
        retry_after_seconds := 0;
        permit_count := 0;
        RETURN NEXT;
        RETURN;
    END IF;

    v_limit := CASE
        WHEN p_surface = 'mutation' THEN v_policy.mutation_permit_limit
        ELSE v_policy.read_permit_limit
    END;
    v_window_start := to_timestamp(
        (floor(extract(epoch FROM v_now) / v_policy.window_seconds))::bigint
        * v_policy.window_seconds);
    v_retry := GREATEST(
        1,
        CEIL(EXTRACT(EPOCH FROM (
            v_window_start + make_interval(secs => v_policy.window_seconds) - v_now)))::integer);

    INSERT INTO submissions_enrollment_request_counters (
        organization_id,
        actor_id,
        surface,
        window_start,
        window_seconds,
        policy_revision,
        permit_count)
    VALUES (
        p_organization_id,
        p_actor_id,
        p_surface,
        v_window_start,
        v_policy.window_seconds,
        v_policy.policy_revision,
        1)
    ON CONFLICT (organization_id, actor_id, surface, window_start)
    DO UPDATE SET permit_count = submissions_enrollment_request_counters.permit_count + 1
    WHERE submissions_enrollment_request_counters.permit_count < v_limit
    RETURNING submissions_enrollment_request_counters.permit_count INTO v_count;

    IF v_count IS NULL THEN
        decision := 'exhausted';
        retry_after_seconds := v_retry;
        permit_count := v_limit;
        RETURN NEXT;
        RETURN;
    END IF;

    decision := 'permitted';
    retry_after_seconds := 0;
    permit_count := v_count;
    RETURN NEXT;
END;
$$;
