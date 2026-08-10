using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using Dapper;
using Npgsql;

namespace FlexAgent.Postgres.Migrations;

public static class GrateMigrationRunner
{
    public const string AllowEmbeddedFallbackEnvironmentVariable = "FLEXAGENT_ALLOW_EMBEDDED_MIGRATION_FALLBACK";

    public static async Task RunAsync(
        string connectionString,
        string migrationsDirectory,
        CancellationToken cancellationToken = default,
        bool? allowEmbeddedFallback = null)
    {
        var toolResult = TryRunDotnetTool(connectionString, GrateToolInvocationOptions.Default);
        if (toolResult.WasSuccessful)
        {
            return;
        }

        if (!IsEmbeddedFallbackAllowed(allowEmbeddedFallback))
        {
            throw new InvalidOperationException(
                $"Grate migration failed and embedded fallback is disabled.{Environment.NewLine}{toolResult.Error}");
        }

        if (!toolResult.IsRuntimeIncompatibility)
        {
            throw new InvalidOperationException(
                $"Grate migration failed with a non-recoverable error.{Environment.NewLine}{toolResult.Error}");
        }

        await RunEmbeddedMigrationsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            toolResult.Error);
    }

    public static GrateToolInvocationResult InvokeTool(
        string connectionString,
        GrateToolInvocationOptions? options = null)
    {
        var toolResult = TryRunDotnetTool(connectionString, options ?? GrateToolInvocationOptions.Default);
        return new GrateToolInvocationResult(
            toolResult.WasSuccessful,
            toolResult.Error ?? string.Empty,
            toolResult.ExitCode,
            toolResult.IsRuntimeIncompatibility);
    }

    private static bool IsEmbeddedFallbackAllowed(bool? allowEmbeddedFallback)
    {
        if (allowEmbeddedFallback.HasValue)
        {
            return allowEmbeddedFallback.Value;
        }

        var configured = Environment.GetEnvironmentVariable(AllowEmbeddedFallbackEnvironmentVariable);
        return string.Equals(configured, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(configured, "1", StringComparison.OrdinalIgnoreCase);
    }

    private static ToolInvocationResult TryRunDotnetTool(
        string connectionString,
        GrateToolInvocationOptions options)
    {
        var root = FindRepositoryRoot();
        var script = Path.Combine(root, "build", "scripts", "run-grate-migrations.sh");
        var scriptArguments = options.DryRun ? $"\"{script}\" --dryrun" : $"\"{script}\"";
        var startInfo = new ProcessStartInfo("/bin/bash", scriptArguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment["FLEXAGENT_DATABASE_URL"] = connectionString;
        startInfo.Environment["DOTNET_ROLL_FORWARD"] = "LatestPatch";
        if (!string.IsNullOrWhiteSpace(options.MigrationsDirectory))
        {
            startInfo.Environment["FLEXAGENT_MIGRATIONS_DIRECTORY"] = options.MigrationsDirectory;
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return ToolInvocationResult.Failed(
                "Failed to start grate migration script.",
                isRuntimeIncompatibility: true,
                exitCode: -1);
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            return ToolInvocationResult.Success();
        }

        var combined = $"{stdout}{Environment.NewLine}{stderr}";
        var isRuntimeIncompatibility = process.ExitCode == 150
            && combined.Contains("You must install or update .NET", StringComparison.OrdinalIgnoreCase);

        return ToolInvocationResult.Failed(
            $"Grate tool failed ({process.ExitCode}):{Environment.NewLine}{combined}",
            isRuntimeIncompatibility,
            process.ExitCode);
    }

    public static Task RunEmbeddedMigrationsForTestsAsync(
        string connectionString,
        string migrationsDirectory,
        CancellationToken cancellationToken = default,
        string? inclusiveMaxScriptName = null) =>
        RunEmbeddedMigrationsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            toolError: null,
            inclusiveMaxScriptName: inclusiveMaxScriptName);

    public static async Task ApplyRecordedMigrationForTestsAsync(
        string connectionString,
        string scriptName,
        string sql,
        CancellationToken cancellationToken = default)
    {
        var scriptHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql))).ToLowerInvariant();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                CREATE TABLE IF NOT EXISTS grate_migrations (
                    script_name TEXT PRIMARY KEY,
                    script_hash CHAR(64) NOT NULL,
                    applied_at TIMESTAMPTZ NOT NULL
                );
                """,
                cancellationToken: cancellationToken));

        var existingHash = await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                "SELECT script_hash FROM grate_migrations WHERE script_name = @ScriptName;",
                new { ScriptName = scriptName },
                cancellationToken: cancellationToken));

        if (existingHash is not null
            && !string.Equals(existingHash, scriptHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Migration script '{scriptName}' changed after it was applied.");
        }

        if (existingHash is not null)
        {
            return;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken));
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO grate_migrations (script_name, script_hash, applied_at)
                    VALUES (@ScriptName, @ScriptHash, NOW() AT TIME ZONE 'UTC');
                    """,
                    new { ScriptName = scriptName, ScriptHash = scriptHash },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public static string ComputeScriptHash(string sql) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql))).ToLowerInvariant();

    private static async Task RunEmbeddedMigrationsAsync(
        string connectionString,
        string migrationsDirectory,
        CancellationToken cancellationToken,
        string? toolError,
        string? inclusiveMaxScriptName = null)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                CREATE TABLE IF NOT EXISTS grate_migrations (
                    script_name TEXT PRIMARY KEY,
                    script_hash CHAR(64) NOT NULL,
                    applied_at TIMESTAMPTZ NOT NULL
                );
                """,
                cancellationToken: cancellationToken));

        var upDirectory = Path.Combine(migrationsDirectory, "up");
        if (!Directory.Exists(upDirectory))
        {
            throw new InvalidOperationException($"Migration up directory not found: {upDirectory}. {toolError}");
        }

        foreach (var scriptPath in Directory.GetFiles(upDirectory, "*.sql").OrderBy(path => path, StringComparer.Ordinal))
        {
            var scriptName = Path.GetFileName(scriptPath);
            var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);
            var scriptHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql))).ToLowerInvariant();

            var existingHash = await connection.ExecuteScalarAsync<string?>(
                new CommandDefinition(
                    "SELECT script_hash FROM grate_migrations WHERE script_name = @ScriptName;",
                    new { ScriptName = scriptName },
                    cancellationToken: cancellationToken));

            if (existingHash is not null)
            {
                if (!string.Equals(existingHash, scriptHash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Migration script '{scriptName}' changed after it was applied.");
                }

                continue;
            }

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        sql,
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO grate_migrations (script_name, script_hash, applied_at)
                        VALUES (@ScriptName, @ScriptHash, NOW() AT TIME ZONE 'UTC');
                        """,
                        new { ScriptName = scriptName, ScriptHash = scriptHash },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            if (string.Equals(scriptName, inclusiveMaxScriptName, StringComparison.Ordinal))
            {
                break;
            }
        }
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

    private sealed record ToolInvocationResult(
        bool WasSuccessful,
        string? Error,
        bool IsRuntimeIncompatibility,
        int ExitCode)
    {
        public static ToolInvocationResult Success() => new(true, null, false, 0);

        public static ToolInvocationResult Failed(string error, bool isRuntimeIncompatibility, int exitCode) =>
            new(false, error, isRuntimeIncompatibility, exitCode);
    }
}

public sealed record GrateToolInvocationOptions(
    bool DryRun = false,
    string? MigrationsDirectory = null)
{
    public static GrateToolInvocationOptions Default { get; } = new();
}

public sealed record GrateToolInvocationResult(
    bool WasSuccessful,
    string CombinedOutput,
    int ExitCode,
    bool IsRuntimeIncompatibility)
{
    public void EnsureSuccessful()
    {
        if (WasSuccessful)
        {
            return;
        }

        throw new InvalidOperationException(
            IsRuntimeIncompatibility
                ? $"Grate tool requires a compatible .NET 10 patch runtime (exit {ExitCode}).{Environment.NewLine}{CombinedOutput}"
                : $"Grate tool failed (exit {ExitCode}).{Environment.NewLine}{CombinedOutput}");
    }
}
