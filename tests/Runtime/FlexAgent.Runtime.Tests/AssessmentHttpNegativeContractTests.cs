using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Buffers.Text;
using FlexAgent.Api;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Submissions.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ApiProgram = FlexAgent.Api.Program;

namespace FlexAgent.Runtime.Tests;

public sealed class AssessmentHttpNegativeContractTests
{
    private const string Issuer = "https://issuer.example/realms/flex";
    private const string ClientId = "flex-agent-api";
    private const string Subject = "assessment-http-subject";

    [Fact]
    public async Task Activate_without_antiforgery_is_rejected_before_session_authentication()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.PostAsync(ActivateUrl(), JsonContent(), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("csrf.invalid", body, StringComparison.Ordinal);
        AssertNoBaseline(body);
    }

    [Fact]
    public async Task Activate_with_anonymous_csrf_and_no_session_is_unauthorized_and_omits_assessment_state()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var session = await client.GetAsync("/auth/session", TestContext.Current.CancellationToken);
        var payload = JsonDocument.Parse(await session.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.False(payload.RootElement.GetProperty("authenticated").GetBoolean());
        using var request = new HttpRequestMessage(HttpMethod.Post, ActivateUrl())
        {
            Content = JsonContent(),
        };
        request.Headers.TryAddWithoutValidation(
            HumanAuthenticationHostOptions.AntiforgeryHeaderName,
            payload.RootElement.GetProperty("csrf_token").GetString());
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertNoBaseline(body);
        Assert.DoesNotContain("outcome_code", body, StringComparison.Ordinal);
        Assert.DoesNotContain("cohort_state", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Activate_without_antiforgery_is_invalid_and_omits_baseline()
    {
        await using var context = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, ActivateUrl())
        {
            Content = JsonContent(),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("csrf.invalid", body, StringComparison.Ordinal);
        AssertNoBaseline(body);
    }

    [Fact]
    public async Task Activate_with_a_malformed_key_is_invalid_and_omits_baseline()
    {
        await using var context = await LoginAsync();
        using var response = await SendActivateAsync(context, "   ");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(AssessmentFailureCodes.InvalidField, document.RootElement.GetProperty("outcome_code").GetString());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("baseline_id").ValueKind);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("baseline_digest").ValueKind);
        Assert.Equal(CohortStates.Draft, document.RootElement.GetProperty("cohort_state").GetString());
    }

