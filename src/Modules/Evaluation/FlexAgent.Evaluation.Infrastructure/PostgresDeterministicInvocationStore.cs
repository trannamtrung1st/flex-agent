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
                    protected_input_ref, protected_output_ref, output_content_digest, failure_category,
                    started_at, finished_at)
                SELECT
                    @OrganizationId, @DeterministicAttemptId, @RequestId, @InvocationAttemptId,
                    @CriterionId, @CriterionVersion, @EvaluatorId, @EvaluatorVersion, @EvaluatorDigest,
                    @CanonicalInputDigest, @DependencyDigest, @ConfigurationDigest, @Outcome,
                    @ProtectedInputRef, @ProtectedOutputRef, @OutputContentDigest, @FailureCategory,
                    @StartedAt, @FinishedAt
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
                    command.Result.OutputContentDigest,
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
                SELECT
                    invocation_attempt_id,
                    criterion_id,
                    criterion_version,
                    evaluator_id,
                    evaluator_version,
                    evaluator_digest,
                    configuration_digest,
                    dependency_digest,
                    canonical_input_digest,
                    protected_input_ref,
                    protected_output_ref,
                    output_content_digest,
                    outcome,
                    failure_category
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

        if (existing is null)
        {
            return EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.InvalidField);
        }

        var candidate = DeterministicInvocationProvenance.FromAppendCommand(command);
        var persisted = new DeterministicInvocationProvenanceSnapshot(
            existing.invocation_attempt_id,
            existing.criterion_id,
            existing.criterion_version,
            existing.evaluator_id,
            existing.evaluator_version,
            existing.evaluator_digest,
            existing.configuration_digest,
            existing.dependency_digest,
            existing.canonical_input_digest,
            existing.protected_input_ref,
            existing.protected_output_ref,
            existing.output_content_digest,
            existing.outcome,
            existing.failure_category);

        if (!DeterministicInvocationProvenance.IsEquivalentRetry(persisted, candidate))
        {
            return EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.DeterministicConflict);
        }

        return EvaluationDecision<Guid>.Ok(command.DeterministicAttemptId);
    }

    private sealed record PersistedDeterministicAttemptRow(
        Guid invocation_attempt_id,
        string criterion_id,
        string criterion_version,
        string evaluator_id,
        string evaluator_version,
        string evaluator_digest,
        string configuration_digest,
        string dependency_digest,
        string canonical_input_digest,
        string? protected_input_ref,
        string? protected_output_ref,
        string? output_content_digest,
        string outcome,
        string? failure_category);
}
