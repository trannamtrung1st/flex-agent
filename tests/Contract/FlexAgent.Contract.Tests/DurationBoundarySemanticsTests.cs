namespace FlexAgent.Contract.Tests;

public sealed class DurationBoundarySemanticsTests
{
    [Theory]
    [InlineData("PT1S")]
    [InlineData("PT5M")]
    [InlineData("PT1H")]
    [InlineData("PT23H59M59S")]
    [InlineData("PT24H")]
    public void Documented_timer_durations_are_within_policy_bounds(string duration)
    {
        Assert.True(Harness.Iso8601DurationSemantics.IsWithinTimerPolicyBounds(duration), duration);
    }

    [Theory]
    [InlineData("PT0S")]
    [InlineData("PT24H1S")]
    [InlineData("-PT5S")]
    [InlineData("P1D")]
    public void Out_of_policy_durations_fail_semantic_bounds(string duration)
    {
        Assert.False(Harness.Iso8601DurationSemantics.IsWithinTimerPolicyBounds(duration), duration);
    }
}
