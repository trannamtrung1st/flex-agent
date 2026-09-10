using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresEvaluationRequestAuthorityStore(
    PostgresConnectionAccessor connectionAccessor) : IEvaluationRequestAuthorityStore
{
    public async Task<AdmittedEvaluationRequestAuthority?> TryLoadAsync(
        EvaluationOwnership ownership,
        Guid requestId,
        Guid invocationAttemptId,
        CancellationToken cancellationToken)
    {
        if (ownership.OrganizationId == Guid.Empty
            || requestId == Guid.Empty
            || invocationAttemptId == Guid.Empty)
        {
            return null;
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AuthorityRow>(
            new CommandDefinition(
                """
                SELECT
                    request.request_id,
                    attempt.invocation_attempt_id,
                    request.activity_id,
                    request.participant_id,
                    request.attempt_id,
                    request.session_id,
                    request.frozen_input_digest,
                    request.rubric_source_id,
                    request.rubric_source_version_id,
                    request.rubric_content_digest,
                    request.evaluator_registry_version
                FROM evaluation_requests AS request
                INNER JOIN evaluation_invocation_attempts AS attempt
                  ON attempt.organization_id = request.organization_id
                 AND attempt.request_id = request.request_id
                 AND attempt.invocation_attempt_id = @InvocationAttemptId
                 AND attempt.activity_id = request.activity_id
                 AND attempt.participant_id = request.participant_id
                 AND attempt.attempt_id = request.attempt_id
                 AND attempt.session_id = request.session_id
                WHERE request.organization_id = @OrganizationId
                  AND request.request_id = @RequestId
                  AND request.activity_id = @ActivityId
                  AND request.participant_id = @ParticipantId
                  AND request.attempt_id = @AttemptId
                  AND request.session_id = @SessionId;
                """,
                new
                {
                    ownership.OrganizationId,
                    RequestId = requestId,
                    InvocationAttemptId = invocationAttemptId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                },
                cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var procedureRef = ExactSourceIdentity.TryCreate(
            "rubric_evaluation",
            row.rubric_source_id,
            row.rubric_source_version_id,
            row.rubric_content_digest);
        if (!procedureRef.Succeeded || procedureRef.Value is null)
        {
            return null;
        }

        var scopedOwnership = EvaluationOwnership.TryCreate(
            ownership.OrganizationId,
            row.activity_id,
            row.participant_id,
            row.attempt_id,
            row.session_id);
        if (!scopedOwnership.Succeeded || scopedOwnership.Value is null)
        {
            return null;
        }

        return new AdmittedEvaluationRequestAuthority(
            row.request_id,
            row.invocation_attempt_id,
            scopedOwnership.Value,
            row.frozen_input_digest,
            procedureRef.Value,
            row.evaluator_registry_version);
    }

    private sealed record AuthorityRow(
        Guid request_id,
        Guid invocation_attempt_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        string frozen_input_digest,
        Guid rubric_source_id,
        Guid rubric_source_version_id,
        string rubric_content_digest,
        string evaluator_registry_version);
}
