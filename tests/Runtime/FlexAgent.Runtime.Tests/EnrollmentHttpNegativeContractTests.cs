using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Buffers.Text;
using FlexAgent.Api;
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

public sealed class EnrollmentHttpNegativeContractTests
{
    private const string Issuer = "https://issuer.example/realms/flex";
    private const string ClientId = "flex-agent-api";
    private const string Subject = "enrollment-http-subject";

    [Fact]
    public async Task Assign_without_antiforgery_is_rejected_before_session_authentication()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.PostAsync(AssignUrl(), AssignContent(), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("csrf.invalid", body, StringComparison.Ordinal);
        AssertNoAssignment(body);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task My_work_without_a_session_is_unauthorized_and_not_cached()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/v1/assessment/my-work", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(HumanAuthenticationReasonCodes.MissingSession, body, StringComparison.Ordinal);
        AssertNoAssignment(body);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task My_work_over_the_actor_read_limit_is_rate_limited_and_not_cached()
    {
        await using var context = await LoginAsync(permitEnrollment: true, readPermitLimit: 2);
        using var first = await SendMyWorkAsync(context);
        using var second = await SendMyWorkAsync(context);
        using var third = await SendMyWorkAsync(context);
        var body = await third.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Contains(EnrollmentFailureCodes.RateLimited, body, StringComparison.Ordinal);
        AssertNoAssignment(body);
        Assert.Equal("no-store", third.Headers.CacheControl?.ToString());
        Assert.True(third.Headers.RetryAfter?.Delta >= TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Unauthenticated_my_work_does_not_acquire_a_shared_admission_permit()
    {
        var shared = new StubSharedAdmissionPort { Result = EnrollmentSharedAdmissionResult.Permitted() };
        await using var factory = CreateFactory(sharedAdmission: shared);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/v1/assessment/my-work", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, shared.AcquireCount);
    }

    [Fact]
    public async Task Shared_admission_exhaustion_is_rate_limited_without_running_protected_work()
    {
        var shared = new StubSharedAdmissionPort { Result = EnrollmentSharedAdmissionResult.Exhausted(7) };
        await using var context = await LoginAsync(
            permitEnrollment: true,
            sharedAdmission: shared,
            queries: new ThrowingEnrollmentQueryService());
        using var response = await SendMyWorkAsync(context);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Contains(EnrollmentFailureCodes.RateLimited, body, StringComparison.Ordinal);
        AssertNoAssignment(body);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(TimeSpan.FromSeconds(7), response.Headers.RetryAfter?.Delta);
        Assert.Equal(1, shared.AcquireCount);
    }

    [Fact]
    public async Task Shared_admission_uncertainty_is_unavailable_not_rate_limited()
    {
        var shared = new StubSharedAdmissionPort { Result = EnrollmentSharedAdmissionResult.Unavailable() };
        var telemetry = new RecordingEnrollmentTelemetry();
        await using var context = await LoginAsync(
            permitEnrollment: true,
            sharedAdmission: shared,
            queries: new ThrowingEnrollmentQueryService(),
            telemetry: telemetry);
        using var response = await SendMyWorkAsync(context);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains(EnrollmentFailureCodes.Unavailable, body, StringComparison.Ordinal);
        Assert.DoesNotContain(EnrollmentFailureCodes.RateLimited, body, StringComparison.Ordinal);
        AssertNoAssignment(body);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Null(response.Headers.RetryAfter);
        Assert.Equal(EnrollmentTelemetryLabels.Unavailable, telemetry.Points[0][EnrollmentTelemetryLabels.Decision]);
        Assert.All(telemetry.Points[0], pair =>
        {
            Assert.Contains(pair.Key, EnrollmentTelemetryLabels.AllowedKeys);
            Assert.Contains(pair.Value, EnrollmentTelemetryLabels.AllowedValues);
        });
    }

    [Fact]
    public async Task Tampered_my_work_cursor_is_invalid_and_not_cached()
    {
        await using var context = await LoginAsync(permitEnrollment: true);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/assessment/my-work?cursor=not-a-cursor");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EnrollmentFailureCodes.InvalidField, body, StringComparison.Ordinal);
        AssertNoAssignment(body);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task My_work_with_an_out_of_range_limit_is_invalid_and_not_cached()
    {
        await using var context = await LoginAsync(permitEnrollment: true);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/assessment/my-work?limit=0");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EnrollmentFailureCodes.InvalidField, body, StringComparison.Ordinal);
        AssertNoAssignment(body);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task My_work_with_an_over_maximum_limit_is_invalid_and_not_cached()
    {
        await using var context = await LoginAsync(permitEnrollment: true);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/assessment/my-work?limit=999999");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(
            EnrollmentFailureCodes.InvalidField,
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task My_work_with_an_unparsable_limit_is_invalid_and_not_cached()
    {
        await using var context = await LoginAsync(permitEnrollment: true);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/assessment/my-work?limit=not-a-number");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EnrollmentFailureCodes.InvalidField, body, StringComparison.Ordinal);
        AssertNoAssignment(body);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task My_work_with_an_overlong_cursor_is_invalid_and_not_cached()
    {
        await using var context = await LoginAsync(permitEnrollment: true);
        var cursor = new string('a', EnrollmentPageBounds.MaximumCursorLength + 1);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/assessment/my-work?cursor={cursor}");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EnrollmentFailureCodes.InvalidField, body, StringComparison.Ordinal);
        AssertNoAssignment(body);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Guessed_my_work_detail_is_concealed_as_not_found()
    {
        await using var context = await LoginAsync();
        var enrollmentId = Guid.CreateVersion7();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/assessment/my-work/{enrollmentId}");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(EnrollmentFailureCodes.Denied, body, StringComparison.Ordinal);
        AssertNoAssignment(body);
        Assert.DoesNotContain("open_assignment", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Assign_with_an_unknown_member_is_invalid()
    {
        await using var context = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, AssignUrl())
        {
            Content = new StringContent(
                """{"schema_version":"v1","participant_actor_id":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab","idempotency_key":"enr-1","display_label":"no"}""",
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EnrollmentFailureCodes.InvalidField, body, StringComparison.Ordinal);
        AssertNoAssignment(body);
    }

    [Fact]
    public async Task Assign_with_an_oversized_body_is_invalid()
    {
        await using var context = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, AssignUrl())
        {
            Content = new StringContent(
                "{\"schema_version\":\"v1\",\"participant_actor_id\":\"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab\",\"idempotency_key\":\""
                + new string('a', 5000)
                + "\"}",
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        request.Headers.TryAddWithoutValidation(HumanAuthenticationHostOptions.AntiforgeryHeaderName, context.CsrfToken);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(
            EnrollmentFailureCodes.InvalidField,
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
    }

    private static async Task<LoggedInContext> LoginAsync(
        bool permitEnrollment = false,
        int? readPermitLimit = null,
        IEnrollmentSharedAdmissionPort? sharedAdmission = null,
        IEnrollmentQueryService? queries = null,
        IEnrollmentTelemetry? telemetry = null)
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        var factory = CreateFactory(rsa, tokens, permitEnrollment, readPermitLimit, sharedAdmission, queries, telemetry);
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

    private static async Task<HttpResponseMessage> SendMyWorkAsync(LoggedInContext context)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/assessment/my-work");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        return await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static WebApplicationFactory<ApiProgram> CreateFactory(
        RSA? rsa = null,
        FakeOidcAuthorizationClient? tokens = null,
        bool permitEnrollment = false,
        int? readPermitLimit = null,
        IEnrollmentSharedAdmissionPort? sharedAdmission = null,
        IEnrollmentQueryService? queries = null,
        IEnrollmentTelemetry? telemetry = null)
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
            if (readPermitLimit is not null)
            {
                builder.UseSetting("Enrollment:RequestLimits:ReadPermitLimit", readPermitLimit.Value.ToString());
            }
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IOidcAuthorizationClient>(tokens);
                services.AddSingleton<IJwksKeySource>(new StaticJwksKeySource(keys));
                if (permitEnrollment)
                {
                    services.AddSingleton<IEnrollmentAuthorizationPort>(_ => new AllowEnrollmentAuthorizationPort { Permit = true });
                }
                if (sharedAdmission is not null)
                {
                    services.AddSingleton(sharedAdmission);
                }
                if (queries is not null)
                {
                    services.AddSingleton(queries);
                }
                if (telemetry is not null)
                {
                    services.AddSingleton(telemetry);
                }
            });
        });
    }

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
            ["sid"] = "sid-enroll-1",
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

    private static StringContent AssignContent() =>
        new(
            """{"schema_version":"v1","participant_actor_id":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab","idempotency_key":"enr-1"}""",
            Encoding.UTF8,
            "application/json");

    private static string AssignUrl() =>
        $"/v1/assessment/activities/{Guid.CreateVersion7()}/cohorts/{Guid.CreateVersion7()}/enrollments";

    private static void AssertNoAssignment(string body)
    {
        Assert.DoesNotContain("\"activity_title\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("open_assignment", body, StringComparison.Ordinal);
        Assert.DoesNotContain("start_attempt", body, StringComparison.Ordinal);
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

    private sealed class StubSharedAdmissionPort : IEnrollmentSharedAdmissionPort
    {
        public EnrollmentSharedAdmissionResult Result { get; init; }

        public int AcquireCount;

        public Task<EnrollmentSharedAdmissionResult> AcquireAsync(
            Guid organizationId,
            Guid actorId,
            string surface,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref AcquireCount);
            return Task.FromResult(Result);
        }

        public Task<bool> PolicyMatchesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class ThrowingEnrollmentQueryService : IEnrollmentQueryService
    {
        public Task<EnrollmentDecision<CursorPage<EnrollmentCandidate>>> ListCandidatesAsync(
            EnrollmentActorContext actor,
            Guid activityId,
            Guid cohortId,
            string? prefix,
            string? cursor,
            int limit,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Protected Enrollment query must not run.");

        public Task<EnrollmentDecision<CursorPage<EnrollmentSummary>>> ListEnrollmentsAsync(
            EnrollmentActorContext actor,
            Guid activityId,
            Guid cohortId,
            string? cursor,
            int limit,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Protected Enrollment query must not run.");

        public Task<EnrollmentDecision<EnrollmentDetail>> GetEnrollmentAsync(
            EnrollmentActorContext actor,
            Guid activityId,
            Guid cohortId,
            Guid enrollmentId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Protected Enrollment query must not run.");

        public Task<EnrollmentDecision<CursorPage<AssignmentSummary>>> ListMyWorkAsync(
            EnrollmentActorContext actor,
            string? cursor,
            int limit,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Protected Enrollment query must not run.");

        public Task<EnrollmentDecision<AssignmentSummary>> GetMyWorkAsync(
            EnrollmentActorContext actor,
            Guid enrollmentId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Protected Enrollment query must not run.");
    }
}
