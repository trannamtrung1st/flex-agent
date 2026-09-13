using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Application.Review;
using FlexAgent.Evaluation.Infrastructure.Review;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

[Collection(nameof(PostgresCollection))]
public sealed class AssignedReviewAuthorizationTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Unassigned_reviewer_is_denied_for_assigned_case_read()
    {
        var organizationId = Guid.CreateVersion7();
        var reviewerId = Guid.CreateVersion7();
        var reviewCaseId = Guid.CreateVersion7();
        await SeedReviewCaseAsync(organizationId, reviewCaseId);
        var service = new PostgresAssignedReviewQueryService(
            Fixture.Services.ConnectionAccessor,
            new PostgresActiveReviewAssignmentPort(Fixture.Services.ConnectionAccessor));
        var actor = new AssignedReviewActorContext(
            organizationId,
            reviewerId,
            "reviewer",
            [
                ReviewAuthorizedActions.ListWork,
                ReviewAuthorizedActions.ReadCase,
                ReviewAuthorizedActions.ReadCriterion,
                ReviewAuthorizedActions.OpenEvidence,
            ]);

        var result = await service.GetCaseAsync(actor, reviewCaseId, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewFailureCodes.Denied, result.OutcomeCode);
    }

    [Fact]
    public async Task Assigned_reviewer_can_list_only_active_assignments()
    {
        var organizationId = Guid.CreateVersion7();
        var reviewerId = Guid.CreateVersion7();
        var assignedCaseId = Guid.CreateVersion7();
        var unassignedCaseId = Guid.CreateVersion7();
        await SeedReviewCaseAsync(organizationId, assignedCaseId);
        await SeedReviewCaseAsync(organizationId, unassignedCaseId);
        await SeedAssignmentAsync(organizationId, assignedCaseId, reviewerId);
        var service = new PostgresAssignedReviewQueryService(
            Fixture.Services.ConnectionAccessor,
            new PostgresActiveReviewAssignmentPort(Fixture.Services.ConnectionAccessor));
        var actor = new AssignedReviewActorContext(
            organizationId,
            reviewerId,
            "reviewer",
            [ReviewAuthorizedActions.ListWork]);

        var result = await service.ListWorkAsync(
            actor,
            new AssignedReviewWorkListRequest(null, 25),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.Items);
        Assert.Equal(assignedCaseId, result.Value.Items[0].ReviewCaseId);
    }

    [Fact]
    public async Task Revoked_assignment_denies_case_read()
    {
        var organizationId = Guid.CreateVersion7();
        var reviewerId = Guid.CreateVersion7();
        var reviewCaseId = Guid.CreateVersion7();
        await SeedReviewCaseAsync(organizationId, reviewCaseId);
        await SeedAssignmentAsync(organizationId, reviewCaseId, reviewerId, revoked: true);
        var service = new PostgresAssignedReviewQueryService(
            Fixture.Services.ConnectionAccessor,
            new PostgresActiveReviewAssignmentPort(Fixture.Services.ConnectionAccessor));
        var actor = new AssignedReviewActorContext(
            organizationId,
            reviewerId,
            "reviewer",
            [ReviewAuthorizedActions.ReadCase]);

        var result = await service.GetCaseAsync(actor, reviewCaseId, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewFailureCodes.Denied, result.OutcomeCode);
    }

    private async Task SeedReviewCaseAsync(Guid organizationId, Guid reviewCaseId)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at)
            VALUES (@OrganizationId, CLOCK_TIMESTAMP())
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO review_cases (
                organization_id, review_case_id, activity_id, participant_id,
                attempt_id, session_id, case_state, candidate_state,
                current_candidate_evaluation_id, created_at, updated_at)
            VALUES (
                @OrganizationId, @ReviewCaseId, @ActivityId, @ParticipantId,
                @AttemptId, @SessionId, 'evaluation_available', 'none',
                NULL, CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP());
            """,
            new
            {
                OrganizationId = organizationId,
                ReviewCaseId = reviewCaseId,
                ActivityId = Guid.CreateVersion7(),
                ParticipantId = Guid.CreateVersion7(),
                AttemptId = Guid.CreateVersion7(),
                SessionId = Guid.CreateVersion7(),
            });
    }

    private async Task SeedAssignmentAsync(
        Guid organizationId,
        Guid reviewCaseId,
        Guid reviewerId,
        bool revoked = false)
    {
        var assignmentId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO review_case_assignments (
                organization_id, assignment_id, review_case_id, reviewer_actor_id,
                assignment_state, content_capability, assigned_at, revoked_at)
            VALUES (
                @OrganizationId, @AssignmentId, @ReviewCaseId, @ReviewerId,
                @AssignmentState, @ContentCapability, @AssignedAt, @RevokedAt);
            """,
            new
            {
                OrganizationId = organizationId,
                AssignmentId = assignmentId,
                ReviewCaseId = reviewCaseId,
                ReviewerId = reviewerId,
                AssignmentState = revoked ? "revoked" : "assigned",
                ContentCapability = ReviewAuthorizedActions.ContentRead,
                AssignedAt = now,
                RevokedAt = revoked ? now : (DateTimeOffset?)null,
            });
    }
}
