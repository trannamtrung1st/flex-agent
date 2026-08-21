using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public static class AssessmentHostEnvironment
{
    public static string FromAspNetCore(string? environmentName) =>
        string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
        || string.Equals(environmentName, "Staging", StringComparison.OrdinalIgnoreCase)
            ? DeploymentEnvironments.Production
            : DeploymentEnvironments.Development;
}
