using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using Npgsql;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresEvaluationAdmissionStore(
    PostgresConnectionAccessor connectionAccessor,
    EvaluationAdmissionAuthority authority,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null)
    : IEvaluationAdmissionStore
{
    private readonly IAuditEventWriter _auditEventWriter =
        auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter =
        outboxItemWriter ?? new PostgresOutboxItemWriter();

    public async Task<EvaluationAdmissionResult> AdmitAsync(
        AdmitEvaluationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.DelegationId == Guid.Empty
            || command.ActorId == Guid.Empty
            || command.CorrelationId == Guid.Empty
            || command.OrganizationBacklogLimit is < 1 or > 10000
            || command.MaxAttempts is < 1 or > 10
            || command.AttemptTimeoutSeconds is < 1 or > 3600
            || command.BackoffSeconds is < 1 or > 3600
            || command.Request.State != EvaluationRequestStates.Queued
            || !string.Equals(
                command.Request.DelegationRef,
                EvaluationDelegationReference.Format(command.DelegationId),
                StringComparison.Ordinal)
            || command.ActorType != "service"
            || !EvaluationIdentity.IsStableId(command.SourceChannel)
            || !string.Equals(
                command.Request.FrozenInput.EvaluatorRegistryVersion,
                authority.EvaluatorRegistryVersion,
                StringComparison.Ordinal)
            || !string.Equals(
                command.Request.FrozenInput.LifecyclePolicyRef,
                authority.LifecyclePolicyRef,
                StringComparison.Ordinal))
        {
            return new(false, EvaluationFailureCodes.InvalidField, null, null);
        }

        var input = command.Request.FrozenInput;
        var inputDigest = FrozenInputDigest.Compute(input);
        var ownership = input.Ownership;

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            cancellationToken);
        try
        {
            var organizationLocked = await scope.Connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    """
                    SELECT id
                    FROM organizations
                    WHERE id = @OrganizationId
                    FOR UPDATE;
                    """,
                    new { ownership.OrganizationId },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (organizationLocked is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationPersistenceOutcomeCodes.IneligibleHandoff, null, null);
            }

            var authorizedDelegation = await scope.Connection.QuerySingleOrDefaultAsync<Guid?>(
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
                        ownership.OrganizationId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        command.ActorId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (authorizedDelegation is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(false, EvaluationPersistenceOutcomeCodes.Denied, null, null);
            }

            var existing = await scope.Connection.QuerySingleOrDefaultAsync<ExistingAdmission>(
                new CommandDefinition(
                    """
                    SELECT
                        request.request_id,
                        request.frozen_input_digest,
                        request.request_kind,
                        request.predecessor_evaluation_id,
                        request.replacement_reason,
                        request.delegation_id,
                        work.work_id,
                        work.max_attempts,
                        work.attempt_timeout_seconds,
                        work.backoff_seconds
                    FROM evaluation_requests AS request
                    LEFT JOIN evaluation_durable_work AS work
                      ON work.organization_id = request.organization_id
                     AND work.request_id = request.request_id
                    WHERE request.organization_id = @OrganizationId
                      AND request.session_id = @SessionId
                      AND request.idempotency_key = @IdempotencyKey
                    FOR UPDATE OF request;
                    """,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.SessionId,
                        command.Request.IdempotencyKey,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (existing is not null)
            {
                await scope.CommitAsync(cancellationToken);
                return existing.Matches(command, inputDigest)
                    ? new(
                        true,
                        EvaluationPersistenceOutcomeCodes.Reconciled,
                        existing.request_id,
                        existing.work_id)
                    : new(
                        false,
                        EvaluationPersistenceOutcomeCodes.IdempotencyConflict,
                        existing.request_id,
                        existing.work_id);
            }

            var backlog = await scope.Connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT COUNT(*)::int
                    FROM evaluation_durable_work
                    WHERE organization_id = @OrganizationId
                      AND state IN ('pending', 'claimed');
                    """,
                    new { ownership.OrganizationId },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (backlog >= command.OrganizationBacklogLimit)
            {
                await scope.RollbackAsync(cancellationToken);
                return new(
                    false,
                    EvaluationPersistenceOutcomeCodes.BacklogLimitReached,
                    null,
                    null);
            }

            var workId = Guid.CreateVersion7();
            var now = await scope.Connection.ExecuteScalarAsync<DateTimeOffset>(
                new CommandDefinition(
                    "SELECT clock_timestamp();",
                    transaction: scope.Transaction,
                    cancellationToken: cancellationToken));

            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    InsertRequestSql,
                    new
                    {
                        ownership.OrganizationId,
                        command.Request.RequestId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        input.HandoffId,
                        input.TerminalRecordId,
                        HandoffEligibility = "eligible",
                        HandoffTerminalState = input.TerminalState,
                        input.CutoffSequence,
                        command.Request.RequestKind,
                        FrozenInputDigest = inputDigest,
                        command.Request.IdempotencyKey,
                        command.DelegationId,
                        State = EvaluationRequestStates.Queued,
                        command.Request.PredecessorEvaluationId,
                        command.Request.ReplacementReason,
                        RubricSourceId = input.Rubric.SourceId,
                        RubricSourceVersionId = input.Rubric.SourceVersionId,
                        RubricContentDigest = input.Rubric.ContentDigest,
                        SubmissionSourceId = input.Submission.SourceId,
                        SubmissionVersionId = input.Submission.SourceVersionId,
                        SubmissionContentDigest = input.Submission.ContentDigest,
                        ConfigurationId = input.ConfigurationId.ToString("D"),
                        ConfigurationRecordId = input.ConfigurationId,
                        input.ConfigurationDigest,
                        ManifestId = input.ManifestId.ToString("D"),
                        ManifestRecordId = input.ManifestId,
                        input.ManifestDigest,
                        input.ManifestSealProcedureId,
                        input.TerminalSealDigest,
                        input.Model.ProfileId,
                        input.Model.ProfileVersion,
                        input.Model.ProfileDigest,
                        input.Model.ProviderId,
                        input.Model.CredentialMode,
                        input.Model.CredentialBindingReference,
                        input.Model.CredentialBindingVersion,
                        input.EvaluatorRegistryVersion,
                        input.LifecyclePolicyRef,
                        command.CorrelationId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO evaluation_durable_work (
                        organization_id, work_id, request_id, activity_id, participant_id,
                        attempt_id, session_id, state, available_at, attempt_count,
                        max_attempts, attempt_timeout_seconds, backoff_seconds)
                    VALUES (
                        @OrganizationId, @WorkId, @RequestId, @ActivityId, @ParticipantId,
                        @AttemptId, @SessionId, 'pending', @Now, 0,
                        @MaxAttempts, @AttemptTimeoutSeconds, @BackoffSeconds);
                    """,
                    new
                    {
                        ownership.OrganizationId,
                        WorkId = workId,
                        command.Request.RequestId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        Now = now,
                        command.MaxAttempts,
                        command.AttemptTimeoutSeconds,
                        command.BackoffSeconds,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            await _auditEventWriter.InsertAsync(
                new AuditEventWriteModel(
                    Guid.CreateVersion7(),
                    ownership.OrganizationId,
                    "evaluation.request.admitted.v1",
                    now,
                    command.CorrelationId,
                    command.ActorType,
                    command.ActorId,
                    "evaluation.request.admit",
                    "evaluation.request",
                    command.Request.RequestId,
                    "succeeded",
                    null,
                    null,
                    command.SourceChannel,
                    inputDigest,
                    "service_delegation",
                    command.DelegationId),
                scope.Transaction,
                cancellationToken);
            await _outboxItemWriter.InsertAsync(
                new OutboxItemWriteModel(
                    Guid.CreateVersion7(),
                    ownership.OrganizationId,
                    "evaluation.request.admitted.v1",
                    "evaluation.request",
                    command.Request.RequestId,
                    command.CorrelationId,
                    inputDigest,
                    now),
                scope.Transaction,
                cancellationToken);

            await scope.CommitAsync(cancellationToken);
            return new(
                true,
                EvaluationPersistenceOutcomeCodes.Admitted,
                command.Request.RequestId,
                workId);
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            await scope.RollbackAsync(CancellationToken.None);
            return new(false, EvaluationPersistenceOutcomeCodes.IneligibleHandoff, null, null);
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private const string InsertRequestSql = """
        INSERT INTO evaluation_requests (
            organization_id, request_id, activity_id, participant_id, attempt_id,
            session_id, handoff_id, terminal_record_id, handoff_eligibility, handoff_terminal_state,
            cutoff_sequence, request_kind, frozen_input_digest, idempotency_key,
            delegation_id, state, predecessor_evaluation_id, replacement_reason,
            rubric_source_id, rubric_source_version_id, rubric_content_digest,
            submission_source_id, submission_version_id, submission_content_digest,
            configuration_id, configuration_record_id, configuration_digest,
            manifest_id, manifest_record_id, manifest_digest,
            manifest_seal_procedure_id, terminal_seal_digest,
            model_profile_id, model_profile_version, model_profile_digest, provider_id,
            credential_mode, credential_binding_reference, credential_binding_version,
            evaluator_registry_version, lifecycle_policy_ref, correlation_id)
        VALUES (
            @OrganizationId, @RequestId, @ActivityId, @ParticipantId, @AttemptId,
            @SessionId, @HandoffId, @TerminalRecordId, @HandoffEligibility, @HandoffTerminalState,
            @CutoffSequence, @RequestKind, @FrozenInputDigest, @IdempotencyKey,
            @DelegationId, @State, @PredecessorEvaluationId, @ReplacementReason,
            @RubricSourceId, @RubricSourceVersionId, @RubricContentDigest,
            @SubmissionSourceId, @SubmissionVersionId, @SubmissionContentDigest,
            @ConfigurationId, @ConfigurationRecordId, @ConfigurationDigest,
            @ManifestId, @ManifestRecordId, @ManifestDigest,
            @ManifestSealProcedureId, @TerminalSealDigest,
            @ProfileId, @ProfileVersion, @ProfileDigest, @ProviderId,
            @CredentialMode, @CredentialBindingReference, @CredentialBindingVersion,
            @EvaluatorRegistryVersion, @LifecyclePolicyRef, @CorrelationId);
        """;

    private sealed record ExistingAdmission(
        Guid request_id,
        string frozen_input_digest,
        string request_kind,
        Guid? predecessor_evaluation_id,
        string? replacement_reason,
        Guid delegation_id,
        Guid? work_id,
        int? max_attempts,
        int? attempt_timeout_seconds,
        int? backoff_seconds)
    {
        public bool Matches(AdmitEvaluationCommand command, string inputDigest) =>
            request_id == command.Request.RequestId
            && string.Equals(frozen_input_digest, inputDigest, StringComparison.Ordinal)
            && string.Equals(request_kind, command.Request.RequestKind, StringComparison.Ordinal)
            && predecessor_evaluation_id == command.Request.PredecessorEvaluationId
            && string.Equals(replacement_reason, command.Request.ReplacementReason, StringComparison.Ordinal)
            && delegation_id == command.DelegationId
            && work_id is not null
            && max_attempts == command.MaxAttempts
            && attempt_timeout_seconds == command.AttemptTimeoutSeconds
            && backoff_seconds == command.BackoffSeconds;
    }
}
