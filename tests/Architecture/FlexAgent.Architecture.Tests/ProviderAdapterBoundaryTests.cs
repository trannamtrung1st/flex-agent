using FlexAgent.Sessions.OpenAi;
using FlexAgent.Sessions.OpenRouter;
using NetArchTest.Rules;
using OpenAI;

namespace FlexAgent.Architecture.Tests;

public sealed class ProviderAdapterBoundaryTests
{
    [Fact]
    public void Only_the_sessions_openai_adapter_references_the_openai_sdk()
    {
        Assert.Contains(
            typeof(DirectOpenAiModelExecutionAdapter).Assembly.GetReferencedAssemblies(),
            assembly => string.Equals(assembly.Name, "OpenAI", StringComparison.Ordinal));
        Assert.True(
            Types.InAssembly(typeof(DirectOpenAiModelExecutionAdapter).Assembly)
                .That()
                .HaveName(nameof(DirectOpenAiModelExecutionAdapter))
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
    public void OpenRouter_adapter_does_not_reference_the_openai_sdk_or_direct_openai_adapter()
    {
        var assembly = typeof(OpenRouterModelExecutionAdapter).Assembly;
        Assert.DoesNotContain(
            assembly.GetReferencedAssemblies(),
            referenced => string.Equals(referenced.Name, "OpenAI", StringComparison.Ordinal)
                || string.Equals(referenced.Name, "FlexAgent.Sessions.OpenAi", StringComparison.Ordinal));

        var openai = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn("OpenAI")
            .GetResult();
        Assert.True(openai.IsSuccessful, string.Join(Environment.NewLine, openai.FailingTypeNames ?? []));

        var direct = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn("FlexAgent.Sessions.OpenAi")
            .GetResult();
        Assert.True(direct.IsSuccessful, string.Join(Environment.NewLine, direct.FailingTypeNames ?? []));
    }

    [Fact]
    public void Direct_openai_adapter_does_not_reference_openrouter()
    {
        var result = Types.InAssembly(typeof(DirectOpenAiModelExecutionAdapter).Assembly)
            .ShouldNot()
            .HaveDependencyOn("FlexAgent.Sessions.OpenRouter")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
