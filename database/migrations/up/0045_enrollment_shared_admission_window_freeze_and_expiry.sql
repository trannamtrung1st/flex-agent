-- Additive Enrollment shared-admission hardening. Do not edit 0001-0044.
-- Freeze the already-deployed window_seconds value so it cannot change
-- mid-flight. Do not rewrite a valid longer 0044 window back to 10 seconds.
-- Store and index expires_at so hot-path cleanup is a bounded expiry-range
-- lookup. Backfill and the freeze guard use the aligned end of the deployed
-- policy bucket that contains the last instant of a mismatched counter, not
-- window_start + max(stored, deployed). That keeps a 12s row from being
-- cleaned up while the overlapping 20s acquisition bucket is still open.

ALTER TABLE submissions_enrollment_request_counters
    ADD COLUMN expires_at timestamptz;

CREATE OR REPLACE FUNCTION submissions_enrollment_request_aligned_policy_window_end(
    p_window_start timestamptz,
    p_counter_window_seconds integer,
    p_policy_window_seconds integer)
RETURNS timestamptz
LANGUAGE sql
STABLE
AS $$
    SELECT to_timestamp(
        (
            (
                floor(extract(epoch FROM p_window_start))::bigint
                + p_counter_window_seconds
                + p_policy_window_seconds
                - 1
            ) / p_policy_window_seconds
        ) * p_policy_window_seconds);
$$;

UPDATE submissions_enrollment_request_counters AS counters
SET expires_at = CASE
        WHEN counters.window_seconds IS DISTINCT FROM policies.window_seconds THEN
            submissions_enrollment_request_aligned_policy_window_end(
                counters.window_start,
                counters.window_seconds,
                policies.window_seconds)
        ELSE
            counters.window_start + make_interval(secs => counters.window_seconds)
    END
FROM submissions_enrollment_request_policies AS policies
WHERE policies.singleton_key = 1
  AND counters.expires_at IS NULL;

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

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM submissions_enrollment_request_counters AS counters
        CROSS JOIN submissions_enrollment_request_policies AS policies
        WHERE policies.singleton_key = 1
          AND submissions_enrollment_request_aligned_policy_window_end(
                counters.window_start,
                counters.window_seconds,
                policies.window_seconds) > clock_timestamp()
          AND counters.window_seconds IS DISTINCT FROM policies.window_seconds
    ) THEN
        RAISE EXCEPTION
            'enrollment shared admission cannot freeze the policy while counters still overlap the frozen policy window; wait until those overlapping budgets expire';
    END IF;
END
$$;

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
