using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresEvaluationEvidenceLocatorStore(
    PostgresConnectionAccessor connectionAccessor) : IEvaluationEvidenceLocatorStore
{
    public async Task<EvaluationDecision<IReadOnlyList<Guid>>> TryPersistAsync(
        Guid organizationId,
        Guid evaluationId,
        Guid requestId,
        EvaluationOwnership ownership,
        IReadOnlyList<EvaluationEvidenceLocatorRecord> records,
        string createdByService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (organizationId == Guid.Empty
            || evaluationId == Guid.Empty
            || requestId == Guid.Empty
            || records.Count is < 1 or > 128
            || string.IsNullOrWhiteSpace(createdByService))
        {
            return EvaluationDecision<IReadOnlyList<Guid>>.Fail(EvaluationFailureCodes.InvalidField);
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var record in records)
        {
            var inserted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO evaluation_evidence_items (
                        organization_id, evaluation_id, evidence_id, request_id, activity_id,
                        participant_id, attempt_id, session_id, source_type, source_id,
                        source_version_id, source_content_digest, locator_schema, locator_digest,
                        precision, integrity_state, created_by_service, created_at)
                    SELECT
                        @OrganizationId, @EvaluationId, @EvidenceId, @RequestId, @ActivityId,
                        @ParticipantId, @AttemptId, @SessionId, @SourceType, @SourceId,
                        @SourceVersionId, @SourceContentDigest, @LocatorSchema, @LocatorDigest,
                        @Precision, @IntegrityState, @CreatedByService, clock_timestamp()
                    FROM evaluation_requests AS request
                    WHERE request.organization_id = @OrganizationId
                      AND request.activity_id = @ActivityId
                      AND request.participant_id = @ParticipantId
                      AND request.attempt_id = @AttemptId
                      AND request.session_id = @SessionId
                      AND request.request_id = @RequestId
                      AND NOT EXISTS (
                            SELECT 1
                            FROM evaluation_evidence_items AS existing
                            WHERE existing.organization_id = @OrganizationId
                              AND existing.evaluation_id = @EvaluationId
                              AND existing.evidence_id = @EvidenceId);
                    """,
                    new
                    {
                        OrganizationId = organizationId,
                        EvaluationId = evaluationId,
                        EvidenceId = record.EvidenceId,
                        RequestId = requestId,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId,
                        ownership.SessionId,
                        record.SourceType,
                        SourceId = record.SourceId,
                        SourceVersionId = record.SourceVersionId,
                        SourceContentDigest = record.SourceContentDigest,
                        LocatorSchema = record.LocatorSchema,
                        LocatorDigest = record.LocatorDigest,
                        record.Precision,
                        IntegrityState = record.IntegrityState,
                        CreatedByService = createdByService,
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (inserted == 1)
            {
                continue;
            }

            var existing = await connection.QuerySingleOrDefaultAsync<PersistedEvidenceItemRow>(
                new CommandDefinition(
                    """
                    SELECT source_type, source_id, source_version_id, source_content_digest,
                           locator_schema, locator_digest, precision, integrity_state
                    FROM evaluation_evidence_items
                    WHERE organization_id = @OrganizationId
                      AND evaluation_id = @EvaluationId
                      AND evidence_id = @EvidenceId;
                    """,
                    new
                    {
                        OrganizationId = organizationId,
                        EvaluationId = evaluationId,
                        EvidenceId = record.EvidenceId,
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (existing is null || !Matches(record, existing))
            {
                await transaction.RollbackAsync(cancellationToken);
                return EvaluationDecision<IReadOnlyList<Guid>>.Fail(EvaluationFailureCodes.DuplicateIdentity);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return EvaluationDecision<IReadOnlyList<Guid>>.Ok(records.Select(record => record.EvidenceId).ToArray());
    }

    private static bool Matches(
        EvaluationEvidenceLocatorRecord record,
        PersistedEvidenceItemRow existing) =>
        record.SourceType == existing.source_type
        && record.SourceId == existing.source_id
        && record.SourceVersionId == existing.source_version_id
        && string.Equals(record.SourceContentDigest, existing.source_content_digest, StringComparison.Ordinal)
        && record.LocatorSchema == existing.locator_schema
        && string.Equals(record.LocatorDigest, existing.locator_digest, StringComparison.Ordinal)
        && record.Precision == existing.precision
        && record.IntegrityState == existing.integrity_state;

    private sealed record PersistedEvidenceItemRow(
        string source_type,
        Guid source_id,
        Guid source_version_id,
        string source_content_digest,
        string locator_schema,
        string locator_digest,
        string precision,
        string integrity_state);
}
