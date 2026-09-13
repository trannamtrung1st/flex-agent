using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Application.Review;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class AssignedReviewAdmissionTests
{
    [Fact]
    public void Participant_relationship_cannot_list_work_even_with_review_grants()
    {
        var actor = new AssignedReviewActorContext(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            string.Empty,
            [ReviewAuthorizedActions.ListWork]);

        Assert.False(AssignedReviewAdmission.CanListWork(actor));
    }

    [Fact]
    public void Reviewer_without_assignment_grant_cannot_read_case()
    {
        var actor = new AssignedReviewActorContext(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "reviewer",
            [ReviewAuthorizedActions.ListWork]);

        Assert.False(AssignedReviewAdmission.CanReadCase(actor));
    }

    [Fact]
    public void Reviewer_with_evidence_grant_can_open_evidence()
    {
        var actor = new AssignedReviewActorContext(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "reviewer",
            [ReviewAuthorizedActions.OpenEvidence]);

        Assert.True(AssignedReviewAdmission.CanOpenEvidence(actor));
    }
}
