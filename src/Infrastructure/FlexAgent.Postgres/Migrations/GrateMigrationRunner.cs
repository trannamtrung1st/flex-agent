using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using Dapper;
using Npgsql;

namespace FlexAgent.Postgres.Migrations;

public static class GrateMigrationRunner
{
    public static async Task RunAsync(string connectionString, string migrationsDirectory, CancellationToken cancellationToken = default)
    {
        if (TryRunDotnetTool(connectionString, migrationsDirectory, out var toolError))
        {
            return;
        }

        await RunEmbeddedMigrationsAsync(connectionString, migrationsDirectory, cancellationToken, toolError);
    }

    private static bool TryRunDotnetTool(string connectionString, string migrationsDirectory, out string? error)
    {
        error = null;
        var root = FindRepositoryRoot();
        var script = Path.Combine(root, "build", "scripts", "run-grate-migrations.sh");
        var startInfo = new ProcessStartInfo("/bin/bash", script)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment["FLEXAGENT_DATABASE_URL"] = connectionString;
        startInfo.Environment["DOTNET_ROLL_FORWARD"] = "LatestPatch";

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            error = "Failed to start grate migration script.";
            return false;
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            return true;
        }

        error = $"Grate tool failed ({process.ExitCode}):{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}";
        return false;
    }

    private static async Task RunEmbeddedMigrationsAsync(
        string connectionString,
        string migrationsDirectory,
        CancellationToken cancellationToken,
        string? toolError)
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
}
