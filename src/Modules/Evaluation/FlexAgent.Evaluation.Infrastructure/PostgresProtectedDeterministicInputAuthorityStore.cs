using System.Security.Cryptography;
using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresProtectedDeterministicInputAuthorityStore(
    PostgresConnectionAccessor connectionAccessor) : IProtectedDeterministicInputAuthorityStore
{
    public async Task<EvaluationDecision<bool>> TryEstablishAsync(
        ProtectedDeterministicInputAuthorityEstablishCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Ownership.OrganizationId == Guid.Empty
            || command.RequestId == Guid.Empty
            || command.InvocationAttemptId == Guid.Empty
            || command.DeterministicAttemptId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.CriterionId)
            || string.IsNullOrWhiteSpace(command.CriterionVersion)
            || !EvaluationIdentity.IsSha256Hex(command.CanonicalInputDigest)
            || string.IsNullOrWhiteSpace(command.ProtectedInputRef)
            || command.InputUtf8.IsEmpty
            || command.InputUtf8.Length > EvaluatorBindingValidator.MaxOutputLimitBytes)
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (!string.Equals(
                command.ProtectedInputRef,
                DeterministicInvocationProvenance.ProtectedInputRef(command.CanonicalInputDigest),
                StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.InvalidField, "protected_input_ref");
        }

        var computedDigest = Convert.ToHexString(SHA256.HashData(command.InputUtf8.Span)).ToLowerInvariant();
        if (!string.Equals(computedDigest, command.CanonicalInputDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.InvalidField, "canonical_input_digest");
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var inserted = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO evaluation_deterministic_input_authority (
                    organization_id, request_id, invocation_attempt_id,
                    criterion_id, criterion_version, canonical_input_digest,
                    protected_input_ref, input_utf8, established_by_attempt_id, created_at)
                SELECT
                    @OrganizationId, @RequestId, @InvocationAttemptId,
                    @CriterionId, @CriterionVersion, @CanonicalInputDigest,
                    @ProtectedInputRef, @InputUtf8, @DeterministicAttemptId, @CreatedAt
                FROM evaluation_deterministic_attempts AS attempt
                INNER JOIN evaluation_requests AS request
                  ON request.organization_id = attempt.organization_id
                 AND request.request_id = attempt.request_id
                WHERE attempt.organization_id = @OrganizationId
                  AND attempt.request_id = @RequestId
                  AND attempt.invocation_attempt_id = @InvocationAttemptId
                  AND attempt.deterministic_attempt_id = @DeterministicAttemptId
                  AND attempt.criterion_id = @CriterionId
                  AND attempt.criterion_version = @CriterionVersion
                  AND attempt.outcome = 'succeeded'
                  AND request.activity_id = @ActivityId
                  AND request.participant_id = @ParticipantId
                  AND request.attempt_id = @AttemptId
                  AND request.session_id = @SessionId
                  AND NOT EXISTS (
                        SELECT 1
                        FROM evaluation_deterministic_input_authority AS existing
                        WHERE existing.organization_id = @OrganizationId
                          AND existing.request_id = @RequestId
                          AND existing.invocation_attempt_id = @InvocationAttemptId
                          AND existing.criterion_id = @CriterionId
                          AND existing.criterion_version = @CriterionVersion);
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.RequestId,
                    command.InvocationAttemptId,
                    command.CriterionId,
                    command.CriterionVersion,
                    command.CanonicalInputDigest,
                    command.ProtectedInputRef,
                    InputUtf8 = command.InputUtf8.ToArray(),
                    command.DeterministicAttemptId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    command.Ownership.ActivityId,
                    command.Ownership.ParticipantId,
                    command.Ownership.AttemptId,
                    command.Ownership.SessionId,
                },
                cancellationToken: cancellationToken));

        if (inserted == 1)
        {
            return EvaluationDecision<bool>.Ok(true);
        }

        var existing = await connection.QuerySingleOrDefaultAsync<PersistedInputAuthorityRow>(
            new CommandDefinition(
                """
                SELECT
                    authority.canonical_input_digest,
                    authority.protected_input_ref,
                    authority.input_utf8,
                    authority.request_id,
                    request.activity_id,
                    request.participant_id,
                    request.attempt_id,
                    request.session_id
                FROM evaluation_deterministic_input_authority AS authority
                INNER JOIN evaluation_requests AS request
                  ON request.organization_id = authority.organization_id
                 AND request.request_id = authority.request_id
                WHERE authority.organization_id = @OrganizationId
                  AND authority.request_id = @RequestId
                  AND authority.invocation_attempt_id = @InvocationAttemptId
                  AND authority.criterion_id = @CriterionId
                  AND authority.criterion_version = @CriterionVersion;
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.RequestId,
                    command.InvocationAttemptId,
                    command.CriterionId,
                    command.CriterionVersion,
                },
                cancellationToken: cancellationToken));

        if (existing is null)
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (existing.request_id != command.RequestId
            || existing.activity_id != command.Ownership.ActivityId
            || existing.participant_id != command.Ownership.ParticipantId
            || existing.attempt_id != command.Ownership.AttemptId
            || existing.session_id != command.Ownership.SessionId)
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.DeterministicConflict);
        }

        if (!string.Equals(existing.canonical_input_digest, command.CanonicalInputDigest, StringComparison.Ordinal)
            || !string.Equals(existing.protected_input_ref, command.ProtectedInputRef, StringComparison.Ordinal)
            || !existing.input_utf8.AsSpan().SequenceEqual(command.InputUtf8.Span))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.DeterministicConflict);
        }

        return EvaluationDecision<bool>.Ok(true);
    }

    private sealed record PersistedInputAuthorityRow(
        string canonical_input_digest,
        string protected_input_ref,
        byte[] input_utf8,
        Guid request_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id);
}
