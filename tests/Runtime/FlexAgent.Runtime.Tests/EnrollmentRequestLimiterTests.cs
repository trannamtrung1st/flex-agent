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

        Assert.True(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Read));
        Assert.True(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Read));
        Assert.False(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Read));
        Assert.True(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Mutation));
        Assert.False(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Mutation));
    }

    [Fact]
    public void Other_actors_and_organizations_keep_independent_quotas()
    {
        var limiter = CreateLimiter(readLimit: 1, mutationLimit: 1);
        var organizationId = Guid.CreateVersion7();
        var otherOrganizationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var otherActorId = Guid.CreateVersion7();

        Assert.True(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Read));
        Assert.False(limiter.TryAcquire(organizationId, actorId, EnrollmentRequestSurfaces.Read));
        Assert.True(limiter.TryAcquire(organizationId, otherActorId, EnrollmentRequestSurfaces.Read));
        Assert.True(limiter.TryAcquire(otherOrganizationId, actorId, EnrollmentRequestSurfaces.Read));
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
            WindowSeconds = 60,
        }));
}
