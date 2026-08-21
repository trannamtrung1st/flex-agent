using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class AssessmentHttpStatusTests
{
    [Theory]
    [InlineData(AssessmentFailureCodes.Denied, 403)]
    [InlineData(HumanAuthenticationReasonCodes.UnrecognizedAuthenticationStrength, 403)]
    [InlineData(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, 403)]
    [InlineData(AssessmentFailureCodes.InvalidField, 400)]
    [InlineData(AssessmentFailureCodes.MissingSource, 400)]
    [InlineData(AssessmentFailureCodes.StaleRevision, 409)]
    public void Draft_mutation_failures_map_access_invalid_and_conflict_statuses(string outcomeCode, int expected)
    {
        Assert.Equal(expected, AssessmentHttpStatus.ForDraftMutation(false, outcomeCode));
    }

    [Fact]
    public void Successful_create_uses_the_caller_success_status()
    {
        Assert.Equal(201, AssessmentHttpStatus.ForDraftMutation(true, "assessment.ok", 201));
    }
}
