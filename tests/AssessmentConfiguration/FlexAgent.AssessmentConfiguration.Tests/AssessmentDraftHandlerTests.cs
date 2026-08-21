using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class AssessmentDraftHandlerTests
{
    [Fact]
    public async Task Create_stores_a_findable_empty_cohort_for_the_activity()
    {
        var store = new InMemoryAssessmentDraftStore();
        var catalog = new InMemoryAssessmentSourceCatalog();
        catalog.EnsureOrganization(AssessmentFixtures.OrganizationId);
        var handler = new AssessmentDraftHandler(new InMemoryAssessmentAuthorizationPort(), catalog, store);
        var actor = new AssessmentActorContext(
            new TrustedActor(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "human.interactive"),
            new OrganizationScope(AssessmentFixtures.OrganizationId),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            "https");

        var created = await handler.CreateAsync(
            new CreateAssessmentDraftCommand(
                actor,
                "P0 Assessment",
                AssessmentFixtures.ValidTask(),
                AssessmentFixtures.ValidTiming(),
                AssessmentDevelopmentSources.OrganizationPolicy,
                AssessmentDevelopmentSources.Agent,
                AssessmentDevelopmentSources.Harness,
                AssessmentDevelopmentSources.Workflow,
                AssessmentDevelopmentSources.AdaptiveFollowUp,
                AssessmentDevelopmentSources.Rubric,
                AssessmentDevelopmentSources.ModelDeployment,
                [AssessmentDevelopmentSources.Knowledge],
                AssessmentDevelopmentSources.Capability,
                AssessmentDevelopmentSources.ReviewRelease),
            TestContext.Current.CancellationToken);

        Assert.True(created.Succeeded);
        var listed = await store.ListDraftsAsync(AssessmentFixtures.OrganizationId, TestContext.Current.CancellationToken);
        var cohort = await store.FindCohortForActivityAsync(
            AssessmentFixtures.OrganizationId,
            created.Value!.ActivityId,
            TestContext.Current.CancellationToken);
        var readiness = await handler.CheckReadinessAsync(
            new CheckReadinessQuery(actor, created.Value.ActivityId, DeploymentEnvironments.Development),
            TestContext.Current.CancellationToken);

        Assert.Single(listed);
        Assert.NotNull(cohort);
        Assert.Equal(CohortStates.Draft, cohort!.State);
        Assert.Equal(ReadinessSeverities.Ready, readiness.Value!.OverallSeverity);
    }
}
