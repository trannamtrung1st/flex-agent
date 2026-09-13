namespace FlexAgent.Evaluation.Application.Review;

public static class AssignedReviewAdmission
{
    public const int MaxWorkPageSize = 25;
    public const int MaxCriterionSummaries = 64;

    public static bool HasGrant(IReadOnlyList<string> permittedActions, string requiredAction) =>
        permittedActions.Contains(requiredAction, StringComparer.Ordinal);

    public static bool CanListWork(AssignedReviewActorContext actor) =>
        HasGrant(actor.PermittedActions, ReviewAuthorizedActions.ListWork)
        && IsReviewerRelationship(actor.Relationship);

    public static bool CanReadCase(AssignedReviewActorContext actor) =>
        HasGrant(actor.PermittedActions, ReviewAuthorizedActions.ReadCase)
        && IsReviewerRelationship(actor.Relationship);

    public static bool CanReadCriterion(AssignedReviewActorContext actor) =>
        HasGrant(actor.PermittedActions, ReviewAuthorizedActions.ReadCriterion)
        && IsReviewerRelationship(actor.Relationship);

    public static bool CanOpenEvidence(AssignedReviewActorContext actor) =>
        HasGrant(actor.PermittedActions, ReviewAuthorizedActions.OpenEvidence)
        && IsReviewerRelationship(actor.Relationship);

    public static int NormalizeWorkLimit(int? requested) =>
        requested is null or < 1 ? MaxWorkPageSize : Math.Min(requested.Value, MaxWorkPageSize);

    private const string ReviewerRelationship = "reviewer";

    private static bool IsReviewerRelationship(string relationship) =>
        string.Equals(relationship, ReviewerRelationship, StringComparison.Ordinal);
}