    [Fact]
    public async Task Activate_with_an_unauthorized_relationship_is_denied_and_omits_baseline()
    {
        await using var context = await LoginAsync();
        using var response = await SendActivateAsync(context, "idem-1");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(AssessmentFailureCodes.Denied, document.RootElement.GetProperty("outcome_code").GetString());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("baseline_id").ValueKind);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("baseline_digest").ValueKind);
        Assert.Equal(CohortStates.Draft, document.RootElement.GetProperty("cohort_state").GetString());
    }

    [Fact]
    public async Task Reconcile_without_a_session_is_unauthorized_and_omits_baseline()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(ReconcileUrl("idem-1"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertNoBaseline(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Reconcile_without_an_idempotency_key_is_invalid_and_omits_baseline()
    {
        await using var context = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, ReconcileUrl(null));
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(AssessmentFailureCodes.InvalidField, body, StringComparison.Ordinal);
        AssertNoBaseline(body);
    }

    [Fact]
    public async Task Shell_without_a_session_is_unauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/v1/assessment/shell", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_and_get_without_a_session_are_unauthorized_and_omit_activities()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var list = await client.GetAsync("/v1/assessment/activities", TestContext.Current.CancellationToken);
        using var get = await client.GetAsync($"/v1/assessment/activities/{Guid.CreateVersion7()}", TestContext.Current.CancellationToken);
        using var sources = await client.GetAsync("/v1/assessment/source-options", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, sources.StatusCode);
        Assert.DoesNotContain("activities", await list.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        Assert.DoesNotContain("activity_id", await get.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Numbered_list_rejects_unsupported_paging_and_invalid_bounds_without_activities()
    {
        await using var context = await LoginAsync(
            mfa: true,
            relationship: AuthenticationStrengthEvaluator.AdministratorRelationship,
            actions: [AssessmentAuthorizationActions.ReadActivity],
            permitAuthorization: true);
        using var cursor = await SendGetAsync(context, "/v1/assessment/activities?paging=cursor");
        using var oversized = await SendGetAsync(context, "/v1/assessment/activities?paging=numbered&page_size=51");
        using var duplicateSort = await SendGetAsync(
            context,
            "/v1/assessment/activities?paging=numbered&sort=title:asc,title:desc");
        using var encoded = await SendGetAsync(
            context,
            "/v1/assessment/activities?paging=numbered&q=%2Awild%25");
        var cursorBody = await cursor.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var oversizedBody = await oversized.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, cursor.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateSort.StatusCode);
        Assert.Equal(HttpStatusCode.OK, encoded.StatusCode);
        Assert.Equal("no-store", encoded.Headers.CacheControl?.ToString());
        Assert.Contains(AssessmentFailureCodes.InvalidField, cursorBody, StringComparison.Ordinal);
        Assert.Contains(AssessmentFailureCodes.InvalidField, oversizedBody, StringComparison.Ordinal);
        Assert.DoesNotContain("\"activity_id\":\"", cursorBody, StringComparison.Ordinal);
        using var encodedDocument = JsonDocument.Parse(await encoded.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("numbered", encodedDocument.RootElement.GetProperty("pagination").GetProperty("mode").GetString());
        Assert.Equal(1, encodedDocument.RootElement.GetProperty("pagination").GetProperty("page").GetInt32());
        Assert.Equal(16, encodedDocument.RootElement.GetProperty("pagination").GetProperty("page_size").GetInt32());
        Assert.False(encodedDocument.RootElement.TryGetProperty("activities", out var activities) && activities.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Omitted_paging_keeps_the_legacy_complete_list_shape()
    {
        await using var context = await LoginAsync(
            mfa: true,
            relationship: AuthenticationStrengthEvaluator.AdministratorRelationship,
            actions: [AssessmentAuthorizationActions.ReadActivity],
            permitAuthorization: true);
        using var response = await SendGetAsync(context, "/v1/assessment/activities");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.True(document.RootElement.TryGetProperty("activities", out _));
        Assert.True(document.RootElement.TryGetProperty("permitted_actions", out _));
        Assert.False(document.RootElement.TryGetProperty("pagination", out _));
    }

    [Fact]
    public async Task Numbered_list_pages_created_activities_and_returns_empty_out_of_range_metadata()
    {
        await using var context = await LoginAsync(
            mfa: true,
            relationship: AuthenticationStrengthEvaluator.AdministratorRelationship,
            actions:
            [
                AssessmentAuthorizationActions.ReadActivity,
                AssessmentAuthorizationActions.CreateActivity,
                AssessmentAuthorizationActions.SelectSources,
            ],
            permitAuthorization: true);
        using var firstCreate = await SendMutationAsync(context, HttpMethod.Post, "/v1/assessment/activities", """{"title":"Alpha"}""");
        using var secondCreate = await SendMutationAsync(context, HttpMethod.Post, "/v1/assessment/activities", """{"title":"Beta"}""");
        Assert.Equal(HttpStatusCode.Created, firstCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondCreate.StatusCode);
        using var page = await SendGetAsync(
            context,
            "/v1/assessment/activities?paging=numbered&page=1&page_size=1&sort=title:asc");
        using var drifted = await SendGetAsync(
            context,
            "/v1/assessment/activities?paging=numbered&page=9&page_size=1&sort=title:asc");
        using var pageDocument = JsonDocument.Parse(await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var driftedDocument = JsonDocument.Parse(await drifted.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("Alpha", pageDocument.RootElement.GetProperty("activities")[0].GetProperty("title").GetString());
        Assert.Equal(2, pageDocument.RootElement.GetProperty("pagination").GetProperty("total_items").GetInt32());
        Assert.Equal(2, pageDocument.RootElement.GetProperty("pagination").GetProperty("total_pages").GetInt32());
        Assert.Equal(0, driftedDocument.RootElement.GetProperty("activities").GetArrayLength());
        Assert.Equal(9, driftedDocument.RootElement.GetProperty("pagination").GetProperty("page").GetInt32());
        Assert.Equal(2, driftedDocument.RootElement.GetProperty("pagination").GetProperty("total_items").GetInt32());
    }

    [Fact]
    public async Task Create_without_antiforgery_is_rejected_before_session_authentication()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.PostAsync(
            "/v1/assessment/activities",
            new StringContent("""{"title":"Campaign"}""", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("csrf.invalid", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_with_anonymous_csrf_and_no_session_is_unauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var session = await client.GetAsync("/auth/session", TestContext.Current.CancellationToken);
        var payload = JsonDocument.Parse(await session.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/assessment/activities")
        {
            Content = new StringContent("""{"title":"Campaign"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation(
            HumanAuthenticationHostOptions.AntiforgeryHeaderName,
            payload.RootElement.GetProperty("csrf_token").GetString());
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_without_mfa_keeps_the_shell_but_cannot_use_activities_or_list()
    {
        await using var context = await LoginAsync(mfa: false, relationship: AuthenticationStrengthEvaluator.AdministratorRelationship);
        using var shell = await SendGetAsync(context, "/v1/assessment/shell");
        using var list = await SendGetAsync(context, "/v1/assessment/activities");
        var shellBody = await shell.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var listBody = await list.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, shell.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        using var shellDocument = JsonDocument.Parse(shellBody);
        Assert.False(string.IsNullOrWhiteSpace(shellDocument.RootElement.GetProperty("actor_id").GetString()));
        Assert.False(DestinationAvailable(shellDocument, "activities"));
        Assert.False(DestinationAvailable(shellDocument, "my-work"));
        Assert.DoesNotContain("activities", listBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Administrator_with_mfa_receives_a_server_derived_shell_and_guessed_activity_is_not_disclosed()
    {
        await using var context = await LoginAsync(
            mfa: true,
            relationship: AuthenticationStrengthEvaluator.AdministratorRelationship,
            actions:
            [
                AssessmentAuthorizationActions.ReadActivity,
                AssessmentAuthorizationActions.CreateActivity,
            ]);
        using var shell = await SendGetAsync(context, "/v1/assessment/shell");
        var document = JsonDocument.Parse(await shell.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var guessed = await SendGetAsync(context, $"/v1/assessment/activities/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.OK, shell.StatusCode);
        Assert.Equal("v1", document.RootElement.GetProperty("schema_version").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("organization_id").GetString()));
        Assert.Equal("Demo Administrator", document.RootElement.GetProperty("display_name").GetString());
        Assert.Equal(HttpStatusCode.NotFound, guessed.StatusCode);
        Assert.DoesNotContain("title", await guessed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reconcile_with_an_invalid_key_is_rejected_without_creating_authority()
    {
        await using var context = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, ReconcileUrl("not a valid key"));
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(AssessmentFailureCodes.InvalidField, body, StringComparison.Ordinal);
        AssertNoBaseline(body);
    }

    [Fact]
    public async Task Create_save_and_readiness_without_authorization_are_forbidden_and_omit_activity_authority()
    {
        await using var context = await LoginAsync(
            mfa: true,
            relationship: AuthenticationStrengthEvaluator.AdministratorRelationship,
            actions: [AssessmentAuthorizationActions.ReadActivity]);
        using var create = await SendMutationAsync(context, HttpMethod.Post, "/v1/assessment/activities", CreateActivityJson());
        var activityId = Guid.CreateVersion7();
        using var save = await SendMutationAsync(
            context,
            HttpMethod.Post,
            $"/v1/assessment/activities/{activityId}",
            """{"title":"Next","expected_revision_number":1}""");
        using var readiness = await SendMutationAsync(
            context,
            HttpMethod.Post,
            $"/v1/assessment/activities/{activityId}/readiness",
            null);
        var createBody = await create.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var saveBody = await save.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var readinessBody = await readiness.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, save.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, readiness.StatusCode);
        Assert.Contains(AssessmentFailureCodes.Denied, createBody, StringComparison.Ordinal);
        Assert.Contains(AssessmentFailureCodes.Denied, readinessBody, StringComparison.Ordinal);
        Assert.DoesNotContain("\"activity_id\":\"", createBody, StringComparison.Ordinal);
        Assert.DoesNotContain("revision_id", saveBody, StringComparison.Ordinal);
        Assert.DoesNotContain("recovery_hint", readinessBody, StringComparison.Ordinal);
        Assert.DoesNotContain("overall_severity\":\"", readinessBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reviewer_without_mfa_keeps_the_shell_but_cannot_read_activity_or_mutate()
    {
        await using var context = await LoginAsync(
            mfa: false,
            relationship: AuthenticationStrengthEvaluator.ReviewerRelationship,
            actions:
            [
                AssessmentAuthorizationActions.ReadActivity,
                AssessmentAuthorizationActions.ReadBaseline,
                AssessmentAuthorizationActions.ReadBaselineProvenance,
            ]);
        using var shell = await SendGetAsync(context, "/v1/assessment/shell");
        using var activity = await SendGetAsync(context, $"/v1/assessment/activities/{Guid.CreateVersion7()}");
        using var create = await SendMutationAsync(context, HttpMethod.Post, "/v1/assessment/activities", CreateActivityJson());
        var shellBody = await shell.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var activityBody = await activity.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, shell.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, activity.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        using var shellDocument = JsonDocument.Parse(shellBody);
        Assert.False(string.IsNullOrWhiteSpace(shellDocument.RootElement.GetProperty("actor_id").GetString()));
        Assert.False(DestinationAvailable(shellDocument, "activities"));
        Assert.False(DestinationAvailable(shellDocument, "my-work"));
        Assert.DoesNotContain("title", activityBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dual_capability_actor_without_administrator_mfa_keeps_my_work_and_hides_activities()
    {
        await using var context = await LoginAsync(
            mfa: false,
            relationship: AuthenticationStrengthEvaluator.AdministratorRelationship,
            actions:
            [
                EnrollmentAuthorizationActions.Assign,
                EnrollmentAuthorizationActions.Discover,
            ]);
        using var shell = await SendGetAsync(context, "/v1/assessment/shell");
        using var shellDocument = JsonDocument.Parse(await shell.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.OK, shell.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(shellDocument.RootElement.GetProperty("actor_id").GetString()));
        Assert.False(DestinationAvailable(shellDocument, "activities"));
        Assert.True(DestinationAvailable(shellDocument, "my-work"));
    }

    [Fact]
    public async Task Reviewer_with_mfa_receives_a_shell_and_cannot_create_or_activate()
    {
        await using var context = await LoginAsync(
            mfa: true,
            relationship: AuthenticationStrengthEvaluator.ReviewerRelationship,
            actions:
            [
                AssessmentAuthorizationActions.ReadActivity,
                AssessmentAuthorizationActions.ReadBaseline,
                AssessmentAuthorizationActions.ReconcileActivation,
            ]);
        using var shell = await SendGetAsync(context, "/v1/assessment/shell");
        using var create = await SendMutationAsync(context, HttpMethod.Post, "/v1/assessment/activities", CreateActivityJson());
        using var activate = await SendActivateAsync(context, "reviewer-activate");
        var shellDocument = JsonDocument.Parse(await shell.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var createBody = await create.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var activateDocument = JsonDocument.Parse(await activate.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.OK, shell.StatusCode);
        Assert.Equal(AuthenticationStrengthEvaluator.ReviewerRelationship, shellDocument.RootElement.GetProperty("relationship").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Contains(AssessmentFailureCodes.Denied, createBody, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Conflict, activate.StatusCode);
        Assert.Equal(AssessmentFailureCodes.Denied, activateDocument.RootElement.GetProperty("outcome_code").GetString());
        Assert.Equal(JsonValueKind.Null, activateDocument.RootElement.GetProperty("baseline_id").ValueKind);
    }

    [Fact]
    public async Task Save_and_readiness_without_antiforgery_are_invalid_and_omit_draft_state()
    {
        await using var context = await LoginAsync(
            mfa: true,
            relationship: AuthenticationStrengthEvaluator.AdministratorRelationship,
            actions:
            [
                AssessmentAuthorizationActions.SaveActivity,
                AssessmentAuthorizationActions.CheckReadiness,
            ]);
        var activityId = Guid.CreateVersion7();
        using var save = new HttpRequestMessage(HttpMethod.Post, $"/v1/assessment/activities/{activityId}")
        {
            Content = new StringContent("""{"title":"Next","expected_revision_number":1}""", Encoding.UTF8, "application/json"),
        };
        save.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var readiness = new HttpRequestMessage(HttpMethod.Post, $"/v1/assessment/activities/{activityId}/readiness");
        readiness.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var saveResponse = await context.Client.SendAsync(save, TestContext.Current.CancellationToken);
        using var readinessResponse = await context.Client.SendAsync(readiness, TestContext.Current.CancellationToken);
        var saveBody = await saveResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var readinessBody = await readinessResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, saveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, readinessResponse.StatusCode);
        Assert.Contains("csrf.invalid", saveBody, StringComparison.Ordinal);
        Assert.Contains("csrf.invalid", readinessBody, StringComparison.Ordinal);
        Assert.DoesNotContain("revision_number", saveBody, StringComparison.Ordinal);
        Assert.DoesNotContain("overall_severity", readinessBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_without_a_title_is_invalid_and_does_not_create_an_activity()
    {
        await using var context = await LoginAsync(
            mfa: true,
            relationship: AuthenticationStrengthEvaluator.AdministratorRelationship,
            actions: [AssessmentAuthorizationActions.CreateActivity],
            permitAuthorization: true);
        using var response = await SendMutationAsync(context, HttpMethod.Post, "/v1/assessment/activities", """{"title":"  "}""");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(AssessmentFailureCodes.InvalidField, body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"activity_id\":\"", body, StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> SendMutationAsync(
        LoggedInContext context,
        HttpMethod method,
        string url,
        string? json)
    {
        using var request = new HttpRequestMessage(method, url);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        return await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<HttpResponseMessage> SendActivateAsync(LoggedInContext context, string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ActivateUrl())
        {
            Content = JsonContent(idempotencyKey),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        return await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static bool DestinationAvailable(JsonDocument document, string destinationId)
    {
        foreach (var item in document.RootElement.GetProperty("navigation").EnumerateArray())
        {
            if (string.Equals(item.GetProperty("destination_id").GetString(), destinationId, StringComparison.Ordinal))
            {
                return item.GetProperty("is_available").GetBoolean();
            }
        }

        return false;
    }

    private static async Task<HttpResponseMessage> SendGetAsync(LoggedInContext context, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        return await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<LoggedInContext> LoginAsync(
        bool mfa = true,
        string? relationship = null,
        IReadOnlyList<string>? actions = null,
        bool permitAuthorization = false)
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        var factory = CreateFactory(rsa, tokens, relationship, actions, acceptPasswordAcr: !mfa, permitAuthorization);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        SeedBinding(factory);
        var cancellationToken = TestContext.Current.CancellationToken;
        using var login = await client.GetAsync("/auth/login?return_path=/work", cancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        tokens.IdToken = CreateIdToken(rsa, query["nonce"].ToString(), mfa);
        using var callback = await client.GetAsync(
            $"/auth/callback?code=one-time-code&state={Uri.EscapeDataString(query["state"].ToString())}",
            cancellationToken);
        var sessionCookie = callback.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(HumanAuthenticationHostOptions.CookieName, StringComparison.Ordinal))
            .Split(';', 2)[0];
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
        using var session = await client.GetAsync("/auth/session", cancellationToken);
        var payload = JsonDocument.Parse(await session.Content.ReadAsStringAsync(cancellationToken));
        return new LoggedInContext(
            factory,
            client,
            sessionCookie,
            payload.RootElement.GetProperty("csrf_token").GetString()!);
    }

    private static void SeedBinding(WebApplicationFactory<ApiProgram> factory)
    {
        var bindings = factory.Services.GetRequiredService<MemoryHumanIdentityBindingStore>();
        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        bindings.RegisterActor(actorId);
        bindings.GrantOrganization(actorId, organizationId);
        bindings.TryProvisionAsync(
                new HumanIdentityBinding(
                    Guid.NewGuid(),
                    new ExactIssuerSubject(Issuer, Subject),
                    actorId,
                    DateTimeOffset.UtcNow,
                    null))
            .GetAwaiter()
            .GetResult();
    }

    private static WebApplicationFactory<ApiProgram> CreateFactory(
        RSA? rsa = null,
        FakeOidcAuthorizationClient? tokens = null,
        string? relationship = null,
        IReadOnlyList<string>? actions = null,
        bool acceptPasswordAcr = false,
        bool permitAuthorization = false)
    {
        rsa ??= RSA.Create(2048);
        tokens ??= new FakeOidcAuthorizationClient();
        var keys = new Dictionary<string, RSA>(StringComparer.Ordinal) { ["test"] = rsa };
        return new WebApplicationFactory<ApiProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("HumanAuthentication:Enabled", "true");
            builder.UseSetting("HumanAuthentication:Issuer", Issuer);
            builder.UseSetting("HumanAuthentication:ClientId", ClientId);
            builder.UseSetting("HumanAuthentication:AuthorizationEndpoint", "https://issuer.example/realms/flex/protocol/openid-connect/auth");
            builder.UseSetting("HumanAuthentication:TokenEndpoint", "https://issuer.example/realms/flex/protocol/openid-connect/token");
            builder.UseSetting("HumanAuthentication:JwksUri", "https://issuer.example/realms/flex/protocol/openid-connect/certs");
            builder.UseSetting("HumanAuthentication:RedirectUri", "https://app.example/auth/callback");
            builder.UseSetting("HumanAuthentication:AcceptedAcr:0", acceptPasswordAcr ? "pwd" : "acr:mfa");
            builder.UseSetting("HumanAuthentication:AcceptedAmr:0", acceptPasswordAcr ? "pwd" : "mfa");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IOidcAuthorizationClient>(tokens);
                services.AddSingleton<IJwksKeySource>(new StaticJwksKeySource(keys));
                if (relationship is not null)
                {
                    services.AddSingleton<IAssessmentRelationshipResolver>(
                        new StubAssessmentRelationshipResolver(relationship, actions ?? []));
                }

                if (permitAuthorization)
                {
                    services.AddSingleton<IAssessmentAuthorizationPort>(_ => new InMemoryAssessmentAuthorizationPort(permit: true));
                }
            });
        });
    }

    private static string CreateIdToken(RSA rsa, string nonce, bool mfa = true)
    {
        var now = DateTimeOffset.UtcNow;
        var header = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT", kid = "test" });
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["iss"] = Issuer,
            ["aud"] = ClientId,
            ["sub"] = Subject,
            ["nonce"] = nonce,
            ["sid"] = "sid-1",
            ["acr"] = mfa ? "acr:mfa" : "pwd",
            ["amr"] = mfa ? new[] { "mfa" } : new[] { "pwd" },
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.AddMinutes(-1).ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(5).ToUnixTimeSeconds(),
            ["given_name"] = "Demo",
            ["family_name"] = "Administrator",
            ["preferred_username"] = "demo.admin",
        });
        var encodedHeader = Encode(Encoding.UTF8.GetBytes(header));
        var encodedPayload = Encode(Encoding.UTF8.GetBytes(payload));
        var signingInput = $"{encodedHeader}.{encodedPayload}";
        var signature = Encode(rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        return $"{signingInput}.{signature}";
    }

    private static string Encode(ReadOnlySpan<byte> bytes)
    {
        var buffer = new byte[Base64Url.GetEncodedLength(bytes.Length)];
        Base64Url.EncodeToUtf8(bytes, buffer, out _, out var written);
        return Encoding.ASCII.GetString(buffer.AsSpan(0, written));
    }

    private static string CreateActivityJson()
    {
        var digest = new string('a', 64);
        var source = Guid.CreateVersion7();
        var version = Guid.CreateVersion7();
        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["title"] = "Campaign",
            ["organization_policy_source_id"] = source,
            ["organization_policy_version_id"] = version,
            ["organization_policy_digest"] = digest,
            ["agent_source_id"] = source,
            ["agent_version_id"] = version,
            ["agent_digest"] = digest,
            ["harness_source_id"] = source,
            ["harness_version_id"] = version,
            ["harness_digest"] = digest,
            ["workflow_source_id"] = source,
            ["workflow_version_id"] = version,
            ["workflow_digest"] = digest,
            ["adaptive_follow_up_source_id"] = source,
            ["adaptive_follow_up_version_id"] = version,
            ["adaptive_follow_up_digest"] = digest,
            ["rubric_source_id"] = source,
            ["rubric_version_id"] = version,
            ["rubric_digest"] = digest,
            ["model_source_id"] = source,
            ["model_version_id"] = version,
            ["model_digest"] = digest,
            ["capability_source_id"] = source,
            ["capability_version_id"] = version,
            ["capability_digest"] = digest,
            ["review_source_id"] = source,
            ["review_version_id"] = version,
            ["review_digest"] = digest,
            ["task_source_id"] = source,
            ["task_version_id"] = version,
            ["task_digest"] = digest,
        });
    }

    private static StringContent JsonContent(string idempotencyKey = "idem-1") =>
        new(
            JsonSerializer.Serialize(new
            {
                expected_revision_id = Guid.CreateVersion7(),
                expected_revision_number = 1,
                idempotency_key = idempotencyKey,
            }),
            Encoding.UTF8,
            "application/json");

    private static string ActivateUrl() =>
        $"/v1/assessment/activities/{Guid.CreateVersion7()}/cohorts/{Guid.CreateVersion7()}/activate";

    private static string ReconcileUrl(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? $"/v1/assessment/activities/{Guid.CreateVersion7()}/cohorts/{Guid.CreateVersion7()}/activation"
            : $"/v1/assessment/activities/{Guid.CreateVersion7()}/cohorts/{Guid.CreateVersion7()}/activation?idempotency_key={Uri.EscapeDataString(idempotencyKey)}";

    private static void AssertNoBaseline(string body)
    {
        Assert.DoesNotContain("\"baseline_id\":\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"baseline_digest\":\"", body, StringComparison.Ordinal);
    }

    private sealed record LoggedInContext(
        WebApplicationFactory<ApiProgram> Factory,
        HttpClient Client,
        string SessionCookie,
        string CsrfToken) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Factory.DisposeAsync();
    }

    private sealed class FakeOidcAuthorizationClient : IOidcAuthorizationClient
    {
        public string? IdToken { get; set; }

        public Task<OidcTokenExchangeResult> ExchangeAuthorizationCodeAsync(
            string code,
            string codeVerifier,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new OidcTokenExchangeResult(IdToken, IdToken is null ? HumanAuthenticationReasonCodes.InvalidProviderResponse : null));
    }

    private sealed class StubAssessmentRelationshipResolver(
        string relationship,
        IReadOnlyList<string> actions) : IAssessmentRelationshipResolver
    {
        public Task<AssessmentActorAuthorization> ResolveAsync(
            Guid actorId,
            Guid organizationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new AssessmentActorAuthorization(relationship, actions));
    }

    private sealed class StaticJwksKeySource(IReadOnlyDictionary<string, RSA> keys) : IJwksKeySource
    {
        public Task<JwksKeySnapshot?> TryGetKeysAsync(
            string jwksUri,
            CancellationToken cancellationToken = default) =>
            TryGetKeysAsync(jwksUri, requiredKid: null, cancellationToken);

        public Task<JwksKeySnapshot?> TryGetKeysAsync(
            string jwksUri,
            string? requiredKid,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<JwksKeySnapshot?>(JwksKeySnapshot.Borrowed(keys));
    }
}
