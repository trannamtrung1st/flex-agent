using System.Security.Cryptography;
using System.Text;
using Dapper;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class GrateMigrationTests(PostgresIntegrationFixture fixture) : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Empty_database_migrates_successfully()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var tableCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE';
                """,
                cancellationToken: CancellationToken));

        Assert.True(tableCount >= 7);
    }

    [Fact]
    public async Task Repeat_migration_is_no_op()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var tablesBefore = await ListApplicationTablesAsync(connection);

        await Fixture.RunMigrationsAsync();

        var tablesAfter = await ListApplicationTablesAsync(connection);
        Assert.Equal(tablesBefore.OrderBy(t => t), tablesAfter.OrderBy(t => t));
        Assert.Contains("configuration_source_versions", tablesAfter);
    }

    private async Task<IReadOnlyList<string>> ListApplicationTablesAsync(Npgsql.NpgsqlConnection connection)
    {
        var tables = await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE'
                  AND table_name NOT LIKE 'pg_%'
                ORDER BY table_name;
                """,
                cancellationToken: CancellationToken));

        return tables.AsList();
    }
}
