using System.Reflection;
using NetArchTest.Rules;

namespace FlexAgent.Architecture.Tests;

public sealed class CompositionRootTests
{
    [Fact]
    public void Api_and_worker_hosts_use_expected_assembly_names()
    {
        var apiAssembly = typeof(FlexAgent.Api.Program).Assembly;
        var workerAssembly = typeof(FlexAgent.Worker.Program).Assembly;

        Assert.Equal("FlexAgent.Api", apiAssembly.GetName().Name);
        Assert.Equal("FlexAgent.Worker", workerAssembly.GetName().Name);
    }

    [Fact]
    public void Host_assemblies_do_not_reference_test_projects()
    {
        var apiAssembly = typeof(FlexAgent.Api.Program).Assembly;
        var workerAssembly = typeof(FlexAgent.Worker.Program).Assembly;

        Assert.DoesNotContain("Tests", apiAssembly.GetReferencedAssemblies().Select(a => a.Name), StringComparer.Ordinal);
        Assert.DoesNotContain("Tests", workerAssembly.GetReferencedAssemblies().Select(a => a.Name), StringComparer.Ordinal);
    }

    [Fact]
    public void Api_host_does_not_depend_on_provider_packages()
    {
        var result = Types.InAssembly(typeof(FlexAgent.Api.Program).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("OpenAI", "AWSSDK")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Worker_host_does_not_depend_on_provider_packages()
    {
        var result = Types.InAssembly(typeof(FlexAgent.Worker.Program).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("OpenAI", "AWSSDK")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
