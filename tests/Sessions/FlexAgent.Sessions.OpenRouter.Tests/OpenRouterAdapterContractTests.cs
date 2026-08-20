using System.Net;
using System.Text;
using System.Text.Json;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.OpenRouter;
using FlexAgent.Sessions.Tests.Domain;
using Json.Schema;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterAdapterContractTests
{
    private const string Model = "meta-llama/llama-3.1-8b-instruct:free";
    private const string Provider = "Together";
    private const string CanarySecret = "sk-or-canary-secret-do-not-leak";
    private const string CanaryPrompt = "synthetic.canary.prompt.do-not-log";

    [Fact]
    public async Task Control_request_pins_destination_headers_provider_object_and_concrete_model()
    {
        var harness = CreateHarness();
        var handler = new RecordingHandler(ControlBody(harness.InvocationJson()));
        var result = await harness.Adapter(handler).ExecuteAsync(harness.ControlRequest(CanaryPrompt), CancellationToken.None);

        Assert.IsType<ModelExecutionStructuredControl>(result);
        Assert.Equal(OpenRouterDestination.ChatCompletionsUri, handler.RequestUri);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("Bearer " + CanarySecret, handler.Authorization);
        Assert.Equal("enabled", handler.MetadataHeader);
        Assert.Equal("false", handler.CacheHeader);
        Assert.DoesNotContain("HTTP-Referer", handler.HeaderNames, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Title", handler.HeaderNames, StringComparer.OrdinalIgnoreCase);
        using var body = JsonDocument.Parse(handler.Body);
        Assert.Equal(Model, body.RootElement.GetProperty("model").GetString());
        Assert.Equal(256, body.RootElement.GetProperty("max_tokens").GetInt32());
        var provider = body.RootElement.GetProperty("provider");
        Assert.Equal("Together", Assert.Single(provider.GetProperty("only").EnumerateArray()).GetString());
        Assert.False(provider.GetProperty("allow_fallbacks").GetBoolean());
        Assert.True(provider.GetProperty("require_parameters").GetBoolean());
        Assert.Equal("allow", provider.GetProperty("data_collection").GetString());
        Assert.False(provider.GetProperty("zdr").GetBoolean());
        Assert.False(body.RootElement.TryGetProperty("plugins", out _));
        Assert.Equal("json_schema", body.RootElement.GetProperty("response_format").GetProperty("type").GetString());
        Assert.True(body.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("strict").GetBoolean());
        Assert.DoesNotContain(CanarySecret, handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Discovery_omits_provider_only_and_records_the_selected_free_endpoint()
    {
        var handler = new RecordingHandler(
            DiscoveryBody(Model, Provider, availableFirstProvider: "Groq"));
        var client = new OpenRouterDiscoveryClient(handler);
        var candidate = await client.DiscoverAsync(CanarySecret, CancellationToken.None);

        Assert.NotNull(candidate);
        Assert.Equal(Model, candidate.Model);
        Assert.Equal(Provider, candidate.ProviderIdentity);
        Assert.Equal(OpenRouterDestination.ChatCompletionsUri, handler.RequestUri);
        Assert.Equal("Bearer " + CanarySecret, handler.Authorization);
        Assert.Equal("enabled", handler.MetadataHeader);
        Assert.Equal("false", handler.CacheHeader);
        using var body = JsonDocument.Parse(handler.Body);
        Assert.Equal(OpenRouterAdapterContracts.DiscoveryModel, body.RootElement.GetProperty("model").GetString());
        Assert.False(body.RootElement.GetProperty("provider").TryGetProperty("only", out _));
        Assert.False(body.RootElement.GetProperty("provider").GetProperty("allow_fallbacks").GetBoolean());
        Assert.True(body.RootElement.GetProperty("provider").GetProperty("require_parameters").GetBoolean());
        Assert.Equal("allow", body.RootElement.GetProperty("provider").GetProperty("data_collection").GetString());
        Assert.False(body.RootElement.GetProperty("provider").GetProperty("zdr").GetBoolean());
    }

    [Fact]
    public async Task Discovery_rejects_returned_alias_cache_hit_and_default_transport_without_opt_in()
    {
        var alias = await new OpenRouterDiscoveryClient(
            new RecordingHandler(DiscoveryBody("openrouter/free", Provider)))
            .DiscoverAsync(CanarySecret, CancellationToken.None);
        Assert.Null(alias);

        var cache = await new OpenRouterDiscoveryClient(
            new RecordingHandler(DiscoveryBody(Model, Provider), cacheStatus: "HIT"))
            .DiscoverAsync(CanarySecret, CancellationToken.None);
        Assert.Null(cache);

        var promptCache = await new OpenRouterDiscoveryClient(
            new RecordingHandler(DiscoveryBody(Model, Provider, cachedTokens: 2)))
            .DiscoverAsync(CanarySecret, CancellationToken.None);
        Assert.NotNull(promptCache);

        Assert.Null(
            await new OpenRouterDiscoveryClient().DiscoverAsync(CanarySecret, CancellationToken.None));
    }

    [Fact]
    public async Task Discovery_reports_only_sanitized_failure_categories()
    {
        var unauthorized = await new OpenRouterDiscoveryClient(new StatusHandler(401))
            .DiscoverOutcomeAsync(CanarySecret, CancellationToken.None);
        Assert.Null(unauthorized.Candidate);
        Assert.Equal(OpenRouterDiscoveryFailureReasons.Authentication, unauthorized.FailureReason);
        Assert.Equal(401, unauthorized.HttpStatusCode);

        var missingMetadata = await new OpenRouterDiscoveryClient(
                new RecordingHandler(DiscoveryBody(Model, Provider).Replace("\"openrouter_metadata\"", "\"ignored_metadata\"", StringComparison.Ordinal)))
            .DiscoverOutcomeAsync(CanarySecret, CancellationToken.None);
        Assert.Null(missingMetadata.Candidate);
        Assert.Equal(OpenRouterDiscoveryFailureReasons.MissingProviderMetadata, missingMetadata.FailureReason);
        Assert.Equal(200, missingMetadata.HttpStatusCode);

        var cacheHit = await new OpenRouterDiscoveryClient(
                new RecordingHandler(DiscoveryBody(Model, Provider), cacheStatus: "HIT"))
            .DiscoverOutcomeAsync(CanarySecret, CancellationToken.None);
        Assert.Null(cacheHit.Candidate);
        Assert.Equal(OpenRouterDiscoveryFailureReasons.ResponseCacheHit, cacheHit.FailureReason);
        Assert.Equal(200, cacheHit.HttpStatusCode);
    }

    [Fact]
    public async Task Discovery_rejects_provider_controlled_identity_values_that_are_unsafe_for_evidence()
    {
        var unsafeModel = await new OpenRouterDiscoveryClient(
                new RecordingHandler(DiscoveryBody("model\ninjected:free", Provider)))
            .DiscoverOutcomeAsync(CanarySecret, CancellationToken.None);
        Assert.Null(unsafeModel.Candidate);
        Assert.Equal(OpenRouterDiscoveryFailureReasons.ModelIdentity, unsafeModel.FailureReason);

        var unsafeProvider = await new OpenRouterDiscoveryClient(
                new RecordingHandler(DiscoveryBody(Model, "Together\ninjected")))
            .DiscoverOutcomeAsync(CanarySecret, CancellationToken.None);
        Assert.Null(unsafeProvider.Candidate);
        Assert.Equal(OpenRouterDiscoveryFailureReasons.ProviderIdentity, unsafeProvider.FailureReason);
    }

    [Fact]
    public async Task Discovery_alias_and_digest_mismatch_fail_before_network_io()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OpenRouterInstalledConfiguration.Create(
                "openrouter.synthetic.example",
                "1",
                "openrouter/free",
                "openrouter/free",
                Provider,
                Provider,
                ModelDeploymentCredentialModes.OrganizationByok,
                "openrouter.synthetic"));

        var harness = CreateHarness();
        var wrong = new InMemoryOpenRouterInstalledConfigurationRegistry();
        var adapter = new OpenRouterModelExecutionAdapter(
            harness.Profiles,
            harness.Catalog,
            harness.Secrets,
            wrong,
            new CountingHandler(),
            syntheticDataPolicyAccepted: true);
        var result = await adapter.ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.CredentialBindingFailed,
            Assert.IsType<ModelExecutionFailed>(result).ReasonCategory);
    }

    [Fact]
    public async Task Missing_metadata_cache_hit_provider_drift_and_attempt_drift_fail_closed()
    {
        var harness = CreateHarness();
        var json = harness.InvocationJson();
        var missing = await harness.Adapter(new RecordingHandler(ControlBody(json, includeMetadata: false)))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(ExecutionFailureReasons.ProviderUnavailable, Assert.IsType<ModelExecutionFailed>(missing).ReasonCategory);

        var drift = await harness.Adapter(new RecordingHandler(ControlBody(json, provider: "Groq")))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(ExecutionFailureReasons.ProviderUnavailable, Assert.IsType<ModelExecutionFailed>(drift).ReasonCategory);

        var attempts = await harness.Adapter(new RecordingHandler(ControlBody(json, attempt: 2)))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(ExecutionFailureReasons.ProviderUnavailable, Assert.IsType<ModelExecutionFailed>(attempts).ReasonCategory);

        var missingUsage = await harness.Adapter(new RecordingHandler(ControlBody(json, includeUsage: false)))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelExecutionFailed>(missingUsage).ReasonCategory);

        var promptCache = await harness.Adapter(new RecordingHandler(ControlBody(json, cachedTokens: 3)))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.IsType<ModelExecutionStructuredControl>(promptCache);

        var responseCache = await harness.Adapter(new RecordingHandler(ControlBody(json), cacheStatus: "HIT"))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelExecutionFailed>(responseCache).ReasonCategory);
    }

    [Fact]
    public async Task Control_body_stall_after_headers_is_provider_timeout_not_caller_cancel()
    {
        var harness = CreateHarness();
        var timedOut = await harness.Adapter(new StallAfterHeadersHandler(), testControlTimeout: TimeSpan.FromMilliseconds(250))
            .ExecuteAsync(harness.ControlRequest(), TestContext.Current.CancellationToken);
        Assert.Equal(
            ExecutionFailureReasons.ProviderTimeout,
            Assert.IsType<ModelExecutionFailed>(timedOut).ReasonCategory);

        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var defaultHarness = CreateHarness();
        var pending = defaultHarness.Adapter(new StallAfterHeadersHandler())
            .ExecuteAsync(defaultHarness.ControlRequest(), caller.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await caller.CancelAsync();
        var cancelled = await pending;
        Assert.Equal(
            ExecutionAttemptOutcomeCategories.Cancelled,
            Assert.IsType<ModelExecutionFailed>(cancelled).ReasonCategory);
    }

    [Fact]
    public async Task Control_rejects_an_oversized_provider_envelope_before_decision_admission()
    {
        var harness = CreateHarness();
        var padding = new string('x', OpenRouterAdapterContracts.MaxControlEnvelopeUtf8Bytes);
        var body = ControlBody(harness.InvocationJson())[..^1] + ",\"padding\":\"" + padding + "\"}";
        var result = await harness.Adapter(new RecordingHandler(body))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelExecutionFailed>(result).ReasonCategory);
    }

    [Fact]
    public async Task Control_accepts_an_envelope_at_the_byte_limit_and_rejects_one_extra_byte()
    {
        var harness = CreateHarness();
        var json = harness.InvocationJson();
        var exact = ControlBodyWithPadding(json, OpenRouterAdapterContracts.MaxControlEnvelopeUtf8Bytes);
        Assert.Equal(OpenRouterAdapterContracts.MaxControlEnvelopeUtf8Bytes, Encoding.UTF8.GetByteCount(exact));
        Assert.IsType<ModelExecutionStructuredControl>(
            await harness.Adapter(new RecordingHandler(exact))
                .ExecuteAsync(harness.ControlRequest(), TestContext.Current.CancellationToken));

        var over = ControlBodyWithPadding(json, OpenRouterAdapterContracts.MaxControlEnvelopeUtf8Bytes + 1);
        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelExecutionFailed>(
                await harness.Adapter(new RecordingHandler(over))
                    .ExecuteAsync(harness.ControlRequest(), TestContext.Current.CancellationToken)).ReasonCategory);
    }

    [Fact]
    public async Task Streaming_body_stall_after_headers_is_provider_timeout_not_caller_cancel()
    {
        var harness = CreateHarness();
        var events = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(
                new StallAfterHeadersHandler(),
                testContentTimeout: TimeSpan.FromMilliseconds(250))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), TestContext.Current.CancellationToken))
        {
            events.Add(item);
        }

        Assert.Equal(
            ExecutionFailureReasons.ProviderTimeout,
            Assert.IsType<ModelContentFailed>(Assert.Single(events)).ReasonCategory);

        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var pending = new List<ModelContentEvent>();
        var enumerate = EnumerateAsync();
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await caller.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(() => enumerate);
        Assert.Equal(
            ExecutionAttemptOutcomeCategories.Cancelled,
            Assert.IsType<ModelContentFailed>(Assert.Single(pending)).ReasonCategory);

        async Task EnumerateAsync()
        {
            await foreach (var item in harness.Adapter(new StallAfterHeadersHandler())
                .StreamParticipantVisibleContentAsync(harness.StreamRequest(), caller.Token))
            {
                pending.Add(item);
            }
        }
    }

    [Fact]
    public async Task Streaming_rejects_malformed_utf8_before_and_after_the_first_fragment()
    {
        var harness = CreateHarness();
        var before = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new RawSseHandler(MalformedUtf8Sse(includeFragment: false)))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), TestContext.Current.CancellationToken))
        {
            before.Add(item);
        }

        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelContentFailed>(Assert.Single(before)).ReasonCategory);

        var after = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new RawSseHandler(MalformedUtf8Sse(includeFragment: true)))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), TestContext.Current.CancellationToken))
        {
            after.Add(item);
        }

        Assert.Equal("Hi", Assert.IsType<ModelContentTextDelta>(after[0]).ExactUtf8Text);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelContentFailed>(after[^1]).ReasonCategory);
        Assert.DoesNotContain(after, item => item is ModelContentCompleted);
    }

    [Fact]
    public async Task Escaped_invalid_surrogates_fail_closed_instead_of_escaping_the_adapter()
    {
        var harness = CreateHarness();
        var json = harness.InvocationJson();
        var control = await harness.Adapter(new RecordingHandler(ControlBody(json, rawAssistantContentJson: "\"\\uD800\"")))
            .ExecuteAsync(harness.ControlRequest(), TestContext.Current.CancellationToken);
        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelExecutionFailed>(control).ReasonCategory);

        var model = await harness.Adapter(new RecordingHandler(ControlBody(json, rawModelJson: "\"\\uD800\"")))
            .ExecuteAsync(harness.ControlRequest(), TestContext.Current.CancellationToken);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelExecutionFailed>(model).ReasonCategory);

        var provider = await harness.Adapter(new RecordingHandler(ControlBody(json, rawProviderJson: "\"\\uD800\"")))
            .ExecuteAsync(harness.ControlRequest(), TestContext.Current.CancellationToken);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelExecutionFailed>(provider).ReasonCategory);

        var events = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new StreamingHandler(["Hi"], escapedInvalidSurrogate: true))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), TestContext.Current.CancellationToken))
        {
            events.Add(item);
        }

        Assert.Equal("Hi", Assert.IsType<ModelContentTextDelta>(events[0]).ExactUtf8Text);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelContentFailed>(events[^1]).ReasonCategory);
        Assert.DoesNotContain(events, item => item is ModelContentCompleted);
    }

    [Fact]
    public async Task Streaming_requires_exactly_one_terminal_metadata_then_done()
    {
        var harness = CreateHarness();
        var missingDone = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new StreamingHandler(["Hi"], omitDone: true))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            missingDone.Add(item);
        }

        Assert.Equal("Hi", Assert.IsType<ModelContentTextDelta>(missingDone[0]).ExactUtf8Text);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelContentFailed>(missingDone[^1]).ReasonCategory);
        Assert.DoesNotContain(missingDone, item => item is ModelContentCompleted);

        var duplicate = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new StreamingHandler(["Hi"], duplicateTerminal: true))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            duplicate.Add(item);
        }

        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelContentFailed>(duplicate[^1]).ReasonCategory);
        Assert.DoesNotContain(duplicate, item => item is ModelContentCompleted);

        var extra = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new StreamingHandler(["Hi"], extraAfterTerminal: true))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            extra.Add(item);
        }

        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelContentFailed>(extra[^1]).ReasonCategory);
    }

    [Fact]
    public async Task Streaming_rejects_oversized_events_and_excessive_visible_content()
    {
        var harness = CreateHarness();
        var oversized = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new StreamingHandler(["Hi"], oversizedEvent: true))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            oversized.Add(item);
        }

        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelContentFailed>(Assert.Single(oversized)).ReasonCategory);

        var excessive = new List<ModelContentEvent>();
        var huge = new string('a', OpenRouterAdapterContracts.MaxVisibleContentUtf8Bytes);
        await foreach (var item in harness.Adapter(new StreamingHandler(["ok", huge]))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            excessive.Add(item);
        }

        Assert.Equal("ok", Assert.IsType<ModelContentTextDelta>(excessive[0]).ExactUtf8Text);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelContentFailed>(excessive[^1]).ReasonCategory);
        Assert.DoesNotContain(excessive, item => item is ModelContentCompleted);
    }

    [Fact]
    public async Task Malformed_refusal_empty_and_oversized_control_fail_without_healing()
    {
        var harness = CreateHarness();
        var malformed = await harness.Adapter(new RecordingHandler(ControlBody("{ not json")))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(ExecutionFailureReasons.MalformedControl, Assert.IsType<ModelExecutionFailed>(malformed).ReasonCategory);

        var empty = await harness.Adapter(new RecordingHandler(ControlBody("")))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(ExecutionFailureReasons.MalformedControl, Assert.IsType<ModelExecutionFailed>(empty).ReasonCategory);

        var oversized = await harness.Adapter(new RecordingHandler(ControlBody(harness.InvocationJson())))
            .ExecuteAsync(harness.ControlRequest(maxControlUtf8Bytes: 16), CancellationToken.None);
        Assert.Equal(ExecutionFailureReasons.MalformedControl, Assert.IsType<ModelExecutionFailed>(oversized).ReasonCategory);
    }

    [Fact]
    public async Task Streaming_emits_non_overlapping_unicode_fragments_then_terminal_metadata()
    {
        var harness = CreateHarness();
        var events = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new StreamingHandler(["Hel", "lo 🌍"]))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            events.Add(item);
        }

        Assert.Equal("Hel", Assert.IsType<ModelContentTextDelta>(events[0]).ExactUtf8Text);
        Assert.Equal("lo 🌍", Assert.IsType<ModelContentTextDelta>(events[1]).ExactUtf8Text);
        Assert.IsType<ModelContentCompleted>(events[^1]);
    }

    [Fact]
    public async Task Failure_after_a_fragment_does_not_fabricate_completion_or_restart()
    {
        var harness = CreateHarness();
        var events = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new StreamingHandler(["Hi"], truncateAfterFirst: true))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            events.Add(item);
        }

        Assert.Equal("Hi", Assert.IsType<ModelContentTextDelta>(events[0]).ExactUtf8Text);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelContentFailed>(events[^1]).ReasonCategory);
        Assert.DoesNotContain(events, item => item is ModelContentCompleted);
    }

    [Fact]
    public async Task Http_errors_timeouts_redirects_and_private_destinations_fail_closed()
    {
        var harness = CreateHarness();
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelExecutionFailed>(
                await harness.Adapter(new StatusHandler(429)).ExecuteAsync(harness.ControlRequest(), CancellationToken.None)).ReasonCategory);
        Assert.Equal(
            ExecutionFailureReasons.ProviderTimeout,
            Assert.IsType<ModelExecutionFailed>(
                await harness.Adapter(new StatusHandler(408)).ExecuteAsync(harness.ControlRequest(), CancellationToken.None)).ReasonCategory);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelExecutionFailed>(
                await harness.Adapter(new RedirectHandler("https://127.0.0.1/api/v1/chat/completions"))
                    .ExecuteAsync(harness.ControlRequest(), CancellationToken.None)).ReasonCategory);
        Assert.False(OpenRouterDestination.IsAllowed(new Uri("https://example.com/api/v1/chat/completions")));
        Assert.False(OpenRouterDestination.IsAllowed(new Uri("http://openrouter.ai/api/v1/chat/completions")));
        Assert.False(OpenRouterDestination.IsAllowed(new Uri("https://openrouter.ai:8443/api/v1/chat/completions")));
        Assert.False(OpenRouterDestination.IsAllowed(new Uri("https://openrouter.ai/api/v1/models")));
        Assert.False(OpenRouterDestination.IsAllowed(new Uri("https://10.0.0.1/api/v1/chat/completions")));
        Assert.True(OpenRouterDestination.IsAllowed(OpenRouterDestination.ChatCompletionsUri));
    }

    [Fact]
    public async Task Privacy_preflight_false_and_canary_secret_do_not_leave_the_adapter()
    {
        var harness = CreateHarness();
        var counting = new CountingHandler();
        var closed = new OpenRouterModelExecutionAdapter(
            harness.Profiles,
            harness.Catalog,
            harness.Secrets,
            harness.Configurations,
            counting,
            syntheticDataPolicyAccepted: false);
        var result = await closed.ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(0, counting.Requests);
        Assert.Equal(ExecutionFailureReasons.ProviderUnavailable, Assert.IsType<ModelExecutionFailed>(result).ReasonCategory);

        using var secret = new ProviderSecret(CanarySecret);
        Assert.Equal("[redacted]", secret.ToString());
        Assert.DoesNotContain(CanarySecret, secret.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(CanaryPrompt, result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Streaming_completes_when_the_terminal_event_has_no_trailing_blank_line()
    {
        var harness = CreateHarness();
        var events = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new StreamingHandler(["Hi"], omitTrailingBlankLine: true))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            events.Add(item);
        }

        Assert.Equal("Hi", Assert.IsType<ModelContentTextDelta>(events[0]).ExactUtf8Text);
        Assert.IsType<ModelContentCompleted>(events[^1]);
    }

    [Fact]
    public void Transport_schema_matches_canonical_v2_fixtures_and_keeps_p0_denial_independent()
    {
        var options = new EvaluationOptions { RequireFormatValidation = true };
        using var projectionDocument = JsonDocument.Parse(OpenRouterTransportSchema.ReadUtf8());
        var projection = JsonSchema.FromText(OpenRouterTransportSchema.ReadUtf8Text());
        var fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "contracts", "fixtures", "schema", "v2", "session", "agent-decision");
        foreach (var path in Directory.GetFiles(fixtureDirectory, "*.json"))
        {
            var utf8 = File.ReadAllBytes(path);
            using var fixture = JsonDocument.Parse(utf8);
            var projected = projection.Evaluate(fixture.RootElement, options).IsValid;
            var canonical = AgentDecisionV2SchemaValidator.IsSchemaValid(utf8);
            Assert.Equal(canonical, projected);
        }

        var voice = File.ReadAllBytes(Path.Combine(fixtureDirectory, "valid-respond-message-and-voice.json"));
        Assert.True(AgentDecisionV2SchemaValidator.IsSchemaValid(voice));
        Assert.True(ValidatedAgentDecisionEnvelope.TryAdmit(voice, out var admitted, out _) && admitted is not null);
        var profile = P0DecisionProfileValidator.Validate(
            admitted!.Envelope,
            RuntimePolicyTestFixtures.ResolveEnabledTimerPolicy(),
            P0TextSessionRuntimeCapabilityPolicy.Create().IsDecisionTypeSupportedByP0);
        Assert.Equal(DecisionValidationOutcomes.Rejected, profile.Outputs[1].ValidationOutcome);

        var unknown = File.ReadAllBytes(Path.Combine(fixtureDirectory, "invalid-unknown-output-kind.json"));
        Assert.False(AgentDecisionV2SchemaValidator.IsSchemaValid(unknown));
        Assert.False(projection.Evaluate(JsonDocument.Parse(unknown).RootElement, options).IsValid);
        _ = projectionDocument;
    }

    [Fact]
    public void Live_qualification_remains_opt_in_and_does_not_read_the_key()
    {
        Assert.False(OpenRouterLiveQualification.IsEnabled);
        Assert.Equal("sessions.openrouter.v1", OpenRouterModelExecutionAdapter.AdapterContractVersion);
        Assert.NotEqual(CanarySecret, Environment.GetEnvironmentVariable("HOME"));
    }

    internal static Harness CreateHarness()
    {
        var configuration = OpenRouterInstalledConfiguration.Create(
            "openrouter.synthetic.example",
            "1",
            Model,
            Model,
            Provider,
            Provider,
            ModelDeploymentCredentialModes.OrganizationByok,
            "openrouter.synthetic");
        var profile = configuration.Profile;
        var frozen = new FrozenModelDeploymentBinding(
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.ProviderId,
            ModelDeploymentCredentialModes.OrganizationByok,
            "bind.opaque.0001",
            "bind.v1");
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var profiles = new InMemoryInstalledModelDeploymentProfileRegistry(profile);
        var catalog = new InMemoryModelDeploymentCredentialCatalog(
            SessionRuntimeTestFixtures.CreateCatalogRecord(
                ownership.OrganizationId,
                providerId: "openrouter.synthetic"));
        var secrets = new StaticSecretSource(CanarySecret);
        var configurations = new InMemoryOpenRouterInstalledConfigurationRegistry(configuration);
        return new Harness(profile, frozen, ownership, profiles, catalog, secrets, configurations);
    }

    private static string ControlBody(
        string content,
        bool includeMetadata = true,
        string provider = Provider,
        int attempt = 1,
        int cachedTokens = 0,
        string model = Model,
        bool includeUsage = true,
        string? rawAssistantContentJson = null,
        string? rawModelJson = null,
        string? rawProviderJson = null)
    {
        var encoded = rawAssistantContentJson ?? JsonSerializer.Serialize(content);
        var encodedModel = rawModelJson ?? JsonSerializer.Serialize(model);
        var encodedProvider = rawProviderJson ?? JsonSerializer.Serialize(provider);
        var metadata = includeMetadata
            ? ",\"openrouter_metadata\":{\"attempt\":" + attempt
              + ",\"endpoints\":{\"available\":[{\"provider\":" + encodedProvider
              + ",\"model\":" + encodedModel
              + ",\"selected\":true}]}}"
            : string.Empty;
        var usage = includeUsage
            ? ",\"usage\":{\"prompt_tokens\":9,\"completion_tokens\":4,\"prompt_tokens_details\":{\"cached_tokens\":"
              + cachedTokens + "}}"
            : string.Empty;
        return "{\"id\":\"gen-test\",\"model\":" + encodedModel
            + ",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":" + encoded
            + "},\"finish_reason\":\"stop\"}]" + usage + metadata + "}";
    }

    private static string ControlBodyWithPadding(string content, int totalUtf8Bytes)
    {
        var prefix = ControlBody(content)[..^1] + ",\"padding\":\"";
        const string suffix = "\"}";
        var padding = totalUtf8Bytes - Encoding.UTF8.GetByteCount(prefix) - Encoding.UTF8.GetByteCount(suffix);
        Assert.True(padding >= 0);
        return prefix + new string('x', padding) + suffix;
    }

    private static byte[] MalformedUtf8Sse(bool includeFragment)
    {
        var builder = new MemoryStream();
        builder.Write(": keep-alive\n\n"u8);
        if (includeFragment)
        {
            builder.Write("data: {\"id\":\"gen-test\",\"model\":"u8);
            builder.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Model)));
            builder.Write(",\"choices\":[{\"index\":0,\"delta\":{\"content\":\"Hi\"},\"finish_reason\":null}]}\n\n"u8);
        }

        builder.Write("data: "u8);
        builder.WriteByte(0xFF);
        builder.Write("\n\n"u8);
        return builder.ToArray();
    }

    private static string DiscoveryBody(
        string returnedModel,
        string selectedProvider,
        string? availableFirstProvider = null,
        int cachedTokens = 0)
    {
        var first = availableFirstProvider ?? selectedProvider;
        return "{\"id\":\"gen-test\",\"model\":" + JsonSerializer.Serialize(returnedModel)
            + ",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"ok\"},\"finish_reason\":\"stop\"}],"
            + "\"usage\":{\"prompt_tokens\":1,\"completion_tokens\":1,\"prompt_tokens_details\":{\"cached_tokens\":"
            + cachedTokens + "}},\"openrouter_metadata\":{\"attempt\":1,\"endpoints\":{\"available\":["
            + "{\"provider\":" + JsonSerializer.Serialize(first) + ",\"model\":" + JsonSerializer.Serialize(returnedModel)
            + ",\"selected\":false},"
            + "{\"provider\":" + JsonSerializer.Serialize(selectedProvider) + ",\"model\":" + JsonSerializer.Serialize(returnedModel)
            + ",\"selected\":true}]}}}";
    }

    internal sealed record Harness(
        InstalledModelDeploymentProfile Profile,
        FrozenModelDeploymentBinding Frozen,
        SessionOwnership Ownership,
        IInstalledModelDeploymentProfileRegistry Profiles,
        IModelDeploymentCredentialCatalog Catalog,
        IProviderCredentialSecretSource Secrets,
        IOpenRouterInstalledConfigurationRegistry Configurations)
    {
        public OpenRouterModelExecutionAdapter Adapter(
            HttpMessageHandler handler,
            TimeSpan? testControlTimeout = null,
            TimeSpan? testContentTimeout = null) =>
            new(Profiles, Catalog, Secrets, Configurations, handler, syntheticDataPolicyAccepted: true)
            {
                TestControlTimeout = testControlTimeout,
                TestContentTimeout = testContentTimeout,
            };

        public string InvocationJson() =>
            """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"no_action","outputs":[],"requested_actions":[],"no_action":{"reason_category":"intentional_silence"}}
            """;

        public ModelExecutionAttemptRequest ControlRequest(string? participantText = null, int maxControlUtf8Bytes = 65_536)
        {
            IReadOnlyList<VisibleTranscriptItemRef> transcript = participantText is null
                ? []
                : [
                    new VisibleTranscriptItemRef(
                        "msg.p.1",
                        TranscriptAuthorTypes.Participant,
                        "turn.1",
                        new ProtectedContentRef("msg:msg.p.1", new string('d', 64)),
                        participantText),
                ];
            var context = new InvocationContext(
                Ownership,
                new string('a', 64),
                new string('b', 64),
                [],
                [],
                [],
                transcript,
                transcript.Count == 0 ? [] : [InvocationContextFactCategories.TranscriptItem]);
            return new ModelExecutionAttemptRequest(
                Ownership,
                "ainv.00000001",
                Frozen.ProviderId,
                Frozen.CredentialBindingReference,
                Frozen.CredentialBindingVersion,
                context,
                1,
                maxControlUtf8Bytes,
                Frozen,
                "prat.test.1",
                Profile.RequestedModel,
                Profile.ProfileDigest);
        }

        public ModelContentStreamRequest StreamRequest() =>
            new(
                Ownership,
                "ainv.00000001",
                "agen.test.1",
                Frozen,
                new InvocationContext(Ownership, new string('a', 64), new string('b', 64), [], [], [], [], []),
                1,
                "prat.test.1",
                Frozen.ProviderId,
                Frozen.CredentialBindingReference,
                Frozen.CredentialBindingVersion);
    }

    private sealed class StaticSecretSource(string value) : IProviderCredentialSecretSource
    {
        public Task<ProviderSecret?> TryReadAsync(string secretName, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProviderSecret?>(new ProviderSecret(value));
    }

    private sealed class RecordingHandler(string body, string? cacheStatus = null) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string Body { get; private set; } = string.Empty;
        public string? Authorization { get; private set; }
        public string? MetadataHeader { get; private set; }
        public string? CacheHeader { get; private set; }
        public string[] HeaderNames { get; private set; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Method = request.Method;
            Authorization = request.Headers.Authorization?.ToString();
            MetadataHeader = request.Headers.TryGetValues("X-OpenRouter-Metadata", out var metadata) ? metadata.Single() : null;
            CacheHeader = request.Headers.TryGetValues("X-OpenRouter-Cache", out var cache) ? cache.Single() : null;
            HeaderNames = request.Headers.Select(header => header.Key).ToArray();
            if (request.Content is not null)
            {
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            if (!string.IsNullOrWhiteSpace(cacheStatus))
            {
                response.Headers.TryAddWithoutValidation(
                    OpenRouterAdapterContracts.ResponseCacheStatusHeader,
                    cacheStatus);
            }

            return response;
        }
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class StatusHandler(int status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent("""{"error":{"message":"limited","token":"sk-or-canary-secret-do-not-leak"}}""", Encoding.UTF8, "application/json"),
            });
    }

    private sealed class RedirectHandler(string location) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri(location);
            return Task.FromResult(response);
        }
    }

    private sealed class RawSseHandler(byte[] utf8) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new ByteArrayContent(utf8);
            content.Headers.TryAddWithoutValidation("Content-Type", "text/event-stream");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }

    private sealed class StallAfterHeadersHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new StallStream()),
            });
    }

    private sealed class StallStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class StreamingHandler(
        string[] deltas,
        bool truncateAfterFirst = false,
        bool omitTrailingBlankLine = false,
        bool omitDone = false,
        bool duplicateTerminal = false,
        bool extraAfterTerminal = false,
        bool oversizedEvent = false,
        bool escapedInvalidSurrogate = false) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            builder.Append(": keep-alive\n\n");
            if (oversizedEvent)
            {
                builder.Append("data: {\"padding\":\"");
                builder.Append('x', OpenRouterAdapterContracts.MaxSseEventUtf8Bytes);
                builder.Append("\"}\n\n");
            }

            for (var i = 0; i < deltas.Length; i++)
            {
                builder.Append("data: {\"id\":\"gen-test\",\"model\":")
                    .Append(JsonSerializer.Serialize(Model))
                    .Append(",\"choices\":[{\"index\":0,\"delta\":{\"content\":")
                    .Append(JsonSerializer.Serialize(deltas[i]))
                    .Append("},\"finish_reason\":null}]}\n\n");
                if (truncateAfterFirst)
                {
                    break;
                }
            }

            if (escapedInvalidSurrogate)
            {
                builder.Append("data: {\"id\":\"gen-test\",\"model\":")
                    .Append(JsonSerializer.Serialize(Model))
                    .Append(",\"choices\":[{\"index\":0,\"delta\":{\"content\":\"\\uD800\"},\"finish_reason\":null}]}\n\n");
            }

            if (!truncateAfterFirst)
            {
                builder.Append(TerminalEvent());
                if (duplicateTerminal)
                {
                    builder.Append(TerminalEvent());
                }

                if (extraAfterTerminal)
                {
                    builder.Append("data: {\"id\":\"gen-test\",\"model\":")
                        .Append(JsonSerializer.Serialize(Model))
                        .Append(",\"choices\":[{\"index\":0,\"delta\":{\"content\":\"late\"},\"finish_reason\":null}]}\n\n");
                }

                if (!omitDone)
                {
                    builder.Append("data: [DONE]");
                    if (!omitTrailingBlankLine)
                    {
                        builder.Append("\n\n");
                    }
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(builder.ToString(), Encoding.UTF8, "text/event-stream"),
            });
        }

        private static string TerminalEvent() =>
            "data: {\"id\":\"gen-test\",\"model\":"
            + JsonSerializer.Serialize(Model)
            + ",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":1,\"completion_tokens\":2,\"prompt_tokens_details\":{\"cached_tokens\":0}},\"openrouter_metadata\":{\"attempt\":1,\"endpoints\":{\"available\":[{\"provider\":\"Together\",\"model\":"
            + JsonSerializer.Serialize(Model)
            + ",\"selected\":true}]}}}\n\n";
    }
}
