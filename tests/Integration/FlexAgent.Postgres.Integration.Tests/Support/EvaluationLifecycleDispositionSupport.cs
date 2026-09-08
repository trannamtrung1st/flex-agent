using Dapper;
using FlexAgent.Evaluation.Application;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal static class EvaluationLifecycleDispositionSupport
{
    internal const string LifecycleExecutorRole = "flexagent_lifecycle_executor";
    internal const string ApplicationRole = "flexagent_application";
    internal const string DisposeDelegationAction = "evaluation.lifecycle.dispose";

    internal static async Task EnsureRoleSwitchMembershipAsync(NpgsqlConnection connection)
    {
        await connection.ExecuteAsync(
            $"""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_auth_members
                    WHERE roleid = '{LifecycleExecutorRole}'::regrole
                      AND member = CURRENT_USER::regrole)
                THEN
                    EXECUTE 'GRANT {LifecycleExecutorRole} TO ' || quote_ident(current_user);
                END IF;

                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_auth_members
                    WHERE roleid = '{ApplicationRole}'::regrole
                      AND member = CURRENT_USER::regrole)
                THEN
                    EXECUTE 'GRANT {ApplicationRole} TO ' || quote_ident(current_user);
                END IF;
            END $$;
            """);
    }

    internal static async Task RunAsLifecycleExecutorAsync(
        NpgsqlConnection connection,
        Func<NpgsqlConnection, Task> action)
    {
        await EnsureRoleSwitchMembershipAsync(connection);
        await connection.ExecuteAsync($"SET ROLE {LifecycleExecutorRole};");
        try
        {
            await action(connection);
        }
        finally
        {
            await connection.ExecuteAsync("RESET ROLE;");
        }
    }

    internal static async Task RunAsApplicationRoleAsync(
        NpgsqlConnection connection,
        Func<NpgsqlConnection, Task> action)
    {
        await EnsureRoleSwitchMembershipAsync(connection);
        await connection.ExecuteAsync($"SET ROLE {ApplicationRole};");
        try
        {
            await action(connection);
        }
        finally
        {
            await connection.ExecuteAsync("RESET ROLE;");
        }
    }

    internal static async Task<T> RunAsLifecycleExecutorAsync<T>(
        NpgsqlConnection connection,
        Func<NpgsqlConnection, Task<T>> action)
    {
        await EnsureRoleSwitchMembershipAsync(connection);
        await connection.ExecuteAsync($"SET ROLE {LifecycleExecutorRole};");
        try
        {
            return await action(connection);
        }
        finally
        {
            await connection.ExecuteAsync("RESET ROLE;");
        }
    }

    internal static async Task<T> RunAsApplicationRoleAsync<T>(
        NpgsqlConnection connection,
        Func<NpgsqlConnection, Task<T>> action)
    {
        await EnsureRoleSwitchMembershipAsync(connection);
        await connection.ExecuteAsync($"SET ROLE {ApplicationRole};");
        try
        {
            return await action(connection);
        }
        finally
        {
            await connection.ExecuteAsync("RESET ROLE;");
        }
    }

    internal static Task ExecuteDisposeAsync(
        NpgsqlConnection connection,
        Guid organizationId,
        Guid evaluationId,
        Guid providerArtifactId,
        Guid auditEventId,
        Guid delegationId,
        string reasonCode,
        Guid actorId) =>
        connection.ExecuteAsync(
            """
            SELECT dispose_evaluation_provider_artifact(
                @OrganizationId,
                @EvaluationId,
                @ProviderArtifactId,
                @AuditEventId,
                @DelegationId,
                @ReasonCode,
                @ActorId);
            """,
            new
            {
                OrganizationId = organizationId,
                EvaluationId = evaluationId,
                ProviderArtifactId = providerArtifactId,
                AuditEventId = auditEventId,
                DelegationId = delegationId,
                ReasonCode = reasonCode,
                ActorId = actorId,
            });

    internal static async Task<Guid> InsertLifecycleDisposeDelegationAsync(
        NpgsqlConnection connection,
        EvaluationDurableWorkItem claimed,
        Guid lifecycleActorId,
        CancellationToken cancellationToken)
    {
        var delegationId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO service_delegations (
                    delegation_id, organization_id, activity_id, participant_id, attempt_id,
                    session_id, service_actor_id, allowed_action, system_purpose,
                    initiating_authority, effective_at, expires_at, revoked_at, delegation_version)
                VALUES (
                    @DelegationId, @OrganizationId, @ActivityId, @ParticipantId, @AttemptId,
                    @SessionId, @LifecycleActorId, @AllowedAction, 'evaluation.lifecycle.disposition',
                    'synthetic.integration', clock_timestamp() - interval '1 minute',
                    clock_timestamp() + interval '1 hour', NULL, 1);
                """,
                new
                {
                    DelegationId = delegationId,
                    claimed.Ownership.OrganizationId,
                    claimed.Ownership.ActivityId,
                    claimed.Ownership.ParticipantId,
                    claimed.Ownership.AttemptId,
                    claimed.Ownership.SessionId,
                    LifecycleActorId = lifecycleActorId,
                    AllowedAction = DisposeDelegationAction,
                },
                cancellationToken: cancellationToken));
        return delegationId;
    }
}
