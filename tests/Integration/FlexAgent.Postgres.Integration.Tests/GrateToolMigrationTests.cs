using Dapper;
using FlexAgent.Postgres.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class GrateToolMigrationTests
{
    private const int ExpectedOneTimeScriptCount = 12;

    [Fact]
    public async Task Grate_tool_migrates_empty_database()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();

        GrateMigrationRunner.InvokeTool(connectionString).EnsureSuccessful();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var oneTimeScriptCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM grate."ScriptsRun"
            WHERE one_time_script = true;
            """);

        Assert.Equal(ExpectedOneTimeScriptCount, oneTimeScriptCount);
        Assert.True(await TableExistsAsync(connection, "configuration_source_versions"));
        Assert.True(await TableExistsAsync(connection, "configuration_source_version_idempotency"));
    }

    [Fact]
    public async Task Grate_tool_repeat_is_no_op()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();

        GrateMigrationRunner.InvokeTool(connectionString).EnsureSuccessful();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var scriptsAfterFirst = await ListOneTimeScriptNamesAsync(connection);
        var tablesAfterFirst = await ListApplicationTablesAsync(connection);

        GrateMigrationRunner.InvokeTool(connectionString).EnsureSuccessful();

        var scriptsAfterSecond = await ListOneTimeScriptNamesAsync(connection);
        var tablesAfterSecond = await ListApplicationTablesAsync(connection);

        Assert.Equal(scriptsAfterFirst, scriptsAfterSecond);
        Assert.Equal(tablesAfterFirst, tablesAfterSecond);
        Assert.Equal(ExpectedOneTimeScriptCount, scriptsAfterSecond.Count);
    }

    [Fact]
    public async Task Grate_tool_dry_run_is_non_mutating()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();

        GrateMigrationRunner.InvokeTool(
            connectionString,
            new GrateToolInvocationOptions(DryRun: true)).EnsureSuccessful();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        Assert.False(await TableExistsAsync(connection, "configuration_source_versions"));
        Assert.False(await GrateScriptsRunTableExistsAsync(connection));
    }

    [Fact]
    public async Task Grate_tool_changed_one_time_script_fails_closed()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var tempMigrationsDirectory = CopyProductionMigrationsToTempDirectory();

        try
        {
            GrateMigrationRunner.InvokeTool(
                connectionString,
                new GrateToolInvocationOptions(MigrationsDirectory: tempMigrationsDirectory))
                .EnsureSuccessful();

            var tamperedScriptPath = Path.Combine(
                tempMigrationsDirectory,
                "up",
                "0004_harden_constraint_scope_checks.sql");
            await File.AppendAllTextAsync(
                tamperedScriptPath,
                Environment.NewLine + "-- flexagent grate changed-script probe",
                TestContext.Current.CancellationToken);

            var secondRun = GrateMigrationRunner.InvokeTool(
                connectionString,
                new GrateToolInvocationOptions(MigrationsDirectory: tempMigrationsDirectory));

            Assert.False(secondRun.WasSuccessful);
            Assert.Contains("Script has changed", secondRun.CombinedOutput, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempMigrationsDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Grate_tool_failed_script_rolls_back_within_transaction()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var failureMigrationsDirectory = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "Integration",
            "FlexAgent.Postgres.Integration.Tests",
            "Fixtures",
            "grate-migrations",
            "atomic-failure");

        var result = GrateMigrationRunner.InvokeTool(
            connectionString,
            new GrateToolInvocationOptions(MigrationsDirectory: failureMigrationsDirectory));

        Assert.False(result.WasSuccessful);
        Assert.Contains("injected migration failure", result.CombinedOutput, StringComparison.OrdinalIgnoreCase);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        Assert.False(await TableExistsAsync(connection, "grate_atomic_failure_probe"));
        Assert.Equal(0, await CountOneTimeScriptsAsync(connection));
    }

    [Fact]
    public async Task RunAsync_concurrent_invocations_on_migrated_database_both_succeed()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = GetProductionMigrationsDirectory();

        await GrateMigrationRunner.RunAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken,
            allowEmbeddedFallback: false);

        await RunTwoGatedConcurrentRunAsyncCallsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await AssertFullyMigratedExactlyOnceAsync(connection);
    }

    [Fact]
    public async Task RunAsync_concurrent_invocations_on_empty_database_both_succeed()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var migrationsDirectory = GetProductionMigrationsDirectory();

        await RunTwoGatedConcurrentRunAsyncCallsAsync(
            connectionString,
            migrationsDirectory,
            TestContext.Current.CancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await AssertFullyMigratedExactlyOnceAsync(connection);
    }

    [Fact]
    public async Task RunAsync_retries_transient_grate_internal_bootstrap_failure()
    {
        var toolInvoker = new SequenceGrateToolInvoker(attempt =>
        {
            if (attempt == 1)
            {
                return GrateMigrationRunner.ToolInvocationResult.Failed(
                    """
                    grate-internal/01_create_schema_grate.sql: 42P01: relation "grate.GrateVersion" does not exist
                    """,
                    isRuntimeIncompatibility: false,
                    exitCode: 1);
            }

            return GrateMigrationRunner.ToolInvocationResult.Success();
        });

        await GrateMigrationRunner.RunAsync(
            "Host=unused;Database=unused;Username=unused;Password=unused",
            GetProductionMigrationsDirectory(),
            TestContext.Current.CancellationToken,
            allowEmbeddedFallback: false,
            toolInvoker,
            ZeroBootstrapRetryDelayPolicy.Instance);

        Assert.Equal(2, toolInvoker.Attempts);
    }

    [Fact]
    public async Task RunAsync_retries_transient_tool_restore_file_lock()
    {
        var toolInvoker = new SequenceGrateToolInvoker(attempt =>
        {
            if (attempt == 1)
            {
                return GrateMigrationRunner.ToolInvocationResult.Failed(
                    """
                    Unhandled exception: The process cannot access the file '/path/grate.2.1.6.nupkg' because it is being used by another process.
                    """,
                    isRuntimeIncompatibility: false,
                    exitCode: 1);
            }

            return GrateMigrationRunner.ToolInvocationResult.Success();
        });

        await GrateMigrationRunner.RunAsync(
            "Host=unused;Database=unused;Username=unused;Password=unused",
            GetProductionMigrationsDirectory(),
            TestContext.Current.CancellationToken,
            allowEmbeddedFallback: false,
            toolInvoker,
            ZeroBootstrapRetryDelayPolicy.Instance);

        Assert.Equal(2, toolInvoker.Attempts);
    }

    [Fact]
    public async Task RunAsync_uses_explicit_migrations_directory_over_ambient_environment()
    {
        await using var container = await StartContainerAsync();
        var connectionString = container.GetConnectionString();
        var productionMigrationsDirectory = GetProductionMigrationsDirectory();
        var hostileMigrationsDirectory = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "Integration",
            "FlexAgent.Postgres.Integration.Tests",
            "Fixtures",
            "grate-migrations",
            "atomic-failure");

        var previousMigrationsDirectory = Environment.GetEnvironmentVariable(
            "FLEXAGENT_MIGRATIONS_DIRECTORY");

        Environment.SetEnvironmentVariable(
            "FLEXAGENT_MIGRATIONS_DIRECTORY",
            hostileMigrationsDirectory);

        try
        {
            await GrateMigrationRunner.RunAsync(
                connectionString,
                productionMigrationsDirectory,
                TestContext.Current.CancellationToken,
                allowEmbeddedFallback: false);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);

            await AssertFullyMigratedExactlyOnceAsync(connection);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                "FLEXAGENT_MIGRATIONS_DIRECTORY",
                previousMigrationsDirectory);
        }
    }

    private static string GetProductionMigrationsDirectory() =>
        Path.Combine(FindRepositoryRoot(), "database", "migrations");

    private static async Task RunTwoGatedConcurrentRunAsyncCallsAsync(
        string connectionString,
        string migrationsDirectory,
        CancellationToken cancellationToken)
    {
        using var ready = new CountdownEvent(2);
        using var start = new ManualResetEventSlim(initialState: false);

        Task Worker() => Task.Run(async () =>
        {
            ready.Signal();
            if (!start.Wait(Timeout.InfiniteTimeSpan, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            await GrateMigrationRunner.RunAsync(
                connectionString,
                migrationsDirectory,
                cancellationToken,
                allowEmbeddedFallback: false);
        }, cancellationToken);

        var first = Worker();
        var second = Worker();

        ready.Wait(cancellationToken);
        start.Set();

        await Task.WhenAll(first, second);
    }

    private static async Task AssertFullyMigratedExactlyOnceAsync(NpgsqlConnection connection)
    {
        Assert.Equal(ExpectedOneTimeScriptCount, await CountOneTimeScriptsAsync(connection));
        Assert.Equal(ExpectedOneTimeScriptCount, (await ListOneTimeScriptNamesAsync(connection)).Count);
        Assert.True(await TableExistsAsync(connection, "configuration_source_versions"));
        Assert.True(await TableExistsAsync(connection, "configuration_source_version_idempotency"));
    }

    private static string CopyProductionMigrationsToTempDirectory()
    {
        var tempRoot = Directory.CreateTempSubdirectory("flexagent-grate-tool-");
        var sourceUpDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations", "up");
        var destinationUpDirectory = Path.Combine(tempRoot.FullName, "up");
        Directory.CreateDirectory(destinationUpDirectory);

        foreach (var sourcePath in Directory.GetFiles(sourceUpDirectory, "*.sql"))
        {
            File.Copy(sourcePath, Path.Combine(destinationUpDirectory, Path.GetFileName(sourcePath)));
        }

        return tempRoot.FullName;
    }

    private static async Task<PostgreSqlContainer> StartContainerAsync()
    {
        var container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("flexagent_grate_tool_test")
            .WithUsername("flexagent")
            .WithPassword("flexagent_grate_tool_password")
            .Build();

        await container.StartAsync(TestContext.Current.CancellationToken);
        return container;
    }

    private static async Task<bool> TableExistsAsync(NpgsqlConnection connection, string tableName)
    {
        return await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name = @TableName);
            """,
            new { TableName = tableName });
    }

    private static async Task<bool> GrateScriptsRunTableExistsAsync(NpgsqlConnection connection)
    {
        return await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'grate'
                  AND table_name = 'ScriptsRun');
            """);
    }

    private static async Task<int> CountOneTimeScriptsAsync(NpgsqlConnection connection) =>
        await connection.ExecuteScalarAsync<int>(
            """
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'grate'
                      AND table_name = 'ScriptsRun')
                THEN (
                    SELECT COUNT(*)::int
                    FROM grate."ScriptsRun"
                    WHERE one_time_script = true)
                ELSE 0
            END;
            """);

    private static async Task<IReadOnlyList<string>> ListOneTimeScriptNamesAsync(NpgsqlConnection connection)
    {
        var scripts = await connection.QueryAsync<string>(
            """
            SELECT script_name
            FROM grate."ScriptsRun"
            WHERE one_time_script = true
            ORDER BY script_name;
            """);

        return scripts.AsList();
    }

    private static async Task<IReadOnlyList<string>> ListApplicationTablesAsync(NpgsqlConnection connection)
    {
        var tables = await connection.QueryAsync<string>(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
              AND table_name NOT LIKE 'pg_%'
            ORDER BY table_name;
            """);

        return tables.AsList();
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

    private sealed class SequenceGrateToolInvoker(Func<int, GrateMigrationRunner.ToolInvocationResult> resultFactory)
        : IGrateToolInvoker
    {
        private int _attempts;

        public int Attempts => _attempts;

        public GrateMigrationRunner.ToolInvocationResult Invoke(
            string connectionString,
            GrateToolInvocationOptions options) =>
            resultFactory(++_attempts);
    }

    private sealed class ZeroBootstrapRetryDelayPolicy : IGrateBootstrapRetryDelayPolicy
    {
        public static ZeroBootstrapRetryDelayPolicy Instance { get; } = new();

        public TimeSpan GetDelay(int attempt) => TimeSpan.Zero;
    }
}
