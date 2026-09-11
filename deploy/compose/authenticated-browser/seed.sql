-- Deterministic Development/Testing IdentityAccess and Assessment fixtures.
-- Synthetic data only. Binds demo.admin to the gateway issuer.

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
        ('assessment.enrollment.revoke'),
        ('assessment.enrollment.accommodation.read'),
        ('assessment.enrollment.accommodation.grant'),
        ('assessment.enrollment.accommodation.decide'),
        ('assessment.enrollment.accommodation.revoke'),
        ('session.events.subscribe'),
        ('session.snapshot.read'),
        ('session.operations.read'),
        ('session.transcript.read'),
        ('session.pause'),
        ('session.resume'),
        ('session.terminate')
) AS grants(granted_action)
ON CONFLICT (organization_id, actor_id, granted_action) DO NOTHING;

-- demo.participant actor, display profile, and discovery grants.
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
        ('assessment.enrollment.receive'),
        ('session.events.subscribe'),
        ('session.snapshot.read'),
        ('session.message.send'),
        ('session.complete'),
        ('session.reconcile'),
        ('session.transcript.read')
) AS grants(granted_action)
ON CONFLICT (organization_id, actor_id, granted_action) DO NOTHING;

INSERT INTO identity_human_display_profiles (
    organization_id, actor_id, display_label, created_at, updated_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab',
    'Demo Participant',
    CLOCK_TIMESTAMP(),
    CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, actor_id) DO UPDATE
SET display_label = EXCLUDED.display_label,
    updated_at = EXCLUDED.updated_at;

