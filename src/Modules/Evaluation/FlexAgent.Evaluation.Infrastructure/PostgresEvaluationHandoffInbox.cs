using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresEvaluationHandoffInbox(PostgresConnectionAccessor connectionAccessor)
    : IEvaluationHandoffInbox
{
    public async Task<bool> RecordAsync(
        Guid deliveryId,
        EvaluationHandoffSnapshot handoff,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handoff);
        if (deliveryId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryId));
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var inserted = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO evaluation_handoff_inbox (
                    organization_id, delivery_id, activity_id, participant_id,
                    attempt_id, session_id, handoff_id, event_schema, state)
                VALUES (
                    @OrganizationId, @DeliveryId, @ActivityId, @ParticipantId,
                    @AttemptId, @SessionId, @HandoffId,
                    'session.evaluation_handoff.recorded.v1', 'pending')
                ON CONFLICT (organization_id, session_id, handoff_id) DO NOTHING;
                """,
                new
                {
                    handoff.Ownership.OrganizationId,
                    DeliveryId = deliveryId,
                    handoff.Ownership.ActivityId,
                    handoff.Ownership.ParticipantId,
                    handoff.Ownership.AttemptId,
                    handoff.Ownership.SessionId,
                    handoff.HandoffId,
                },
                cancellationToken: cancellationToken));
        if (inserted == 1)
        {
            return true;
        }

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM evaluation_handoff_inbox
                    WHERE organization_id = @OrganizationId
                      AND activity_id = @ActivityId
                      AND participant_id = @ParticipantId
                      AND attempt_id = @AttemptId
                      AND session_id = @SessionId
                      AND handoff_id = @HandoffId);
                """,
                new
                {
                    handoff.Ownership.OrganizationId,
                    handoff.Ownership.ActivityId,
                    handoff.Ownership.ParticipantId,
                    handoff.Ownership.AttemptId,
                    handoff.Ownership.SessionId,
                    handoff.HandoffId,
                },
                cancellationToken: cancellationToken));
    }
}
