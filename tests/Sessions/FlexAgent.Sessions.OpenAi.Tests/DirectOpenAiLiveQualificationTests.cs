using FlexAgent.Sessions.OpenAi;

namespace FlexAgent.Sessions.OpenAi.Tests;

public sealed class DirectOpenAiLiveQualificationTests
{
    [Fact]
    public void Live_qualification_remains_opt_in_and_is_not_claimed_without_an_owner_profile()
    {
        var enabled = string.Equals(
            Environment.GetEnvironmentVariable("FLEXAGENT_LIVE_OPENAI_QUALIFICATION"),
            "1",
            StringComparison.Ordinal);
        Assert.False(enabled);
        Assert.Equal("sessions.openai.v1", DirectOpenAiModelExecutionAdapter.AdapterContractVersion);
    }
}
