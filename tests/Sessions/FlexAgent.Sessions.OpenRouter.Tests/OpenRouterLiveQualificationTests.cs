using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

[Collection("OpenRouterLiveQualification")]
public sealed class OpenRouterLiveQualificationTests(ITestOutputHelper output)
{
    [Fact(Explicit = true)]
    public async Task Discovery_returns_one_pinnable_free_model_and_provider_within_the_persistent_budget()
    {
        Assert.True(
            OpenRouterLiveQualification.IsEnabled,
            $"Set {OpenRouterLiveQualification.EnableEnvironmentVariable}=1 only for an approved live run.");
        Assert.True(
            OpenRouterLiveQualification.SyntheticDataPolicyAccepted,
            $"Set {OpenRouterLiveQualification.SyntheticDataPolicyEnvironmentVariable}=1 only after confirming every disclosed value is synthetic and accepting retention/training risk.");

        var budgetPath = Environment.GetEnvironmentVariable(
            OpenRouterLiveQualification.BudgetPathEnvironmentVariable);
        Assert.False(string.IsNullOrWhiteSpace(budgetPath));
        var budget = new OpenRouterQualificationBudget(budgetPath);
        Assert.True(budget.TryRead(out var alreadyConsumed));
        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.DiscoveryPhase,
                alreadyConsumed,
                out var denial),
            $"Sanitized live discovery refused before reserve: {denial} consumed={alreadyConsumed}.");
        Assert.True(
            budget.TryReserveExpected(alreadyConsumed, out var reservedRequestCount),
            "The persistent qualification budget is unavailable, corrupt, busy, stale, or exhausted.");

        var secretRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "flex-agent",
            "secrets");
        var secretSource = new UnixOwnerOnlyMountedFileProviderSecretSource(secretRoot);
        using var secret = await secretSource.TryReadAsync(
            "openrouter-api-key",
            TestContext.Current.CancellationToken);
        Assert.NotNull(secret);

        var outcome = await new OpenRouterDiscoveryClient().DiscoverOutcomeAsync(
            secret.Reveal(),
            TestContext.Current.CancellationToken);

        Assert.True(
            outcome.Candidate is not null,
            $"Sanitized discovery failure: category={outcome.FailureReason ?? "none"} status={outcome.HttpStatusCode?.ToString() ?? "none"}.");
        var candidate = outcome.Candidate;
        output.WriteLine(
            "sanitized_discovery request={0}/{1} model={2} provider={3}",
            reservedRequestCount,
            OpenRouterLiveQualification.MaxInferenceRequests,
            candidate!.Model,
            candidate.ProviderIdentity);
    }
}
