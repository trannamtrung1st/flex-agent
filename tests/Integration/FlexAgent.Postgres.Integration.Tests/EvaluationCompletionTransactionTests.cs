using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationCompletionTransactionTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Completion_coordinator_commits_evaluation_review_handoff_and_work_state()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        var bundle = await EvaluationCompletionTestSupport.BuildBundleAsync(
            Fixture,
            prepared,
            claimed!,
            CancellationToken);
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);

        var result = await coordinator.TryCompleteAsync(bundle.Command, CancellationToken);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(EvaluationCompletionOutcomeCodes.Completed, result.OutcomeCode);
        Assert.NotNull(result.EvaluationId);
        Assert.NotNull(result.ReviewHandoffId);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluations
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;
            """,
            new { claimed!.Ownership.OrganizationId, claimed.RequestId }));
        var initialReviewCase = await connection.QuerySingleAsync<(string CaseState, string CandidateState)>(
            """
            SELECT case_state, candidate_state
            FROM review_cases
            WHERE organization_id = @OrganizationId AND session_id = @SessionId;
            """,
            new { claimed.Ownership.OrganizationId, claimed.Ownership.SessionId });
        Assert.Equal(("evaluation_available", "selected"), initialReviewCase);
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM review_case_events
            WHERE organization_id = @OrganizationId
              AND event_kind = 'initial_candidate_selected';
            """,
            new { claimed.Ownership.OrganizationId }));
        Assert.Equal("completed", await connection.ExecuteScalarAsync<string>(
            """
            SELECT state FROM evaluation_durable_work
            WHERE organization_id = @OrganizationId AND work_id = @WorkId;
            """,
            new { claimed.Ownership.OrganizationId, claimed.WorkId }));
        await EvaluationProhibitedSideEffectAssertions.AssertAbsentAsync(connection);
    }

    [Fact]
    public async Task Equivalent_completion_retry_reconciles_without_duplicate_evaluation()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        var bundle = await EvaluationCompletionTestSupport.BuildBundleAsync(
            Fixture,
            prepared,
            claimed!,
            CancellationToken);
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);

        var first = await coordinator.TryCompleteAsync(bundle.Command, CancellationToken);
        var second = await coordinator.TryCompleteAsync(bundle.Command, CancellationToken);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(EvaluationCompletionOutcomeCodes.Completed, first.OutcomeCode);
        Assert.Equal(EvaluationCompletionOutcomeCodes.Reconciled, second.OutcomeCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluations
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;
            """,
            new { claimed!.Ownership.OrganizationId, claimed.RequestId }));
    }

    [Fact]
    public async Task Audit_failure_rolls_back_completion_writes()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        var bundle = await EvaluationCompletionTestSupport.BuildBundleAsync(
            Fixture,
            prepared,
            claimed!,
            CancellationToken);
        var coordinator = new PostgresEvaluationCompletionCoordinator(
            Fixture.Services.ConnectionAccessor,
            new ThrowingAuditEventWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.TryCompleteAsync(bundle.Command, CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluations
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;
            """,
            new { claimed!.Ownership.OrganizationId, claimed.RequestId }));
    }

    [Fact]
    public async Task Replacement_completion_records_lineage_and_marks_review_candidate_stale()
    {
        var key = Guid.CreateVersion7().ToString("N");
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(Fixture, key, CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var initialClaimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(initialClaimed);
        var initialBundle = await EvaluationCompletionTestSupport.BuildBundleAsync(
            Fixture,
            prepared,
            initialClaimed!,
            CancellationToken);
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);
        var initialCompleted = await coordinator.TryCompleteAsync(initialBundle.Command, CancellationToken);
        Assert.True(initialCompleted.Succeeded, initialCompleted.OutcomeCode);
        Assert.NotNull(initialCompleted.EvaluationId);

        var replacementPrepared = EvaluationPersistenceTestSeed.WithReplacementRequest(
            prepared,
            initialCompleted.EvaluationId!.Value,
            key);
        Assert.True((await replacementPrepared.Admission.AdmitAsync(
            replacementPrepared.Command(),
            CancellationToken)).Succeeded);
        var replacementClaimed = await replacementPrepared.Work.TryClaimAsync(
            replacementPrepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(replacementClaimed);
        var replacementBundle = await EvaluationCompletionTestSupport.BuildBundleAsync(
            Fixture,
            replacementPrepared,
            replacementClaimed!,
            CancellationToken,
            EvaluationRequestKinds.Replacement,
            initialCompleted.EvaluationId,
            "authorized.replacement");

        var replacementCompleted = await coordinator.TryCompleteAsync(
            replacementBundle.Command,
            CancellationToken);
        Assert.True(replacementCompleted.Succeeded, replacementCompleted.OutcomeCode);
        Assert.Equal(EvaluationCompletionOutcomeCodes.Completed, replacementCompleted.OutcomeCode);
        Assert.NotEqual(initialCompleted.EvaluationId, replacementCompleted.EvaluationId);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_lineage
            WHERE organization_id = @OrganizationId
              AND predecessor_evaluation_id = @PredecessorEvaluationId
              AND successor_evaluation_id = @SuccessorEvaluationId;
            """,
            new
            {
                initialClaimed!.Ownership.OrganizationId,
                PredecessorEvaluationId = initialCompleted.EvaluationId,
                SuccessorEvaluationId = replacementCompleted.EvaluationId,
            }));
        var reviewCase = await connection.QuerySingleAsync<(string CaseState, string CandidateState)>(
            """
            SELECT case_state, candidate_state
            FROM review_cases
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            new
            {
                initialClaimed.Ownership.OrganizationId,
                initialClaimed.Ownership.SessionId,
            });
        Assert.Equal(("candidate_stale", "replacement_available"), reviewCase);
        Assert.Equal(initialCompleted.EvaluationId, await connection.ExecuteScalarAsync<Guid?>(
            """
            SELECT current_candidate_evaluation_id
            FROM review_cases
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            new
            {
                initialClaimed.Ownership.OrganizationId,
                initialClaimed.Ownership.SessionId,
            }));
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM review_case_events
            WHERE organization_id = @OrganizationId
              AND event_kind = 'replacement_available'
              AND evaluation_id = @SuccessorEvaluationId;
            """,
            new
            {
                initialClaimed.Ownership.OrganizationId,
                SuccessorEvaluationId = replacementCompleted.EvaluationId,
            }));
        Assert.Equal("complete", await connection.ExecuteScalarAsync<string>(
            """
            SELECT aggregate_status
            FROM evaluations
            WHERE organization_id = @OrganizationId AND evaluation_id = @EvaluationId;
            """,
            new
            {
                initialClaimed.Ownership.OrganizationId,
                initialCompleted.EvaluationId,
            }));
        await EvaluationProhibitedSideEffectAssertions.AssertAbsentAsync(connection);
    }

    [Fact]
    public async Task Concurrent_completion_attempts_reconcile_to_one_evaluation()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        var bundle = await EvaluationCompletionTestSupport.BuildBundleAsync(
            Fixture,
            prepared,
            claimed!,
            CancellationToken);
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);

        var results = await Task.WhenAll(
            coordinator.TryCompleteAsync(bundle.Command, CancellationToken),
            coordinator.TryCompleteAsync(bundle.Command, CancellationToken));

        Assert.All(results, result => Assert.True(result.Succeeded, result.OutcomeCode));
        Assert.Equal(1, results.Count(result =>
            result.OutcomeCode == EvaluationCompletionOutcomeCodes.Completed));
        Assert.Equal(1, results.Count(result =>
            result.OutcomeCode == EvaluationCompletionOutcomeCodes.Reconciled));
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluations
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;
            """,
            new { claimed!.Ownership.OrganizationId, claimed.RequestId }));
    }

    [Fact]
    public async Task Empty_judgments_are_rejected_before_persistence()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        var bundle = await EvaluationCompletionTestSupport.BuildBundleAsync(
            Fixture,
            prepared,
            claimed!,
            CancellationToken);
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);
        var invalid = bundle.Command with { Judgments = [] };

        var result = await coordinator.TryCompleteAsync(invalid, CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
    }

    [Fact]
    public async Task Conflicting_evaluation_identity_returns_integrity_conflict()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
                connection,
                claimed!,
                CancellationToken);
        }

        var bundle = await EvaluationCompletionTestSupport.BuildBundleAsync(
            Fixture,
            prepared,
            claimed!,
            CancellationToken);
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);
        var result = await coordinator.TryCompleteAsync(bundle.Command, CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationCompletionOutcomeCodes.IntegrityConflict, result.OutcomeCode);
    }

    [Fact]
    public async Task Annotation_service_appends_disposition_without_mutating_evaluation_row()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        var bundle = await EvaluationCompletionTestSupport.BuildBundleAsync(
            Fixture,
            prepared,
            claimed!,
            CancellationToken);
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);
        var completed = await coordinator.TryCompleteAsync(bundle.Command, CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        var annotationService = new PostgresEvaluationAnnotationService(Fixture.Services.ConnectionAccessor);
        var annotation = await annotationService.TryAppendAsync(
            new EvaluationAnnotationAppendCommand(
                completed.EvaluationId!.Value,
                claimed!.Ownership.OrganizationId,
                prepared.WorkerActorId,
                EvaluationActorTypes.Service,
                EvaluationAnnotationKinds.SourceLawfullyUnavailable,
                EvaluationDispositions.LawfullyUnavailable,
                "source.lawfully.unavailable",
                DateTimeOffset.UtcNow),
            CancellationToken);
        Assert.True(annotation.Succeeded, annotation.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        Assert.Equal("complete", await connection.ExecuteScalarAsync<string>(
            """
            SELECT aggregate_status
            FROM evaluations
            WHERE organization_id = @OrganizationId AND evaluation_id = @EvaluationId;
            """,
            new { claimed.Ownership.OrganizationId, completed.EvaluationId }));
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_dispositions
            WHERE organization_id = @OrganizationId
              AND evaluation_id = @EvaluationId
              AND current_disposition = 'lawfully_unavailable';
            """,
            new { claimed.Ownership.OrganizationId, completed.EvaluationId }));
        await EvaluationProhibitedSideEffectAssertions.AssertAbsentAsync(connection);
    }

    private sealed class ThrowingAuditEventWriter : IAuditEventWriter
    {
        public Task InsertAsync(
            AuditEventWriteModel model,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("audit rejected");
    }
}
