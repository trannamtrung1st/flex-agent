-- Phase 8 corrective: canonical evidence locators and criterion-specific evidence refs.
-- Additive after 0082.

ALTER TABLE evaluation_evidence_items
    ADD COLUMN locator_canonical_json JSONB NULL;

CREATE TABLE evaluation_criterion_judgment_evidence_refs (
    organization_id UUID NOT NULL,
    evaluation_id UUID NOT NULL,
    judgment_id UUID NOT NULL,
    evidence_id UUID NOT NULL,
    reference_ordinal INTEGER NOT NULL,
    PRIMARY KEY (organization_id, evaluation_id, judgment_id, evidence_id),
    CONSTRAINT fk_evaluation_criterion_judgment_evidence_refs_judgment
        FOREIGN KEY (organization_id, evaluation_id, judgment_id)
        REFERENCES evaluation_criterion_judgments (organization_id, evaluation_id, judgment_id),
    CONSTRAINT fk_evaluation_criterion_judgment_evidence_refs_evidence
        FOREIGN KEY (organization_id, evaluation_id, evidence_id)
        REFERENCES evaluation_evidence_items (organization_id, evaluation_id, evidence_id),
    CONSTRAINT uq_evaluation_criterion_judgment_evidence_refs_ordinal
        UNIQUE (organization_id, evaluation_id, judgment_id, reference_ordinal),
    CONSTRAINT chk_evaluation_criterion_judgment_evidence_refs_ordinal
        CHECK (reference_ordinal >= 0)
);

CREATE INDEX ix_evaluation_criterion_judgment_evidence_refs_evidence
    ON evaluation_criterion_judgment_evidence_refs (organization_id, evaluation_id, evidence_id);

CREATE TRIGGER trg_evaluation_criterion_judgment_evidence_refs_immutable
    BEFORE UPDATE OR DELETE ON evaluation_criterion_judgment_evidence_refs
    FOR EACH ROW
    EXECUTE FUNCTION reject_evaluation_append_only_mutation();
