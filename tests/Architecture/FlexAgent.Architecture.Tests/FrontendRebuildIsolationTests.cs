namespace FlexAgent.Architecture.Tests;

public sealed class FrontendRebuildIsolationTests
{
    private static readonly string[] ProductionSourceExtensions = [".ts", ".tsx", ".js", ".jsx", ".css", ".html"];

    [Fact]
    public void Spa_dockerfile_points_at_web_legacy_until_cutover()
    {
        var dockerfile = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "deploy/docker/spa.Dockerfile"));

        Assert.Contains("COPY web-legacy/package.json web-legacy/package.json", dockerfile, StringComparison.Ordinal);
        Assert.Contains("COPY web-legacy/ web-legacy/", dockerfile, StringComparison.Ordinal);
        Assert.Contains("pnpm --filter @flex-agent/web-legacy build", dockerfile, StringComparison.Ordinal);
        Assert.Contains("/app/web-legacy/dist", dockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("COPY web/ web/", dockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("pnpm --filter @flex-agent/web build", dockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("design-lab", dockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_e2e_server_points_at_web_legacy()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "build/scripts/serve-e2e-spa.sh"));
        Assert.Contains("cd \"$ROOT/web-legacy\"", script, StringComparison.Ordinal);
        Assert.Contains("$ROOT/web-legacy/dist", script, StringComparison.Ordinal);
        Assert.DoesNotContain("$ROOT/web/dist", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Candidate_vite_production_input_excludes_design_lab()
    {
        var config = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "web", "vite.config.ts"));
        Assert.Contains("input: \"index.html\"", config, StringComparison.Ordinal);
        Assert.DoesNotContain("design-lab.html", config, StringComparison.Ordinal);
        Assert.Contains("src/design-lab/**", config, StringComparison.Ordinal);
    }

    [Fact]
    public void Candidate_spa_dockerfile_is_explicitly_non_production()
    {
        var dockerfile = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "deploy/docker/spa-candidate.Dockerfile"));

        Assert.Contains("NON-PRODUCTION", dockerfile, StringComparison.Ordinal);
        Assert.Contains("COPY web/ web/", dockerfile, StringComparison.Ordinal);
        Assert.Contains("pnpm --filter @flex-agent/web build", dockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("web-legacy/dist", dockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("design-lab.html", dockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_candidate_source_does_not_import_legacy_design_lab_or_snapshot()
    {
        var root = FindRepositoryRoot();
        var productionRoot = Path.Combine(root, "web", "src");
        Assert.True(Directory.Exists(productionRoot), "new web/src must exist");

        var violations = new List<string>();
        foreach (var file in EnumerateSourceFiles(productionRoot))
        {
            if (IsDesignLabPath(productionRoot, file))
            {
                continue;
            }

            var relative = ToRepoRelative(root, file);
            var content = File.ReadAllText(file);
            AddIfContains(violations, relative, content, "web-legacy");
            AddIfContains(violations, relative, content, "design-lab");
            AddIfContains(violations, relative, content, ".work/resources");
            AddIfContains(violations, relative, content, "impeccable-prototype");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Design_lab_source_does_not_import_legacy_or_snapshot()
    {
        var root = FindRepositoryRoot();
        var designLabRoot = Path.Combine(root, "web", "src", "design-lab");
        if (!Directory.Exists(designLabRoot))
        {
            return;
        }

        var violations = new List<string>();
        foreach (var file in EnumerateSourceFiles(designLabRoot))
        {
            var relative = ToRepoRelative(root, file);
            var content = File.ReadAllText(file);
            AddIfContains(violations, relative, content, "web-legacy");
            AddIfContains(violations, relative, content, ".work/resources");
            AddIfContains(violations, relative, content, "impeccable-prototype");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Legacy_source_does_not_import_new_web_design_lab_or_snapshot()
    {
        var root = FindRepositoryRoot();
        var legacyRoot = Path.Combine(root, "web-legacy", "src");
        Assert.True(Directory.Exists(legacyRoot), "web-legacy/src must exist");

        var violations = new List<string>();
        foreach (var file in EnumerateSourceFiles(legacyRoot))
        {
            var relative = ToRepoRelative(root, file);
            var content = File.ReadAllText(file);
            AddIfContains(violations, relative, content, "@flex-agent/web");
            AddIfContains(violations, relative, content, "design-lab");
            AddIfContains(violations, relative, content, ".work/resources");
            AddIfContains(violations, relative, content, "impeccable-prototype");
            if (HasParentWebImport(content))
            {
                violations.Add($"{relative} imports the new web tree");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static bool HasParentWebImport(string content)
    {
        return content.Contains("../web/", StringComparison.Ordinal)
            || content.Contains("\"web/src", StringComparison.Ordinal)
            || content.Contains("'web/src", StringComparison.Ordinal);
    }

    private static void AddIfContains(List<string> violations, string relative, string content, string needle)
    {
        if (content.Contains(needle, StringComparison.Ordinal))
        {
            violations.Add($"{relative} contains forbidden '{needle}'");
        }
    }

    private static bool IsDesignLabPath(string productionRoot, string file)
    {
        var designLab = Path.Combine(productionRoot, "design-lab") + Path.DirectorySeparatorChar;
        return file.StartsWith(designLab, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateSourceFiles(string directory) =>
        Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(path => ProductionSourceExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase));

    private static string ToRepoRelative(string root, string fullPath) =>
        Path.GetRelativePath(root, fullPath).Replace('\\', '/');

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
