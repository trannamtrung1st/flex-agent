using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresEvaluationSessionEvidenceSource(
    PostgresConnectionAccessor connectionAccessor,
    IEvaluationHandoffSource handoffSource) : IEvaluationSessionEvidenceSource
{
    public async Task<EvaluationSessionEvidenceBundle?> LoadAsync(
        Guid organizationId,
        Guid sessionId,
        string handoffId,
        CancellationToken cancellationToken)
    {
        var handoff = await handoffSource.GetCompletedHandoffAsync(
            organizationId,
            sessionId,
            handoffId,
            cancellationToken);
        if (handoff is null)
        {
            return null;
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<TranscriptRow>(
            new CommandDefinition(
                """
                SELECT
                    transcript.message_id,
                    transcript.protected_ref,
                    transcript.content_digest,
                    transcript.exact_utf8_text,
                    transcript.author_type,
                    COALESCE(message.sealed_session_sequence, 0) AS published_sequence
                FROM session_visible_transcript_items AS transcript
                LEFT JOIN session_messages AS message
                  ON message.organization_id = transcript.organization_id
                 AND message.session_id = transcript.session_id
                 AND message.message_id = transcript.message_id
                WHERE transcript.organization_id = @OrganizationId
                  AND transcript.activity_id = @ActivityId
                  AND transcript.participant_id = @ParticipantId
                  AND transcript.attempt_id = @AttemptId
                  AND transcript.session_id = @SessionId
                  AND transcript.exact_utf8_text IS NOT NULL
                  AND (
                        transcript.author_type = 'participant'
                        OR (
                            message.sealed_session_sequence IS NOT NULL
                            AND message.sealed_session_sequence <= @CutoffSequence
                            AND message.completion_state = 'complete'
                        )
                      )
                ORDER BY transcript.committed_at, transcript.message_id;
                """,
                new
                {
                    handoff.Ownership.OrganizationId,
                    handoff.Ownership.ActivityId,
                    handoff.Ownership.ParticipantId,
                    handoff.Ownership.AttemptId,
                    handoff.Ownership.SessionId,
                    CutoffSequence = handoff.CutoffSequence,
                },
                cancellationToken: cancellationToken))).AsList();

        var items = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.exact_utf8_text))
            .Select(row => new EvaluationSessionTranscriptMaterial(
                row.message_id,
                EvaluationEvidenceSourceIdentity.TranscriptSourceVersion(row.protected_ref),
                row.published_sequence,
                row.content_digest,
                System.Text.Encoding.UTF8.GetBytes(row.exact_utf8_text)))
            .ToArray();

        return new EvaluationSessionEvidenceBundle(handoff, items);
    }

    private sealed record TranscriptRow(
        string message_id,
        string protected_ref,
        string content_digest,
        string exact_utf8_text,
        string author_type,
        long published_sequence);
}
