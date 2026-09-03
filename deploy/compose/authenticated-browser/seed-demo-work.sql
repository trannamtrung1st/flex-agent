-- Deterministic Development/Testing demo-work list fixtures.
-- Synthetic data only. Requires identity seed.sql in the demo organization.

BEGIN;

WITH draft_template AS (
    SELECT $draft${"title":"Demo Campaign 01","organizationPolicy":{"sourceId":"22222222-2222-2222-2222-222222222201","versionId":"33333333-3333-3333-3333-333333333301","contentDigest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"},"agent":{"sourceId":"22222222-2222-2222-2222-222222222202","versionId":"33333333-3333-3333-3333-333333333302","contentDigest":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"},"harness":{"sourceId":"22222222-2222-2222-2222-222222222203","versionId":"33333333-3333-3333-3333-333333333303","contentDigest":"dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"},"task":{"taskId":"44444444-4444-4444-4444-000000000001","title":"Task 01","submissionRequirementSummary":"Submit one written response","requirementSource":{"sourceId":"22222222-2222-2222-2222-222222222209","versionId":"33333333-3333-3333-3333-333333333309","contentDigest":"jjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjj"}},"workflow":{"sourceId":"22222222-2222-2222-2222-222222222204","versionId":"33333333-3333-3333-3333-333333333304","contentDigest":"eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"},"adaptiveFollowUp":{"sourceId":"22222222-2222-2222-2222-222222222205","versionId":"33333333-3333-3333-3333-333333333305","contentDigest":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"},"rubric":{"sourceId":"22222222-2222-2222-2222-222222222206","versionId":"33333333-3333-3333-3333-333333333306","contentDigest":"gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg"},"modelDeployment":{"sourceId":"22222222-2222-2222-2222-222222222207","versionId":"33333333-3333-3333-3333-333333333307","contentDigest":"hhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh"},"knowledge":[{"sourceId":"22222222-2222-2222-2222-222222222208","versionId":"33333333-3333-3333-3333-333333333308","contentDigest":"iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"}],"capabilityProfile":{"sourceId":"22222222-2222-2222-2222-222222222210","versionId":"33333333-3333-3333-3333-333333333310","contentDigest":"kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk"},"reviewRelease":{"sourceId":"22222222-2222-2222-2222-222222222211","versionId":"33333333-3333-3333-3333-333333333311","contentDigest":"llllllllllllllllllllllllllllllllllllllllllllllllllllllllllllllll"},"memory":{"mode":"disabled","snapshot":null},"timing":{"startsAtUtc":"2026-09-01T00:00:00+00:00","endsAtUtc":"2026-09-30T23:59:00+00:00","deadlineUtc":"2026-09-30T17:00:00+00:00","timeZoneId":"UTC","attemptLimit":2,"perAttemptDurationSeconds":3600,"warningApproachingRemainingSeconds":900,"warningImminentRemainingSeconds":300},"requestedCapabilities":{"textEnabled":true,"voiceEnabled":false,"toolsEnabled":false,"dynamicMemoryWritesEnabled":false,"sharedSessionEnabled":false,"directDeploymentEnabled":false,"permittedTools":[]},"approvedException":null}$draft$::jsonb AS content
),
draft_catalog AS (
    SELECT * FROM (
        VALUES
            (1, 'New Hire Policy Acknowledgment', 'Policy attestation response'),
            (2, 'Workplace Safety Refresher', 'Incident scenario write-up'),
            (3, 'Data Handling Certification', 'Classification exercise'),
            (4, 'Customer Escalation Playbook', 'Escalation decision memo'),
            (5, 'Product Launch Readiness', 'Launch checklist reflection'),
            (6, 'Incident Response Drill', 'Runbook gap analysis'),
            (7, 'Accessibility Standards Review', 'Barrier identification task'),
            (8, 'Anti-Harassment Annual Training', 'Workplace conduct scenario'),
            (9, 'Cloud Migration Knowledge Check', 'Cutover readiness summary'),
            (10, 'Field Operations Hazard Brief', 'Hazard walkthrough notes'),
            (11, 'Quality Assurance Sampling', 'Sampling rationale response'),
            (12, 'Remote Work Security Audit', 'Control attestation'),
            (13, 'Sales Methodology Assessment', 'Discovery call critique'),
            (14, 'Supplier Code of Conduct', 'Vendor obligation review'),
            (15, 'Clinical Documentation Standards', 'Charting accuracy case'),
            (16, 'Leadership Communication Module', 'Stakeholder update draft'),
            (17, 'Financial Controls Awareness', 'Control failure analysis'),
            (18, 'Environmental Compliance Survey', 'Permit compliance check'),
            (19, 'Code Review Practices Check', 'Review findings summary'),
            (20, 'Vendor Risk Questionnaire', 'Risk tier justification'),
            (21, 'Diversity and Inclusion Reflection', 'Inclusive practices essay'),
            (22, 'Executive Briefing Simulation', 'Briefing talking points'),
            (23, 'Warehouse Equipment Certification', 'Equipment inspection log'),
            (24, 'Privacy Impact Review', 'Data flow assessment')
    ) AS catalog(i, title, task_title)
),
draft_rows AS (
    SELECT
        catalog.i,
        format('a1000000-0000-4000-8000-%1$s', lpad(catalog.i::text, 12, '0'))::uuid AS activity_id,
        format('b1000000-0000-4000-8000-%1$s', lpad(catalog.i::text, 12, '0'))::uuid AS revision_id,
        format('c1000000-0000-4000-8000-%1$s', lpad(catalog.i::text, 12, '0'))::uuid AS cohort_id,
        catalog.title,
        jsonb_set(
            jsonb_set(
                jsonb_set(
                    draft_template.content,
                    '{title}',
                    to_jsonb(catalog.title),
                    false),
                '{task,title}',
                to_jsonb(catalog.task_title),
                    false),
            '{task,taskId}',
            to_jsonb(format('44444444-4444-4444-4444-%1$s', lpad(catalog.i::text, 12, '0'))),
            false) AS content,
        TIMESTAMPTZ '2026-08-01 00:00:00+00' + ((25 - catalog.i) * INTERVAL '1 hour') AS stamp
    FROM draft_catalog AS catalog
    CROSS JOIN draft_template
)
INSERT INTO assessment_activities (
    organization_id, activity_id, form, configured_type, current_revision_id,
    current_revision_number, has_activated_cohort, created_at, updated_at)
