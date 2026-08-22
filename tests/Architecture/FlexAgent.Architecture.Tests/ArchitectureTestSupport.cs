using System.Reflection;
using NetArchTest.Rules;

namespace FlexAgent.Architecture.Tests;

internal static class ArchitectureTestSupport
{
    internal static readonly string[] ForbiddenPersistencePrefixes =
    [
        "Npgsql",
        "Dapper",
    ];

    internal static readonly string[] ForbiddenSessionsInfrastructurePrefixes =
    [
        "Microsoft.AspNetCore",
        "Npgsql",
        "Dapper",
        "FlexAgent.Postgres",
        "OpenAI",
        "AWSSDK",
        "FlexAgent.Api",
        "FlexAgent.Worker",
        "FlexAgent.Contracts",
        "FlexAgent.SyntheticBrowser",
        "FlexAgent.Sessions.OpenRouter",
        "OpenTelemetry",
    ];

    internal static readonly string[] ForbiddenModuleInfrastructureNamespaces =
    [
        "FlexAgent.IdentityAccess.Infrastructure",
        "FlexAgent.Configuration.Infrastructure",
        "FlexAgent.AssessmentConfiguration.Infrastructure",
        "FlexAgent.Submissions.Infrastructure",
    ];

    internal static void AssertNoForbiddenDependencies(
        Assembly assembly,
        IReadOnlyList<string> forbiddenPrefixes,
        string? owner = null)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenPrefixes.ToArray())
            .GetResult();

        var prefix = owner is null ? string.Empty : $"{owner}: ";
        Assert.True(result.IsSuccessful, prefix + string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    internal static void AssertDomainDoesNotDependOnLayer(Assembly assembly, string layerNamespace)
    {
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespaceContaining(".Domain")
            .ShouldNot()
            .HaveDependencyOn(layerNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    internal static void AssertNegativeControlDetectsForbiddenDependency<TControl>(
        string forbiddenPrefix,
        string? controlName = null)
        where TControl : class
    {
        var result = Types.InAssembly(typeof(TControl).Assembly)
            .That()
            .HaveName(controlName ?? typeof(TControl).Name)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenPrefix)
            .GetResult();

        Assert.False(result.IsSuccessful);
    }

    internal static void AssertNegativeControlDetectsDomainLayerDependency<TControl>(
        string layerNamespace,
        string? controlName = null)
        where TControl : class
    {
        var result = Types.InAssembly(typeof(TControl).Assembly)
            .That()
            .HaveName(controlName ?? typeof(TControl).Name)
            .Should()
            .ResideInNamespaceContaining(".Domain")
            .And()
            .HaveDependencyOn(layerNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
