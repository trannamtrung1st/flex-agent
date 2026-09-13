using Dapper;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure;

internal static class PostgresEvaluationServiceDelegation
{
    internal static async Task<bool> IsAuthorizedAsync(
        PostgresTransactionScope scope,
        Guid delegationId,
        Guid organizationId,
        Guid activityId,
        Guid participantId,
        Guid attemptId,
        Guid sessionId,
        Guid actorId,
        string allowedAction,
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
                  AND allowed_action = @AllowedAction
                  AND revoked_at IS NULL
                  AND effective_at <= clock_timestamp()
                  AND (expires_at IS NULL OR expires_at > clock_timestamp())
                FOR UPDATE;
                """,
                new
                {
                    DelegationId = delegationId,
                    OrganizationId = organizationId,
                    ActivityId = activityId,
                    ParticipantId = participantId,
                    AttemptId = attemptId,
                    SessionId = sessionId,
                    ActorId = actorId,
                    AllowedAction = allowedAction,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));

        return authorized is not null;
    }
}