SELECT
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    activity_id,
    'campaign',
    'assessment',
    revision_id,
    1,
    FALSE,
    stamp,
    stamp
FROM draft_rows
ON CONFLICT (organization_id, activity_id) DO NOTHING;


WITH draft_template AS (
    SELECT $draft${"title":"Demo Campaign 01","organizationPolicy":{"sourceId":"22222222-2222-2222-2222-222222222201","versionId":"33333333-3333-3333-3333-333333333301","contentDigest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"},"agent":{"sourceId":"22222222-2222-2222-2222-222222222202","versionId":"33333333-3333-3333-3333-333333333302","contentDigest":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"},"harness":{"sourceId":"22222222-2222-2222-2222-222222222203","versionId":"33333333-3333-3333-3333-333333333303","contentDigest":"dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"},"task":{"taskId":"44444444-4444-4444-4444-000000000001","title":"Task 01","submissionRequirementSummary":"Submit one written response","requirementSource":{"sourceId":"22222222-2222-2222-2222-222222222209","versionId":"33333333-3333-3333-3333-333333333309","contentDigest":"jjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjj"}},"workflow":{"sourceId":"22222222-2222-2222-2222-222222222204","versionId":"33333333-3333-3333-3333-333333333304","contentDigest":"eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"},"adaptiveFollowUp":{"sourceId":"22222222-2222-2222-2222-222222222205","versionId":"33333333-3333-3333-3333-333333333305","contentDigest":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"},"rubric":{"sourceId":"22222222-2222-2222-2222-222222222206","versionId":"33333333-3333-3333-3333-333333333306","contentDigest":"gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg"},"modelDeployment":{"sourceId":"22222222-2222-2222-2222-222222222207","versionId":"33333333-3333-3333-3333-333333333307","contentDigest":"hhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh"},"knowledge":[{"sourceId":"22222222-2222-2222-2222-222222222208","versionId":"33333333-3333-3333-3333-333333333308","contentDigest":"iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"}],"capabilityProfile":{"sourceId":"22222222-2222-2222-2222-222222222210","versionId":"33333333-3333-3333-3333-333333333310","contentDigest":"kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk"},"reviewRelease":{"sourceId":"22222222-2222-2222-2222-222222222211","versionId":"33333333-3333-3333-3333-333333333311","contentDigest":"llllllllllllllllllllllllllllllllllllllllllllllllllllllllllllllll"},"memory":{"mode":"disabled","snapshot":null},"timing":{"startsAtUtc":"2026-09-01T00:00:00+00:00","endsAtUtc":"2026-09-30T23:59:00+00:00","deadlineUtc":"2026-09-30T17:00:00+00:00","timeZoneId":"UTC","attemptLimit":2,"perAttemptDurationSeconds":3600,"warningApproachingRemainingSeconds":900,"warningImminentRemainingSeconds":300},"requestedCapabilities":{"textEnabled":true,"voiceEnabled":false,"toolsEnabled":false,"dynamicMemoryWritesEnabled":false,"sharedSessionEnabled":false,"directDeploymentEnabled":false,"permittedTools":[]},"approvedException":null}$draft$::jsonb AS content
),
draft_catalog AS (
    SELECT * FROM (
        VALUES
            (1, 'New Hire Policy Acknowledgment', 'Policy attestation response'),
            (2, 'Workplace Safety Refresher', 'Incident scenario write-up'),
            (3, 'Data Handling Certification', 'Classification exercise'),
            (4, 'Customer Escalation Playbook', 'Escalation decision memo'),
            (5, 'Product Launch Readiness', 'Launch checklist reflection'),
            (6, 'Incident Response Drill', 'Runbook gap analysis'),
            (7, 'Accessibility Standards Review', 'Barrier identification task'),
            (8, 'Anti-Harassment Annual Training', 'Workplace conduct scenario'),
            (9, 'Cloud Migration Knowledge Check', 'Cutover readiness summary'),
            (10, 'Field Operations Hazard Brief', 'Hazard walkthrough notes'),
            (11, 'Quality Assurance Sampling', 'Sampling rationale response'),
            (12, 'Remote Work Security Audit', 'Control attestation'),
            (13, 'Sales Methodology Assessment', 'Discovery call critique'),
            (14, 'Supplier Code of Conduct', 'Vendor obligation review'),
            (15, 'Clinical Documentation Standards', 'Charting accuracy case'),
            (16, 'Leadership Communication Module', 'Stakeholder update draft'),
            (17, 'Financial Controls Awareness', 'Control failure analysis'),
            (18, 'Environmental Compliance Survey', 'Permit compliance check'),
            (19, 'Code Review Practices Check', 'Review findings summary'),
            (20, 'Vendor Risk Questionnaire', 'Risk tier justification'),
            (21, 'Diversity and Inclusion Reflection', 'Inclusive practices essay'),
            (22, 'Executive Briefing Simulation', 'Briefing talking points'),
            (23, 'Warehouse Equipment Certification', 'Equipment inspection log'),
            (24, 'Privacy Impact Review', 'Data flow assessment')
    ) AS catalog(i, title, task_title)
),
draft_rows AS (
    SELECT
        catalog.i,
        format('a1000000-0000-4000-8000-%1$s', lpad(catalog.i::text, 12, '0'))::uuid AS activity_id,
        format('b1000000-0000-4000-8000-%1$s', lpad(catalog.i::text, 12, '0'))::uuid AS revision_id,
        format('c1000000-0000-4000-8000-%1$s', lpad(catalog.i::text, 12, '0'))::uuid AS cohort_id,
        catalog.title,
        jsonb_set(
            jsonb_set(
                jsonb_set(
                    draft_template.content,
                    '{title}',
                    to_jsonb(catalog.title),
                    false),
                '{task,title}',
                to_jsonb(catalog.task_title),
                    false),
            '{task,taskId}',
            to_jsonb(format('44444444-4444-4444-4444-%1$s', lpad(catalog.i::text, 12, '0'))),
            false) AS content,
        TIMESTAMPTZ '2026-08-01 00:00:00+00' + ((25 - catalog.i) * INTERVAL '1 hour') AS stamp
    FROM draft_catalog AS catalog
    CROSS JOIN draft_template
)
INSERT INTO assessment_activity_revisions (
    organization_id, activity_id, revision_id, revision_number, title, content,
    created_at, previous_revision_id, actor_id, actor_type, correlation_id,
    change_category, saved_at)
