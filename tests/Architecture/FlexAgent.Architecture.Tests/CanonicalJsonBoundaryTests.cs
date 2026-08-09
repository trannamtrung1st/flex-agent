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
    public void Assembly_exports_only_application_wrapper_surface()
    {
        var assembly = typeof(CanonicalJsonProcessor).Assembly;
        var exportedTypes = assembly.GetExportedTypes()
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "FlexAgent.CanonicalJson.CanonicalJsonException",
                "FlexAgent.CanonicalJson.CanonicalJsonFailureCategory",
                "FlexAgent.CanonicalJson.CanonicalJsonLimits",
                "FlexAgent.CanonicalJson.CanonicalJsonProcessor",
            ],
            exportedTypes);
    }
}
