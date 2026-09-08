using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Infrastructure;

public sealed class PostgresEvaluationSubmissionEvidenceSource(
    PostgresConnectionAccessor connectionAccessor,
    IArtifactStore artifacts) : IEvaluationSubmissionEvidenceSource
{
    public async Task<EvaluationSubmissionEvidenceBundle?> LoadBoundItemsAsync(
        Guid organizationId,
        Guid activityId,
        Guid participantId,
        Guid attemptId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var attempt = await connection.QuerySingleOrDefaultAsync<AttemptRow>(
            new CommandDefinition(
                """
                SELECT status, session_id
                FROM submissions_attempts
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND participant_actor_id = @ParticipantId
                  AND attempt_id = @AttemptId
                  AND session_id = @SessionId;
                """,
                new
                {
                    OrganizationId = organizationId,
                    ActivityId = activityId,
                    ParticipantId = participantId,
                    AttemptId = attemptId,
                    SessionId = sessionId,
                },
                cancellationToken: cancellationToken));
        if (attempt is null || !string.Equals(attempt.status, AttemptStates.Completed, StringComparison.Ordinal))
        {
            return null;
        }

        var rows = (await connection.QueryAsync<ItemRow>(
            new CommandDefinition(
                """
                SELECT
                    binding.version_id,
                    binding.version_number,
                    item.item_id,
                    item.category,
                    item.content_digest,
                    item.byte_count,
                    item.artifact_object_key,
                    item.artifact_version_id
                FROM submissions_attempt_submission_bindings AS binding
                INNER JOIN submissions_accepted_version_items AS item
                  ON item.organization_id = binding.organization_id
                 AND item.version_id = binding.version_id
                WHERE binding.organization_id = @OrganizationId
                  AND binding.attempt_id = @AttemptId
                ORDER BY binding.binding_order, item.item_id;
                """,
                new { OrganizationId = organizationId, AttemptId = attemptId },
                cancellationToken: cancellationToken))).AsList();
        if (rows.Count == 0)
        {
            return null;
        }

        var items = new List<EvaluationSubmissionMaterial>(rows.Count);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.artifact_object_key)
                || string.IsNullOrWhiteSpace(row.artifact_version_id))
            {
                return null;
            }

            var artifact = await artifacts.GetExactVersionAsync(
                new ArtifactGetRequest(
                    organizationId,
                    new StoredArtifactReference(
                        new ArtifactObjectKey(row.artifact_object_key),
                        new ArtifactVersionId(row.artifact_version_id),
                        ArtifactDigest.FromHex(row.content_digest),
                        row.byte_count)),
                cancellationToken);
            if (!artifact.Succeeded)
            {
                return null;
            }

            items.Add(new EvaluationSubmissionMaterial(
                EvaluationEvidenceSourceIdentity.SubmissionItemSourceId(row.item_id),
                EvaluationEvidenceSourceIdentity.SubmissionVersionSourceVersion(row.version_number),
                row.category,
                row.content_digest,
                artifact.Content,
                row.version_id,
                row.item_id));
        }

        return new EvaluationSubmissionEvidenceBundle(items);
    }

    private sealed record AttemptRow(string status, Guid session_id);

    private sealed record ItemRow(
        Guid version_id,
        int version_number,
        Guid item_id,
        string category,
        string content_digest,
        long byte_count,
        string? artifact_object_key,
        string? artifact_version_id);
}
