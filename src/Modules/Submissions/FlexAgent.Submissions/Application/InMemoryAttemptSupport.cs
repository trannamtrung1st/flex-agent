using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class EmptyRetryEntitlementReader : IRetryEntitlementReader
{
    public static EmptyRetryEntitlementReader Instance { get; } = new();

    public Task<IReadOnlyList<RetryEntitlementFact>> ListUnusedAsync(
        Guid organizationId,
        Guid enrollmentId,
        DateTimeOffset nowUtc,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        _ = (organizationId, enrollmentId, nowUtc, transaction, cancellationToken);
        return Task.FromResult<IReadOnlyList<RetryEntitlementFact>>([]);
    }
}

public sealed class InMemoryAttemptStore : IAttemptStore
{
    private readonly List<Attempt> _items = [];

    public IReadOnlyList<Attempt> Items => _items;

    public void Restore(IReadOnlyList<Attempt> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    public Task<IReadOnlyList<Attempt>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        _ = transaction;
        _ = cancellationToken;
        return Task.FromResult<IReadOnlyList<Attempt>>(
            _items.Where(item => item.OrganizationId == organizationId && item.EnrollmentId == enrollmentId)
                .OrderBy(item => item.Ordinal)
                .ToArray());
    }

    public Task InsertAsync(
        Attempt attempt,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        _ = transaction;
        _ = cancellationToken;
        _items.Add(attempt);
        return Task.CompletedTask;
    }

    public Task<Attempt?> FindByIdAsync(
        Guid organizationId,
        Guid attemptId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        _ = transaction;
        _ = cancellationToken;
        return Task.FromResult(_items.SingleOrDefault(item =>
            item.OrganizationId == organizationId && item.AttemptId == attemptId));
    }

    public Task UpdateTerminalAsync(
        Attempt attempt,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        _ = transaction;
        _ = cancellationToken;
        var index = _items.FindIndex(item => item.AttemptId == attempt.AttemptId);
        if (index >= 0)
        {
            _items[index] = attempt;
        }

        return Task.CompletedTask;
    }
}

public sealed class InMemoryStartOperationStore : IStartOperationStore
{
    private readonly List<StartOperation> _items = [];

    public IReadOnlyList<StartOperation> Items => _items;

    public void Restore(IReadOnlyList<StartOperation> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    public Task AcquireLockAsync(
        Guid organizationId,
        Guid enrollmentId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<StartOperation?> FindAsync(
        Guid organizationId,
        Guid enrollmentId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        _ = transaction;
        _ = cancellationToken;
        return Task.FromResult(_items.SingleOrDefault(item =>
            item.OrganizationId == organizationId
            && item.EnrollmentId == enrollmentId
            && item.IdempotencyKey == idempotencyKey));
    }

    public Task<IReadOnlyList<StartOperation>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        _ = transaction;
        _ = cancellationToken;
        return Task.FromResult<IReadOnlyList<StartOperation>>(
            _items.Where(item => item.OrganizationId == organizationId && item.EnrollmentId == enrollmentId)
                .ToArray());
    }

    public Task UpsertAsync(
        StartOperation operation,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        _ = transaction;
        _ = cancellationToken;
        var index = _items.FindIndex(item =>
            item.OrganizationId == operation.OrganizationId
            && item.EnrollmentId == operation.EnrollmentId
            && item.IdempotencyKey == operation.IdempotencyKey);
        if (index >= 0)
        {
            var existing = _items[index];
            if (existing.Status == StartOperationStates.Committed
                && operation.Status == StartOperationStates.Failed)
            {
                return Task.CompletedTask;
            }

            _items[index] = operation;
        }
        else
        {
            _items.Add(operation);
        }

        return Task.CompletedTask;
    }
}