SELECT
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    activity_id,
    revision_id,
    1,
    title,
    content,
    stamp,
    NULL,
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
    'human.interactive',
    activity_id,
    'created',
    stamp
FROM draft_rows
ON CONFLICT (organization_id, revision_id) DO NOTHING;


WITH draft_template AS (
    SELECT $draft${"title":"Demo Campaign 01","organizationPolicy":{"sourceId":"22222222-2222-2222-2222-222222222201","versionId":"33333333-3333-3333-3333-333333333301","contentDigest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"},"agent":{"sourceId":"22222222-2222-2222-2222-222222222202","versionId":"33333333-3333-3333-3333-333333333302","contentDigest":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"},"harness":{"sourceId":"22222222-2222-2222-2222-222222222203","versionId":"33333333-3333-3333-3333-333333333303","contentDigest":"dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"},"task":{"taskId":"44444444-4444-4444-4444-000000000001","title":"Task 01","submissionRequirementSummary":"Submit one written response","requirementSource":{"sourceId":"22222222-2222-2222-2222-222222222209","versionId":"33333333-3333-3333-3333-333333333309","contentDigest":"jjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjj"}},"workflow":{"sourceId":"22222222-2222-2222-2222-222222222204","versionId":"33333333-3333-3333-3333-333333333304","contentDigest":"eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"},"adaptiveFollowUp":{"sourceId":"22222222-2222-2222-2222-222222222205","versionId":"33333333-3333-3333-3333-333333333305","contentDigest":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"},"rubric":{"sourceId":"22222222-2222-2222-2222-222222222206","versionId":"33333333-3333-3333-3333-333333333306","contentDigest":"gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg"},"modelDeployment":{"sourceId":"22222222-2222-2222-2222-222222222207","versionId":"33333333-3333-3333-3333-333333333307","contentDigest":"hhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh"},"knowledge":[{"sourceId":"22222222-2222-2222-2222-222222222208","versionId":"33333333-3333-3333-3333-333333333308","contentDigest":"iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"}],"capabilityProfile":{"sourceId":"22222222-2222-2222-2222-222222222210","versionId":"33333333-3333-3333-3333-333333333310","contentDigest":"kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk"},"reviewRelease":{"sourceId":"22222222-2222-2222-2222-222222222211","versionId":"33333333-3333-3333-3333-333333333311","contentDigest":"llllllllllllllllllllllllllllllllllllllllllllllllllllllllllllllll"},"memory":{"mode":"disabled","snapshot":null},"timing":{"startsAtUtc":"2026-09-01T00:00:00+00:00","endsAtUtc":"2026-09-30T23:59:00+00:00","deadlineUtc":"2026-09-30T17:00:00+00:00","timeZoneId":"UTC","attemptLimit":2,"perAttemptDurationSeconds":3600,"warningApproachingRemainingSeconds":900,"warningImminentRemainingSeconds":300},"requestedCapabilities":{"textEnabled":true,"voiceEnabled":false,"toolsEnabled":false,"dynamicMemoryWritesEnabled":false,"sharedSessionEnabled":false,"directDeploymentEnabled":false,"permittedTools":[]},"approvedException":null}$draft$::jsonb AS content
),
draft_catalog AS (
    SELECT * FROM (
        VALUES
            (1, 'New Hire Policy Acknowledgment', 'Policy attestation response'),
            (2, 'Workplace Safety Refresher', 'Incident scenario write-up'),
            (3, 'Data Handling Certification', 'Classification exercise'),
            (4, 'Customer Escalation Playbook', 'Escalation decision memo'),
            (5, 'Product Launch Readiness', 'Launch checklist reflection'),
            (6, 'Incident Response Drill', 'Runbook gap analysis'),
            (7, 'Accessibility Standards Review', 'Barrier identification task'),
            (8, 'Anti-Harassment Annual Training', 'Workplace conduct scenario'),
            (9, 'Cloud Migration Knowledge Check', 'Cutover readiness summary'),
            (10, 'Field Operations Hazard Brief', 'Hazard walkthrough notes'),
            (11, 'Quality Assurance Sampling', 'Sampling rationale response'),
            (12, 'Remote Work Security Audit', 'Control attestation'),
            (13, 'Sales Methodology Assessment', 'Discovery call critique'),
            (14, 'Supplier Code of Conduct', 'Vendor obligation review'),
            (15, 'Clinical Documentation Standards', 'Charting accuracy case'),
            (16, 'Leadership Communication Module', 'Stakeholder update draft'),
            (17, 'Financial Controls Awareness', 'Control failure analysis'),
            (18, 'Environmental Compliance Survey', 'Permit compliance check'),
            (19, 'Code Review Practices Check', 'Review findings summary'),
            (20, 'Vendor Risk Questionnaire', 'Risk tier justification'),
            (21, 'Diversity and Inclusion Reflection', 'Inclusive practices essay'),
            (22, 'Executive Briefing Simulation', 'Briefing talking points'),
            (23, 'Warehouse Equipment Certification', 'Equipment inspection log'),
            (24, 'Privacy Impact Review', 'Data flow assessment')
    ) AS catalog(i, title, task_title)
),
draft_rows AS (
    SELECT
        catalog.i,
        format('a1000000-0000-4000-8000-%1$s', lpad(catalog.i::text, 12, '0'))::uuid AS activity_id,
        format('b1000000-0000-4000-8000-%1$s', lpad(catalog.i::text, 12, '0'))::uuid AS revision_id,
        format('c1000000-0000-4000-8000-%1$s', lpad(catalog.i::text, 12, '0'))::uuid AS cohort_id,
        catalog.title,
        jsonb_set(
            jsonb_set(
                jsonb_set(
                    draft_template.content,
                    '{title}',
                    to_jsonb(catalog.title),
                    false),
                '{task,title}',
                to_jsonb(catalog.task_title),
                    false),
            '{task,taskId}',
            to_jsonb(format('44444444-4444-4444-4444-%1$s', lpad(catalog.i::text, 12, '0'))),
            false) AS content,
        TIMESTAMPTZ '2026-08-01 00:00:00+00' + ((25 - catalog.i) * INTERVAL '1 hour') AS stamp
    FROM draft_catalog AS catalog
    CROSS JOIN draft_template
)
INSERT INTO assessment_cohorts (
    organization_id, activity_id, cohort_id, state, bound_revision_id,
    bound_revision_number, created_at, updated_at)
