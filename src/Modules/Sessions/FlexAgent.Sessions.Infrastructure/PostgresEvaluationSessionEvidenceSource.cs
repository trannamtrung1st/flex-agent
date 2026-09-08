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
                    CASE transcript.author_type
                        WHEN 'participant' THEN participant_invocation.admitted_session_sequence
                        ELSE message.sealed_session_sequence
                    END AS published_sequence
                FROM session_visible_transcript_items AS transcript
                INNER JOIN session_runtimes AS runtime
                  ON runtime.organization_id = transcript.organization_id
                 AND runtime.activity_id = transcript.activity_id
                 AND runtime.participant_id = transcript.participant_id
                 AND runtime.attempt_id = transcript.attempt_id
                 AND runtime.session_id = transcript.session_id
                INNER JOIN session_evaluation_handoffs AS handoff
                  ON handoff.organization_id = transcript.organization_id
                 AND handoff.activity_id = transcript.activity_id
                 AND handoff.participant_id = transcript.participant_id
                 AND handoff.attempt_id = transcript.attempt_id
                 AND handoff.session_id = transcript.session_id
                 AND handoff.handoff_id = @HandoffId
                 AND handoff.eligibility = 'eligible'
                 AND handoff.terminal_state = 'completed'
                 AND handoff.cutoff_sequence = runtime.cutoff_sequence
                LEFT JOIN session_turns AS turn
                  ON turn.organization_id = transcript.organization_id
                 AND turn.activity_id = transcript.activity_id
                 AND turn.participant_id = transcript.participant_id
                 AND turn.attempt_id = transcript.attempt_id
                 AND turn.session_id = transcript.session_id
                 AND turn.turn_id = transcript.turn_id
                LEFT JOIN session_invocations AS participant_invocation
                  ON participant_invocation.organization_id = turn.organization_id
                 AND participant_invocation.session_id = turn.session_id
                 AND participant_invocation.agent_invocation_id = turn.trigger_invocation_id
                LEFT JOIN session_messages AS message
                  ON message.organization_id = transcript.organization_id
                 AND message.activity_id = transcript.activity_id
                 AND message.participant_id = transcript.participant_id
                 AND message.attempt_id = transcript.attempt_id
                 AND message.session_id = transcript.session_id
                 AND message.message_id = transcript.message_id
                WHERE transcript.organization_id = @OrganizationId
                  AND transcript.activity_id = @ActivityId
                  AND transcript.participant_id = @ParticipantId
                  AND transcript.attempt_id = @AttemptId
                  AND transcript.session_id = @SessionId
                  AND transcript.exact_utf8_text IS NOT NULL
                  AND runtime.cutoff_sequence = @CutoffSequence
                  AND (
                        (
                            transcript.author_type = 'participant'
                            AND participant_invocation.admitted_session_sequence IS NOT NULL
                            AND participant_invocation.admitted_session_sequence <= @CutoffSequence
                            AND participant_invocation.status IN (
                                'admitted',
                                'executing',
                                'decision_recorded',
                                'decided',
                                'execution_failed')
                        )
                        OR (
                            transcript.author_type = 'agent'
                            AND message.sealed_session_sequence IS NOT NULL
                            AND message.sealed_session_sequence <= @CutoffSequence
                            AND message.completion_state = 'complete'
                        )
                      )
                ORDER BY published_sequence, transcript.message_id;
                """,
                new
                {
                    handoff.Ownership.OrganizationId,
                    handoff.Ownership.ActivityId,
                    handoff.Ownership.ParticipantId,
                    handoff.Ownership.AttemptId,
                    handoff.Ownership.SessionId,
                    HandoffId = handoffId,
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

        var configurationFact = await LoadConfigurationFactAsync(
            connection,
            handoff,
            cancellationToken);
        if (configurationFact is null)
        {
            return null;
        }

        var manifestFact = await LoadManifestFactAsync(
            connection,
            handoff,
            cancellationToken);
        if (manifestFact is null)
        {
            return null;
        }

        return new EvaluationSessionEvidenceBundle(handoff, items, configurationFact, manifestFact);
    }

    private static async Task<EvaluationSafeFactProjection?> LoadConfigurationFactAsync(
        Npgsql.NpgsqlConnection connection,
        EvaluationHandoffSnapshot handoff,
        CancellationToken cancellationToken)
    {
        var row = await connection.QuerySingleOrDefaultAsync<ConfigurationJsonRow>(
            new CommandDefinition(
                """
                SELECT configuration_digest, canonical_json
                FROM session_resolved_configurations
                WHERE organization_id = @OrganizationId
                  AND configuration_id = @ConfigurationId
                  AND configuration_digest = @ConfigurationDigest;
                """,
                new
                {
                    handoff.Ownership.OrganizationId,
                    ConfigurationId = handoff.ConfigurationId,
                    ConfigurationDigest = handoff.ConfigurationDigest,
                },
                cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        var projection = EvaluationSafeFactProjector.TryBuildConfigurationFact(
            handoff.ConfigurationId,
            row.configuration_digest,
            System.Text.Encoding.UTF8.GetBytes(row.canonical_json));
        return projection.Succeeded ? projection.Value : null;
    }

    private static async Task<EvaluationSafeFactProjection?> LoadManifestFactAsync(
        Npgsql.NpgsqlConnection connection,
        EvaluationHandoffSnapshot handoff,
        CancellationToken cancellationToken)
    {
        var row = await connection.QuerySingleOrDefaultAsync<ManifestJsonRow>(
            new CommandDefinition(
                """
                SELECT manifest_digest, canonical_json
                FROM session_initial_manifests
                WHERE organization_id = @OrganizationId
                  AND manifest_id = @ManifestId
                  AND configuration_id = @ConfigurationId
                  AND manifest_digest = @ManifestDigest;
                """,
                new
                {
                    handoff.Ownership.OrganizationId,
                    ManifestId = handoff.ManifestId,
                    ConfigurationId = handoff.ConfigurationId,
                    ManifestDigest = handoff.ManifestDigest,
                },
                cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        var projection = EvaluationSafeFactProjector.TryBuildManifestFact(
            handoff.ManifestId,
            row.manifest_digest,
            System.Text.Encoding.UTF8.GetBytes(row.canonical_json));
        return projection.Succeeded ? projection.Value : null;
    }

    private sealed record ConfigurationJsonRow(string configuration_digest, string canonical_json);

    private sealed record ManifestJsonRow(string manifest_digest, string canonical_json);

    private sealed record TranscriptRow(
        string message_id,
        string protected_ref,
        string content_digest,
        string exact_utf8_text,
        string author_type,
        long published_sequence);
}
