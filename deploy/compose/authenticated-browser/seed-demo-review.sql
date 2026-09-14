-- Deterministic Development/Testing assigned-review inspection fixtures.
-- Synthetic data only. Requires identity seed.sql and demo-work seed.sql.

BEGIN;

INSERT INTO actors (id, created_at)
VALUES ('f2100000-0000-4000-8000-00000000000d', CLOCK_TIMESTAMP())
ON CONFLICT (id) DO NOTHING;

INSERT INTO session_resolved_configurations (
    organization_id, configuration_id, configuration_digest, canonical_json, created_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-000000000003',
    'e5f9898c1fdfa4b20a792818ddb34ea20b2756e980c6bd48160dcede3be05b89',
    '{"sources":[{"source_key":"rubric_evaluation","source_id":"22222222-2222-2222-2222-222222222206","source_version_id":"33333333-3333-3333-3333-333333333316","content_digest":"cb9db3a1cf6f96961cf8506ec3196edd2b9ce3d0dcf7cbfc4334ecaf624860de"}],"permitted_submissions":[{"protected_ref":"submission.version.f2100000000040008000000000000007","content_digest":"eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"}]}',
    CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, configuration_id) DO NOTHING;

INSERT INTO session_initial_manifests (
    organization_id, manifest_id, configuration_id, manifest_digest, canonical_json, created_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-000000000004',
    'f2100000-0000-4000-8000-000000000003',
    '5d3e95b981c053f6104ead70a95e67c76c727a2f3ad02fe4b8b65fe4518e5ef9',
    '{"manifest_id":"f2100000-0000-4000-8000-000000000004","configuration_id":"f2100000-0000-4000-8000-000000000003","configuration_digest":"e5f9898c1fdfa4b20a792818ddb34ea20b2756e980c6bd48160dcede3be05b89","provenance":[{"source_key":"rubric_evaluation","source_id":"22222222-2222-2222-2222-222222222206","source_version_id":"33333333-3333-3333-3333-333333333316","content_digest":"cb9db3a1cf6f96961cf8506ec3196edd2b9ce3d0dcf7cbfc4334ecaf624860de"}]}',
    CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, manifest_id) DO NOTHING;

INSERT INTO submissions_submissions (
    organization_id, submission_id, activity_id, cohort_id, baseline_id,
    enrollment_id, participant_actor_id, task_source_id, task_version_id,
    task_content_digest, created_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-000000000006',
    'a1000000-0000-4000-8000-000000000025',
    'c1000000-0000-4000-8000-000000000025',
    'd1000000-0000-4000-8000-000000000025',
    'e1000000-0000-4000-8000-000000000002',
    'f1000000-0000-4000-8000-000000000001',
    '22222222-2222-2222-2222-222222222209',
    '33333333-3333-3333-3333-333333333309',
    repeat('j', 64),
    CLOCK_TIMESTAMP())
ON CONFLICT DO NOTHING;

INSERT INTO submissions_accepted_versions (
    organization_id, submission_id, version_id, version_number, activity_id,
    cohort_id, baseline_id, enrollment_id, participant_actor_id, task_source_id,
    task_version_id, task_content_digest, policy_digest, predecessor_version_id,
    accepted_at, accepted_by_actor_id)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-000000000006',
    'f2100000-0000-4000-8000-000000000007',
    1,
    'a1000000-0000-4000-8000-000000000025',
    'c1000000-0000-4000-8000-000000000025',
    'd1000000-0000-4000-8000-000000000025',
    'e1000000-0000-4000-8000-000000000002',
    'f1000000-0000-4000-8000-000000000001',
    '22222222-2222-2222-2222-222222222209',
    '33333333-3333-3333-3333-333333333309',
    repeat('j', 64),
    repeat('e', 64),
    NULL,
    CLOCK_TIMESTAMP(),
    'f1000000-0000-4000-8000-000000000001')
ON CONFLICT DO NOTHING;

INSERT INTO submissions_attempts (
    organization_id, attempt_id, activity_id, cohort_id, baseline_id, enrollment_id,
    participant_actor_id, task_source_id, ordinal, entitlement_source, retry_entitlement_id,
    status, consumed, requested_at, started_at, terminal_at, terminal_reason_category,
    session_id, resolved_configuration_id, initial_manifest_id,
    configuration_digest, manifest_digest)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-000000000001',
    'a1000000-0000-4000-8000-000000000025',
    'c1000000-0000-4000-8000-000000000025',
    'd1000000-0000-4000-8000-000000000025',
    'e1000000-0000-4000-8000-000000000002',
    'f1000000-0000-4000-8000-000000000001',
    '22222222-2222-2222-2222-222222222209',
    1,
    'baseline',
    NULL,
    'completed',
    TRUE,
    CLOCK_TIMESTAMP(),
    CLOCK_TIMESTAMP(),
    CLOCK_TIMESTAMP(),
    'completed',
    'f2100000-0000-4000-8000-000000000002',
    'f2100000-0000-4000-8000-000000000003',
    'f2100000-0000-4000-8000-000000000004',
    'e5f9898c1fdfa4b20a792818ddb34ea20b2756e980c6bd48160dcede3be05b89',
    '5d3e95b981c053f6104ead70a95e67c76c727a2f3ad02fe4b8b65fe4518e5ef9')
