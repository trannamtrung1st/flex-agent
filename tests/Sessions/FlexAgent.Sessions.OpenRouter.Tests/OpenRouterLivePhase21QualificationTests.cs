using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

[Collection("OpenRouterLiveQualification")]
public sealed class OpenRouterLivePhase21QualificationTests(ITestOutputHelper output)
{
    [Fact]
    public void Phase21_runner_remains_opt_in_and_binds_exact_gpt_oss_route_constants()
    {
        Assert.Equal("gpt-oss-darkbloom-matrix", OpenRouterLiveQualification.GptOssDarkbloomPhase);
        Assert.Equal(0, OpenRouterLiveQualification.GptOssDarkbloomStartsAtConsumed);
        Assert.Equal(8, OpenRouterLiveQualification.Phase21MaxInferenceRequests);
        Assert.Equal("openai/gpt-oss-20b:free", OpenRouterLiveQualification.GptOssDarkbloomModel);
        Assert.Equal("darkbloom", OpenRouterLiveQualification.GptOssDarkbloomProviderSlug);
        Assert.Equal("Darkbloom", OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity);
        Assert.Equal("openrouter.synthetic.local.gpt-oss-20b", OpenRouterLiveQualification.GptOssDarkbloomProfileId);
        Assert.Equal(
            "a291c5a83dade637fe78fd68b0cb964ecbb704b48723ecad348eaa8a53810e7a",
            OpenRouterLiveQualification.GptOssDarkbloomAdapterDigest);
        Assert.Equal(
            "9b4c641463dd33ab3c8900f00089eebb08e8abb9d1700592ba23292d0e7ce611",
            OpenRouterLiveQualification.GptOssDarkbloomProfileDigest);
        Assert.Equal(
            "FLEXAGENT_OPENROUTER_PHASE21_QUALIFICATION_BUDGET_PATH",
            OpenRouterLiveQualification.Phase21BudgetPathEnvironmentVariable);
        Assert.Equal(
            "FLEXAGENT_OPENROUTER_PHASE21_EVIDENCE_PATH",
            OpenRouterLiveQualification.Phase21EvidencePathEnvironmentVariable);
        Assert.Equal(
            OpenRouterLiveQualification.IsEnabled,
            string.Equals(
                Environment.GetEnvironmentVariable(OpenRouterLiveQualification.EnableEnvironmentVariable),
                "1",
                StringComparison.Ordinal));
    }

    [Fact(Explicit = true, Timeout = 400_000)]
    public Task Gpt_oss_darkbloom_control_then_content_only_after_admitted_decision() =>
        OpenRouterLivePhase21QualificationRunner.RunAsync(output);
}
