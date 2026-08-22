using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class InMemoryEnrollmentUnitOfWork : IEnrollmentUnitOfWork
{
    public bool AuditAccepted { get; set; } = true;

    public bool OutboxAccepted { get; set; } = true;

    public Task<T> ExecuteAsync<T>(
        Func<IEnrollmentTransaction, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var transaction = new InMemoryEnrollmentTransaction
        {
            AuditAccepted = AuditAccepted,
            OutboxAccepted = OutboxAccepted,
        };
        return action(transaction);
    }
}

public sealed class InMemoryEnrollmentTransaction : IEnrollmentTransaction
{
    public bool AuditAccepted { get; set; } = true;

    public bool OutboxAccepted { get; set; } = true;

    public object CommitHandle => this;
}

public sealed class InMemoryEnrollmentStore : IEnrollmentStore
{
    private readonly List<Enrollment> _enrollments = [];
    private readonly List<EnrollmentEvent> _events = [];

    public IReadOnlyList<Enrollment> Items => _enrollments;

    public Task<Enrollment?> FindAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken) =>
        Task.FromResult(_enrollments.SingleOrDefault(item =>
            item.OrganizationId == organizationId && item.EnrollmentId == enrollmentId));

    public Task<Enrollment?> FindLiveForParticipantAsync(
        Guid organizationId,
        Guid activityId,
        Guid participantActorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken) =>
        Task.FromResult(_enrollments.SingleOrDefault(item =>
            item.OrganizationId == organizationId
            && item.ActivityId == activityId
            && item.ParticipantActorId == participantActorId
            && EnrollmentProjection.IsLive(item.Status)));

    public Task InsertAsync(
        Enrollment enrollment,
        EnrollmentEvent enrollmentEvent,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        _enrollments.Add(enrollment);
        _events.Add(enrollmentEvent);
        return Task.CompletedTask;
    }

    public bool ForceStaleUpdate { get; set; }

    public Task UpdateAsync(
        Enrollment enrollment,
        EnrollmentEvent enrollmentEvent,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (ForceStaleUpdate)
        {
            throw new EnrollmentStaleRevisionException();
        }

        var index = _enrollments.FindIndex(item => item.EnrollmentId == enrollment.EnrollmentId);
        if (index >= 0)
        {
            _enrollments[index] = enrollment;
        }

        _events.Add(enrollmentEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EnrollmentHistoryItem>> ListHistoryAsync(
        Guid organizationId,
        Guid enrollmentId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EnrollmentHistoryItem>>(
            _events
                .Where(item => item.OrganizationId == organizationId && item.EnrollmentId == enrollmentId)
                .OrderBy(item => item.Sequence)
                .Select(item => new EnrollmentHistoryItem(
                    item.Sequence,
                    item.PriorStatus,
                    item.NewStatus,
                    item.ReasonCode,
                    item.OccurredAtUtc))
                .ToArray());

    public Task<CursorPage<Enrollment>> ListForCohortAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        var items = _enrollments
            .Where(item =>
                item.OrganizationId == organizationId
                && item.ActivityId == activityId
                && item.CohortId == cohortId)
            .OrderBy(item => item.UpdatedAtUtc)
            .ThenBy(item => item.EnrollmentId)
            .ToArray();
        return Task.FromResult(TakePage(items, cursor, limit, item => item.EnrollmentId));
    }

    public Task<CursorPage<Enrollment>> ListCurrentForParticipantAsync(
        Guid organizationId,
        Guid participantActorId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        var items = _enrollments
            .Where(item =>
                item.OrganizationId == organizationId
                && item.ParticipantActorId == participantActorId
                && item.VisibilityForParticipant() != EnrollmentVisibilityStates.Unavailable)
            .OrderBy(item => item.UpdatedAtUtc)
            .ThenBy(item => item.EnrollmentId)
            .ToArray();
        return Task.FromResult(TakePage(items, cursor, limit, item => item.EnrollmentId));
    }

    private static CursorPage<Enrollment> TakePage(
        IReadOnlyList<Enrollment> items,
        string? cursor,
        int limit,
        Func<Enrollment, Guid> id)
    {
        var start = 0;
        if (!string.IsNullOrWhiteSpace(cursor) && Guid.TryParse(cursor, out var after))
        {
            start = items.ToList().FindIndex(item => id(item) == after) + 1;
            if (start <= 0)
            {
                return new CursorPage<Enrollment>([], null, false);
            }
        }

        var page = items.Skip(start).Take(limit + 1).ToArray();
        var hasMore = page.Length > limit;
        var taken = page.Take(limit).ToArray();
        return new CursorPage<Enrollment>(
            taken,
            hasMore ? taken[^1].EnrollmentId.ToString("D") : null,
            hasMore);
    }
}

public sealed class InMemoryEnrollmentOperationStore : IEnrollmentOperationStore
{
    private readonly List<EnrollmentOperation> _operations = [];

    public Task AcquireLockAsync(
        Guid organizationId,
        Guid actorId,
        string operationKind,
        Guid resourceId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task AcquireLiveParticipantLockAsync(
        Guid organizationId,
        Guid activityId,
        Guid participantActorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<EnrollmentOperation?> FindAsync(
        Guid organizationId,
        Guid actorId,
        string operationKind,
        Guid resourceId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken) =>
        Task.FromResult(_operations.SingleOrDefault(item =>
            item.OrganizationId == organizationId
            && item.ActorId == actorId
            && item.OperationKind == operationKind
            && item.ResourceId == resourceId
            && item.IdempotencyKey == idempotencyKey));

    public Task InsertAsync(
        EnrollmentOperation operation,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        _operations.Add(operation);
        return Task.CompletedTask;
    }
}

public sealed class AllowEnrollmentSessionPort : IEnrollmentSessionPort
{
    public bool Permit { get; set; } = true;

    public bool ConfirmPermit { get; set; } = true;

    public int RevalidateCount { get; private set; }

    public int ConfirmCount { get; private set; }

    public Task<bool> RevalidateLiveAsync(
        EnrollmentActorContext actor,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        RevalidateCount++;
        return Task.FromResult(Permit);
    }

    public Task<bool> ConfirmLiveAsync(
        EnrollmentActorContext actor,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ConfirmCount++;
        return Task.FromResult(ConfirmPermit);
    }
}

public sealed class AllowEnrollmentAuthorizationPort : IEnrollmentAuthorizationPort
{
    public bool Permit { get; set; } = true;

    public string? LastResourceType { get; private set; }

    public Task<AuthorizationDecision> AuthorizeAdmissionAsync(
        EnrollmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        CancellationToken cancellationToken = default)
    {
        LastResourceType = resourceType;
        return Task.FromResult(Decision());
    }

    public Task<AuthorizationDecision> ReauthorizeAsync(
        EnrollmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        LastResourceType = resourceType;
        return Task.FromResult(Decision());
    }

    private AuthorizationDecision Decision() =>
        Permit
            ? AuthorizationDecision.Permit(1)
            : new AuthorizationDecision(
                false,
                AuthorizationOutcomes.Deny,
                AuthorizationReasonCodes.DeniedNoGrant,
                1,
                "policy.v1");
}

public sealed class FixedActivatedCohortPort : IActivatedCohortPort
{
    public ActivatedCohortBinding? Binding { get; set; }

    public Task<ActivatedCohortBinding?> FindActivatedAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Matches(organizationId, activityId, cohortId) ? Binding : null);

    public Task<ActivatedCohortBinding?> RevalidateAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default) =>
        FindActivatedAsync(organizationId, activityId, cohortId, cancellationToken);

    private bool Matches(Guid organizationId, Guid activityId, Guid cohortId) =>
        Binding is not null
        && Binding.OrganizationId == organizationId
        && Binding.ActivityId == activityId
        && Binding.CohortId == cohortId;
}

public sealed class InMemoryCandidatePort : IEnrollmentCandidatePort
{
    public List<EnrollmentCandidate> Candidates { get; } = [];

    public Task<CursorPage<EnrollmentCandidate>> ListEligibleAsync(
        Guid organizationId,
        string? prefix,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var items = Candidates
            .Where(item =>
                string.IsNullOrWhiteSpace(prefix)
                || item.DisplayLabel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.DisplayLabel, StringComparer.Ordinal)
            .Take(limit)
            .ToArray();
        return Task.FromResult(new CursorPage<EnrollmentCandidate>(items, null, false));
    }

    public Task<EnrollmentCandidate?> RevalidateEligibleAsync(
        Guid organizationId,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Candidates.SingleOrDefault(item => item.ActorId == actorId));

    public Task<string?> DisplayLabelAsync(
        Guid organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Candidates.SingleOrDefault(item => item.ActorId == actorId)?.DisplayLabel);
}

public sealed class RecordingEnrollmentAuditPort : IEnrollmentAuditPort
{
    public int RequiredWrites { get; private set; }

    public int AvailabilityWrites { get; private set; }

    public Guid? LastResourceId { get; private set; }

    public string? LastResourceType { get; private set; }

    public Task WriteRequiredDurableAsync(
        EnrollmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        string outcome,
        string? reasonCode,
        AuthorizationDecision? authorization,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        RequiredWrites++;
        LastResourceId = resourceId;
        LastResourceType = resourceType;
        return Task.CompletedTask;
    }

    public Task WriteAvailabilityAsync(
        Enrollment enrollment,
        EnrollmentActorContext actor,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        AvailabilityWrites++;
        return Task.CompletedTask;
    }
}

public sealed class FixedEnrollmentClock(DateTimeOffset utcNow) : IEnrollmentClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}
