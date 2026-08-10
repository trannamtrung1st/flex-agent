using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FlexAgent.SyntheticBrowser;
using FlexAgent.SyntheticBrowser.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ApiProgram = FlexAgent.Api.Program;

namespace FlexAgent.Runtime.Tests;

public sealed class SyntheticBrowserRuntimeTests : IClassFixture<WebApplicationFactory<ApiProgram>>
{
    private const string HarnessApiKey = "test-harness-key";
    private readonly WebApplicationFactory<ApiProgram> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public SyntheticBrowserRuntimeTests(WebApplicationFactory<ApiProgram> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SyntheticBrowser:Enabled"] = "true",
                    ["SyntheticBrowser:HarnessApiKey"] = HarnessApiKey,
                });
            });
        });
    }

    [Fact]
    public async Task Scenario_grant_exchange_issues_http_only_session_cookie()
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var grant = await CreateGrantAsync(client, SyntheticScenarioIds.CampaignFullJourney, SyntheticActorStages.Administrator, cancellationToken);
        var exchangeResponse = await ExchangeGrantRawAsync(client, grant, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, exchangeResponse.StatusCode);
        Assert.Contains(
            SyntheticBrowserEndpointExtensions.SessionCookieName,
            exchangeResponse.Headers.GetValues("Set-Cookie").First(),
            StringComparison.Ordinal);

        var exchangeContent = await exchangeResponse.Content.ReadAsStringAsync(cancellationToken);
        Assert.DoesNotContain("session_id", exchangeContent, StringComparison.OrdinalIgnoreCase);

        var actorResponse = await client.GetAsync("/browser/actor-context", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, actorResponse.StatusCode);
    }

    [Fact]
    public async Task Scenario_grant_cannot_be_reused()
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var grant = await CreateGrantAsync(client, SyntheticScenarioIds.CampaignFullJourney, SyntheticActorStages.Administrator, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, (await ExchangeGrantRawAsync(client, grant, cancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ExchangeGrantRawAsync(client, grant, cancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Harness_grant_creation_requires_api_key()
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var withoutKey = await client.PostAsJsonAsync("/browser/harness/scenario-grants", new
        {
            scenario_id = SyntheticScenarioIds.CampaignFullJourney,
            actor_stage = SyntheticActorStages.Administrator,
        }, JsonOptions, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, withoutKey.StatusCode);
    }

    [Fact]
    public async Task Actor_context_rejects_unauthenticated_requests()
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/browser/actor-context", cancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_navigation_includes_activities_and_governance()
    {
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator);
        var cancellationToken = TestContext.Current.CancellationToken;

        var navigation = await client.GetFromJsonAsync<NavigationDto>("/browser/navigation", JsonOptions, cancellationToken);
        Assert.NotNull(navigation);
        Assert.Contains(navigation!.Destinations, d => d.DestinationId == "activities" && d.IsAvailable);
        Assert.Contains(navigation.Destinations, d => d.DestinationId == "governance" && d.IsAvailable);
        Assert.Contains(navigation.Destinations, d => d.DestinationId == "my-work" && !d.IsAvailable);
    }

    [Fact]
    public async Task Participant_cannot_access_activities_list()
    {
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Participant);
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/browser/activities", cancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_cannot_execute_release_confirm()
    {
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Participant);
        var cancellationToken = TestContext.Current.CancellationToken;

        await PrepareReleasedScenarioAsync(cancellationToken);

        var response = await PostCommandRawAsync(
            client,
            "release.confirm",
            "participant-release-forgery",
            cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CommandResultDto>(JsonOptions, cancellationToken);
        Assert.Equal("denied", body!.Outcome);
    }

    [Fact]
    public async Task Reviewer_cannot_access_session_projection_or_events()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await PrepareActiveSessionAsync(cancellationToken);

        var reviewer = await CreateAuthenticatedClientAsync(SyntheticActorStages.Reviewer);
        var sessionResponse = await reviewer.GetAsync("/browser/sessions/sess.synthetic.001", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, sessionResponse.StatusCode);

        var eventsResponse = await reviewer.GetAsync("/browser/sessions/sess.synthetic.001/events", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, eventsResponse.StatusCode);
    }

    [Fact]
    public async Task Idempotency_key_reuse_with_different_command_types_are_independent()
    {
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator);
        var cancellationToken = TestContext.Current.CancellationToken;

        var save = await PostCommandRawAsync(client, "activity.save_draft", "shared-key", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        var activate = await PostCommandRawAsync(client, "activity.activate_cohort", "shared-key", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);
    }

    [Fact]
    public async Task Idempotency_key_reuse_with_different_digest_conflicts()
    {
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator);
        var cancellationToken = TestContext.Current.CancellationToken;

        var first = await client.PostAsJsonAsync("/browser/commands", new
        {
            schema_version = "v1",
            command_id = Guid.NewGuid().ToString("N"),
            idempotency_key = "digest-key",
            command_type = "activity.save_draft",
            expected_version = 1,
        }, JsonOptions, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/browser/commands", new
        {
            schema_version = "v1",
            command_id = Guid.NewGuid().ToString("N"),
            idempotency_key = "digest-key",
            command_type = "activity.save_draft",
            expected_version = 2,
        }, JsonOptions, cancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<CommandResultDto>(JsonOptions, cancellationToken);
        Assert.Equal("conflict", body!.Outcome);
    }

    [Fact]
    public async Task Denied_scenario_returns_null_actor_context()
    {
        var client = await CreateAuthenticatedClientAsync(
            SyntheticActorStages.Administrator,
            SyntheticScenarioIds.DeniedAccess);
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/browser/actor-context", cancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Activate_cohort_command_transitions_activity_state()
    {
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator);
        var cancellationToken = TestContext.Current.CancellationToken;

        await PostCommandAsync(client, "activity.save_draft", "save-1", cancellationToken);
        await PostCommandAsync(client, "activity.activate_cohort", "activate-1", cancellationToken);

        var detail = await client.GetFromJsonAsync<ActivityDetailDto>(
            "/browser/activities/act.synthetic.campaign-001",
            JsonOptions,
            cancellationToken);

        Assert.Equal("activated", detail!.LifecycleState);
    }

    [Fact]
    public async Task Full_synthetic_campaign_journey_reaches_released_result()
    {
        var scenarioId = SyntheticScenarioIds.CampaignFullJourney;
        var cancellationToken = TestContext.Current.CancellationToken;

        async Task<HttpClient> ClientFor(string stage)
        {
            var client = _factory.CreateClient();
            var grant = await CreateGrantAsync(client, scenarioId, stage, cancellationToken);
            await ExchangeGrantAsync(client, grant, cancellationToken);
            return client;
        }

        var admin = await ClientFor(SyntheticActorStages.Administrator);
        await PostCommandAsync(admin, "activity.save_draft", "save", cancellationToken);
        await PostCommandAsync(admin, "activity.activate_cohort", "activate", cancellationToken);
        await PostCommandAsync(admin, "enrollment.assign", "enroll", cancellationToken);

        var participant = await ClientFor(SyntheticActorStages.Participant);
        await PostCommandAsync(
            participant,
            "submission.submit_text",
            "submit",
            cancellationToken,
            new Dictionary<string, string> { ["submission_text"] = "Synthetic answer text." });
        await PostCommandAsync(participant, "attempt.start", "start", cancellationToken);
        await PostCommandAsync(
            participant,
            "session.send_message",
            "message",
            cancellationToken,
            new Dictionary<string, string> { ["message_text"] = "Ready." });
        await PostCommandAsync(participant, "session.complete", "complete", cancellationToken);

        var reviewer = await ClientFor(SyntheticActorStages.Reviewer);
        var reviewWork = await reviewer.GetFromJsonAsync<ReviewWorkDto>("/browser/review-work", JsonOptions, cancellationToken);
        Assert.NotEmpty(reviewWork!.Cases);
        await PostCommandAsync(reviewer, "review.approve", "approve", cancellationToken);

        var releaseActor = await ClientFor(SyntheticActorStages.ReleaseActor);
        var releaseWork = await releaseActor.GetFromJsonAsync<ReleaseWorkDto>("/browser/release-work", JsonOptions, cancellationToken);
        Assert.Contains(releaseWork!.Items, item => item.StatusLabel.Contains("Not released", StringComparison.Ordinal));
        await PostCommandAsync(releaseActor, "release.confirm", "release", cancellationToken);

        var participantResult = await participant.GetFromJsonAsync<ResultDetailDto>(
            "/browser/results/res.synthetic.001",
            JsonOptions,
            cancellationToken);
        Assert.Equal("released", participantResult!.LifecycleState);
        Assert.Contains("Synthetic released Result", participantResult.Content, StringComparison.Ordinal);
    }

    private async Task PrepareActiveSessionAsync(CancellationToken cancellationToken)
    {
        var admin = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator);
        await PostCommandAsync(admin, "activity.save_draft", "prep-save", cancellationToken);
        await PostCommandAsync(admin, "activity.activate_cohort", "prep-activate", cancellationToken);
        await PostCommandAsync(admin, "enrollment.assign", "prep-enroll", cancellationToken);

        var participant = await CreateAuthenticatedClientAsync(SyntheticActorStages.Participant);
        await PostCommandAsync(
            participant,
            "submission.submit_text",
            "prep-submit",
            cancellationToken,
            new Dictionary<string, string> { ["submission_text"] = "Prep answer." });
        await PostCommandAsync(participant, "attempt.start", "prep-start", cancellationToken);
    }

    private async Task PrepareReleasedScenarioAsync(CancellationToken cancellationToken)
    {
        await PrepareActiveSessionAsync(cancellationToken);

        var participant = await CreateAuthenticatedClientAsync(SyntheticActorStages.Participant);
        await PostCommandAsync(participant, "session.complete", "prep-complete", cancellationToken);

        var reviewer = await CreateAuthenticatedClientAsync(SyntheticActorStages.Reviewer);
        await PostCommandAsync(reviewer, "review.approve", "prep-approve", cancellationToken);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(
        string actorStage,
        string scenarioId = SyntheticScenarioIds.CampaignFullJourney)
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var grant = await CreateGrantAsync(client, scenarioId, actorStage, cancellationToken);
        await ExchangeGrantAsync(client, grant, cancellationToken);
        return client;
    }

    private async Task<string> CreateGrantAsync(
        HttpClient client,
        string scenarioId,
        string actorStage,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/browser/harness/scenario-grants")
        {
            Content = JsonContent.Create(new
            {
                scenario_id = scenarioId,
                actor_stage = actorStage,
            }, options: JsonOptions),
        };
        request.Headers.Add(SyntheticBrowserEndpointExtensions.HarnessApiKeyHeaderName, HarnessApiKey);

        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<GrantDto>(JsonOptions, cancellationToken);
        return body!.GrantToken;
    }

    private static async Task ExchangeGrantAsync(
        HttpClient client,
        string grantToken,
        CancellationToken cancellationToken)
    {
        var response = await ExchangeGrantRawAsync(client, grantToken, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static Task<HttpResponseMessage> ExchangeGrantRawAsync(
        HttpClient client,
        string grantToken,
        CancellationToken cancellationToken) =>
        client.PostAsJsonAsync("/browser/auth/exchange", new { grant_token = grantToken }, JsonOptions, cancellationToken);

    private static async Task PostCommandAsync(
        HttpClient client,
        string commandType,
        string idempotencyKey,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? payload = null)
    {
        var response = await PostCommandRawAsync(client, commandType, idempotencyKey, cancellationToken, payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CommandResultDto>(JsonOptions, cancellationToken);
        Assert.Equal("succeeded", body!.Outcome);
    }

    private static Task<HttpResponseMessage> PostCommandRawAsync(
        HttpClient client,
        string commandType,
        string idempotencyKey,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? payload = null) =>
        client.PostAsJsonAsync("/browser/commands", new
        {
            schema_version = "v1",
            command_id = Guid.NewGuid().ToString("N"),
            idempotency_key = idempotencyKey,
            command_type = commandType,
            payload,
        }, JsonOptions, cancellationToken);

    private sealed record ReviewWorkDto(IReadOnlyList<ReviewCaseDto> Cases);
    private sealed record ReviewCaseDto(string StatusLabel);
    private sealed record ReleaseWorkDto(IReadOnlyList<ReleaseItemDto> Items);
    private sealed record ReleaseItemDto(string StatusLabel);
    private sealed record ResultDetailDto(string LifecycleState, string? Content);
    private sealed record CommandResultDto(string Outcome);
    private sealed record GrantDto(string GrantToken);
    private sealed record ExchangeDto(string SchemaVersion, DateTimeOffset ExpiresAt);
    private sealed record NavigationDto(IReadOnlyList<DestinationDto> Destinations);
    private sealed record DestinationDto(string DestinationId, bool IsAvailable);
    private sealed record ActivityDetailDto(string LifecycleState);
}
