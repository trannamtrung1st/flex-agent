using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record PersistedEvaluationEvidenceRow(
    Guid EvidenceId,
    string SourceType,
    Guid SourceId,
    Guid SourceVersionId,
    string SourceContentDigest,
    string LocatorSchema,
    string LocatorDigest,
    string Precision,
    string IntegrityState,
    string? LocatorCanonicalJson);

public static class EvaluationCompletionPersistedEvidenceVerifier
{
    public static EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>> TryBindAuthoritative(
        FrozenInputIdentity frozenInput,
        Guid evaluationId,
        IReadOnlyList<EvidenceItem> commandItems,
        IReadOnlyList<PersistedEvaluationEvidenceRow> persistedRows)
    {
        ArgumentNullException.ThrowIfNull(frozenInput);
        ArgumentNullException.ThrowIfNull(commandItems);
        ArgumentNullException.ThrowIfNull(persistedRows);

        if (commandItems.Count == 0
            || commandItems.Any(item => item.EvaluationId != evaluationId))
        {
            return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                EvaluationFailureCodes.InvalidField);
        }

        if (persistedRows.Count == 0)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "persisted_evidence");
        }

        if (commandItems.Count != persistedRows.Count)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                EvaluationFailureCodes.InvalidField);
        }

        var persistedById = persistedRows.ToDictionary(row => row.EvidenceId);
        if (persistedById.Count != persistedRows.Count)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                EvaluationFailureCodes.DuplicateIdentity);
        }

        var authoritative = new List<EvaluationEvidenceLocatorRecord>(commandItems.Count);
        foreach (var commandItem in commandItems)
        {
            if (!persistedById.TryGetValue(commandItem.EvidenceId, out var persisted))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "evidence_id");
            }

            if (!string.Equals(persisted.IntegrityState, "verified", StringComparison.Ordinal))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "integrity_state");
            }

            if (!MatchesCommandItem(commandItem, persisted))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                    EvaluationCompletionOutcomeCodes.IntegrityConflict,
                    "evidence_item");
            }

            if (!BindsToFrozenSource(frozenInput, persisted))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "source_binding");
            }

            if (string.IsNullOrWhiteSpace(persisted.LocatorCanonicalJson))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "locator_canonical_json");
            }

            authoritative.Add(
                new EvaluationEvidenceLocatorRecord(
                    persisted.EvidenceId,
                    persisted.SourceType,
                    persisted.SourceId,
                    persisted.SourceVersionId,
                    persisted.SourceContentDigest,
                    persisted.LocatorSchema,
                    persisted.LocatorDigest,
                    persisted.Precision,
                    persisted.IntegrityState,
                    persisted.LocatorCanonicalJson));
        }

        return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Ok(authoritative);
    }

    public static EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>> TryBindVerifiedLocatorRecords(
        IReadOnlyList<EvidenceItem> commandItems,
        IReadOnlyList<EvaluationEvidenceLocatorRecord> locatorRecords)
    {
        ArgumentNullException.ThrowIfNull(commandItems);
        ArgumentNullException.ThrowIfNull(locatorRecords);

        if (commandItems.Count == 0
            || commandItems.Count != locatorRecords.Count)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                EvaluationFailureCodes.InvalidField);
        }

        var recordsById = locatorRecords.ToDictionary(record => record.EvidenceId);
        if (recordsById.Count != locatorRecords.Count)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                EvaluationFailureCodes.DuplicateIdentity);
        }

        var authoritative = new List<EvaluationEvidenceLocatorRecord>(commandItems.Count);
        foreach (var commandItem in commandItems)
        {
            if (!recordsById.TryGetValue(commandItem.EvidenceId, out var record))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "evidence_id");
            }

            if (!string.Equals(record.IntegrityState, "verified", StringComparison.Ordinal))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "integrity_state");
            }

            if (!MatchesCommandItem(commandItem, ToPersistedRow(record)))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Fail(
                    EvaluationCompletionOutcomeCodes.IntegrityConflict,
                    "evidence_item");
            }

            authoritative.Add(record);
        }

        return EvaluationDecision<IReadOnlyList<EvaluationEvidenceLocatorRecord>>.Ok(authoritative);
    }

    public static EvaluationDecision<EvidenceItem> ToAuthoritativeEvidenceItem(
        EvaluationEvidenceLocatorRecord record,
        EvaluationOwnership ownership,
        Guid evaluationId)
    {
        var source = ResolveExactSource(record);
        if (!source.Succeeded || source.Value is null)
        {
            return EvaluationDecision<EvidenceItem>.Fail(source.OutcomeCode, source.Field);
        }

        return EvidenceItem.TryCreate(
            record.EvidenceId,
            record.SourceType,
            source.Value,
            ownership,
            evaluationId,
            MapEvidencePrecision(record.Precision));
    }

    private static string MapEvidencePrecision(string persistedPrecision) =>
        persistedPrecision switch
        {
            "whole_item" => "whole_item_fallback",
            _ => persistedPrecision,
        };

    private static PersistedEvaluationEvidenceRow ToPersistedRow(EvaluationEvidenceLocatorRecord record) =>
        new(
            record.EvidenceId,
            record.SourceType,
            record.SourceId,
            record.SourceVersionId,
            record.SourceContentDigest,
            record.LocatorSchema,
            record.LocatorDigest,
            record.Precision,
            record.IntegrityState,
            record.LocatorCanonicalJson);

    public static bool LocatorRecordsEquivalent(
        IReadOnlyList<EvaluationEvidenceLocatorRecord> persisted,
        IReadOnlyList<EvaluationEvidenceLocatorRecord> verified)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        ArgumentNullException.ThrowIfNull(verified);

        if (persisted.Count != verified.Count)
        {
            return false;
        }

        var verifiedById = verified.ToDictionary(record => record.EvidenceId);
        foreach (var record in persisted)
        {
            if (!verifiedById.TryGetValue(record.EvidenceId, out var current)
                || !LocatorRecordMatches(record, current))
            {
                return false;
            }
        }

        return true;
    }

    private static bool LocatorRecordMatches(
        EvaluationEvidenceLocatorRecord left,
        EvaluationEvidenceLocatorRecord right) =>
        left.EvidenceId == right.EvidenceId
        && string.Equals(left.SourceType, right.SourceType, StringComparison.Ordinal)
        && left.SourceId == right.SourceId
        && left.SourceVersionId == right.SourceVersionId
        && string.Equals(left.SourceContentDigest, right.SourceContentDigest, StringComparison.Ordinal)
        && string.Equals(left.LocatorSchema, right.LocatorSchema, StringComparison.Ordinal)
        && string.Equals(left.LocatorDigest, right.LocatorDigest, StringComparison.Ordinal)
        && string.Equals(left.Precision, right.Precision, StringComparison.Ordinal)
        && string.Equals(left.IntegrityState, right.IntegrityState, StringComparison.Ordinal)
        && string.Equals(left.LocatorCanonicalJson, right.LocatorCanonicalJson, StringComparison.Ordinal);

    private static bool MatchesCommandItem(
        EvidenceItem commandItem,
        PersistedEvaluationEvidenceRow persisted) =>
        string.Equals(commandItem.SourceType, persisted.SourceType, StringComparison.Ordinal)
        && commandItem.Source.SourceId == persisted.SourceId
        && commandItem.Source.SourceVersionId == persisted.SourceVersionId
        && string.Equals(commandItem.Source.ContentDigest, persisted.SourceContentDigest, StringComparison.Ordinal)
        && string.Equals(commandItem.Precision, MapEvidencePrecision(persisted.Precision), StringComparison.Ordinal);

    private static bool BindsToFrozenSource(
        FrozenInputIdentity frozenInput,
        PersistedEvaluationEvidenceRow persisted) =>
        persisted.SourceType switch
        {
            "submission.direct_text" or "submission.text_attachment" =>
                persisted.SourceId != Guid.Empty
                && persisted.SourceVersionId != Guid.Empty
                && EvaluationIdentity.IsSha256Hex(persisted.SourceContentDigest),
            "deterministic.fact" =>
                persisted.SourceId != Guid.Empty
                && EvaluationIdentity.IsSha256Hex(persisted.SourceContentDigest),
            "session.transcript_item" or "session.work_trace" or "configuration.fact" or "manifest.fact" =>
                persisted.SourceId != Guid.Empty
                && persisted.SourceVersionId != Guid.Empty
                && EvaluationIdentity.IsSha256Hex(persisted.SourceContentDigest),
            _ => false,
        };

    private static EvaluationDecision<ExactSourceIdentity> ResolveExactSource(
        EvaluationEvidenceLocatorRecord record) =>
        record.SourceType switch
        {
            "submission.direct_text" or "submission.text_attachment" =>
                ExactSourceIdentity.TryCreate(
                    "task_submission",
                    record.SourceId,
                    record.SourceVersionId,
                    record.SourceContentDigest),
            "deterministic.fact" =>
                ExactSourceIdentity.TryCreate(
                    "deterministic_fact",
                    record.SourceId,
                    record.SourceVersionId,
                    record.SourceContentDigest),
            "session.transcript_item" =>
                ExactSourceIdentity.TryCreate(
                    "session_transcript",
                    record.SourceId,
                    record.SourceVersionId,
                    record.SourceContentDigest),
            "session.work_trace" =>
                ExactSourceIdentity.TryCreate(
                    "session_work_trace",
                    record.SourceId,
                    record.SourceVersionId,
                    record.SourceContentDigest),
            "configuration.fact" =>
                ExactSourceIdentity.TryCreate(
                    "configuration_fact",
                    record.SourceId,
                    record.SourceVersionId,
                    record.SourceContentDigest),
            "manifest.fact" =>
                ExactSourceIdentity.TryCreate(
                    "manifest_fact",
                    record.SourceId,
                    record.SourceVersionId,
                    record.SourceContentDigest),
            _ => EvaluationDecision<ExactSourceIdentity>.Fail(EvaluationFailureCodes.InvalidField, "source_type"),
        };
}
