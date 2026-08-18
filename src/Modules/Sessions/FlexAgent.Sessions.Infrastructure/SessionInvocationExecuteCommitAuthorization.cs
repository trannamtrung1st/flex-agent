using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Sessions.Domain;
using Npgsql;

namespace FlexAgent.Sessions.Infrastructure;

internal static class SessionInvocationExecuteCommitAuthorization
{
    public static async Task<AuthorizationDecision> ReauthorizeAsync(
        ICommitAuthorizationKernel kernel,
        TrustedRuntimeActor actor,
        SessionOwnership ownership,
        Guid correlationId,
        string sourceChannel,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = transaction.Connection
            ?? throw new InvalidOperationException("Commit authorization requires an open transaction.");
        var delegationId = await connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                """
                SELECT invocation_execute_delegation_id
                FROM session_runtimes
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId;
                """,
                new { ownership.OrganizationId, ownership.SessionId },
                transaction,
                cancellationToken: cancellationToken));
        return await kernel.ReauthorizeInTransactionAsync(
            new AuthorizationRequest(
                new TrustedActor(actor.ActorId, actor.ActorType),
                new OrganizationScope(ownership.OrganizationId),
                AuthorizationActions.ExecuteSessionInvocation,
                new ResourceScope(
                    new OrganizationScope(ownership.OrganizationId),
                    AuthorizationResourceTypes.Session,
                    ownership.SessionId),
                sourceChannel,
                correlationId,
                delegationId,
                ownership.ActivityId,
                ownership.ParticipantId,
                ownership.AttemptId),
            transaction,
            cancellationToken);
    }
}
