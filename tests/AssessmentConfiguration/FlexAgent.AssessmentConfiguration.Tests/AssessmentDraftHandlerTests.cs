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
        var handler = new AssessmentDraftHandler(
            new InMemoryAssessmentAuthorizationPort(),
            catalog,
            store,
            new InMemoryAssessmentUnitOfWork());
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
                AssessmentDevelopmentSources.ReviewRelease,
                DeploymentEnvironments.Development),
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

    [Fact]
    public async Task Concurrent_save_against_a_moved_head_is_stale()
    {
        var store = new InMemoryAssessmentDraftStore();
        var catalog = new InMemoryAssessmentSourceCatalog();
        catalog.EnsureOrganization(AssessmentFixtures.OrganizationId);
        var handler = new AssessmentDraftHandler(
            new InMemoryAssessmentAuthorizationPort(),
            catalog,
            store,
            new InMemoryAssessmentUnitOfWork());
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
                AssessmentDevelopmentSources.ReviewRelease,
                DeploymentEnvironments.Development),
            TestContext.Current.CancellationToken);
        Assert.True(created.Succeeded);
        var firstSave = await handler.SaveAsync(
            new SaveAssessmentDraftCommand(actor, created.Value!.ActivityId, 1, created.Value.Content with { Title = "First" }, DeploymentEnvironments.Development),
            TestContext.Current.CancellationToken);
        var stale = await handler.SaveAsync(
            new SaveAssessmentDraftCommand(actor, created.Value.ActivityId, 1, created.Value.Content with { Title = "Second" }, DeploymentEnvironments.Development),
            TestContext.Current.CancellationToken);

        Assert.True(firstSave.Succeeded);
        Assert.False(stale.Succeeded);
        Assert.Equal(AssessmentFailureCodes.StaleRevision, stale.OutcomeCode);
        Assert.Equal(2, firstSave.Value!.RevisionNumber);
    }

    [Fact]
    public async Task Save_reauthorization_denial_does_not_persist_a_revision()
    {
        var store = new InMemoryAssessmentDraftStore();
        var catalog = new InMemoryAssessmentSourceCatalog();
        catalog.EnsureOrganization(AssessmentFixtures.OrganizationId);
        var authorization = new InMemoryAssessmentAuthorizationPort();
        var handler = new AssessmentDraftHandler(authorization, catalog, store, new InMemoryAssessmentUnitOfWork());
        var actor = new AssessmentActorContext(
            new TrustedActor(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "human.interactive"),
            new OrganizationScope(AssessmentFixtures.OrganizationId),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            "https");
        var created = await handler.CreateAsync(ValidCreate(actor), TestContext.Current.CancellationToken);
        Assert.True(created.Succeeded, created.OutcomeCode);
        authorization.DeniedOnReauthorize.Add(AssessmentAuthorizationActions.SaveActivity);

        var saved = await handler.SaveAsync(
            new SaveAssessmentDraftCommand(
                actor,
                created.Value!.ActivityId,
                created.Value.RevisionNumber,
                created.Value.Content with { Title = "Revoked" },
                DeploymentEnvironments.Development),
            TestContext.Current.CancellationToken);
        var current = await store.GetDraftAsync(
            AssessmentFixtures.OrganizationId,
            created.Value.ActivityId,
            TestContext.Current.CancellationToken);

        Assert.Equal(AssessmentFailureCodes.Denied, saved.OutcomeCode);
        Assert.Equal(created.Value.RevisionNumber, current!.RevisionNumber);
        Assert.Equal(created.Value.Content.Title, current.Content.Title);
    }

    [Fact]
    public async Task Create_rejects_a_forged_source_without_storing_a_draft()
    {
        var store = new InMemoryAssessmentDraftStore();
        var catalog = new InMemoryAssessmentSourceCatalog();
        catalog.EnsureOrganization(AssessmentFixtures.OrganizationId);
        var handler = new AssessmentDraftHandler(
            new InMemoryAssessmentAuthorizationPort(),
            catalog,
            store,
            new InMemoryAssessmentUnitOfWork());
        var actor = new AssessmentActorContext(
            new TrustedActor(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "human.interactive"),
            new OrganizationScope(AssessmentFixtures.OrganizationId),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            "https");
        var forged = new ExactSourceRef(Guid.CreateVersion7(), Guid.CreateVersion7(), AssessmentFixtures.Digest('f'));

        var created = await handler.CreateAsync(
            new CreateAssessmentDraftCommand(
                actor,
                "P0 Assessment",
                AssessmentFixtures.ValidTask(),
                AssessmentFixtures.ValidTiming(),
                forged,
                AssessmentDevelopmentSources.Agent,
                AssessmentDevelopmentSources.Harness,
                AssessmentDevelopmentSources.Workflow,
                AssessmentDevelopmentSources.AdaptiveFollowUp,
                AssessmentDevelopmentSources.Rubric,
                AssessmentDevelopmentSources.ModelDeployment,
                [AssessmentDevelopmentSources.Knowledge],
                AssessmentDevelopmentSources.Capability,
                AssessmentDevelopmentSources.ReviewRelease,
                DeploymentEnvironments.Development),
            TestContext.Current.CancellationToken);
        var listed = await store.ListDraftsAsync(AssessmentFixtures.OrganizationId, TestContext.Current.CancellationToken);

        Assert.False(created.Succeeded);
        Assert.Equal(AssessmentFailureCodes.MissingSource, created.OutcomeCode);
        Assert.Empty(listed);
    }

    [Fact]
    public async Task Create_requires_select_sources_and_rejects_wrong_kind_or_production_ineligible_sources()
    {
        var store = new InMemoryAssessmentDraftStore();
        var catalog = new InMemoryAssessmentSourceCatalog();
        catalog.EnsureOrganization(AssessmentFixtures.OrganizationId);
        var authorization = new InMemoryAssessmentAuthorizationPort();
        var handler = new AssessmentDraftHandler(authorization, catalog, store, new InMemoryAssessmentUnitOfWork());
        var actor = new AssessmentActorContext(
            new TrustedActor(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "human.interactive"),
            new OrganizationScope(AssessmentFixtures.OrganizationId),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            "https");

        authorization.DeniedActions.Add(AssessmentAuthorizationActions.SelectSources);
        var deniedSelect = await handler.CreateAsync(ValidCreate(actor), TestContext.Current.CancellationToken);
        authorization.DeniedActions.Clear();

        var wrongKind = await handler.CreateAsync(
            ValidCreate(actor) with { Agent = AssessmentDevelopmentSources.Harness },
            TestContext.Current.CancellationToken);
        var production = await handler.CreateAsync(
            ValidCreate(actor) with { Environment = DeploymentEnvironments.Production },
            TestContext.Current.CancellationToken);

        Assert.Equal(AssessmentFailureCodes.Denied, deniedSelect.OutcomeCode);
        Assert.Equal(AssessmentFailureCodes.Incompatible, wrongKind.OutcomeCode);
        Assert.Equal(AssessmentFailureCodes.MissingSource, production.OutcomeCode);
        Assert.Empty(await store.ListDraftsAsync(AssessmentFixtures.OrganizationId, TestContext.Current.CancellationToken));
    }

    private static CreateAssessmentDraftCommand ValidCreate(AssessmentActorContext actor) =>
        new(
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
            AssessmentDevelopmentSources.ReviewRelease,
            DeploymentEnvironments.Development);

    [Fact]
    public async Task List_and_get_require_mfa_and_read_permission()
    {
        var store = new InMemoryAssessmentDraftStore();
        var catalog = new InMemoryAssessmentSourceCatalog();
        catalog.EnsureOrganization(AssessmentFixtures.OrganizationId);
        var handler = new AssessmentDraftHandler(
            new InMemoryAssessmentAuthorizationPort(),
            catalog,
            store,
            new InMemoryAssessmentUnitOfWork());
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
                AssessmentDevelopmentSources.ReviewRelease,
                DeploymentEnvironments.Development),
            TestContext.Current.CancellationToken);
        Assert.True(created.Succeeded);

        var listed = await handler.ListActivitiesAsync(actor, TestContext.Current.CancellationToken);
        var fetched = await handler.GetActivityAsync(actor, created.Value!.ActivityId, TestContext.Current.CancellationToken);
        var withoutMfa = await handler.ListActivitiesAsync(
            actor with { Strength = new AuthenticationStrength(null, []) },
            TestContext.Current.CancellationToken);
        var createOnly = AssessmentDraftProjection.PermittedActions(
            [AssessmentAuthorizationActions.CreateActivity],
            created.Value.HasActivatedCohort);

        Assert.True(listed.Succeeded);
        Assert.True(fetched.Succeeded);
        Assert.Equal(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, withoutMfa.OutcomeCode);
        Assert.Empty(createOnly);
    }

    [Fact]
    public async Task Source_options_require_administrator_and_exclude_ineligible_production_sources()
    {
        var store = new InMemoryAssessmentDraftStore();
        var catalog = new InMemoryAssessmentSourceCatalog(AssessmentFixtures.PermittedSources());
        var handler = new AssessmentDraftHandler(
            new InMemoryAssessmentAuthorizationPort(),
            catalog,
            store,
            new InMemoryAssessmentUnitOfWork());
        var actor = new AssessmentActorContext(
            new TrustedActor(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "human.interactive"),
            new OrganizationScope(AssessmentFixtures.OrganizationId),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            "https");

        var development = await handler.ListSourceOptionsAsync(actor, DeploymentEnvironments.Development, TestContext.Current.CancellationToken);
        var production = await handler.ListSourceOptionsAsync(actor, DeploymentEnvironments.Production, TestContext.Current.CancellationToken);
        var participant = await handler.ListSourceOptionsAsync(
            actor with { Relationship = "participant" },
            DeploymentEnvironments.Development,
            TestContext.Current.CancellationToken);

        Assert.True(development.Succeeded);
        Assert.NotEmpty(development.Value!);
        Assert.True(production.Succeeded);
        Assert.Empty(production.Value!);
        Assert.Equal(AssessmentFailureCodes.Denied, participant.OutcomeCode);
    }
}
