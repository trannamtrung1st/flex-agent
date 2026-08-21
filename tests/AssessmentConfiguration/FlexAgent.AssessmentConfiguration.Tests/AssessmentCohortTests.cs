using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class AssessmentCohortTests
{
    [Fact]
    public void Empty_cohort_can_be_created_in_draft_state()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var result = AssessmentCohort.CreateEmpty(
            draft.OrganizationId,
            draft.ActivityId,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            draft.RevisionId,
            draft.RevisionNumber);

        Assert.True(result.Succeeded);
        Assert.Equal(CohortStates.Draft, result.Value!.State);
        Assert.Null(result.Value.BaselineId);
    }

    [Fact]
    public void Activation_binds_one_baseline_and_rejects_later_mutation()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var cohort = AssessmentCohort.CreateEmpty(
            draft.OrganizationId,
            draft.ActivityId,
            Guid.NewGuid(),
            draft.RevisionId,
            draft.RevisionNumber).Value!;
        var baselineId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var digest = AssessmentFixtures.Digest('b');

        var activated = cohort.BindActivation(draft.RevisionId, draft.RevisionNumber, baselineId, digest);

        Assert.True(activated.Succeeded);
        Assert.Equal(CohortStates.Activated, activated.Value!.State);
        Assert.Equal(baselineId, activated.Value.BaselineId);

        var equivalent = activated.Value.BindActivation(draft.RevisionId, draft.RevisionNumber, baselineId, digest);
        Assert.True(equivalent.Succeeded);

        var conflicting = activated.Value.BindActivation(
            draft.RevisionId,
            draft.RevisionNumber,
            Guid.NewGuid(),
            AssessmentFixtures.Digest('c'));
        Assert.Equal(AssessmentFailureCodes.Immutable, conflicting.OutcomeCode);

        Assert.Equal(AssessmentFailureCodes.NewCohortRequired, activated.Value.RejectMaterialMutation().OutcomeCode);
    }

    [Fact]
    public void Activation_rejects_stale_expected_revision()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var cohort = AssessmentCohort.CreateEmpty(
            draft.OrganizationId,
            draft.ActivityId,
            Guid.NewGuid(),
            draft.RevisionId,
            draft.RevisionNumber).Value!;

        var result = cohort.BindActivation(Guid.NewGuid(), draft.RevisionNumber, Guid.NewGuid(), AssessmentFixtures.Digest('d'));

        Assert.Equal(AssessmentFailureCodes.StaleRevision, result.OutcomeCode);
        Assert.Equal(CohortStates.Draft, cohort.State);
    }
}
