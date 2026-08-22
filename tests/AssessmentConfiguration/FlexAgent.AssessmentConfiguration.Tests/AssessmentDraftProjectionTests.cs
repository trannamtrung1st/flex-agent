using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class AssessmentDraftProjectionTests
{
    [Fact]
    public void Activated_cohort_exposes_assign_participants_only_when_granted()
    {
        Assert.Empty(AssessmentDraftProjection.PermittedActions(
            [AssessmentAuthorizationActions.ActivateCohort],
            hasActivatedCohort: true));
        Assert.Equal(
            ["assign_participants"],
            AssessmentDraftProjection.PermittedActions(
                ["assessment.enrollment.assign"],
                hasActivatedCohort: true));
    }
}
