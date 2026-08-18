using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresSessionActorRelationshipStore(PostgresConnectionAccessor connectionAccessor)
    : ISessionActorRelationshipStore, ISessionEventSubjectSource
{
    private const string SetCurrentSql = """
        INSERT INTO session_actor_relationships (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            actor_id, actor_type, relationship, relationship_version)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @ActorId, @ActorType, @Relationship, @RelationshipVersion)
        ON CONFLICT (organization_id, session_id, actor_id) DO UPDATE
        SET actor_type = EXCLUDED.actor_type,
            relationship = EXCLUDED.relationship,
            activity_id = EXCLUDED.activity_id,
            participant_id = EXCLUDED.participant_id,
            attempt_id = EXCLUDED.attempt_id,
            relationship_version = EXCLUDED.relationship_version,
            revoked_at = NULL
        WHERE EXCLUDED.relationship_version > session_actor_relationships.relationship_version;
        """;

    private const string RevokeCurrentSql = """
        INSERT INTO session_actor_relationships (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            actor_id, actor_type, relationship, relationship_version, revoked_at)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @ActorId, @ActorType, @Relationship, @RelationshipVersion, clock_timestamp())
        ON CONFLICT (organization_id, session_id, actor_id) DO UPDATE
        SET actor_type = EXCLUDED.actor_type,
            relationship = EXCLUDED.relationship,
            activity_id = EXCLUDED.activity_id,
            participant_id = EXCLUDED.participant_id,
            attempt_id = EXCLUDED.attempt_id,
            relationship_version = EXCLUDED.relationship_version,
            revoked_at = COALESCE(
                session_actor_relationships.revoked_at,
                EXCLUDED.revoked_at)
        WHERE EXCLUDED.relationship_version > session_actor_relationships.relationship_version;
        """;

    private const string ResolveCurrentSql = """
        SELECT
            rel.actor_id,
            rel.actor_type,
            rel.organization_id,
            rel.participant_id,
            rel.relationship
        FROM session_actor_relationships AS rel
        INNER JOIN session_runtimes AS runtime
            ON runtime.organization_id = rel.organization_id
           AND runtime.activity_id = rel.activity_id
           AND runtime.participant_id = rel.participant_id
           AND runtime.attempt_id = rel.attempt_id
           AND runtime.session_id = rel.session_id
        WHERE rel.actor_id = @ActorId
          AND rel.session_id = @SessionId
          AND rel.revoked_at IS NULL;
        """;

    public async Task<bool> SetCurrentAsync(
        SessionActorRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        EnsureValidRelationship(relationship);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                SetCurrentSql,
                RelationshipParameters(relationship),
                cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> RevokeCurrentAsync(
        SessionActorRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        EnsureValidRelationship(relationship);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                RevokeCurrentSql,
                RelationshipParameters(relationship),
                cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<SessionEventSubject?> ResolveCurrentAsync(
        TrustedRuntimeActor actor,
        Guid untrustedSessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.ActorId == Guid.Empty
            || string.IsNullOrWhiteSpace(actor.ActorType)
            || untrustedSessionId == Guid.Empty)
        {
            return null;
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<SubjectRow>(
            new CommandDefinition(
                ResolveCurrentSql,
                new { ActorId = actor.ActorId, SessionId = untrustedSessionId },
                cancellationToken: cancellationToken))).AsList();
        if (rows.Count != 1)
        {
            return null;
        }

        var row = rows[0];
        if (row.actor_id != actor.ActorId
            || !string.Equals(row.actor_type, actor.ActorType, StringComparison.Ordinal)
            || row.organization_id == Guid.Empty
            || string.IsNullOrWhiteSpace(row.relationship))
        {
            return null;
        }

        return new SessionEventSubject(
            row.actor_id,
            row.actor_type,
            row.organization_id,
            row.participant_id == Guid.Empty ? null : row.participant_id,
            row.relationship);
    }

    private static void EnsureValidRelationship(SessionActorRelationship relationship)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        if (relationship.ActorId == Guid.Empty
            || string.IsNullOrWhiteSpace(relationship.ActorType)
            || relationship.Ownership.OrganizationId == Guid.Empty
            || relationship.Ownership.SessionId == Guid.Empty
            || relationship.RelationshipVersion < 1
            || relationship.Relationship is not (
                SessionEventSubscriptionRelationships.Participant
                or SessionEventSubscriptionRelationships.Reviewer
                or SessionEventSubscriptionRelationships.Administrator))
        {
            throw new ArgumentOutOfRangeException(nameof(relationship));
        }
    }

    private static object RelationshipParameters(SessionActorRelationship relationship) =>
        new
        {
            relationship.Ownership.OrganizationId,
            relationship.Ownership.ActivityId,
            relationship.Ownership.ParticipantId,
            relationship.Ownership.AttemptId,
            relationship.Ownership.SessionId,
            relationship.ActorId,
            relationship.ActorType,
            relationship.Relationship,
            relationship.RelationshipVersion,
        };

    private sealed record SubjectRow(
        Guid actor_id,
        string actor_type,
        Guid organization_id,
        Guid participant_id,
        string relationship);
}
