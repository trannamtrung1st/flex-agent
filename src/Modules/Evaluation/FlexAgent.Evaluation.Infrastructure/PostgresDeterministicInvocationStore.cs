using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresDeterministicInvocationStore(
    PostgresConnectionAccessor connectionAccessor) : IDeterministicInvocationStore
{
    public async Task<EvaluationDecision<Guid>> TryAppendAsync(
        DeterministicInvocationAppendCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Ownership.OrganizationId == Guid.Empty
            || command.RequestId == Guid.Empty
            || command.InvocationAttemptId == Guid.Empty
            || command.DeterministicAttemptId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.CriterionId)
            || string.IsNullOrWhiteSpace(command.CriterionVersion)
            || !EvaluationIdentity.IsSha256Hex(command.CanonicalInputDigest))
        {
            return EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.InvalidField);
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var inserted = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO evaluation_deterministic_attempts (
                    organization_id, deterministic_attempt_id, request_id, invocation_attempt_id,
                    criterion_id, criterion_version, evaluator_id, evaluator_version, evaluator_digest,
                    canonical_input_digest, dependency_digest, configuration_digest, outcome,
                    protected_input_ref, protected_output_ref, failure_category, started_at, finished_at)
                SELECT
                    @OrganizationId, @DeterministicAttemptId, @RequestId, @InvocationAttemptId,
                    @CriterionId, @CriterionVersion, @EvaluatorId, @EvaluatorVersion, @EvaluatorDigest,
                    @CanonicalInputDigest, @DependencyDigest, @ConfigurationDigest, @Outcome,
                    @ProtectedInputRef, @ProtectedOutputRef, @FailureCategory, @StartedAt, @FinishedAt
                FROM evaluation_requests AS request
                INNER JOIN evaluation_invocation_attempts AS attempt
                  ON attempt.organization_id = request.organization_id
                 AND attempt.request_id = request.request_id
                 AND attempt.invocation_attempt_id = @InvocationAttemptId
                WHERE request.organization_id = @OrganizationId
                  AND request.request_id = @RequestId
                  AND request.activity_id = @ActivityId
                  AND request.participant_id = @ParticipantId
                  AND request.attempt_id = @AttemptId
                  AND request.session_id = @SessionId
                  AND NOT EXISTS (
                        SELECT 1
                        FROM evaluation_deterministic_attempts AS existing
                        WHERE existing.organization_id = @OrganizationId
                          AND existing.request_id = @RequestId
                          AND existing.deterministic_attempt_id = @DeterministicAttemptId);
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.DeterministicAttemptId,
                    command.RequestId,
                    command.InvocationAttemptId,
                    command.CriterionId,
                    command.CriterionVersion,
                    command.Binding.EvaluatorId,
                    command.Binding.EvaluatorVersion,
                    command.Binding.EvaluatorDigest,
                    command.CanonicalInputDigest,
                    command.Binding.DependencyDigest,
                    command.Binding.ConfigurationDigest,
                    Outcome = DeterministicInvocationOutcomes.ToPersistenceOutcome(command.Result.Outcome),
                    command.Result.ProtectedInputRef,
                    command.Result.ProtectedOutputRef,
                    command.Result.FailureCategory,
                    StartedAt = command.Result.StartedAt,
                    FinishedAt = command.Result.FinishedAt,
                    command.Ownership.ActivityId,
                    command.Ownership.ParticipantId,
                    command.Ownership.AttemptId,
                    command.Ownership.SessionId,
                },
                cancellationToken: cancellationToken));

        if (inserted == 1)
        {
            return EvaluationDecision<Guid>.Ok(command.DeterministicAttemptId);
        }

        var existing = await connection.QuerySingleOrDefaultAsync<PersistedDeterministicAttemptRow>(
            new CommandDefinition(
                """
                SELECT outcome, protected_output_ref, failure_category, canonical_input_digest
                FROM evaluation_deterministic_attempts
                WHERE organization_id = @OrganizationId
                  AND request_id = @RequestId
                  AND deterministic_attempt_id = @DeterministicAttemptId;
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.RequestId,
                    command.DeterministicAttemptId,
                },
                cancellationToken: cancellationToken));

        if (existing is null
            || !string.Equals(
                DeterministicInvocationOutcomes.ToPersistenceOutcome(command.Result.Outcome),
                existing.outcome,
                StringComparison.Ordinal)
            || !string.Equals(
                command.Result.ProtectedOutputRef,
                existing.protected_output_ref,
                StringComparison.Ordinal)
            || !string.Equals(
                command.Result.FailureCategory,
                existing.failure_category,
                StringComparison.Ordinal)
            || !string.Equals(
                command.CanonicalInputDigest,
                existing.canonical_input_digest,
                StringComparison.Ordinal))
        {
            return EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.DuplicateIdentity);
        }

        return EvaluationDecision<Guid>.Ok(command.DeterministicAttemptId);
    }

    private sealed record PersistedDeterministicAttemptRow(
        string outcome,
        string? protected_output_ref,
        string? failure_category,
        string canonical_input_digest);
}