ON CONFLICT DO NOTHING;

INSERT INTO submissions_attempt_submission_bindings (
    organization_id, attempt_id, version_id, version_number, binding_order, content_digest)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000007',
    1,
    1,
    repeat('e', 64))
ON CONFLICT DO NOTHING;

INSERT INTO session_runtimes (
    organization_id, activity_id, participant_id, attempt_id, session_id,
    configuration_id, configuration_digest, manifest_id, lifecycle_state,
    session_version, session_sequence, cutoff_sequence)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'a1000000-0000-4000-8000-000000000025',
    'f1000000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000002',
    'f2100000-0000-4000-8000-000000000003',
    'e5f9898c1fdfa4b20a792818ddb34ea20b2756e980c6bd48160dcede3be05b89',
    'f2100000-0000-4000-8000-000000000004',
    'completed',
    1,
    42,
    42)
ON CONFLICT DO NOTHING;

INSERT INTO session_frozen_model_deployments (
    organization_id, activity_id, participant_id, attempt_id, session_id,
    profile_id, profile_version, profile_digest, provider_id, credential_mode,
    credential_binding_reference, credential_binding_version)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'a1000000-0000-4000-8000-000000000025',
    'f1000000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000002',
    'mdl.p0.text.synthetic',
    'mdl.p0.text.synthetic.v1',
    repeat('a', 64),
    'provider.synthetic',
    'organization_byok',
    'cred.bind.synthetic',
    'cred.bind.synthetic.v1')
ON CONFLICT DO NOTHING;

INSERT INTO session_terminal_records (
    organization_id, activity_id, participant_id, attempt_id, session_id,
    terminal_record_id, lifecycle_state, cutoff_sequence, reason_category,
    attempt_mapping, procedure_id, seal_digest)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'a1000000-0000-4000-8000-000000000025',
    'f1000000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000002',
    'f2100000-0000-4000-8000-000000000005',
    'completed',
    42,
    'completed',
    'completed',
    'manifest-jcs-sha256-v2',
    repeat('f', 64))
ON CONFLICT DO NOTHING;

INSERT INTO session_evaluation_handoffs (
    organization_id, activity_id, participant_id, attempt_id, session_id,
    handoff_id, terminal_record_id, procedure_id, eligibility, terminal_state,
    cutoff_sequence, configuration_id, configuration_digest, manifest_id, seal_digest)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'a1000000-0000-4000-8000-000000000025',
    'f1000000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000002',
    'handoff.demo-review.001',
    'f2100000-0000-4000-8000-000000000005',
    'manifest-jcs-sha256-v2',
    'eligible',
    'completed',
    42,
    'f2100000-0000-4000-8000-000000000003',
    'e5f9898c1fdfa4b20a792818ddb34ea20b2756e980c6bd48160dcede3be05b89',
    'f2100000-0000-4000-8000-000000000004',
    repeat('f', 64))
ON CONFLICT DO NOTHING;

INSERT INTO service_delegations (
    delegation_id, organization_id, activity_id, participant_id, attempt_id, session_id,
    service_actor_id, allowed_action, system_purpose, initiating_authority,
    effective_at, expires_at, revoked_at, delegation_version)
VALUES (
    'f2100000-0000-4000-8000-000000000008',
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'a1000000-0000-4000-8000-000000000025',
    'f1000000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000002',
    'f2100000-0000-4000-8000-00000000000d',
    'evaluation.execute',
    'evaluation.execution',
    'synthetic.demo-review',
    CLOCK_TIMESTAMP() - INTERVAL '1 hour',
    CLOCK_TIMESTAMP() + INTERVAL '12 hours',
    NULL,
    1)
ON CONFLICT DO NOTHING;

