namespace FlexAgent.Sessions.Domain;

public static class AcknowledgmentOutcomes
{
    public const string Affirmed = "affirmed";
    public const string Declined = "declined";
    public const string Withdrawn = "withdrawn";
}

public static class AcknowledgmentFailureCodes
{
    public const string InvalidField = "acknowledgment.invalid_field";
    public const string Denied = "acknowledgment.denied";
    public const string Stale = "acknowledgment.stale";
    public const string CrossScope = "acknowledgment.cross_scope";
    public const string IdempotencyConflict = "acknowledgment.idempotency_conflict";
    public const string Unavailable = "acknowledgment.unavailable";
}

public static class AcknowledgmentOutcomesCodes
{
    public const string Recorded = "acknowledgment.recorded";
    public const string Reconciled = "acknowledgment.reconciled";
    public const string Bound = "acknowledgment.bound";
}

public static class AcknowledgmentOperationKinds
{
    public const string Record = "acknowledgment_record";
}

public sealed record AcknowledgmentDecision<T>(
    bool Succeeded,
    string OutcomeCode,
    T? Value)
{
    public static AcknowledgmentDecision<T> Ok(T value, string outcomeCode) =>
        new(true, outcomeCode, value);

    public static AcknowledgmentDecision<T> Fail(string outcomeCode) =>
        new(false, outcomeCode, default);
}

public sealed record ParticipantNoticeDescriptor(
    Guid NoticeId,
    string NoticeType,
    string RequiredOutcome,
    string ProtectedContentRef,
    string ContentDigest,
    Guid SourceId,
    Guid SourceVersionId,
    string SourceContentDigest);

public sealed record AcknowledgmentRecord(
    Guid RecordId,
    Guid OrganizationId,
    Guid EnrollmentId,
    Guid ParticipantActorId,
    Guid NoticeId,
    Guid SourceId,
    Guid SourceVersionId,
    string SourceContentDigest,
    string NoticeContentDigest,
    string Outcome,
    DateTimeOffset RecordedAtUtc,
    Guid? BoundAttemptId);

public static class AcknowledgmentPolicy
{
    public static AcknowledgmentDecision<AcknowledgmentRecord> Record(
        Guid recordId,
        Guid organizationId,
        Guid enrollmentId,
        Guid participantActorId,
        ParticipantNoticeDescriptor notice,
        string outcome,
        DateTimeOffset recordedAtUtc)
    {
        if (recordId == Guid.Empty
            || organizationId == Guid.Empty
            || enrollmentId == Guid.Empty
            || participantActorId == Guid.Empty
            || notice.NoticeId == Guid.Empty
            || notice.SourceId == Guid.Empty
            || notice.SourceVersionId == Guid.Empty
            || notice.SourceContentDigest.Length != 64
            || notice.ContentDigest.Length != 64
            || string.IsNullOrWhiteSpace(notice.ProtectedContentRef)
            || recordedAtUtc.Offset != TimeSpan.Zero)
        {
            return AcknowledgmentDecision<AcknowledgmentRecord>.Fail(AcknowledgmentFailureCodes.InvalidField);
        }

        if (outcome is not (AcknowledgmentOutcomes.Affirmed or AcknowledgmentOutcomes.Declined or AcknowledgmentOutcomes.Withdrawn))
        {
            return AcknowledgmentDecision<AcknowledgmentRecord>.Fail(AcknowledgmentFailureCodes.InvalidField);
        }

        return AcknowledgmentDecision<AcknowledgmentRecord>.Ok(
            new AcknowledgmentRecord(
                recordId,
                organizationId,
                enrollmentId,
                participantActorId,
                notice.NoticeId,
                notice.SourceId,
                notice.SourceVersionId,
                notice.SourceContentDigest,
                notice.ContentDigest,
                outcome,
                recordedAtUtc,
                null),
            AcknowledgmentOutcomesCodes.Recorded);
    }

    public static AcknowledgmentDecision<AcknowledgmentRecord> BindToAttempt(
        AcknowledgmentRecord record,
        Guid attemptId,
        Guid enrollmentId,
        Guid participantActorId)
    {
        if (record.BoundAttemptId is not null)
        {
            return record.BoundAttemptId == attemptId
                ? AcknowledgmentDecision<AcknowledgmentRecord>.Ok(record, AcknowledgmentOutcomesCodes.Bound)
                : AcknowledgmentDecision<AcknowledgmentRecord>.Fail(AcknowledgmentFailureCodes.CrossScope);
        }

        if (record.EnrollmentId != enrollmentId || record.ParticipantActorId != participantActorId)
        {
            return AcknowledgmentDecision<AcknowledgmentRecord>.Fail(AcknowledgmentFailureCodes.CrossScope);
        }

        if (record.Outcome != AcknowledgmentOutcomes.Affirmed)
        {
            return AcknowledgmentDecision<AcknowledgmentRecord>.Fail(AcknowledgmentFailureCodes.Stale);
        }

        return AcknowledgmentDecision<AcknowledgmentRecord>.Ok(
            record with { BoundAttemptId = attemptId },
            AcknowledgmentOutcomesCodes.Bound);
    }

    public static string? ValidateCurrentAffirmations(
        IReadOnlyList<ParticipantNoticeDescriptor> required,
        IReadOnlyList<AcknowledgmentRecord> current,
        Guid enrollmentId,
        Guid participantActorId)
    {
        foreach (var notice in required)
        {
            if (notice.RequiredOutcome != AcknowledgmentOutcomes.Affirmed)
            {
                continue;
            }

            var match = current
                .Where(record =>
                    record.NoticeId == notice.NoticeId
                    && record.SourceVersionId == notice.SourceVersionId
                    && record.EnrollmentId == enrollmentId
                    && record.ParticipantActorId == participantActorId)
                .OrderByDescending(record => record.RecordedAtUtc)
                .FirstOrDefault();
            if (match is null
                || match.Outcome != AcknowledgmentOutcomes.Affirmed
                || !string.Equals(match.NoticeContentDigest, notice.ContentDigest, StringComparison.Ordinal)
                || !string.Equals(match.SourceContentDigest, notice.SourceContentDigest, StringComparison.Ordinal))
            {
                return AcknowledgmentFailureCodes.Stale;
            }
        }

        return null;
    }
}
