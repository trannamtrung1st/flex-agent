using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Application.Review;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Evaluation.Infrastructure.Review;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests;

[Collection(nameof(PostgresCollection))]
public sealed class AssignedReviewCorrectiveTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Criterion_detail_returns_only_judgment_bound_evidence()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            TestContext.Current.CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), TestContext.Current.CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            TestContext.Current.CancellationToken);
        Assert.NotNull(claimed);

        var organizationId = prepared.Request.FrozenInput.Ownership.OrganizationId;
        var requestId = prepared.Request.RequestId;
        var ownership = prepared.Request.FrozenInput.Ownership;
        var reviewerId = Guid.CreateVersion7();
        var reviewCaseId = Guid.CreateVersion7();
        var judgmentAId = Guid.CreateVersion7();
        var judgmentBId = Guid.CreateVersion7();
        var evidenceAId = Guid.CreateVersion7();
        var evidenceBId = Guid.CreateVersion7();

        Guid evaluationId;
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(TestContext.Current.CancellationToken))
        {
            evaluationId = await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
                connection,
                claimed!,
                TestContext.Current.CancellationToken);

            await connection.ExecuteAsync(
                """
                INSERT INTO review_cases (
                    organization_id, review_case_id, activity_id, participant_id,
                    attempt_id, session_id, case_state, candidate_state,
                    current_candidate_evaluation_id, created_at, updated_at)
                VALUES (
                    @OrganizationId, @ReviewCaseId, @ActivityId, @ParticipantId,
                    @AttemptId, @SessionId, 'evaluation_available', 'none',
                    @EvaluationId, CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP());

                INSERT INTO evaluation_criterion_judgments (
                    organization_id, evaluation_id, judgment_id, request_id, criterion_id, criterion_version,
                    evaluator_mode, status, confidence, uncertainty_json, rationale)
                VALUES
                    (@OrganizationId, @EvaluationId, @JudgmentAId, @RequestId, 'criterion.a', 'v1',
                     'deterministic', 'satisfied', 'high', '[]'::jsonb, 'A rationale'),
                    (@OrganizationId, @EvaluationId, @JudgmentBId, @RequestId, 'criterion.b', 'v1',
                     'deterministic', 'satisfied', 'high', '[]'::jsonb, 'B rationale');

                INSERT INTO evaluation_evidence_items (
                    organization_id, evaluation_id, evidence_id, request_id, activity_id, participant_id,
                    attempt_id, session_id, source_type, source_id, source_version_id, source_content_digest,
                    locator_schema, locator_digest, precision, integrity_state, locator_canonical_json, created_by_service, created_at)
                VALUES
                    (@OrganizationId, @EvaluationId, @EvidenceAId, @RequestId, @ActivityId, @ParticipantId,
                     @AttemptId, @SessionId, 'configuration.fact', @SourceId, @SourceVersionId, @Digest,
                     'evidence-locator.v1', @LocatorDigest, 'whole_item', 'verified', @CanonicalJson::jsonb,
                     'evaluation.integration', CLOCK_TIMESTAMP()),
                    (@OrganizationId, @EvaluationId, @EvidenceBId, @RequestId, @ActivityId, @ParticipantId,
                     @AttemptId, @SessionId, 'configuration.fact', @SourceId, @SourceVersionId, @Digest,
                     'evidence-locator.v1', @LocatorDigest, 'whole_item', 'verified', @CanonicalJson::jsonb,
                     'evaluation.integration', CLOCK_TIMESTAMP());

                INSERT INTO evaluation_criterion_judgment_evidence_refs (
                    organization_id, evaluation_id, judgment_id, evidence_id, reference_ordinal)
                VALUES
                    (@OrganizationId, @EvaluationId, @JudgmentAId, @EvidenceAId, 0),
                    (@OrganizationId, @EvaluationId, @JudgmentBId, @EvidenceBId, 0);

                INSERT INTO review_case_assignments (
                    organization_id, assignment_id, review_case_id, reviewer_actor_id,
                    assignment_state, content_capability, assigned_at, revoked_at)
                VALUES (
                    @OrganizationId, @AssignmentId, @ReviewCaseId, @ReviewerId,
                    'assigned', @ContentCapability, CLOCK_TIMESTAMP(), NULL);
                """,
                new
                {
                    OrganizationId = organizationId,
                    ReviewCaseId = reviewCaseId,
                    ActivityId = ownership.ActivityId,
                    ParticipantId = ownership.ParticipantId,
                    AttemptId = ownership.AttemptId,
                    SessionId = ownership.SessionId,
                    EvaluationId = evaluationId,
                    RequestId = requestId,
                    JudgmentAId = judgmentAId,
                    JudgmentBId = judgmentBId,
                    EvidenceAId = evidenceAId,
                    EvidenceBId = evidenceBId,
                    SourceId = Guid.CreateVersion7(),
                    SourceVersionId = Guid.CreateVersion7(),
                    Digest = new string('c', 64),
                    LocatorDigest = new string('d', 64),
                    CanonicalJson = """{"locator_schema":"evidence-locator.v1"}""",
                    AssignmentId = Guid.CreateVersion7(),
                    ReviewerId = reviewerId,
                    ContentCapability = ReviewAuthorizedActions.ContentRead,
                });
        }

        var service = CreateQueryService();
        var actor = new AssignedReviewActorContext(
            organizationId,
            reviewerId,
            "reviewer",
            [ReviewAuthorizedActions.ReadCriterion, ReviewAuthorizedActions.ContentRead]);

        var criterionA = await service.GetCriterionAsync(
            actor,
            reviewCaseId,
            "criterion.a",
            TestContext.Current.CancellationToken);
        var criterionB = await service.GetCriterionAsync(
            actor,
            reviewCaseId,
            "criterion.b",
            TestContext.Current.CancellationToken);

        Assert.True(criterionA.Succeeded);
        Assert.True(criterionB.Succeeded);
        Assert.Single(criterionA.Value!.EvidenceReferences);
        Assert.Single(criterionB.Value!.EvidenceReferences);
        Assert.Equal(EvaluationEvidenceSourceIdentity.StableEvidenceId(evidenceAId), criterionA.Value.EvidenceReferences[0].EvidenceId);
        Assert.Equal(EvaluationEvidenceSourceIdentity.StableEvidenceId(evidenceBId), criterionB.Value.EvidenceReferences[0].EvidenceId);
    }

    [Fact]
    public async Task Evidence_open_without_canonical_locator_is_unavailable()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            TestContext.Current.CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), TestContext.Current.CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            TestContext.Current.CancellationToken);
        Assert.NotNull(claimed);

        var organizationId = prepared.Request.FrozenInput.Ownership.OrganizationId;
        var requestId = prepared.Request.RequestId;
        var ownership = prepared.Request.FrozenInput.Ownership;
        var reviewerId = Guid.CreateVersion7();
        var reviewCaseId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();

        Guid evaluationId;
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(TestContext.Current.CancellationToken))
        {
            evaluationId = await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
                connection,
                claimed!,
                TestContext.Current.CancellationToken);

            await connection.ExecuteAsync(
                """
                INSERT INTO review_cases (
                    organization_id, review_case_id, activity_id, participant_id,
                    attempt_id, session_id, case_state, candidate_state,
                    current_candidate_evaluation_id, created_at, updated_at)
                VALUES (
                    @OrganizationId, @ReviewCaseId, @ActivityId, @ParticipantId,
                    @AttemptId, @SessionId, 'evaluation_available', 'none',
                    @EvaluationId, CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP());

                INSERT INTO evaluation_evidence_items (
                    organization_id, evaluation_id, evidence_id, request_id, activity_id, participant_id,
                    attempt_id, session_id, source_type, source_id, source_version_id, source_content_digest,
                    locator_schema, locator_digest, precision, integrity_state, created_by_service, created_at)
                VALUES (
                    @OrganizationId, @EvaluationId, @EvidenceId, @RequestId, @ActivityId, @ParticipantId,
                    @AttemptId, @SessionId, 'configuration.fact', @SourceId, @SourceVersionId, @Digest,
                    'evidence-locator.v1', @LocatorDigest, 'whole_item', 'verified',
                    'evaluation.integration', CLOCK_TIMESTAMP());

                INSERT INTO review_case_assignments (
                    organization_id, assignment_id, review_case_id, reviewer_actor_id,
                    assignment_state, content_capability, assigned_at, revoked_at)
                VALUES (
                    @OrganizationId, @AssignmentId, @ReviewCaseId, @ReviewerId,
                    'assigned', @ContentCapability, CLOCK_TIMESTAMP(), NULL);
                """,
                new
                {
                    OrganizationId = organizationId,
                    ReviewCaseId = reviewCaseId,
                    ActivityId = ownership.ActivityId,
                    ParticipantId = ownership.ParticipantId,
                    AttemptId = ownership.AttemptId,
                    SessionId = ownership.SessionId,
                    EvaluationId = evaluationId,
                    RequestId = requestId,
                    EvidenceId = evidenceId,
                    SourceId = Guid.CreateVersion7(),
                    SourceVersionId = Guid.CreateVersion7(),
                    Digest = new string('c', 64),
                    LocatorDigest = new string('d', 64),
                    AssignmentId = Guid.CreateVersion7(),
                    ReviewerId = reviewerId,
                    ContentCapability = ReviewAuthorizedActions.ContentRead,
                });
        }

        var service = CreateQueryService();
        var actor = new AssignedReviewActorContext(
            organizationId,
            reviewerId,
            "reviewer",
            [ReviewAuthorizedActions.OpenEvidence, ReviewAuthorizedActions.ContentRead]);

        var result = await service.OpenEvidenceAsync(
            actor,
            reviewCaseId,
            EvaluationEvidenceSourceIdentity.StableEvidenceId(evidenceId),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewFailureCodes.Unavailable, result.OutcomeCode);
    }

    private PostgresAssignedReviewQueryService CreateQueryService()
    {
        var connections = Fixture.Services.ConnectionAccessor;
        var assignments = new PostgresActiveReviewAssignmentPort(connections);
        var resolver = new PostgresAssignedReviewProtectedEvidenceResolver(
            connections,
            new PostgresEvaluationSessionEvidenceSource(connections, new PostgresEvaluationHandoffSource(connections)),
            new PostgresEvaluationSubmissionEvidenceSource(connections, new InMemoryArtifactStore()),
            new PostgresProtectedDeterministicOutputStore(connections));
        return new PostgresAssignedReviewQueryService(connections, assignments, resolver);
    }
}
