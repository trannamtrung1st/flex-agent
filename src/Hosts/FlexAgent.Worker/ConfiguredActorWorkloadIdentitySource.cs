using FlexAgent.IdentityAccess.Application;

namespace FlexAgent.Worker;

internal sealed class ConfiguredActorWorkloadIdentitySource(
    Guid serviceActorId) : IAuthenticatedWorkloadContextSource
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

internal sealed class StaticUnavailableWorkloadIdentitySource : IAuthenticatedWorkloadContextSource
{
    public static StaticUnavailableWorkloadIdentitySource Instance { get; } = new();

    public Task<AuthenticatedWorkloadContext?> TryGetCurrentAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AuthenticatedWorkloadContext?>(null);
}
