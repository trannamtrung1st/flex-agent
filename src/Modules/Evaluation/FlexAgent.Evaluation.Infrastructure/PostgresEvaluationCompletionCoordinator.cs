using System.Text.Json;
using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure.Review;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using Npgsql;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresEvaluationCompletionCoordinator(
    PostgresConnectionAccessor connectionAccessor,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null) : IEvaluationCompletionCoordinator
{
    private readonly IAuditEventWriter _auditEventWriter =
        auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter =
        outboxItemWriter ?? new PostgresOutboxItemWriter();

    public async Task<EvaluationCompletionResult> TryCompleteAsync(
        EvaluationCompletionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.WorkId == Guid.Empty
            || command.RequestId == Guid.Empty
            || command.InvocationAttemptId == Guid.Empty
            || command.DelegationId == Guid.Empty
            || command.ActorId == Guid.Empty
            || command.CorrelationId == Guid.Empty
            || command.Completed.EvaluationId == Guid.Empty
            || command.Completed.RequestId != command.RequestId
            || command.EvidenceItems.Count == 0
            || command.Judgments.Count == 0)
        {
            return new(false, EvaluationFailureCodes.InvalidField, null, null, false);
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            cancellationToken);
        try
        {
            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    SELECT pg_advisory_xact_lock(
                        hashtextextended(@OrganizationId::text || ':' || @RequestId::text, 0));
                    """,
                    new
                    {
                        command.Completed.Ownership.OrganizationId,
                        command.RequestId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            var requestRow = await scope.Connection.QuerySingleOrDefaultAsync<RequestRow>(
                new CommandDefinition(
                    """
                    SELECT
                        request.request_id,
                        request.request_kind,
                        request.state,
                        request.predecessor_evaluation_id,
                        request.replacement_reason,
                        request.activity_id,
                        request.participant_id,
                        request.attempt_id,
                        request.session_id,
                        request.rubric_source_id,
                        request.rubric_source_version_id,
                        request.rubric_content_digest,
                        existing.evaluation_id AS existing_evaluation_id,
                        existing.evidence_set_id AS existing_evidence_set_id
                    FROM evaluation_requests AS request
                    LEFT JOIN evaluations AS existing
                      ON existing.organization_id = request.organization_id
                     AND existing.request_id = request.request_id
                    WHERE request.organization_id = @OrganizationId
                      AND request.request_id = @RequestId
                    FOR UPDATE OF request;
                    """,
                    new
                    {
                        command.Completed.Ownership.OrganizationId,
                        command.RequestId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            if (requestRow is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            if (!await IsAuthorizedAsync(scope, command, cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            if (requestRow.existing_evaluation_id is Guid existingEvaluationId)
            {
                if (existingEvaluationId != command.Completed.EvaluationId)
                {
                    await scope.RollbackAsync(cancellationToken);
                    return new(false, EvaluationCompletionOutcomeCodes.IntegrityConflict, null, null, false);
                }

                await ReconcileWorkAsync(scope, command, cancellationToken);
                await scope.CommitAsync(cancellationToken);
                return new(
                    true,
                    EvaluationCompletionOutcomeCodes.Reconciled,
                    existingEvaluationId,
                    null,
                    true);
            }

            if (!string.Equals(requestRow.state, EvaluationRequestStates.Completing, StringComparison.Ordinal))
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            foreach (var item in command.EvidenceItems)
            {
                await scope.Connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO evaluation_evidence_items (
                            organization_id, evaluation_id, evidence_id, request_id, activity_id,
                            participant_id, attempt_id, session_id, source_type, source_id,
                            source_version_id, source_content_digest, locator_schema, locator_digest,
                            precision, integrity_state, created_by_service, created_at)
                        VALUES (
                            @OrganizationId, @EvaluationId, @EvidenceId, @RequestId, @ActivityId,
                            @ParticipantId, @AttemptId, @SessionId, @SourceType, @SourceId,
                            @SourceVersionId, @SourceContentDigest, 'evidence-locator.v1', @LocatorDigest,
                            @Precision, 'verified', @CreationServiceId, @CompletedAt);
                        """,
                        new
                        {
                            command.Completed.Ownership.OrganizationId,
                            command.Completed.EvaluationId,
                            item.EvidenceId,
                            command.RequestId,
                            command.Completed.Ownership.ActivityId,
                            command.Completed.Ownership.ParticipantId,
                            command.Completed.Ownership.AttemptId,
                            command.Completed.Ownership.SessionId,
                            item.SourceType,
                            SourceId = item.Source.SourceId,
                            SourceVersionId = item.Source.SourceVersionId,
                            SourceContentDigest = item.Source.ContentDigest,
                            LocatorDigest = item.Source.ContentDigest,
                            item.Precision,
                            CreationServiceId = command.Completed.CreationServiceId,
                            CompletedAt = command.Completed.CompletedAtUtc,
                        },
                        scope.Transaction,
                        cancellationToken: cancellationToken));
            }

            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO evaluation_evidence_sets (
                        organization_id, evaluation_id, evidence_set_id, request_id,
                        invocation_attempt_id, seal_schema, seal_digest, sealed_at, sealed_by_service)
                    VALUES (
                        @OrganizationId, @EvaluationId, @EvidenceSetId, @RequestId,
                        @InvocationAttemptId, 'evidence-set-jcs-sha256-v1', @SealDigest,
                        @CompletedAt, @CreationServiceId);
                    """,
                    new
                    {
                        command.Completed.Ownership.OrganizationId,
                        command.Completed.EvaluationId,
                        command.Completed.EvidenceSetId,
                        command.RequestId,
                        command.InvocationAttemptId,
                        SealDigest = command.Completed.EvidenceSetDigest,
                        CompletedAt = command.Completed.CompletedAtUtc,
                        CreationServiceId = command.Completed.CreationServiceId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            var ordinal = 1;
            foreach (var evidenceId in command.EvidenceItems.Select(item => item.EvidenceId))
            {
                await scope.Connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO evaluation_evidence_set_items (
                            organization_id, evaluation_id, evidence_set_id, evidence_id, item_ordinal)
                        VALUES (
                            @OrganizationId, @EvaluationId, @EvidenceSetId, @EvidenceId, @Ordinal);
                        """,
                        new
                        {
                            command.Completed.Ownership.OrganizationId,
                            command.Completed.EvaluationId,
                            command.Completed.EvidenceSetId,
                            EvidenceId = evidenceId,
                            Ordinal = ordinal++,
                        },
                        scope.Transaction,
                        cancellationToken: cancellationToken));
            }

            foreach (var judgment in command.Judgments)
            {
                await scope.Connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO evaluation_criterion_judgments (
                            organization_id, evaluation_id, judgment_id, request_id, criterion_id,
                            criterion_version, evaluator_mode, status, confidence, uncertainty_json,
                            rationale, score_json, provisional_feedback, deterministic_attempt_id)
                        VALUES (
                            @OrganizationId, @EvaluationId, @JudgmentId, @RequestId, @CriterionId,
                            @CriterionVersion, @EvaluatorMode, @Status, @Confidence, @UncertaintyJson::jsonb,
                            @Rationale, @ScoreJson::jsonb, @ProvisionalFeedback, @DeterministicAttemptId);
                        """,
                        new
                        {
                            command.Completed.Ownership.OrganizationId,
                            command.Completed.EvaluationId,
                            judgment.JudgmentId,
                            command.RequestId,
                            judgment.CriterionId,
                            judgment.CriterionVersion,
                            judgment.EvaluatorMode,
                            judgment.Status,
                            judgment.Confidence,
                            UncertaintyJson = JsonSerializer.Serialize(judgment.Uncertainty),
                            judgment.Rationale,
                            ScoreJson = judgment.Score is null ? null : JsonSerializer.Serialize(judgment.Score),
                            judgment.ProvisionalFeedback,
                            DeterministicAttemptId = judgment.DeterministicInvocationId,
                        },
                        scope.Transaction,
                        cancellationToken: cancellationToken));
            }

            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO evaluations (
                        organization_id, evaluation_id, request_id, activity_id, participant_id,
                        attempt_id, session_id, evidence_set_id, procedure_source_id,
                        procedure_source_version_id, procedure_digest, aggregate_status,
                        creation_service_id, completed_at, predecessor_evaluation_id)
                    VALUES (
                        @OrganizationId, @EvaluationId, @RequestId, @ActivityId, @ParticipantId,
                        @AttemptId, @SessionId, @EvidenceSetId, @ProcedureSourceId,
                        @ProcedureSourceVersionId, @ProcedureDigest, @AggregateStatus,
                        @CreationServiceId, @CompletedAt, @PredecessorEvaluationId);
                    """,
                    new
                    {
                        command.Completed.Ownership.OrganizationId,
                        command.Completed.EvaluationId,
                        command.RequestId,
                        command.Completed.Ownership.ActivityId,
                        command.Completed.Ownership.ParticipantId,
                        command.Completed.Ownership.AttemptId,
                        command.Completed.Ownership.SessionId,
                        command.Completed.EvidenceSetId,
                        ProcedureSourceId = command.Completed.ProcedureRef.SourceId,
                        ProcedureSourceVersionId = command.Completed.ProcedureRef.SourceVersionId,
                        ProcedureDigest = command.Completed.ProcedureRef.ContentDigest,
                        command.Completed.AggregateStatus,
                        CreationServiceId = command.Completed.CreationServiceId,
                        CompletedAt = command.Completed.CompletedAtUtc,
                        command.Completed.PredecessorEvaluationId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            foreach (var manifestRef in command.ManifestRefs)
            {
                await scope.Connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO evaluation_manifest_refs (
                            organization_id, evaluation_id, manifest_ref_id, ref_kind, protected_ref,
                            content_digest, created_at)
                        VALUES (
                            @OrganizationId, @EvaluationId, @ManifestRefId, @RefKind, @ProtectedRef,
                            @ContentDigest, @CompletedAt);
                        """,
                        new
                        {
                            command.Completed.Ownership.OrganizationId,
                            command.Completed.EvaluationId,
                            manifestRef.ManifestRefId,
                            manifestRef.RefKind,
                            manifestRef.ProtectedRef,
                            manifestRef.ContentDigest,
                            CompletedAt = command.Completed.CompletedAtUtc,
                        },
                        scope.Transaction,
                        cancellationToken: cancellationToken));
            }

            if (string.Equals(requestRow.request_kind, EvaluationRequestKinds.Replacement, StringComparison.Ordinal)
                && requestRow.predecessor_evaluation_id is Guid predecessorId
                && !string.IsNullOrWhiteSpace(requestRow.replacement_reason))
            {
                await scope.Connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO evaluation_lineage (
                            organization_id, lineage_id, predecessor_evaluation_id, successor_request_id,
                            successor_evaluation_id, reason, actor_type, actor_id, occurred_at)
                        VALUES (
                            @OrganizationId, @LineageId, @PredecessorEvaluationId, @SuccessorRequestId,
                            @SuccessorEvaluationId, @Reason, @ActorType, @ActorId, @OccurredAt);
                        """,
                        new
                        {
                            command.Completed.Ownership.OrganizationId,
                            LineageId = Guid.CreateVersion7(),
                            PredecessorEvaluationId = predecessorId,
                            SuccessorRequestId = command.RequestId,
                            SuccessorEvaluationId = command.Completed.EvaluationId,
                            Reason = requestRow.replacement_reason,
                            command.ActorType,
                            command.ActorId,
                            OccurredAt = command.Completed.CompletedAtUtc,
                        },
                        scope.Transaction,
                        cancellationToken: cancellationToken));
            }

            var handoffId = Guid.CreateVersion7();
            var isReplacement = string.Equals(
                requestRow.request_kind,
                EvaluationRequestKinds.Replacement,
                StringComparison.Ordinal);
            var eligibleCaseCount = await scope.Connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT COUNT(*)
                    FROM evaluation_review_handoffs
                    WHERE organization_id = @OrganizationId
                      AND activity_id = @ActivityId
                      AND participant_id = @ParticipantId
                      AND attempt_id = @AttemptId
                      AND session_id = @SessionId;
                    """,
                    new
                    {
                        command.Completed.Ownership.OrganizationId,
                        command.Completed.Ownership.ActivityId,
                        command.Completed.Ownership.ParticipantId,
                        command.Completed.Ownership.AttemptId,
                        command.Completed.Ownership.SessionId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            await PostgresReviewEvaluationHandoffWriter.TryWriteAsync(
                scope.Connection,
                scope.Transaction,
                new ReviewHandoffWriteCommand(
                    handoffId,
                    command.Completed.EvaluationId,
                    command.Completed.Ownership,
                    command.RequestId,
                    RecordInitialCandidate: eligibleCaseCount == 0 && !isReplacement,
                    InitialCandidateReason: eligibleCaseCount == 0 && !isReplacement
                        ? "single.eligible.evaluation"
                        : null,
                    PublishReplacementAvailable: isReplacement,
                    ReplacementReason: requestRow.replacement_reason,
                    command.ActorId,
                    command.ActorType,
                    command.Completed.CompletedAtUtc),
                cancellationToken);

            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE evaluation_requests
                    SET state = @CompletedState, completed_at = @CompletedAt, failure_category = NULL
                    WHERE organization_id = @OrganizationId AND request_id = @RequestId;
                    """,
                    new
                    {
                        CompletedState = EvaluationRequestStates.Completed,
                        CompletedAt = command.Completed.CompletedAtUtc,
                        command.Completed.Ownership.OrganizationId,
                        command.RequestId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            await ReconcileWorkAsync(scope, command, cancellationToken);

            await _auditEventWriter.InsertAsync(
                new AuditEventWriteModel(
                    Guid.CreateVersion7(),
                    command.Completed.Ownership.OrganizationId,
                    "evaluation.completed.v1",
                    command.Completed.CompletedAtUtc,
                    command.CorrelationId,
                    command.ActorType,
                    command.ActorId,
                    "evaluation.complete",
                    "evaluation",
                    command.Completed.EvaluationId,
                    "succeeded",
                    null,
                    null,
                    command.SourceChannel,
                    command.Completed.EvidenceSetDigest,
                    "service_delegation",
                    command.DelegationId),
                scope.Transaction,
                cancellationToken);
            await _outboxItemWriter.InsertAsync(
                new OutboxItemWriteModel(
                    Guid.CreateVersion7(),
                    command.Completed.Ownership.OrganizationId,
                    "evaluation.completed.v1",
                    "evaluation",
                    command.Completed.EvaluationId,
                    command.CorrelationId,
                    command.Completed.EvidenceSetDigest,
                    command.Completed.CompletedAtUtc),
                scope.Transaction,
                cancellationToken);

            await scope.CommitAsync(cancellationToken);
            return new(
                true,
                EvaluationCompletionOutcomeCodes.Completed,
                command.Completed.EvaluationId,
                handoffId,
                false);
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await scope.RollbackAsync(CancellationToken.None);
            return await TryReconcileUniqueRaceAsync(command, cancellationToken);
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<EvaluationCompletionResult> TryReconcileUniqueRaceAsync(
        EvaluationCompletionCommand command,
        CancellationToken cancellationToken)
    {
        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            cancellationToken);
        try
        {
            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    SELECT pg_advisory_xact_lock(
                        hashtextextended(@OrganizationId::text || ':' || @RequestId::text, 0));
                    """,
                    new
                    {
                        command.Completed.Ownership.OrganizationId,
                        command.RequestId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            if (!await IsAuthorizedAsync(scope, command, cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            var existingEvaluationId = await scope.Connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    """
                    SELECT evaluation_id
                    FROM evaluations
                    WHERE organization_id = @OrganizationId
                      AND request_id = @RequestId;
                    """,
                    new
                    {
                        command.Completed.Ownership.OrganizationId,
                        command.RequestId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            if (existingEvaluationId is null
                || existingEvaluationId != command.Completed.EvaluationId)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.IntegrityConflict, null, null, false);
            }

            await ReconcileWorkAsync(scope, command, cancellationToken);
            await scope.CommitAsync(cancellationToken);
            return new(
                true,
                EvaluationCompletionOutcomeCodes.Reconciled,
                existingEvaluationId,
                null,
                true);
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<bool> IsAuthorizedAsync(
        PostgresTransactionScope scope,
        EvaluationCompletionCommand command,
        CancellationToken cancellationToken)
    {
        var authorized = await scope.Connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                """
                SELECT delegation_id
                FROM service_delegations
                WHERE delegation_id = @DelegationId
                  AND organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND participant_id = @ParticipantId
                  AND attempt_id = @AttemptId
                  AND session_id = @SessionId
                  AND service_actor_id = @ActorId
                  AND allowed_action = 'evaluation.execute'
                  AND revoked_at IS NULL
                  AND effective_at <= clock_timestamp()
                  AND (expires_at IS NULL OR expires_at > clock_timestamp())
                FOR UPDATE;
                """,
                new
                {
                    command.DelegationId,
                    command.Completed.Ownership.OrganizationId,
                    command.Completed.Ownership.ActivityId,
                    command.Completed.Ownership.ParticipantId,
                    command.Completed.Ownership.AttemptId,
                    command.Completed.Ownership.SessionId,
                    command.ActorId,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));

        return authorized is not null;
    }

    private static async Task ReconcileWorkAsync(
        PostgresTransactionScope scope,
        EvaluationCompletionCommand command,
        CancellationToken cancellationToken)
    {
        await scope.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE evaluation_durable_work
                SET
                    state = 'completed',
                    claim_owner = NULL,
                    claim_lease_until = NULL,
                    failure_category = NULL,
                    last_committed_at = clock_timestamp()
                WHERE organization_id = @OrganizationId
                  AND work_id = @WorkId
                  AND request_id = @RequestId
                  AND state <> 'completed';

                UPDATE evaluation_invocation_attempts
                SET state = 'completed', finished_at = clock_timestamp(), failure_category = NULL
                WHERE organization_id = @OrganizationId
                  AND request_id = @RequestId
                  AND invocation_attempt_id = @InvocationAttemptId
                  AND state <> 'completed';
                """,
                new
                {
                    command.Completed.Ownership.OrganizationId,
                    command.WorkId,
                    command.RequestId,
                    command.InvocationAttemptId,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));
    }

    private sealed record RequestRow(
        Guid request_id,
        string request_kind,
        string state,
        Guid? predecessor_evaluation_id,
        string? replacement_reason,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        Guid rubric_source_id,
        Guid rubric_source_version_id,
        string rubric_content_digest,
        Guid? existing_evaluation_id,
        Guid? existing_evidence_set_id);
}
