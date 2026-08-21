using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class AssessmentHostEnvironmentTests
{
    [Theory]
    [InlineData("Production", DeploymentEnvironments.Production)]
    [InlineData("Staging", DeploymentEnvironments.Production)]
    [InlineData("Development", DeploymentEnvironments.Development)]
    [InlineData("Testing", DeploymentEnvironments.Development)]
    public void Staging_is_classified_with_production(string aspNetCore, string expected)
    {
        Assert.Equal(expected, AssessmentHostEnvironment.FromAspNetCore(aspNetCore));
    }
}
