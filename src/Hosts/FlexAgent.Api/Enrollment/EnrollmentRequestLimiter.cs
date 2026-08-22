using System.Collections.Concurrent;
using FlexAgent.Submissions.Domain;
using Microsoft.Extensions.Options;

namespace FlexAgent.Api;

public sealed class EnrollmentRequestLimitOptions
{
    public int ReadPermitLimit { get; set; } = EnrollmentRequestLimitDefaults.ReadPermitLimit;

    public int MutationPermitLimit { get; set; } = EnrollmentRequestLimitDefaults.MutationPermitLimit;

    public int WindowSeconds { get; set; } = EnrollmentRequestLimitDefaults.WindowSeconds;
}

public interface IEnrollmentRequestLimiter
{
    bool TryAcquire(Guid organizationId, Guid actorId, string surface);
}

public sealed class FixedWindowEnrollmentRequestLimiter(IOptions<EnrollmentRequestLimitOptions> options)
    : IEnrollmentRequestLimiter
{
    private readonly ConcurrentDictionary<PartitionKey, WindowCounter> _windows = new();

    public bool TryAcquire(Guid organizationId, Guid actorId, string surface)
    {
        var configured = options.Value;
        var limit = string.Equals(surface, EnrollmentRequestSurfaces.Mutation, StringComparison.Ordinal)
            ? configured.MutationPermitLimit
            : configured.ReadPermitLimit;
        var windowSeconds = Math.Max(1, configured.WindowSeconds);
        if (limit < 1)
        {
            return false;
        }

        var nowTicks = DateTimeOffset.UtcNow.UtcTicks;
        var windowTicks = TimeSpan.TicksPerSecond * windowSeconds;
        var windowStart = nowTicks / windowTicks * windowTicks;
        if (_windows.Count >= EnrollmentRequestLimitDefaults.MaximumTrackedPartitions)
        {
            Sweep(nowTicks - windowTicks);
            if (_windows.Count >= EnrollmentRequestLimitDefaults.MaximumTrackedPartitions)
            {
                return false;
            }
        }

        var key = new PartitionKey(organizationId, actorId, surface, windowStart);
        var window = _windows.GetOrAdd(key, static _ => new WindowCounter());
        return Interlocked.Increment(ref window.Count) <= limit;
    }

    private void Sweep(long expiredBeforeInclusive)
    {
        foreach (var key in _windows.Keys)
        {
            if (key.WindowStartTicks <= expiredBeforeInclusive)
            {
                _windows.TryRemove(key, out _);
            }
        }
    }

    private readonly record struct PartitionKey(
        Guid OrganizationId,
        Guid ActorId,
        string Surface,
        long WindowStartTicks);

    private sealed class WindowCounter
    {
        public int Count;
    }
}
