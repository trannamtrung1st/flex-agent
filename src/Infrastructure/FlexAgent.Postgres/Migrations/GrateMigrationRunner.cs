using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using Dapper;
using Npgsql;

namespace FlexAgent.Postgres.Migrations;

public static class GrateMigrationRunner
{
    public const string AllowEmbeddedFallbackEnvironmentVariable = "FLEXAGENT_ALLOW_EMBEDDED_MIGRATION_FALLBACK";

    private const int MaxConcurrentBootstrapRetryAttempts = 5;

    public static Task RunAsync(
        string connectionString,
        string migrationsDirectory,
        CancellationToken cancellationToken = default,
        bool? allowEmbeddedFallback = null) =>
        RunAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            allowEmbeddedFallback,
            ProcessGrateToolInvoker.Instance,
            ExponentialBootstrapRetryDelayPolicy.Instance);

    internal static async Task RunAsync(
        string connectionString,
        string migrationsDirectory,
        CancellationToken cancellationToken,
        bool? allowEmbeddedFallback,
        IGrateToolInvoker toolInvoker,
        IGrateBootstrapRetryDelayPolicy retryDelayPolicy)
    {
        var toolOptions = new GrateToolInvocationOptions(MigrationsDirectory: migrationsDirectory);
        ToolInvocationResult? lastToolResult = null;

        for (var attempt = 1; attempt <= MaxConcurrentBootstrapRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var toolResult = toolInvoker.Invoke(connectionString, toolOptions);
            if (toolResult.WasSuccessful)
            {
                return;
            }

            lastToolResult = toolResult;

            if (IsTransientConcurrentBootstrapFailure(toolResult.Error)
                && attempt < MaxConcurrentBootstrapRetryAttempts)
            {
                await Task.Delay(retryDelayPolicy.GetDelay(attempt), cancellationToken);
                continue;
            }

            break;
        }

        var toolResultForFailure = lastToolResult
            ?? throw new InvalidOperationException("Grate migration did not run.");

        if (!IsEmbeddedFallbackAllowed(allowEmbeddedFallback))
        {
            throw new InvalidOperationException(
                $"Grate migration failed and embedded fallback is disabled.{Environment.NewLine}{toolResultForFailure.Error}");
        }

        if (!toolResultForFailure.IsRuntimeIncompatibility)
        {
            throw new InvalidOperationException(
                $"Grate migration failed with a non-recoverable error.{Environment.NewLine}{toolResultForFailure.Error}");
        }

        await RunEmbeddedMigrationsAsync(
            connectionString,
            migrationsDirectory,
            cancellationToken,
            toolResultForFailure.Error);
    }

    public static GrateToolInvocationResult InvokeTool(
        string connectionString,
        GrateToolInvocationOptions? options = null)
    {
        var toolResult = ProcessGrateToolInvoker.Instance.Invoke(
            connectionString,
            options ?? GrateToolInvocationOptions.Default);
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

    private static bool IsTransientConcurrentBootstrapFailure(string? combinedOutput) =>
        !string.IsNullOrEmpty(combinedOutput)
        && combinedOutput.Contains("grate-internal/", StringComparison.OrdinalIgnoreCase)
        && combinedOutput.Contains("42P01", StringComparison.OrdinalIgnoreCase);

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

    internal sealed record ToolInvocationResult(
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

internal interface IGrateToolInvoker
{
    GrateMigrationRunner.ToolInvocationResult Invoke(string connectionString, GrateToolInvocationOptions options);
}

internal interface IGrateBootstrapRetryDelayPolicy
{
    TimeSpan GetDelay(int attempt);
}

internal sealed class ProcessGrateToolInvoker : IGrateToolInvoker
{
  public static ProcessGrateToolInvoker Instance { get; } = new();

  public GrateMigrationRunner.ToolInvocationResult Invoke(string connectionString, GrateToolInvocationOptions options)
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
    var migrationsDirectory = string.IsNullOrWhiteSpace(options.MigrationsDirectory)
      ? Path.Combine(root, "database", "migrations")
      : options.MigrationsDirectory;
    startInfo.Environment["FLEXAGENT_MIGRATIONS_DIRECTORY"] = migrationsDirectory;

    using var process = Process.Start(startInfo);
    if (process is null)
    {
      return GrateMigrationRunner.ToolInvocationResult.Failed(
        "Failed to start grate migration script.",
        isRuntimeIncompatibility: true,
        exitCode: -1);
    }

    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode == 0)
    {
      return GrateMigrationRunner.ToolInvocationResult.Success();
    }

    var combined = $"{stdout}{Environment.NewLine}{stderr}";
    var isRuntimeIncompatibility = process.ExitCode == 150
      && combined.Contains("You must install or update .NET", StringComparison.OrdinalIgnoreCase);

    return GrateMigrationRunner.ToolInvocationResult.Failed(
      $"Grate tool failed ({process.ExitCode}):{Environment.NewLine}{combined}",
      isRuntimeIncompatibility,
      process.ExitCode);
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

internal sealed class ExponentialBootstrapRetryDelayPolicy : IGrateBootstrapRetryDelayPolicy
{
  private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(100);

  public static ExponentialBootstrapRetryDelayPolicy Instance { get; } = new();

  public TimeSpan GetDelay(int attempt) =>
    TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
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
