using FlexAgent.Evaluation.Application.Review;

namespace FlexAgent.Evaluation.Tests.Application.Review;

public sealed class AssignedReviewProjectionMapperTests
{
    [Theory]
    [InlineData(null, null, false, "awaiting")]
    [InlineData("queued", null, false, "queued")]
    [InlineData("running", null, false, "running")]
    [InlineData("completing", null, false, "running")]
    [InlineData("failed_retryable", null, false, "retryable_failure")]
    [InlineData("completed", "complete", true, "completed")]
    [InlineData("completed", "conflict_review_required", true, "review_required")]
    public void MapEvaluationProcessingState_uses_request_state_until_evaluation_exists(
        string? requestState,
        string? aggregateStatus,
        bool hasEvaluation,
        string expected)
    {
        var actual = AssignedReviewProjectionMapper.MapEvaluationProcessingState(
            requestState,
            aggregateStatus,
            hasEvaluation);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("completed", "open_review")]
    [InlineData("review_required", "open_review")]
    [InlineData("queued", "none")]
    [InlineData("running", "none")]
    [InlineData("awaiting", "none")]
    [InlineData("retryable_failure", "none")]
    public void MapNextAction_blocks_inspect_until_evaluation_is_ready(
        string processingState,
        string expected)
    {
        Assert.Equal(expected, AssignedReviewProjectionMapper.MapNextAction(processingState));
    }
}
