using FlexAgent.Contracts.Session;

namespace FlexAgent.Architecture.Tests;

public sealed class ContractsBoundaryTests
{
    [Fact]
    public void Contracts_has_no_nuget_or_host_references()
    {
        var assembly = typeof(ISessionCommandEnvelopeV1).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.All(references, reference =>
            Assert.True(
                reference is null or "System.Runtime" or "System.Collections" or "System.Linq"
                or "netstandard" or "mscorlib"
                || reference.StartsWith("System.", StringComparison.Ordinal),
                $"Unexpected assembly reference: {reference}"));
    }

    [Fact]
    public void Contracts_exports_only_browser_safe_dto_surface()
    {
        var exported = typeof(ISessionCommandEnvelopeV1).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.All(exported, name =>
        {
            Assert.DoesNotContain("Secret", name, StringComparison.Ordinal);
            Assert.DoesNotContain("Authorization", name, StringComparison.Ordinal);
            Assert.DoesNotContain("Credential", name, StringComparison.Ordinal);
        });
    }
}
