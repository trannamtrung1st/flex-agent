using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class EmptyParticipantNoticePort : IParticipantNoticePort
{
    public static EmptyParticipantNoticePort Instance { get; } = new();

    public Task<IReadOnlyList<RequiredNoticeProjection>?> ListRequiredAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid baselineId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        _ = (organizationId, activityId, cohortId, baselineId, transaction, cancellationToken);
        return Task.FromResult<IReadOnlyList<RequiredNoticeProjection>?>([]);
    }
}

public sealed class InMemoryAcknowledgmentLifecyclePort : IAcknowledgmentLifecyclePort
{
    private readonly List<Stored> _items = [];

    public IReadOnlyList<Stored> Items => _items;

    public void Restore(IReadOnlyList<Stored> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    public Task<AcknowledgmentMutationOutcome> RecordAsync(
        AcknowledgeAttemptNoticeCommand command,
        RequiredNoticeProjection notice,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        _ = commitTransaction;
        _ = cancellationToken;
        var existing = _items.SingleOrDefault(item =>
            item.OrganizationId == command.Actor.Organization.OrganizationId
            && item.Record.EnrollmentId == command.EnrollmentId
            && item.IdempotencyKey == command.IdempotencyKey);
        if (existing is not null)
        {
            if (!string.Equals(existing.CommandDigest, command.TrustedCommandDigest, StringComparison.Ordinal))
            {
                return Task.FromResult(new AcknowledgmentMutationOutcome(
                    false,
                    AttemptFailureCodes.IdempotencyConflict,
                    null,
                    null));
            }

            return Task.FromResult(new AcknowledgmentMutationOutcome(
                true,
                "acknowledgment.reconciled",
                existing.Record.RecordId,
                existing.Record.Outcome));
        }

        var recorded = new CurrentAcknowledgmentFact(
            Guid.CreateVersion7(),
            command.EnrollmentId,
            command.Actor.Actor.ActorId,
            notice.NoticeId,
            notice.SourceVersionId,
            notice.ContentDigest,
            command.Outcome,
            DateTimeOffset.UtcNow,
            null);
        _items.Add(new Stored(
            command.Actor.Organization.OrganizationId,
            command.IdempotencyKey,
            command.TrustedCommandDigest,
            recorded));
        return Task.FromResult(new AcknowledgmentMutationOutcome(true, "acknowledgment.recorded", recorded.RecordId, command.Outcome));
    }

    public Task<IReadOnlyList<CurrentAcknowledgmentFact>> ListCurrentAsync(
        Guid organizationId,
        Guid enrollmentId,
        Guid participantActorId,
        IReadOnlyList<RequiredNoticeProjection> notices,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        _ = (notices, commitTransaction, cancellationToken);
        return Task.FromResult<IReadOnlyList<CurrentAcknowledgmentFact>>(
            _items.Where(item =>
                    item.OrganizationId == organizationId
                    && item.Record.EnrollmentId == enrollmentId
                    && item.Record.ParticipantActorId == participantActorId)
                .Select(item => item.Record)
                .ToArray());
    }

    public Task<string?> BindToAttemptAsync(
        IReadOnlyList<CurrentAcknowledgmentFact> records,
        Guid attemptId,
        Guid enrollmentId,
        Guid participantActorId,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        _ = (commitTransaction, cancellationToken);
        foreach (var record in records)
        {
            var index = _items.FindIndex(item => item.Record.RecordId == record.RecordId);
            if (index < 0
                || _items[index].Record.EnrollmentId != enrollmentId
                || _items[index].Record.ParticipantActorId != participantActorId)
            {
                return Task.FromResult<string?>(AttemptFailureCodes.AcknowledgmentInvalid);
            }

            if (_items[index].Record.BoundAttemptId is Guid bound && bound != attemptId)
            {
                return Task.FromResult<string?>(AttemptFailureCodes.AcknowledgmentInvalid);
            }

            _items[index] = _items[index] with
            {
                Record = _items[index].Record with { BoundAttemptId = attemptId },
            };
        }

        return Task.FromResult<string?>(null);
    }

    public sealed record Stored(
        Guid OrganizationId,
        string IdempotencyKey,
        string CommandDigest,
        CurrentAcknowledgmentFact Record);
}
