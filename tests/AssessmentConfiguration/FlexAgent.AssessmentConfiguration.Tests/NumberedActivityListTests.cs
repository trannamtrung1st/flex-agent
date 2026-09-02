using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class NumberedActivityListTests
{
    [Fact]
    public void Defaults_page_size_and_title_sort_when_the_request_omits_them()
    {
        var created = NumberedActivityListSpecification.TryCreate(new NumberedActivityListRequest(null, null, "  Alpha  ", null));

        Assert.True(created.Succeeded);
        Assert.Equal(1, created.Value!.Page);
        Assert.Equal(16, created.Value.PageSize);
        Assert.Equal("Alpha", created.Value.Search);
        Assert.Equal(
            [new ActivityListSortEntry(ActivityListSortField.Title, ActivityListSortDirection.Asc)],
            created.Value.Sort);
    }

    [Fact]
    public void Rejects_non_positive_page_oversize_page_size_and_overlong_search_without_a_store_call()
    {
        Assert.Equal(AssessmentFailureCodes.InvalidField, NumberedActivityListSpecification.TryCreate(new NumberedActivityListRequest(0, 16, null, null)).OutcomeCode);
        Assert.Equal(AssessmentFailureCodes.InvalidField, NumberedActivityListSpecification.TryCreate(new NumberedActivityListRequest(-1, 16, null, null)).OutcomeCode);
        Assert.Equal(AssessmentFailureCodes.InvalidField, NumberedActivityListSpecification.TryCreate(new NumberedActivityListRequest(1, 0, null, null)).OutcomeCode);
        Assert.Equal(AssessmentFailureCodes.InvalidField, NumberedActivityListSpecification.TryCreate(new NumberedActivityListRequest(1, 51, null, null)).OutcomeCode);
        Assert.Equal(
            AssessmentFailureCodes.InvalidField,
            NumberedActivityListSpecification.TryCreate(new NumberedActivityListRequest(1, 16, new string('a', 201), null)).OutcomeCode);
    }

    [Fact]
    public void Rejects_duplicate_unknown_over_limit_and_invalid_direction_sort_entries()
    {
        Assert.Equal(
            AssessmentFailureCodes.InvalidField,
            NumberedActivityListSpecification.TryCreate(new NumberedActivityListRequest(
                1,
                16,
                null,
                [
                    new ActivityListSortTerm("title", "asc"),
                    new ActivityListSortTerm("title", "desc"),
                ])).OutcomeCode);
        Assert.Equal(
            AssessmentFailureCodes.InvalidField,
            NumberedActivityListSpecification.TryCreate(new NumberedActivityListRequest(
                1,
                16,
                null,
                [new ActivityListSortTerm("score", "asc")])).OutcomeCode);
        Assert.Equal(
            AssessmentFailureCodes.InvalidField,
            NumberedActivityListSpecification.TryCreate(new NumberedActivityListRequest(
                1,
                16,
                null,
                [new ActivityListSortTerm("title", "up")])).OutcomeCode);
        Assert.Equal(
            AssessmentFailureCodes.InvalidField,
            NumberedActivityListSpecification.TryCreate(new NumberedActivityListRequest(
                1,
                16,
                null,
                [
                    new ActivityListSortTerm("title", "asc"),
                    new ActivityListSortTerm("activation", "desc"),
                    new ActivityListSortTerm("updated", "asc"),
                    new ActivityListSortTerm("revision", "desc"),
                    new ActivityListSortTerm("title", "asc"),
                ])).OutcomeCode);
    }

    [Fact]
    public async Task Handler_does_not_query_the_store_after_invalid_input_or_denied_authorization()
    {
        var store = new InMemoryAssessmentDraftStore();
        var handler = Handler(store);
        var invalid = await handler.ListActivitiesPageAsync(
            Actor(),
            new NumberedActivityListRequest(0, 16, null, null),
            TestContext.Current.CancellationToken);
        var denied = await handler.ListActivitiesPageAsync(
            Actor() with { Strength = new AuthenticationStrength(null, []) },
            new NumberedActivityListRequest(1, 16, null, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(AssessmentFailureCodes.InvalidField, invalid.OutcomeCode);
        Assert.Equal(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, denied.OutcomeCode);
        Assert.Equal(0, store.ListNumberedPageCallCount);
    }

    [Fact]
    public async Task Returns_the_requested_page_with_authorized_totals_and_literal_wildcard_search()
    {
        var store = new InMemoryAssessmentDraftStore();
        await SeedAsync(
            store,
            Draft("10000000-0000-0000-0000-000000000001", "Alpha 100%", false, 1, 1),
            Draft("10000000-0000-0000-0000-000000000002", "Beta", true, 3, 2),
            Draft("10000000-0000-0000-0000-000000000003", "alpha zest", false, 2, 3));
        var other = Draft(
            "20000000-0000-0000-0000-000000000099",
            "Alpha other",
            false,
            1,
            4,
            Guid.Parse("22222222-2222-2222-2222-222222222222"));
        await SeedAsync(store, other);
        var handler = Handler(store);

        var page = await handler.ListActivitiesPageAsync(
            Actor(),
            new NumberedActivityListRequest(
                1,
                2,
                "100%",
                [new ActivityListSortTerm("title", "asc")]),
            TestContext.Current.CancellationToken);

        Assert.True(page.Succeeded);
        Assert.Equal(1, page.Value!.TotalItems);
        Assert.Equal(1, page.Value.TotalPages);
        Assert.Equal(["Alpha 100%"], page.Value.Items.Select(item => item.Content.Title).ToArray());
        Assert.DoesNotContain(page.Value.Items, item => item.OrganizationId != AssessmentFixtures.OrganizationId);
    }

    [Fact]
    public async Task Orders_equal_titles_by_activity_id_and_returns_empty_out_of_range_metadata()
    {
        var store = new InMemoryAssessmentDraftStore();
        await SeedAsync(
            store,
            Draft("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2", "Same", false, 1, 2),
            Draft("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1", "Same", false, 1, 1),
            Draft("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3", "Same", false, 1, 3));
        var handler = Handler(store);

        var first = await handler.ListActivitiesPageAsync(
            Actor(),
            new NumberedActivityListRequest(
                1,
                2,
                null,
                [new ActivityListSortTerm("title", "asc")]),
            TestContext.Current.CancellationToken);
        var drifted = await handler.ListActivitiesPageAsync(
            Actor(),
            new NumberedActivityListRequest(9, 2, null, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
            ],
            first.Value!.Items.Select(item => item.ActivityId).ToArray());
        Assert.Empty(drifted.Value!.Items);
        Assert.Equal(9, drifted.Value.Page);
        Assert.Equal(3, drifted.Value.TotalItems);
        Assert.Equal(2, drifted.Value.TotalPages);
    }

    [Fact]
    public async Task Activation_ascending_places_draft_before_activated()
    {
        var store = new InMemoryAssessmentDraftStore();
        await SeedAsync(
            store,
            Draft("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa11", "Live", true, 2, 2),
            Draft("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10", "Draft", false, 1, 1));
        var handler = Handler(store);

        var page = await handler.ListActivitiesPageAsync(
            Actor(),
            new NumberedActivityListRequest(
                1,
                16,
                null,
                [new ActivityListSortTerm("activation", "asc")]),
            TestContext.Current.CancellationToken);

        Assert.Equal(["Draft", "Live"], page.Value!.Items.Select(item => item.Content.Title).ToArray());
    }

    private static AssessmentDraftHandler Handler(InMemoryAssessmentDraftStore store)
    {
        var catalog = new InMemoryAssessmentSourceCatalog();
        catalog.EnsureOrganization(AssessmentFixtures.OrganizationId);
        return new AssessmentDraftHandler(
            new InMemoryAssessmentAuthorizationPort(),
            catalog,
            store,
            new InMemoryAssessmentUnitOfWork());
    }

    private static AssessmentActorContext Actor() =>
        new(
            new TrustedActor(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "human.interactive"),
            new OrganizationScope(AssessmentFixtures.OrganizationId),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            "https");

    private static ActivityDraft Draft(
        string activityId,
        string title,
        bool activated,
        long revision,
        int updatedOffsetHours,
        Guid? organizationId = null)
    {
        var created = AssessmentFixtures.CreateDraft();
        Assert.True(created.Succeeded);
        return created.Value! with
        {
            OrganizationId = organizationId ?? AssessmentFixtures.OrganizationId,
            ActivityId = Guid.Parse(activityId),
            RevisionNumber = revision,
            HasActivatedCohort = activated,
            UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, updatedOffsetHours, 0, 0, TimeSpan.Zero),
            Content = created.Value.Content with { Title = title },
        };
    }

    private static async Task SeedAsync(InMemoryAssessmentDraftStore store, params ActivityDraft[] drafts)
    {
        foreach (var draft in drafts)
        {
            var cohort = AssessmentCohort.CreateEmpty(
                draft.OrganizationId,
                draft.ActivityId,
                Guid.CreateVersion7(),
                draft.RevisionId,
                draft.RevisionNumber);
            Assert.True(cohort.Succeeded);
            await store.AddAsync(
                draft,
                cohort.Value!,
                transaction: null,
                new AssessmentRevisionProvenance(
                    Actor(),
                    null,
                    "create",
                    AuthorizationDecision.Permit(1)),
                TestContext.Current.CancellationToken);
        }
    }
}