INSERT INTO evaluation_requests (
    organization_id, request_id, activity_id, participant_id, attempt_id,
    session_id, handoff_id, terminal_record_id, handoff_eligibility, handoff_terminal_state,
    cutoff_sequence, request_kind, frozen_input_digest, idempotency_key,
    delegation_id, state, predecessor_evaluation_id, replacement_reason,
    rubric_source_id, rubric_source_version_id, rubric_content_digest,
    submission_source_id, submission_version_id, submission_content_digest,
    configuration_id, configuration_record_id, configuration_digest,
    manifest_id, manifest_record_id, manifest_digest,
    manifest_seal_procedure_id, terminal_seal_digest,
    model_profile_id, model_profile_version, model_profile_digest, provider_id,
    credential_mode, credential_binding_reference, credential_binding_version,
    evaluator_registry_version, lifecycle_policy_ref, correlation_id, completed_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-000000000009',
    'a1000000-0000-4000-8000-000000000025',
    'f1000000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000002',
    'handoff.demo-review.001',
    'f2100000-0000-4000-8000-000000000005',
    'eligible',
    'completed',
    42,
    'initial',
    repeat('1', 64),
    'idem.demo-review.001',
    'f2100000-0000-4000-8000-000000000008',
    'completed',
    NULL,
    NULL,
    '22222222-2222-2222-2222-222222222206',
    '33333333-3333-3333-3333-333333333316',
    'cb9db3a1cf6f96961cf8506ec3196edd2b9ce3d0dcf7cbfc4334ecaf624860de',
    'f2100000-0000-4000-8000-000000000006',
    'f2100000-0000-4000-8000-000000000007',
    repeat('e', 64),
    'f2100000-0000-4000-8000-000000000003',
    'f2100000-0000-4000-8000-000000000003',
    'e5f9898c1fdfa4b20a792818ddb34ea20b2756e980c6bd48160dcede3be05b89',
    'f2100000-0000-4000-8000-000000000004',
    'f2100000-0000-4000-8000-000000000004',
    '5d3e95b981c053f6104ead70a95e67c76c727a2f3ad02fe4b8b65fe4518e5ef9',
    'manifest-jcs-sha256-v2',
    repeat('f', 64),
    'mdl.p0.text.synthetic',
    'mdl.p0.text.synthetic.v1',
    repeat('a', 64),
    'provider.synthetic',
    'organization_byok',
    'cred.bind.synthetic',
    'cred.bind.synthetic.v1',
    'evalreg.p0.v1',
    'lifecycle.activity-closure-365d.v1',
    'f2100000-0000-4000-8000-000000000009',
    CLOCK_TIMESTAMP())
ON CONFLICT DO NOTHING;

INSERT INTO evaluation_invocation_attempts (
    organization_id, request_id, invocation_attempt_id, activity_id, participant_id,
    attempt_id, session_id, attempt_ordinal, state, started_at, finished_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-000000000009',
    'f2100000-0000-4000-8000-00000000000c',
    'a1000000-0000-4000-8000-000000000025',
    'f1000000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000002',
    1,
    'completed',
    CLOCK_TIMESTAMP() - INTERVAL '30 minutes',
    CLOCK_TIMESTAMP() - INTERVAL '20 minutes')
ON CONFLICT DO NOTHING;

INSERT INTO evaluation_evidence_sets (
    organization_id, evaluation_id, evidence_set_id, request_id,
    invocation_attempt_id, seal_schema, seal_digest, sealed_at, sealed_by_service)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-00000000000a',
    'f2100000-0000-4000-8000-00000000000b',
    'f2100000-0000-4000-8000-000000000009',
    'f2100000-0000-4000-8000-00000000000c',
    'evidence-set-jcs-sha256-v1',
    repeat('7', 64),
    CLOCK_TIMESTAMP(),
    'evaluation.demo-review')
ON CONFLICT DO NOTHING;

INSERT INTO evaluations (
    organization_id, evaluation_id, request_id, activity_id, participant_id,
    attempt_id, session_id, evidence_set_id, procedure_source_id,
    procedure_source_version_id, procedure_digest, aggregate_status,
    creation_service_id, completed_at, predecessor_evaluation_id)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-00000000000a',
    'f2100000-0000-4000-8000-000000000009',
    'a1000000-0000-4000-8000-000000000025',
    'f1000000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000002',
    'f2100000-0000-4000-8000-00000000000b',
    '22222222-2222-2222-2222-222222222206',
    '33333333-3333-3333-3333-333333333316',
    'cb9db3a1cf6f96961cf8506ec3196edd2b9ce3d0dcf7cbfc4334ecaf624860de',
    'complete',
    'evaluation.demo-review',
    CLOCK_TIMESTAMP(),
    NULL)
ON CONFLICT DO NOTHING;

INSERT INTO evaluation_criterion_judgments (
    organization_id, evaluation_id, judgment_id, request_id, criterion_id, criterion_version,
    evaluator_mode, status, confidence, uncertainty_json, rationale, score_json, provisional_feedback)
