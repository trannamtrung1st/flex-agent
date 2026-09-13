using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal static class EvaluationMutationDelegationSupport
{
    internal static async Task<Guid> EnsureDelegationAsync(
        Npgsql.NpgsqlConnection connection,
        Guid organizationId,
        Guid activityId,
        Guid participantId,
        Guid attemptId,
        Guid sessionId,
        Guid serviceActorId,
        string allowedAction,
        CancellationToken cancellationToken)
    {
        var delegationId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            """
            INSERT INTO service_delegations (
                delegation_id, organization_id, activity_id, participant_id, attempt_id,
                session_id, service_actor_id, allowed_action, system_purpose,
                initiating_authority, effective_at, expires_at, revoked_at, delegation_version)
            VALUES (
                @DelegationId, @OrganizationId, @ActivityId, @ParticipantId, @AttemptId,
                @SessionId, @ServiceActorId, @AllowedAction, @SystemPurpose,
                'synthetic.integration', clock_timestamp() - interval '1 minute',
                clock_timestamp() + interval '1 hour', NULL, 1);
            """,
            new
            {
                DelegationId = delegationId,
                OrganizationId = organizationId,
                ActivityId = activityId,
                ParticipantId = participantId,
                AttemptId = attemptId,
                SessionId = sessionId,
                ServiceActorId = serviceActorId,
                AllowedAction = allowedAction,
                SystemPurpose = allowedAction,
            });
        return delegationId;
    }

    internal static Task<Guid> EnsureAnnotateDelegationAsync(
        Npgsql.NpgsqlConnection connection,
        EvaluationOwnership ownership,
        Guid serviceActorId,
        CancellationToken cancellationToken) =>
        EnsureDelegationAsync(
            connection,
            ownership.OrganizationId,
            ownership.ActivityId,
            ownership.ParticipantId,
            ownership.AttemptId,
            ownership.SessionId,
            serviceActorId,
            EvaluationAuthorizedActions.Annotate,
            cancellationToken);

    internal static Task<Guid> EnsureReplacementSignalDelegationAsync(
        Npgsql.NpgsqlConnection connection,
        EvaluationOwnership ownership,
        Guid serviceActorId,
        CancellationToken cancellationToken) =>
        EnsureDelegationAsync(
            connection,
            ownership.OrganizationId,
            ownership.ActivityId,
            ownership.ParticipantId,
            ownership.AttemptId,
            ownership.SessionId,
            serviceActorId,
            EvaluationAuthorizedActions.ReplacementSignal,
            cancellationToken);
}
