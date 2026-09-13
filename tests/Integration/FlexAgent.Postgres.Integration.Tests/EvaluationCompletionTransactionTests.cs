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
    public async Task Forged_procedure_identity_rejects_without_publication()
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
        var forged = bundle.Command.Completed with
        {
            ProcedureRef = bundle.Command.Completed.ProcedureRef with
            {
                SourceId = Guid.CreateVersion7(),
            },
        };
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);
        var result = await coordinator.TryCompleteAsync(
            bundle.Command with { Completed = forged },
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationCompletionOutcomeCodes.IntegrityConflict, result.OutcomeCode);
        await AssertNoCompletionPublicationAsync(claimed!);
    }

    [Fact]
    public async Task Forged_aggregate_status_rejects_without_publication()
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
        var forgedJudgments = bundle.Command.Judgments
            .Select(judgment => judgment with { Status = CriterionStatuses.InsufficientEvidence })
            .ToArray();
        var forged = bundle.Command.Completed with
        {
            AggregateStatus = EvaluationAggregateStatuses.Complete,
        };
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);
        var result = await coordinator.TryCompleteAsync(
            bundle.Command with
            {
                Completed = forged,
                Judgments = forgedJudgments,
            },
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationCompletionOutcomeCodes.IntegrityConflict, result.OutcomeCode);
        await AssertNoCompletionPublicationAsync(claimed!);
    }

    [Fact]
    public async Task Forged_evidence_set_digest_rejects_without_publication()
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
        var forged = bundle.Command.Completed with
        {
            EvidenceSetDigest = new string('e', 64),
        };
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);
        var result = await coordinator.TryCompleteAsync(
            bundle.Command with { Completed = forged },
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationCompletionOutcomeCodes.IntegrityConflict, result.OutcomeCode);
        await AssertNoCompletionPublicationAsync(claimed!);
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

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var annotateDelegationId = await EvaluationMutationDelegationSupport.EnsureAnnotateDelegationAsync(
            connection,
            claimed!.Ownership,
            prepared.WorkerActorId,
            CancellationToken);
        var annotationService = new PostgresEvaluationAnnotationService(Fixture.Services.ConnectionAccessor);
        var annotation = await annotationService.TryAppendAsync(
            new EvaluationAnnotationAppendCommand(
                completed.EvaluationId!.Value,
                annotateDelegationId,
                prepared.WorkerActorId,
                Guid.CreateVersion7(),
                "integration.test",
                EvaluationAnnotationKinds.SourceLawfullyUnavailable,
                EvaluationDispositions.LawfullyUnavailable,
                "source.lawfully.unavailable",
                DateTimeOffset.UtcNow),
            CancellationToken);
        Assert.True(annotation.Succeeded, annotation.OutcomeCode);

        await using var verifyConnection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        Assert.Equal("complete", await verifyConnection.ExecuteScalarAsync<string>(
            """
            SELECT aggregate_status
            FROM evaluations
            WHERE organization_id = @OrganizationId AND evaluation_id = @EvaluationId;
            """,
            new { claimed.Ownership.OrganizationId, completed.EvaluationId }));
        Assert.Equal(1, await verifyConnection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_dispositions
            WHERE organization_id = @OrganizationId
              AND evaluation_id = @EvaluationId
              AND current_disposition = 'lawfully_unavailable';
            """,
            new { claimed.Ownership.OrganizationId, completed.EvaluationId }));
        Assert.Equal(1, await verifyConnection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND event_schema_version = 'evaluation.annotation.appended.v1'
              AND resource_id = @AnnotationId;
            """,
            new
            {
                claimed.Ownership.OrganizationId,
                AnnotationId = annotation.Value,
            }));
        await EvaluationProhibitedSideEffectAssertions.AssertAbsentAsync(verifyConnection);
    }

    [Fact]
    public async Task Annotation_without_delegation_is_denied_without_writes()
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
                prepared.DelegationId,
                prepared.WorkerActorId,
                Guid.CreateVersion7(),
                "integration.test",
                EvaluationAnnotationKinds.SourceLawfullyUnavailable,
                EvaluationDispositions.LawfullyUnavailable,
                "source.lawfully.unavailable",
                DateTimeOffset.UtcNow),
            CancellationToken);

        Assert.False(annotation.Succeeded);
        Assert.Equal(EvaluationPersistenceOutcomeCodes.Denied, annotation.OutcomeCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_annotations
            WHERE organization_id = @OrganizationId AND evaluation_id = @EvaluationId;
            """,
            new { claimed!.Ownership.OrganizationId, completed.EvaluationId }));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND event_schema_version = 'evaluation.annotation.appended.v1';
            """,
            new { claimed.Ownership.OrganizationId }));
    }

    [Fact]
    public async Task Unbound_evidence_identity_with_recomputed_seal_rejects_without_publication()
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
        var forgedSource = ExactSourceIdentity.TryCreate(
            "task_submission",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new string('9', 64)).Value!;
        var forgedItems = bundle.Command.EvidenceItems
            .Select(item => EvidenceItem.TryCreate(
                item.EvidenceId,
                item.SourceType,
                forgedSource,
                item.Ownership,
                item.EvaluationId,
                item.Precision).Value!)
            .ToArray();
        var forgedSeal = EvaluationCompletionEvidenceSeal.TryComputeExpectedDigest(
            bundle.Command.Completed.EvidenceSetId,
            bundle.Command.InvocationAttemptId,
            prepared.Request.FrozenInput,
            forgedItems);
        Assert.True(forgedSeal.Succeeded, forgedSeal.OutcomeCode);
        var forgedCompleted = bundle.Command.Completed with
        {
            EvidenceSetDigest = forgedSeal.Value!,
        };
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);
        var result = await coordinator.TryCompleteAsync(
            bundle.Command with
            {
                Completed = forgedCompleted,
                EvidenceItems = forgedItems,
            },
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.True(
            result.OutcomeCode is EvaluationCompletionOutcomeCodes.IntegrityConflict
                or EvaluationFailureCodes.CitationIntegrity,
            result.OutcomeCode);
        await AssertNoCompletionPublicationAsync(claimed!);
    }

    [Fact]
    public async Task Judgment_citing_failed_deterministic_attempt_rejects_without_publication()
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
        var citedJudgment = bundle.Command.Judgments
            .First(judgment => judgment.DeterministicInvocationId is not null);
        var failedAttemptId = Guid.CreateVersion7();
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            var sourceAttempt = await connection.QuerySingleAsync<DeterministicAttemptRow>(
                """
                SELECT criterion_id, criterion_version, evaluator_id, evaluator_version,
                       evaluator_digest, canonical_input_digest, dependency_digest,
                       configuration_digest, started_at, finished_at
                FROM evaluation_deterministic_attempts
                WHERE organization_id = @OrganizationId
                  AND request_id = @RequestId
                  AND deterministic_attempt_id = @DeterministicAttemptId;
                """,
                new
                {
                    claimed!.Ownership.OrganizationId,
                    claimed.RequestId,
                    DeterministicAttemptId = citedJudgment.DeterministicInvocationId,
                });
            await connection.ExecuteAsync(
                """
                INSERT INTO evaluation_deterministic_attempts (
                    organization_id, deterministic_attempt_id, request_id, invocation_attempt_id,
                    criterion_id, criterion_version, evaluator_id, evaluator_version, evaluator_digest,
                    canonical_input_digest, dependency_digest, configuration_digest, outcome,
                    protected_input_ref, protected_output_ref, output_content_digest,
                    started_at, finished_at)
                VALUES (
                    @OrganizationId, @DeterministicAttemptId, @RequestId, @InvocationAttemptId,
                    @CriterionId, @CriterionVersion, @EvaluatorId, @EvaluatorVersion, @EvaluatorDigest,
                    @CanonicalInputDigest, @DependencyDigest, @ConfigurationDigest, 'failed',
                    'protected.input.ref', 'protected.output.ref', @OutputContentDigest,
                    @StartedAt, @FinishedAt);
                """,
                new
                {
                    OrganizationId = claimed.Ownership.OrganizationId,
                    DeterministicAttemptId = failedAttemptId,
                    RequestId = claimed.RequestId,
                    InvocationAttemptId = bundle.Command.InvocationAttemptId,
                    CriterionId = sourceAttempt.criterion_id,
                    CriterionVersion = sourceAttempt.criterion_version,
                    EvaluatorId = sourceAttempt.evaluator_id,
                    EvaluatorVersion = sourceAttempt.evaluator_version,
                    EvaluatorDigest = sourceAttempt.evaluator_digest,
                    CanonicalInputDigest = sourceAttempt.canonical_input_digest,
                    DependencyDigest = sourceAttempt.dependency_digest,
                    ConfigurationDigest = sourceAttempt.configuration_digest,
                    OutputContentDigest = new string('2', 64),
                    StartedAt = sourceAttempt.started_at,
                    FinishedAt = sourceAttempt.finished_at,
                });
        }

        var forgedJudgments = bundle.Command.Judgments
            .Select(judgment => judgment.JudgmentId == citedJudgment.JudgmentId
                ? judgment with { DeterministicInvocationId = failedAttemptId }
                : judgment)
            .ToArray();
        var coordinator = new PostgresEvaluationCompletionCoordinator(Fixture.Services.ConnectionAccessor);
        var result = await coordinator.TryCompleteAsync(
            bundle.Command with { Judgments = forgedJudgments },
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        await AssertNoCompletionPublicationAsync(claimed!);
    }

    [Fact]
    public async Task Conflicting_completion_replay_with_same_evaluation_id_returns_integrity_conflict()
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
        Assert.True(first.Succeeded, first.OutcomeCode);

        var conflicting = bundle.Command.Completed with
        {
            AggregateStatus = EvaluationAggregateStatuses.ConflictReviewRequired,
        };
        var replay = await coordinator.TryCompleteAsync(
            bundle.Command with { Completed = conflicting },
            CancellationToken);

        Assert.False(replay.Succeeded);
        Assert.Equal(EvaluationCompletionOutcomeCodes.IntegrityConflict, replay.OutcomeCode);
    }

    [Fact]
    public async Task Annotation_audit_failure_rolls_back_writes()
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

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var annotateDelegationId = await EvaluationMutationDelegationSupport.EnsureAnnotateDelegationAsync(
            connection,
            claimed!.Ownership,
            prepared.WorkerActorId,
            CancellationToken);
        var annotationService = new PostgresEvaluationAnnotationService(
            Fixture.Services.ConnectionAccessor,
            new ThrowingAuditEventWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            annotationService.TryAppendAsync(
                new EvaluationAnnotationAppendCommand(
                    completed.EvaluationId!.Value,
                    annotateDelegationId,
                    prepared.WorkerActorId,
                    Guid.CreateVersion7(),
                    "integration.test",
                    EvaluationAnnotationKinds.SourceLawfullyUnavailable,
                    EvaluationDispositions.LawfullyUnavailable,
                    "source.lawfully.unavailable",
                    DateTimeOffset.UtcNow),
                CancellationToken));

        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_annotations
            WHERE organization_id = @OrganizationId AND evaluation_id = @EvaluationId;
            """,
            new { claimed.Ownership.OrganizationId, completed.EvaluationId }));
    }

    private async Task AssertNoCompletionPublicationAsync(EvaluationDurableWorkItem claimed)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluations
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;
            """,
            new { claimed.Ownership.OrganizationId, claimed.RequestId }));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_review_handoffs
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;
            """,
            new { claimed.Ownership.OrganizationId, claimed.RequestId }));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND event_schema_version = 'evaluation.completed.v1';
            """,
            new { claimed.Ownership.OrganizationId }));
        await EvaluationProhibitedSideEffectAssertions.AssertAbsentAsync(connection);
    }

    private sealed record DeterministicAttemptRow(
        string criterion_id,
        string criterion_version,
        string evaluator_id,
        string evaluator_version,
        string evaluator_digest,
        string canonical_input_digest,
        string dependency_digest,
        string configuration_digest,
        DateTimeOffset started_at,
        DateTimeOffset finished_at);

    private sealed class ThrowingAuditEventWriter : IAuditEventWriter
    {
        public Task InsertAsync(
            AuditEventWriteModel model,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("audit rejected");
    }
}
