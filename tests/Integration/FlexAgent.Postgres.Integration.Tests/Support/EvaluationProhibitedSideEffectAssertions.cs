using Dapper;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal static class EvaluationProhibitedSideEffectAssertions
{
    internal static async Task AssertAbsentAsync(NpgsqlConnection connection)
    {
        Assert.False(await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name IN (
                      'review_decisions',
                      'results',
                      'releases',
                      'human_revisions',
                      'agent_memory_records',
                      'harness_calibration_records'));
            """));
    }
}
