using Dapper;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;
using Npgsql;

namespace FlexAgent.Evaluation.Infrastructure.Review;

internal sealed record ReviewHandoffWriteCommand(
    Guid HandoffId,
    Guid EvaluationId,
    EvaluationOwnership Ownership,
    Guid RequestId,
    bool RecordInitialCandidate,
    string? InitialCandidateReason,
    bool PublishReplacementAvailable,
    string? ReplacementReason,
    Guid ActorId,
    string ActorType,
    DateTimeOffset OccurredAtUtc);

internal static class PostgresReviewEvaluationHandoffWriter
{
    public static async Task<bool> TryWriteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ReviewHandoffWriteCommand command,
        CancellationToken cancellationToken)
    {
        var reviewCaseId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                """
                SELECT review_case_id
                FROM review_cases
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND participant_id = @ParticipantId
                  AND attempt_id = @AttemptId
                  AND session_id = @SessionId
                FOR UPDATE;
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.Ownership.ActivityId,
                    command.Ownership.ParticipantId,
                    command.Ownership.AttemptId,
                    command.Ownership.SessionId,
                },
                transaction,
                cancellationToken: cancellationToken));

        if (reviewCaseId is null)
        {
            reviewCaseId = Guid.CreateVersion7();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO review_cases (
                        organization_id, review_case_id, activity_id, participant_id,
                        attempt_id, session_id, case_state, candidate_state,
                        current_candidate_evaluation_id, created_at, updated_at)
                    VALUES (
                        @OrganizationId, @ReviewCaseId, @ActivityId, @ParticipantId,
                        @AttemptId, @SessionId, 'evaluation_available', 'none',
                        NULL, @OccurredAt, @OccurredAt);
                    """,
                    new
                    {
                        command.Ownership.OrganizationId,
                        ReviewCaseId = reviewCaseId.Value,
                        command.Ownership.ActivityId,
                        command.Ownership.ParticipantId,
                        command.Ownership.AttemptId,
                        command.Ownership.SessionId,
                        OccurredAt = command.OccurredAtUtc,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO evaluation_review_handoffs (
                    organization_id, handoff_id, evaluation_id, request_id, activity_id,
                    participant_id, attempt_id, session_id, candidate_eligible, created_at)
                VALUES (
                    @OrganizationId, @HandoffId, @EvaluationId, @RequestId, @ActivityId,
                    @ParticipantId, @AttemptId, @SessionId, TRUE, @OccurredAt)
                ON CONFLICT (organization_id, evaluation_id) DO NOTHING;
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.HandoffId,
                    command.EvaluationId,
                    command.RequestId,
                    command.Ownership.ActivityId,
                    command.Ownership.ParticipantId,
                    command.Ownership.AttemptId,
                    command.Ownership.SessionId,
                    OccurredAt = command.OccurredAtUtc,
                },
                transaction,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO review_case_events (
                    organization_id, event_id, review_case_id, event_kind, evaluation_id,
                    reason, actor_type, actor_id, occurred_at)
                VALUES (
                    @OrganizationId, @EventId, @ReviewCaseId, 'evaluation_available',
                    @EvaluationId, NULL, @ActorType, @ActorId, @OccurredAt);
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    EventId = Guid.CreateVersion7(),
                    ReviewCaseId = reviewCaseId.Value,
                    command.EvaluationId,
                    command.ActorType,
                    command.ActorId,
                    OccurredAt = command.OccurredAtUtc,
                },
                transaction,
                cancellationToken: cancellationToken));

        if (command.RecordInitialCandidate)
        {
            var candidateUpdated = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE review_cases
                    SET
                        candidate_state = 'selected',
                        current_candidate_evaluation_id = @EvaluationId,
                        updated_at = @OccurredAt
                    WHERE organization_id = @OrganizationId
                      AND review_case_id = @ReviewCaseId
                      AND current_candidate_evaluation_id IS NULL
                      AND candidate_state = 'none';
                    """,
                    new
                    {
                        command.Ownership.OrganizationId,
                        ReviewCaseId = reviewCaseId.Value,
                        command.EvaluationId,
                        OccurredAt = command.OccurredAtUtc,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            if (candidateUpdated == 1)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO review_case_events (
                            organization_id, event_id, review_case_id, event_kind, evaluation_id,
                            reason, actor_type, actor_id, occurred_at)
                        VALUES (
                            @OrganizationId, @EventId, @ReviewCaseId, 'initial_candidate_selected',
                            @EvaluationId, @Reason, @ActorType, @ActorId, @OccurredAt);
                        """,
                        new
                        {
                            command.Ownership.OrganizationId,
                            EventId = Guid.CreateVersion7(),
                            ReviewCaseId = reviewCaseId.Value,
                            command.EvaluationId,
                            Reason = command.InitialCandidateReason,
                            command.ActorType,
                            command.ActorId,
                            OccurredAt = command.OccurredAtUtc,
                        },
                        transaction,
                        cancellationToken: cancellationToken));
            }
        }

        if (command.PublishReplacementAvailable)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE review_cases
                    SET
                        case_state = 'candidate_stale',
                        candidate_state = 'replacement_available',
                        updated_at = @OccurredAt
                    WHERE organization_id = @OrganizationId
                      AND review_case_id = @ReviewCaseId
                      AND current_candidate_evaluation_id IS NOT NULL;
                    """,
                    new
                    {
                        command.Ownership.OrganizationId,
                        ReviewCaseId = reviewCaseId.Value,
                        OccurredAt = command.OccurredAtUtc,
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO review_case_events (
                        organization_id, event_id, review_case_id, event_kind, evaluation_id,
                        reason, actor_type, actor_id, occurred_at)
                    VALUES (
                        @OrganizationId, @EventId, @ReviewCaseId, 'replacement_available',
                        @EvaluationId, @Reason, @ActorType, @ActorId, @OccurredAt);
                    """,
                    new
                    {
                        command.Ownership.OrganizationId,
                        EventId = Guid.CreateVersion7(),
                        ReviewCaseId = reviewCaseId.Value,
                        command.EvaluationId,
                        Reason = command.ReplacementReason,
                        command.ActorType,
                        command.ActorId,
                        OccurredAt = command.OccurredAtUtc,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        return true;
    }
}
