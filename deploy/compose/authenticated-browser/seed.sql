-- Deterministic Development/Testing IdentityAccess and Assessment fixtures.
-- Synthetic data only. Binds synthetic.administrator to the gateway issuer.

INSERT INTO organizations (id, created_at)
VALUES ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', CLOCK_TIMESTAMP())
ON CONFLICT (id) DO NOTHING;

INSERT INTO actors (id, created_at)
VALUES ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', CLOCK_TIMESTAMP())
ON CONFLICT (id) DO NOTHING;

INSERT INTO human_identity_bindings (
    binding_id, issuer, subject, actor_id, created_at, disabled_at)
VALUES (
    'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
    'http://localhost:18080/realms/flex-agent',
    'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
    CLOCK_TIMESTAMP(),
    NULL)
ON CONFLICT (issuer, subject) DO NOTHING;

INSERT INTO actor_organization_grants (
    organization_id, actor_id, relationship_version, granted_action, created_at)
SELECT
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
    1,
    granted_action,
    CLOCK_TIMESTAMP()
FROM (
    VALUES
        ('assessment.activity.create'),
        ('assessment.activity.read'),
        ('assessment.activity.save'),
        ('assessment.readiness.check'),
        ('assessment.cohort.activate'),
        ('assessment.source.select'),
        ('assessment.activation.reconcile'),
        ('assessment.baseline.read'),
        ('assessment.baseline.provenance.read'),
        ('assessment.enrollment.candidate.read'),
        ('assessment.enrollment.list'),
        ('assessment.enrollment.read'),
        ('assessment.enrollment.assign'),
        ('assessment.enrollment.suspend'),
        ('assessment.enrollment.restore'),
        ('assessment.enrollment.close'),
        ('assessment.enrollment.revoke')
) AS grants(granted_action)
ON CONFLICT (organization_id, actor_id, granted_action) DO NOTHING;

-- synthetic.participant actor, display profile, and discovery grants.
INSERT INTO actors (id, created_at)
VALUES ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab', CLOCK_TIMESTAMP())
ON CONFLICT (id) DO NOTHING;

INSERT INTO human_identity_bindings (
    binding_id, issuer, subject, actor_id, created_at, disabled_at)
VALUES (
    'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbc',
    'http://localhost:18080/realms/flex-agent',
    'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab',
    CLOCK_TIMESTAMP(),
    NULL)
ON CONFLICT (issuer, subject) DO NOTHING;

INSERT INTO actor_organization_grants (
    organization_id, actor_id, relationship_version, granted_action, created_at)
SELECT
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab',
    1,
    granted_action,
    CLOCK_TIMESTAMP()
FROM (
    VALUES
        ('assessment.assignment.discover'),
        ('assessment.enrollment.receive')
) AS grants(granted_action)
ON CONFLICT (organization_id, actor_id, granted_action) DO NOTHING;

INSERT INTO identity_human_display_profiles (
    organization_id, actor_id, display_label, created_at, updated_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab',
    'Synthetic Participant',
    CLOCK_TIMESTAMP(),
    CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, actor_id) DO NOTHING;

INSERT INTO configuration_sources (id, organization_id, source_kind, created_at)
VALUES
    ('22222222-2222-2222-2222-222222222201', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.organization_policy.v1', CLOCK_TIMESTAMP()),
    ('22222222-2222-2222-2222-222222222202', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.agent_revision.v1', CLOCK_TIMESTAMP()),
    ('22222222-2222-2222-2222-222222222203', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.harness_revision.v1', CLOCK_TIMESTAMP()),
    ('22222222-2222-2222-2222-222222222204', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.workflow_policy.v1', CLOCK_TIMESTAMP()),
    ('22222222-2222-2222-2222-222222222205', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.adaptive_follow_up.v1', CLOCK_TIMESTAMP()),
    ('22222222-2222-2222-2222-222222222206', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.rubric_evaluation.v1', CLOCK_TIMESTAMP()),
    ('22222222-2222-2222-2222-222222222207', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.model_deployment.v1', CLOCK_TIMESTAMP()),
    ('22222222-2222-2222-2222-222222222208', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.knowledge_reference.v1', CLOCK_TIMESTAMP()),
    ('22222222-2222-2222-2222-222222222209', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.task_requirement.v1', CLOCK_TIMESTAMP()),
    ('22222222-2222-2222-2222-222222222210', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.capability_profile.v1', CLOCK_TIMESTAMP()),
    ('22222222-2222-2222-2222-222222222211', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'assessment.review_release.v1', CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, id) DO NOTHING;

INSERT INTO configuration_source_versions (
    id, organization_id, configuration_source_id, schema_version, procedure_id,
    content_digest, idempotency_key, created_at)
VALUES
    ('33333333-3333-3333-3333-333333333301', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222201', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('b', 64), '33333333-3333-3333-3333-333333333301', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333302', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222202', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('c', 64), '33333333-3333-3333-3333-333333333302', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333303', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222203', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('d', 64), '33333333-3333-3333-3333-333333333303', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333304', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222204', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('e', 64), '33333333-3333-3333-3333-333333333304', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333305', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222205', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('f', 64), '33333333-3333-3333-3333-333333333305', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333306', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222206', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('g', 64), '33333333-3333-3333-3333-333333333306', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333307', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222207', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('h', 64), '33333333-3333-3333-3333-333333333307', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333308', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222208', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('i', 64), '33333333-3333-3333-3333-333333333308', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333309', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222209', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('j', 64), '33333333-3333-3333-3333-333333333309', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333310', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222210', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('k', 64), '33333333-3333-3333-3333-333333333310', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333311', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222211', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('l', 64), '33333333-3333-3333-3333-333333333311', CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, id) DO NOTHING;

INSERT INTO configuration_source_readiness_descriptors (
    organization_id, configuration_source_id, version_id, source_kind, category,
    lifecycle_state, compatibility_key, capability_text_enabled, capability_voice_enabled,
    capability_tools_enabled, capability_dynamic_memory_writes_enabled,
    capability_shared_session_enabled, capability_direct_deployment_enabled,
    production_eligible, transactionally_revalidatable, effective_values, created_at)
VALUES
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222201', '33333333-3333-3333-3333-333333333301', 'assessment.organization_policy.v1', 'organization_policy', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333301"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222202', '33333333-3333-3333-3333-333333333302', 'assessment.agent_revision.v1', 'agent', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333302"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222203', '33333333-3333-3333-3333-333333333303', 'assessment.harness_revision.v1', 'harness', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333303"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222204', '33333333-3333-3333-3333-333333333304', 'assessment.workflow_policy.v1', 'workflow', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333304"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222205', '33333333-3333-3333-3333-333333333305', 'assessment.adaptive_follow_up.v1', 'adaptive_follow_up', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333305"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222206', '33333333-3333-3333-3333-333333333306', 'assessment.rubric_evaluation.v1', 'rubric_evaluation', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333306"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222207', '33333333-3333-3333-3333-333333333307', 'assessment.model_deployment.v1', 'model_deployment', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333307"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222208', '33333333-3333-3333-3333-333333333308', 'assessment.knowledge_reference.v1', 'knowledge', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333308"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222209', '33333333-3333-3333-3333-333333333309', 'assessment.task_requirement.v1', 'task_submission', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333309"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222210', '33333333-3333-3333-3333-333333333310', 'assessment.capability_profile.v1', 'capability', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333310"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222211', '33333333-3333-3333-3333-333333333311', 'assessment.review_release.v1', 'review_release', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333311"}'::jsonb, CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, version_id) DO NOTHING;
