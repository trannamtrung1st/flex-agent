using System.Reflection;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using NetArchTest.Rules;
using Npgsql;

namespace FlexAgent.Architecture.Tests;

public sealed class EvaluationBoundaryTests
{
    private static readonly Assembly EvaluationAssembly = typeof(EvaluationOwnership).Assembly;
    private static readonly Assembly EvaluationInfrastructureAssembly = typeof(EvaluationInfrastructure).Assembly;

    [Fact]
    public void Evaluation_domain_does_not_depend_on_application_layer()
    {
        ArchitectureTestSupport.AssertDomainDoesNotDependOnLayer(
            EvaluationAssembly,
            "FlexAgent.Evaluation.Application");
    }

    [Fact]
    public void Evaluation_domain_does_not_depend_on_infrastructure_layer()
    {
        ArchitectureTestSupport.AssertDomainDoesNotDependOnLayer(
            EvaluationAssembly,
            "FlexAgent.Evaluation.Infrastructure");
    }

    [Fact]
    public void Evaluation_domain_and_application_do_not_reference_persistence_packages()
    {
        var result = Types.InAssembly(EvaluationAssembly)
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
    public void Evaluation_assemblies_do_not_reference_hosts_providers_or_other_module_infrastructure()
    {
        foreach (var assembly in new[] { EvaluationAssembly, EvaluationInfrastructureAssembly })
        {
            ArchitectureTestSupport.AssertNoForbiddenDependencies(
                assembly,
                ArchitectureTestSupport.ForbiddenEvaluationPrefixes,
                assembly.GetName().Name);

            foreach (var forbiddenNamespace in ArchitectureTestSupport.ForbiddenModuleInfrastructureNamespaces)
            {
                if (forbiddenNamespace == "FlexAgent.Evaluation.Infrastructure"
                    && assembly == EvaluationInfrastructureAssembly)
                {
                    continue;
                }

                var result = Types.InAssembly(assembly)
                    .ShouldNot()
                    .HaveDependencyOn(forbiddenNamespace)
                    .GetResult();
                Assert.True(
                    result.IsSuccessful,
                    $"{assembly.GetName().Name} -> {forbiddenNamespace}: {string.Join(Environment.NewLine, result.FailingTypeNames ?? [])}");
            }
        }
    }

    [Fact]
    public void Evaluation_source_does_not_import_design_lab()
    {
        var root = FindRepositoryRoot();
        var evaluationRoot = Path.Combine(root, "src", "Modules", "Evaluation");
        Assert.True(Directory.Exists(evaluationRoot), evaluationRoot);
        foreach (var path in Directory.EnumerateFiles(evaluationRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("design-lab", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DesignLab", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Negative_control_detects_domain_type_that_references_npgsql()
    {
        ArchitectureTestSupport.AssertNegativeControlDetectsForbiddenDependency<EvaluationNegativeControlFixtures.ViolatingDomainType>(
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

internal static class EvaluationNegativeControlFixtures
{
    internal sealed class ViolatingDomainType
    {
        public object CreateClient() => new NpgsqlConnection();
    }
}
