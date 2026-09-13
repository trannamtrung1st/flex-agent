using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;

namespace FlexAgent.Evaluation.Infrastructure.Review;

public sealed class PostgresEvaluationReplacementReviewSignal(
    PostgresConnectionAccessor connectionAccessor,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null) : IEvaluationReplacementReviewSignal
{
    private readonly IAuditEventWriter _auditEventWriter =
        auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter =
        outboxItemWriter ?? new PostgresOutboxItemWriter();

    public async Task<EvaluationDecision<bool>> TryPublishReplacementAvailableAsync(
        EvaluationReplacementStaleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.PredecessorEvaluationId == Guid.Empty
            || command.SuccessorEvaluationId == Guid.Empty
            || command.RequestId == Guid.Empty
            || command.DelegationId == Guid.Empty
            || command.ActorId == Guid.Empty
            || command.CorrelationId == Guid.Empty
            || !EvaluationIdentity.IsStableId(command.SourceChannel)
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
                        evaluation.organization_id,
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
                    WHERE evaluation.evaluation_id = @SuccessorEvaluationId
                      AND evaluation.request_id = @RequestId
                    FOR UPDATE OF evaluation;
                    """,
                    new
                    {
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

            if (!await PostgresEvaluationServiceDelegation.IsAuthorizedAsync(
                    scope,
                    command.DelegationId,
                    scopeRow.organization_id,
                    scopeRow.activity_id,
                    scopeRow.participant_id,
                    scopeRow.attempt_id,
                    scopeRow.session_id,
                    command.ActorId,
                    EvaluationAuthorizedActions.ReplacementSignal,
                    cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return EvaluationDecision<bool>.Fail(EvaluationPersistenceOutcomeCodes.Denied);
            }

            var handoffId = Guid.CreateVersion7();
            await PostgresReviewEvaluationHandoffWriter.TryWriteAsync(
                scope.Connection,
                scope.Transaction,
                new ReviewHandoffWriteCommand(
                    handoffId,
                    command.SuccessorEvaluationId,
                    EvaluationOwnership.TryCreate(
                        scopeRow.organization_id,
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
                    EvaluationActorTypes.Service,
                    command.OccurredAtUtc),
                cancellationToken);

            await _auditEventWriter.InsertAsync(
                new AuditEventWriteModel(
                    Guid.CreateVersion7(),
                    scopeRow.organization_id,
                    "evaluation.replacement.available.v1",
                    command.OccurredAtUtc,
                    command.CorrelationId,
                    EvaluationActorTypes.Service,
                    command.ActorId,
                    "evaluation.replacement.signal",
                    "evaluation.review_handoff",
                    handoffId,
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
                    scopeRow.organization_id,
                    "evaluation.replacement.available.v1",
                    "evaluation.review_handoff",
                    handoffId,
                    command.CorrelationId,
                    command.Reason,
                    command.OccurredAtUtc),
                scope.Transaction,
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
        Guid organization_id,
        Guid evaluation_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        Guid? predecessor_evaluation_id);
}