SELECT
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    activity_id,
    cohort_id,
    'draft',
    revision_id,
    1,
    stamp,
    stamp
FROM draft_rows
ON CONFLICT (organization_id, cohort_id) DO NOTHING;

INSERT INTO assessment_activities (
    organization_id, activity_id, form, configured_type, current_revision_id,
    current_revision_number, has_activated_cohort, created_at, updated_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'a1000000-0000-4000-8000-000000000025',
    'campaign',
    'assessment',
    'b1000000-0000-4000-8000-000000000025',
    1,
    TRUE,
    TIMESTAMPTZ '2026-08-26 00:00:00+00',
    TIMESTAMPTZ '2026-08-26 00:00:00+00')
ON CONFLICT (organization_id, activity_id) DO NOTHING;

INSERT INTO assessment_activity_revisions (
    organization_id, activity_id, revision_id, revision_number, title, content,
    created_at, previous_revision_id, actor_id, actor_type, correlation_id,
    change_category, saved_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'a1000000-0000-4000-8000-000000000025',
    'b1000000-0000-4000-8000-000000000025',
    1,
    'Q3 Safety Compliance — Pilot Cohort',
    $activated${"title":"Q3 Safety Compliance \u2014 Pilot Cohort","organizationPolicy":{"sourceId":"22222222-2222-2222-2222-222222222201","versionId":"33333333-3333-3333-3333-333333333301","contentDigest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"},"agent":{"sourceId":"22222222-2222-2222-2222-222222222202","versionId":"33333333-3333-3333-3333-333333333302","contentDigest":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"},"harness":{"sourceId":"22222222-2222-2222-2222-222222222203","versionId":"33333333-3333-3333-3333-333333333303","contentDigest":"dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"},"task":{"taskId":"44444444-4444-4444-4444-444444444444","title":"Hazard identification response","submissionRequirementSummary":"Submit one written response","requirementSource":{"sourceId":"22222222-2222-2222-2222-222222222209","versionId":"33333333-3333-3333-3333-333333333309","contentDigest":"jjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjj"}},"workflow":{"sourceId":"22222222-2222-2222-2222-222222222204","versionId":"33333333-3333-3333-3333-333333333304","contentDigest":"eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"},"adaptiveFollowUp":{"sourceId":"22222222-2222-2222-2222-222222222205","versionId":"33333333-3333-3333-3333-333333333305","contentDigest":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"},"rubric":{"sourceId":"22222222-2222-2222-2222-222222222206","versionId":"33333333-3333-3333-3333-333333333306","contentDigest":"gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg"},"modelDeployment":{"sourceId":"22222222-2222-2222-2222-222222222207","versionId":"33333333-3333-3333-3333-333333333307","contentDigest":"hhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh"},"knowledge":[{"sourceId":"22222222-2222-2222-2222-222222222208","versionId":"33333333-3333-3333-3333-333333333308","contentDigest":"iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"}],"capabilityProfile":{"sourceId":"22222222-2222-2222-2222-222222222210","versionId":"33333333-3333-3333-3333-333333333310","contentDigest":"kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk"},"reviewRelease":{"sourceId":"22222222-2222-2222-2222-222222222211","versionId":"33333333-3333-3333-3333-333333333311","contentDigest":"llllllllllllllllllllllllllllllllllllllllllllllllllllllllllllllll"},"memory":{"mode":"disabled","snapshot":null},"timing":{"startsAtUtc":"2026-09-01T00:00:00+00:00","endsAtUtc":"2026-09-30T23:59:00+00:00","deadlineUtc":"2026-09-30T17:00:00+00:00","timeZoneId":"UTC","attemptLimit":2,"perAttemptDurationSeconds":3600,"warningApproachingRemainingSeconds":900,"warningImminentRemainingSeconds":300},"requestedCapabilities":{"textEnabled":true,"voiceEnabled":false,"toolsEnabled":false,"dynamicMemoryWritesEnabled":false,"sharedSessionEnabled":false,"directDeploymentEnabled":false,"permittedTools":[]},"approvedException":null}$activated$::jsonb,
    TIMESTAMPTZ '2026-08-26 00:00:00+00',
    NULL,
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
    'human.interactive',
    'a1000000-0000-4000-8000-000000000025',
    'created',
    TIMESTAMPTZ '2026-08-26 00:00:00+00')
