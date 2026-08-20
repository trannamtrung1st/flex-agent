using FlexAgent.Sessions.OpenAiCompatible;
using FlexAgent.Sessions.OpenRouter;
using NetArchTest.Rules;
using OpenAI;

namespace FlexAgent.Architecture.Tests;

public sealed class ProviderAdapterBoundaryTests
{
    [Fact]
    public void Only_the_sessions_openai_compatible_adapter_references_the_openai_sdk()
    {
        Assert.Contains(
            typeof(OpenAiCompatibleModelExecutionAdapter).Assembly.GetReferencedAssemblies(),
            assembly => string.Equals(assembly.Name, "OpenAI", StringComparison.Ordinal));
        Assert.True(
            Types.InAssembly(typeof(OpenAiCompatibleModelExecutionAdapter).Assembly)
                .That()
                .HaveName(nameof(OpenAiCompatibleModelExecutionAdapter))
                .Should()
                .HaveDependencyOn("OpenAI")
                .GetResult()
                .IsSuccessful);

        foreach (var assembly in new[]
                 {
                     typeof(FlexAgent.Sessions.Domain.SessionOwnership).Assembly,
                     typeof(FlexAgent.Sessions.Infrastructure.PostgresTrustedSessionBindingSource).Assembly,
                     typeof(OpenRouterModelExecutionAdapter).Assembly,
                     typeof(FlexAgent.Api.Program).Assembly,
                     typeof(FlexAgent.Worker.Program).Assembly,
                     typeof(FlexAgent.Contracts.Session.SessionLocatorV1).Assembly,
                 })
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn("OpenAI")
                .GetResult();
            Assert.True(
                result.IsSuccessful,
                $"{assembly.GetName().Name}: {string.Join(Environment.NewLine, result.FailingTypeNames ?? [])}");
        }
    }

    [Fact]
    public void Negative_control_detects_an_openai_sdk_type_reference()
    {
        ArchitectureTestSupport.AssertNegativeControlDetectsForbiddenDependency<OpenAiNegativeControl>(
            "OpenAI");
    }

    private sealed class OpenAiNegativeControl
    {
        public object Create() => new OpenAIClient("negative-control");
    }

    [Fact]
    public void OpenRouter_adapter_does_not_reference_the_openai_sdk_or_openai_compatible_adapter()
    {
        var assembly = typeof(OpenRouterModelExecutionAdapter).Assembly;
        Assert.DoesNotContain(
            assembly.GetReferencedAssemblies(),
            referenced => string.Equals(referenced.Name, "OpenAI", StringComparison.Ordinal)
                || string.Equals(referenced.Name, "FlexAgent.Sessions.OpenAiCompatible", StringComparison.Ordinal)
                || string.Equals(referenced.Name, "FlexAgent.Sessions.OpenAi", StringComparison.Ordinal));

        var openai = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn("OpenAI")
            .GetResult();
        Assert.True(openai.IsSuccessful, string.Join(Environment.NewLine, openai.FailingTypeNames ?? []));

        var compatible = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn("FlexAgent.Sessions.OpenAiCompatible")
            .GetResult();
        Assert.True(compatible.IsSuccessful, string.Join(Environment.NewLine, compatible.FailingTypeNames ?? []));
    }

    [Fact]
    public void OpenAi_compatible_adapter_does_not_reference_openrouter()
    {
        var result = Types.InAssembly(typeof(OpenAiCompatibleModelExecutionAdapter).Assembly)
            .ShouldNot()
            .HaveDependencyOn("FlexAgent.Sessions.OpenRouter")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
