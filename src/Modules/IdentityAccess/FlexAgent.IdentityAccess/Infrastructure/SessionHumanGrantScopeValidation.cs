using Dapper;
using FlexAgent.IdentityAccess.Domain;
using Npgsql;

namespace FlexAgent.IdentityAccess.Infrastructure;

internal static class SessionHumanGrantScopeValidation
{
    private const string LoadSessionOwnershipSql = """
        SELECT activity_id, participant_id, attempt_id
        FROM session_runtimes
        WHERE organization_id = @OrganizationId
          AND session_id = @SessionId;
        """;

    private const string ActivityStewardshipSql = """
        SELECT 1
        FROM session_runtimes AS runtime
        INNER JOIN assessment_activities AS activity
            ON activity.organization_id = runtime.organization_id
           AND activity.activity_id = runtime.activity_id
        INNER JOIN assessment_activity_revisions AS revision
            ON revision.organization_id = activity.organization_id
           AND revision.activity_id = activity.activity_id
           AND revision.revision_id = activity.current_revision_id
        WHERE runtime.organization_id = @OrganizationId
          AND runtime.session_id = @SessionId
          AND revision.actor_id = @ActorId;
        """;

    private const string SessionAdministratorRelationshipSql = """
        SELECT 1
        FROM session_actor_relationships AS rel
        INNER JOIN session_runtimes AS runtime
            ON runtime.organization_id = rel.organization_id
           AND runtime.activity_id = rel.activity_id
           AND runtime.participant_id = rel.participant_id
           AND runtime.attempt_id = rel.attempt_id
           AND runtime.session_id = rel.session_id
        WHERE rel.organization_id = @OrganizationId
          AND rel.session_id = @SessionId
          AND rel.actor_id = @ActorId
          AND rel.relationship = 'administrator'
          AND rel.revoked_at IS NULL;
        """;

    public static bool IsSessionAdministrativeAction(string action) =>
        action switch
        {
            AuthorizationActions.PauseSession => true,
            AuthorizationActions.ResumeSession => true,
            AuthorizationActions.TerminateSession => true,
            AuthorizationActions.ReadSessionOperations => true,
            AuthorizationActions.ReadSessionTranscript => true,
            _ => false,
        };

    public static async Task<AuthorizationDecision?> ValidateAsync(
        AuthorizationRequest request,
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(
                request.Resource.ResourceType,
                AuthorizationResourceTypes.Session,
                StringComparison.Ordinal))
        {
            return null;
        }

        var ownership = await connection.QuerySingleOrDefaultAsync<SessionOwnershipRow>(
            new CommandDefinition(
                LoadSessionOwnershipSql,
                new
                {
                    OrganizationId = request.Organization.OrganizationId,
                    SessionId = request.Resource.ResourceId,
                },
                transaction,
                cancellationToken: cancellationToken));
        if (ownership is null)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.ParentNotFound);
        }

        if (request.ActivityId is { } activityId && activityId != ownership.activity_id)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.ScopeMismatch);
        }

        if (request.ParticipantId is { } participantId && participantId != ownership.participant_id)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.ScopeMismatch);
        }

        if (request.AttemptId is { } attemptId && attemptId != ownership.attempt_id)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.ScopeMismatch);
        }

        if (!IsSessionAdministrativeAction(request.Action))
        {
            return null;
        }

        var parameters = new
        {
            OrganizationId = request.Organization.OrganizationId,
            SessionId = request.Resource.ResourceId,
            ActorId = request.Actor!.ActorId,
        };

        var steward = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                ActivityStewardshipSql,
                parameters,
                transaction,
                cancellationToken: cancellationToken));
        if (steward is not null)
        {
            return null;
        }

        var delegated = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                SessionAdministratorRelationshipSql,
                parameters,
                transaction,
                cancellationToken: cancellationToken));
        return delegated is null
            ? AuthorizationDecision.Deny(AuthorizationReasonCodes.ScopeMismatch)
            : null;
    }

    private sealed record SessionOwnershipRow(Guid activity_id, Guid participant_id, Guid attempt_id);
}
