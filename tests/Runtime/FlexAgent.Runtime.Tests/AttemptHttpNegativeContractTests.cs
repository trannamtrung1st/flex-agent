using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Buffers.Text;
using FlexAgent.Api;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ApiProgram = FlexAgent.Api.Program;

namespace FlexAgent.Runtime.Tests;

public sealed class AttemptHttpNegativeContractTests
{
    private const string Issuer = "https://issuer.example/realms/flex";
    private const string ClientId = "flex-agent-api";
    private const string Subject = "attempt-http-subject";

    [Fact]
    public async Task Attempt_readiness_without_a_session_is_unauthorized_and_not_cached()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/attempt",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(HumanAuthenticationReasonCodes.MissingSession, body, StringComparison.Ordinal);
        Assert.DoesNotContain("required_notices", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Start_without_antiforgery_is_rejected_before_session_authentication()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.PostAsync(
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/attempt/start",
            StartContent(),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("csrf.invalid", body, StringComparison.Ordinal);
        Assert.DoesNotContain("attempt_id", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Acknowledge_without_antiforgery_is_rejected_before_session_authentication()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.PostAsync(
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/attempt/acknowledgments",
            new StringContent(
                """{"schema_version":"v2","notice_id":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1","source_version_id":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2","outcome":"affirmed","idempotency_key":"attempt-ack-synthetic-0001"}""",
                Encoding.UTF8,
                "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("csrf.invalid", body, StringComparison.Ordinal);
        Assert.DoesNotContain("record_id", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Start_with_uppercase_digest_is_invalid()
    {
        await using var context = await LoginAsync();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/attempt/start")
        {
            Content = new StringContent(
                """{"schema_version":"v2","idempotency_key":"attempt-start-synthetic-0001","trusted_command_digest":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"}""",
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(AttemptFailureCodes.InvalidField, body, StringComparison.Ordinal);
        Assert.DoesNotContain("session_id", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Guessed_attempt_readiness_is_concealed_as_not_found()
    {
        await using var context = await LoginAsync();
        var enrollmentId = Guid.CreateVersion7();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/v2/assessment/my-work/{enrollmentId}/attempt");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(AttemptFailureCodes.Denied, body, StringComparison.Ordinal);
        Assert.DoesNotContain("required_notices", body, StringComparison.Ordinal);
        Assert.DoesNotContain(enrollmentId.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Guessed_start_is_concealed_as_not_found()
    {
        await using var context = await LoginAsync();
        var enrollmentId = Guid.CreateVersion7();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/v2/assessment/my-work/{enrollmentId}/attempt/start")
        {
            Content = StartContent(),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(AttemptFailureCodes.Denied, body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"attempt_id\":\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"session_id\":\"", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Start_with_an_unknown_schema_version_is_invalid()
    {
        await using var context = await LoginAsync();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/attempt/start")
        {
            Content = new StringContent(
                """{"schema_version":"v1","idempotency_key":"attempt-start-synthetic-0001","trusted_command_digest":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}""",
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(AttemptFailureCodes.InvalidField, body, StringComparison.Ordinal);
        Assert.DoesNotContain("session_id", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Acknowledge_with_a_withdrawn_outcome_is_accepted_at_the_contract_boundary()
    {
        await using var context = await LoginAsync();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/attempt/acknowledgments")
        {
            Content = new StringContent(
                """{"schema_version":"v2","notice_id":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1","source_version_id":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2","outcome":"not-an-outcome","idempotency_key":"attempt-ack-synthetic-0001"}""",
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(AttemptFailureCodes.InvalidField, body, StringComparison.Ordinal);
        Assert.DoesNotContain("record_id", body, StringComparison.Ordinal);
        Assert.DoesNotContain("notice:", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Discover_denial_conceals_owned_enrollment_readiness()
    {
        await using var context = await LoginAsync();
        var enrollmentId = SeedOwnedEnrollment(context.Factory, ownedByActor: true);
        AuthorizationPort(context.Factory).DeniedActions.Add(EnrollmentAuthorizationActions.Discover);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/assessment/my-work/{enrollmentId}/attempt");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(AttemptFailureCodes.Denied, body, StringComparison.Ordinal);
        Assert.DoesNotContain("required_notices", body, StringComparison.Ordinal);
        Assert.DoesNotContain(enrollmentId.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Foreign_participant_enrollment_is_concealed_as_not_found()
    {
        await using var context = await LoginAsync();
        var enrollmentId = SeedOwnedEnrollment(context.Factory, ownedByActor: false);
        AuthorizationPort(context.Factory).Permit = true;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/assessment/my-work/{enrollmentId}/attempt");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(AttemptFailureCodes.Denied, body, StringComparison.Ordinal);
        Assert.DoesNotContain("required_notices", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"attempt_id\":\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Administrator_relationship_cannot_start_another_participants_attempt()
    {
        await using var context = await LoginAsync(
            relationship: AuthenticationStrengthEvaluator.AdministratorRelationship,
            actions: [EnrollmentAuthorizationActions.Assign]);
        var enrollmentId = SeedOwnedEnrollment(context.Factory, ownedByActor: false);
        AuthorizationPort(context.Factory).Permit = true;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/v2/assessment/my-work/{enrollmentId}/attempt/start")
        {
            Content = StartContent(),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(AttemptFailureCodes.Denied, body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"session_id\":\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Acknowledgment_for_an_unknown_notice_version_is_invalid()
    {
        await using var context = await LoginAsync(notices: new FixedNoticePort(RequiredNotice()));
        var enrollmentId = SeedOwnedEnrollment(context.Factory, ownedByActor: true);
        AuthorizationPort(context.Factory).Permit = true;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/v2/assessment/my-work/{enrollmentId}/attempt/acknowledgments")
        {
            Content = new StringContent(
                """{"schema_version":"v2","notice_id":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1","source_version_id":"bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2","outcome":"affirmed","idempotency_key":"attempt-ack-synthetic-0001"}""",
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(AttemptFailureCodes.AcknowledgmentInvalid, body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"record_id\":\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("notice:", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Audit_failure_returns_unavailable_without_a_session_locator()
    {
        await using var context = await LoginAsync();
        var enrollmentId = SeedEligibleStart(context.Factory);
        AuthorizationPort(context.Factory).Permit = true;
        context.Factory.Services.GetRequiredService<RecordingEnrollmentAuditPort>().FailRequired = true;
        using var readinessRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/v2/assessment/my-work/{enrollmentId}/attempt");
        readinessRequest.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var readiness = await context.Client.SendAsync(readinessRequest, TestContext.Current.CancellationToken);
        var readinessBody = await readiness.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
        using var readinessDocument = JsonDocument.Parse(readinessBody);
        Assert.Equal(
            AttemptReadinessStates.Eligible,
            readinessDocument.RootElement.GetProperty("readiness_state").GetString());
        var digest = readinessDocument.RootElement.GetProperty("start_command_digest").GetString();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/v2/assessment/my-work/{enrollmentId}/attempt/start")
        {
            Content = new StringContent(
                $$"""{"schema_version":"v2","idempotency_key":"attempt-start-synthetic-0001","trusted_command_digest":"{{digest}}"}""",
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains(AttemptFailureCodes.AuditUnavailable, body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"session_id\":\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"attempt_id\":\"", body, StringComparison.Ordinal);
        Assert.Empty(context.Factory.Services.GetRequiredService<InMemoryAttemptStore>().Items);
    }

    [Fact]
    public async Task Reconcile_without_a_session_is_unauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/attempt/reconcile")
        {
            Content = StartContent(),
        };
        using var session = await client.GetAsync("/auth/session", TestContext.Current.CancellationToken);
        var payload = JsonDocument.Parse(await session.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        request.Headers.TryAddWithoutValidation(
            HumanAuthenticationHostOptions.AntiforgeryHeaderName,
            payload.RootElement.GetProperty("csrf_token").GetString());
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(HumanAuthenticationReasonCodes.MissingSession, body, StringComparison.Ordinal);
        Assert.DoesNotContain("attempt_id", body, StringComparison.Ordinal);
    }

    private static async Task<LoggedInContext> LoginAsync(
        string? relationship = null,
        IReadOnlyList<string>? actions = null,
        IParticipantNoticePort? notices = null)
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        var factory = CreateFactory(rsa, tokens, relationship, actions, notices);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        SeedBinding(factory);
        var cancellationToken = TestContext.Current.CancellationToken;
        using var login = await client.GetAsync("/auth/login?return_path=/my-work", cancellationToken);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        tokens.IdToken = CreateIdToken(rsa, query["nonce"].ToString());
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
        IParticipantNoticePort? notices = null)
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
            builder.UseSetting("HumanAuthentication:AcceptedAcr:0", "acr:mfa");
            builder.UseSetting("HumanAuthentication:AcceptedAmr:0", "mfa");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IOidcAuthorizationClient>(tokens);
                services.AddSingleton<IJwksKeySource>(new StaticJwksKeySource(keys));
                if (relationship is not null)
                {
                    services.AddSingleton<IAssessmentRelationshipResolver>(
                        new StubAssessmentRelationshipResolver(relationship, actions ?? []));
                }

                if (notices is not null)
                {
                    services.AddSingleton(notices);
                }
            });
        });
    }

    private static AllowEnrollmentAuthorizationPort AuthorizationPort(WebApplicationFactory<ApiProgram> factory) =>
        (AllowEnrollmentAuthorizationPort)factory.Services.GetRequiredService<IEnrollmentAuthorizationPort>();

    private static Guid SeedOwnedEnrollment(WebApplicationFactory<ApiProgram> factory, bool ownedByActor)
    {
        var identity = factory.Services.GetRequiredService<MemoryHumanIdentityBindingStore>()
            .FindByIdentityAsync(new ExactIssuerSubject(Issuer, Subject))
            .GetAwaiter()
            .GetResult()!;
        var organizationId = factory.Services.GetRequiredService<MemoryHumanIdentityBindingStore>()
            .ListEligibleOrganizationIdsAsync(identity.ActorId)
            .GetAwaiter()
            .GetResult()[0];
        var now = DateTimeOffset.UtcNow;
        var digest = new string('a', 64);
        var activityId = Guid.CreateVersion7();
        var cohortId = Guid.CreateVersion7();
        var baselineId = Guid.CreateVersion7();
        var taskSourceId = Guid.CreateVersion7();
        var taskVersionId = Guid.CreateVersion7();
        var enrollmentId = Guid.CreateVersion7();
        var participantId = ownedByActor ? identity.ActorId : Guid.CreateVersion7();
        factory.Services.GetRequiredService<InMemoryEnrollmentStore>().Restore(
            [
                Enrollment.Create(
                    enrollmentId,
                    organizationId,
                    activityId,
                    cohortId,
                    baselineId,
                    taskSourceId,
                    taskVersionId,
                    digest,
                    EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId,
                    EnrollmentLifecyclePolicy.RestrictedPreservationVersion,
                    participantId,
                    participantId,
                    now).Value!,
            ],
            []);
        ((FixedActivatedCohortPort)factory.Services.GetRequiredService<IActivatedCohortPort>()).Binding =
            new ActivatedCohortBinding(
                organizationId,
                activityId,
                cohortId,
                baselineId,
                digest,
                "activated",
                taskSourceId,
                taskVersionId,
                digest,
                "Campaign",
                "Task",
                "UTC",
                now.AddDays(-1),
                now.AddDays(30),
                now.AddDays(20),
                EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId,
                EnrollmentLifecyclePolicy.RestrictedPreservationVersion,
                false);
        return enrollmentId;
    }

    private static Guid SeedEligibleStart(WebApplicationFactory<ApiProgram> factory)
    {
        var enrollmentId = SeedOwnedEnrollment(factory, ownedByActor: true);
        var enrollment = factory.Services.GetRequiredService<InMemoryEnrollmentStore>().Items[0];
        var version = new AcceptedSubmissionVersion(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            new SubmissionParentScope(
                enrollment.OrganizationId,
                enrollment.ActivityId,
                enrollment.CohortId,
                enrollment.BaselineId,
                enrollment.EnrollmentId,
                enrollment.ParticipantActorId,
                enrollment.TaskSourceId,
                enrollment.TaskVersionId,
                enrollment.TaskContentDigest),
            new string('a', 64),
            null,
            DateTimeOffset.UtcNow,
            [new AcceptedVersionItem(Guid.CreateVersion7(), MaterialCategories.DirectText, null, 12, new string('a', 64), "obj", "v1")]);
        factory.Services.GetRequiredService<ISubmissionVersionStore>()
            .InsertAcceptedVersionAsync(version, enrollment.ParticipantActorId, new InMemoryEnrollmentTransaction(), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return enrollmentId;
    }

    private static RequiredNoticeProjection RequiredNotice() =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
            "instructions",
            "affirmed",
            "notice:1",
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2"),
            new string('d', 64),
            Guid.CreateVersion7());

    private static string CreateIdToken(RSA rsa, string nonce)
    {
        var now = DateTimeOffset.UtcNow;
        var header = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT", kid = "test" });
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["iss"] = Issuer,
            ["aud"] = ClientId,
            ["sub"] = Subject,
            ["nonce"] = nonce,
            ["sid"] = "sid-attempt-1",
            ["acr"] = "acr:mfa",
            ["amr"] = new[] { "mfa" },
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.AddMinutes(-1).ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(5).ToUnixTimeSeconds(),
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

    private static StringContent StartContent() =>
        new(
            """{"schema_version":"v2","idempotency_key":"attempt-start-synthetic-0001","trusted_command_digest":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}""",
            Encoding.UTF8,
            "application/json");

    private sealed record LoggedInContext(
        WebApplicationFactory<ApiProgram> Factory,
        HttpClient Client,
        string SessionCookie,
        string CsrfToken) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Factory.DisposeAsync();
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

    private sealed class FixedNoticePort(RequiredNoticeProjection notice) : IParticipantNoticePort
    {
        public Task<IReadOnlyList<RequiredNoticeProjection>?> ListRequiredAsync(
            Guid organizationId,
            Guid activityId,
            Guid cohortId,
            Guid baselineId,
            IEnrollmentTransaction? transaction,
            CancellationToken cancellationToken = default)
        {
            _ = (organizationId, activityId, cohortId, baselineId, transaction, cancellationToken);
            return Task.FromResult<IReadOnlyList<RequiredNoticeProjection>?>([notice]);
        }
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
