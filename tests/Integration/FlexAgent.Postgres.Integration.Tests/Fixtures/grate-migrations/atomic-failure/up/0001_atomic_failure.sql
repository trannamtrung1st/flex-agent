-- Test-only migration: proves Grate rolls back a failed one-time script transactionally.
CREATE TABLE grate_atomic_failure_probe (id INT PRIMARY KEY);
DO $$ BEGIN RAISE EXCEPTION 'flexagent injected migration failure'; END $$;
