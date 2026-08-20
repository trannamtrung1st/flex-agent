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
        Assert.Equal(4, OpenRouterLiveQualification.Phase21MaxInferenceRequests);
        Assert.Equal("openai/gpt-oss-20b:free", OpenRouterLiveQualification.GptOssDarkbloomModel);
        Assert.Equal("darkbloom", OpenRouterLiveQualification.GptOssDarkbloomProviderSlug);
        Assert.Equal("Darkbloom", OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity);
        Assert.Equal("openrouter.synthetic.local.gpt-oss-20b", OpenRouterLiveQualification.GptOssDarkbloomProfileId);
        Assert.Equal(
            "d392ac50dafcfedd6810afec54016d0e8867f6a7401b61558016382c08b9e7bd",
            OpenRouterLiveQualification.GptOssDarkbloomAdapterDigest);
        Assert.Equal(
            "64f98960972b425ed65e4db960836f59e4bebfd386f0076af295334f49a6ebf5",
            OpenRouterLiveQualification.GptOssDarkbloomProfileDigest);
        Assert.Equal(
            "FLEXAGENT_OPENROUTER_PHASE21_QUALIFICATION_BUDGET_PATH",
            OpenRouterLiveQualification.Phase21BudgetPathEnvironmentVariable);
        Assert.Equal(
            OpenRouterLiveQualification.IsEnabled,
            string.Equals(
                Environment.GetEnvironmentVariable(OpenRouterLiveQualification.EnableEnvironmentVariable),
                "1",
                StringComparison.Ordinal));
    }

    [Fact(Explicit = true, Timeout = 180_000)]
    public Task Gpt_oss_darkbloom_control_then_content_only_after_admitted_decision() =>
        OpenRouterLivePhase21QualificationRunner.RunAsync(output);
}
