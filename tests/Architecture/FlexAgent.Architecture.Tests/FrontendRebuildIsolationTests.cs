using System.Text.RegularExpressions;

namespace FlexAgent.Architecture.Tests;

public sealed class FrontendRebuildIsolationTests
{
    private static readonly string[] ProductionSourceExtensions = [".ts", ".tsx", ".js", ".jsx", ".css", ".html"];
    private static readonly Regex[] SpecifierPatterns =
    [
        new(@"(?:import|export)\s+(?:type\s+)?(?:[\s\S]*?\sfrom\s+)?[""']([^""']+)[""']", RegexOptions.Compiled),
        new(@"import\s*\(\s*[""']([^""']+)[""']\s*\)", RegexOptions.Compiled),
        new(@"require\s*\(\s*[""']([^""']+)[""']\s*\)", RegexOptions.Compiled),
        new(@"@import\s+(?:url\(\s*)?[""']([^""']+)[""']", RegexOptions.Compiled),
    ];
    private static readonly Regex DesignLabSegment = new(@"(?:^|/)design-lab(?:/|$)", RegexOptions.Compiled);
    private static readonly Regex WebSrcSegment = new(@"(?:^|/)web/src/", RegexOptions.Compiled);
    private static readonly Regex LabOwnedStylesheet = new(@"(?:^|/)styles/(?:design-lab\.css|components/demo\.css|surfaces/)", RegexOptions.Compiled);
    private static readonly Regex[] DesignLabOutboundAllow =
    [
        new(@"(?:^|/)web/src/design-lab/", RegexOptions.Compiled),
        new(@"(?:^|/)web/src/design-system/", RegexOptions.Compiled),
        new(@"(?:^|/)web/src/lib/", RegexOptions.Compiled),
        new(@"(?:^|/)web/src/styles/", RegexOptions.Compiled),
    ];

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
        Assert.Contains("exclude:", config, StringComparison.Ordinal);
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
            if (IsLabOwnedStylesheet(relative))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            AddIfContains(violations, relative, content, "web-legacy");
            AddIfContains(violations, relative, content, ".work/resources");
            AddIfContains(violations, relative, content, "impeccable-prototype");
            AddDesignLabImportViolations(violations, file, relative, content);
            AddLabOwnedStylesheetImportViolations(violations, file, relative, content);
            if (!relative.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            {
                AddIfContains(violations, relative, content, "HOME_ENROLLMENTS");
                AddIfContains(violations, relative, content, "Approve & Release");
                AddIfContains(violations, relative, content, "Mark Submission Complete");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Candidate_design_system_does_not_import_design_lab_or_snapshot()
    {
        var root = FindRepositoryRoot();
        var designSystemRoot = Path.Combine(root, "web", "src", "design-system");
        Assert.True(Directory.Exists(Path.Combine(designSystemRoot, "foundations")));
        Assert.True(Directory.Exists(Path.Combine(designSystemRoot, "components")));
        Assert.True(Directory.Exists(Path.Combine(designSystemRoot, "patterns")));

        var violations = new List<string>();
        foreach (var file in EnumerateSourceFiles(designSystemRoot))
        {
            var relative = ToRepoRelative(root, file);
            var content = File.ReadAllText(file);
            AddIfContains(violations, relative, content, "web-legacy");
            AddIfContains(violations, relative, content, ".work/resources");
            AddIfContains(violations, relative, content, "impeccable-prototype");
            AddDesignLabImportViolations(violations, file, relative, content);
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Relative_design_lab_import_specifiers_are_detected()
    {
        const string fromFile = "/repo/web/src/design-system/components/chrome/Brand.tsx";
        Assert.True(SpecifierResolvesToDesignLab(fromFile, "../../design-lab/data/fixtures"));
        Assert.True(SpecifierResolvesToDesignLab(fromFile, "src/design-lab/app/router"));
        Assert.False(SpecifierResolvesToDesignLab(fromFile, "./operator"));
        var specifiers = ExtractImportSpecifiers("import { x } from \"../../design-lab/data/fixtures\";");
        Assert.Contains("../../design-lab/data/fixtures", specifiers);
    }

    [Fact]
    public void Relative_lab_owned_stylesheet_import_specifiers_are_detected()
    {
        const string fromFile = "/repo/web/src/App.tsx";
        Assert.True(SpecifierResolvesToLabOwnedStylesheet(fromFile, "./styles/surfaces/participant-home.css"));
        Assert.True(SpecifierResolvesToLabOwnedStylesheet(fromFile, "./styles/components/demo.css"));
        Assert.False(SpecifierResolvesToLabOwnedStylesheet(fromFile, "./styles/shared.css"));
    }

    [Fact]
    public void Candidate_source_does_not_import_lab_owned_stylesheets()
    {
        var root = FindRepositoryRoot();
        var productionRoot = Path.Combine(root, "web", "src");
        var violations = new List<string>();

        foreach (var file in EnumerateSourceFiles(productionRoot))
        {
            if (IsDesignLabPath(productionRoot, file))
            {
                continue;
            }

            var relative = ToRepoRelative(root, file);
            if (IsLabOwnedStylesheet(relative))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            AddLabOwnedStylesheetImportViolations(violations, file, relative, content);
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Design_lab_outbound_import_allowlist_blocks_future_production_modules()
    {
        const string fromFile = "/repo/web/src/design-lab/features/admin/SampleArea.tsx";
        Assert.True(SpecifierResolvesToAllowedDesignLabOutbound(fromFile, "../../components"));
        Assert.False(SpecifierResolvesToAllowedDesignLabOutbound(fromFile, "../../../api/client"));
        Assert.False(SpecifierResolvesToAllowedDesignLabOutbound(fromFile, "../../../components/ErrorBoundary"));
    }

    [Fact]
    public void Candidate_production_entry_does_not_load_lab_style_graph()
    {
        var root = FindRepositoryRoot();
        var main = File.ReadAllText(Path.Combine(root, "web", "src", "main.tsx"));
        Assert.Contains("styles/shared.css", main, StringComparison.Ordinal);
        Assert.DoesNotContain("styles/index.css", main, StringComparison.Ordinal);
        Assert.DoesNotContain("design-lab.css", main, StringComparison.Ordinal);

        var shared = File.ReadAllText(Path.Combine(root, "web", "src", "styles", "shared.css"));
        Assert.DoesNotContain("demo.css", shared, StringComparison.Ordinal);
        Assert.DoesNotContain("./surfaces/", shared, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "web", "src", "styles", "index.css")));
    }

    [Fact]
    public void Design_lab_source_does_not_import_legacy_snapshot_or_production_modules()
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
            if (!relative.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            {
                AddDesignLabOutboundImportViolations(violations, file, relative, content);
            }
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

    private static void AddLabOwnedStylesheetImportViolations(List<string> violations, string file, string relative, string content)
    {
        foreach (var specifier in ExtractImportSpecifiers(content))
        {
            if (SpecifierResolvesToLabOwnedStylesheet(file, specifier))
            {
                violations.Add($"{relative} imports lab-owned stylesheet ('{specifier}')");
            }
        }
    }

    private static void AddDesignLabOutboundImportViolations(List<string> violations, string file, string relative, string content)
    {
        foreach (var specifier in ExtractImportSpecifiers(content))
        {
            if (!SpecifierResolvesToAllowedDesignLabOutbound(file, specifier))
            {
                violations.Add($"{relative} imports forbidden production module ('{specifier}')");
            }
        }
    }

    private static void AddDesignLabImportViolations(List<string> violations, string file, string relative, string content)
    {
        foreach (var specifier in ExtractImportSpecifiers(content))
        {
            if (SpecifierResolvesToDesignLab(file, specifier))
            {
                violations.Add($"{relative} imports a design-lab module ('{specifier}')");
            }
        }
    }

    private static IReadOnlyList<string> ExtractImportSpecifiers(string content)
    {
        var specifiers = new List<string>();
        foreach (var pattern in SpecifierPatterns)
        {
            foreach (Match match in pattern.Matches(content))
            {
                specifiers.Add(match.Groups[1].Value);
            }
        }

        return specifiers;
    }

    private static bool SpecifierResolvesToLabOwnedStylesheet(string fromFile, string specifier)
    {
        var normalized = specifier.Replace('\\', '/');
        if (!normalized.StartsWith('.') && !Path.IsPathRooted(specifier))
        {
            return false;
        }

        var directory = Path.GetDirectoryName(fromFile);
        ArgumentException.ThrowIfNullOrEmpty(directory);
        var resolved = Path.GetFullPath(Path.Combine(directory, specifier)).Replace('\\', '/');
        return LabOwnedStylesheet.IsMatch(resolved);
    }

    private static bool SpecifierResolvesToAllowedDesignLabOutbound(string fromFile, string specifier)
    {
        var normalized = specifier.Replace('\\', '/');
        if (!normalized.StartsWith('.') && !Path.IsPathRooted(specifier))
        {
            return true;
        }

        var directory = Path.GetDirectoryName(fromFile);
        ArgumentException.ThrowIfNullOrEmpty(directory);
        var resolved = Path.GetFullPath(Path.Combine(directory, specifier)).Replace('\\', '/');
        if (!WebSrcSegment.IsMatch(resolved))
        {
            return true;
        }

        return DesignLabOutboundAllow.Any(pattern => pattern.IsMatch(resolved));
    }

    private static bool SpecifierResolvesToDesignLab(string fromFile, string specifier)
    {
        var normalized = specifier.Replace('\\', '/');
        if (normalized.StartsWith('.') || Path.IsPathRooted(specifier))
        {
            var directory = Path.GetDirectoryName(fromFile);
            ArgumentException.ThrowIfNullOrEmpty(directory);
            var resolved = Path.GetFullPath(Path.Combine(directory, specifier)).Replace('\\', '/');
            return DesignLabSegment.IsMatch(resolved);
        }

        return DesignLabSegment.IsMatch(normalized);
    }

    private static bool IsLabOwnedStylesheet(string relative) =>
        LabOwnedStylesheet.IsMatch(relative.Replace('\\', '/'));

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
