using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class Iso8601PositiveDurationTests
{
    [Theory]
    [InlineData("PT1S")]
    [InlineData("PT5M")]
    [InlineData("PT1H")]
    [InlineData("PT23H59M59S")]
    [InlineData("PT24H")]
    public void Valid_timer_durations_are_accepted(string duration)
    {
        Assert.True(Iso8601PositiveDuration.TryParse(duration, out var parsed));
        Assert.Equal(duration, parsed.WireValue);
    }

    [Theory]
    [InlineData("PT0S")]
    [InlineData("PT24H1S")]
    [InlineData("-PT5S")]
    [InlineData("P1D")]
    [InlineData("")]
    [InlineData("5m")]
    public void Invalid_durations_are_rejected(string duration)
    {
        Assert.False(Iso8601PositiveDuration.TryParse(duration, out _));
    }

    [Theory]
    [InlineData("PT999999999999H")]
    [InlineData("PT2147483647H")]
    public void Overflow_durations_are_rejected_without_throwing(string duration)
    {
        var ex = Record.Exception(() => Iso8601PositiveDuration.TryParse(duration, out _));
        Assert.Null(ex);
        Assert.False(Iso8601PositiveDuration.TryParse(duration, out _));
    }

    [Fact]
    public void Parsed_durations_compare_by_total_seconds()
    {
        Assert.True(Iso8601PositiveDuration.TryParse("PT1M", out var oneMinute));
        Assert.True(Iso8601PositiveDuration.TryParse("PT60S", out var sixtySeconds));
        Assert.Equal(oneMinute.TotalSeconds, sixtySeconds.TotalSeconds);
    }
}
