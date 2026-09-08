using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresEvaluationHandoffSource(PostgresConnectionAccessor connectionAccessor)
    : IEvaluationHandoffSource
{
    public Task<EvaluationHandoffSnapshot?> GetCompletedHandoffAsync(
        Guid organizationId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        GetCompletedHandoffCoreAsync(
            organizationId,
            sessionId,
            handoffId: null,
            cancellationToken);

    public async Task<EvaluationHandoffSnapshot?> GetCompletedHandoffAsync(
        Guid organizationId,
        Guid sessionId,
        string handoffId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty
            || sessionId == Guid.Empty
            || !EvaluationIdentity.IsStableId(handoffId))
        {
            return null;
        }

        return await GetCompletedHandoffCoreAsync(
            organizationId,
            sessionId,
            handoffId,
            cancellationToken);
    }

    private async Task<EvaluationHandoffSnapshot?> GetCompletedHandoffCoreAsync(
        Guid organizationId,
        Guid sessionId,
        string? handoffId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || sessionId == Guid.Empty)
        {
            return null;
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<HandoffRow>(
            new CommandDefinition(
                """
                SELECT
                    handoff.handoff_id,
                    handoff.organization_id,
                    handoff.activity_id,
                    handoff.participant_id,
                    handoff.attempt_id,
                    handoff.session_id,
                    handoff.terminal_state,
                    handoff.terminal_record_id,
                    handoff.cutoff_sequence,
                    handoff.procedure_id,
                    handoff.seal_digest,
                    handoff.configuration_id,
                    handoff.configuration_digest,
                    handoff.manifest_id,
                    attempt.manifest_digest,
                    handoff.committed_at
                FROM session_evaluation_handoffs AS handoff
                INNER JOIN submissions_attempts AS attempt
                  ON attempt.organization_id = handoff.organization_id
                 AND attempt.activity_id = handoff.activity_id
                 AND attempt.participant_actor_id = handoff.participant_id
                 AND attempt.attempt_id = handoff.attempt_id
                 AND attempt.session_id = handoff.session_id
                 AND attempt.status = 'completed'
                 AND attempt.resolved_configuration_id::text = handoff.configuration_id
                 AND attempt.configuration_digest = handoff.configuration_digest
                 AND attempt.initial_manifest_id::text = handoff.manifest_id
                WHERE handoff.organization_id = @OrganizationId
                  AND handoff.session_id = @SessionId
                  AND (CAST(@HandoffId AS text) IS NULL OR handoff.handoff_id = @HandoffId)
                  AND handoff.eligibility = 'eligible'
                  AND handoff.terminal_state = 'completed';
                """,
                new
                {
                    OrganizationId = organizationId,
                    SessionId = sessionId,
                    HandoffId = handoffId,
                },
                cancellationToken: cancellationToken));
        if (row is null
            || row.cutoff_sequence is null
            || !Guid.TryParse(row.configuration_id, out _)
            || !Guid.TryParse(row.manifest_id, out _))
        {
            return null;
        }

        return Map(row);
    }

    public async Task<EvaluationHandoffScanPage> ScanCompletedHandoffsAsync(
        EvaluationHandoffCursor? after,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<HandoffRow>(
            new CommandDefinition(
                """
                SELECT
                    handoff.handoff_id,
                    handoff.organization_id,
                    handoff.activity_id,
                    handoff.participant_id,
                    handoff.attempt_id,
                    handoff.session_id,
                    handoff.terminal_state,
                    handoff.terminal_record_id,
                    handoff.cutoff_sequence,
                    handoff.procedure_id,
                    handoff.seal_digest,
                    handoff.configuration_id,
                    handoff.configuration_digest,
                    handoff.manifest_id,
                    attempt.manifest_digest,
                    handoff.committed_at
                FROM session_evaluation_handoffs AS handoff
                INNER JOIN submissions_attempts AS attempt
                  ON attempt.organization_id = handoff.organization_id
                 AND attempt.activity_id = handoff.activity_id
                 AND attempt.participant_actor_id = handoff.participant_id
                 AND attempt.attempt_id = handoff.attempt_id
                 AND attempt.session_id = handoff.session_id
                 AND attempt.status = 'completed'
                 AND attempt.resolved_configuration_id::text = handoff.configuration_id
                 AND attempt.configuration_digest = handoff.configuration_digest
                 AND attempt.initial_manifest_id::text = handoff.manifest_id
                WHERE handoff.eligibility = 'eligible'
                  AND handoff.terminal_state = 'completed'
                  AND (
                        @AfterCommittedAt IS NULL
                        OR (handoff.committed_at, handoff.organization_id, handoff.session_id, handoff.handoff_id)
                            > (@AfterCommittedAt, @AfterOrganizationId, @AfterSessionId, @AfterHandoffId)
                      )
                ORDER BY handoff.committed_at, handoff.organization_id, handoff.session_id, handoff.handoff_id
                LIMIT @Limit;
                """,
                new
                {
                    AfterCommittedAt = after?.CommittedAt,
                    AfterOrganizationId = after?.OrganizationId,
                    AfterSessionId = after?.SessionId,
                    AfterHandoffId = after?.HandoffId,
                    Limit = limit,
                },
                cancellationToken: cancellationToken))).AsList();
        var validRows = rows
            .Where(row =>
                row.cutoff_sequence is not null
                && Guid.TryParse(row.configuration_id, out _)
                && Guid.TryParse(row.manifest_id, out _))
            .ToArray();
        var items = validRows.Select(Map).ToArray();
        var last = rows.LastOrDefault();
        return new EvaluationHandoffScanPage(
            items,
            last is null
                ? after
                : new EvaluationHandoffCursor(
                    last.committed_at,
                    last.organization_id,
                    last.session_id,
                    last.handoff_id));
    }

    private static EvaluationHandoffSnapshot Map(HandoffRow row)
    {
        var configurationId = Guid.Parse(row.configuration_id);
        var manifestId = Guid.Parse(row.manifest_id);
        return new(
            row.handoff_id,
            new EvaluationOwnership(
                row.organization_id,
                row.activity_id,
                row.participant_id,
                row.attempt_id,
                row.session_id),
            row.terminal_state,
            row.terminal_record_id,
            row.cutoff_sequence.GetValueOrDefault(),
            row.procedure_id,
            row.seal_digest,
            configurationId,
            row.configuration_digest,
            manifestId,
            row.manifest_digest);
    }

    private sealed record HandoffRow(
        string handoff_id,
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        string terminal_state,
        Guid terminal_record_id,
        long? cutoff_sequence,
        string procedure_id,
        string seal_digest,
        string configuration_id,
        string configuration_digest,
        string manifest_id,
        string manifest_digest,
        DateTimeOffset committed_at);
}
