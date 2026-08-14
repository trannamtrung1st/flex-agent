using System.Reflection;
using FlexAgent.Contracts.Session;
using FlexAgent.Sessions.Domain;
using NetArchTest.Rules;
using Npgsql;

namespace FlexAgent.Architecture.Tests;

public sealed class SessionsBoundaryTests
{
    private static readonly Assembly SessionsAssembly = typeof(P0TextSessionRuntimeCapabilityPolicy).Assembly;

    [Fact]
    public void Sessions_domain_does_not_depend_on_application_layer()
    {
        ArchitectureTestSupport.AssertDomainDoesNotDependOnLayer(
            SessionsAssembly,
            "FlexAgent.Sessions.Application");
    }

    [Fact]
    public void Sessions_domain_does_not_depend_on_infrastructure_layer()
    {
        ArchitectureTestSupport.AssertDomainDoesNotDependOnLayer(
            SessionsAssembly,
            "FlexAgent.Sessions.Infrastructure");
    }

    [Fact]
    public void Sessions_domain_does_not_reference_forbidden_infrastructure_or_host_dependencies()
    {
        var result = Types.InAssembly(SessionsAssembly)
            .That()
            .ResideInNamespaceContaining(".Domain")
            .ShouldNot()
            .HaveDependencyOnAny(ArchitectureTestSupport.ForbiddenSessionsInfrastructurePrefixes)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Sessions_domain_does_not_depend_on_json_schema_net()
    {
        var result = Types.InAssembly(SessionsAssembly)
            .That()
            .ResideInNamespaceContaining(".Domain")
            .ShouldNot()
            .HaveDependencyOn("Json.Schema")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Sessions_assembly_does_not_reference_forbidden_infrastructure_or_host_dependencies()
    {
        ArchitectureTestSupport.AssertNoForbiddenDependencies(
            SessionsAssembly,
            ArchitectureTestSupport.ForbiddenSessionsInfrastructurePrefixes,
            SessionsAssembly.GetName().Name);
    }

    [Fact]
    public void Sessions_assembly_does_not_reference_other_module_infrastructure_namespaces()
    {
        foreach (var forbiddenNamespace in ArchitectureTestSupport.ForbiddenModuleInfrastructureNamespaces)
        {
            var result = Types.InAssembly(SessionsAssembly)
                .ShouldNot()
                .HaveDependencyOn(forbiddenNamespace)
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                $"{forbiddenNamespace}: {string.Join(Environment.NewLine, result.FailingTypeNames ?? [])}");
        }
    }

    [Fact]
    public void Negative_control_detects_domain_dependency_on_application_layer()
    {
        ArchitectureTestSupport.AssertNegativeControlDetectsDomainLayerDependency<ViolatingDomainDependsOnApplication>(
            "FlexAgent.Sessions.Application");
    }

    [Fact]
    public void Negative_control_detects_domain_dependency_on_infrastructure_layer()
    {
        ArchitectureTestSupport.AssertNegativeControlDetectsDomainLayerDependency<ViolatingDomainDependsOnInfrastructure>(
            "FlexAgent.Sessions.Infrastructure");
    }

    [Fact]
    public void Negative_control_detects_domain_type_that_references_npgsql()
    {
        ArchitectureTestSupport.AssertNegativeControlDetectsForbiddenDependency<SessionsNegativeControlFixtures.ViolatingDomainType>(
            "Npgsql");
    }

    [Fact]
    public void Negative_control_detects_type_that_references_aspnetcore()
    {
        ArchitectureTestSupport.AssertNegativeControlDetectsForbiddenDependency<SessionsNegativeControlFixtures.ViolatingHostAuthorityType>(
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Negative_control_detects_type_that_references_contracts()
    {
        ArchitectureTestSupport.AssertNegativeControlDetectsForbiddenDependency<SessionsNegativeControlFixtures.ViolatingBrowserContractType>(
            "FlexAgent.Contracts");
    }
}

internal static class SessionsNegativeControlFixtures
{
    internal sealed class ViolatingDomainType
    {
        public object CreateClient() => new NpgsqlConnection();
    }

    internal sealed class ViolatingHostAuthorityType
    {
        public object CreateContext() => new Microsoft.AspNetCore.Http.DefaultHttpContext();
    }

    internal sealed class ViolatingBrowserContractType
    {
        public object CreateEnvelope() => new SessionMessageSendCommandV1(
            "v1",
            "session.message.send",
            "cmd.negative-control",
            "idem.negative-control",
            new SessionLocatorV1("sess.negative"),
            1,
            null,
            new MessageSendPayloadV1("negative control"));
    }
}
