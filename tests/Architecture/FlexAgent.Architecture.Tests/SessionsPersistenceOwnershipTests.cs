using System.Text.RegularExpressions;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Architecture.Tests;

public sealed class SessionsPersistenceOwnershipTests
{
    private static readonly string[] OtherModuleTables =
    [
        "organizations",
        "actors",
        "actor_organization_grants",
        "service_delegations",
        "configuration_sources",
        "configuration_source_versions",
        "configuration_source_version_idempotency",
        "audit_events",
        "outbox_items",
    ];

    [Fact]
    public void Session_runtime_migration_creates_only_session_prefixed_tables()
    {
        var tables = FindSessionRuntimeMigrationPaths()
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"CREATE TABLE\s+([a-z0-9_]+)", RegexOptions.IgnoreCase)
                .Select(match => match.Groups[1].Value))
            .ToArray();

        Assert.NotEmpty(tables);
        Assert.All(tables, table => Assert.StartsWith("session_", table, StringComparison.Ordinal));
        Assert.DoesNotContain(tables, table => OtherModuleTables.Contains(table, StringComparer.Ordinal));
    }

    [Fact]
    public void Sessions_module_source_does_not_write_other_module_tables()
    {
        var sessionsRoot = Path.Combine(FindRepositoryRoot(), "src", "Modules", "Sessions");
        var writePattern = new Regex(
            @"(INSERT\s+INTO|UPDATE|DELETE\s+FROM)\s+(" + string.Join("|", OtherModuleTables) + ")",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (var path in Directory.EnumerateFiles(sessionsRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            Assert.False(
                writePattern.IsMatch(source),
                $"{Path.GetRelativePath(sessionsRoot, path)} must not write another module's tables.");
        }
    }

    [Fact]
    public void Decision_payload_digest_format_version_is_explicit_v1()
    {
        Assert.Equal("v1", DecisionPayloadDigest.FormatVersionV1);
    }

    private static IReadOnlyList<string> FindSessionRuntimeMigrationPaths()
    {
        var upDirectory = Path.Combine(FindRepositoryRoot(), "database", "migrations", "up");
        var matches = Directory.GetFiles(upDirectory, "*session*.sql")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(matches.Length > 0, "Expected Sessions runtime Grate one-time scripts under database/migrations/up.");
        return matches;
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
