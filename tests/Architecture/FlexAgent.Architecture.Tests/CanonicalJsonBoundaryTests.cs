using FlexAgent.CanonicalJson;

namespace FlexAgent.Architecture.Tests;

public sealed class CanonicalJsonBoundaryTests
{
    [Fact]
    public void CanonicalJson_has_no_nuget_or_foreign_assembly_references()
    {
        var assembly = typeof(CanonicalJsonProcessor).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.All(references, reference =>
            Assert.True(
                reference is null or "System.Runtime" or "System.Collections" or "System.Linq"
                or "netstandard" or "mscorlib"
                || reference.StartsWith("System.", StringComparison.Ordinal),
                $"Unexpected assembly reference: {reference}"));
    }

    [Fact]
    public void Host_assemblies_do_not_reference_canonical_json_yet()
    {
        var hostAssemblies = new[]
        {
            typeof(FlexAgent.Api.Program).Assembly,
            typeof(FlexAgent.Worker.Program).Assembly,
        };

        foreach (var assembly in hostAssemblies)
        {
            Assert.DoesNotContain(
                "FlexAgent.CanonicalJson",
                assembly.GetReferencedAssemblies().Select(a => a.Name),
                StringComparer.Ordinal);
        }
    }

    [Fact]
    public void Application_public_surface_is_wrapper_only()
    {
        var assembly = typeof(CanonicalJsonProcessor).Assembly;
        var applicationTypes = assembly.GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith("FlexAgent.CanonicalJson", StringComparison.Ordinal) == true)
            .Select(type => type.FullName)
            .ToArray();

        Assert.Equal(
            [
                "FlexAgent.CanonicalJson.CanonicalJsonException",
                "FlexAgent.CanonicalJson.CanonicalJsonFailureCategory",
                "FlexAgent.CanonicalJson.CanonicalJsonLimits",
                "FlexAgent.CanonicalJson.CanonicalJsonProcessor",
            ],
            applicationTypes.OrderBy(name => name, StringComparer.Ordinal));
    }
}
