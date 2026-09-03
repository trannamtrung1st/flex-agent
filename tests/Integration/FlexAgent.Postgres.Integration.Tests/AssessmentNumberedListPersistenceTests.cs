using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.AssessmentConfiguration.Infrastructure;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class AssessmentNumberedListPersistenceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Numbered_page_excludes_other_organizations_and_treats_wildcards_literally()
    {
        var store = new PostgresAssessmentDraftStore(Fixture.Services.ConnectionAccessor, new PostgresAuditEventWriter());
        var visible = await Fixture.SeedOrganizationAsync();
        var hidden = await Fixture.SeedOrganizationAsync();
        await SeedDraftAsync(store, visible.OrganizationId, Guid.Parse("10000000-0000-0000-0000-000000000001"), "Alpha 100%");
        await SeedDraftAsync(store, visible.OrganizationId, Guid.Parse("10000000-0000-0000-0000-000000000002"), "Beta");
        await SeedDraftAsync(store, hidden.OrganizationId, Guid.Parse("20000000-0000-0000-0000-000000000099"), "Alpha 100% hidden");

        var specified = NumberedActivityListSpecification.TryCreate(
            new NumberedActivityListRequest(1, 16, "100%", [new ActivityListSortTerm("title", "asc")]));
        Assert.True(specified.Succeeded);
        var page = await store.ListNumberedPageAsync(visible.OrganizationId, specified.Value!, CancellationToken);
        var unbounded = await store.ListDraftsAsync(visible.OrganizationId, CancellationToken);

        Assert.Equal(1, page.TotalItems);
        Assert.Equal(["Alpha 100%"], page.Items.Select(item => item.Content.Title).ToArray());
        Assert.Equal(2, unbounded.Count);
        Assert.DoesNotContain(unbounded, item => item.OrganizationId != visible.OrganizationId);
        Assert.DoesNotContain(page.Items, item => item.OrganizationId != visible.OrganizationId);
    }

    [Fact]
    public async Task Numbered_page_applies_activity_id_tie_break_and_returns_empty_out_of_range_metadata()
    {
        var store = new PostgresAssessmentDraftStore(Fixture.Services.ConnectionAccessor, new PostgresAuditEventWriter());
        var seeded = await Fixture.SeedOrganizationAsync();
        await SeedDraftAsync(store, seeded.OrganizationId, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "Same");
        await SeedDraftAsync(store, seeded.OrganizationId, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "Same");
        await SeedDraftAsync(store, seeded.OrganizationId, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "Same");
        var firstRequest = NumberedActivityListSpecification.TryCreate(
            new NumberedActivityListRequest(1, 2, null, [new ActivityListSortTerm("title", "asc")]));
        var driftedRequest = NumberedActivityListSpecification.TryCreate(
            new NumberedActivityListRequest(9, 2, null, null));
        Assert.True(firstRequest.Succeeded);
        Assert.True(driftedRequest.Succeeded);

        var first = await store.ListNumberedPageAsync(seeded.OrganizationId, firstRequest.Value!, CancellationToken);
        var drifted = await store.ListNumberedPageAsync(seeded.OrganizationId, driftedRequest.Value!, CancellationToken);

        Assert.Equal(
            [
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
            ],
            first.Items.Select(item => item.ActivityId).ToArray());
        Assert.Empty(drifted.Items);
        Assert.Equal(3, drifted.TotalItems);
        Assert.Equal(2, drifted.TotalPages);
        Assert.Equal(9, drifted.Page);
    }

    [Fact]
    public async Task Extreme_valid_page_returns_empty_metadata_without_offset_overflow()
    {
        var store = new PostgresAssessmentDraftStore(Fixture.Services.ConnectionAccessor, new PostgresAuditEventWriter());
        var seeded = await Fixture.SeedOrganizationAsync();
        await SeedDraftAsync(store, seeded.OrganizationId, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "Alpha");
        var request = NumberedActivityListSpecification.TryCreate(
            new NumberedActivityListRequest(int.MaxValue, 50, null, null));
        Assert.True(request.Succeeded);

        var page = await store.ListNumberedPageAsync(seeded.OrganizationId, request.Value!, CancellationToken);

        Assert.Empty(page.Items);
        Assert.Equal(int.MaxValue, page.Page);
        Assert.Equal(50, page.PageSize);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(1, page.TotalPages);
    }

    private async Task SeedDraftAsync(PostgresAssessmentDraftStore store, Guid organizationId, Guid activityId, string title)
    {
        var created = ActivityDraft.Create(
            organizationId,
            activityId,
            Guid.CreateVersion7(),
            title,
            new TaskBinding(Guid.CreateVersion7(), "Task 1", "Submit one written response", AssessmentDevelopmentSources.TaskRequirement),
            new TimingRules(
                new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 30, 17, 0, 0, TimeSpan.Zero),
                "UTC",
                2,
                3600,
                900,
                300),
            AssessmentDevelopmentSources.OrganizationPolicy,
            AssessmentDevelopmentSources.Agent,
            AssessmentDevelopmentSources.Harness,
            AssessmentDevelopmentSources.Workflow,
            AssessmentDevelopmentSources.AdaptiveFollowUp,
            AssessmentDevelopmentSources.Rubric,
            AssessmentDevelopmentSources.ModelDeployment,
            [AssessmentDevelopmentSources.Knowledge],
            AssessmentDevelopmentSources.Capability,
            AssessmentDevelopmentSources.ReviewRelease);
        Assert.True(created.Succeeded, created.OutcomeCode);
        var cohort = AssessmentCohort.CreateEmpty(
            organizationId,
            activityId,
            Guid.CreateVersion7(),
            created.Value!.RevisionId,
            created.Value.RevisionNumber);
        Assert.True(cohort.Succeeded);
        var actor = new AssessmentActorContext(
            new TrustedActor(Guid.CreateVersion7(), "human.interactive"),
            new OrganizationScope(organizationId),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.CreateVersion7(),
            "https");
        await store.AddAsync(
            created.Value,
            cohort.Value!,
            transaction: null,
            new AssessmentRevisionProvenance(actor, null, AssessmentRevisionChangeCategories.Created, AuthorizationDecision.Permit(1)),
            CancellationToken);
    }
}
