using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure.Review;

public sealed class PostgresEvaluationReplacementReviewSignal(
    PostgresConnectionAccessor connectionAccessor) : IEvaluationReplacementReviewSignal
{
    public async Task<EvaluationDecision<bool>> TryPublishReplacementAvailableAsync(
        EvaluationReplacementStaleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.OrganizationId == Guid.Empty
            || command.PredecessorEvaluationId == Guid.Empty
            || command.SuccessorEvaluationId == Guid.Empty
            || command.RequestId == Guid.Empty
            || command.ActorId == Guid.Empty
            || !EvaluationIdentity.IsStableId(command.Reason)
            || !EvaluationIdentity.IsUtc(command.OccurredAtUtc))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.InvalidField);
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            cancellationToken);
        try
        {
            var scopeRow = await scope.Connection.QuerySingleOrDefaultAsync<EvaluationScopeRow>(
                new CommandDefinition(
                    """
                    SELECT
                        evaluation.evaluation_id,
                        evaluation.activity_id,
                        evaluation.participant_id,
                        evaluation.attempt_id,
                        evaluation.session_id,
                        request.predecessor_evaluation_id
                    FROM evaluations AS evaluation
                    INNER JOIN evaluation_requests AS request
                      ON request.organization_id = evaluation.organization_id
                     AND request.request_id = evaluation.request_id
                    WHERE evaluation.organization_id = @OrganizationId
                      AND evaluation.evaluation_id = @SuccessorEvaluationId
                      AND evaluation.request_id = @RequestId;
                    """,
                    new
                    {
                        command.OrganizationId,
                        command.SuccessorEvaluationId,
                        command.RequestId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            if (scopeRow is null
                || scopeRow.predecessor_evaluation_id != command.PredecessorEvaluationId)
            {
                await scope.RollbackAsync(cancellationToken);
                return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.InvalidField, "predecessor");
            }

            await PostgresReviewEvaluationHandoffWriter.TryWriteAsync(
                scope.Connection,
                scope.Transaction,
                new ReviewHandoffWriteCommand(
                    Guid.CreateVersion7(),
                    command.SuccessorEvaluationId,
                    EvaluationOwnership.TryCreate(
                        command.OrganizationId,
                        scopeRow.activity_id,
                        scopeRow.participant_id,
                        scopeRow.attempt_id,
                        scopeRow.session_id).Value!,
                    command.RequestId,
                    RecordInitialCandidate: false,
                    InitialCandidateReason: null,
                    PublishReplacementAvailable: true,
                    ReplacementReason: command.Reason,
                    command.ActorId,
                    command.ActorType,
                    command.OccurredAtUtc),
                cancellationToken);

            await scope.CommitAsync(cancellationToken);
            return EvaluationDecision<bool>.Ok(true);
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private sealed record EvaluationScopeRow(
        Guid evaluation_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        Guid? predecessor_evaluation_id);
}
