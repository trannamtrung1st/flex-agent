using FlexAgent.Sessions.OpenAiCompatible;

namespace FlexAgent.Sessions.OpenAiCompatible.Tests;

public sealed class OpenAiCompatibleLiveQualificationTests
{
    [Fact]
    public void Live_qualification_remains_opt_in_and_is_not_claimed_without_an_owner_profile()
    {
        var enabled = string.Equals(
            Environment.GetEnvironmentVariable(OpenAiCompatibleAdapterContracts.LiveQualificationEnvironmentVariable),
            "1",
            StringComparison.Ordinal);
        Assert.False(enabled);
        Assert.Equal("sessions.openai_compatible.v1", OpenAiCompatibleModelExecutionAdapter.AdapterContractVersion);
        Assert.Equal("openai_compatible", OpenAiCompatibleAdapterContracts.AdapterKind);
        Assert.Equal("direct_openai", OpenAiCompatibleAdapterContracts.HistoricalAdapterKind);
        Assert.Equal("sessions.openai.v1", OpenAiCompatibleAdapterContracts.HistoricalAdapterContractVersion);
    }
}