ON CONFLICT (organization_id, revision_id) DO NOTHING;

INSERT INTO assessment_cohorts (
    organization_id, activity_id, cohort_id, state, bound_revision_id,
    bound_revision_number, created_at, updated_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'a1000000-0000-4000-8000-000000000025',
    'c1000000-0000-4000-8000-000000000025',
    'activated',
    'b1000000-0000-4000-8000-000000000025',
    1,
    TIMESTAMPTZ '2026-08-26 00:00:00+00',
    TIMESTAMPTZ '2026-08-26 00:00:00+00')
ON CONFLICT (organization_id, cohort_id) DO NOTHING;

INSERT INTO assessment_activation_baselines (
    organization_id, activity_id, baseline_id, content_digest, procedure_id,
    schema_version, canonicalization_version, document, created_at,
    actor_id, correlation_id)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'a1000000-0000-4000-8000-000000000025',
    'd1000000-0000-4000-8000-000000000025',
    '1406e373f68c6f136b2317c991af05f3b4813c0adb3abc79c70f0dc4ab28be17',
    'activation-baseline-jcs-sha256-v1',
    'v1',
    'rfc8785',
    $baseline${"procedureId":"activation-baseline-jcs-sha256-v1","schemaVersion":"v1","canonicalizationVersion":"rfc8785","fairnessDomains":[{"domainKey":"activity_revision","effectiveValue":{"activity_id":"a1000000-0000-4000-8000-000000000025","revision_id":"b1000000-0000-4000-8000-000000000025","revision_number":"1","title":"Q3 Safety Compliance \u2014 Pilot Cohort"},"classification":"activity_supplied"},{"domainKey":"adaptive_follow_up","effectiveValue":{"ref":"33333333-3333-3333-3333-333333333305"},"classification":"inherited"},{"domainKey":"agent","effectiveValue":{"ref":"33333333-3333-3333-3333-333333333302"},"classification":"inherited"},{"domainKey":"capability","effectiveValue":{"ref":"33333333-3333-3333-3333-333333333310"},"classification":"most_restrictive"},{"domainKey":"harness","effectiveValue":{"ref":"33333333-3333-3333-3333-333333333303"},"classification":"inherited"},{"domainKey":"knowledge","effectiveValue":{"ref":"33333333-3333-3333-3333-333333333308"},"classification":"inherited"},{"domainKey":"memory","effectiveValue":{"mode":"disabled","stable":"true","learning_disabled":"true"},"classification":"derived"},{"domainKey":"model_deployment","effectiveValue":{"ref":"33333333-3333-3333-3333-333333333307"},"classification":"inherited"},{"domainKey":"organization_policy","effectiveValue":{"ref":"33333333-3333-3333-3333-333333333301"},"classification":"inherited"},{"domainKey":"review_release","effectiveValue":{"ref":"33333333-3333-3333-3333-333333333311"},"classification":"inherited"},{"domainKey":"rubric_evaluation","effectiveValue":{"ref":"33333333-3333-3333-3333-333333333306"},"classification":"inherited"},{"domainKey":"task_submission","effectiveValue":{"task_id":"44444444-4444-4444-4444-444444444444","title":"Hazard identification response","requirement_digest":"jjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjj"},"classification":"activity_supplied"},{"domainKey":"timing","effectiveValue":{"starts_at":"2026-09-01T00:00:00.000Z","ends_at":"2026-09-30T23:59:00.000Z","deadline_at":"2026-09-30T17:00:00.000Z","time_zone_id":"UTC","attempt_limit":"2","per_attempt_duration_seconds":"3600","warning_approaching_remaining_seconds":"900","warning_imminent_remaining_seconds":"300"},"classification":"cohort_supplied"},{"domainKey":"workflow","effectiveValue":{"ref":"33333333-3333-3333-3333-333333333304"},"classification":"inherited"}],"sourceReferences":[{"sourceKey":"adaptive_follow_up","sourceId":"22222222-2222-2222-2222-222222222205","sourceVersion":"33333333-3333-3333-3333-333333333305","contentDigest":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"},{"sourceKey":"agent","sourceId":"22222222-2222-2222-2222-222222222202","sourceVersion":"33333333-3333-3333-3333-333333333302","contentDigest":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"},{"sourceKey":"capability","sourceId":"22222222-2222-2222-2222-222222222210","sourceVersion":"33333333-3333-3333-3333-333333333310","contentDigest":"kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk"},{"sourceKey":"harness","sourceId":"22222222-2222-2222-2222-222222222203","sourceVersion":"33333333-3333-3333-3333-333333333303","contentDigest":"dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"},{"sourceKey":"knowledge","sourceId":"22222222-2222-2222-2222-222222222208","sourceVersion":"33333333-3333-3333-3333-333333333308","contentDigest":"iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"},{"sourceKey":"model_deployment","sourceId":"22222222-2222-2222-2222-222222222207","sourceVersion":"33333333-3333-3333-3333-333333333307","contentDigest":"hhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh"},{"sourceKey":"organization_policy","sourceId":"22222222-2222-2222-2222-222222222201","sourceVersion":"33333333-3333-3333-3333-333333333301","contentDigest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"},{"sourceKey":"review_release","sourceId":"22222222-2222-2222-2222-222222222211","sourceVersion":"33333333-3333-3333-3333-333333333311","contentDigest":"llllllllllllllllllllllllllllllllllllllllllllllllllllllllllllllll"},{"sourceKey":"rubric_evaluation","sourceId":"22222222-2222-2222-2222-222222222206","sourceVersion":"33333333-3333-3333-3333-333333333306","contentDigest":"gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg"},{"sourceKey":"task_submission","sourceId":"22222222-2222-2222-2222-222222222209","sourceVersion":"33333333-3333-3333-3333-333333333309","contentDigest":"jjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjj"},{"sourceKey":"workflow","sourceId":"22222222-2222-2222-2222-222222222204","sourceVersion":"33333333-3333-3333-3333-333333333304","contentDigest":"eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"}],"resolutionDecisions":[{"decisionKey":"capability_profile","outcome":"p0_assessment_text"},{"decisionKey":"empty_cohort_permitted","outcome":"true"},{"decisionKey":"exception_path","outcome":"none"},{"decisionKey":"memory_mode","outcome":"disabled"}],"approvedExceptionRefs":[]}$baseline$::jsonb,
    TIMESTAMPTZ '2026-08-26 00:00:00+00',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
    'a1000000-0000-4000-8000-000000000025')
