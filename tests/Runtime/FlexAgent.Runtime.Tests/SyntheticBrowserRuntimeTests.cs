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
    private const string ActivityId = "act.synthetic.campaign-001";
    private const string EnrollmentId = "enr.synthetic.001";
    private const string SessionId = "sess.synthetic.001";
    private const string ReviewCaseId = "rev.synthetic.001";
    private const string ReleaseId = "rel.synthetic.001";
    private const string ResultId = "res.synthetic.001";
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

        var grant = await CreateGrantAsync(client, SyntheticScenarioIds.CampaignFullJourney, SyntheticActorStages.Administrator, NewInstanceId(), cancellationToken);
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

        var grant = await CreateGrantAsync(client, SyntheticScenarioIds.CampaignFullJourney, SyntheticActorStages.Administrator, NewInstanceId(), cancellationToken);
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
    public async Task Command_without_resource_id_is_denied()
    {
        var instanceId = NewInstanceId();
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator, instanceId: instanceId);
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await PostCommandRawAsync(
            client,
            "activity.save_draft",
            "missing-resource",
            cancellationToken,
            resourceId: string.Empty,
            expectedVersion: 1);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_grant_exchange_allows_only_one_session()
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var grant = await CreateGrantAsync(
            client,
            SyntheticScenarioIds.CampaignFullJourney,
            SyntheticActorStages.Administrator,
            NewInstanceId(),
            cancellationToken);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => ExchangeGrantRawAsync(client, grant, cancellationToken))
                .ToArray());

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(11, responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized));
    }

    [Fact]
    public async Task Concurrent_idempotent_commands_apply_once()
    {
        var instanceId = NewInstanceId();
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator, instanceId: instanceId);
        var cancellationToken = TestContext.Current.CancellationToken;

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => PostCommandRawAsync(
                    client,
                    "activity.save_draft",
                    "concurrent-save",
                    cancellationToken,
                    resourceId: ActivityId,
                    expectedVersion: 1))
                .ToArray());

        Assert.Equal(12, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        var detail = await client.GetFromJsonAsync<ActivityDetailDto>(
            $"/browser/activities/{ActivityId}",
            JsonOptions,
            cancellationToken);
        Assert.Equal(2, detail!.ExpectedVersion);
    }

    [Fact]
    public async Task Release_detail_before_approval_is_not_readable()
    {
        var instanceId = NewInstanceId();
        var releaseActor = await CreateAuthenticatedClientAsync(SyntheticActorStages.ReleaseActor, instanceId: instanceId);
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await releaseActor.GetAsync($"/browser/release-work/{ReleaseId}", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Review_case_before_session_handoff_is_not_readable()
    {
        var instanceId = NewInstanceId();
        var reviewer = await CreateAuthenticatedClientAsync(SyntheticActorStages.Reviewer, instanceId: instanceId);
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await reviewer.GetAsync($"/browser/review-work/{ReviewCaseId}", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Active_session_access_is_revoked_immediately_after_harness_revocation()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        await PrepareActiveSessionAsync(instanceId, cancellationToken);

        var participant = await CreateAuthenticatedClientAsync(
            SyntheticActorStages.Participant,
            instanceId: instanceId);
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync($"/browser/sessions/{SessionId}", cancellationToken)).StatusCode);

        await RevokeScenarioAccessAsync(instanceId, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, (await participant.GetAsync($"/browser/sessions/{SessionId}", cancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await participant.GetAsync($"/browser/sessions/{SessionId}/events", cancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Review_escalate_records_escalated_decision()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        await PrepareCompletedSessionAsync(instanceId, cancellationToken);

        var reviewer = await CreateAuthenticatedClientAsync(SyntheticActorStages.Reviewer, instanceId: instanceId);
        await PostCommandAsync(reviewer, "review.escalate", "escalate", cancellationToken, reviewVersion: 1);

        var detail = await reviewer.GetFromJsonAsync<ReviewCaseDetailDto>(
            $"/browser/review-work/{ReviewCaseId}",
            JsonOptions,
            cancellationToken);
        Assert.Equal("Escalated", detail!.StatusLabel);
    }

    [Fact]
    public async Task Participant_cannot_execute_release_confirm()
    {
        var instanceId = NewInstanceId();
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Participant, instanceId: instanceId);
        var cancellationToken = TestContext.Current.CancellationToken;

        await PrepareReleasedScenarioAsync(instanceId, cancellationToken);

        var response = await PostCommandRawAsync(
            client,
            "release.confirm",
            "participant-release-forgery",
            cancellationToken,
            resourceId: ReleaseId,
            expectedVersion: 1);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CommandResultDto>(JsonOptions, cancellationToken);
        Assert.Equal("denied", body!.Outcome);
    }

    [Fact]
    public async Task Reviewer_cannot_access_session_projection_or_events()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var instanceId = NewInstanceId();
        await PrepareActiveSessionAsync(instanceId, cancellationToken);

        var reviewer = await CreateAuthenticatedClientAsync(SyntheticActorStages.Reviewer, instanceId: instanceId);
        var sessionResponse = await reviewer.GetAsync("/browser/sessions/sess.synthetic.001", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, sessionResponse.StatusCode);

        var eventsResponse = await reviewer.GetAsync("/browser/sessions/sess.synthetic.001/events", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, eventsResponse.StatusCode);
    }

    [Fact]
    public async Task Idempotency_key_reuse_with_different_command_types_are_independent()
    {
        var instanceId = NewInstanceId();
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator, instanceId: instanceId);
        var cancellationToken = TestContext.Current.CancellationToken;

        var save = await PostCommandRawAsync(
            client,
            "activity.save_draft",
            "shared-key",
            cancellationToken,
            resourceId: ActivityId,
            expectedVersion: 1);
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        var activate = await PostCommandRawAsync(
            client,
            "activity.activate_cohort",
            "shared-key",
            cancellationToken,
            resourceId: ActivityId,
            expectedVersion: 2);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);
    }

    [Fact]
    public async Task Idempotency_key_reuse_with_different_digest_conflicts()
    {
        var instanceId = NewInstanceId();
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator, instanceId: instanceId);
        var cancellationToken = TestContext.Current.CancellationToken;

        var first = await client.PostAsJsonAsync("/browser/commands", new
        {
            schema_version = "v1",
            command_id = Guid.NewGuid().ToString("N"),
            idempotency_key = "digest-key",
            command_type = "activity.save_draft",
            resource_id = ActivityId,
            expected_version = 1,
        }, JsonOptions, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/browser/commands", new
        {
            schema_version = "v1",
            command_id = Guid.NewGuid().ToString("N"),
            idempotency_key = "digest-key",
            command_type = "activity.save_draft",
            resource_id = ActivityId,
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
        var instanceId = NewInstanceId();
        var client = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator, instanceId: instanceId);
        var cancellationToken = TestContext.Current.CancellationToken;

        await PostCommandAsync(client, "activity.save_draft", "save-1", cancellationToken, activityVersion: 1);
        await PostCommandAsync(client, "activity.activate_cohort", "activate-1", cancellationToken, activityVersion: 2);

        var detail = await client.GetFromJsonAsync<ActivityDetailDto>(
            $"/browser/activities/{ActivityId}",
            JsonOptions,
            cancellationToken);

        Assert.Equal("activated", detail!.LifecycleState);
    }

    [Fact]
    public async Task Full_synthetic_campaign_journey_reaches_released_result()
    {
        var scenarioId = SyntheticScenarioIds.CampaignFullJourney;
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;

        async Task<HttpClient> ClientFor(string stage)
        {
            var client = _factory.CreateClient();
            var grant = await CreateGrantAsync(client, scenarioId, stage, instanceId, cancellationToken);
            await ExchangeGrantAsync(client, grant, cancellationToken);
            return client;
        }

        var admin = await ClientFor(SyntheticActorStages.Administrator);
        await PostCommandAsync(admin, "activity.save_draft", "save", cancellationToken, activityVersion: 1);
        await PostCommandAsync(admin, "activity.activate_cohort", "activate", cancellationToken, activityVersion: 2);
        await PostCommandAsync(admin, "enrollment.assign", "enroll", cancellationToken, activityVersion: 3);

        var participant = await ClientFor(SyntheticActorStages.Participant);
        await PostCommandAsync(
            participant,
            "submission.submit_text",
            "submit",
            cancellationToken,
            payload: new Dictionary<string, string> { ["submission_text"] = "Synthetic answer text." });
        await PostCommandAsync(participant, "attempt.start", "start", cancellationToken);
        await PostCommandAsync(
            participant,
            "session.send_message",
            "message",
            cancellationToken,
            sessionVersion: 1,
            payload: new Dictionary<string, string> { ["message_text"] = "Ready." });
        await PostCommandAsync(participant, "session.complete", "complete", cancellationToken, sessionVersion: 2);

        var reviewer = await ClientFor(SyntheticActorStages.Reviewer);
        var reviewWork = await reviewer.GetFromJsonAsync<ReviewWorkDto>("/browser/review-work", JsonOptions, cancellationToken);
        Assert.NotEmpty(reviewWork!.Cases);
        await PostCommandAsync(reviewer, "review.approve", "approve", cancellationToken, reviewVersion: 1);

        var releaseActor = await ClientFor(SyntheticActorStages.ReleaseActor);
        var releaseWork = await releaseActor.GetFromJsonAsync<ReleaseWorkDto>("/browser/release-work", JsonOptions, cancellationToken);
        Assert.Contains(releaseWork!.Items, item => item.StatusLabel.Contains("Not released", StringComparison.Ordinal));
        await PostCommandAsync(releaseActor, "release.confirm", "release", cancellationToken, releaseVersion: 1);

        var participantResult = await participant.GetFromJsonAsync<ResultDetailDto>(
            $"/browser/results/{ResultId}",
            JsonOptions,
            cancellationToken);
        Assert.Equal("released", participantResult!.LifecycleState);
        Assert.Contains("Synthetic released Result", participantResult.Content, StringComparison.Ordinal);
    }

    private async Task PrepareActiveSessionAsync(string instanceId, CancellationToken cancellationToken)
    {
        var admin = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator, instanceId: instanceId);
        await PostCommandAsync(admin, "activity.save_draft", "prep-save", cancellationToken, activityVersion: 1);
        await PostCommandAsync(admin, "activity.activate_cohort", "prep-activate", cancellationToken, activityVersion: 2);
        await PostCommandAsync(admin, "enrollment.assign", "prep-enroll", cancellationToken, activityVersion: 3);

        var participant = await CreateAuthenticatedClientAsync(SyntheticActorStages.Participant, instanceId: instanceId);
        await PostCommandAsync(
            participant,
            "submission.submit_text",
            "prep-submit",
            cancellationToken,
            payload: new Dictionary<string, string> { ["submission_text"] = "Prep answer." });
        await PostCommandAsync(participant, "attempt.start", "prep-start", cancellationToken);
    }

    private async Task PrepareCompletedSessionAsync(string instanceId, CancellationToken cancellationToken)
    {
        await PrepareActiveSessionAsync(instanceId, cancellationToken);
        var participant = await CreateAuthenticatedClientAsync(SyntheticActorStages.Participant, instanceId: instanceId);
        await PostCommandAsync(participant, "session.complete", "prep-complete", cancellationToken, sessionVersion: 1);
    }

    private async Task PrepareReleasedScenarioAsync(string instanceId, CancellationToken cancellationToken)
    {
        await PrepareCompletedSessionAsync(instanceId, cancellationToken);

        var reviewer = await CreateAuthenticatedClientAsync(SyntheticActorStages.Reviewer, instanceId: instanceId);
        await PostCommandAsync(reviewer, "review.approve", "prep-approve", cancellationToken, reviewVersion: 1);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(
        string actorStage,
        string scenarioId = SyntheticScenarioIds.CampaignFullJourney,
        string? instanceId = null)
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var grant = await CreateGrantAsync(
            client,
            scenarioId,
            actorStage,
            instanceId ?? NewInstanceId(),
            cancellationToken);
        await ExchangeGrantAsync(client, grant, cancellationToken);
        return client;
    }

    private async Task<string> CreateGrantAsync(
        HttpClient client,
        string scenarioId,
        string actorStage,
        string scenarioInstanceId,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/browser/harness/scenario-grants")
        {
            Content = JsonContent.Create(new
            {
                scenario_id = scenarioId,
                actor_stage = actorStage,
                scenario_instance_id = scenarioInstanceId,
            }, options: JsonOptions),
        };
        request.Headers.Add(SyntheticBrowserEndpointExtensions.HarnessApiKeyHeaderName, HarnessApiKey);

        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<GrantDto>(JsonOptions, cancellationToken);
        return body!.GrantToken;
    }

    private async Task RevokeScenarioAccessAsync(string instanceId, CancellationToken cancellationToken)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/browser/harness/scenario-instances/revoke-access")
        {
            Content = JsonContent.Create(new
            {
                scenario_id = SyntheticScenarioIds.CampaignFullJourney,
                scenario_instance_id = instanceId,
            }, options: JsonOptions),
        };
        request.Headers.Add(SyntheticBrowserEndpointExtensions.HarnessApiKeyHeaderName, HarnessApiKey);
        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static string NewInstanceId() => Guid.NewGuid().ToString("N");

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
        int? activityVersion = null,
        int? sessionVersion = null,
        int? reviewVersion = null,
        int? releaseVersion = null,
        IReadOnlyDictionary<string, string>? payload = null)
    {
        var response = await PostCommandRawAsync(
            client,
            commandType,
            idempotencyKey,
            cancellationToken,
            resourceId: ResolveResourceId(commandType),
            expectedVersion: ResolveExpectedVersion(commandType, activityVersion, sessionVersion, reviewVersion, releaseVersion),
            payload: payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CommandResultDto>(JsonOptions, cancellationToken);
        Assert.Equal("succeeded", body!.Outcome);
    }

    private static Task<HttpResponseMessage> PostCommandRawAsync(
        HttpClient client,
        string commandType,
        string idempotencyKey,
        CancellationToken cancellationToken,
        string? resourceId = null,
        int? expectedVersion = null,
        IReadOnlyDictionary<string, string>? payload = null,
        string schemaVersion = "v1") =>
        client.PostAsJsonAsync("/browser/commands", new
        {
            schema_version = schemaVersion,
            command_id = Guid.NewGuid().ToString("N"),
            idempotency_key = idempotencyKey,
            command_type = commandType,
            resource_id = resourceId,
            expected_version = expectedVersion,
            payload,
        }, JsonOptions, cancellationToken);

    private static string? ResolveResourceId(string commandType) => commandType switch
    {
        "activity.save_draft" or "activity.activate_cohort" or "enrollment.assign" => ActivityId,
        "submission.submit_text" or "attempt.start" => EnrollmentId,
        "session.send_message" or "session.pause" or "session.resume" or "session.complete" => SessionId,
        "review.approve" or "review.reject" or "review.escalate" => ReviewCaseId,
        "release.confirm" => ReleaseId,
        _ => null,
    };

    private static int? ResolveExpectedVersion(
        string commandType,
        int? activityVersion,
        int? sessionVersion,
        int? reviewVersion,
        int? releaseVersion) => commandType switch
    {
        "activity.save_draft" or "activity.activate_cohort" or "enrollment.assign" => activityVersion,
        "session.send_message" or "session.pause" or "session.resume" or "session.complete" => sessionVersion,
        "review.approve" or "review.reject" or "review.escalate" => reviewVersion,
        "release.confirm" => releaseVersion,
        _ => null,
    };

    private sealed record ReviewWorkDto(IReadOnlyList<ReviewCaseDto> Cases);
    private sealed record ReviewCaseDto(string StatusLabel);
    private sealed record ReviewCaseDetailDto(string StatusLabel);
    private sealed record ReleaseWorkDto(IReadOnlyList<ReleaseItemDto> Items);
    private sealed record ReleaseItemDto(string StatusLabel);
    private sealed record ResultDetailDto(string LifecycleState, string? Content);
    private sealed record CommandResultDto(string Outcome);
    private sealed record GrantDto(string GrantToken);
    private sealed record ExchangeDto(string SchemaVersion, DateTimeOffset ExpiresAt);
    private sealed record NavigationDto(IReadOnlyList<DestinationDto> Destinations);
    private sealed record DestinationDto(string DestinationId, bool IsAvailable);
    private sealed record ActivityDetailDto(string LifecycleState, int ExpectedVersion);
}
