-- Assessment Configuration ownership plus Configuration readiness descriptors.
-- Additive after 0033. Do not edit 0001-0033.

CREATE TABLE configuration_source_readiness_descriptors (
    organization_id UUID NOT NULL,
    configuration_source_id UUID NOT NULL,
    version_id UUID NOT NULL,
    source_kind TEXT NOT NULL,
    category TEXT NOT NULL,
    lifecycle_state TEXT NOT NULL,
    compatibility_key TEXT NOT NULL,
    capability_text_enabled BOOLEAN NOT NULL,
    capability_voice_enabled BOOLEAN NOT NULL,
    capability_tools_enabled BOOLEAN NOT NULL,
    capability_dynamic_memory_writes_enabled BOOLEAN NOT NULL,
    capability_shared_session_enabled BOOLEAN NOT NULL,
    capability_direct_deployment_enabled BOOLEAN NOT NULL,
    production_eligible BOOLEAN NOT NULL,
    transactionally_revalidatable BOOLEAN NOT NULL,
    effective_values JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, version_id),
    CONSTRAINT fk_configuration_source_readiness_descriptors_version
        FOREIGN KEY (organization_id, configuration_source_id, version_id)
        REFERENCES configuration_source_versions (organization_id, configuration_source_id, id),
    CONSTRAINT chk_configuration_source_readiness_lifecycle
        CHECK (lifecycle_state IN ('available', 'revoked', 'unavailable', 'mutable_alias')),
    CONSTRAINT chk_configuration_source_readiness_kind
        CHECK (char_length(source_kind) BETWEEN 1 AND 128),
    CONSTRAINT chk_configuration_source_readiness_category
        CHECK (char_length(category) BETWEEN 1 AND 64),
    CONSTRAINT chk_configuration_source_readiness_compatibility
        CHECK (char_length(compatibility_key) BETWEEN 1 AND 128)
);

CREATE TABLE assessment_activities (
    organization_id UUID NOT NULL REFERENCES organizations (id),
    activity_id UUID NOT NULL,
    form TEXT NOT NULL,
    configured_type TEXT NOT NULL,
    current_revision_id UUID NOT NULL,
    current_revision_number BIGINT NOT NULL,
    has_activated_cohort BOOLEAN NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, activity_id),
    CONSTRAINT chk_assessment_activities_form CHECK (form = 'campaign'),
    CONSTRAINT chk_assessment_activities_type CHECK (configured_type = 'assessment'),
    CONSTRAINT chk_assessment_activities_revision_number CHECK (current_revision_number >= 1)
);

CREATE TABLE assessment_activity_revisions (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    revision_id UUID NOT NULL,
    revision_number BIGINT NOT NULL,
    title TEXT NOT NULL,
    content JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, revision_id),
    CONSTRAINT fk_assessment_activity_revisions_activity
        FOREIGN KEY (organization_id, activity_id)
        REFERENCES assessment_activities (organization_id, activity_id),
    CONSTRAINT uq_assessment_activity_revisions_number
        UNIQUE (organization_id, activity_id, revision_number),
    CONSTRAINT chk_assessment_activity_revisions_title
        CHECK (char_length(title) BETWEEN 1 AND 200),
    CONSTRAINT chk_assessment_activity_revisions_number
        CHECK (revision_number >= 1)
);

ALTER TABLE assessment_activities
    ADD CONSTRAINT fk_assessment_activities_current_revision
        FOREIGN KEY (organization_id, current_revision_id)
        REFERENCES assessment_activity_revisions (organization_id, revision_id)
        DEFERRABLE INITIALLY DEFERRED;

CREATE TABLE assessment_cohorts (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    cohort_id UUID NOT NULL,
    state TEXT NOT NULL,
    bound_revision_id UUID NOT NULL,
    bound_revision_number BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, cohort_id),
    CONSTRAINT fk_assessment_cohorts_activity
        FOREIGN KEY (organization_id, activity_id)
        REFERENCES assessment_activities (organization_id, activity_id),
    CONSTRAINT fk_assessment_cohorts_revision
        FOREIGN KEY (organization_id, bound_revision_id)
        REFERENCES assessment_activity_revisions (organization_id, revision_id),
    CONSTRAINT chk_assessment_cohorts_state
        CHECK (state IN ('draft', 'activated')),
    CONSTRAINT chk_assessment_cohorts_revision_number
        CHECK (bound_revision_number >= 1)
);

CREATE TABLE assessment_activation_baselines (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    baseline_id UUID NOT NULL,
    content_digest CHAR(64) NOT NULL,
    procedure_id TEXT NOT NULL,
    schema_version TEXT NOT NULL,
    canonicalization_version TEXT NOT NULL,
    document JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, baseline_id),
    CONSTRAINT fk_assessment_activation_baselines_activity
        FOREIGN KEY (organization_id, activity_id)
        REFERENCES assessment_activities (organization_id, activity_id),
    CONSTRAINT chk_assessment_activation_baselines_digest
        CHECK (content_digest = lower(content_digest) AND char_length(content_digest) = 64),
    CONSTRAINT chk_assessment_activation_baselines_procedure
        CHECK (procedure_id = 'activation-baseline-jcs-sha256-v1'),
    CONSTRAINT chk_assessment_activation_baselines_schema
        CHECK (schema_version = 'v1'),
    CONSTRAINT chk_assessment_activation_baselines_canonicalization
        CHECK (canonicalization_version = 'rfc8785')
);

