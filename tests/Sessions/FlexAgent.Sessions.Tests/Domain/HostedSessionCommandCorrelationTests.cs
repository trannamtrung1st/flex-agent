using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class HostedSessionCommandCorrelationTests
{
    [Fact]
    public void Guid_command_ids_are_used_directly()
    {
        var commandId = "7f1d3c2e-4a90-4b11-8c22-9d33e44f55a6";

        Assert.Equal(Guid.Parse(commandId), HostedSessionCommandCorrelation.ForCommandId(commandId));
    }

    [Fact]
    public void Stable_command_ids_share_one_deterministic_correlation()
    {
        const string commandId = "sessioncommand.abc123";

        var first = HostedSessionCommandCorrelation.ForCommandId(commandId);
        var second = HostedSessionCommandCorrelation.ForCommandId(commandId);

        Assert.Equal(first, second);
        Assert.NotEqual(Guid.Empty, first);
        Assert.NotEqual(
            HostedSessionCommandCorrelation.ForCommandId("sessioncommand.other"),
            first);
    }
}