ON CONFLICT (organization_id, baseline_id) DO NOTHING;

INSERT INTO assessment_cohort_baseline_bindings (
    organization_id, activity_id, cohort_id, baseline_id, bound_at)
VALUES (
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    'a1000000-0000-4000-8000-000000000025',
    'c1000000-0000-4000-8000-000000000025',
    'd1000000-0000-4000-8000-000000000025',
    TIMESTAMPTZ '2026-08-26 00:00:00+00')
ON CONFLICT (organization_id, activity_id, cohort_id) DO NOTHING;

INSERT INTO actors (id, created_at)
SELECT format('f1000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid, CLOCK_TIMESTAMP()
FROM generate_series(1, 23) AS gs(i)
ON CONFLICT (id) DO NOTHING;

-- Demo-work roster actors are pre-enrolled only; they must not appear as assignable
-- candidates because they have no Keycloak login. Remove legacy receive grants.
DELETE FROM actor_organization_grants
WHERE organization_id = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc'
    AND granted_action = 'assessment.enrollment.receive'
    AND actor_id::text LIKE 'f1000000-%';

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

WITH participant_catalog AS (
    SELECT * FROM (
        VALUES
            (1, 'Alex Chen — Operations'),
            (2, 'Jordan Lee — Warehouse'),
            (3, 'Sam Patel — Quality assurance'),
            (4, 'Riley Nguyen — Customer support'),
            (5, 'Casey Brooks — Engineering'),
            (6, 'Avery Kim — Facilities'),
            (7, 'Quinn Okafor — Logistics'),
            (8, 'Taylor Reed — HR partner'),
            (9, 'Cameron Diaz — Sales enablement'),
            (10, 'Peyton Shaw — Security operations'),
            (11, 'Reese Alvarez — Clinical staff'),
            (12, 'Morgan Blake — Finance controls'),
            (13, 'Jamie Hart — Product research'),
            (14, 'Skylar Wong — Legal counsel'),
            (15, 'Drew Martinez — Maintenance'),
            (16, 'Emerson Clark — Training lead'),
            (17, 'Hayden Price — Regional manager'),
            (18, 'Logan Singh — Contractor pool'),
            (19, 'Parker Jones — Intern cohort'),
            (20, 'Rowan Foster — Night shift'),
            (21, 'Sydney Monroe — Vendor liaison'),
            (22, 'Blake Turner — Remote worker'),
            (23, 'Dakota Evans — Compliance analyst')
    ) AS catalog(i, display_label)
)
INSERT INTO identity_human_display_profiles (
    organization_id, actor_id, display_label, created_at, updated_at)
SELECT
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    format('f1000000-0000-4000-8000-%1$s', lpad(catalog.i::text, 12, '0'))::uuid,
    catalog.display_label,
    CLOCK_TIMESTAMP(),
    CLOCK_TIMESTAMP()
FROM participant_catalog AS catalog
ON CONFLICT (organization_id, actor_id) DO UPDATE
SET display_label = EXCLUDED.display_label,
    updated_at = EXCLUDED.updated_at;

WITH enrollment_rows AS (
    SELECT
        gs.i,
        format('e1000000-0000-4000-8000-%1$s', lpad(gs.i::text, 12, '0'))::uuid AS enrollment_id,
        CASE
            WHEN gs.i = 1 THEN 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab'::uuid
            ELSE format('f1000000-0000-4000-8000-%1$s', lpad((gs.i - 1)::text, 12, '0'))::uuid
        END AS participant_actor_id,
        CASE gs.i
            WHEN 5 THEN 'suspended'
            WHEN 7 THEN 'closed'
            WHEN 9 THEN 'suspended'
            WHEN 11 THEN 'revoked'
            WHEN 12 THEN 'closed'
            WHEN 14 THEN 'suspended'
            WHEN 18 THEN 'suspended'
            WHEN 20 THEN 'closed'
            WHEN 22 THEN 'revoked'
            WHEN 23 THEN 'closed'
            ELSE 'active'
        END AS status,
        TIMESTAMPTZ '2026-08-27 00:00:00+00' + (gs.i * INTERVAL '1 minute') AS stamp
    FROM generate_series(1, 24) AS gs(i)
)
INSERT INTO submissions_enrollments (
    organization_id, enrollment_id, activity_id, cohort_id, baseline_id,
    task_source_id, task_version_id, task_content_digest, lifecycle_policy_id,
    lifecycle_policy_version, participant_actor_id, status, revision,
    assigned_by_actor_id, assigned_at, updated_at)
SELECT
    'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    enrollment_id,
    'a1000000-0000-4000-8000-000000000025',
    'c1000000-0000-4000-8000-000000000025',
    'd1000000-0000-4000-8000-000000000025',
    '22222222-2222-2222-2222-222222222209',
    '33333333-3333-3333-3333-333333333309',
    repeat('j', 64),
    '11111111-1111-4111-8111-111111111118',
    1,
    participant_actor_id,
    status,
    1,
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
    stamp,
    stamp
FROM enrollment_rows
ON CONFLICT (organization_id, enrollment_id) DO NOTHING;

COMMIT;