CREATE TABLE assessment_cohort_baseline_bindings (
    organization_id UUID NOT NULL,
    cohort_id UUID NOT NULL,
    baseline_id UUID NOT NULL,
    bound_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, cohort_id),
    CONSTRAINT uq_assessment_cohort_baseline_bindings_baseline
        UNIQUE (organization_id, baseline_id),
    CONSTRAINT fk_assessment_cohort_baseline_bindings_cohort
        FOREIGN KEY (organization_id, cohort_id)
        REFERENCES assessment_cohorts (organization_id, cohort_id),
    CONSTRAINT fk_assessment_cohort_baseline_bindings_baseline
        FOREIGN KEY (organization_id, baseline_id)
        REFERENCES assessment_activation_baselines (organization_id, baseline_id)
);

CREATE TABLE assessment_activation_attempts (
    organization_id UUID NOT NULL,
    activity_id UUID NOT NULL,
    cohort_id UUID NOT NULL,
    attempt_id UUID NOT NULL,
    expected_revision_id UUID NOT NULL,
    expected_revision_number BIGINT NOT NULL,
    idempotency_key TEXT NOT NULL,
    command_digest CHAR(64) NOT NULL,
    outcome_code TEXT NOT NULL,
    baseline_id UUID NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (organization_id, attempt_id),
    CONSTRAINT fk_assessment_activation_attempts_cohort
        FOREIGN KEY (organization_id, cohort_id)
        REFERENCES assessment_cohorts (organization_id, cohort_id),
    CONSTRAINT uq_assessment_activation_attempts_idempotency
        UNIQUE (organization_id, activity_id, cohort_id, idempotency_key),
    CONSTRAINT chk_assessment_activation_attempts_digest
        CHECK (command_digest = lower(command_digest) AND char_length(command_digest) = 64)
);

CREATE OR REPLACE FUNCTION reject_assessment_revision_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'assessment_activity_revisions are immutable';
END;
$$;

CREATE TRIGGER trg_assessment_activity_revisions_no_update
    BEFORE UPDATE ON assessment_activity_revisions
    FOR EACH ROW
    EXECUTE FUNCTION reject_assessment_revision_mutation();

CREATE TRIGGER trg_assessment_activity_revisions_no_delete
    BEFORE DELETE ON assessment_activity_revisions
    FOR EACH ROW
    EXECUTE FUNCTION reject_assessment_revision_mutation();

CREATE OR REPLACE FUNCTION reject_assessment_baseline_mutation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'assessment activation baselines and bindings are immutable';
END;
$$;

CREATE TRIGGER trg_assessment_activation_baselines_no_update
    BEFORE UPDATE ON assessment_activation_baselines
    FOR EACH ROW
    EXECUTE FUNCTION reject_assessment_baseline_mutation();

CREATE TRIGGER trg_assessment_activation_baselines_no_delete
    BEFORE DELETE ON assessment_activation_baselines
    FOR EACH ROW
    EXECUTE FUNCTION reject_assessment_baseline_mutation();

CREATE TRIGGER trg_assessment_cohort_baseline_bindings_no_update
    BEFORE UPDATE ON assessment_cohort_baseline_bindings
    FOR EACH ROW
    EXECUTE FUNCTION reject_assessment_baseline_mutation();

CREATE TRIGGER trg_assessment_cohort_baseline_bindings_no_delete
    BEFORE DELETE ON assessment_cohort_baseline_bindings
    FOR EACH ROW
    EXECUTE FUNCTION reject_assessment_baseline_mutation();

CREATE OR REPLACE FUNCTION reject_activated_cohort_material_update()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF OLD.state = 'activated' AND (
        NEW.state IS DISTINCT FROM OLD.state
        OR NEW.bound_revision_id IS DISTINCT FROM OLD.bound_revision_id
        OR NEW.bound_revision_number IS DISTINCT FROM OLD.bound_revision_number
        OR NEW.activity_id IS DISTINCT FROM OLD.activity_id
        OR NEW.organization_id IS DISTINCT FROM OLD.organization_id
    ) THEN
        RAISE EXCEPTION 'activated assessment cohorts are immutable';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_assessment_cohorts_activated_immutable
    BEFORE UPDATE ON assessment_cohorts
    FOR EACH ROW
    EXECUTE FUNCTION reject_activated_cohort_material_update();

CREATE INDEX ix_assessment_activities_org_updated
    ON assessment_activities (organization_id, updated_at DESC);

CREATE INDEX ix_assessment_cohorts_org_activity
    ON assessment_cohorts (organization_id, activity_id);