VALUES
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'f2100000-0000-4000-8000-00000000000a',
     'f2100000-0000-4000-8000-000000000010', 'f2100000-0000-4000-8000-000000000009',
     'crit.judgment.quality', 'crit.judgment.quality.v1',
     'deterministic', 'satisfied', 'high', '[]'::jsonb,
     'The response addresses the safety scenario with concrete controls.',
     '1'::jsonb,
     'Keep the explanation specific to observed controls.'),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'f2100000-0000-4000-8000-00000000000a',
     'f2100000-0000-4000-8000-000000000011', 'f2100000-0000-4000-8000-000000000009',
     'crit.submission.presence', 'crit.submission.presence.v1',
     'deterministic', 'satisfied', 'high', '[]'::jsonb,
     'A written submission was bound before session completion.',
     '1'::jsonb,
     'Submission presence is confirmed from the bound attempt.')
ON CONFLICT DO NOTHING;

INSERT INTO evaluation_evidence_items (
    organization_id, evaluation_id, evidence_id, request_id, activity_id, participant_id,
    attempt_id, session_id, source_type, source_id, source_version_id, source_content_digest,
    locator_schema, locator_digest, precision, integrity_state, locator_canonical_json,
    created_by_service, created_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-00000000000a',
    'f2100000-0000-4000-8000-000000000012',
    'f2100000-0000-4000-8000-000000000009',
    'a1000000-0000-4000-8000-000000000025',
    'f1000000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000002',
    'configuration.fact',
    'f2100000-0000-4000-8000-000000000003',
    'c923b9f6-f293-9b5f-a48e-96c053543c32',
    'e5f9898c1fdfa4b20a792818ddb34ea20b2756e980c6bd48160dcede3be05b89',
    'evidence-locator.v1',
    'e18f698cb086a4e70cedf05770c2da9c9826e127ed9809252980a289dff73393',
    'exact_range',
    'verified',
    $locator${
  "locator_schema":"evidence-locator.v1",
  "source_type":"configuration.fact",
  "source_ref":{"source_id":"cfg.f2100000000040008000000000000003","source_version":"rev.e5f9898c1fdfa4b20a792818ddb34ea20b2756e980c6bd48160dcede3be05b89"},
  "ownership_ref":{
    "organization_id":"org.cccccccccccc4ccc8ccccccccccccccc",
    "activity_id":"act.a1000000000040008000000000000025",
    "participant_id":"part.f1000000000040008000000000000001",
    "attempt_id":"att.f2100000000040008000000000000001",
    "session_id":"sess.f2100000000040008000000000000002",
    "evaluation_id":"eval.f210000000004000800000000000000a"
  },
  "location":{"location_type":"json_pointer","json_pointer":"/sources/0/source_key"},
  "precision":"exact_range",
  "integrity":{
    "source_digest":"e5f9898c1fdfa4b20a792818ddb34ea20b2756e980c6bd48160dcede3be05b89",
    "adapter_version":"locator-adapter.v1",
    "verification_state":"verified"
  },
  "created_by":{"service_id":"evaluation.demo-review","invocation_id":"inv.demo-review.001"}
}$locator$::jsonb,
    'evaluation.demo-review',
    CLOCK_TIMESTAMP())
ON CONFLICT DO NOTHING;

INSERT INTO evaluation_criterion_judgment_evidence_refs (
    organization_id, evaluation_id, judgment_id, evidence_id, reference_ordinal)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-00000000000a',
    'f2100000-0000-4000-8000-000000000010',
    'f2100000-0000-4000-8000-000000000012',
    0)
ON CONFLICT DO NOTHING;

INSERT INTO review_cases (
    organization_id, review_case_id, activity_id, participant_id,
    attempt_id, session_id, case_state, candidate_state,
    current_candidate_evaluation_id, created_at, updated_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-00000000000e',
    'a1000000-0000-4000-8000-000000000025',
    'f1000000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000001',
    'f2100000-0000-4000-8000-000000000002',
    'evaluation_available',
    'none',
    'f2100000-0000-4000-8000-00000000000a',
    CLOCK_TIMESTAMP(),
    CLOCK_TIMESTAMP())
ON CONFLICT DO NOTHING;

INSERT INTO review_case_assignments (
    organization_id, assignment_id, review_case_id, reviewer_actor_id,
    assignment_state, content_capability, assigned_at, revoked_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'f2100000-0000-4000-8000-00000000000f',
    'f2100000-0000-4000-8000-00000000000e',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaafb',
    'assigned',
    'review.content.read',
    CLOCK_TIMESTAMP(),
    NULL)
ON CONFLICT DO NOTHING;

COMMIT;
