using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresEvaluationAnnotationService(
    PostgresConnectionAccessor connectionAccessor,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null) : IEvaluationAnnotationService
{
    private readonly IAuditEventWriter _auditEventWriter =
        auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter =
        outboxItemWriter ?? new PostgresOutboxItemWriter();

    public async Task<EvaluationDecision<Guid>> TryAppendAsync(
        EvaluationAnnotationAppendCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.EvaluationId == Guid.Empty
            || command.DelegationId == Guid.Empty
            || command.ActorId == Guid.Empty
            || command.CorrelationId == Guid.Empty
            || !EvaluationIdentity.IsStableId(command.SourceChannel))
        {
            return EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.InvalidField);
        }

        var annotationId = Guid.CreateVersion7();
        var created = EvaluationAnnotation.TryCreate(
            annotationId,
            command.EvaluationId,
            command.Kind,
            command.Disposition,
            command.Reason,
            command.ActorType,
            "evaluation.annotation",
            command.OccurredAtUtc,
            command.EvidenceId);
        if (!created.Succeeded || created.Value is null)
        {
            return EvaluationDecision<Guid>.Fail(created.OutcomeCode, created.Field);
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            cancellationToken);
        try
        {
            var evaluationRow = await scope.Connection.QuerySingleOrDefaultAsync<EvaluationScopeRow>(
                new CommandDefinition(
                    """
                    SELECT
                        evaluation.organization_id,
                        evaluation.activity_id,
                        evaluation.participant_id,
                        evaluation.attempt_id,
                        evaluation.session_id
                    FROM evaluations AS evaluation
                    WHERE evaluation.evaluation_id = @EvaluationId
                    FOR UPDATE;
                    """,
                    new { command.EvaluationId },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (evaluationRow is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.InvalidField, "evaluation_id");
            }

            if (!await PostgresEvaluationServiceDelegation.IsAuthorizedAsync(
                    scope,
                    command.DelegationId,
                    evaluationRow.organization_id,
                    evaluationRow.activity_id,
                    evaluationRow.participant_id,
                    evaluationRow.attempt_id,
                    evaluationRow.session_id,
                    command.ActorId,
                    EvaluationAuthorizedActions.Annotate,
                    cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return EvaluationDecision<Guid>.Fail(EvaluationPersistenceOutcomeCodes.Denied);
            }

            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO evaluation_annotations (
                        organization_id, annotation_id, evaluation_id, evidence_id, kind,
                        disposition, reason, actor_type, actor_id, occurred_at)
                    VALUES (
                        @OrganizationId, @AnnotationId, @EvaluationId, @EvidenceId, @Kind,
                        @Disposition, @Reason, @ActorType, @ActorId, @OccurredAt);
                    """,
                    new
                    {
                        OrganizationId = evaluationRow.organization_id,
                        AnnotationId = annotationId,
                        command.EvaluationId,
                        command.EvidenceId,
                        command.Kind,
                        command.Disposition,
                        command.Reason,
                        command.ActorType,
                        ActorId = command.ActorId,
                        OccurredAt = command.OccurredAtUtc,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO evaluation_dispositions (
                        organization_id, evaluation_id, current_disposition, last_annotation_id,
                        version, updated_at)
                    VALUES (
                        @OrganizationId, @EvaluationId, @Disposition, @AnnotationId, 1, @OccurredAt)
                    ON CONFLICT (organization_id, evaluation_id) DO UPDATE
                    SET
                        current_disposition = EXCLUDED.current_disposition,
                        last_annotation_id = EXCLUDED.last_annotation_id,
                        version = evaluation_dispositions.version + 1,
                        updated_at = EXCLUDED.updated_at;
                    """,
                    new
                    {
                        OrganizationId = evaluationRow.organization_id,
                        command.EvaluationId,
                        command.Disposition,
                        AnnotationId = annotationId,
                        OccurredAt = command.OccurredAtUtc,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            await _auditEventWriter.InsertAsync(
                new AuditEventWriteModel(
                    Guid.CreateVersion7(),
                    evaluationRow.organization_id,
                    "evaluation.annotation.appended.v1",
                    command.OccurredAtUtc,
                    command.CorrelationId,
                    command.ActorType,
                    command.ActorId,
                    "evaluation.annotate",
                    "evaluation.annotation",
                    annotationId,
                    "succeeded",
                    null,
                    null,
                    command.SourceChannel,
                    command.Reason,
                    "service_delegation",
                    command.DelegationId),
                scope.Transaction,
                cancellationToken);
            await _outboxItemWriter.InsertAsync(
                new OutboxItemWriteModel(
                    Guid.CreateVersion7(),
                    evaluationRow.organization_id,
                    "evaluation.annotation.appended.v1",
                    "evaluation.annotation",
                    annotationId,
                    command.CorrelationId,
                    command.Reason,
                    command.OccurredAtUtc),
                scope.Transaction,
                cancellationToken);

            await scope.CommitAsync(cancellationToken);
            return EvaluationDecision<Guid>.Ok(annotationId);
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private sealed record EvaluationScopeRow(
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id);
}
