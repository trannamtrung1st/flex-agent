using FlexAgent.Sessions.OpenAi;
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
}
