using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class FrozenRuntimePolicySnapshotTests
{
    [Fact]
    public void Canonical_snapshot_round_trips_enabled_timer_policy()
    {
        var policy = RuntimePolicyTestFixtures.ResolveEnabledTimerPolicy();

        var json = FrozenRuntimePolicySnapshot.ToCanonicalJson(policy);
        var restored = FrozenRuntimePolicySnapshot.TryParse(json, policy.PolicyDigest);

        Assert.NotNull(restored);
        Assert.Equal(policy.PolicyDigest, restored!.PolicyDigest);
        Assert.Equal(policy.InvocationContractVersion, restored.InvocationContractVersion);
        Assert.True(restored.TimerLane!.IsEnabled);
        Assert.Equal(policy.TimerLane!.DefaultDelay.WireValue, restored.TimerLane.DefaultDelay.WireValue);
        Assert.Equal(
            policy.StreamingPublicationBounds.MaxFragmentUtf8Bytes,
            restored.StreamingPublicationBounds.MaxFragmentUtf8Bytes);
    }

    [Fact]
    public void Snapshot_parse_rejects_digest_mismatch()
    {
        var policy = RuntimePolicyTestFixtures.ResolveEnabledTimerPolicy();
        var json = FrozenRuntimePolicySnapshot.ToCanonicalJson(policy);

        Assert.Null(FrozenRuntimePolicySnapshot.TryParse(json, new string('a', 64)));
        Assert.Null(FrozenRuntimePolicySnapshot.TryParse("{not-json", policy.PolicyDigest));
    }
}
