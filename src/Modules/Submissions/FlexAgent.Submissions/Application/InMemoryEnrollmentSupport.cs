using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class InMemoryEnrollmentUnitOfWork(
    IEnrollmentSessionPort sessions,
    InMemoryEnrollmentStore store,
    InMemoryEnrollmentOperationStore operations,
    RecordingEnrollmentAuditPort audit) : IEnrollmentUnitOfWork
{
    public bool AuditAccepted { get; set; } = true;

    public bool OutboxAccepted { get; set; } = true;

    public async Task<T> ExecuteAsync<T>(
        EnrollmentActorContext actor,
        Func<IEnrollmentTransaction, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var snapshot = new InMemoryEnrollmentSnapshot(store, operations, audit);
        var transaction = new InMemoryEnrollmentTransaction
        {
            AuditAccepted = AuditAccepted,
            OutboxAccepted = OutboxAccepted,
        };
        try
        {
            var result = await action(transaction);
            if (!await sessions.ConfirmLiveAsync(actor, transaction, cancellationToken))
            {
                throw new EnrollmentSessionExpiredException();
            }

            return result;
        }
        catch
        {
            snapshot.Restore();
            throw;
        }
    }
}

file sealed class InMemoryEnrollmentSnapshot
{
    private readonly InMemoryEnrollmentStore _store;
    private readonly InMemoryEnrollmentOperationStore _operations;
    private readonly RecordingEnrollmentAuditPort _audit;
    private readonly Enrollment[] _enrollments;
    private readonly EnrollmentEvent[] _events;
    private readonly EnrollmentOperation[] _operationItems;
    private readonly int _requiredWrites;
    private readonly int _availabilityWrites;
    private readonly Guid? _lastResourceId;
    private readonly string? _lastResourceType;

    public InMemoryEnrollmentSnapshot(
        InMemoryEnrollmentStore store,
        InMemoryEnrollmentOperationStore operations,
        RecordingEnrollmentAuditPort audit)
    {
        _store = store;
        _operations = operations;
        _audit = audit;
        _enrollments = [.. store.Items];
        _events = [.. store.Events];
        _operationItems = [.. operations.Items];
        _requiredWrites = audit.RequiredWrites;
        _availabilityWrites = audit.AvailabilityWrites;
        _lastResourceId = audit.LastResourceId;
        _lastResourceType = audit.LastResourceType;
    }

    public void Restore()
    {
        _store.Restore(_enrollments, _events);
        _operations.Restore(_operationItems);
        _audit.Restore(_requiredWrites, _availabilityWrites, _lastResourceId, _lastResourceType);
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

    public IReadOnlyList<EnrollmentEvent> Events => _events;

    public int TransactionalFindCount { get; private set; }

    public void Restore(
        IReadOnlyList<Enrollment> enrollments,
        IReadOnlyList<EnrollmentEvent> events)
    {
        _enrollments.Clear();
        _enrollments.AddRange(enrollments);
        _events.Clear();
        _events.AddRange(events);
    }

    public Task<Enrollment?> FindAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            TransactionalFindCount++;
        }

        return Task.FromResult(_enrollments.SingleOrDefault(item =>
            item.OrganizationId == organizationId && item.EnrollmentId == enrollmentId));
    }

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
        DateTimeOffset? afterTime,
        Guid? afterId,
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
        return Task.FromResult(TakePage(items, afterTime, afterId, limit));
    }

    public Task<CursorPage<Enrollment>> ListCurrentForParticipantAsync(
        Guid organizationId,
        Guid participantActorId,
        DateTimeOffset? afterTime,
        Guid? afterId,
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
        return Task.FromResult(TakePage(items, afterTime, afterId, limit));
    }

    private static CursorPage<Enrollment> TakePage(
        IReadOnlyList<Enrollment> items,
        DateTimeOffset? afterTime,
        Guid? afterId,
        int limit)
    {
        var filtered = afterTime is null || afterId is null
            ? items
            : items
                .Where(item =>
                    item.UpdatedAtUtc > afterTime
                    || (item.UpdatedAtUtc == afterTime && item.EnrollmentId.CompareTo(afterId.Value) > 0))
                .ToArray();
        var page = filtered.Take(limit + 1).ToArray();
        var hasMore = page.Length > limit;
        var taken = page.Take(limit).ToArray();
        return new CursorPage<Enrollment>(taken, null, hasMore);
    }
}

public sealed class InMemoryEnrollmentOperationStore : IEnrollmentOperationStore
{
    private readonly List<EnrollmentOperation> _operations = [];

    public IReadOnlyList<EnrollmentOperation> Items => _operations;

    public void Restore(IReadOnlyList<EnrollmentOperation> operations)
    {
        _operations.Clear();
        _operations.AddRange(operations);
    }

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

    public Func<bool>? ConfirmWhen { get; set; }

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
        return Task.FromResult(ConfirmWhen?.Invoke() ?? ConfirmPermit);
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
        Guid? afterActorId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var filtered = Candidates
            .Where(item =>
                (string.IsNullOrWhiteSpace(prefix)
                    || item.DisplayLabel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                && (afterActorId is null || item.ActorId.CompareTo(afterActorId.Value) > 0))
            .OrderBy(item => item.ActorId)
            .Take(limit + 1)
            .ToArray();
        var hasMore = filtered.Length > limit;
        var items = filtered.Take(limit).ToArray();
        return Task.FromResult(new CursorPage<EnrollmentCandidate>(items, null, hasMore));
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

    public void Restore(
        int requiredWrites,
        int availabilityWrites,
        Guid? lastResourceId,
        string? lastResourceType)
    {
        RequiredWrites = requiredWrites;
        AvailabilityWrites = availabilityWrites;
        LastResourceId = lastResourceId;
        LastResourceType = lastResourceType;
    }

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
