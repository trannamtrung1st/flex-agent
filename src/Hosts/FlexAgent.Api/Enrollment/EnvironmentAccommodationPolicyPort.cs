using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Api;

public sealed class EnvironmentAccommodationPolicyPort(IHostEnvironment environment) : IAccommodationPolicyPort
{
    public Task<NormalizedAccommodationPolicy?> ResolveCurrentAsync(
        Guid organizationId,
        BaselineTiming baseline,
        DateTimeOffset nowUtc,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        if (environment.IsProduction() || environment.IsEnvironment("Staging"))
        {
            return Task.FromResult<NormalizedAccommodationPolicy?>(null);
        }

        return Task.FromResult<NormalizedAccommodationPolicy?>(
            DevelopmentAccommodationPolicy.Create(organizationId, baseline, environment.EnvironmentName));
    }
}
