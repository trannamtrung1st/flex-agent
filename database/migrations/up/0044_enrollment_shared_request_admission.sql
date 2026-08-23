-- Additive Submissions-owned deployment-wide Enrollment request-admission
-- policy and short-lived fixed-window counters. Do not edit 0001-0043.

CREATE TABLE submissions_enrollment_request_policies (
    singleton_key SMALLINT NOT NULL PRIMARY KEY,
    policy_revision INTEGER NOT NULL,
    read_permit_limit INTEGER NOT NULL,
    mutation_permit_limit INTEGER NOT NULL,
    window_seconds INTEGER NOT NULL,
    activated_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_submissions_enrollment_request_policies_singleton
        CHECK (singleton_key = 1),
    CONSTRAINT chk_submissions_enrollment_request_policies_revision
        CHECK (policy_revision >= 1),
    CONSTRAINT chk_submissions_enrollment_request_policies_read_limit
        CHECK (read_permit_limit BETWEEN 1 AND 60),
    CONSTRAINT chk_submissions_enrollment_request_policies_mutation_limit
        CHECK (mutation_permit_limit BETWEEN 1 AND 20),
    CONSTRAINT chk_submissions_enrollment_request_policies_window
        CHECK (window_seconds >= 10)
);

CREATE OR REPLACE FUNCTION submissions_enrollment_request_policies_tighten_only()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.policy_revision <= OLD.policy_revision THEN
        RAISE EXCEPTION 'enrollment request policy revision must increase';
    END IF;

    IF NEW.read_permit_limit > OLD.read_permit_limit
        OR NEW.mutation_permit_limit > OLD.mutation_permit_limit
        OR NEW.window_seconds < OLD.window_seconds THEN
        RAISE EXCEPTION 'enrollment request policy may only tighten';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_submissions_enrollment_request_policies_tighten_only
    BEFORE UPDATE ON submissions_enrollment_request_policies
    FOR EACH ROW
    EXECUTE FUNCTION submissions_enrollment_request_policies_tighten_only();

INSERT INTO submissions_enrollment_request_policies (
    singleton_key,
    policy_revision,
    read_permit_limit,
    mutation_permit_limit,
    window_seconds,
    activated_at)
VALUES (
    1,
    1,
    60,
    20,
    10,
    TIMESTAMPTZ '2026-08-23 00:00:00+00');

CREATE TABLE submissions_enrollment_request_counters (
    organization_id UUID NOT NULL,
    actor_id UUID NOT NULL,
    surface TEXT NOT NULL,
    window_start TIMESTAMPTZ NOT NULL,
    window_seconds INTEGER NOT NULL,
    policy_revision INTEGER NOT NULL,
    permit_count INTEGER NOT NULL,
    PRIMARY KEY (organization_id, actor_id, surface, window_start),
    CONSTRAINT chk_submissions_enrollment_request_counters_surface
        CHECK (surface IN ('read', 'mutation')),
    CONSTRAINT chk_submissions_enrollment_request_counters_window
        CHECK (window_seconds >= 10),
    CONSTRAINT chk_submissions_enrollment_request_counters_revision
        CHECK (policy_revision >= 1),
    CONSTRAINT chk_submissions_enrollment_request_counters_count
        CHECK (permit_count >= 0)
);

CREATE INDEX ix_submissions_enrollment_request_counters_cleanup
    ON submissions_enrollment_request_counters (window_start);

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

    DELETE FROM submissions_enrollment_request_counters
    WHERE ctid IN (
        SELECT ctid
        FROM submissions_enrollment_request_counters
        WHERE window_start + make_interval(secs => window_seconds) <= v_now
        ORDER BY window_start
        LIMIT GREATEST(COALESCE(p_cleanup_limit, 0), 0)
    );

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
