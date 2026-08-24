using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Api;

public sealed class EnvironmentMaterialPolicyPort(IHostEnvironment environment) : IMaterialPolicyPort
{
    public Task<NormalizedMaterialPolicy?> ResolveCurrentAsync(
        Guid organizationId,
        PolicySourceRef frozenOrganizationPolicyRef,
        DateTimeOffset nowUtc,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        if (environment.IsProduction() || environment.IsEnvironment("Staging"))
        {
            return Task.FromResult<NormalizedMaterialPolicy?>(null);
        }

        _ = (organizationId, nowUtc, transaction, cancellationToken);
        return Task.FromResult<NormalizedMaterialPolicy?>(
            DevelopmentMaterialPolicy.OrganizationPolicy(frozenOrganizationPolicyRef));
    }
}
