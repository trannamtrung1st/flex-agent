using System.Text.Json;

namespace FlexAgent.Runtime.Tests;

public sealed class KeycloakContractProfileTests
{
    [Fact]
    public void Realm_client_configures_backchannel_logout_to_the_api()
    {
        var realmPath = Path.Combine(
            FindRepositoryRoot(),
            "deploy",
            "compose",
            "keycloak",
            "flex-agent-realm.json");

        using var document = JsonDocument.Parse(File.ReadAllText(realmPath));
        var client = document.RootElement.GetProperty("clients").EnumerateArray()
            .Single(item => item.GetProperty("clientId").GetString() == "flex-agent-api");
        var attributes = client.GetProperty("attributes");

        Assert.Equal(
            "http://host.docker.internal:18082",
            client.GetProperty("adminUrl").GetString());
        Assert.Equal(
            "http://host.docker.internal:18082/auth/backchannel-logout",
            attributes.GetProperty("backchannel.logout.url").GetString());
        Assert.Equal("S256", attributes.GetProperty("pkce.code.challenge.method").GetString());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FlexAgent.slnx"))
                || File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
