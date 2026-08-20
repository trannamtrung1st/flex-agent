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
            "7559112b33caad06504136309a0216e7bdf7643391a8bb7b4084245c517092fd",
            OpenRouterLiveQualification.GptOssDarkbloomAdapterDigest);
        Assert.Equal(
            "fb1fb631fc25dcc05c07b19345c00986f4120d34e751b3a922d1df7bc3d04b48",
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

    [Fact(Explicit = true, Timeout = 400_000)]
    public Task Gpt_oss_darkbloom_control_then_content_only_after_admitted_decision() =>
        OpenRouterLivePhase21QualificationRunner.RunAsync(output);
}
