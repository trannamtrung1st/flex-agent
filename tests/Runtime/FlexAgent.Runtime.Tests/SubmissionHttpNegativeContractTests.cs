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

public sealed class SubmissionHttpNegativeContractTests
{
    private const string Issuer = "https://issuer.example/realms/flex";
    private const string ClientId = "flex-agent-api";
    private const string Subject = "submission-http-subject";

    [Fact]
    public async Task Begin_intake_without_antiforgery_is_rejected_before_session_authentication()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.PostAsync(
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/submission/intake",
            new StringContent("""{"schema_version":"v2","idempotency_key":"sub-1"}""", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("csrf.invalid", body, StringComparison.Ordinal);
        Assert.DoesNotContain("intake_id", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Cancel_intake_without_antiforgery_is_rejected_before_session_authentication()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.PostAsync(
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/submission/intake/{Guid.CreateVersion7()}/cancel",
            new StringContent(
                """{"schema_version":"v2","expected_revision":1,"idempotency_key":"sub-cancel-1"}""",
                Encoding.UTF8,
                "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("csrf.invalid", body, StringComparison.Ordinal);
        Assert.DoesNotContain("cancelled", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Finalize_intake_without_antiforgery_is_rejected_before_session_authentication()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.PostAsync(
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/submission/intake/{Guid.CreateVersion7()}/finalize",
            new StringContent(
                """{"schema_version":"v2","expected_revision":1,"idempotency_key":"sub-finalize-1"}""",
                Encoding.UTF8,
                "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("csrf.invalid", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"accepted\"", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task My_work_submission_without_a_session_is_unauthorized_and_not_cached()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/submission",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(HumanAuthenticationReasonCodes.MissingSession, body, StringComparison.Ordinal);
        Assert.DoesNotContain("intake_available", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Item_preview_without_a_session_is_unauthorized_and_not_cached()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/submission/versions/{Guid.CreateVersion7()}/items/{Guid.CreateVersion7()}/preview",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(HumanAuthenticationReasonCodes.MissingSession, body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"content\"", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Item_download_without_a_session_is_unauthorized_and_not_cached()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/submission/versions/{Guid.CreateVersion7()}/items/{Guid.CreateVersion7()}/download",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(HumanAuthenticationReasonCodes.MissingSession, body, StringComparison.Ordinal);
        Assert.DoesNotContain("Direct text", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Unauthenticated_submission_read_does_not_acquire_a_shared_admission_permit()
    {
        var shared = new StubSharedAdmissionPort { Result = EnrollmentSharedAdmissionResult.Permitted() };
        await using var factory = CreateFactory(sharedAdmission: shared);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/submission",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, shared.AcquireCount);
    }

    [Fact]
    public async Task Shared_admission_exhaustion_does_not_run_protected_submission_query()
    {
        var shared = new StubSharedAdmissionPort { Result = EnrollmentSharedAdmissionResult.Exhausted(7) };
        await using var context = await LoginAsync(
            permitEnrollment: true,
            sharedAdmission: shared,
            queries: new ThrowingSubmissionQueryService());
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/v2/assessment/my-work/{Guid.CreateVersion7()}/submission");
        request.Headers.TryAddWithoutValidation("Cookie", context.SessionCookie);
        using var response = await context.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Contains(EnrollmentFailureCodes.RateLimited, body, StringComparison.Ordinal);
        Assert.DoesNotContain("intake_available", body, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(1, shared.AcquireCount);
    }

    private static async Task<LoggedInContext> LoginAsync(
        bool permitEnrollment = false,
        IEnrollmentSharedAdmissionPort? sharedAdmission = null,
        ISubmissionQueryService? queries = null)
    {
        var rsa = RSA.Create(2048);
        var tokens = new FakeOidcAuthorizationClient();
        var factory = CreateFactory(rsa, tokens, permitEnrollment, sharedAdmission, queries);
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
        bool permitEnrollment = false,
        IEnrollmentSharedAdmissionPort? sharedAdmission = null,
        ISubmissionQueryService? queries = null)
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
            ["sid"] = "sid-sub-1",
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

    private sealed class ThrowingSubmissionQueryService : ISubmissionQueryService
    {
        public Task<QueryResult<MyWorkSubmissionProjection>> GetMyWorkSubmissionAsync(
            EnrollmentActorContext actor,
            Guid enrollmentId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Protected Submission query must not run.");

        public Task<QueryResult<AcceptedVersionDetail>> GetAcceptedVersionAsync(
            EnrollmentActorContext actor,
            Guid enrollmentId,
            Guid versionId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Protected Submission query must not run.");

        public Task<QueryResult<ProtectedItemContent>> GetAcceptedItemPreviewAsync(
            EnrollmentActorContext actor,
            Guid enrollmentId,
            Guid versionId,
            Guid itemId,
            CancellationToken cancellationToken = default,
            string accessKind = SubmissionPermittedActions.PreviewItem) =>
            throw new InvalidOperationException("Protected Submission query must not run.");
    }
}
