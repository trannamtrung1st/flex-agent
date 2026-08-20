using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterLiveRunGateTests
{
    [Fact]
    public void Discovery_stays_retired_after_the_recorded_pin()
    {
        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.DiscoveryPhase,
                OpenRouterLiveQualification.DiscoveryPhase,
                currentConsumed: 6,
                expectedConsumedText: "6",
                out var discoveryDenial));
        Assert.Equal("discovery_retired", discoveryDenial);
    }

    [Theory]
    [InlineData(null, "0", "phase_mismatch")]
    [InlineData("discovery", null, "expected_consumed_mismatch")]
    [InlineData("discovery", "1", "expected_consumed_mismatch")]
    [InlineData("pinned-matrix", "0", "phase_mismatch")]
    [InlineData("discovery", "6", "discovery_retired")]
    public void Mismatched_or_retired_gates_fail_closed(
        string? configuredPhase,
        string? expected,
        string denial)
    {
        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.DiscoveryPhase,
                configuredPhase,
                currentConsumed: 6,
                expectedConsumedText: expected,
                out var reason));
        Assert.Equal(denial, reason);
    }

    [Fact]
    public void Matching_fresh_discovery_authorizes_only_before_retirement()
    {
        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.DiscoveryPhase,
                OpenRouterLiveQualification.DiscoveryPhase,
                currentConsumed: 5,
                expectedConsumedText: "5",
                out var discovery));
        Assert.Equal(string.Empty, discovery);
    }

    [Fact]
    public void Retired_candidate_phases_fail_closed_even_when_count_matches()
    {
        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                "pinned-matrix",
                "pinned-matrix",
                currentConsumed: 6,
                expectedConsumedText: "6",
                out var lightning));
        Assert.Equal("retired_candidate", lightning);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                "gemma-darkbloom-matrix",
                "gemma-darkbloom-matrix",
                currentConsumed: 9,
                expectedConsumedText: "9",
                out var gemma));
        Assert.Equal("retired_candidate", gemma);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                "nemotron-nano-backup-matrix",
                "nemotron-nano-backup-matrix",
                currentConsumed: 10,
                expectedConsumedText: "10",
                out var nano));
        Assert.Equal("retired_candidate", nano);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                "glm-5-2-decart-matrix",
                "glm-5-2-decart-matrix",
                currentConsumed: 19,
                expectedConsumedText: "19",
                out var glm));
        Assert.Equal("retired_candidate", glm);
    }
}
