using FlexAgent.Api;
using Microsoft.Extensions.Configuration;

namespace FlexAgent.Runtime.Tests;

public sealed class HumanAuthenticationPersistencePolicyTests
{
    [Fact]
    public void Differing_identity_and_sessions_connection_strings_fail_closed()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = "Host=identity.example;Database=identity",
                ["ConnectionStrings:Sessions"] = "Host=sessions.example;Database=sessions",
            })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(
            () => HumanAuthenticationPersistencePolicy.ResolveConnectionString(configuration));

        Assert.Contains("Identity", error.Message, StringComparison.Ordinal);
        Assert.Contains("Sessions", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Matching_or_single_connection_strings_are_accepted()
    {
        var shared = "Host=shared.example;Database=flexagent";
        var both = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = shared,
                ["ConnectionStrings:Sessions"] = shared,
            })
            .Build();
        var identityOnly = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = shared,
            })
            .Build();

        Assert.Equal(shared, HumanAuthenticationPersistencePolicy.ResolveConnectionString(both));
        Assert.Equal(shared, HumanAuthenticationPersistencePolicy.ResolveConnectionString(identityOnly));
    }
}
