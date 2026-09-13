using System.Text.Json;
using Dapper;
using FlexAgent.Contracts.Evaluation;
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
    IOutboxItemWriter? outboxItemWriter = null,
    IEvidenceLocatorCompletionService? evidenceLocatorCompletionService = null) : IEvaluationCompletionCoordinator
{
    private readonly IAuditEventWriter _auditEventWriter =
        auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter =
        outboxItemWriter ?? new PostgresOutboxItemWriter();
    private readonly IEvidenceLocatorCompletionService? _evidenceLocatorCompletionService =
        evidenceLocatorCompletionService;

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
                        request.organization_id,
                        request.activity_id,
                        request.participant_id,
                        request.attempt_id,
                        request.session_id,
                        request.handoff_id,
                        request.terminal_record_id,
                        request.handoff_terminal_state,
                        request.cutoff_sequence,
                        request.manifest_seal_procedure_id,
                        request.terminal_seal_digest,
                        request.configuration_record_id,
                        request.configuration_digest,
                        request.manifest_record_id,
                        request.manifest_digest,
                        request.rubric_source_id,
                        request.rubric_source_version_id,
                        request.rubric_content_digest,
                        request.submission_source_id,
                        request.submission_version_id,
                        request.submission_content_digest,
                        request.model_profile_id,
                        request.model_profile_version,
                        request.model_profile_digest,
                        request.provider_id,
                        request.credential_mode,
                        request.credential_binding_reference,
                        request.credential_binding_version,
                        request.evaluator_registry_version,
                        request.lifecycle_policy_ref,
                        request.idempotency_key,
                        request.delegation_id,
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

            var requestOwnership = ToRequestOwnership(requestRow);
            if (command.Completed.Ownership != requestOwnership)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            if (!await PostgresEvaluationServiceDelegation.IsAuthorizedAsync(
                    scope,
                    command.DelegationId,
                    requestRow.organization_id,
                    requestRow.activity_id,
                    requestRow.participant_id,
                    requestRow.attempt_id,
                    requestRow.session_id,
                    command.ActorId,
                    EvaluationAuthorizedActions.Execute,
                    cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            if (!string.Equals(requestRow.state, EvaluationRequestStates.Completing, StringComparison.Ordinal)
                && requestRow.existing_evaluation_id is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            var authoritativeRequest = EvaluationRequestRehydration.TryRebuild(
                MapAuthoritativeSnapshot(requestRow));
            if (!authoritativeRequest.Succeeded || authoritativeRequest.Value is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            var procedureSource = new PostgresProtectedEvaluationProcedureSource(connectionAccessor);
            var procedurePayload = await procedureSource.GetCanonicalUtf8Async(
                requestRow.organization_id,
                requestRow.rubric_source_id,
                requestRow.rubric_source_version_id,
                requestRow.rubric_content_digest,
                cancellationToken);
            if (procedurePayload is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            var procedure = EvaluationProcedureResolver.TryResolve(procedurePayload.Utf8);
            if (!procedure.Succeeded || procedure.Value is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            if (requestRow.existing_evaluation_id is Guid existingEvaluationId)
            {
                var reconciled = await TryReconcileExistingEvaluationAsync(
                    scope,
                    command,
                    existingEvaluationId,
                    authoritativeRequest.Value,
                    cancellationToken);
                if (!reconciled.Succeeded)
                {
                    await scope.RollbackAsync(cancellationToken);
                    return reconciled;
                }

                await scope.CommitAsync(cancellationToken);
                return reconciled;
            }

            if (!string.Equals(requestRow.state, EvaluationRequestStates.Completing, StringComparison.Ordinal))
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            var persistedRows = await LoadPersistedEvidenceRowsAsync(
                scope,
                command,
                cancellationToken);
            var authoritativeRecords = await TryResolveAuthoritativeEvidenceRecordsAsync(
                scope,
                authoritativeRequest.Value,
                procedure.Value,
                requestRow.handoff_id,
                command,
                persistedRows,
                cancellationToken);
            if (!authoritativeRecords.Succeeded || authoritativeRecords.Value is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, authoritativeRecords.OutcomeCode, null, null, false);
            }

            var authoritativeEvidenceItems = new List<EvidenceItem>(authoritativeRecords.Value.Count);
            foreach (var record in authoritativeRecords.Value)
            {
                var evidenceItem = EvaluationCompletionPersistedEvidenceVerifier.ToAuthoritativeEvidenceItem(
                    record,
                    authoritativeRequest.Value.FrozenInput.Ownership,
                    command.Completed.EvaluationId);
                if (!evidenceItem.Succeeded || evidenceItem.Value is null)
                {
                    await scope.RollbackAsync(cancellationToken);
                    return new(false, evidenceItem.OutcomeCode, null, null, false);
                }

                authoritativeEvidenceItems.Add(evidenceItem.Value);
            }

            var verifiedCompletion = EvaluationCompletionAuthorityVerifier.TryVerify(
                authoritativeRequest.Value,
                procedure.Value,
                command,
                authoritativeEvidenceItems,
                authoritativeRecords.Value);
            if (!verifiedCompletion.Succeeded || verifiedCompletion.Value is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(
                    false,
                    verifiedCompletion.OutcomeCode,
                    null,
                    null,
                    false);
            }

            if (!await VerifyDeterministicProvenanceAsync(
                    scope,
                    command,
                    procedure.Value,
                    authoritativeRecords.Value,
                    cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationFailureCodes.InvalidJudgment, null, null, false);
            }

            var completedAtUtc = EvaluationCompletionTimestampCanonicalization.ToPostgresUtc(
                command.Completed.CompletedAtUtc);
            var persistedEvidenceIds = persistedRows.Select(row => row.EvidenceId).ToHashSet();
            foreach (var record in authoritativeRecords.Value)
            {
                if (persistedEvidenceIds.Contains(record.EvidenceId))
                {
                    continue;
                }

                await scope.Connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO evaluation_evidence_items (
                            organization_id, evaluation_id, evidence_id, request_id, activity_id,
                            participant_id, attempt_id, session_id, source_type, source_id,
                            source_version_id, source_content_digest, locator_schema, locator_digest,
                            precision, integrity_state, locator_canonical_json, created_by_service, created_at)
                        VALUES (
                            @OrganizationId, @EvaluationId, @EvidenceId, @RequestId, @ActivityId,
                            @ParticipantId, @AttemptId, @SessionId, @SourceType, @SourceId,
                            @SourceVersionId, @SourceContentDigest, @LocatorSchema, @LocatorDigest,
                            @Precision, @IntegrityState, CAST(@LocatorCanonicalJson AS jsonb), @CreationServiceId, @CompletedAt);
                        """,
                        new
                        {
                            command.Completed.Ownership.OrganizationId,
                            command.Completed.EvaluationId,
                            record.EvidenceId,
                            command.RequestId,
                            command.Completed.Ownership.ActivityId,
                            command.Completed.Ownership.ParticipantId,
                            command.Completed.Ownership.AttemptId,
                            command.Completed.Ownership.SessionId,
                            record.SourceType,
                            SourceId = record.SourceId,
                            SourceVersionId = record.SourceVersionId,
                            SourceContentDigest = record.SourceContentDigest,
                            record.LocatorSchema,
                            LocatorDigest = record.LocatorDigest,
                            record.Precision,
                            IntegrityState = record.IntegrityState,
                            LocatorCanonicalJson = record.LocatorCanonicalJson,
                            CreationServiceId = command.Completed.CreationServiceId,
                            CompletedAt = completedAtUtc,
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
                        CompletedAt = completedAtUtc,
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

                var evidenceOrdinal = 0;
                foreach (var evidenceId in judgment.EvidenceIds)
                {
                    await scope.Connection.ExecuteAsync(
                        new CommandDefinition(
                            """
                            INSERT INTO evaluation_criterion_judgment_evidence_refs (
                                organization_id, evaluation_id, judgment_id, evidence_id, reference_ordinal)
                            VALUES (
                                @OrganizationId, @EvaluationId, @JudgmentId, @EvidenceId, @ReferenceOrdinal);
                            """,
                            new
                            {
                                command.Completed.Ownership.OrganizationId,
                                command.Completed.EvaluationId,
                                judgment.JudgmentId,
                                EvidenceId = evidenceId,
                                ReferenceOrdinal = evidenceOrdinal++,
                            },
                            scope.Transaction,
                            cancellationToken: cancellationToken));
                }
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
                        CompletedAt = completedAtUtc,
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
                            CompletedAt = completedAtUtc,
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
                            ActorType = EvaluationActorTypes.Service,
                            command.ActorId,
                            OccurredAt = completedAtUtc,
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
                    EvaluationActorTypes.Service,
                    completedAtUtc),
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
                        CompletedAt = completedAtUtc,
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
                    completedAtUtc,
                    command.CorrelationId,
                    EvaluationActorTypes.Service,
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
                    completedAtUtc),
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

            var requestRow = await scope.Connection.QuerySingleOrDefaultAsync<RequestRow>(
                new CommandDefinition(
                    """
                    SELECT
                        request.request_id,
                        request.request_kind,
                        request.state,
                        request.predecessor_evaluation_id,
                        request.replacement_reason,
                        request.organization_id,
                        request.activity_id,
                        request.participant_id,
                        request.attempt_id,
                        request.session_id,
                        request.handoff_id,
                        request.terminal_record_id,
                        request.handoff_terminal_state,
                        request.cutoff_sequence,
                        request.manifest_seal_procedure_id,
                        request.terminal_seal_digest,
                        request.configuration_record_id,
                        request.configuration_digest,
                        request.manifest_record_id,
                        request.manifest_digest,
                        request.rubric_source_id,
                        request.rubric_source_version_id,
                        request.rubric_content_digest,
                        request.submission_source_id,
                        request.submission_version_id,
                        request.submission_content_digest,
                        request.model_profile_id,
                        request.model_profile_version,
                        request.model_profile_digest,
                        request.provider_id,
                        request.credential_mode,
                        request.credential_binding_reference,
                        request.credential_binding_version,
                        request.evaluator_registry_version,
                        request.lifecycle_policy_ref,
                        request.idempotency_key,
                        request.delegation_id,
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

            var requestOwnership = ToRequestOwnership(requestRow);
            if (command.Completed.Ownership != requestOwnership)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            if (!await PostgresEvaluationServiceDelegation.IsAuthorizedAsync(
                    scope,
                    command.DelegationId,
                    requestRow.organization_id,
                    requestRow.activity_id,
                    requestRow.participant_id,
                    requestRow.attempt_id,
                    requestRow.session_id,
                    command.ActorId,
                    EvaluationAuthorizedActions.Execute,
                    cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            var authoritativeRequest = EvaluationRequestRehydration.TryRebuild(
                MapAuthoritativeSnapshot(requestRow));
            if (!authoritativeRequest.Succeeded || authoritativeRequest.Value is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationCompletionOutcomeCodes.Denied, null, null, false);
            }

            var existingEvaluationId = requestRow.existing_evaluation_id
                ?? await scope.Connection.QuerySingleOrDefaultAsync<Guid?>(
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

            var reconciled = await TryReconcileExistingEvaluationAsync(
                scope,
                command,
                existingEvaluationId,
                authoritativeRequest.Value,
                cancellationToken);
            if (!reconciled.Succeeded)
            {
                await scope.RollbackAsync(cancellationToken);
                return reconciled;
            }

            await scope.CommitAsync(cancellationToken);
            return reconciled;
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<EvaluationCompletionResult> TryReconcileExistingEvaluationAsync(
        PostgresTransactionScope scope,
        EvaluationCompletionCommand command,
        Guid? existingEvaluationId,
        EvaluationRequest authoritativeRequest,
        CancellationToken cancellationToken)
    {
        if (existingEvaluationId is null
            || existingEvaluationId != command.Completed.EvaluationId)
        {
            return new(false, EvaluationCompletionOutcomeCodes.IntegrityConflict, null, null, false);
        }

        var storedCompletion = await LoadStoredCompletionSnapshotAsync(
            scope,
            command,
            cancellationToken);
        if (storedCompletion is null
            || !EvaluationCompletionEquivalence.IsEquivalent(
                command.Completed,
                storedCompletion,
                command.Judgments,
                command.ManifestRefs,
                authoritativeRequest))
        {
            return new(false, EvaluationCompletionOutcomeCodes.IntegrityConflict, null, null, false);
        }

        await ReconcileWorkAsync(scope, command, cancellationToken);
        return new(
            true,
            EvaluationCompletionOutcomeCodes.Reconciled,
            existingEvaluationId,
            null,
            true);
    }

    private async Task<EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>> TryResolveAuthoritativeEvidenceRecordsAsync(
        PostgresTransactionScope scope,
        EvaluationRequest authoritativeRequest,
        EvaluationProcedureV1 procedure,
        string handoffId,
        EvaluationCompletionCommand command,
        IReadOnlyList<PersistedEvaluationEvidenceRow> persistedRows,
        CancellationToken cancellationToken)
    {
        if (_evidenceLocatorCompletionService is null
            || command.EvidenceLocators.Count == 0
            || command.EvidenceLocators.Count != command.EvidenceItems.Count)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "persisted_evidence");
        }

        var locatorRequest = new EvidenceLocatorCompletionRequest(
            command.Completed.EvaluationId,
            command.RequestId,
            handoffId,
            command.EvidenceLocators);
        var verified = await _evidenceLocatorCompletionService.TryVerifyAsync(
            command.Completed.Ownership.OrganizationId,
            command.Completed.Ownership.SessionId,
            procedure,
            locatorRequest,
            cancellationToken);
        if (!verified.Succeeded || verified.Value is null)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                verified.OutcomeCode,
                verified.Field);
        }

        var bindVerified = EvaluationCompletionPersistedEvidenceVerifier.TryBindVerifiedLocatorRecords(
            command.EvidenceItems,
            verified.Value.LocatorRecords);
        if (!bindVerified.Succeeded || bindVerified.Value is null)
        {
            return bindVerified;
        }

        if (persistedRows.Count > 0)
        {
            var bindPersisted = EvaluationCompletionPersistedEvidenceVerifier.TryBindAuthoritative(
                authoritativeRequest.FrozenInput,
                command.Completed.EvaluationId,
                command.EvidenceItems,
                persistedRows);
            if (!bindPersisted.Succeeded || bindPersisted.Value is null)
            {
                return bindPersisted;
            }

            if (!EvaluationCompletionPersistedEvidenceVerifier.LocatorRecordsEquivalent(
                    bindPersisted.Value,
                    bindVerified.Value))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                    EvaluationCompletionOutcomeCodes.IntegrityConflict,
                    "persisted_evidence");
            }
        }

        return bindVerified;
    }

    private static async Task<bool> VerifyDeterministicProvenanceAsync(
        PostgresTransactionScope scope,
        EvaluationCompletionCommand command,
        EvaluationProcedureV1 procedure,
        IReadOnlyList<EvaluationEvidenceLocatorRecord> authoritativeEvidence,
        CancellationToken cancellationToken)
    {
        var attempts = await LoadInvocationDeterministicAttemptsAsync(
            scope,
            command,
            cancellationToken);
        var inputAuthorities = await LoadDeterministicInputAuthoritiesAsync(
            scope,
            command,
            cancellationToken);
        foreach (var judgment in command.Judgments)
        {
            if (judgment.DeterministicInvocationId is null)
            {
                continue;
            }

            var criterion = procedure.Criteria.FirstOrDefault(item =>
                string.Equals(item.CriterionId, judgment.CriterionId, StringComparison.Ordinal)
                && string.Equals(item.CriterionVersion, judgment.CriterionVersion, StringComparison.Ordinal));
            if (criterion?.DeterministicEvaluator is null)
            {
                return false;
            }

            if (!EvaluationCompletionDeterministicProvenanceVerifier.IsJudgmentProvenanceValid(
                    judgment,
                    criterion,
                    command.InvocationAttemptId,
                    attempts,
                    inputAuthorities,
                    authoritativeEvidence,
                    out _))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<IReadOnlyList<DeterministicAttemptProvenanceRow>> LoadInvocationDeterministicAttemptsAsync(
        PostgresTransactionScope scope,
        EvaluationCompletionCommand command,
        CancellationToken cancellationToken)
    {
        var rows = await scope.Connection.QueryAsync<DeterministicAttemptProvenanceRow>(
            new CommandDefinition(
                """
                SELECT
                    deterministic_attempt_id AS DeterministicAttemptId,
                    invocation_attempt_id AS InvocationAttemptId,
                    criterion_id AS CriterionId,
                    criterion_version AS CriterionVersion,
                    evaluator_id AS EvaluatorId,
                    evaluator_version AS EvaluatorVersion,
                    evaluator_digest AS EvaluatorDigest,
                    dependency_digest AS DependencyDigest,
                    configuration_digest AS ConfigurationDigest,
                    canonical_input_digest AS CanonicalInputDigest,
                    protected_input_ref AS ProtectedInputRef,
                    outcome AS Outcome
                FROM evaluation_deterministic_attempts
                WHERE organization_id = @OrganizationId
                  AND request_id = @RequestId
                  AND invocation_attempt_id = @InvocationAttemptId;
                """,
                new
                {
                    command.Completed.Ownership.OrganizationId,
                    command.RequestId,
                    command.InvocationAttemptId,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));

        return rows.AsList();
    }

    private static async Task<IReadOnlyList<DeterministicInputAuthorityRecord>> LoadDeterministicInputAuthoritiesAsync(
        PostgresTransactionScope scope,
        EvaluationCompletionCommand command,
        CancellationToken cancellationToken)
    {
        var rows = await scope.Connection.QueryAsync<DeterministicInputAuthorityRecord>(
            new CommandDefinition(
                """
                SELECT
                    criterion_id AS CriterionId,
                    criterion_version AS CriterionVersion,
                    canonical_input_digest AS CanonicalInputDigest,
                    protected_input_ref AS ProtectedInputRef
                FROM evaluation_deterministic_input_authority
                WHERE organization_id = @OrganizationId
                  AND request_id = @RequestId
                  AND invocation_attempt_id = @InvocationAttemptId;
                """,
                new
                {
                    command.Completed.Ownership.OrganizationId,
                    command.RequestId,
                    command.InvocationAttemptId,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));

        return rows.AsList();
    }

    private static EvaluationOwnership ToRequestOwnership(RequestRow requestRow) =>
        new(
            requestRow.organization_id,
            requestRow.activity_id,
            requestRow.participant_id,
            requestRow.attempt_id,
            requestRow.session_id);

    private static async Task<IReadOnlyList<PersistedEvaluationEvidenceRow>> LoadPersistedEvidenceRowsAsync(
        PostgresTransactionScope scope,
        EvaluationCompletionCommand command,
        CancellationToken cancellationToken)
    {
        var rows = await scope.Connection.QueryAsync<PersistedEvaluationEvidenceRow>(
            new CommandDefinition(
                """
                SELECT
                    evidence_id AS EvidenceId,
                    source_type AS SourceType,
                    source_id AS SourceId,
                    source_version_id AS SourceVersionId,
                    source_content_digest AS SourceContentDigest,
                    locator_schema AS LocatorSchema,
                    locator_digest AS LocatorDigest,
                    precision AS Precision,
                    integrity_state AS IntegrityState,
                    locator_canonical_json::text AS LocatorCanonicalJson
                FROM evaluation_evidence_items
                WHERE organization_id = @OrganizationId
                  AND evaluation_id = @EvaluationId
                  AND request_id = @RequestId
                FOR UPDATE;
                """,
                new
                {
                    command.Completed.Ownership.OrganizationId,
                    command.Completed.EvaluationId,
                    command.RequestId,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));

        return rows.AsList();
    }

    private static async Task<StoredCompletionSnapshot?> LoadStoredCompletionSnapshotAsync(
        PostgresTransactionScope scope,
        EvaluationCompletionCommand command,
        CancellationToken cancellationToken)
    {
        var evaluation = await scope.Connection.QuerySingleOrDefaultAsync<StoredCompletionSnapshotRow>(
            new CommandDefinition(
                """
                SELECT
                    evaluation.evaluation_id AS EvaluationId,
                    evaluation.request_id AS RequestId,
                    evaluation.activity_id AS ActivityId,
                    evaluation.participant_id AS ParticipantId,
                    evaluation.attempt_id AS AttemptId,
                    evaluation.session_id AS SessionId,
                    evaluation.evidence_set_id AS EvidenceSetId,
                    evaluation.procedure_source_id AS ProcedureSourceId,
                    evaluation.procedure_source_version_id AS ProcedureSourceVersionId,
                    evaluation.procedure_digest AS ProcedureDigest,
                    evaluation.aggregate_status AS AggregateStatus,
                    evaluation.creation_service_id AS CreationServiceId,
                    evaluation.completed_at AS CompletedAtUtc,
                    evaluation.predecessor_evaluation_id AS PredecessorEvaluationId,
                    evidence_set.seal_digest AS EvidenceSetDigest
                FROM evaluations AS evaluation
                INNER JOIN evaluation_evidence_sets AS evidence_set
                  ON evidence_set.organization_id = evaluation.organization_id
                 AND evidence_set.evaluation_id = evaluation.evaluation_id
                 AND evidence_set.evidence_set_id = evaluation.evidence_set_id
                WHERE evaluation.organization_id = @OrganizationId
                  AND evaluation.request_id = @RequestId
                  AND evaluation.evaluation_id = @EvaluationId;
                """,
                new
                {
                    command.Completed.Ownership.OrganizationId,
                    command.RequestId,
                    command.Completed.EvaluationId,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));
        if (evaluation is null)
        {
            return null;
        }

        var judgmentRows = await scope.Connection.QueryAsync<StoredJudgmentRow>(
            new CommandDefinition(
                """
                SELECT
                    judgment_id AS JudgmentId,
                    criterion_id AS CriterionId,
                    criterion_version AS CriterionVersion,
                    evaluator_mode AS EvaluatorMode,
                    status AS Status,
                    confidence AS Confidence,
                    uncertainty_json::text AS UncertaintyJson,
                    rationale AS Rationale,
                    score_json::text AS ScoreJson,
                    provisional_feedback AS ProvisionalFeedback,
                    deterministic_attempt_id AS DeterministicAttemptId
                FROM evaluation_criterion_judgments
                WHERE organization_id = @OrganizationId
                  AND evaluation_id = @EvaluationId
                  AND request_id = @RequestId;
                """,
                new
                {
                    command.Completed.Ownership.OrganizationId,
                    command.Completed.EvaluationId,
                    command.RequestId,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));

        var evidenceRefRows = await scope.Connection.QueryAsync<StoredJudgmentEvidenceRefRow>(
            new CommandDefinition(
                """
                SELECT
                    judgment_id AS JudgmentId,
                    evidence_id AS EvidenceId,
                    reference_ordinal AS ReferenceOrdinal
                FROM evaluation_criterion_judgment_evidence_refs
                WHERE organization_id = @OrganizationId
                  AND evaluation_id = @EvaluationId
                ORDER BY judgment_id, reference_ordinal;
                """,
                new
                {
                    command.Completed.Ownership.OrganizationId,
                    command.Completed.EvaluationId,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));

        var evidenceIdsByJudgment = evidenceRefRows
            .GroupBy(row => row.JudgmentId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Guid>)group
                    .OrderBy(row => row.ReferenceOrdinal)
                    .Select(row => row.EvidenceId)
                    .ToArray());
        var judgments = judgmentRows
            .Select(row => new StoredJudgmentSnapshot(
                row.JudgmentId,
                row.CriterionId,
                row.CriterionVersion,
                row.EvaluatorMode,
                row.Status,
                row.Confidence,
                row.UncertaintyJson,
                row.Rationale,
                row.ScoreJson,
                row.ProvisionalFeedback,
                row.DeterministicAttemptId,
                evidenceIdsByJudgment.TryGetValue(row.JudgmentId, out var evidenceIds)
                    ? evidenceIds
                    : Array.Empty<Guid>()))
            .ToList();

        var manifestRefs = await scope.Connection.QueryAsync<StoredManifestRefSnapshot>(
            new CommandDefinition(
                """
                SELECT
                    ref_kind AS RefKind,
                    protected_ref AS ProtectedRef,
                    content_digest AS ContentDigest
                FROM evaluation_manifest_refs
                WHERE organization_id = @OrganizationId
                  AND evaluation_id = @EvaluationId;
                """,
                new
                {
                    command.Completed.Ownership.OrganizationId,
                    command.Completed.EvaluationId,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));

        return new StoredCompletionSnapshot(
            evaluation.EvaluationId,
            evaluation.RequestId,
            new EvaluationOwnership(
                command.Completed.Ownership.OrganizationId,
                evaluation.ActivityId,
                evaluation.ParticipantId,
                evaluation.AttemptId,
                evaluation.SessionId),
            evaluation.ProcedureSourceId,
            evaluation.ProcedureSourceVersionId,
            evaluation.ProcedureDigest,
            evaluation.AggregateStatus,
            evaluation.EvidenceSetId,
            evaluation.EvidenceSetDigest,
            evaluation.CompletedAtUtc,
            evaluation.CreationServiceId,
            evaluation.PredecessorEvaluationId,
            judgments.AsList(),
            manifestRefs.AsList());
    }

    private static AuthoritativeEvaluationRequestSnapshot MapAuthoritativeSnapshot(RequestRow row) =>
        new(
            row.request_id,
            row.request_kind,
            row.state,
            row.predecessor_evaluation_id,
            row.replacement_reason,
            row.idempotency_key,
            row.delegation_id,
            row.organization_id,
            row.activity_id,
            row.participant_id,
            row.attempt_id,
            row.session_id,
            row.handoff_id,
            row.terminal_record_id,
            row.handoff_terminal_state,
            row.cutoff_sequence,
            row.manifest_seal_procedure_id,
            row.terminal_seal_digest,
            row.configuration_record_id,
            row.configuration_digest,
            row.manifest_record_id,
            row.manifest_digest,
            row.rubric_source_id,
            row.rubric_source_version_id,
            row.rubric_content_digest,
            row.submission_source_id,
            row.submission_version_id,
            row.submission_content_digest,
            row.model_profile_id,
            row.model_profile_version,
            row.model_profile_digest,
            row.provider_id,
            row.credential_mode,
            row.credential_binding_reference,
            row.credential_binding_version,
            row.evaluator_registry_version,
            row.lifecycle_policy_ref);

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

    private sealed record StoredJudgmentRow(
        Guid JudgmentId,
        string CriterionId,
        string CriterionVersion,
        string EvaluatorMode,
        string Status,
        string Confidence,
        string UncertaintyJson,
        string Rationale,
        string? ScoreJson,
        string? ProvisionalFeedback,
        Guid? DeterministicAttemptId);

    private sealed record StoredJudgmentEvidenceRefRow(
        Guid JudgmentId,
        Guid EvidenceId,
        int ReferenceOrdinal);

    private sealed record StoredCompletionSnapshotRow(
        Guid EvaluationId,
        Guid RequestId,
        Guid ActivityId,
        Guid ParticipantId,
        Guid AttemptId,
        Guid SessionId,
        Guid EvidenceSetId,
        Guid ProcedureSourceId,
        Guid ProcedureSourceVersionId,
        string ProcedureDigest,
        string AggregateStatus,
        string CreationServiceId,
        DateTimeOffset CompletedAtUtc,
        Guid? PredecessorEvaluationId,
        string EvidenceSetDigest);

    private sealed record RequestRow(
        Guid request_id,
        string request_kind,
        string state,
        Guid? predecessor_evaluation_id,
        string? replacement_reason,
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        string handoff_id,
        Guid terminal_record_id,
        string handoff_terminal_state,
        long cutoff_sequence,
        string manifest_seal_procedure_id,
        string terminal_seal_digest,
        Guid configuration_record_id,
        string configuration_digest,
        Guid manifest_record_id,
        string manifest_digest,
        Guid rubric_source_id,
        Guid rubric_source_version_id,
        string rubric_content_digest,
        Guid submission_source_id,
        Guid submission_version_id,
        string submission_content_digest,
        string model_profile_id,
        string model_profile_version,
        string model_profile_digest,
        string provider_id,
        string credential_mode,
        string credential_binding_reference,
        string credential_binding_version,
        string evaluator_registry_version,
        string lifecycle_policy_ref,
        string idempotency_key,
        Guid delegation_id,
        Guid? existing_evaluation_id,
        Guid? existing_evidence_set_id);
}
