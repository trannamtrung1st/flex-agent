using FlexAgent.Api;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using Microsoft.Extensions.Options;

namespace FlexAgent.Runtime.Tests;

public sealed class EnrollmentRequestLimiterTests
{
    [Fact]
    public void Same_actor_and_organization_are_limited_per_surface()
    {
        var limiter = CreateLimiter(readLimit: 2, mutationLimit: 1);
        var organizationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();

        Assert.True(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Read).Permitted);
        Assert.True(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Read).Permitted);
        var deniedRead = limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Read);
        Assert.False(deniedRead.Permitted);
        Assert.True(deniedRead.RetryAfterSeconds >= 1);
        Assert.True(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Mutation).Permitted);
        Assert.False(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Mutation).Permitted);
    }

    [Fact]
    public void Other_actors_and_organizations_keep_independent_quotas()
    {
        var limiter = CreateLimiter(readLimit: 1, mutationLimit: 1);
        var organizationId = Guid.CreateVersion7();
        var otherOrganizationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var otherActorId = Guid.CreateVersion7();

        Assert.True(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Read).Permitted);
        Assert.False(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Read).Permitted);
        Assert.True(limiter.TryAcquire(organizationId, otherActorId, EnrollmentRequestSurfaces.Read).Permitted);
        Assert.True(limiter.TryAcquire(otherOrganizationId, actorId, EnrollmentRequestSurfaces.Read).Permitted);
    }

    [Fact]
    public void Configuration_cannot_raise_the_frozen_read_or_mutation_ceiling()
    {
        Assert.Throws<InvalidOperationException>(() => CreateLimiter(readLimit: EnrollmentRequestLimitDefaults.ReadPermitLimit + 1, mutationLimit: 1));
        Assert.Throws<InvalidOperationException>(() => CreateLimiter(readLimit: 1, mutationLimit: EnrollmentRequestLimitDefaults.MutationPermitLimit + 1));
    }

    [Fact]
    public void Configuration_cannot_shorten_the_minimum_window()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new FixedWindowEnrollmentRequestLimiter(Options.Create(new EnrollmentRequestLimitOptions
            {
                ReadPermitLimit = 1,
                MutationPermitLimit = 1,
                WindowSeconds = EnrollmentRequestLimitDefaults.WindowSeconds - 1,
            })));
    }

    [Fact]
    public void Configuration_may_use_a_longer_window_than_the_minimum()
    {
        var limiter = new FixedWindowEnrollmentRequestLimiter(Options.Create(new EnrollmentRequestLimitOptions
        {
            ReadPermitLimit = 1,
            MutationPermitLimit = 1,
            WindowSeconds = EnrollmentRequestLimitDefaults.WindowSeconds + 10,
        }));

        Assert.True(limiter.TryAcquire(Guid.CreateVersion7(), Guid.CreateVersion7(), EnrollmentRequestSurfaces.Read).Permitted);
    }

    [Fact]
    public void Configuration_cannot_use_an_invalid_shared_admission_timeout_or_cleanup_batch()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new FixedWindowEnrollmentRequestLimiter(Options.Create(new EnrollmentRequestLimitOptions
            {
                ReadPermitLimit = 1,
                MutationPermitLimit = 1,
                WindowSeconds = EnrollmentRequestLimitDefaults.WindowSeconds,
                AdmissionTimeoutMilliseconds = 49,
            })));
        Assert.Throws<InvalidOperationException>(() =>
            new FixedWindowEnrollmentRequestLimiter(Options.Create(new EnrollmentRequestLimitOptions
            {
                ReadPermitLimit = 1,
                MutationPermitLimit = 1,
                WindowSeconds = EnrollmentRequestLimitDefaults.WindowSeconds,
                CleanupBatchSize = 0,
            })));
    }

    [Fact]
    public void Request_limit_telemetry_exposes_only_surface_and_decision()
    {
        var telemetry = new RecordingEnrollmentTelemetry();
        telemetry.RecordRequestLimit(EnrollmentRequestSurfaces.Read, EnrollmentTelemetryLabels.Limited);

        Assert.All(telemetry.Points[0], pair =>
        {
            Assert.Contains(pair.Key, EnrollmentTelemetryLabels.AllowedKeys);
            Assert.Contains(pair.Value, EnrollmentTelemetryLabels.AllowedValues);
        });
    }

    private static FixedWindowEnrollmentRequestLimiter CreateLimiter(int readLimit, int mutationLimit) =>
        new(Options.Create(new EnrollmentRequestLimitOptions
        {
            ReadPermitLimit = readLimit,
            MutationPermitLimit = mutationLimit,
            WindowSeconds = EnrollmentRequestLimitDefaults.WindowSeconds,
        }));
}
