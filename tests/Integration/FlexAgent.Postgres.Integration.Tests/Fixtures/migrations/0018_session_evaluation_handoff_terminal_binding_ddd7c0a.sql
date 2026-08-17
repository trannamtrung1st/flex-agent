-- Bind Evaluation handoff to the sealed terminal record. Do not rewrite 0017.
-- 0017 is frozen at f1122dc (legacy-unsealed CHECK plus v2 cutoff). Databases
-- that recorded aa424f3's earlier 0017 hash cannot apply this frozen 0017 and
-- must be rebuilt; that script was unfrozen and replaced before any intended
-- persistent environment freeze.
-- UTC-ordered; additive after frozen 0017.

ALTER TABLE session_runtimes
    ADD CONSTRAINT uq_session_runtimes_org_session_lifecycle
    UNIQUE (organization_id, session_id, lifecycle_state);

ALTER TABLE session_terminal_records
    ADD CONSTRAINT uq_session_terminal_records_handoff_identity
    UNIQUE NULLS NOT DISTINCT (
        organization_id,
        session_id,
        terminal_record_id,
        lifecycle_state,
        cutoff_sequence,
        seal_digest,
        procedure_id);

ALTER TABLE session_evaluation_handoffs
    ADD COLUMN terminal_record_id UUID NULL;

ALTER TABLE session_evaluation_handoffs
    ADD COLUMN procedure_id TEXT NULL;

UPDATE session_evaluation_handoffs AS handoff
SET
    terminal_record_id = terminal.terminal_record_id,
    procedure_id = terminal.procedure_id
FROM session_terminal_records AS terminal
WHERE handoff.organization_id = terminal.organization_id
  AND handoff.session_id = terminal.session_id
  AND handoff.terminal_state = terminal.lifecycle_state
  AND handoff.cutoff_sequence IS NOT DISTINCT FROM terminal.cutoff_sequence
  AND handoff.seal_digest IS NOT DISTINCT FROM terminal.seal_digest
  AND terminal.procedure_id IS NOT NULL
  AND terminal.seal_digest IS NOT NULL;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM session_evaluation_handoffs
        WHERE terminal_record_id IS NULL
           OR procedure_id IS NULL)
    THEN
        RAISE EXCEPTION
            'session_evaluation_handoffs cannot bind to a matching session_terminal_records row';
    END IF;
END $$;

ALTER TABLE session_evaluation_handoffs
    ALTER COLUMN terminal_record_id SET NOT NULL;

ALTER TABLE session_evaluation_handoffs
    ALTER COLUMN procedure_id SET NOT NULL;

ALTER TABLE session_evaluation_handoffs
    ADD CONSTRAINT chk_session_evaluation_handoffs_procedure
        CHECK (procedure_id IN ('manifest-jcs-sha256-v1', 'manifest-jcs-sha256-v2'));

ALTER TABLE session_evaluation_handoffs
    ADD CONSTRAINT chk_session_evaluation_handoffs_eligible_v2
        CHECK (
            eligibility <> 'eligible'
            OR (
                procedure_id = 'manifest-jcs-sha256-v2'
                AND cutoff_sequence IS NOT NULL));

ALTER TABLE session_evaluation_handoffs
    ADD CONSTRAINT fk_session_evaluation_handoffs_terminal
        FOREIGN KEY (
            organization_id,
            session_id,
            terminal_record_id,
            terminal_state,
            cutoff_sequence,
            seal_digest,
            procedure_id)
        REFERENCES session_terminal_records (
            organization_id,
            session_id,
            terminal_record_id,
            lifecycle_state,
            cutoff_sequence,
            seal_digest,
            procedure_id);

ALTER TABLE session_evaluation_handoffs
    ADD CONSTRAINT fk_session_evaluation_handoffs_runtime_lifecycle
        FOREIGN KEY (organization_id, session_id, terminal_state)
        REFERENCES session_runtimes (organization_id, session_id, lifecycle_state);
