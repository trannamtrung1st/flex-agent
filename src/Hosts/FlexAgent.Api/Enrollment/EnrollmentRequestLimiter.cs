using System.Collections.Concurrent;
using FlexAgent.Submissions.Domain;
using Microsoft.Extensions.Options;

namespace FlexAgent.Api;

public sealed class EnrollmentRequestLimitOptions
{
    public int ReadPermitLimit { get; set; } = EnrollmentRequestLimitDefaults.ReadPermitLimit;

    public int MutationPermitLimit { get; set; } = EnrollmentRequestLimitDefaults.MutationPermitLimit;

    public int WindowSeconds { get; set; } = EnrollmentRequestLimitDefaults.WindowSeconds;

    public static bool MeetsFrozenCeiling(EnrollmentRequestLimitOptions options) =>
        options.ReadPermitLimit is >= 1 and <= EnrollmentRequestLimitDefaults.ReadPermitLimit
        && options.MutationPermitLimit is >= 1 and <= EnrollmentRequestLimitDefaults.MutationPermitLimit
        && options.WindowSeconds >= EnrollmentRequestLimitDefaults.WindowSeconds;

    public static void EnsureFrozenCeiling(EnrollmentRequestLimitOptions options)
    {
        if (!MeetsFrozenCeiling(options))
        {
            throw new InvalidOperationException(
                "Enrollment request limits may only be tightened. Reads must be 1–60, mutations 1–20, and the window at least 10 seconds.");
        }
    }
}

public readonly record struct EnrollmentRequestAdmission(bool Permitted, int RetryAfterSeconds);

public interface IEnrollmentRequestLimiter
{
    EnrollmentRequestAdmission TryAcquire(Guid organizationId, Guid actorId, string surface);
}

public sealed class FixedWindowEnrollmentRequestLimiter : IEnrollmentRequestLimiter
{
    private readonly ConcurrentDictionary<PartitionKey, WindowCounter> _windows = new();
    private readonly EnrollmentRequestLimitOptions _options;

    public FixedWindowEnrollmentRequestLimiter(IOptions<EnrollmentRequestLimitOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        EnrollmentRequestLimitOptions.EnsureFrozenCeiling(options.Value);
        _options = options.Value;
    }

    public EnrollmentRequestAdmission TryAcquire(Guid organizationId, Guid actorId, string surface)
    {
        var limit = string.Equals(surface, EnrollmentRequestSurfaces.Mutation, StringComparison.Ordinal)
            ? _options.MutationPermitLimit
            : _options.ReadPermitLimit;
        var windowSeconds = _options.WindowSeconds;
        var nowTicks = DateTimeOffset.UtcNow.UtcTicks;
        var windowTicks = TimeSpan.TicksPerSecond * windowSeconds;
        var windowStart = nowTicks / windowTicks * windowTicks;
        var retryAfter = Math.Max(1, (int)Math.Ceiling((windowStart + windowTicks - nowTicks) / (double)TimeSpan.TicksPerSecond));
        if (_windows.Count >= EnrollmentRequestLimitDefaults.MaximumTrackedPartitions)
        {
            Sweep(nowTicks - windowTicks);
            if (_windows.Count >= EnrollmentRequestLimitDefaults.MaximumTrackedPartitions)
            {
                return new EnrollmentRequestAdmission(false, retryAfter);
            }
        }

        var key = new PartitionKey(organizationId, actorId, surface, windowStart);
        var window = _windows.GetOrAdd(key, static _ => new WindowCounter());
        var permitted = Interlocked.Increment(ref window.Count) <= limit;
        return new EnrollmentRequestAdmission(permitted, permitted ? 0 : retryAfter);
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
