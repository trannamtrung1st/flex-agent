using FlexAgent.IdentityAccess.Application;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal sealed class SyntheticConfiguredActorWorkloadIdentitySource(Guid serviceActorId)
    : IAuthenticatedWorkloadContextSource
{
    public Task<AuthenticatedWorkloadContext?> TryGetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return Task.FromResult<AuthenticatedWorkloadContext?>(
            new AuthenticatedWorkloadContext(
                WorkloadIdentityProfiles.SyntheticConfiguredActor,
                WorkloadAuthenticationMethods.ConfiguredActor,
                "flex-agent.synthetic",
                serviceActorId.ToString("D"),
                null,
                "flex-agent.worker",
                now,
                now,
                now.AddHours(1),
                now,
                serviceActorId,
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                1,
                "synthetic"));
    }
}

internal sealed class CachedOAuthWorkloadIdentitySource(
    Guid actorId,
    Guid bindingId,
    long bindingVersion) : IAuthenticatedWorkloadContextSource
{
    public Task<AuthenticatedWorkloadContext?> TryGetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return Task.FromResult<AuthenticatedWorkloadContext?>(
            new AuthenticatedWorkloadContext(
                WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                "https://issuer.example/realms/flex-agent",
                "worker-client",
                "worker-client",
                "flex-agent-worker",
                now,
                now,
                now.AddMinutes(5),
                now,
                actorId,
                bindingId,
                bindingVersion,
                "cached"));
    }
}
