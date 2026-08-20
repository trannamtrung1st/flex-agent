using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterLiveRunGateTests
{
    [Fact]
    public void Recorded_nine_of_twelve_state_refuses_both_live_phases_before_reserve()
    {
        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.PinnedMatrixPhase,
                OpenRouterLiveQualification.PinnedMatrixPhase,
                currentConsumed: 9,
                expectedConsumedText: "9",
                out var pinnedDenial));
        Assert.Equal("pinned_matrix_already_recorded", pinnedDenial);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.DiscoveryPhase,
                OpenRouterLiveQualification.DiscoveryPhase,
                currentConsumed: 9,
                expectedConsumedText: "9",
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
    public void Matching_fresh_counters_authorize_the_requested_phase_only()
    {
        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.DiscoveryPhase,
                OpenRouterLiveQualification.DiscoveryPhase,
                currentConsumed: 5,
                expectedConsumedText: "5",
                out var discovery));
        Assert.Equal(string.Empty, discovery);

        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.PinnedMatrixPhase,
                OpenRouterLiveQualification.PinnedMatrixPhase,
                currentConsumed: 6,
                expectedConsumedText: "6",
                out var pinned));
        Assert.Equal(string.Empty, pinned);
    }
}