-- Numbered extras: demo.admin1–5 and demo.participant1–30 (Keycloak ids d2000000 / e2000000).
INSERT INTO actors (id, created_at)
SELECT format('a2000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid, CLOCK_TIMESTAMP()
FROM generate_series(1, 5) AS gs(i)
ON CONFLICT (id) DO NOTHING;

INSERT INTO human_identity_bindings (
    binding_id, issuer, subject, actor_id, created_at, disabled_at)
SELECT
    format('b2000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid,
    'http://localhost:18080/realms/flex-agent',
    format('d2000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0')),
    format('a2000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid,
    CLOCK_TIMESTAMP(),
    NULL
FROM generate_series(1, 5) AS gs(i)
ON CONFLICT (issuer, subject) DO NOTHING;

INSERT INTO actor_organization_grants (
    organization_id, actor_id, relationship_version, granted_action, created_at)
SELECT
    grants.organization_id,
    format('a2000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid,
    1,
    grants.granted_action,
    CLOCK_TIMESTAMP()
FROM actor_organization_grants AS grants
CROSS JOIN generate_series(1, 5) AS gs(i)
WHERE grants.organization_id = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc'
    AND grants.actor_id = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'
ON CONFLICT (organization_id, actor_id, granted_action) DO NOTHING;

INSERT INTO actors (id, created_at)
SELECT format('a3000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid, CLOCK_TIMESTAMP()
FROM generate_series(1, 30) AS gs(i)
ON CONFLICT (id) DO NOTHING;

INSERT INTO human_identity_bindings (
    binding_id, issuer, subject, actor_id, created_at, disabled_at)
SELECT
    format('b3000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid,
    'http://localhost:18080/realms/flex-agent',
    format('e2000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0')),
    format('a3000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid,
    CLOCK_TIMESTAMP(),
    NULL
FROM generate_series(1, 30) AS gs(i)
ON CONFLICT (issuer, subject) DO NOTHING;

INSERT INTO actor_organization_grants (
    organization_id, actor_id, relationship_version, granted_action, created_at)
SELECT
    grants.organization_id,
    format('a3000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid,
    1,
    grants.granted_action,
    CLOCK_TIMESTAMP()
FROM actor_organization_grants AS grants
CROSS JOIN generate_series(1, 30) AS gs(i)
WHERE grants.organization_id = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc'
    AND grants.actor_id = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab'
ON CONFLICT (organization_id, actor_id, granted_action) DO NOTHING;

INSERT INTO identity_human_display_profiles (
    organization_id, actor_id, display_label, created_at, updated_at)
SELECT
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    format('a3000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid,
    format('Demo Participant %s', gs.i),
    CLOCK_TIMESTAMP(),
    CLOCK_TIMESTAMP()
FROM generate_series(1, 30) AS gs(i)
ON CONFLICT (organization_id, actor_id) DO UPDATE
SET display_label = EXCLUDED.display_label,
    updated_at = EXCLUDED.updated_at;

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
    ('33333333-3333-3333-3333-333333333316', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222206', 'v1', 'activation-baseline-jcs-sha256-v1', '36d492272fdf8baa1a9d0a45d10cfd1aff5dcea2ee4381eed4c411dcb01d6a9a', '33333333-3333-3333-3333-333333333316', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333307', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222207', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('h', 64), '33333333-3333-3333-3333-333333333307', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333308', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222208', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('i', 64), '33333333-3333-3333-3333-333333333308', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333309', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222209', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('j', 64), '33333333-3333-3333-3333-333333333309', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333310', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222210', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('k', 64), '33333333-3333-3333-3333-333333333310', CLOCK_TIMESTAMP()),
    ('33333333-3333-3333-3333-333333333311', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222211', 'v1', 'activation-baseline-jcs-sha256-v1', repeat('l', 64), '33333333-3333-3333-3333-333333333311', CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, id) DO NOTHING;

-- Verifiable empty notice receipts for the frozen workflow/policy source versions.
-- Start fails closed without these; they are not participant-visible notice content.
INSERT INTO configuration_participant_notice_projection_sets (
    organization_id, source_id, source_version_id, source_content_digest, notice_count, created_at)
VALUES
    (
        'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
        '22222222-2222-2222-2222-222222222201',
        '33333333-3333-3333-3333-333333333301',
        repeat('b', 64),
        0,
        CLOCK_TIMESTAMP()),
    (
        'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
        '22222222-2222-2222-2222-222222222204',
        '33333333-3333-3333-3333-333333333304',
        repeat('e', 64),
        0,
        CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, source_version_id) DO NOTHING;


INSERT INTO configuration_source_payloads (
    organization_id, configuration_source_id, source_version_id, content_digest, canonical_utf8, created_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    '22222222-2222-2222-2222-222222222206',
    '33333333-3333-3333-3333-333333333316',
    '36d492272fdf8baa1a9d0a45d10cfd1aff5dcea2ee4381eed4c411dcb01d6a9a',
    decode('7b226167677265676174696f6e223a7b226167677265676174696f6e5f6b696e64223a22616c6c5f72657175697265645f736174697366696564227d2c22636f6e666c6963745f6265686176696f72223a227265766965775f72657175697265645f70726573657276655f64657465726d696e69737469635f66616374222c226372697465726961223a5b7b22636f6e666964656e63655f6669656c64223a7b226669656c645f6b696e64223a227175616c69746174697665222c227065726d69747465645f76616c756573223a5b226c6f77222c226d656469756d222c2268696768225d7d2c22637269746572696f6e5f6964223a22637269742e6f626a6563746976652e776f72642d636f756e74222c22637269746572696f6e5f76657273696f6e223a22637269742e6f626a6563746976652e776f72642d636f756e742e7631222c2264657465726d696e69737469635f6576616c7561746f72223a7b2263616e6f6e6963616c697a6174696f6e5f70726f636564757265223a226a63732d7368613235362d7631222c22636f6e66696775726174696f6e5f646967657374223a2265353636353636633266376534623665666632636239373766306361396465383733653331336234616561643237343033663262666335363862303963653632222c226370755f74696d655f6c696d6974223a2250543553222c22646570656e64656e63795f646967657374223a2233663432656239323535313338306564663737663336396431376661316630623631623564643738306333373938376233346332663031323733656264333236222c22656c61707365645f74696d655f6c696d6974223a225054313053222c226576616c7561746f725f646967657374223a2239626665303966373866396664616264663139323736623463313365633765383833613832313562366564666466613234333436303863643365343530653938222c226576616c7561746f725f6964223a226576616c2e6275696c74696e2e626f756e6465642d63616c63222c226576616c7561746f725f76657273696f6e223a226576616c2e6275696c74696e2e626f756e6465642d63616c632e7631222c2265786563757461626c655f73656c656374696f6e223a2270726f68696269746564222c22696e7075745f736368656d615f6964223a226576616c2e6275696c74696e2e626f756e6465642d63616c632e696e7075742e7631222c226d656d6f72795f6c696d69745f6279746573223a31363737373231362c226e6574776f726b5f656772657373223a2270726f68696269746564222c226f7065726174696f6e223a22626f756e6465645f63616c63756c6174696f6e222c226f75747075745f6c696d69745f6279746573223a343039362c226f75747075745f736368656d615f6964223a226576616c2e6275696c74696e2e626f756e6465642d63616c632e6f75747075742e7631227d2c22646973706c61795f6c6162656c223a22526573706f6e7365206c656e6774682069732077697468696e207468652072657175697265642072616e6765222c226576616c7561746f725f6d6f6465223a2264657465726d696e6973746963222c2265766964656e63655f726571756972656d656e7473223a7b226d6178696d756d5f6974656d73223a342c226d696e696d756d5f6974656d73223a312c227065726d69747465645f736f757263655f7479706573223a5b227375626d697373696f6e2e6469726563745f74657874222c2264657465726d696e69737469632e66616374225d2c2277686f6c655f6974656d5f66616c6c6261636b5f7065726d6974746564223a747275657d2c227065726d69747465645f7374617475736573223a5b22736174697366696564222c226e6f745f736174697366696564222c22696e73756666696369656e745f65766964656e6365225d2c2270726f766973696f6e616c5f666565646261636b5f7065726d6974746564223a66616c73652c22726174696f6e616c655f6d61785f756e69636f64655f7363616c617273223a3530302c2273636f72655f6669656c64223a7b226669656c645f6b696e64223a226e6f6e65227d2c22756e6365727461696e74795f63617465676f72696573223a5b22736f757263655f676170222c226576616c7561746f725f626f756e64225d7d2c7b226167656e745f696f223a7b22696e7075745f736368656d615f6964223a226576616c2e6167656e742e61737369737465642e696e7075742e7631222c226d61785f636f6e746578745f756e69636f64655f7363616c617273223a383030302c226f75747075745f736368656d615f6964223a226576616c2e6167656e742e61737369737465642e6f75747075742e7631227d2c22636f6e666964656e63655f6669656c64223a7b226669656c645f6b696e64223a227175616c69746174697665222c227065726d69747465645f76616c756573223a5b226c6f77222c226d656469756d222c2268696768225d7d2c22637269746572696f6e5f6964223a22637269742e61737369737465642e737472756374757265222c22637269746572696f6e5f76657273696f6e223a22637269742e61737369737465642e7374727563747572652e7631222c2264657465726d696e69737469635f6576616c7561746f72223a7b2263616e6f6e6963616c697a6174696f6e5f70726f636564757265223a226a63732d7368613235362d7631222c22636f6e66696775726174696f6e5f646967657374223a2237373330623938663466313066376461373336313530643939313432663636656661333963623133323532313466376265366366616539663736386663343866222c226370755f74696d655f6c696d6974223a2250543553222c22646570656e64656e63795f646967657374223a2235376633626264613661353163316534346336353166613762653637306631663061333463623164306539376362383731323239313538613436366139393665222c22656c61707365645f74696d655f6c696d6974223a225054313053222c226576616c7561746f725f646967657374223a2236623935653639316438626531643464326630313064353039663035363762316530393164353961623862663836386432383166303731363137356634663231222c226576616c7561746f725f6964223a226576616c2e6275696c74696e2e736368656d612d76616c6964617465222c226576616c7561746f725f76657273696f6e223a226576616c2e6275696c74696e2e736368656d612d76616c69646174652e7631222c2265786563757461626c655f73656c656374696f6e223a2270726f68696269746564222c22696e7075745f736368656d615f6964223a226576616c2e6275696c74696e2e736368656d612d76616c69646174652e696e7075742e7631222c226d656d6f72795f6c696d69745f6279746573223a31363737373231362c226e6574776f726b5f656772657373223a2270726f68696269746564222c226f7065726174696f6e223a22736368656d615f76616c6964617465222c226f75747075745f6c696d69745f6279746573223a343039362c226f75747075745f736368656d615f6964223a226576616c2e6275696c74696e2e736368656d612d76616c69646174652e6f75747075742e7631227d2c22646973706c61795f6c6162656c223a2252657175697265642073656374696f6e73206172652070726573656e7420616e6420636f6d706c657465222c226576616c7561746f725f6d6f6465223a226167656e745f6173736973746564222c2265766964656e63655f726571756972656d656e7473223a7b226d6178696d756d5f6974656d73223a382c226d696e696d756d5f6974656d73223a312c227065726d69747465645f736f757263655f7479706573223a5b227375626d697373696f6e2e6469726563745f74657874222c2264657465726d696e69737469632e66616374225d2c2277686f6c655f6974656d5f66616c6c6261636b5f7065726d6974746564223a747275657d2c227065726d69747465645f7374617475736573223a5b22736174697366696564222c226e6f745f736174697366696564222c22696e73756666696369656e745f65766964656e6365222c22636f6e666c696374225d2c2270726f766973696f6e616c5f666565646261636b5f7065726d6974746564223a747275652c22726174696f6e616c655f6d61785f756e69636f64655f7363616c617273223a313530302c2273636f72655f6669656c64223a7b226669656c645f6b696e64223a22656e756d6572617465645f6465636973696f6e222c227065726d69747465645f76616c756573223a5b2270617373222c226661696c225d7d2c22756e6365727461696e74795f63617465676f72696573223a5b22616d626967756f75735f6c616e6775616765222c22736f757263655f676170225d7d2c7b226167656e745f696f223a7b22696e7075745f736368656d615f6964223a226576616c2e6167656e742e6a7564676d656e742e696e7075742e7631222c226d61785f636f6e746578745f756e69636f64655f7363616c617273223a31323030302c226f75747075745f736368656d615f6964223a226576616c2e6167656e742e6a7564676d656e742e6f75747075742e7631227d2c22636f6e666964656e63655f6669656c64223a7b226669656c645f6b696e64223a227175616c69746174697665222c227065726d69747465645f76616c756573223a5b226c6f77222c226d656469756d222c2268696768225d7d2c22637269746572696f6e5f6964223a22637269742e6a7564676d656e742e7175616c697479222c22637269746572696f6e5f76657273696f6e223a22637269742e6a7564676d656e742e7175616c6974792e7631222c22646973706c61795f6c6162656c223a224578706c616e6174696f6e207175616c697479206d6174636865732074686520727562726963222c226576616c7561746f725f6d6f6465223a226167656e745f6a7564676d656e74222c2265766964656e63655f726571756972656d656e7473223a7b226d6178696d756d5f6974656d73223a382c226d696e696d756d5f6974656d73223a312c227065726d69747465645f736f757263655f7479706573223a5b227375626d697373696f6e2e6469726563745f74657874222c2273657373696f6e2e7472616e7363726970745f6974656d225d2c2277686f6c655f6974656d5f66616c6c6261636b5f7065726d6974746564223a747275657d2c227065726d69747465645f7374617475736573223a5b22736174697366696564222c226e6f745f736174697366696564222c22696e73756666696369656e745f65766964656e6365222c226e6f745f6170706c696361626c65225d2c2270726f766973696f6e616c5f666565646261636b5f7065726d6974746564223a747275652c22726174696f6e616c655f6d61785f756e69636f64655f7363616c617273223a323030302c2273636f72655f6669656c64223a7b226669656c645f6b696e64223a22696e74656765725f72616e6765222c226d6178696d756d223a342c226d696e696d756d223a307d2c22756e6365727461696e74795f63617465676f72696573223a5b22616d626967756f75735f6c616e6775616765222c226c696d697465645f636f6e74657874225d7d5d2c22696e73756666696369656e63795f6265686176696f72223a22636f6d706c6574655f696e73756666696369656e745f616e645f626c6f636b5f6167677265676174696f6e222c226c6966656379636c655f706f6c6963795f726566223a226c6966656379636c652e61637469766974792d636c6f737572652d333635642e7631222c226e6f745f6170706c696361626c655f6265686176696f72223a226578636c7564655f66726f6d5f6167677265676174696f6e222c2270726f6365647572655f6964223a226576616c70726f632e70302e746578742e73796e746865746963222c2270726f6365647572655f736368656d61223a226576616c756174696f6e2d70726f6365647572652e7631222c2270726f6365647572655f76657273696f6e223a226576616c70726f632e70302e746578742e73796e7468657469632e7631222c227265706c6163656d656e745f706f6c6963795f726566223a226576616c2e7265706c6163656d656e742e617574686f72697a65642d7072656465636573736f722e7631222c227265736f757263655f626f756e6473223a7b22656c61707365645f74696d656f7574223a225054324d222c226d61785f65766964656e63655f6974656d73223a36342c226d61785f70726f76696465725f6f75747075745f6279746573223a33323736387d2c2272657472795f626f756e6473223a7b22617474656d70745f74696d656f7574223a225054324d222c226261636b6f6666223a225054333053222c226d61785f617474656d707473223a337d2c227265766965775f706f6c6963795f726566223a227265766965772e706f6c6963792e61737369676e65642d696e7370656374696f6e2e7631227d', 'hex'),
    CLOCK_TIMESTAMP())
ON CONFLICT DO NOTHING;

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
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222206', '33333333-3333-3333-3333-333333333316', 'assessment.rubric_evaluation.v1', 'rubric_evaluation', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333316"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222207', '33333333-3333-3333-3333-333333333307', 'assessment.model_deployment.v1', 'model_deployment', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333307"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222208', '33333333-3333-3333-3333-333333333308', 'assessment.knowledge_reference.v1', 'knowledge', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333308"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222209', '33333333-3333-3333-3333-333333333309', 'assessment.task_requirement.v1', 'task_submission', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333309"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222210', '33333333-3333-3333-3333-333333333310', 'assessment.capability_profile.v1', 'capability', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333310"}'::jsonb, CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', '22222222-2222-2222-2222-222222222211', '33333333-3333-3333-3333-333333333311', 'assessment.review_release.v1', 'review_release', 'available', 'p0-text', TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, '{"ref":"33333333-3333-3333-3333-333333333311"}'::jsonb, CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, version_id) DO NOTHING;

-- Fail-closed identities: exact binding with zero Organizations, and an exact
-- binding with two eligible Organizations. Unbound Keycloak users have no rows.

INSERT INTO actors (id, created_at)
VALUES
    ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaac', CLOCK_TIMESTAMP()),
    ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaad', CLOCK_TIMESTAMP())
ON CONFLICT (id) DO NOTHING;

INSERT INTO human_identity_bindings (
    binding_id, issuer, subject, actor_id, created_at, disabled_at)
VALUES (
    'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbd',
    'http://localhost:18080/realms/flex-agent',
    '11111111-1111-4111-8111-111111111111',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaac',
    CLOCK_TIMESTAMP(),
    NULL),
(
    'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbe',
    'http://localhost:18080/realms/flex-agent',
    '22222222-2222-4222-8222-222222222222',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaad',
    CLOCK_TIMESTAMP(),
    NULL)
ON CONFLICT (issuer, subject) DO NOTHING;

INSERT INTO organizations (id, created_at)
VALUES ('cccccccc-cccc-4ccc-8ccc-cccccccccccd', CLOCK_TIMESTAMP())
ON CONFLICT (id) DO NOTHING;

INSERT INTO actor_organization_grants (
    organization_id, actor_id, relationship_version, granted_action, created_at)
VALUES
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaad', 1, 'assessment.activity.read', CLOCK_TIMESTAMP()),
    ('cccccccc-cccc-4ccc-8ccc-cccccccccccd', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaad', 1, 'assessment.activity.read', CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, actor_id, granted_action) DO NOTHING;

-- Synthetic Worker service principal for fail-closed Invocation processing.
INSERT INTO actors (id, created_at)
VALUES
    ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaae', CLOCK_TIMESTAMP()),
    ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaf', CLOCK_TIMESTAMP())
ON CONFLICT (id) DO NOTHING;

INSERT INTO actor_organization_grants (
    organization_id, actor_id, relationship_version, granted_action, created_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaf',
    1,
    'service_delegation.issue',
    CLOCK_TIMESTAMP())
ON CONFLICT (organization_id, actor_id, granted_action) DO NOTHING;

INSERT INTO service_delegations (
    delegation_id, organization_id, activity_id, participant_id, attempt_id, session_id,
    service_actor_id, allowed_action, system_purpose, initiating_authority,
    effective_at, expires_at, revoked_at, delegation_version, created_at)
SELECT
    gen_random_uuid(),
    runtimes.organization_id,
    runtimes.activity_id,
    runtimes.participant_id,
    runtimes.attempt_id,
    runtimes.session_id,
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaae',
    'session.invocation.execute',
    'session.invocation.worker',
    'system.session_runtime',
    CLOCK_TIMESTAMP() - INTERVAL '1 minute',
    CLOCK_TIMESTAMP() + INTERVAL '12 hours',
    NULL,
    1,
    CLOCK_TIMESTAMP()
FROM session_runtimes AS runtimes
WHERE runtimes.organization_id = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc'
  AND runtimes.invocation_execute_delegation_id IS NULL
  AND NOT EXISTS (
        SELECT 1
        FROM service_delegations AS existing
        WHERE existing.organization_id = runtimes.organization_id
          AND existing.session_id = runtimes.session_id
          AND existing.allowed_action = 'session.invocation.execute'
          AND existing.revoked_at IS NULL);

UPDATE session_runtimes AS runtimes
SET invocation_execute_delegation_id = issued.delegation_id
FROM service_delegations AS issued
WHERE runtimes.organization_id = issued.organization_id
  AND runtimes.session_id = issued.session_id
  AND issued.allowed_action = 'session.invocation.execute'
  AND issued.revoked_at IS NULL
  AND issued.service_actor_id = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaae'
  AND runtimes.invocation_execute_delegation_id IS NULL;

UPDATE session_durable_work AS work
SET invocation_execute_delegation_id = runtimes.invocation_execute_delegation_id
FROM session_runtimes AS runtimes
WHERE work.organization_id = runtimes.organization_id
  AND work.session_id = runtimes.session_id
  AND work.invocation_execute_delegation_id IS NULL
  AND runtimes.invocation_execute_delegation_id IS NOT NULL;
