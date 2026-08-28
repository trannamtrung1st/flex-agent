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
    private static readonly Regex LabOwnedStylesheet = new(@"(?:^|/)styles/(?:design-lab\.css|components/demo\.css|surfaces/)", RegexOptions.Compiled);
    private static readonly Regex[] HtmlReferencePatterns =
    [
        new(@"<script\b[^>]*\btype\s*=\s*[""']module[""'][^>]*\bsrc\s*=\s*[""']([^""']+)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<script\b[^>]*\bsrc\s*=\s*[""']([^""']+)[""'][^>]*\btype\s*=\s*[""']module[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<link\b[^>]*\brel\s*=\s*[""']stylesheet[""'][^>]*\bhref\s*=\s*[""']([^""']+)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<link\b[^>]*\bhref\s*=\s*[""']([^""']+)[""'][^>]*\brel\s*=\s*[""']stylesheet[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    ];
    private static readonly string[] DesignLabOutboundRepoPrefixes =
    [
        "web/src/design-lab/",
        "web/src/design-system/",
        "web/src/lib/",
        "web/src/styles/",
    ];
    private const string CandidateHtmlModuleEntry = "/src/main.tsx";
    private const string DesignLabHtmlModuleEntry = "/src/design-lab/main.tsx";

    [Fact]
    public void Spa_dockerfile_points_at_web_production_entry()
    {
        var dockerfile = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "deploy/docker/spa.Dockerfile"));

        Assert.Contains("COPY web/ web/", dockerfile, StringComparison.Ordinal);
        Assert.Contains("pnpm --filter @flex-agent/web build", dockerfile, StringComparison.Ordinal);
        Assert.Contains("/app/web/dist", dockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("COPY web-legacy", dockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("design-lab.html", dockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_e2e_server_points_at_web()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "build/scripts/serve-e2e-spa.sh"));
        Assert.Contains("cd \"$ROOT/web\"", script, StringComparison.Ordinal);
        Assert.Contains("$ROOT/web/dist", script, StringComparison.Ordinal);
        Assert.DoesNotContain("web-legacy", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Candidate_vite_production_input_excludes_design_lab()
    {
        var config = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "web", "vite.config.ts"));
        Assert.Contains("input: \"index.html\"", config, StringComparison.Ordinal);
        Assert.DoesNotContain("design-lab.html", config, StringComparison.Ordinal);
        Assert.Contains("src/design-lab/**", config, StringComparison.Ordinal);
        Assert.Contains("exclude:", config, StringComparison.Ordinal);
        Assert.Contains("bypass(", config, StringComparison.Ordinal);
        Assert.Contains("\"/sessions\"", config, StringComparison.Ordinal);
        Assert.Contains("/events", config, StringComparison.Ordinal);
    }

    [Fact]
    public void Candidate_spa_dockerfile_is_retired()
    {
        var path = Path.Combine(FindRepositoryRoot(), "deploy/docker/spa-candidate.Dockerfile");
        Assert.False(File.Exists(path), "spa-candidate.Dockerfile must not remain after the single-SPA reset");
    }

    [Fact]
    public void Web_legacy_directory_is_absent()
    {
        var path = Path.Combine(FindRepositoryRoot(), "web-legacy");
        Assert.False(Directory.Exists(path), "web-legacy must not exist after the production frontend reset");
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
        var root = FindRepositoryRoot();
        var fromFile = Path.Combine(root, "web", "src", "design-lab", "features", "admin", "SampleArea.tsx");
        Assert.True(SpecifierResolvesToAllowedDesignLabOutbound(fromFile, "../../components", root));
        Assert.False(SpecifierResolvesToAllowedDesignLabOutbound(fromFile, "../../../api/client", root));
        Assert.False(SpecifierResolvesToAllowedDesignLabOutbound(fromFile, "../../../components/ErrorBoundary", root));
        Assert.False(SpecifierResolvesToAllowedDesignLabOutbound(fromFile, "../../../../../contracts/something", root));
        Assert.False(SpecifierResolvesToAllowedDesignLabOutbound(fromFile, "../../../../../build/scripts/foo", root));
    }

    [Fact]
    public void Candidate_html_entry_loads_only_approved_production_assets()
    {
        var root = FindRepositoryRoot();
        var index = File.ReadAllText(Path.Combine(root, "web", "index.html"));
        Assert.Contains("src=\"/src/main.tsx\"", index, StringComparison.Ordinal);
        Assert.DoesNotContain("design-lab", index, StringComparison.Ordinal);
        Assert.DoesNotContain("styles/surfaces/", index, StringComparison.Ordinal);
        Assert.DoesNotContain("styles/components/demo.css", index, StringComparison.Ordinal);

        var violations = CandidateHtmlEntryViolations(Path.Combine(root, "web", "index.html"), index, root);
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Design_lab_html_entry_loads_only_design_lab_main()
    {
        var root = FindRepositoryRoot();
        var index = File.ReadAllText(Path.Combine(root, "web", "design-lab.html"));
        Assert.Contains("src=\"/src/design-lab/main.tsx\"", index, StringComparison.Ordinal);
        Assert.DoesNotContain("src=\"/src/main.tsx\"", index, StringComparison.Ordinal);

        var violations = DesignLabHtmlEntryViolations(Path.Combine(root, "web", "design-lab.html"), index);
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Html_module_and_stylesheet_references_are_parsed_for_isolation_checks()
    {
        const string html = "<script type=\"module\" src=\"/src/main.tsx\"></script><link rel=\"stylesheet\" href=\"/src/styles/shared.css\" />";
        var specifiers = ExtractImportSpecifiers(html);
        Assert.Contains("/src/main.tsx", specifiers);
        Assert.Contains("/src/styles/shared.css", specifiers);
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
            if (!relative.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                && !relative.Contains(".test.", StringComparison.Ordinal)
                && !relative.Contains(".spec.", StringComparison.Ordinal))
            {
                AddDesignLabOutboundImportViolations(violations, file, relative, content);
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Web_legacy_source_tree_is_absent()
    {
        var root = FindRepositoryRoot();
        var legacyRoot = Path.Combine(root, "web-legacy");
        Assert.False(Directory.Exists(legacyRoot), "web-legacy must not exist after the production frontend reset");
    }

    [Fact]
    public void Pages_and_lab_routes_do_not_compose_outer_chrome_or_reference_layout()
    {
        var root = FindRepositoryRoot();
        var violations = new List<string>();
        var chrome = new[] { "CommandStrip", "ConsoleFoot", "Gangway", "Bulkhead", "AreaGroupList", "RailBrand", "IndexRail" };

        foreach (var file in EnumerateSourceFiles(Path.Combine(root, "web", "src", "pages"))
            .Concat(EnumerateSourceFiles(Path.Combine(root, "web", "src", "design-lab", "routes"))))
        {
            var relative = ToRepoRelative(root, file);
            if (relative.Contains(".test.", StringComparison.Ordinal) || relative.Contains(".spec.", StringComparison.Ordinal))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            foreach (var name in chrome)
            {
                if (Regex.IsMatch(content, $@"import\s+(?:type\s+)?\{{[^}}]*\b{name}\b")
                    || content.Contains($"<{name}", StringComparison.Ordinal))
                {
                    violations.Add($"{relative} composes outer chrome '{name}'");
                }
            }

            if (Regex.IsMatch(content, @"import\s+(?:type\s+)?\{[^}]*\bOperateHead\b")
                || content.Contains("<OperateHead", StringComparison.Ordinal))
            {
                violations.Add($"{relative} assembles OperateHead; use OperateArea");
            }

            var fileName = Path.GetFileName(file);
            var expectedLayout = fileName switch
            {
                "AdminPage.tsx" => "ManagementLayout",
                "HomePage.tsx" => "ManagementLayout",
                "JourneyPage.tsx" => "GuidedTaskLayout",
                "SessionPage.tsx" => "LiveSessionLayout",
                "ReviewerPage.tsx" => "ManagementLayout",
                "SurfacesPage.tsx" => "ReferenceLayout",
                "NotFoundPage.tsx" => "ReferenceLayout",
                _ => null,
            };
            if (expectedLayout is not null && !content.Contains($"<{expectedLayout}", StringComparison.Ordinal))
            {
                violations.Add($"{relative} must render {expectedLayout}");
            }

            if (relative.Contains("/web/src/pages/", StringComparison.Ordinal)
                || relative.StartsWith("web/src/pages/", StringComparison.Ordinal))
            {
                foreach (var name in new[] { "ManagementLayout", "GuidedTaskLayout", "LiveSessionLayout", "ReferenceLayout", "LayoutAssignment" })
                {
                    if (Regex.IsMatch(content, $@"import\s+(?:type\s+)?\{{[^}}]*\b{name}\b")
                        || content.Contains($"<{name}", StringComparison.Ordinal))
                    {
                        violations.Add($"{relative} imports layout '{name}'");
                    }
                }
            }

            if (Regex.IsMatch(content, @"data-layout=[""'](management|guided-task|live-session|reference)[""']"))
            {
                violations.Add($"{relative} uses a layout root attribute outside the layout library");
            }
        }

        foreach (var file in EnumerateSourceFiles(Path.Combine(root, "web", "src")))
        {
            var relative = ToRepoRelative(root, file);
            if (relative.Contains("/design-lab/", StringComparison.Ordinal)
                || relative.Contains("/design-system/patterns/layouts/", StringComparison.Ordinal)
                || relative.EndsWith("/design-system/lab.ts", StringComparison.Ordinal))
            {
                continue;
            }

            if (!relative.EndsWith(".ts", StringComparison.Ordinal) && !relative.EndsWith(".tsx", StringComparison.Ordinal))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            if (content.Contains("ReferenceLayout", StringComparison.Ordinal))
            {
                violations.Add($"{relative} references ReferenceLayout");
            }
        }

        foreach (var file in EnumerateSourceFiles(Path.Combine(root, "web", "src", "styles")))
        {
            var relative = ToRepoRelative(root, file);
            if (relative.EndsWith("styles/components/layouts.css", StringComparison.Ordinal))
            {
                continue;
            }

            if (!relative.EndsWith(".css", StringComparison.Ordinal))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            if (Regex.IsMatch(content, @"(?:^|})\s*\.layout-(?:management|guided|session|reference)(?:__[a-z0-9-]+)?(?=\s*[,{:])"))
            {
                violations.Add($"{relative} declares reserved layout selectors outside layouts.css");
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
        var root = FindRepositoryRoot();
        foreach (var specifier in ExtractImportSpecifiers(content))
        {
            if (!SpecifierResolvesToAllowedDesignLabOutbound(file, specifier, root))
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

        if (content.Contains('<', StringComparison.Ordinal))
        {
            foreach (var pattern in HtmlReferencePatterns)
            {
                foreach (Match match in pattern.Matches(content))
                {
                    specifiers.Add(match.Groups[1].Value);
                }
            }
        }

        return specifiers;
    }

    private static string? ResolveSpecifierToAbsolute(string fromFile, string specifier, string repoRoot)
    {
        var normalized = specifier.Replace('\\', '/');
        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (normalized.StartsWith('/') && !normalized.StartsWith("//", StringComparison.Ordinal))
        {
            return Path.GetFullPath(Path.Combine(repoRoot, "web", normalized.TrimStart('/'))).Replace('\\', '/');
        }

        if (normalized.StartsWith('.') || Path.IsPathRooted(specifier))
        {
            var directory = Path.GetDirectoryName(fromFile);
            ArgumentException.ThrowIfNullOrEmpty(directory);
            return Path.GetFullPath(Path.Combine(directory, specifier)).Replace('\\', '/');
        }

        return null;
    }

    private static bool IsAllowedDesignLabOutboundRepoRelative(string relativeToRepo) =>
        DesignLabOutboundRepoPrefixes.Any(prefix =>
            string.Equals(relativeToRepo, prefix.TrimEnd('/'), StringComparison.Ordinal)
            || relativeToRepo.StartsWith(prefix, StringComparison.Ordinal));

    private static List<string> CandidateHtmlEntryViolations(string htmlFile, string content, string repoRoot)
    {
        var violations = new List<string>();
        foreach (var reference in ExtractHtmlModuleScriptSources(content))
        {
            if (!string.Equals(reference, CandidateHtmlModuleEntry, StringComparison.Ordinal))
            {
                violations.Add($"{htmlFile} module entry must be '{CandidateHtmlModuleEntry}', found '{reference}'");
            }

            if (SpecifierResolvesToDesignLab(htmlFile, reference, repoRoot))
            {
                violations.Add($"{htmlFile} references design-lab module '{reference}'");
            }
        }

        foreach (var reference in ExtractHtmlStylesheetHrefs(content))
        {
            if (SpecifierResolvesToLabOwnedStylesheet(htmlFile, reference, repoRoot))
            {
                violations.Add($"{htmlFile} references lab-owned stylesheet '{reference}'");
            }

            if (SpecifierResolvesToDesignLab(htmlFile, reference, repoRoot))
            {
                violations.Add($"{htmlFile} references design-lab asset '{reference}'");
            }
        }

        return violations;
    }

    private static List<string> DesignLabHtmlEntryViolations(string htmlFile, string content)
    {
        var violations = new List<string>();
        foreach (var reference in ExtractHtmlModuleScriptSources(content))
        {
            if (!string.Equals(reference, DesignLabHtmlModuleEntry, StringComparison.Ordinal))
            {
                violations.Add($"{htmlFile} module entry must be '{DesignLabHtmlModuleEntry}', found '{reference}'");
            }
        }

        return violations;
    }

    private static IEnumerable<string> ExtractHtmlModuleScriptSources(string content)
    {
        foreach (var pattern in HtmlReferencePatterns.Take(2))
        {
            foreach (Match match in pattern.Matches(content))
            {
                yield return match.Groups[1].Value;
            }
        }
    }

    private static IEnumerable<string> ExtractHtmlStylesheetHrefs(string content)
    {
        foreach (var pattern in HtmlReferencePatterns.Skip(2))
        {
            foreach (Match match in pattern.Matches(content))
            {
                yield return match.Groups[1].Value;
            }
        }
    }

    private static bool SpecifierResolvesToLabOwnedStylesheet(string fromFile, string specifier, string? repoRoot = null)
    {
        var normalized = specifier.Replace('\\', '/');
        if (!normalized.StartsWith('.') && !normalized.StartsWith('/') && !Path.IsPathRooted(specifier))
        {
            return false;
        }

        var root = repoRoot ?? InferRepoRootFromWebSrcFile(fromFile);
        var resolved = ResolveSpecifierToAbsolute(fromFile, specifier, root);
        return resolved is not null && LabOwnedStylesheet.IsMatch(resolved);
    }

    private static bool SpecifierResolvesToAllowedDesignLabOutbound(string fromFile, string specifier, string? repoRoot = null)
    {
        var normalized = specifier.Replace('\\', '/');
        if (!normalized.StartsWith('.') && !normalized.StartsWith('/') && !Path.IsPathRooted(specifier))
        {
            return true;
        }

        var root = repoRoot ?? InferRepoRootFromWebSrcFile(fromFile);
        var resolved = ResolveSpecifierToAbsolute(fromFile, specifier, root);
        if (resolved is null)
        {
            return false;
        }

        var relativeToRepo = ToRepoRelative(root, resolved);
        if (relativeToRepo.StartsWith("..", StringComparison.Ordinal))
        {
            return false;
        }

        return IsAllowedDesignLabOutboundRepoRelative(relativeToRepo);
    }

    private static bool SpecifierResolvesToDesignLab(string fromFile, string specifier, string? repoRoot = null)
    {
        var normalized = specifier.Replace('\\', '/');
        if (!normalized.StartsWith('.') && !normalized.StartsWith('/') && !Path.IsPathRooted(specifier))
        {
            return DesignLabSegment.IsMatch(normalized);
        }

        var root = repoRoot ?? InferRepoRootFromWebSrcFile(fromFile);
        var resolved = ResolveSpecifierToAbsolute(fromFile, specifier, root);
        return resolved is not null && DesignLabSegment.IsMatch(resolved);
    }

    private static string InferRepoRootFromWebSrcFile(string fromFile)
    {
        var normalized = fromFile.Replace('\\', '/');
        const string marker = "/web/src/";
        var index = normalized.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            throw new InvalidOperationException($"Cannot infer repository root from file path: {fromFile}");
        }

        return normalized[..index];
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
