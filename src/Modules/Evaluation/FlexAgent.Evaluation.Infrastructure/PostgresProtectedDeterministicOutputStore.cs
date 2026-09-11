using Dapper;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresProtectedDeterministicOutputStore(
    PostgresConnectionAccessor connectionAccessor) : IProtectedDeterministicOutputStore
{
    public async Task<EvaluationDecision<bool>> TryPersistAsync(
        ProtectedDeterministicOutputPersistCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Ownership.OrganizationId == Guid.Empty
            || command.RequestId == Guid.Empty
            || command.DeterministicAttemptId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.ProtectedOutputRef)
            || !EvaluationIdentity.IsSha256Hex(command.OutputContentDigest)
            || command.OutputUtf8.IsEmpty
            || command.OutputUtf8.Length > EvaluatorBindingValidator.MaxOutputLimitBytes)
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.InvalidField);
        }

        var projection = EvaluationDeterministicFactProjector.TryCreate(
            command.DeterministicAttemptId,
            command.OutputUtf8,
            command.OutputContentDigest);
        if (!projection.Succeeded)
        {
            return EvaluationDecision<bool>.Fail(projection.OutcomeCode, projection.Field);
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var inserted = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO evaluation_deterministic_payloads (
                    organization_id, deterministic_attempt_id, request_id,
                    protected_ref, content_digest, output_utf8, created_at)
                SELECT
                    @OrganizationId, @DeterministicAttemptId, @RequestId,
                    @ProtectedOutputRef, @OutputContentDigest, @OutputUtf8, @CreatedAt
                FROM evaluation_deterministic_attempts AS attempt
                INNER JOIN evaluation_requests AS request
                  ON request.organization_id = attempt.organization_id
                 AND request.request_id = attempt.request_id
                WHERE attempt.organization_id = @OrganizationId
                  AND attempt.request_id = @RequestId
                  AND attempt.deterministic_attempt_id = @DeterministicAttemptId
                  AND request.activity_id = @ActivityId
                  AND request.participant_id = @ParticipantId
                  AND request.attempt_id = @AttemptId
                  AND request.session_id = @SessionId
                  AND attempt.outcome = 'succeeded'
                  AND attempt.protected_output_ref = @ProtectedOutputRef
                  AND attempt.output_content_digest = @OutputContentDigest
                  AND NOT EXISTS (
                        SELECT 1
                        FROM evaluation_deterministic_payloads AS existing
                        WHERE existing.organization_id = @OrganizationId
                          AND existing.deterministic_attempt_id = @DeterministicAttemptId);
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.DeterministicAttemptId,
                    command.RequestId,
                    command.ProtectedOutputRef,
                    command.OutputContentDigest,
                    OutputUtf8 = command.OutputUtf8.ToArray(),
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

        var existing = await connection.QuerySingleOrDefaultAsync<PersistedPayloadProvenanceRow>(
            new CommandDefinition(
                """
                SELECT
                    payload.protected_ref,
                    payload.content_digest,
                    payload.output_utf8,
                    payload.request_id,
                    request.activity_id,
                    request.participant_id,
                    request.attempt_id,
                    request.session_id
                FROM evaluation_deterministic_payloads AS payload
                INNER JOIN evaluation_deterministic_attempts AS attempt
                  ON attempt.organization_id = payload.organization_id
                 AND attempt.request_id = payload.request_id
                 AND attempt.deterministic_attempt_id = payload.deterministic_attempt_id
                INNER JOIN evaluation_requests AS request
                  ON request.organization_id = payload.organization_id
                 AND request.request_id = payload.request_id
                WHERE payload.organization_id = @OrganizationId
                  AND payload.deterministic_attempt_id = @DeterministicAttemptId;
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.DeterministicAttemptId,
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

        if (!string.Equals(existing.protected_ref, command.ProtectedOutputRef, StringComparison.Ordinal)
            || !string.Equals(existing.content_digest, command.OutputContentDigest, StringComparison.Ordinal)
            || !existing.output_utf8.AsSpan().SequenceEqual(command.OutputUtf8.Span))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.DeterministicConflict);
        }

        return EvaluationDecision<bool>.Ok(true);
    }

    public async Task<EvaluationSafeFactProjection?> TryLoadProjectionAsync(
        Guid organizationId,
        Guid requestId,
        Guid deterministicAttemptId,
        string expectedContentDigest,
        string expectedCriterionId,
        string expectedCriterionVersion,
        CancellationToken cancellationToken)
    {
        var material = await TryLoadVerifiedMaterialAsync(
            organizationId,
            requestId,
            deterministicAttemptId,
            expectedContentDigest,
            expectedCriterionId,
            expectedCriterionVersion,
            cancellationToken);
        return material?.Projection;
    }

    public async Task<VerifiedDeterministicOutputMaterial?> TryLoadVerifiedMaterialAsync(
        Guid organizationId,
        Guid requestId,
        Guid deterministicAttemptId,
        string expectedContentDigest,
        string expectedCriterionId,
        string expectedCriterionVersion,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty
            || requestId == Guid.Empty
            || deterministicAttemptId == Guid.Empty
            || !EvaluationIdentity.IsSha256Hex(expectedContentDigest)
            || string.IsNullOrWhiteSpace(expectedCriterionId)
            || string.IsNullOrWhiteSpace(expectedCriterionVersion))
        {
            return null;
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PersistedPayloadRow>(
            new CommandDefinition(
                """
                SELECT payload.protected_ref, payload.content_digest, payload.output_utf8
                FROM evaluation_deterministic_payloads AS payload
                INNER JOIN evaluation_deterministic_attempts AS attempt
                  ON attempt.organization_id = payload.organization_id
                 AND attempt.request_id = payload.request_id
                 AND attempt.deterministic_attempt_id = payload.deterministic_attempt_id
                WHERE payload.organization_id = @OrganizationId
                  AND payload.deterministic_attempt_id = @DeterministicAttemptId
                  AND payload.request_id = @RequestId
                  AND attempt.criterion_id = @ExpectedCriterionId
                  AND attempt.criterion_version = @ExpectedCriterionVersion
                  AND attempt.outcome = 'succeeded'
                  AND payload.protected_ref = attempt.protected_output_ref
                  AND payload.content_digest = @ExpectedContentDigest
                  AND attempt.output_content_digest = @ExpectedContentDigest;
                """,
                new
                {
                    OrganizationId = organizationId,
                    RequestId = requestId,
                    DeterministicAttemptId = deterministicAttemptId,
                    ExpectedContentDigest = expectedContentDigest,
                    ExpectedCriterionId = expectedCriterionId,
                    ExpectedCriterionVersion = expectedCriterionVersion,
                },
                cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var projection = EvaluationDeterministicFactProjector.TryCreate(
            deterministicAttemptId,
            row.output_utf8,
            row.content_digest);
        if (!projection.Succeeded || projection.Value is null)
        {
            return null;
        }

        return new VerifiedDeterministicOutputMaterial(
            projection.Value,
            new ProtectedPayloadRefV1(row.protected_ref, row.content_digest));
    }

    private sealed record PersistedPayloadRow(
        string protected_ref,
        string content_digest,
        byte[] output_utf8);

    private sealed record PersistedPayloadProvenanceRow(
        string protected_ref,
        string content_digest,
        byte[] output_utf8,
        Guid request_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id);
}
