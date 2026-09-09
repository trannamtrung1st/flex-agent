using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Contract.Tests;

public sealed class EvaluationPositiveDurationTests
{
    [Theory]
    [InlineData("PT5S")]
    [InlineData("PT10S")]
    [InlineData("PT10M")]
    [InlineData("PT1H")]
    [InlineData("PT24H")]
    [InlineData("PT1H30M")]
    [InlineData("PT5M10S")]
    [InlineData("PT1H30S")]
    [InlineData("PT1H30M45S")]
    public void Valid_positive_durations_parse(string value)
    {
        Assert.True(EvaluationPositiveDuration.IsValid(value));
        Assert.True(EvaluationPositiveDuration.TryParseTotalSeconds(value, out var seconds));
        Assert.True(seconds > 0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PT")]
    [InlineData("PT0S")]
    [InlineData("PTgarbage")]
    [InlineData("PT5X")]
    [InlineData("PT5Sextra")]
    [InlineData("P1D")]
    public void Invalid_durations_are_rejected(string? value)
    {
        Assert.False(EvaluationPositiveDuration.IsValid(value));
        Assert.False(EvaluationPositiveDuration.TryParseTotalSeconds(value, out _));
    }
}
