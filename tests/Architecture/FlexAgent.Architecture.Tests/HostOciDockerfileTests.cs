using System.Xml.Linq;

namespace FlexAgent.Architecture.Tests;

public sealed class HostOciDockerfileTests
{
    [Fact]
    public void Worker_dockerfile_copies_referenced_projects_and_embedded_contracts()
    {
        var root = FindRepositoryRoot();
        var dockerfile = File.ReadAllText(Path.Combine(root, "deploy", "docker", "worker.Dockerfile"));
        var copiedSources = ParseCopySources(dockerfile);
        var workerCsproj = Path.Combine(root, "src", "Hosts", "FlexAgent.Worker", "FlexAgent.Worker.csproj");

        foreach (var required in CollectPublishInputs(root, workerCsproj))
        {
            Assert.True(
                copiedSources.Any(copied => IsCoveredByCopy(required, copied)),
                $"worker.Dockerfile must COPY '{required}' so OCI restore/publish can find it.");
        }
    }

    private static IReadOnlyList<string> CollectPublishInputs(string root, string csprojPath)
    {
        var required = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push(csprojPath);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            var projectDirectory = Path.GetDirectoryName(current)!;
            required.Add(ToRepoRelative(root, projectDirectory).TrimEnd('/') + "/");

            var document = XDocument.Load(current);
            foreach (var include in document.Descendants("ProjectReference").Select(el => el.Attribute("Include")?.Value))
            {
                if (string.IsNullOrWhiteSpace(include))
                {
                    continue;
                }

                pending.Push(Path.GetFullPath(Path.Combine(projectDirectory, include.Replace('\\', Path.DirectorySeparatorChar))));
            }

            foreach (var include in document.Descendants("EmbeddedResource").Select(el => el.Attribute("Include")?.Value))
            {
                if (string.IsNullOrWhiteSpace(include))
                {
                    continue;
                }

                var fullPath = Path.GetFullPath(Path.Combine(projectDirectory, include.Replace('\\', Path.DirectorySeparatorChar)));
                if (!fullPath.StartsWith(projectDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    required.Add(ToRepoRelative(root, fullPath));
                }
            }
        }

        return required.OrderBy(path => path, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<string> ParseCopySources(string dockerfile) =>
        dockerfile
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith("COPY ", StringComparison.Ordinal)
                && !line.StartsWith("COPY --from=", StringComparison.Ordinal))
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1].Replace('\\', '/'))
            .ToArray();

    private static bool IsCoveredByCopy(string required, string copied)
    {
        var normalizedRequired = required.Replace('\\', '/');
        var normalizedCopied = copied.TrimEnd('/') + (copied.EndsWith('/') ? "/" : string.Empty);
        if (normalizedCopied.EndsWith('/'))
        {
            return normalizedRequired == normalizedCopied.TrimEnd('/') + "/"
                || normalizedRequired.StartsWith(normalizedCopied, StringComparison.Ordinal);
        }

        return normalizedRequired == normalizedCopied
            || normalizedRequired.StartsWith(normalizedCopied.TrimEnd('/') + "/", StringComparison.Ordinal);
    }

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
