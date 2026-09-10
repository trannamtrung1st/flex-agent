using Dapper;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationPersistenceSchemaTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    private static readonly string[] RequiredTables =
    [
        "evaluation_handoff_inbox",
        "evaluation_requests",
        "evaluation_invocation_attempts",
        "evaluation_deterministic_attempts",
        "evaluation_deterministic_payloads",
        "evaluation_provider_artifacts",
        "evaluation_durable_work",
        "evaluation_work_claim_partitions",
        "evaluation_evidence_items",
        "evaluation_evidence_sets",
        "evaluation_evidence_set_items",
        "evaluation_criterion_judgments",
        "evaluations",
        "evaluation_lineage",
        "evaluation_annotations",
        "evaluation_dispositions",
        "evaluation_manifest_refs",
        "evaluation_review_handoffs",
        "evaluation_lifecycle_holds",
        "evaluation_lifecycle_disposition_events",
    ];

    [Fact]
    public async Task Migration_0072_creates_the_complete_evaluation_persistence_family()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);

        var tables = (await connection.QueryAsync<string>(
            """
            SELECT tablename
            FROM pg_tables
            WHERE schemaname = 'public'
              AND tablename = ANY(@RequiredTables);
            """,
            new { RequiredTables })).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(RequiredTables.Order(), tables.Order());
    }

    [Fact]
    public async Task Migration_0072_installs_claim_and_completion_uniqueness()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);

        var indexes = (await connection.QueryAsync<string>(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname = ANY(@RequiredIndexes);
            """,
            new
            {
                RequiredIndexes = new[]
                {
                    "uq_evaluation_requests_initial_input",
                    "uq_evaluation_requests_idempotency",
                    "uq_evaluation_attempts_ordinal",
                    "uq_evaluations_request",
                    "ix_evaluation_durable_work_claimable",
                },
            })).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(5, indexes.Count);
    }

    [Fact]
    public async Task Migration_0073_binds_disposition_and_audit_provenance()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);

        var indexes = (await connection.QueryAsync<string>(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname = ANY(@RequiredIndexes);
            """,
            new
            {
                RequiredIndexes = new[]
                {
                    "uq_evaluation_annotations_owned",
                    "uq_audit_events_organization_event",
                },
            })).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(2, indexes.Count);
    }

    [Fact]
    public async Task Completed_artifact_tables_have_immutability_triggers()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);

        var protectedTables = new[]
        {
            "evaluation_evidence_items",
            "evaluation_evidence_sets",
            "evaluation_evidence_set_items",
            "evaluation_criterion_judgments",
            "evaluations",
            "evaluation_lineage",
            "evaluation_annotations",
            "evaluation_manifest_refs",
            "evaluation_review_handoffs",
            "evaluation_lifecycle_disposition_events",
        };
        var triggerTables = (await connection.QueryAsync<string>(
            """
            SELECT DISTINCT event_object_table
            FROM information_schema.triggers
            WHERE trigger_schema = 'public'
              AND event_manipulation IN ('UPDATE', 'DELETE')
              AND event_object_table = ANY(@ProtectedTables);
            """,
            new { ProtectedTables = protectedTables })).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(protectedTables.Order(), triggerTables.Order());
    }
}
