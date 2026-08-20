using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

[Collection("OpenRouterLiveQualification")]
public sealed class OpenRouterLivePhase20QualificationTests(ITestOutputHelper output)
{
    [Fact]
    public void Phase20_runners_remain_opt_in_and_bind_exact_route_constants()
    {
        Assert.Equal("gemma-darkbloom-matrix", OpenRouterLiveQualification.GemmaDarkbloomPhase);
        Assert.Equal("nemotron-nano-backup-matrix", OpenRouterLiveQualification.NemotronNanoBackupPhase);
        Assert.Equal(9, OpenRouterLiveQualification.GemmaDarkbloomStartsAtConsumed);
        Assert.Equal(10, OpenRouterLiveQualification.NemotronNanoBackupStartsAtConsumed);
        Assert.Equal("google/gemma-4-26b-a4b-it:free", OpenRouterLiveQualification.GemmaDarkbloomModel);
        Assert.Equal("darkbloom", OpenRouterLiveQualification.GemmaDarkbloomProviderSlug);
        Assert.Equal("Darkbloom", OpenRouterLiveQualification.GemmaDarkbloomProviderIdentity);
        Assert.Equal("e442124b72a4a9d71ec3f5c39f64ce7d3a661de3e9211ef0641bc297ec631e52", OpenRouterLiveQualification.GemmaDarkbloomAdapterDigest);
        Assert.Equal("48a2e696b6d0970ea58d9a5a040ccc4ff25c4e6d089447aa2dbe66c21f5d7ad9", OpenRouterLiveQualification.GemmaDarkbloomProfileDigest);
        Assert.Equal("nvidia/nemotron-nano-9b-v2:free", OpenRouterLiveQualification.NemotronNanoBackupModel);
        Assert.Equal(
            OpenRouterLiveQualification.IsEnabled,
            string.Equals(
                Environment.GetEnvironmentVariable(OpenRouterLiveQualification.EnableEnvironmentVariable),
                "1",
                StringComparison.Ordinal));
    }

    [Fact(Explicit = true, Timeout = 180_000)]
    public Task Gemma_darkbloom_control_then_content_only_after_admitted_decision() =>
        OpenRouterLivePhase20QualificationRunner.RunAsync(
            output,
            OpenRouterLiveQualification.GemmaDarkbloomPhase,
            OpenRouterLivePinnedRouteAcceptance.GemmaDarkbloom,
            "synthetic.openrouter.phase20.gemma.control",
            "Say only: ok");

    [Fact(Explicit = true, Timeout = 180_000)]
    public Task Nemotron_nano_backup_control_then_content_only_after_admitted_decision() =>
        OpenRouterLivePhase20QualificationRunner.RunAsync(
            output,
            OpenRouterLiveQualification.NemotronNanoBackupPhase,
            OpenRouterLivePinnedRouteAcceptance.NemotronNanoBackup,
            "synthetic.openrouter.phase20.nano.control",
            "Say only: ok");
}
