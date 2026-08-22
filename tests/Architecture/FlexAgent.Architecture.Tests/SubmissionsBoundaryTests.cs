using System.Reflection;
using FlexAgent.Submissions.Domain;
using NetArchTest.Rules;
using Npgsql;

namespace FlexAgent.Architecture.Tests;

public sealed class SubmissionsBoundaryTests
{
    private static readonly Assembly SubmissionsAssembly = typeof(EnrollmentAuthorizationActions).Assembly;

    [Fact]
    public void Submissions_domain_does_not_depend_on_application_layer()
    {
        ArchitectureTestSupport.AssertDomainDoesNotDependOnLayer(
            SubmissionsAssembly,
            "FlexAgent.Submissions.Application");
    }

    [Fact]
    public void Submissions_domain_does_not_depend_on_infrastructure_layer()
    {
        ArchitectureTestSupport.AssertDomainDoesNotDependOnLayer(
            SubmissionsAssembly,
            "FlexAgent.Submissions.Infrastructure");
    }

    [Fact]
    public void Submissions_domain_and_application_do_not_reference_persistence_packages()
    {
        var result = Types.InAssembly(SubmissionsAssembly)
            .That()
            .ResideInNamespaceContaining(".Domain")
            .Or()
            .ResideInNamespaceContaining(".Application")
            .ShouldNot()
            .HaveDependencyOnAny(ArchitectureTestSupport.ForbiddenPersistencePrefixes)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Submissions_assembly_does_not_reference_other_module_infrastructure_namespaces()
    {
        foreach (var forbiddenNamespace in ArchitectureTestSupport.ForbiddenModuleInfrastructureNamespaces)
        {
            var result = Types.InAssembly(SubmissionsAssembly)
                .ShouldNot()
                .HaveDependencyOn(forbiddenNamespace)
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                $"{forbiddenNamespace}: {string.Join(Environment.NewLine, result.FailingTypeNames ?? [])}");
        }
    }

    [Fact]
    public void Submissions_infrastructure_does_not_query_identity_application_sessions()
    {
        var infrastructure = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Modules",
            "Submissions",
            "FlexAgent.Submissions.Infrastructure");
        Assert.True(Directory.Exists(infrastructure), infrastructure);
        foreach (var path in Directory.EnumerateFiles(infrastructure, "*.cs", SearchOption.AllDirectories))
        {
            Assert.DoesNotContain(
                "application_sessions",
                File.ReadAllText(path),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Negative_control_detects_domain_type_that_references_npgsql()
    {
        ArchitectureTestSupport.AssertNegativeControlDetectsForbiddenDependency<SubmissionsNegativeControlFixtures.ViolatingDomainType>(
            "Npgsql");
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

internal static class SubmissionsNegativeControlFixtures
{
    internal sealed class ViolatingDomainType
    {
        public object CreateClient() => new NpgsqlConnection();
    }
}
