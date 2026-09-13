using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresEvaluationAnnotationService(
    PostgresConnectionAccessor connectionAccessor) : IEvaluationAnnotationService
{
    public async Task<EvaluationDecision<Guid>> TryAppendAsync(
        EvaluationAnnotationAppendCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
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
                        command.OrganizationId,
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
                        command.OrganizationId,
                        command.EvaluationId,
                        command.Disposition,
                        AnnotationId = annotationId,
                        OccurredAt = command.OccurredAtUtc,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            await scope.CommitAsync(cancellationToken);
            return EvaluationDecision<Guid>.Ok(annotationId);
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
