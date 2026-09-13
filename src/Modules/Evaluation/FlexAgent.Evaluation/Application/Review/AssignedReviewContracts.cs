using FlexAgent.Contracts.Review;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application.Review;

public static class ReviewFailureCodes
{
    public const string Denied = "review.denied";
    public const string InvalidField = "review.invalid_field";
    public const string Unavailable = "review.unavailable";
}

public sealed record AssignedReviewActorContext(
    Guid OrganizationId,
    Guid ActorId,
    string Relationship,
    IReadOnlyList<string> PermittedActions);

public sealed record AssignedReviewWorkListRequest(
    string? Cursor,
    int Limit);

public sealed record AssignedReviewWorkListPage(
    IReadOnlyList<ReviewWorkItemV1> Items,
    string? NextCursor,
    bool HasMore);

public interface IActiveReviewAssignmentPort
{
    Task<bool> HasActiveAssignmentAsync(
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveContentCapabilityAsync(
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        CancellationToken cancellationToken);
}

public interface IAssignedReviewQueryService
{
    Task<EvaluationDecision<AssignedReviewWorkListPage>> ListWorkAsync(
        AssignedReviewActorContext actor,
        AssignedReviewWorkListRequest request,
        CancellationToken cancellationToken);

    Task<EvaluationDecision<ReviewCaseReadV1>> GetCaseAsync(
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        CancellationToken cancellationToken);

    Task<EvaluationDecision<ReviewCriterionReadV1>> GetCriterionAsync(
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        string criterionId,
        CancellationToken cancellationToken);

    Task<EvaluationDecision<ReviewEvidenceOpenV1>> OpenEvidenceAsync(
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        string evidenceId,
        CancellationToken cancellationToken);
}
