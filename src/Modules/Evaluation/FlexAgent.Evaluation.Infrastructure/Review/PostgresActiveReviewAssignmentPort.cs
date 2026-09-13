using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Application.Review;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure.Review;

public sealed class PostgresActiveReviewAssignmentPort(PostgresConnectionAccessor connections)
    : IActiveReviewAssignmentPort
{
    public async Task<bool> HasActiveAssignmentAsync(
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        CancellationToken cancellationToken)
    {
        if (actor.OrganizationId == Guid.Empty
            || actor.ActorId == Guid.Empty
            || reviewCaseId == Guid.Empty)
        {
            return false;
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM review_case_assignments AS assignment
                    INNER JOIN review_cases AS review_case
                      ON review_case.organization_id = assignment.organization_id
                     AND review_case.review_case_id = assignment.review_case_id
                    WHERE assignment.organization_id = @OrganizationId
                      AND assignment.review_case_id = @ReviewCaseId
                      AND assignment.reviewer_actor_id = @ActorId
                      AND assignment.assignment_state = 'assigned'
                      AND assignment.revoked_at IS NULL);
                """,
                new
                {
                    OrganizationId = actor.OrganizationId,
                    ReviewCaseId = reviewCaseId,
                    ActorId = actor.ActorId,
                },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> HasActiveContentCapabilityAsync(
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        CancellationToken cancellationToken)
    {
        if (!await HasActiveAssignmentAsync(actor, reviewCaseId, cancellationToken))
        {
            return false;
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM review_case_assignments
                    WHERE organization_id = @OrganizationId
                      AND review_case_id = @ReviewCaseId
                      AND reviewer_actor_id = @ActorId
                      AND assignment_state = 'assigned'
                      AND revoked_at IS NULL
                      AND content_capability = @ContentCapability);
                """,
                new
                {
                    OrganizationId = actor.OrganizationId,
                    ReviewCaseId = reviewCaseId,
                    ActorId = actor.ActorId,
                    ContentCapability = ReviewAuthorizedActions.ContentRead,
                },
                cancellationToken: cancellationToken));
    }
}
