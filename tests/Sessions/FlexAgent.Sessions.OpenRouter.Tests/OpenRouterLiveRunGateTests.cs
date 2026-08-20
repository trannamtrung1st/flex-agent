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

    [Fact]
    public void Gemma_darkbloom_phase_authorizes_only_at_recorded_nine_of_twelve()
    {
        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                currentConsumed: 9,
                expectedConsumedText: "9",
                out var authorized));
        Assert.Equal(string.Empty, authorized);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                currentConsumed: 8,
                expectedConsumedText: "8",
                out var stale));
        Assert.Equal("gemma_darkbloom_requires_consumed_9", stale);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                currentConsumed: 10,
                expectedConsumedText: "10",
                out var advanced));
        Assert.Equal("gemma_darkbloom_requires_consumed_9", advanced);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                OpenRouterLiveQualification.PinnedMatrixPhase,
                currentConsumed: 9,
                expectedConsumedText: "9",
                out var wrongPhase));
        Assert.Equal("phase_mismatch", wrongPhase);
    }

    [Fact]
    public void Nemotron_nano_backup_phase_authorizes_only_at_recorded_ten_of_twelve()
    {
        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.NemotronNanoBackupPhase,
                OpenRouterLiveQualification.NemotronNanoBackupPhase,
                currentConsumed: 10,
                expectedConsumedText: "10",
                out var authorized));
        Assert.Equal(string.Empty, authorized);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.NemotronNanoBackupPhase,
                OpenRouterLiveQualification.NemotronNanoBackupPhase,
                currentConsumed: 9,
                expectedConsumedText: "9",
                out var early));
        Assert.Equal("nemotron_nano_backup_requires_consumed_10", early);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.NemotronNanoBackupPhase,
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                currentConsumed: 10,
                expectedConsumedText: "10",
                out var wrongPhase));
        Assert.Equal("phase_mismatch", wrongPhase);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.NemotronNanoBackupPhase,
                OpenRouterLiveQualification.NemotronNanoBackupPhase,
                currentConsumed: 11,
                expectedConsumedText: "11",
                out var afterRecorded));
        Assert.Equal("nemotron_nano_backup_requires_consumed_10", afterRecorded);
    }

    [Fact]
    public void Retired_lightning_phase_stays_closed_when_gemma_is_admissible()
    {
        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.PinnedMatrixPhase,
                OpenRouterLiveQualification.PinnedMatrixPhase,
                currentConsumed: 9,
                expectedConsumedText: "9",
                out var lightning));
        Assert.Equal("pinned_matrix_already_recorded", lightning);
    }
}