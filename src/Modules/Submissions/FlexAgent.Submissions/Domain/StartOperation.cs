using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.Submissions.Domain;

public sealed record StartOperation(
    Guid OrganizationId,
    Guid ParticipantActorId,
    Guid EnrollmentId,
    string Action,
    string IdempotencyKey,
    string CommandDigest,
    string Status,
    Guid ClaimOwner,
    DateTimeOffset ClaimedAtUtc,
    DateTimeOffset LeaseUntilUtc,
    Guid? AttemptId,
    Guid? SessionId,
    string? OutcomeCode,
    DateTimeOffset? FinishedAtUtc);

public static class StartOperationPolicy
{
    public static AttemptDecision<StartOperation> Claim(
        Guid organizationId,
        Guid participantActorId,
        Guid enrollmentId,
        string idempotencyKey,
        string commandDigest,
        Guid claimOwner,
        DateTimeOffset nowUtc,
        StartOperation? existing)
    {
        if (organizationId == Guid.Empty
            || participantActorId == Guid.Empty
            || enrollmentId == Guid.Empty
            || claimOwner == Guid.Empty
            || EnrollmentIdempotencyCharactersInvalid(idempotencyKey)
            || commandDigest.Length != 64
            || commandDigest != commandDigest.ToLowerInvariant())
        {
            return AttemptDecision<StartOperation>.Fail(AttemptFailureCodes.InvalidField);
        }

        if (existing is null)
        {
            return AttemptDecision<StartOperation>.Ok(
                new StartOperation(
                    organizationId,
                    participantActorId,
                    enrollmentId,
                    AttemptOperationKinds.Start,
                    idempotencyKey,
                    commandDigest,
                    StartOperationStates.Claimed,
                    claimOwner,
                    nowUtc,
                    nowUtc + StartOperationLease.Duration,
                    null,
                    null,
                    AttemptOutcomes.Claimed,
                    null),
                AttemptOutcomes.Claimed);
        }

        if (!string.Equals(existing.CommandDigest, commandDigest, StringComparison.Ordinal)
            || existing.OrganizationId != organizationId
            || existing.ParticipantActorId != participantActorId
            || existing.EnrollmentId != enrollmentId)
        {
            return AttemptDecision<StartOperation>.Fail(AttemptFailureCodes.IdempotencyConflict);
        }

        if (existing.Status == StartOperationStates.Committed)
        {
            return AttemptDecision<StartOperation>.Ok(existing, AttemptOutcomes.Reconciled);
        }

        if (existing.Status == StartOperationStates.Failed)
        {
            return AttemptDecision<StartOperation>.Ok(existing, existing.OutcomeCode ?? AttemptOutcomes.StartFailed);
        }

        if (existing.Status == StartOperationStates.Claimed
            && nowUtc >= existing.LeaseUntilUtc)
        {
            return AttemptDecision<StartOperation>.Ok(
                existing with
                {
                    ClaimOwner = claimOwner,
                    ClaimedAtUtc = nowUtc,
                    LeaseUntilUtc = nowUtc + StartOperationLease.Duration,
                    OutcomeCode = AttemptOutcomes.ClaimRecovered,
                },
                AttemptOutcomes.ClaimRecovered);
        }

        if (existing.Status == StartOperationStates.Claimed)
        {
            return AttemptDecision<StartOperation>.Ok(existing, AttemptOutcomes.Claimed);
        }

        return AttemptDecision<StartOperation>.Fail(AttemptFailureCodes.Unavailable);
    }

    public static AttemptDecision<StartOperation> Commit(
        StartOperation operation,
        Guid attemptId,
        Guid sessionId,
        DateTimeOffset finishedAtUtc) =>
        operation.Status != StartOperationStates.Claimed
            ? AttemptDecision<StartOperation>.Fail(AttemptFailureCodes.Unavailable)
            : AttemptDecision<StartOperation>.Ok(
                operation with
                {
                    Status = StartOperationStates.Committed,
                    AttemptId = attemptId,
                    SessionId = sessionId,
                    OutcomeCode = AttemptOutcomes.Activated,
                    FinishedAtUtc = finishedAtUtc,
                    LeaseUntilUtc = finishedAtUtc,
                },
                AttemptOutcomes.Activated);

    public static AttemptDecision<StartOperation> Fail(
        StartOperation operation,
        string outcomeCode,
        DateTimeOffset finishedAtUtc) =>
        operation.Status != StartOperationStates.Claimed
            ? AttemptDecision<StartOperation>.Fail(AttemptFailureCodes.Unavailable)
            : AttemptDecision<StartOperation>.Ok(
                operation with
                {
                    Status = StartOperationStates.Failed,
                    OutcomeCode = outcomeCode,
                    FinishedAtUtc = finishedAtUtc,
                    LeaseUntilUtc = finishedAtUtc,
                },
                AttemptOutcomes.StartFailed);

    public static bool HasActiveConflict(
        IReadOnlyList<StartOperation> operations,
        string excludingIdempotencyKey,
        DateTimeOffset nowUtc) =>
        operations.Any(operation =>
            !string.Equals(operation.IdempotencyKey, excludingIdempotencyKey, StringComparison.Ordinal)
            && operation.Status == StartOperationStates.Claimed
            && nowUtc < operation.LeaseUntilUtc);

    private static bool EnrollmentIdempotencyCharactersInvalid(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
        {
            return true;
        }

        foreach (var character in value)
        {
            if (!(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-'))
            {
                return true;
            }
        }

        return false;
    }
}

public static class AttemptCommandDigest
{
    public static string Compute(
        Guid organizationId,
        Guid enrollmentId,
        Guid participantActorId,
        int nextOrdinal,
        string entitlementSource,
        IReadOnlyList<Guid> acceptedVersionIds,
        IReadOnlyList<Guid> noticeVersionIds)
    {
        var payload = string.Join(
            '\n',
            AttemptOperationKinds.Start,
            organizationId.ToString("D"),
            enrollmentId.ToString("D"),
            participantActorId.ToString("D"),
            nextOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture),
            entitlementSource,
            string.Join(',', acceptedVersionIds.Select(id => id.ToString("D"))),
            string.Join(',', noticeVersionIds.Select(id => id.ToString("D"))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}

public static class AcknowledgmentCommandDigest
{
    public static string Compute(
        Guid organizationId,
        Guid enrollmentId,
        Guid participantActorId,
        Guid noticeId,
        Guid sourceVersionId,
        string outcome)
    {
        var payload = string.Join(
            '\n',
            AttemptOperationKinds.Acknowledge,
            organizationId.ToString("D"),
            enrollmentId.ToString("D"),
            participantActorId.ToString("D"),
            noticeId.ToString("D"),
            sourceVersionId.ToString("D"),
            outcome);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
