using System.Reflection;
using FlexAgent.Configuration.Application;
using FlexAgent.IdentityAccess.Application;
using NetArchTest.Rules;

namespace FlexAgent.Architecture.Tests;

public sealed class ModuleBoundaryTests
{
  private static readonly Assembly IdentityAccessAssembly = typeof(IAuthorizationKernel).Assembly;
  private static readonly Assembly ConfigurationAssembly = typeof(IRegisterConfigurationSourceVersionHandler).Assembly;

  [Fact]
  public void Domain_and_application_layers_do_not_reference_persistence_packages()
  {
      foreach (var assembly in new[] { IdentityAccessAssembly, ConfigurationAssembly })
      {
          var result = Types.InAssembly(assembly)
              .That()
              .ResideInNamespaceContaining(".Domain")
              .Or()
              .ResideInNamespaceContaining(".Application")
              .ShouldNot()
              .HaveDependencyOnAny(ArchitectureTestSupport.ForbiddenPersistencePrefixes)
              .GetResult();

          Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
      }
  }

  [Fact]
  public void Configuration_application_does_not_reference_canonical_json_directly()
  {
      var result = Types.InAssembly(ConfigurationAssembly)
          .That()
          .ResideInNamespaceContaining(".Application")
          .ShouldNot()
          .HaveDependencyOn("FlexAgent.CanonicalJson")
          .GetResult();

      Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
  }

  [Fact]
  public void Protected_repository_methods_require_organization_scope_parameters()
  {
      var repositoryType = ConfigurationAssembly.GetType(
          "FlexAgent.Configuration.Infrastructure.PostgresConfigurationSourceVersionRepository",
          throwOnError: true)!;

      var scopedMethods = new[]
      {
          "ListForSourceAsync",
          "CountForSourceAsync",
          "SourceExistsInOrganizationAsync",
          "GetByIdForSourceAsync",
      };

      foreach (var methodName in scopedMethods)
      {
          var method = repositoryType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
          Assert.NotNull(method);

          var hasOrganizationScope = method!.GetParameters()
              .Any(p => p.Name is "organizationId" or "OrganizationId");

          Assert.True(hasOrganizationScope, $"{methodName} must accept trusted organization scope.");
      }

      var unscopedGetById = repositoryType.GetMethod(
          "GetById",
          BindingFlags.Instance | BindingFlags.Public);

      Assert.Null(unscopedGetById);
  }
}
