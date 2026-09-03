using System.Security.Cryptography;
using System.Text;
using Dapper;
using FlexAgent.Configuration;
using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Postgres.Migrations;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Submissions.Domain;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class MigrationUpgradeTests
{
    private const string Historical0002ScriptName = "0002_idempotency_and_version_immutability.sql";
    private const string Historical0003ScriptName = "0003_repair_idempotency_backfill_and_source_version_fk.sql";
    private const string Current0004ScriptName = "0004_harden_constraint_scope_checks.sql";
    private const string Current0005ScriptName = "0005_session_runtime_schema.sql";
    private const string Current0006ScriptName = "0006_harden_session_runtime_invariants.sql";
    private const string Current0007ScriptName = "0007_session_invocation_admitted_at.sql";
    private const string Current0008ScriptName = "0008_session_turn_created_sequence.sql";
    private const string Current0009ScriptName = "0009_session_decision_envelope_v2.sql";
    private const string Current0010ScriptName = "0010_session_decision_item_effects.sql";
    private const string Current0011ScriptName = "0011_session_decision_item_effect_ownership.sql";
    private const string Current0012ScriptName = "0012_session_agent_message_fragments.sql";
    private const string Current0013ScriptName = "0013_session_fragment_publication_coherence.sql";
    private const string Current0014ScriptName = "0014_session_fragment_accepted_output_from_parent.sql";
    private const string Current0015ScriptName = "0015_session_message_seal_sequence.sql";
    private const string Current0016ScriptName = "0016_session_timer_schedule_contract_states.sql";
    private const string Current0017ScriptName = "0017_session_manifest_runtime_and_handoff.sql";
    private const string Current0018ScriptName = "0018_session_evaluation_handoff_terminal_binding.sql";
    private const string Current0019ScriptName = "0019_session_durable_work_claim_partitions.sql";
    private const string Current0020ScriptName = "0020_session_durable_work_claimable_index.sql";
    private const string Current0021ScriptName = "0021_session_subject_binding_rehydration.sql";
    private const string Current0022ScriptName = "0022_service_delegations_and_timer_lane_reference.sql";
    private const string Current0023ScriptName = "0023_service_delegation_audit_reference_and_timer_expiry.sql";
    private const string Current0024ScriptName = "0024_actor_organization_grant_id.sql";
    private const string Current0025ScriptName = "0025_service_principal_bindings_and_invocation_execute_delegation.sql";
    private const string Current0026ScriptName = "0026_session_frozen_model_deployment_and_provider_attempts.sql";
    private const string Current0027ScriptName = "0027_session_provider_request_attempt_identity.sql";
    private const string Current0028ScriptName = "0028_session_visible_transcript_exact_text.sql";
    private const string Current0029ScriptName = "0029_session_provider_request_started_finished_facts.sql";
    private const string Current0030ScriptName = "0030_human_identity_and_oidc_application_state.sql";
    private const string Current0031ScriptName = "0031_human_auth_rotation_and_logout_replay.sql";
    private const string Current0032ScriptName = "0032_human_auth_correlation_and_provider_revocation.sql";
    private const string Current0033ScriptName = "0033_human_auth_identity_logout_watermarks.sql";
    private const string Current0034ScriptName = "0034_assessment_configuration_and_source_descriptors.sql";
    private const string Current0035ScriptName = "0035_assessment_parent_traversal_and_attempt_hardening.sql";
    private const string Current0036ScriptName = "0036_assessment_activation_attempt_and_baseline_provenance.sql";
    private const string Current0037ScriptName = "0037_assessment_activation_attempt_requested_and_authoritative_revision.sql";
    private const string Current0038ScriptName = "0038_assessment_activation_attempt_retry_and_authoritative_history.sql";
    private const string Current0039ScriptName = "0039_assessment_activation_attempt_unbound_parent_and_timing.sql";
    private const string Current0040ScriptName = "0040_assessment_activation_operation_binding_and_attempt_timing.sql";
    private const string Current0041ScriptName = "0041_assessment_activity_revision_provenance.sql";
    private const string Current0042ScriptName = "0042_assessment_revision_same_activity_predecessor.sql";
    private const string Current0043ScriptName = "0043_enrollment_assignment_and_display_profile.sql";
    private const string Current0044ScriptName = "0044_enrollment_shared_request_admission.sql";
    private const string Current0045ScriptName = "0045_enrollment_shared_admission_window_freeze_and_expiry.sql";
    private const string Current0046ScriptName = "0046_enrollment_accommodations.sql";
    private const string Current0047ScriptName = "0047_accommodation_complete_enrollment_parent.sql";
    private const string Current0048ScriptName = "0048_submission_intake_and_accepted_versions.sql";
    private const string Current0049ScriptName = "0049_submission_complete_parent_scope.sql";
    private const string Current0050ScriptName = "0050_submission_complete_task_binding_scope.sql";
    private const string Current0051ScriptName = "0051_submission_durable_work.sql";
    private const string Current0052ScriptName = "0052_submission_lifecycle_holds_and_dispositions.sql";
    private const string Current0053ScriptName = "0053_submission_accepted_payload_cleanup.sql";
    private const string Current0054ScriptName = "0054_submission_cleanup_exact_artifact_version.sql";
    private const string Current0055ScriptName = "0055_submission_cleanup_version_backfill_and_scan.sql";
    private const string Current0056ScriptName = "0056_submission_cleanup_scan_generation.sql";
    private const string Current0056aScriptName = "0056a_submission_cleanup_park_duplicate_disposition_facts.sql";
    private const string Current0057ScriptName = "0057_submission_cleanup_terminal_failure_and_disposition_uniqueness.sql";
    private const string Current0058ScriptName = "0058_submission_cleanup_disposition_guard_and_reconstruction.sql";
    private const string Current0059ScriptName = "0059_submission_cleanup_accepted_reconstruction_precedence.sql";
    private const string Current0060ScriptName = "0060_submission_cleanup_restore_parked_disposition_facts.sql";
    private const string Current0061ScriptName = "0061_human_auth_provider_id_token_ciphertext.sql";
    private const string Current0062ScriptName = "0062_human_auth_seated_display_name.sql";
    private const string Current0063ScriptName = "0063_attempt_start_and_resolved_configuration.sql";
    private const string Current0064ScriptName = "0064_acknowledgment_one_way_attempt_binding.sql";
    private const string Current0065ScriptName = "0065_attempt_submission_binding_content_digest.sql";
    private const string Current0066ScriptName = "0066_session_frozen_policy_distinct_digests.sql";
    private const string Current0067ScriptName = "0067_participant_notice_projection_sets.sql";
    private const string Current0068ScriptName = "0068_session_runtime_pause_intervals.sql";
    private const string Current0069ScriptName = "0069_session_frozen_timing.sql";

    [Fact]
    public async Task Upgrade_from_0001_backfills_idempotency_and_rejects_conflicting_retry()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: "0001_initial_authorization_configuration_schema.sql");

        var seededState = await SeedLegacyVersionAsync(connectionString);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);

        await AssertRepairEvidenceAsync(connectionString, seededState);
    }

    [Fact]
    public async Task Upgrade_from_0045_creates_accommodation_tables_without_rewriting_enrollment()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0045ScriptName);

        await using (var before = new NpgsqlConnection(connectionString))
        {
            await before.OpenAsync(TestContext.Current.CancellationToken);
            Assert.False(await before.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = 'submissions_accommodations');
                """));
            Assert.True(await before.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = 'submissions_enrollments');
                """));
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(TestContext.Current.CancellationToken);
        Assert.True(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'submissions_accommodations');
            """));
        Assert.True(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'submissions_accommodation_facts');
            """));
        Assert.Equal(
            "fk_submissions_accommodations_enrollment_parent",
            await after.ExecuteScalarAsync<string>(
                """
                SELECT conname
                FROM pg_constraint
                WHERE conrelid = 'submissions_accommodations'::regclass
                  AND conname = 'fk_submissions_accommodations_enrollment_parent';
                """));
        Assert.Equal(
            Current0046ScriptName,
            await after.ExecuteScalarAsync<string>(
                """
                SELECT script_name
                FROM grate_migrations
                WHERE script_name = @ScriptName;
                """,
                new { ScriptName = Current0046ScriptName }));
        Assert.Equal(
            Current0047ScriptName,
            await after.ExecuteScalarAsync<string>(
                """
                SELECT script_name
                FROM grate_migrations
                WHERE script_name = @ScriptName;
                """,
                new { ScriptName = Current0047ScriptName }));
        Assert.Equal(
            Current0048ScriptName,
            await after.ExecuteScalarAsync<string>(
                """
                SELECT script_name
                FROM grate_migrations
                WHERE script_name = @ScriptName;
                """,
                new { ScriptName = Current0048ScriptName }));
    }

    [Fact]
    public async Task Upgrade_from_0047_creates_submission_intake_and_accepted_version_tables()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0047ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0048ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        Assert.True(await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'submissions_intakes');
            """));
        Assert.True(await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'submissions_accepted_versions');
            """));
    }

    [Fact]
    public async Task Upgrade_from_0048_hardens_submission_parent_scope_and_lineage()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0048ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0049ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        Assert.True(await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.table_constraints
                WHERE constraint_name = 'fk_submissions_intakes_submission_parent');
            """));
        Assert.True(await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.table_constraints
                WHERE constraint_name = 'fk_submissions_accepted_versions_predecessor');
            """));
    }

    [Fact]
    public async Task Upgrade_from_0049_extends_submission_parent_scope_with_task_binding()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0049ScriptName);

        await using (var before = new NpgsqlConnection(connectionString))
        {
            await before.OpenAsync(TestContext.Current.CancellationToken);
            Assert.False(await before.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'uq_submissions_enrollments_complete_binding');
                """));
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0053ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(TestContext.Current.CancellationToken);
        var completeScopeColumns = await after.QueryAsync<string>(
            """
            SELECT a.attname
            FROM pg_constraint c
            JOIN unnest(c.conkey) WITH ORDINALITY AS cols(attnum, ordinality) ON TRUE
            JOIN pg_attribute a
                ON a.attrelid = c.conrelid AND a.attnum = cols.attnum
            WHERE c.conname = 'uq_submissions_submissions_complete_scope'
            ORDER BY cols.ordinality;
            """);
        Assert.Equal(
            [
                "organization_id",
                "submission_id",
                "enrollment_id",
                "activity_id",
                "cohort_id",
                "baseline_id",
                "participant_actor_id",
                "task_source_id",
                "task_version_id",
                "task_content_digest",
            ],
            completeScopeColumns.ToArray());
        Assert.True(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conrelid = 'submissions_submissions'::regclass
                  AND conname = 'fk_submissions_submissions_enrollment_parent');
            """));
    }

    [Fact]
    public async Task Upgrade_from_0050_creates_submission_durable_work()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0050ScriptName);

        await using (var before = new NpgsqlConnection(connectionString))
        {
            await before.OpenAsync(TestContext.Current.CancellationToken);
            Assert.False(await before.ExecuteScalarAsync<bool>(
                "SELECT to_regclass('public.submissions_durable_work') IS NOT NULL;"));
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0053ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(TestContext.Current.CancellationToken);
        Assert.Equal("submissions_durable_work", await after.ExecuteScalarAsync<string>(
            "SELECT to_regclass('public.submissions_durable_work')::text;"));
        Assert.True(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM pg_indexes
                WHERE indexname = 'ix_submissions_durable_work_claimable');
            """));
    }

    [Fact]
    public async Task Upgrade_from_0051_creates_submission_lifecycle_hold_and_disposition_tables()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0051ScriptName);

        await using (var before = new NpgsqlConnection(connectionString))
        {
            await before.OpenAsync(TestContext.Current.CancellationToken);
            Assert.False(await before.ExecuteScalarAsync<bool>(
                "SELECT to_regclass('public.submissions_lifecycle_holds') IS NOT NULL;"));
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0052ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(TestContext.Current.CancellationToken);
        Assert.Equal("submissions_lifecycle_holds", await after.ExecuteScalarAsync<string>(
            "SELECT to_regclass('public.submissions_lifecycle_holds')::text;"));
        Assert.Equal("submissions_artifact_dispositions", await after.ExecuteScalarAsync<string>(
            "SELECT to_regclass('public.submissions_artifact_dispositions')::text;"));
        Assert.Equal("submissions_protected_capabilities", await after.ExecuteScalarAsync<string>(
            "SELECT to_regclass('public.submissions_protected_capabilities')::text;"));
    }

    [Fact]
    public async Task Upgrade_from_0052_allows_accepted_payload_cleanup_work_kind()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0052ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0053ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(TestContext.Current.CancellationToken);
        var kindCheck = await after.ExecuteScalarAsync<string>(
            """
            SELECT pg_get_constraintdef(oid)
            FROM pg_constraint
            WHERE conname = 'chk_submissions_durable_work_kind'
            """);
        Assert.Contains("cleanup_accepted", kindCheck, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Upgrade_from_0053_adds_durable_work_artifact_version_id()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0053ScriptName);

        await using (var before = new NpgsqlConnection(connectionString))
        {
            await before.OpenAsync(TestContext.Current.CancellationToken);
            Assert.False(await before.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_name = 'submissions_durable_work'
                      AND column_name = 'artifact_version_id');
                """));
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0054ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(TestContext.Current.CancellationToken);
        Assert.True(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_name = 'submissions_durable_work'
                  AND column_name = 'artifact_version_id');
            """));
    }

    [Fact]
    public async Task Upgrade_from_0053_pending_cleanup_backfills_exact_artifact_version()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0053ScriptName);

        var organizationId = Guid.CreateVersion7();
        var intakeId = Guid.CreateVersion7();
        var workId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var objectKey = $"org/{organizationId:D}/{itemId:D}";
        const string exactVersion = "seaweed-version-legacy-0053";

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(
                """
                SET session_replication_role = replica;
                INSERT INTO submissions_intake_items (
                    organization_id, intake_id, item_id, category, filename, declared_mime_type,
                    byte_count, content_digest, artifact_object_key, artifact_version_id, received_at)
                VALUES (
                    @OrganizationId, @IntakeId, @ItemId, 'direct_text', NULL, 'text/plain',
                    12, @Digest, @ArtifactObjectKey, @ArtifactVersionId, CLOCK_TIMESTAMP());
                INSERT INTO submissions_durable_work (
                    organization_id, work_id, work_kind, enrollment_id, intake_id, version_id,
                    status, attempt_count, available_at, lease_until, artifact_object_key, created_at)
                VALUES (
                    @OrganizationId, @WorkId, 'cleanup_incomplete', @EnrollmentId, @IntakeId, NULL,
                    'pending', 0, CLOCK_TIMESTAMP(), NULL, @ArtifactObjectKey, CLOCK_TIMESTAMP());
                SET session_replication_role = DEFAULT;
                """,
                new
                {
                    OrganizationId = organizationId,
                    IntakeId = intakeId,
                    ItemId = itemId,
                    WorkId = workId,
                    EnrollmentId = Guid.CreateVersion7(),
                    Digest = new string('a', 64),
                    ArtifactObjectKey = objectKey,
                    ArtifactVersionId = exactVersion,
                });
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0055ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(cancellationToken);
        var backfilled = await after.QuerySingleAsync<(string ArtifactVersionId, string Status)>(
            """
            SELECT artifact_version_id, status
            FROM submissions_durable_work
            WHERE organization_id = @OrganizationId AND work_id = @WorkId
            """,
            new { OrganizationId = organizationId, WorkId = workId });
        Assert.Equal(exactVersion, backfilled.ArtifactVersionId);
        Assert.Equal("pending", backfilled.Status);
        Assert.True(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'chk_submissions_durable_work_exact_artifact_version');
            """));
        Assert.Equal("submissions_accepted_cleanup_scan", await after.ExecuteScalarAsync<string>(
            "SELECT to_regclass('public.submissions_accepted_cleanup_scan')::text;"));
    }

    [Fact]
    public async Task Upgrade_from_0053_unbackfillable_cleanup_is_deleted_by_shipped_0055()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0053ScriptName);

        var organizationId = Guid.CreateVersion7();
        var workId = Guid.CreateVersion7();
        var objectKey = $"org/{organizationId:D}/{Guid.CreateVersion7():D}";

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(
                """
                SET session_replication_role = replica;
                INSERT INTO submissions_durable_work (
                    organization_id, work_id, work_kind, enrollment_id, intake_id, version_id,
                    status, attempt_count, available_at, lease_until, artifact_object_key, created_at)
                VALUES (
                    @OrganizationId, @WorkId, 'cleanup_incomplete', @EnrollmentId, @IntakeId, NULL,
                    'pending', 0, CLOCK_TIMESTAMP(), NULL, @ArtifactObjectKey, CLOCK_TIMESTAMP());
                SET session_replication_role = DEFAULT;
                """,
                new
                {
                    OrganizationId = organizationId,
                    WorkId = workId,
                    EnrollmentId = Guid.CreateVersion7(),
                    IntakeId = Guid.CreateVersion7(),
                    ArtifactObjectKey = objectKey,
                });
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0055ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(cancellationToken);
        Assert.False(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM submissions_durable_work
                WHERE organization_id = @OrganizationId AND work_id = @WorkId)
            """,
            new { OrganizationId = organizationId, WorkId = workId }));
    }

    [Fact]
    public async Task Upgrade_from_0057_reconstructs_terminal_failure_from_unversioned_intake_item()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0053ScriptName);

        var organizationId = Guid.CreateVersion7();
        var workId = Guid.CreateVersion7();
        var intakeId = Guid.CreateVersion7();
        var objectKey = $"org/{organizationId:D}/{Guid.CreateVersion7():D}";

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(
                """
                SET session_replication_role = replica;
                INSERT INTO submissions_intake_items (
                    organization_id, intake_id, item_id, category, filename, declared_mime_type,
                    byte_count, content_digest, artifact_object_key, artifact_version_id, received_at)
                VALUES (
                    @OrganizationId, @IntakeId, @ItemId, 'direct_text', NULL, 'text/plain',
                    12, @Digest, @ArtifactObjectKey, NULL, CLOCK_TIMESTAMP());
                INSERT INTO submissions_durable_work (
                    organization_id, work_id, work_kind, enrollment_id, intake_id, version_id,
                    status, attempt_count, available_at, lease_until, artifact_object_key, created_at)
                VALUES (
                    @OrganizationId, @WorkId, 'cleanup_incomplete', @EnrollmentId, @IntakeId, NULL,
                    'pending', 0, CLOCK_TIMESTAMP(), NULL, @ArtifactObjectKey, CLOCK_TIMESTAMP());
                SET session_replication_role = DEFAULT;
                """,
                new
                {
                    OrganizationId = organizationId,
                    IntakeId = intakeId,
                    ItemId = Guid.CreateVersion7(),
                    WorkId = workId,
                    EnrollmentId = Guid.CreateVersion7(),
                    Digest = new string('a', 64),
                    ArtifactObjectKey = objectKey,
                });
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0060ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(cancellationToken);
        Assert.False(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM submissions_durable_work
                WHERE organization_id = @OrganizationId AND work_id = @WorkId)
            """,
            new { OrganizationId = organizationId, WorkId = workId }));
        var reconstructed = await after.QuerySingleAsync<(string Status, string? FailureReason, string WorkKind, Guid? EnrollmentId)>(
            """
            SELECT status, failure_reason, work_kind, enrollment_id
            FROM submissions_durable_work
            WHERE organization_id = @OrganizationId AND artifact_object_key = @ArtifactObjectKey
            """,
            new { OrganizationId = organizationId, ArtifactObjectKey = objectKey });
        Assert.Equal("failed", reconstructed.Status);
        Assert.Equal("legacy_unversioned_reconstruction", reconstructed.FailureReason);
        Assert.Equal("cleanup_legacy_reconstruction", reconstructed.WorkKind);
        Assert.Null(reconstructed.EnrollmentId);
        Assert.False(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_indexes
                WHERE indexname = 'uq_submissions_artifact_dispositions_artifact');
            """));
        Assert.Equal("submissions_artifact_disposition_guards", await after.ExecuteScalarAsync<string>(
            "SELECT to_regclass('public.submissions_artifact_disposition_guards')::text;"));
    }

    [Fact]
    public async Task Upgrade_from_0056_historical_duplicate_dispositions_survive_immutable_0057()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0056ScriptName);

        var organizationId = Guid.CreateVersion7();
        var objectKey = $"org/{organizationId:D}/{Guid.CreateVersion7():D}";
        var firstDispositionId = Guid.CreateVersion7();
        var secondDispositionId = Guid.CreateVersion7();
        var firstDisposedAt = DateTimeOffset.Parse("2026-08-20T12:00:00Z");
        var secondDisposedAt = DateTimeOffset.Parse("2026-08-21T12:00:00Z");

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(
                """
                INSERT INTO submissions_artifact_dispositions (
                    organization_id, disposition_id, work_kind, artifact_object_key, disposed_at)
                VALUES
                    (@OrganizationId, @FirstDispositionId, 'cleanup_accepted', @ArtifactObjectKey, @FirstDisposedAt),
                    (@OrganizationId, @SecondDispositionId, 'cleanup_accepted', @ArtifactObjectKey, @SecondDisposedAt);
                """,
                new
                {
                    OrganizationId = organizationId,
                    FirstDispositionId = firstDispositionId,
                    SecondDispositionId = secondDispositionId,
                    ArtifactObjectKey = objectKey,
                    FirstDisposedAt = firstDisposedAt,
                    SecondDisposedAt = secondDisposedAt,
                });
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0060ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(cancellationToken);
        var dispositions = (await after.QueryAsync<(Guid DispositionId, DateTimeOffset DisposedAt)>(
            """
            SELECT disposition_id, disposed_at
            FROM submissions_artifact_dispositions
            WHERE organization_id = @OrganizationId AND artifact_object_key = @ArtifactObjectKey
            ORDER BY disposed_at, disposition_id
            """,
            new { OrganizationId = organizationId, ArtifactObjectKey = objectKey })).AsList();
        Assert.Equal(2, dispositions.Count);
        Assert.Equal(firstDispositionId, dispositions[0].DispositionId);
        Assert.Equal(secondDispositionId, dispositions[1].DispositionId);
        var guard = await after.QuerySingleAsync<(Guid FirstDispositionId, string ObjectKey)>(
            """
            SELECT first_disposition_id, artifact_object_key
            FROM submissions_artifact_disposition_guards
            WHERE organization_id = @OrganizationId AND artifact_object_key = @ArtifactObjectKey
            """,
            new { OrganizationId = organizationId, ArtifactObjectKey = objectKey });
        Assert.Equal(firstDispositionId, guard.FirstDispositionId);
        Assert.Equal(objectKey, guard.ObjectKey);
        Assert.False(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_indexes
                WHERE indexname = 'uq_submissions_artifact_dispositions_artifact');
            """));
        Assert.Null(await after.ExecuteScalarAsync<string>(
            "SELECT to_regclass('public.submissions_artifact_disposition_upgrade_overflow')::text;"));
    }

    [Fact]
    public async Task Upgrade_from_0060_keeps_later_duplicate_disposition_facts()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0056ScriptName);

        var organizationId = Guid.CreateVersion7();
        var objectKey = $"org/{organizationId:D}/{Guid.CreateVersion7():D}";
        var firstDispositionId = Guid.CreateVersion7();
        var secondDispositionId = Guid.CreateVersion7();

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(
                """
                INSERT INTO submissions_artifact_dispositions (
                    organization_id, disposition_id, work_kind, artifact_object_key, disposed_at)
                VALUES
                    (@OrganizationId, @FirstDispositionId, 'cleanup_accepted', @ArtifactObjectKey, CLOCK_TIMESTAMP());
                """,
                new
                {
                    OrganizationId = organizationId,
                    FirstDispositionId = firstDispositionId,
                    ArtifactObjectKey = objectKey,
                });
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0060ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(cancellationToken);
        await after.ExecuteAsync(
            """
            INSERT INTO submissions_artifact_dispositions (
                organization_id, disposition_id, work_kind, artifact_object_key, disposed_at)
            VALUES
                (@OrganizationId, @SecondDispositionId, 'cleanup_accepted', @ArtifactObjectKey, CLOCK_TIMESTAMP());
            """,
            new
            {
                OrganizationId = organizationId,
                SecondDispositionId = secondDispositionId,
                ArtifactObjectKey = objectKey,
            });
        var dispositionCount = await after.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM submissions_artifact_dispositions
            WHERE organization_id = @OrganizationId AND artifact_object_key = @ArtifactObjectKey
            """,
            new { OrganizationId = organizationId, ArtifactObjectKey = objectKey });
        Assert.Equal(2, dispositionCount);
        var guard = await after.QuerySingleAsync<(Guid FirstDispositionId, string ObjectKey)>(
            """
            SELECT first_disposition_id, artifact_object_key
            FROM submissions_artifact_disposition_guards
            WHERE organization_id = @OrganizationId AND artifact_object_key = @ArtifactObjectKey
            """,
            new { OrganizationId = organizationId, ArtifactObjectKey = objectKey });
        Assert.Equal(firstDispositionId, guard.FirstDispositionId);
        Assert.Equal(objectKey, guard.ObjectKey);
        Assert.False(await after.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_indexes
                WHERE indexname = 'uq_submissions_artifact_dispositions_artifact');
            """));
    }

    [Fact]
    public async Task Upgrade_from_0058_reconstructs_rejected_intake_cleanup_kind_and_enrollment()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0053ScriptName);

        var organizationId = Guid.CreateVersion7();
        var enrollmentId = Guid.CreateVersion7();
        var intakeId = Guid.CreateVersion7();
        var workId = Guid.CreateVersion7();
        var objectKey = $"org/{organizationId:D}/{Guid.CreateVersion7():D}";
        var digest = new string('a', 64);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(
                """
                SET session_replication_role = replica;
                INSERT INTO submissions_intakes (
                    organization_id, intake_id, submission_id, activity_id, cohort_id, baseline_id,
                    enrollment_id, participant_actor_id, task_source_id, task_version_id,
                    task_content_digest, status, revision, policy_digest,
                    frozen_requirement_source_id, frozen_requirement_version_id, frozen_requirement_digest,
                    organization_policy_source_id, organization_policy_version_id, organization_policy_digest,
                    created_at, updated_at, complete_receipt_at)
                VALUES (
                    @OrganizationId, @IntakeId, @SubmissionId, @ActivityId, @CohortId, @BaselineId,
                    @EnrollmentId, @ParticipantId, @TaskSourceId, @TaskVersionId,
                    @Digest, 'rejected', 1, @Digest,
                    @TaskSourceId, @TaskVersionId, @Digest,
                    @TaskSourceId, @TaskVersionId, @Digest,
                    CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP(), NULL);
                INSERT INTO submissions_intake_items (
                    organization_id, intake_id, item_id, category, filename, declared_mime_type,
                    byte_count, content_digest, artifact_object_key, artifact_version_id, received_at)
                VALUES (
                    @OrganizationId, @IntakeId, @ItemId, 'direct_text', NULL, 'text/plain',
                    12, @Digest, @ArtifactObjectKey, NULL, CLOCK_TIMESTAMP());
                INSERT INTO submissions_durable_work (
                    organization_id, work_id, work_kind, enrollment_id, intake_id, version_id,
                    status, attempt_count, available_at, lease_until, artifact_object_key, created_at)
                VALUES (
                    @OrganizationId, @WorkId, 'cleanup_rejected', @EnrollmentId, @IntakeId, NULL,
                    'pending', 0, CLOCK_TIMESTAMP(), NULL, @ArtifactObjectKey, CLOCK_TIMESTAMP());
                SET session_replication_role = DEFAULT;
                """,
                new
                {
                    OrganizationId = organizationId,
                    IntakeId = intakeId,
                    SubmissionId = Guid.CreateVersion7(),
                    ActivityId = Guid.CreateVersion7(),
                    CohortId = Guid.CreateVersion7(),
                    BaselineId = Guid.CreateVersion7(),
                    EnrollmentId = enrollmentId,
                    ParticipantId = Guid.CreateVersion7(),
                    TaskSourceId = Guid.CreateVersion7(),
                    TaskVersionId = Guid.CreateVersion7(),
                    Digest = digest,
                    ItemId = Guid.CreateVersion7(),
                    WorkId = workId,
                    ArtifactObjectKey = objectKey,
                });
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0058ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(cancellationToken);
        var reconstructed = await after.QuerySingleAsync<(string Status, string WorkKind, Guid? EnrollmentId, string? FailureReason)>(
            """
            SELECT status, work_kind, enrollment_id, failure_reason
            FROM submissions_durable_work
            WHERE organization_id = @OrganizationId AND artifact_object_key = @ArtifactObjectKey
            """,
            new { OrganizationId = organizationId, ArtifactObjectKey = objectKey });
        Assert.Equal("failed", reconstructed.Status);
        Assert.Equal("cleanup_rejected", reconstructed.WorkKind);
        Assert.Equal(enrollmentId, reconstructed.EnrollmentId);
        Assert.Equal("exact_artifact_version_unavailable", reconstructed.FailureReason);
    }

    [Fact]
    public async Task Upgrade_from_0058_reconstructs_accepted_cleanup_enrollment()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0053ScriptName);

        var organizationId = Guid.CreateVersion7();
        var enrollmentId = Guid.CreateVersion7();
        var versionId = Guid.CreateVersion7();
        var workId = Guid.CreateVersion7();
        var objectKey = $"org/{organizationId:D}/{Guid.CreateVersion7():D}";
        var digest = new string('a', 64);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(
                """
                SET session_replication_role = replica;
                INSERT INTO submissions_accepted_versions (
                    organization_id, submission_id, version_id, version_number, activity_id, cohort_id,
                    baseline_id, enrollment_id, participant_actor_id, task_source_id, task_version_id,
                    task_content_digest, policy_digest, predecessor_version_id, accepted_at, accepted_by_actor_id)
                VALUES (
                    @OrganizationId, @SubmissionId, @VersionId, 1, @ActivityId, @CohortId,
                    @BaselineId, @EnrollmentId, @ParticipantId, @TaskSourceId, @TaskVersionId,
                    @Digest, @Digest, NULL, CLOCK_TIMESTAMP(), @ParticipantId);
                INSERT INTO submissions_accepted_version_items (
                    organization_id, version_id, item_id, category, filename, byte_count, content_digest,
                    artifact_object_key, artifact_version_id)
                VALUES (
                    @OrganizationId, @VersionId, @ItemId, 'direct_text', NULL, 12, @Digest,
                    @ArtifactObjectKey, '');
                INSERT INTO submissions_durable_work (
                    organization_id, work_id, work_kind, enrollment_id, intake_id, version_id,
                    status, attempt_count, available_at, lease_until, artifact_object_key, created_at)
                VALUES (
                    @OrganizationId, @WorkId, 'cleanup_accepted', @EnrollmentId, NULL, @VersionId,
                    'pending', 0, CLOCK_TIMESTAMP(), NULL, @ArtifactObjectKey, CLOCK_TIMESTAMP());
                SET session_replication_role = DEFAULT;
                """,
                new
                {
                    OrganizationId = organizationId,
                    SubmissionId = Guid.CreateVersion7(),
                    VersionId = versionId,
                    ActivityId = Guid.CreateVersion7(),
                    CohortId = Guid.CreateVersion7(),
                    BaselineId = Guid.CreateVersion7(),
                    EnrollmentId = enrollmentId,
                    ParticipantId = Guid.CreateVersion7(),
                    TaskSourceId = Guid.CreateVersion7(),
                    TaskVersionId = Guid.CreateVersion7(),
                    Digest = digest,
                    ItemId = Guid.CreateVersion7(),
                    WorkId = workId,
                    ArtifactObjectKey = objectKey,
                });
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0058ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(cancellationToken);
        var reconstructed = await after.QuerySingleAsync<(string Status, string WorkKind, Guid? EnrollmentId, Guid? VersionId)>(
            """
            SELECT status, work_kind, enrollment_id, version_id
            FROM submissions_durable_work
            WHERE organization_id = @OrganizationId AND artifact_object_key = @ArtifactObjectKey
            """,
            new { OrganizationId = organizationId, ArtifactObjectKey = objectKey });
        Assert.Equal("failed", reconstructed.Status);
        Assert.Equal("cleanup_accepted", reconstructed.WorkKind);
        Assert.Equal(enrollmentId, reconstructed.EnrollmentId);
        Assert.Equal(versionId, reconstructed.VersionId);
    }

    [Fact]
    public async Task Upgrade_from_0059_accepted_intake_overlap_reconstructs_accepted_kind_and_version()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0053ScriptName);

        var organizationId = Guid.CreateVersion7();
        var enrollmentId = Guid.CreateVersion7();
        var intakeId = Guid.CreateVersion7();
        var versionId = Guid.CreateVersion7();
        var workId = Guid.CreateVersion7();
        var objectKey = $"org/{organizationId:D}/{Guid.CreateVersion7():D}";
        var digest = new string('a', 64);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(
                """
                SET session_replication_role = replica;
                INSERT INTO submissions_intakes (
                    organization_id, intake_id, submission_id, activity_id, cohort_id, baseline_id,
                    enrollment_id, participant_actor_id, task_source_id, task_version_id,
                    task_content_digest, status, revision, policy_digest,
                    frozen_requirement_source_id, frozen_requirement_version_id, frozen_requirement_digest,
                    organization_policy_source_id, organization_policy_version_id, organization_policy_digest,
                    created_at, updated_at, complete_receipt_at)
                VALUES (
                    @OrganizationId, @IntakeId, @SubmissionId, @ActivityId, @CohortId, @BaselineId,
                    @EnrollmentId, @ParticipantId, @TaskSourceId, @TaskVersionId,
                    @Digest, 'accepted', 1, @Digest,
                    @TaskSourceId, @TaskVersionId, @Digest,
                    @TaskSourceId, @TaskVersionId, @Digest,
                    CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP());
                INSERT INTO submissions_intake_items (
                    organization_id, intake_id, item_id, category, filename, declared_mime_type,
                    byte_count, content_digest, artifact_object_key, artifact_version_id, received_at)
                VALUES (
                    @OrganizationId, @IntakeId, @IntakeItemId, 'direct_text', NULL, 'text/plain',
                    12, @Digest, @ArtifactObjectKey, NULL, CLOCK_TIMESTAMP());
                INSERT INTO submissions_accepted_versions (
                    organization_id, submission_id, version_id, version_number, activity_id, cohort_id,
                    baseline_id, enrollment_id, participant_actor_id, task_source_id, task_version_id,
                    task_content_digest, policy_digest, predecessor_version_id, accepted_at, accepted_by_actor_id)
                VALUES (
                    @OrganizationId, @SubmissionId, @VersionId, 1, @ActivityId, @CohortId,
                    @BaselineId, @EnrollmentId, @ParticipantId, @TaskSourceId, @TaskVersionId,
                    @Digest, @Digest, NULL, CLOCK_TIMESTAMP(), @ParticipantId);
                INSERT INTO submissions_accepted_version_items (
                    organization_id, version_id, item_id, category, filename, byte_count, content_digest,
                    artifact_object_key, artifact_version_id)
                VALUES (
                    @OrganizationId, @VersionId, @AcceptedItemId, 'direct_text', NULL, 12, @Digest,
                    @ArtifactObjectKey, '');
                INSERT INTO submissions_durable_work (
                    organization_id, work_id, work_kind, enrollment_id, intake_id, version_id,
                    status, attempt_count, available_at, lease_until, artifact_object_key, created_at)
                VALUES (
                    @OrganizationId, @WorkId, 'cleanup_accepted', @EnrollmentId, @IntakeId, @VersionId,
                    'pending', 0, CLOCK_TIMESTAMP(), NULL, @ArtifactObjectKey, CLOCK_TIMESTAMP());
                SET session_replication_role = DEFAULT;
                """,
                new
                {
                    OrganizationId = organizationId,
                    IntakeId = intakeId,
                    SubmissionId = Guid.CreateVersion7(),
                    ActivityId = Guid.CreateVersion7(),
                    CohortId = Guid.CreateVersion7(),
                    BaselineId = Guid.CreateVersion7(),
                    EnrollmentId = enrollmentId,
                    ParticipantId = Guid.CreateVersion7(),
                    TaskSourceId = Guid.CreateVersion7(),
                    TaskVersionId = Guid.CreateVersion7(),
                    Digest = digest,
                    IntakeItemId = Guid.CreateVersion7(),
                    VersionId = versionId,
                    AcceptedItemId = Guid.CreateVersion7(),
                    WorkId = workId,
                    ArtifactObjectKey = objectKey,
                });
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0060ScriptName);

        await using var after = new NpgsqlConnection(connectionString);
        await after.OpenAsync(cancellationToken);
        var reconstructed = await after.QuerySingleAsync<(string Status, string WorkKind, Guid? EnrollmentId, Guid? VersionId)>(
            """
            SELECT status, work_kind, enrollment_id, version_id
            FROM submissions_durable_work
            WHERE organization_id = @OrganizationId AND artifact_object_key = @ArtifactObjectKey
            """,
            new { OrganizationId = organizationId, ArtifactObjectKey = objectKey });
        Assert.Equal("failed", reconstructed.Status);
        Assert.Equal("cleanup_accepted", reconstructed.WorkKind);
        Assert.Equal(enrollmentId, reconstructed.EnrollmentId);
        Assert.Equal(versionId, reconstructed.VersionId);
    }

    [Fact]
    public async Task Upgrade_from_mutated_0044_keeps_aligned_exhausted_counters_controlling_acquisition()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0044ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var organizationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            """
            UPDATE submissions_enrollment_request_policies
            SET policy_revision = policy_revision + 1,
                window_seconds = 20,
                activated_at = clock_timestamp()
            WHERE singleton_key = 1;

            INSERT INTO submissions_enrollment_request_counters (
                organization_id, actor_id, surface, window_start, window_seconds, policy_revision, permit_count)
            VALUES (
                @OrganizationId,
                @ActorId,
                'mutation',
                to_timestamp((floor(extract(epoch FROM clock_timestamp()) / 20))::bigint * 20),
                20,
                2,
                20);
            """,
            new { OrganizationId = organizationId, ActorId = actorId });

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken);

        Assert.Equal(
            "exhausted",
            await connection.ExecuteScalarAsync<string>(
                """
                SELECT decision
                FROM submissions_try_acquire_enrollment_request_permit(
                    @OrganizationId,
                    @ActorId,
                    'mutation',
                    2,
                    60,
                    20,
                    20,
                    64);
                """,
                new { OrganizationId = organizationId, ActorId = actorId }));
        Assert.Equal(
            20,
            await connection.ExecuteScalarAsync<int>(
                """
                SELECT window_seconds
                FROM submissions_enrollment_request_policies
                WHERE singleton_key = 1;
                """));
    }

    [Fact]
    public async Task Upgrade_from_0044_refuses_live_old_window_counters_then_freezes_after_natural_expiry()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0044ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var actorId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            """
            INSERT INTO submissions_enrollment_request_counters (
                organization_id, actor_id, surface, window_start, window_seconds, policy_revision, permit_count)
            VALUES (
                @OrganizationId,
                @ActorId,
                'mutation',
                to_timestamp((floor(extract(epoch FROM clock_timestamp()) / 10))::bigint * 10),
                10,
                1,
                20);

            UPDATE submissions_enrollment_request_policies
            SET policy_revision = policy_revision + 1,
                window_seconds = 20,
                activated_at = clock_timestamp()
            WHERE singleton_key = 1;
            """,
            new { OrganizationId = Guid.CreateVersion7(), ActorId = actorId });

        var blocked = await Assert.ThrowsAsync<PostgresException>(() =>
            GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
                connectionString,
                migrationsDirectory,
                cancellationToken));
        Assert.Contains("expire", blocked.MessageText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drain", blocked.MessageText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            0,
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM grate_migrations WHERE script_name = @ScriptName;",
                new { ScriptName = Current0045ScriptName }));

        await connection.ExecuteAsync(
            """
            UPDATE submissions_enrollment_request_counters
            SET window_start = clock_timestamp() - INTERVAL '1 hour'
            WHERE actor_id = @ActorId;
            """,
            new { ActorId = actorId });

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken);

        Assert.Equal(
            20,
            await connection.ExecuteScalarAsync<int>(
                """
                SELECT window_seconds
                FROM submissions_enrollment_request_policies
                WHERE singleton_key = 1;
                """));
        Assert.Equal(
            1,
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM grate_migrations WHERE script_name = @ScriptName;",
                new { ScriptName = Current0045ScriptName }));
    }

    [Fact]
    public async Task Upgrade_from_0044_refuses_old_window_counters_until_the_frozen_policy_window_ends()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0044ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var actorId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            """
            INSERT INTO submissions_enrollment_request_counters (
                organization_id, actor_id, surface, window_start, window_seconds, policy_revision, permit_count)
            VALUES (
                @OrganizationId,
                @ActorId,
                'mutation',
                to_timestamp((floor(extract(epoch FROM clock_timestamp()) / 20))::bigint * 20),
                10,
                1,
                20);

            UPDATE submissions_enrollment_request_policies
            SET policy_revision = policy_revision + 1,
                window_seconds = 20,
                activated_at = clock_timestamp()
            WHERE singleton_key = 1;
            """,
            new { OrganizationId = Guid.CreateVersion7(), ActorId = actorId });

        var blocked = await Assert.ThrowsAsync<PostgresException>(() =>
            GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
                connectionString,
                migrationsDirectory,
                cancellationToken));
        Assert.Contains("expire", blocked.MessageText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drain", blocked.MessageText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            0,
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM grate_migrations WHERE script_name = @ScriptName;",
                new { ScriptName = Current0045ScriptName }));

        await connection.ExecuteAsync(
            """
            UPDATE submissions_enrollment_request_counters
            SET window_start = clock_timestamp() - INTERVAL '1 hour'
            WHERE actor_id = @ActorId;
            """,
            new { ActorId = actorId });

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken);

        Assert.Equal(
            20,
            await connection.ExecuteScalarAsync<int>(
                """
                SELECT window_seconds
                FROM submissions_enrollment_request_policies
                WHERE singleton_key = 1;
                """));
        Assert.Equal(
            1,
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM grate_migrations WHERE script_name = @ScriptName;",
                new { ScriptName = Current0045ScriptName }));
    }

    [Fact]
    public async Task Upgrade_from_0044_backfills_12s_counter_expiry_to_the_aligned_20s_bucket_end()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0044ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO submissions_enrollment_request_counters (
                organization_id, actor_id, surface, window_start, window_seconds, policy_revision, permit_count)
            VALUES (
                @OrganizationId,
                @ActorId,
                'mutation',
                to_timestamp(36),
                12,
                1,
                20);

            UPDATE submissions_enrollment_request_policies
            SET policy_revision = policy_revision + 1,
                window_seconds = 20,
                activated_at = clock_timestamp()
            WHERE singleton_key = 1;
            """,
            new { OrganizationId = Guid.CreateVersion7(), ActorId = Guid.CreateVersion7() });

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken);

        Assert.Equal(
            60,
            await connection.ExecuteScalarAsync<double>(
                """
                SELECT extract(epoch FROM expires_at)
                FROM submissions_enrollment_request_counters;
                """));
    }

    [Fact]
    public async Task Upgrade_from_0044_refuses_12s_counters_until_the_aligned_20s_bucket_ends()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0044ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var actorId = Guid.CreateVersion7();
        // 12-aligned start at second 12 of the minute: old budget ends at 24,
        // start+20 is 32, aligned 20s bucket ends at 40. Seconds 33-36 sit in
        // that gap. Wall-clock wait is a known test-quality tradeoff.
        await WaitUntilEpochSecondInRangeAsync(connection, 33, 36, cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO submissions_enrollment_request_counters (
                organization_id, actor_id, surface, window_start, window_seconds, policy_revision, permit_count)
            VALUES (
                @OrganizationId,
                @ActorId,
                'mutation',
                to_timestamp(
                    (floor(extract(epoch FROM clock_timestamp()) / 60))::bigint * 60 + 12),
                12,
                1,
                20);

            UPDATE submissions_enrollment_request_policies
            SET policy_revision = policy_revision + 1,
                window_seconds = 20,
                activated_at = clock_timestamp()
            WHERE singleton_key = 1;
            """,
            new { OrganizationId = Guid.CreateVersion7(), ActorId = actorId });

        var blocked = await Assert.ThrowsAsync<PostgresException>(() =>
            GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
                connectionString,
                migrationsDirectory,
                cancellationToken));
        Assert.Contains("expire", blocked.MessageText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drain", blocked.MessageText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            0,
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM grate_migrations WHERE script_name = @ScriptName;",
                new { ScriptName = Current0045ScriptName }));

        await connection.ExecuteAsync(
            """
            UPDATE submissions_enrollment_request_counters
            SET window_start = clock_timestamp() - INTERVAL '1 hour'
            WHERE actor_id = @ActorId;
            """,
            new { ActorId = actorId });

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken);

        Assert.Equal(
            20,
            await connection.ExecuteScalarAsync<int>(
                """
                SELECT window_seconds
                FROM submissions_enrollment_request_policies
                WHERE singleton_key = 1;
                """));
        Assert.Equal(
            1,
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM grate_migrations WHERE script_name = @ScriptName;",
                new { ScriptName = Current0045ScriptName }));
    }

    [Fact]
    public async Task Upgrade_from_empty_0005_applies_0006()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0005ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_empty_0006_applies_0007()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0006ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_empty_0008_applies_0009()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0008ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_empty_0009_applies_0010()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0009ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_empty_0010_applies_0011()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0010ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_empty_0011_applies_0012()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0011ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_empty_0012_applies_0013()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0012ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_populated_0012_backfills_agent_response_slot_and_keeps_publication_fk()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0012ScriptName);

        var seeded = await SeedPopulated0012AgentFragmentsAsync(connectionString);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0020ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var slotId = await connection.ExecuteScalarAsync<string>(
            """
            SELECT response_slot_id
            FROM session_messages
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND message_id = @MessageId;
            """,
            new
            {
                seeded.OrganizationId,
                seeded.SessionId,
                seeded.MessageId,
            });
        Assert.Equal("slot.1", slotId);

        var fragmentCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM session_message_fragments
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND message_id = @MessageId;
            """,
            new
            {
                seeded.OrganizationId,
                seeded.SessionId,
                seeded.MessageId,
            });
        Assert.Equal(2, fragmentCount);

        var fragmentColumns = (await connection.QueryAsync<string>(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'session_message_fragments';
            """)).AsList();
        Assert.DoesNotContain("accepted_agent_output_id", fragmentColumns);
    }

    [Fact]
    public async Task Upgrade_from_empty_0013_applies_0014()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0013ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_empty_0014_applies_0015()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0014ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_empty_0015_applies_0016()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0015ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_populated_0015_preserves_pending_remaining_and_recommendation_category()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0015ScriptName);

        var seeded = await SeedPopulated0015TimerSchedulesAsync(connectionString);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0020ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var pendingDefault = await connection.QuerySingleAsync<(int Remaining, string Category, string LaneState)>(
            """
            SELECT remaining_active_seconds, requested_by_category, lane_state
            FROM session_timer_schedules
            WHERE organization_id = @OrganizationId
              AND session_id = @DefaultSessionId;
            """,
            new
            {
                seeded.OrganizationId,
                seeded.DefaultSessionId,
            });
        Assert.InRange(pendingDefault.Remaining, 590, 610);
        Assert.Equal(TimerRequestedByCategories.DefaultCadence, pendingDefault.Category);
        Assert.Equal(TimerLaneStates.Pending, pendingDefault.LaneState);

        var pendingRecommended = await connection.QuerySingleAsync<(int Remaining, string Category)>(
            """
            SELECT remaining_active_seconds, requested_by_category
            FROM session_timer_schedules
            WHERE organization_id = @OrganizationId
              AND session_id = @RecommendedSessionId;
            """,
            new
            {
                seeded.OrganizationId,
                seeded.RecommendedSessionId,
            });
        Assert.InRange(pendingRecommended.Remaining, 590, 610);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, pendingRecommended.Category);

        var replaced = await connection.QuerySingleAsync<(int Remaining, string Category, string State, string LaneState)>(
            """
            SELECT remaining_active_seconds, requested_by_category, state, lane_state
            FROM session_timer_schedules
            WHERE organization_id = @OrganizationId
              AND session_id = @ReplacedSessionId;
            """,
            new
            {
                seeded.OrganizationId,
                seeded.ReplacedSessionId,
            });
        Assert.Equal(0, replaced.Remaining);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, replaced.Category);
        Assert.Equal("replaced", replaced.State);
        Assert.Equal(TimerLaneStates.Superseded, replaced.LaneState);
    }

    [Fact]
    public async Task Upgrade_from_populated_0016_marks_pre_seal_terminal_records_legacy_unsealed()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0016ScriptName);

        var seeded = await SeedPopulated0016TerminalRecordAsync(connectionString);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0020ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var upgraded = await connection.QuerySingleAsync<(
            string ProcedureId,
            string? SealDigest,
            string? ReasonCategory,
            string? AttemptMapping,
            long? CutoffSequence)>(
            """
            SELECT procedure_id, seal_digest, reason_category, attempt_mapping, cutoff_sequence
            FROM session_terminal_records
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            seeded);
        Assert.Null(upgraded.ProcedureId);
        Assert.Null(upgraded.SealDigest);
        Assert.Null(upgraded.ReasonCategory);
        Assert.Null(upgraded.AttemptMapping);
        Assert.Equal(12, upgraded.CutoffSequence);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_frozen_0017_applies_0018_handoff_terminal_binding()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0017ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_frozen_0018_applies_0019_claim_partitions()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0018ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var table = await connection.ExecuteScalarAsync<string?>(
            "SELECT to_regclass('public.session_durable_work_claim_partitions')::text;");
        Assert.Equal("session_durable_work_claim_partitions", table);
        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_frozen_0019_applies_0020_claimable_index()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0019ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var index = await connection.ExecuteScalarAsync<string?>(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname = 'ix_session_durable_work_claimable';
            """);
        Assert.Equal("ix_session_durable_work_claimable", index);
        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_frozen_0020_applies_0021_subject_binding_tables()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0020ScriptName);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var tables = (await connection.QueryAsync<string>(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name IN (
                    'session_frozen_policy_snapshots',
                    'session_actor_relationships');
            """)).AsList();
        Assert.Contains("session_frozen_policy_snapshots", tables);
        Assert.Contains("session_actor_relationships", tables);
        var delegationTables = (await connection.QueryAsync<string>(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name = 'service_delegations';
            """)).AsList();
        Assert.Contains("service_delegations", delegationTables);
        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_populated_0022_unbounded_timer_delegation_fails_closed()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0022ScriptName);

        await SeedPopulated0022TimerDelegationAsync(connectionString, expiresAt: null);

        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
            await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
                connectionString,
                migrationsDirectory,
                TestContext.Current.CancellationToken));

        Assert.Contains("0023 refuses unbounded", exception.MessageText, StringComparison.Ordinal);
        Assert.Contains("refusing fabricated expiry backfill", exception.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upgrade_from_populated_0022_revoked_unbounded_timer_delegation_preserves_history()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0022ScriptName);

        var seeded = await SeedPopulated0022TimerDelegationAsync(
            connectionString,
            expiresAt: null,
            revoked: true);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var row = await connection.QuerySingleAsync<(DateTime? ExpiresAt, DateTime? RevokedAt)>(
            """
            SELECT expires_at, revoked_at
            FROM service_delegations
            WHERE delegation_id = @DelegationId;
            """,
            new { seeded.DelegationId });
        Assert.Null(row.ExpiresAt);
        Assert.NotNull(row.RevokedAt);
        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_populated_0022_bounded_timer_delegation_applies_expiry_guard()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0022ScriptName);

        var effectiveAt = DateTimeOffset.UtcNow;
        await SeedPopulated0022TimerDelegationAsync(connectionString, effectiveAt.AddDays(1), effectiveAt);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var constraint = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM pg_constraint
            WHERE conname = 'chk_service_delegations_timer_lane_fire_expiry';
            """);
        Assert.Equal(1, constraint);
        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);
    }

    [Fact]
    public async Task Upgrade_from_populated_0020_runtime_fails_closed()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0020ScriptName);

        await SeedPopulated0020RuntimeAsync(connectionString);

        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
            await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
                connectionString,
                migrationsDirectory,
                TestContext.Current.CancellationToken));

        Assert.Contains("empty session_runtimes", exception.MessageText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0021", exception.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Upgrade_from_populated_0018_seeds_claimed_partitions_so_fairness_skips_busy_activity()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0018ScriptName);

        var seeded = await SeedPopulated0018ClaimedWorkAsync(connectionString);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0020ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var seededPartition = await connection.QuerySingleAsync<(Guid OrganizationId, Guid WorkId)>(
            """
            SELECT organization_id, last_claimed_work_id
            FROM session_durable_work_claim_partitions
            WHERE organization_id = @OrganizationId
              AND activity_id = @ActivityId;
            """,
            new { OrganizationId = seeded.OrganizationA, ActivityId = seeded.ActivityA });
        Assert.Equal(seeded.OrganizationA, seededPartition.OrganizationId);
        Assert.Equal(seeded.ClaimedWorkId, seededPartition.WorkId);

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var store = new PostgresDurableInvocationWorkStore(new PostgresConnectionAccessor(dataSource));
        var claimed = await store.TryClaimExecuteInvocationAsync(
            TimeSpan.FromSeconds(30),
            TestContext.Current.CancellationToken);

        Assert.Null(claimed);
    }

    [Fact]
    public async Task Upgrade_from_0018_captures_a_claim_held_across_0019_application()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0018ScriptName);

        var seeded = await SeedPopulated0018PendingWorkAsync(connectionString);
        await using var holder = new NpgsqlConnection(connectionString);
        await holder.OpenAsync(TestContext.Current.CancellationToken);
        await using var held = await holder.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var claimed = await holder.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE session_durable_work
                SET
                    state = 'claimed',
                    claim_lease_until = clock_timestamp() + INTERVAL '30 minutes'
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                  AND work_id = @WorkId;
                """,
                new
                {
                    OrganizationId = seeded.OrganizationA,
                    SessionId = seeded.SessionA1,
                    WorkId = seeded.WorkA1,
                },
                held,
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, claimed);

        var applying = GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0020ScriptName);
        await WaitForMigrationLockAsync(connectionString);
        await held.CommitAsync(TestContext.Current.CancellationToken);
        await applying;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var partitionWorkId = await connection.ExecuteScalarAsync<Guid?>(
            """
            SELECT last_claimed_work_id
            FROM session_durable_work_claim_partitions
            WHERE organization_id = @OrganizationId
              AND activity_id = @ActivityId;
            """,
            new { OrganizationId = seeded.OrganizationA, ActivityId = seeded.ActivityA });
        Assert.Equal(seeded.WorkA1, partitionWorkId);

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var store = new PostgresDurableInvocationWorkStore(new PostgresConnectionAccessor(dataSource));
        var next = await store.TryClaimExecuteInvocationAsync(
            TimeSpan.FromSeconds(30),
            TestContext.Current.CancellationToken);

        Assert.Null(next);
    }

    [Fact]
    public async Task Recorded_c861da6_0019_fails_closed_against_current_0019()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var historical0019Sql = await ReadHistoricalFixtureAsync(
            "0019_session_durable_work_claim_partitions_c861da6.sql");

        Assert.NotEqual(
            GrateMigrationRunner.ComputeScriptHash(historical0019Sql),
            GrateMigrationRunner.ComputeScriptHash(
                await File.ReadAllTextAsync(
                    Path.Combine(migrationsDirectory, "up", Current0019ScriptName),
                    TestContext.Current.CancellationToken)));

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0018ScriptName);

        await GrateMigrationRunner.ApplyRecordedMigrationForTestsAsync(
            connectionString,
            Current0019ScriptName,
            historical0019Sql,
            TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
                connectionString,
                migrationsDirectory,
                TestContext.Current.CancellationToken));
        Assert.Contains(Current0019ScriptName, error.Message, StringComparison.Ordinal);
        Assert.Contains("changed after it was applied", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Recorded_e15ed80_0019_fails_closed_against_current_0019()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var historical0019Sql = await ReadHistoricalFixtureAsync(
            "0019_session_durable_work_claim_partitions_e15ed80.sql");

        Assert.NotEqual(
            GrateMigrationRunner.ComputeScriptHash(historical0019Sql),
            GrateMigrationRunner.ComputeScriptHash(
                await File.ReadAllTextAsync(
                    Path.Combine(migrationsDirectory, "up", Current0019ScriptName),
                    TestContext.Current.CancellationToken)));

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0018ScriptName);

        await GrateMigrationRunner.ApplyRecordedMigrationForTestsAsync(
            connectionString,
            Current0019ScriptName,
            historical0019Sql,
            TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
                connectionString,
                migrationsDirectory,
                TestContext.Current.CancellationToken));
        Assert.Contains(Current0019ScriptName, error.Message, StringComparison.Ordinal);
        Assert.Contains("changed after it was applied", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upgrade_from_populated_0017_backfills_existing_evaluation_handoff_binding()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0017ScriptName);

        var seeded = await SeedPopulated0017EvaluationHandoffAsync(connectionString);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0020ScriptName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var upgraded = await connection.QuerySingleAsync<(
            Guid TerminalRecordId,
            string ProcedureId,
            string ConfigurationId,
            string ConfigurationDigest,
            string ManifestId)>(
            """
            SELECT terminal_record_id, procedure_id, configuration_id, configuration_digest, manifest_id
            FROM session_evaluation_handoffs
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            seeded);
        Assert.Equal(seeded.TerminalRecordId, upgraded.TerminalRecordId);
        Assert.Equal("manifest-jcs-sha256-v2", upgraded.ProcedureId);
        Assert.Equal("cfg-1", upgraded.ConfigurationId);
        Assert.Equal(new string('a', 64), upgraded.ConfigurationDigest);
        Assert.Equal("man-1", upgraded.ManifestId);

        var appendOnly = await Assert.ThrowsAsync<PostgresException>(() =>
            connection.ExecuteAsync(
                """
                UPDATE session_evaluation_handoffs
                SET eligibility = eligibility
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId;
                """,
                seeded));
        Assert.Contains("append-only", appendOnly.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Recorded_aa424f3_0017_fails_closed_against_frozen_0017()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var historical0017Sql = await ReadHistoricalFixtureAsync(
            "0017_session_manifest_runtime_and_handoff_aa424f3.sql");

        Assert.NotEqual(
            GrateMigrationRunner.ComputeScriptHash(historical0017Sql),
            GrateMigrationRunner.ComputeScriptHash(
                await File.ReadAllTextAsync(
                    Path.Combine(migrationsDirectory, "up", Current0017ScriptName),
                    TestContext.Current.CancellationToken)));

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0016ScriptName);

        await GrateMigrationRunner.ApplyRecordedMigrationForTestsAsync(
            connectionString,
            Current0017ScriptName,
            historical0017Sql,
            TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
                connectionString,
                migrationsDirectory,
                TestContext.Current.CancellationToken));
        Assert.Contains(Current0017ScriptName, error.Message, StringComparison.Ordinal);
        Assert.Contains("changed after it was applied", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Recorded_ddd7c0a_0018_fails_closed_against_current_0018()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var historical0018Sql = await ReadHistoricalFixtureAsync(
            "0018_session_evaluation_handoff_terminal_binding_ddd7c0a.sql");

        Assert.NotEqual(
            GrateMigrationRunner.ComputeScriptHash(historical0018Sql),
            GrateMigrationRunner.ComputeScriptHash(
                await File.ReadAllTextAsync(
                    Path.Combine(migrationsDirectory, "up", Current0018ScriptName),
                    TestContext.Current.CancellationToken)));

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0017ScriptName);

        await GrateMigrationRunner.ApplyRecordedMigrationForTestsAsync(
            connectionString,
            Current0018ScriptName,
            historical0018Sql,
            TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
                connectionString,
                migrationsDirectory,
                TestContext.Current.CancellationToken));
        Assert.Contains(Current0018ScriptName, error.Message, StringComparison.Ordinal);
        Assert.Contains("changed after it was applied", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upgrade_from_populated_0005_runtime_fails_closed()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0005ScriptName);

        await SeedPopulated0005RuntimeAsync(connectionString);

        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
            await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
                connectionString,
                migrationsDirectory,
                TestContext.Current.CancellationToken));

        Assert.Contains("empty Session runtime tables", exception.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upgrade_from_recorded_historical_0002_repairs_via_0003_without_checksum_failure()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var historical0002Sql = await ReadHistoricalFixtureAsync("0002_idempotency_and_version_immutability_4e21917.sql");

        Assert.Equal(
            GrateMigrationRunner.ComputeScriptHash(historical0002Sql),
            GrateMigrationRunner.ComputeScriptHash(
                await File.ReadAllTextAsync(
                    Path.Combine(migrationsDirectory, "up", Historical0002ScriptName),
                    TestContext.Current.CancellationToken)));

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: "0001_initial_authorization_configuration_schema.sql");

        var seededState = await SeedLegacyVersionAsync(connectionString);

        await GrateMigrationRunner.ApplyRecordedMigrationForTestsAsync(
            connectionString,
            Historical0002ScriptName,
            historical0002Sql,
            TestContext.Current.CancellationToken);

        await AssertIdempotencyRowCountAsync(connectionString, seededState, expectedCount: 0);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);

        await AssertRepairEvidenceAsync(connectionString, seededState);
    }

    [Fact]
    public async Task Upgrade_from_recorded_historical_0003_applies_0004_without_checksum_failure()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var historical0002Sql = await ReadHistoricalFixtureAsync("0002_idempotency_and_version_immutability_4e21917.sql");
        var historical0003Sql = await ReadHistoricalFixtureAsync("0003_repair_idempotency_backfill_and_source_version_fk_d244a6a.sql");

        Assert.Equal(
            GrateMigrationRunner.ComputeScriptHash(historical0003Sql),
            GrateMigrationRunner.ComputeScriptHash(
                await File.ReadAllTextAsync(
                    Path.Combine(migrationsDirectory, "up", Historical0003ScriptName),
                    TestContext.Current.CancellationToken)));

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: "0001_initial_authorization_configuration_schema.sql");

        var seededState = await SeedLegacyVersionAsync(connectionString);

        await GrateMigrationRunner.ApplyRecordedMigrationForTestsAsync(
            connectionString,
            Historical0002ScriptName,
            historical0002Sql,
            TestContext.Current.CancellationToken);

        await GrateMigrationRunner.ApplyRecordedMigrationForTestsAsync(
            connectionString,
            Historical0003ScriptName,
            historical0003Sql,
            TestContext.Current.CancellationToken);

        await AssertIdempotencyRowCountAsync(connectionString, seededState, expectedCount: 1);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await AssertAppliedScriptsAsync(
            connectionString,
            "0001_initial_authorization_configuration_schema.sql",
            Historical0002ScriptName,
            Historical0003ScriptName,
            Current0004ScriptName,
            Current0005ScriptName,
            Current0006ScriptName,
            Current0007ScriptName,
            Current0008ScriptName,
            Current0009ScriptName,
            Current0010ScriptName,
            Current0011ScriptName,
            Current0012ScriptName,
            Current0013ScriptName,
            Current0014ScriptName,
            Current0015ScriptName,
            Current0016ScriptName,
            Current0017ScriptName,
            Current0018ScriptName,
            Current0019ScriptName,
            Current0020ScriptName,
            Current0021ScriptName,
            Current0022ScriptName,
            Current0023ScriptName,
            Current0024ScriptName,
            Current0025ScriptName,
            Current0026ScriptName,
            Current0027ScriptName,
            Current0028ScriptName,
            Current0029ScriptName,
            Current0030ScriptName,
            Current0031ScriptName,
            Current0032ScriptName,
            Current0033ScriptName,
            Current0034ScriptName,
            Current0035ScriptName,
            Current0036ScriptName,
            Current0037ScriptName,
            Current0038ScriptName,
            Current0039ScriptName,
            Current0040ScriptName,
            Current0041ScriptName,
            Current0042ScriptName,
            Current0043ScriptName,
            Current0044ScriptName,
            Current0045ScriptName,
            Current0046ScriptName,
            Current0047ScriptName,
            Current0048ScriptName,
            Current0049ScriptName,
            Current0050ScriptName,
            Current0051ScriptName,
            Current0052ScriptName,
            Current0053ScriptName,
            Current0054ScriptName,
            Current0055ScriptName,
            Current0056aScriptName,
            Current0056ScriptName,
            Current0057ScriptName,
            Current0058ScriptName,
            Current0059ScriptName,
            Current0060ScriptName,
            Current0061ScriptName,
            Current0062ScriptName,
            Current0063ScriptName,
            Current0064ScriptName,
            Current0065ScriptName,
            Current0066ScriptName,
            Current0067ScriptName,
            Current0068ScriptName,
            Current0069ScriptName);

        await AssertRepairEvidenceAsync(connectionString, seededState);
    }

    [Fact]
    public async Task Upgrade_from_populated_0026_backfills_provider_request_identity()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            inclusiveMaxScriptName: Current0026ScriptName);

        var seeded = await SeedPopulated0026ProviderAttemptAsync(connectionString);

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var row = await connection.QuerySingleAsync<(string ProviderRequestId, string Phase, int ProviderRequestOrdinal, string FactKind, string AdapterKind, string AdapterContractVersion)>(
            """
            SELECT provider_request_id, phase, provider_request_ordinal, fact_kind,
                   adapter_kind, adapter_contract_version
            FROM session_invocation_provider_attempts
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND agent_invocation_id = @InvocationId;
            """,
            new
            {
                seeded.OrganizationId,
                seeded.SessionId,
                seeded.InvocationId,
            });

        Assert.Equal($"prat.migrated.{seeded.InvocationId}.1", row.ProviderRequestId);
        Assert.Equal("control", row.Phase);
        Assert.Equal(1, row.ProviderRequestOrdinal);
        Assert.Equal("finished", row.FactKind);
        Assert.Equal("direct_openai", row.AdapterKind);
        Assert.Equal("sessions.openai.v1", row.AdapterContractVersion);
    }

    // Polls live PostgreSQL clock_timestamp(); callers may wait almost one
    // minute to enter a short second-of-minute slot.
    private static async Task WaitUntilEpochSecondInRangeAsync(
        NpgsqlConnection connection,
        int inclusiveStart,
        int inclusiveEnd,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var second = await connection.ExecuteScalarAsync<long>(
                "SELECT floor(extract(epoch FROM clock_timestamp()))::bigint % 60;");
            if (second >= inclusiveStart && second <= inclusiveEnd)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task AssertAppliedScriptsAsync(
        string connectionString,
        params string[] expectedScripts)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var appliedScripts = (await connection.QueryAsync<string>(
            "SELECT script_name FROM grate_migrations ORDER BY script_name;")).AsList();

        Assert.Equal(expectedScripts, appliedScripts);
    }

    private static async Task AssertIdempotencyRowCountAsync(
        string connectionString,
        LegacyVersionSeed seededState,
        int expectedCount)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var idempotencyCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM configuration_source_version_idempotency
            WHERE organization_id = @OrganizationId
              AND configuration_source_id = @ConfigurationSourceId
              AND idempotency_key = @IdempotencyKey;
            """,
            new
            {
                seededState.OrganizationId,
                ConfigurationSourceId = seededState.SourceId,
                seededState.IdempotencyKey,
            });

        Assert.Equal(expectedCount, idempotencyCount);
    }

    private static async Task<string> ReadHistoricalFixtureAsync(string fileName) =>
        await File.ReadAllTextAsync(
            Path.Combine(
                FindRepositoryRoot(),
                "tests",
                "Integration",
                "FlexAgent.Postgres.Integration.Tests",
                "Fixtures",
                "migrations",
                fileName),
            TestContext.Current.CancellationToken);

    private static async Task<(Guid DelegationId, Guid OrganizationId)> SeedPopulated0022TimerDelegationAsync(
        string connectionString,
        DateTimeOffset? expiresAt,
        DateTimeOffset? effectiveAt = null,
        bool revoked = false)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var delegationId = Guid.NewGuid();
        var createdAt = effectiveAt ?? DateTimeOffset.UtcNow;
        var digest = new string('a', 64);

        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
            INSERT INTO actors (id, created_at) VALUES (@ActorId, @CreatedAt);
            INSERT INTO session_runtimes (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                configuration_id, configuration_digest, manifest_id, lifecycle_state)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'cfg-1', @Digest, 'man-1', 'active');
            INSERT INTO service_delegations (
                delegation_id, organization_id, activity_id, participant_id, attempt_id, session_id,
                service_actor_id, allowed_action, system_purpose, initiating_authority,
                effective_at, expires_at, revoked_at, delegation_version, created_at)
            VALUES (
                @DelegationId, @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @ActorId, @AllowedAction, 'session.timer_lane.scheduler', 'system.session_runtime',
                @EffectiveAt, @ExpiresAt, @RevokedAt, 1, @CreatedAt);
            """,
            new
            {
                OrganizationId = organizationId,
                ActorId = actorId,
                ActivityId = activityId,
                ParticipantId = participantId,
                AttemptId = attemptId,
                SessionId = sessionId,
                DelegationId = delegationId,
                Digest = digest,
                AllowedAction = AuthorizationActions.FireSessionTimerLane,
                EffectiveAt = createdAt,
                ExpiresAt = expiresAt,
                RevokedAt = revoked ? createdAt : (DateTimeOffset?)null,
                CreatedAt = createdAt,
            });
        return (delegationId, organizationId);
    }

    private static async Task<(Guid OrganizationId, Guid SessionId, string InvocationId)> SeedPopulated0026ProviderAttemptAsync(
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var organizationId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var invocationId = "ainv.populated0026";
        var digest = new string('a', 64);
        var startedAt = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
            INSERT INTO session_runtimes (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                configuration_id, configuration_digest, manifest_id, lifecycle_state)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'cfg-1', @Digest, 'man-1', 'active');
            INSERT INTO session_invocation_provider_attempts (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, attempt_ordinal, adapter_kind, adapter_contract_version,
                profile_id, profile_version, profile_digest, requested_model, resolved_model_version,
                outcome_category, started_at, completed_at)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @InvocationId, 1, 'direct_openai', 'sessions.openai.v1',
                'direct-openai.unqualified.example', '1', @Digest, 'gpt-alias', 'gpt-pinned',
                'decision_produced', @StartedAt, @StartedAt);
            """,
            new
            {
                OrganizationId = organizationId,
                ActivityId = activityId,
                ParticipantId = participantId,
                AttemptId = attemptId,
                SessionId = sessionId,
                InvocationId = invocationId,
                Digest = digest,
                CreatedAt = startedAt,
                StartedAt = startedAt,
            });
        return (organizationId, sessionId, invocationId);
    }

    private static async Task SeedPopulated0020RuntimeAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var organizationId = Guid.NewGuid();
        var digest = new string('a', 64);

        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
            INSERT INTO session_runtimes (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                configuration_id, configuration_digest, manifest_id, lifecycle_state)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'cfg-1', @Digest, 'man-1', 'active');
            """,
            new
            {
                OrganizationId = organizationId,
                ActivityId = Guid.NewGuid(),
                ParticipantId = Guid.NewGuid(),
                AttemptId = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                Digest = digest,
                CreatedAt = DateTimeOffset.UtcNow,
            });
    }

    private static async Task SeedPopulated0005RuntimeAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var organizationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var digest = new string('a', 64);

        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
            INSERT INTO session_runtimes (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                configuration_id, configuration_digest, manifest_id, lifecycle_state)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'cfg-1', @Digest, 'man-1', 'active');
            INSERT INTO session_invocations (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, trigger_family, trigger_type, trigger_id, purpose,
                idempotency_key, policy_digest, admitted_session_sequence, status)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'inv-1', 'participant_input', 'participant_message', 'trig-1',
                'participant_turn.respond', 'idem-1', @Digest, 1, 'admitted');
            INSERT INTO session_decisions (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, decision_id, decision_type, produced_at, payload_digest)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'inv-1', 'dec-1', 'no_action', @CreatedAt, @Digest);
            INSERT INTO session_decision_validations (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, revision_ordinal,
                validated_against_session_version, validated_against_session_sequence,
                validation_commit_session_version, validation_commit_session_sequence,
                validation_outcome, effect_outcome, timer_validation_outcome)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'inv-1', 1, 0, 1, 1, 2, 'accepted', 'not_attempted', 'not_present');
            UPDATE session_decision_validations
            SET effect_outcome = 'applied',
                applied_turn_id = 'turn.1',
                applied_response_slot_id = 'slot.1'
            WHERE agent_invocation_id = 'inv-1';
            """,
            new
            {
                OrganizationId = organizationId,
                ActivityId = Guid.NewGuid(),
                ParticipantId = Guid.NewGuid(),
                AttemptId = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                Digest = digest,
                CreatedAt = now,
            });
    }

    private static async Task<Populated0012PublicationSeed> SeedPopulated0012AgentFragmentsAsync(
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var organizationId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        const string messageId = "aout.roundtrip.0001";
        var digest = new string('a', 64);
        var helDigest = Convert.ToHexString(SHA256.HashData("Hel"u8.ToArray())).ToLowerInvariant();
        var loDigest = Convert.ToHexString(SHA256.HashData("lo"u8.ToArray())).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
            INSERT INTO session_runtimes (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                configuration_id, configuration_digest, manifest_id, lifecycle_state)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'cfg-1', @Digest, 'man-1', 'active');
            INSERT INTO session_invocations (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, trigger_family, trigger_type, trigger_id, purpose,
                idempotency_key, policy_digest, admitted_session_sequence, status)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'inv-1', 'participant_input', 'participant_message', 'trig-1',
                'participant_turn.respond', 'idem-1', @Digest, 1, 'admitted');
            INSERT INTO session_decisions (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, decision_id, decision_type, produced_at, payload_digest,
                decision_payload_digest_version, committed_session_version, committed_session_sequence)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'inv-1', 'dec-1', 'no_action', @CreatedAt, @Digest, @DigestVersion, 1, 2);
            INSERT INTO session_decision_validations (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, revision_ordinal,
                validated_against_session_version, validated_against_session_sequence,
                validation_commit_session_version, validation_commit_session_sequence,
                validation_outcome, effect_outcome, timer_validation_outcome)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'inv-1', 1, 0, 1, 1, 2, 'accepted', 'not_attempted', 'not_present');
            INSERT INTO session_decision_output_validations (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, revision_ordinal, item_ordinal, local_ref, kind,
                validation_outcome, rejection_reason_category, agent_output_id, effect_outcome)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'inv-1', 1, 0, 'out.message.primary', 'message',
                'accepted', NULL, @MessageId, 'not_attempted');
            INSERT INTO session_messages (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                message_id, author_type, turn_id, protected_ref, content_digest, completion_state,
                generation_attempt_id, driving_invocation_id, driving_decision_id,
                accepted_agent_output_id, assembled_content_digest)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @MessageId, 'agent', 'turn.1', @ProtectedRef, @MessageDigest, 'open',
                'agen.1', 'inv-1', 'dec-1', @MessageId, NULL);
            INSERT INTO session_message_fragments (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                message_id, fragment_ordinal, session_sequence, turn_id, response_slot_id,
                generation_attempt_id, protected_ref, content_digest, exact_utf8_text,
                driving_invocation_id, driving_decision_id, accepted_agent_output_id)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @MessageId, 1, 1, 'turn.1', 'slot.1',
                'agen.1', @Fragment1Ref, @HelDigest, 'Hel',
                'inv-1', 'dec-1', @MessageId),
                (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @MessageId, 2, 2, 'turn.1', 'slot.1',
                'agen.1', @Fragment2Ref, @LoDigest, 'lo',
                'inv-1', 'dec-1', NULL);
            """,
            new
            {
                OrganizationId = organizationId,
                ActivityId = activityId,
                ParticipantId = participantId,
                AttemptId = attemptId,
                SessionId = sessionId,
                MessageId = messageId,
                Digest = digest,
                DigestVersion = DecisionPayloadDigest.FormatVersionV1,
                CreatedAt = now,
                ProtectedRef = $"msg:{messageId}",
                MessageDigest = digest,
                Fragment1Ref = $"frag:{messageId}:1",
                Fragment2Ref = $"frag:{messageId}:2",
                HelDigest = helDigest,
                LoDigest = loDigest,
            });

        var nullSlot = await connection.ExecuteScalarAsync<string?>(
            """
            SELECT response_slot_id
            FROM session_messages
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND message_id = @MessageId;
            """,
            new
            {
                OrganizationId = organizationId,
                SessionId = sessionId,
                MessageId = messageId,
            });
        Assert.Null(nullSlot);

        return new Populated0012PublicationSeed(organizationId, sessionId, messageId);
    }

    private static async Task<Populated0015TimerSeed> SeedPopulated0015TimerSchedulesAsync(
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var organizationId = Guid.NewGuid();
        var defaultSessionId = Guid.NewGuid();
        var recommendedSessionId = Guid.NewGuid();
        var replacedSessionId = Guid.NewGuid();
        var digest = new string('a', 64);
        var now = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
            INSERT INTO session_runtimes (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                configuration_id, configuration_digest, manifest_id, lifecycle_state)
            VALUES
                (@OrganizationId, @DefaultActivityId, @DefaultParticipantId, @DefaultAttemptId, @DefaultSessionId,
                 'cfg-1', @Digest, 'man-1', 'active'),
                (@OrganizationId, @RecommendedActivityId, @RecommendedParticipantId, @RecommendedAttemptId, @RecommendedSessionId,
                 'cfg-1', @Digest, 'man-1', 'active'),
                (@OrganizationId, @ReplacedActivityId, @ReplacedParticipantId, @ReplacedAttemptId, @ReplacedSessionId,
                 'cfg-1', @Digest, 'man-1', 'active');
            INSERT INTO session_timer_schedules (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                schedule_revision, state, relative_delay, fire_at, source_decision_id)
            VALUES
                (@OrganizationId, @DefaultActivityId, @DefaultParticipantId, @DefaultAttemptId, @DefaultSessionId,
                 'tsrev.default', 'pending', 'PT10M', clock_timestamp() + INTERVAL '10 minutes', NULL),
                (@OrganizationId, @RecommendedActivityId, @RecommendedParticipantId, @RecommendedAttemptId, @RecommendedSessionId,
                 'tsrev.recommended', 'pending', 'PT10M', clock_timestamp() + INTERVAL '10 minutes', 'dec-hist-1'),
                (@OrganizationId, @ReplacedActivityId, @ReplacedParticipantId, @ReplacedAttemptId, @ReplacedSessionId,
                 'tsrev.replaced', 'replaced', 'PT10M', NULL, 'dec-hist-2');
            """,
            new
            {
                OrganizationId = organizationId,
                DefaultActivityId = Guid.NewGuid(),
                DefaultParticipantId = Guid.NewGuid(),
                DefaultAttemptId = Guid.NewGuid(),
                DefaultSessionId = defaultSessionId,
                RecommendedActivityId = Guid.NewGuid(),
                RecommendedParticipantId = Guid.NewGuid(),
                RecommendedAttemptId = Guid.NewGuid(),
                RecommendedSessionId = recommendedSessionId,
                ReplacedActivityId = Guid.NewGuid(),
                ReplacedParticipantId = Guid.NewGuid(),
                ReplacedAttemptId = Guid.NewGuid(),
                ReplacedSessionId = replacedSessionId,
                Digest = digest,
                CreatedAt = now,
            });

        return new Populated0015TimerSeed(organizationId, defaultSessionId, recommendedSessionId, replacedSessionId);
    }

    private static async Task<Populated0016TerminalSeed> SeedPopulated0016TerminalRecordAsync(
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var organizationId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var digest = new string('a', 64);

        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
            INSERT INTO session_runtimes (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                configuration_id, configuration_digest, manifest_id, lifecycle_state,
                cutoff_sequence)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'cfg-1', @Digest, 'man-1', 'completed', 12);
            INSERT INTO session_terminal_records (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                terminal_record_id, lifecycle_state, cutoff_sequence)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @TerminalRecordId, 'completed', 12);
            """,
            new
            {
                OrganizationId = organizationId,
                ActivityId = activityId,
                ParticipantId = participantId,
                AttemptId = attemptId,
                SessionId = sessionId,
                TerminalRecordId = Guid.NewGuid(),
                Digest = digest,
                CreatedAt = DateTimeOffset.UtcNow,
            });

        return new Populated0016TerminalSeed(organizationId, sessionId);
    }

    private static async Task<Populated0017HandoffSeed> SeedPopulated0017EvaluationHandoffAsync(
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var organizationId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var terminalRecordId = Guid.NewGuid();
        var digest = new string('a', 64);

        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
            INSERT INTO session_runtimes (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                configuration_id, configuration_digest, manifest_id, lifecycle_state,
                cutoff_sequence)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'cfg-1', @Digest, 'man-1', 'completed', 12);
            INSERT INTO session_terminal_records (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                terminal_record_id, lifecycle_state, reason_category, attempt_mapping,
                cutoff_sequence, procedure_id, seal_digest)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @TerminalRecordId, 'completed', 'participant_completed', 'completed',
                12, 'manifest-jcs-sha256-v2', @Digest);
            INSERT INTO session_evaluation_handoffs (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                handoff_id, eligibility, terminal_state, cutoff_sequence,
                configuration_id, configuration_digest, manifest_id, seal_digest)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'eho.upgrade000000000000000000000000', 'eligible', 'completed', 12,
                'cfg-1', @Digest, 'man-1', @Digest);
            """,
            new
            {
                OrganizationId = organizationId,
                ActivityId = activityId,
                ParticipantId = participantId,
                AttemptId = attemptId,
                SessionId = sessionId,
                TerminalRecordId = terminalRecordId,
                Digest = digest,
                CreatedAt = DateTimeOffset.UtcNow,
            });

        return new Populated0017HandoffSeed(organizationId, sessionId, terminalRecordId);
    }

    private static async Task<Populated0018ClaimedWorkSeed> SeedPopulated0018ClaimedWorkAsync(
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var activityA = Guid.NewGuid();
        var activityB = Guid.NewGuid();
        var claimedWorkId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
        var pendingAWorkId = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
        var pendingBWorkId = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
        var digest = new string('a', 64);
        var createdAt = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES
                (@OrganizationA, @CreatedAt),
                (@OrganizationB, @CreatedAt);
            INSERT INTO session_runtimes (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                configuration_id, configuration_digest, manifest_id, lifecycle_state)
            VALUES
                (@OrganizationA, @ActivityA, @ParticipantA1, @AttemptA1, @SessionA1,
                 'cfg-1', @Digest, 'man-1', 'active'),
                (@OrganizationA, @ActivityA, @ParticipantA2, @AttemptA2, @SessionA2,
                 'cfg-1', @Digest, 'man-1', 'active'),
                (@OrganizationB, @ActivityB, @ParticipantB, @AttemptB, @SessionB,
                 'cfg-1', @Digest, 'man-1', 'active');
            INSERT INTO session_durable_work (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                work_id, work_type, business_key, state, claim_lease_until)
            VALUES
                (@OrganizationA, @ActivityA, @ParticipantA1, @AttemptA1, @SessionA1,
                 @ClaimedWorkId, 'invocation.execute', 'ainv.upgrade.a1', 'claimed',
                 clock_timestamp() + INTERVAL '30 minutes'),
                (@OrganizationA, @ActivityA, @ParticipantA2, @AttemptA2, @SessionA2,
                 @PendingAWorkId, 'invocation.execute', 'ainv.upgrade.a2', 'pending', NULL),
                (@OrganizationB, @ActivityB, @ParticipantB, @AttemptB, @SessionB,
                 @PendingBWorkId, 'invocation.execute', 'ainv.upgrade.b1', 'pending', NULL);
            """,
            new
            {
                OrganizationA = organizationA,
                OrganizationB = organizationB,
                ActivityA = activityA,
                ActivityB = activityB,
                ParticipantA1 = Guid.NewGuid(),
                ParticipantA2 = Guid.NewGuid(),
                ParticipantB = Guid.NewGuid(),
                AttemptA1 = Guid.NewGuid(),
                AttemptA2 = Guid.NewGuid(),
                AttemptB = Guid.NewGuid(),
                SessionA1 = Guid.NewGuid(),
                SessionA2 = Guid.NewGuid(),
                SessionB = Guid.NewGuid(),
                ClaimedWorkId = claimedWorkId,
                PendingAWorkId = pendingAWorkId,
                PendingBWorkId = pendingBWorkId,
                Digest = digest,
                CreatedAt = createdAt,
            });

        return new Populated0018ClaimedWorkSeed(
            organizationA,
            organizationB,
            activityA,
            claimedWorkId,
            pendingBWorkId);
    }

    private static async Task<Populated0018PendingWorkSeed> SeedPopulated0018PendingWorkAsync(
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var activityA = Guid.NewGuid();
        var activityB = Guid.NewGuid();
        var sessionA1 = Guid.NewGuid();
        var workA1 = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
        var pendingAWorkId = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
        var pendingBWorkId = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
        var digest = new string('a', 64);
        var createdAt = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES
                (@OrganizationA, @CreatedAt),
                (@OrganizationB, @CreatedAt);
            INSERT INTO session_runtimes (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                configuration_id, configuration_digest, manifest_id, lifecycle_state)
            VALUES
                (@OrganizationA, @ActivityA, @ParticipantA1, @AttemptA1, @SessionA1,
                 'cfg-1', @Digest, 'man-1', 'active'),
                (@OrganizationA, @ActivityA, @ParticipantA2, @AttemptA2, @SessionA2,
                 'cfg-1', @Digest, 'man-1', 'active'),
                (@OrganizationB, @ActivityB, @ParticipantB, @AttemptB, @SessionB,
                 'cfg-1', @Digest, 'man-1', 'active');
            INSERT INTO session_durable_work (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                work_id, work_type, business_key, state, claim_lease_until)
            VALUES
                (@OrganizationA, @ActivityA, @ParticipantA1, @AttemptA1, @SessionA1,
                 @WorkA1, 'invocation.execute', 'ainv.upgrade.hold.a1', 'pending', NULL),
                (@OrganizationA, @ActivityA, @ParticipantA2, @AttemptA2, @SessionA2,
                 @PendingAWorkId, 'invocation.execute', 'ainv.upgrade.hold.a2', 'pending', NULL),
                (@OrganizationB, @ActivityB, @ParticipantB, @AttemptB, @SessionB,
                 @PendingBWorkId, 'invocation.execute', 'ainv.upgrade.hold.b1', 'pending', NULL);
            """,
            new
            {
                OrganizationA = organizationA,
                OrganizationB = organizationB,
                ActivityA = activityA,
                ActivityB = activityB,
                ParticipantA1 = Guid.NewGuid(),
                ParticipantA2 = Guid.NewGuid(),
                ParticipantB = Guid.NewGuid(),
                AttemptA1 = Guid.NewGuid(),
                AttemptA2 = Guid.NewGuid(),
                AttemptB = Guid.NewGuid(),
                SessionA1 = sessionA1,
                SessionA2 = Guid.NewGuid(),
                SessionB = Guid.NewGuid(),
                WorkA1 = workA1,
                PendingAWorkId = pendingAWorkId,
                PendingBWorkId = pendingBWorkId,
                Digest = digest,
                CreatedAt = createdAt,
            });

        return new Populated0018PendingWorkSeed(
            organizationA,
            organizationB,
            activityA,
            sessionA1,
            workA1,
            pendingBWorkId);
    }

    private static async Task WaitForMigrationLockAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var waiting = await connection.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_locks
                    WHERE NOT granted
                );
                """);
            if (waiting)
            {
                return;
            }

            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException("0019 application did not wait on a lock while a claim transaction was held.");
    }

    private static async Task<LegacyVersionSeed> SeedLegacyVersionAsync(string connectionString)
    {
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        const string idempotencyKey = "upgrade-key-1";
        var digest = PostgresIntegrationFixture.MinimalStableDomainDigest;
        var now = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
            INSERT INTO actors (id, created_at) VALUES (@ActorId, @CreatedAt);
            INSERT INTO actor_organization_grants (
                organization_id, actor_id, relationship_version, granted_action, created_at)
            VALUES (
                @OrganizationId, @ActorId, 1, @GrantedAction, @CreatedAt);
            INSERT INTO configuration_sources (id, organization_id, source_kind, created_at)
            VALUES (@SourceId, @OrganizationId, @SourceKind, @CreatedAt);
            INSERT INTO configuration_source_versions (
                id,
                organization_id,
                configuration_source_id,
                schema_version,
                procedure_id,
                content_digest,
                idempotency_key,
                created_at)
            VALUES (
                @VersionId,
                @OrganizationId,
                @SourceId,
                @SchemaVersion,
                @ProcedureId,
                @ContentDigest,
                @IdempotencyKey,
                @CreatedAt);
            """,
            new
            {
                OrganizationId = organizationId,
                ActorId = actorId,
                SourceId = sourceId,
                VersionId = versionId,
                GrantedAction = AuthorizationActions.RegisterConfigurationSourceVersion,
                SourceKind = ConfigurationSourceKinds.SyntheticV1,
                SchemaVersion = ConfigurationSchemaVersions.V1,
                ProcedureId = ConfigurationProcedureIds.RscJcsSha256V1,
                ContentDigest = digest,
                IdempotencyKey = idempotencyKey,
                CreatedAt = now,
            });

        return new LegacyVersionSeed(
            organizationId,
            actorId,
            sourceId,
            versionId,
            idempotencyKey,
            digest);
    }

    private static async Task AssertRepairEvidenceAsync(string connectionString, LegacyVersionSeed seededState)
    {
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var expectedFingerprint = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        $"{ConfigurationProcedureIds.RscJcsSha256V1}|{ConfigurationSchemaVersions.V1}|{seededState.Digest}")))
            .ToLowerInvariant();

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var backfilledFingerprint = await connection.ExecuteScalarAsync<string>(
                """
                SELECT payload_fingerprint
                FROM configuration_source_version_idempotency
                WHERE organization_id = @OrganizationId
                  AND configuration_source_id = @ConfigurationSourceId
                  AND idempotency_key = @IdempotencyKey;
                """,
                new
                {
                    seededState.OrganizationId,
                    ConfigurationSourceId = seededState.SourceId,
                    seededState.IdempotencyKey,
                });

            Assert.Equal(expectedFingerprint, backfilledFingerprint);
        }

        var services = ConfigurationServiceCollection.Create(connectionString);
        var seeded = new SeededOrganization(
            seededState.OrganizationId,
            seededState.ActorId,
            seededState.SourceId,
            new TrustedActor(seededState.ActorId, "synthetic.test_actor"),
            new OrganizationScope(seededState.OrganizationId));

        var alternateContent = Encoding.UTF8.GetBytes(
            """
            {"canonicalization_version":"rfc8785","effective_configuration":{"domains":[{"domain_key":"memory_mode","effective_value":{"mode":"strict"},"provenance_classification":"inherited"}]},"procedure_id":"rsc-jcs-sha256-v1","resolution_decisions":[{"decision_key":"memory_mode","outcome":"stable_required"}],"schema_version":"v1","source_references":[{"content_digest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","source_id":"agent.synth.02","source_key":"agent","source_version":"rev.0002"}]}
            """);
        var alternateDigest = FlexAgent.CanonicalJson.CanonicalJsonProcessor.CanonicalizeSha256Hex(
            alternateContent,
            new FlexAgent.CanonicalJson.CanonicalJsonLimits(65_536, 64, 4_096, 4_096));

        var conflict = await services.RegisterHandler.HandleAsync(
            new RegisterConfigurationSourceVersionCommand(
                seeded.Actor,
                seeded.Scope,
                seeded.ConfigurationSourceId,
                ConfigurationProcedureIds.RscJcsSha256V1,
                ConfigurationSchemaVersions.V1,
                alternateContent,
                alternateDigest,
                seededState.IdempotencyKey,
                Guid.NewGuid(),
                "integration.test"),
            TestContext.Current.CancellationToken);

        Assert.False(conflict.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.IdempotencyConflict, conflict.OutcomeCode);

        var idempotentRetry = await services.RegisterHandler.HandleAsync(
            new RegisterConfigurationSourceVersionCommand(
                seeded.Actor,
                seeded.Scope,
                seeded.ConfigurationSourceId,
                ConfigurationProcedureIds.RscJcsSha256V1,
                ConfigurationSchemaVersions.V1,
                content,
                seededState.Digest,
                seededState.IdempotencyKey,
                Guid.NewGuid(),
                "integration.test"),
            TestContext.Current.CancellationToken);

        Assert.True(idempotentRetry.Succeeded);
        Assert.Equal(seededState.VersionId, idempotentRetry.Identity!.VersionId);
        Assert.Equal(1, await services.VersionRepository.CountForSourceAsync(
            seededState.OrganizationId,
            seededState.SourceId,
            TestContext.Current.CancellationToken));

        await using var verifyConnection = await services.ConnectionAccessor.OpenConnectionAsync(
            TestContext.Current.CancellationToken);
        var idempotencyCount = await verifyConnection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM configuration_source_version_idempotency
            WHERE organization_id = @OrganizationId
              AND configuration_source_id = @ConfigurationSourceId
              AND idempotency_key = @IdempotencyKey;
            """,
            new
            {
                seededState.OrganizationId,
                ConfigurationSourceId = seededState.SourceId,
                seededState.IdempotencyKey,
            });

        Assert.Equal(1, idempotencyCount);
    }

    [Fact]
    public async Task Upgrade_from_populated_0064_backfills_attempt_binding_digest_and_restores_append_only()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations");
        var cancellationToken = TestContext.Current.CancellationToken;

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            inclusiveMaxScriptName: Current0064ScriptName);

        var organizationId = Guid.CreateVersion7();
        var activityId = Guid.CreateVersion7();
        var cohortId = Guid.CreateVersion7();
        var baselineId = Guid.CreateVersion7();
        var enrollmentId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var taskSourceId = Guid.CreateVersion7();
        var taskVersionId = Guid.CreateVersion7();
        var submissionId = Guid.CreateVersion7();
        var versionId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var attemptId = Guid.CreateVersion7();
        var digest = new string('a', 64);
        var now = DateTimeOffset.Parse("2026-09-02T12:00:00Z");

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(
                """
                SET session_replication_role = replica;
                INSERT INTO submissions_accepted_versions (
                    organization_id, submission_id, version_id, version_number, activity_id, cohort_id,
                    baseline_id, enrollment_id, participant_actor_id, task_source_id, task_version_id,
                    task_content_digest, policy_digest, predecessor_version_id, accepted_at, accepted_by_actor_id)
                VALUES (
                    @OrganizationId, @SubmissionId, @VersionId, 1, @ActivityId, @CohortId,
                    @BaselineId, @EnrollmentId, @ParticipantId, @TaskSourceId, @TaskVersionId,
                    @Digest, @Digest, NULL, @AcceptedAt, @ParticipantId);
                INSERT INTO submissions_accepted_version_items (
                    organization_id, version_id, item_id, category, filename, byte_count, content_digest,
                    artifact_object_key, artifact_version_id)
                VALUES (
                    @OrganizationId, @VersionId, @ItemId, 'direct_text', NULL, 12, @Digest,
                    @ArtifactObjectKey, 'v1');
                INSERT INTO submissions_attempts (
                    organization_id, attempt_id, activity_id, cohort_id, baseline_id, enrollment_id,
                    participant_actor_id, task_source_id, ordinal, entitlement_source, retry_entitlement_id,
                    status, consumed, requested_at, started_at, terminal_at, terminal_reason_category,
                    session_id, resolved_configuration_id, initial_manifest_id, configuration_digest, manifest_digest)
                VALUES (
                    @OrganizationId, @AttemptId, @ActivityId, @CohortId, @BaselineId, @EnrollmentId,
                    @ParticipantId, @TaskSourceId, 1, 'baseline', NULL,
                    'active', TRUE, @AcceptedAt, @AcceptedAt, NULL, NULL,
                    @SessionId, @ConfigurationId, @ManifestId, @Digest, @Digest);
                INSERT INTO submissions_attempt_submission_bindings (
                    organization_id, attempt_id, version_id, version_number, binding_order)
                VALUES (
                    @OrganizationId, @AttemptId, @VersionId, 1, 1);
                SET session_replication_role = DEFAULT;
                """,
                new
                {
                    OrganizationId = organizationId,
                    SubmissionId = submissionId,
                    VersionId = versionId,
                    ActivityId = activityId,
                    CohortId = cohortId,
                    BaselineId = baselineId,
                    EnrollmentId = enrollmentId,
                    ParticipantId = participantId,
                    TaskSourceId = taskSourceId,
                    TaskVersionId = taskVersionId,
                    Digest = digest,
                    ItemId = itemId,
                    ArtifactObjectKey = $"org/{organizationId:D}/{itemId:D}",
                    AttemptId = attemptId,
                    AcceptedAt = now,
                    SessionId = Guid.CreateVersion7(),
                    ConfigurationId = Guid.CreateVersion7(),
                    ManifestId = Guid.CreateVersion7(),
                });
        }

        await GrateMigrationRunner.RunEmbeddedMigrationsForTestsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken);

        var expectedDigest = AttemptSubmissionProvenance.ForAcceptedVersion(
            new AcceptedSubmissionVersion(
                submissionId,
                versionId,
                1,
                new SubmissionParentScope(
                    organizationId,
                    activityId,
                    cohortId,
                    baselineId,
                    enrollmentId,
                    participantId,
                    taskSourceId,
                    taskVersionId,
                    digest),
                digest,
                null,
                now,
                [new AcceptedVersionItem(itemId, MaterialCategories.DirectText, null, 12, digest, $"org/{organizationId:D}/{itemId:D}", "v1")]));

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            var persisted = await connection.ExecuteScalarAsync<string>(
                """
                SELECT content_digest
                FROM submissions_attempt_submission_bindings
                WHERE organization_id = @OrganizationId
                  AND attempt_id = @AttemptId
                  AND version_id = @VersionId;
                """,
                new { OrganizationId = organizationId, AttemptId = attemptId, VersionId = versionId });
            Assert.Equal(expectedDigest, persisted);

            var appendOnly = await Assert.ThrowsAsync<PostgresException>(() =>
                connection.ExecuteAsync(
                    """
                    UPDATE submissions_attempt_submission_bindings
                    SET binding_order = binding_order
                    WHERE organization_id = @OrganizationId
                      AND attempt_id = @AttemptId
                      AND version_id = @VersionId;
                    """,
                    new { OrganizationId = organizationId, AttemptId = attemptId, VersionId = versionId }));
            Assert.Contains("append-only", appendOnly.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<PostgreSqlContainer> StartContainerAsync()
    {
        var container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("flexagent_upgrade_test")
            .WithUsername("flexagent")
            .WithPassword("flexagent_upgrade_password")
            .Build();

        await container.StartAsync(TestContext.Current.CancellationToken);
        return container;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FlexAgent.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed record Populated0015TimerSeed(
        Guid OrganizationId,
        Guid DefaultSessionId,
        Guid RecommendedSessionId,
        Guid ReplacedSessionId);

    private sealed record Populated0016TerminalSeed(Guid OrganizationId, Guid SessionId);

    private sealed record Populated0017HandoffSeed(
        Guid OrganizationId,
        Guid SessionId,
        Guid TerminalRecordId);

    private sealed record Populated0018ClaimedWorkSeed(
        Guid OrganizationA,
        Guid OrganizationB,
        Guid ActivityA,
        Guid ClaimedWorkId,
        Guid PendingBWorkId);

    private sealed record Populated0018PendingWorkSeed(
        Guid OrganizationA,
        Guid OrganizationB,
        Guid ActivityA,
        Guid SessionA1,
        Guid WorkA1,
        Guid PendingBWorkId);

    private sealed record Populated0012PublicationSeed(
        Guid OrganizationId,
        Guid SessionId,
        string MessageId);

    private sealed record LegacyVersionSeed(
        Guid OrganizationId,
        Guid ActorId,
        Guid SourceId,
        Guid VersionId,
        string IdempotencyKey,
        string Digest);
}
