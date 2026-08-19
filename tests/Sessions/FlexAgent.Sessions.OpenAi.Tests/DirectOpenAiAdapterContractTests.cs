using System.Net;
using System.Text;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.OpenAi;

namespace FlexAgent.Sessions.OpenAi.Tests;

public sealed class DirectOpenAiAdapterContractTests
{
    [Fact]
    public async Task Fake_transport_returns_structured_control_and_provenance_without_network()
    {
        var harness = CreateHarness();
        var json = """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"no_action","outputs":[],"requested_actions":[],"no_action":{"reason_category":"intentional_silence"}}
            """;
        var adapter = harness.Adapter(new ScriptedOpenAiHandler(json, stream: false));

        var result = await adapter.ExecuteAsync(harness.ControlRequest(), CancellationToken.None);

        var control = Assert.IsType<ModelExecutionStructuredControl>(result);
        Assert.Equal(DecisionDispositions.NoAction, control.Envelope.Disposition);
        Assert.NotNull(result.Provenance);
        Assert.Equal(ModelDeploymentAdapterKinds.DirectOpenAi, result.Provenance!.AdapterKind);
        Assert.Equal("synthetic.model.pinned", result.Provenance.RequestedModel);
        Assert.Equal("synthetic.model.pinned.2026-01-01", result.Provenance.ResolvedModelVersion);
        Assert.Equal(ModelProviderRequestPhases.Control, result.Provenance.Phase);
        Assert.DoesNotContain("sk-", result.Provenance.ProviderRequestRef ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Fake_transport_sends_participant_transcript_and_frozen_resolved_model()
    {
        var harness = CreateHarness(requestedModel: "gpt-alias", resolvedModel: "gpt-pinned-2026-08-01");
        var json = """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"no_action","outputs":[],"requested_actions":[],"no_action":{"reason_category":"intentional_silence"}}
            """;
        var handler = new ScriptedOpenAiHandler(json, stream: false, returnedModel: "gpt-pinned-2026-08-01");
        var adapter = harness.Adapter(handler);
        var participantText = "What is the next assessment question?";

        var result = await adapter.ExecuteAsync(harness.ControlRequest(participantText: participantText), CancellationToken.None);

        Assert.IsType<ModelExecutionStructuredControl>(result);
        Assert.Contains(participantText, handler.CapturedRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"model\":\"gpt-pinned-2026-08-01\"", handler.CapturedRequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("\"model\":\"gpt-alias\"", handler.CapturedRequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain(harness.Ownership.OrganizationId.ToString(), handler.CapturedRequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-test", handler.CapturedRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Provider_returned_model_must_match_frozen_resolved_version()
    {
        var harness = CreateHarness(requestedModel: "gpt-alias", resolvedModel: "gpt-pinned-2026-08-01");
        var json = """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"no_action","outputs":[],"requested_actions":[],"no_action":{"reason_category":"intentional_silence"}}
            """;
        var drifted = await harness.Adapter(new ScriptedOpenAiHandler(json, stream: false, returnedModel: "gpt-alias"))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelExecutionFailed>(drifted).ReasonCategory);
    }

    [Fact]
    public async Task Mismatched_adapter_contract_version_fails_closed()
    {
        var harness = CreateHarness(contractVersion: "sessions.openai.v2");
        var json = """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"no_action","outputs":[],"requested_actions":[],"no_action":{"reason_category":"intentional_silence"}}
            """;
        var result = await harness.Adapter(new ScriptedOpenAiHandler(json, stream: false))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.CredentialBindingFailed,
            Assert.IsType<ModelExecutionFailed>(result).ReasonCategory);
    }

    [Fact]
    public async Task Fake_transport_streams_non_overlapping_text_deltas()
    {
        var harness = CreateHarness();
        var adapter = harness.Adapter(new ScriptedOpenAiHandler("Hel", stream: true, secondDelta: "lo"));

        var events = new List<ModelContentEvent>();
        await foreach (var item in adapter.StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            events.Add(item);
        }

        Assert.Equal("Hel", Assert.IsType<ModelContentTextDelta>(events[0]).ExactUtf8Text);
        Assert.Equal("lo", Assert.IsType<ModelContentTextDelta>(events[1]).ExactUtf8Text);
        var completed = Assert.IsType<ModelContentCompleted>(events[^1]);
        Assert.NotNull(completed.Provenance);
        Assert.Equal(ModelProviderRequestPhases.Content, completed.Provenance.Phase);
    }

    [Fact]
    public async Task Rate_limit_and_malformed_control_are_normalized_failures()
    {
        var harness = CreateHarness();
        var limited = await harness.Adapter(new StatusOpenAiHandler(429))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelExecutionFailed>(limited).ReasonCategory);

        var malformed = await harness.Adapter(new ScriptedOpenAiHandler("{ not json", stream: false))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelExecutionFailed>(malformed).ReasonCategory);
    }

    [Fact]
    public async Task Unapproved_origin_and_loopback_redirect_fail_closed()
    {
        var harness = CreateHarness();
        var loopback = await harness.Adapter(new RedirectOpenAiHandler("https://127.0.0.1/v1/chat/completions"))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelExecutionFailed>(loopback).ReasonCategory);

        Assert.False(ApprovedOrigin.IsAllowed(new Uri("http://api.openai.com/"), harness.Profile.ApprovedHttpsOrigin));
        Assert.False(ApprovedOrigin.IsAllowed(new Uri("https://example.com/"), harness.Profile.ApprovedHttpsOrigin));
        Assert.False(ApprovedOrigin.IsAllowed(new Uri("https://127.0.0.1/"), harness.Profile.ApprovedHttpsOrigin));
        Assert.False(ApprovedOrigin.IsAllowed(new Uri("https://169.254.169.254/"), harness.Profile.ApprovedHttpsOrigin));
        Assert.False(ApprovedOrigin.IsAllowed(new Uri("https://10.0.0.1/"), harness.Profile.ApprovedHttpsOrigin));
        Assert.True(ApprovedOrigin.IsAllowed(new Uri("https://api.openai.com/v1/chat/completions"), harness.Profile.ApprovedHttpsOrigin));
    }

    [Fact]
    public async Task Timeout_outage_and_oversized_control_are_normalized_failures()
    {
        var harness = CreateHarness();
        var timedOut = await harness.Adapter(new StatusOpenAiHandler(408))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.ProviderTimeout,
            Assert.IsType<ModelExecutionFailed>(timedOut).ReasonCategory);

        var outage = await harness.Adapter(new ThrowingOpenAiHandler())
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.ProviderUnavailable,
            Assert.IsType<ModelExecutionFailed>(outage).ReasonCategory);

        var oversized = await harness.Adapter(new ScriptedOpenAiHandler(new string('x', 200), stream: false))
            .ExecuteAsync(harness.ControlRequest(maxControlUtf8Bytes: 16), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelExecutionFailed>(oversized).ReasonCategory);
    }

    [Fact]
    public async Task Stream_provider_errors_do_not_fabricate_content()
    {
        var harness = CreateHarness();
        var events = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new StatusOpenAiHandler(500))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            events.Add(item);
        }

        Assert.Single(events);
        var failed = Assert.IsType<ModelContentFailed>(events[0]);
        Assert.Equal(ExecutionFailureReasons.ProviderUnavailable, failed.ReasonCategory);
        Assert.NotNull(failed.Provenance);
        Assert.Equal(ModelProviderRequestPhases.Content, failed.Provenance.Phase);
    }

    [Fact]
    public async Task Cancellation_does_not_fabricate_a_decision()
    {
        var harness = CreateHarness();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var result = await harness.Adapter(new ScriptedOpenAiHandler("{}", stream: false))
            .ExecuteAsync(harness.ControlRequest(), cts.Token);
        Assert.Equal(
            ExecutionAttemptOutcomeCategories.Cancelled,
            Assert.IsType<ModelExecutionFailed>(result).ReasonCategory);
    }

    [Fact]
    public void Provider_secret_does_not_appear_in_object_display()
    {
        using var secret = new ProviderSecret("sk-live-secret-value");
        Assert.Equal("[redacted]", secret.ToString());
        Assert.DoesNotContain("sk-live", secret.ToString(), StringComparison.Ordinal);
    }

    private static Harness CreateHarness(
        string requestedModel = "synthetic.model.pinned",
        string resolvedModel = "synthetic.model.pinned.2026-01-01",
        string contractVersion = DirectOpenAiModelExecutionAdapter.AdapterContractVersion)
    {
        var profile = InstalledModelDeploymentProfile.Create(
            "direct-openai.unqualified.example",
            "1",
            ModelDeploymentAdapterKinds.DirectOpenAi,
            contractVersion,
            new Uri("https://api.openai.com/"),
            requestedModel,
            resolvedModel,
            "p0.text.structured-control",
            ModelDeploymentCredentialModes.OrganizationByok,
            256,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5),
            1,
            "openai.direct");
        var frozen = new FrozenModelDeploymentBinding(
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.ProviderId,
            ModelDeploymentCredentialModes.OrganizationByok,
            "bind.opaque.0001",
            "bind.v1");
        var ownership = new SessionOwnership(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
        var profiles = new InMemoryInstalledModelDeploymentProfileRegistry(profile);
        var catalog = new InMemoryModelDeploymentCredentialCatalog(
            new ModelDeploymentCredentialCatalogRecord(
                "bind.opaque.0001",
                "bind.v1",
                ownership.OrganizationId,
                "openai.direct",
                ModelDeploymentCredentialModes.OrganizationByok,
                false,
                "org-a-openai"));
        var secrets = new StaticSecretSource("sk-test-not-for-production");
        return new Harness(profile, frozen, ownership, profiles, catalog, secrets);
    }

    private sealed record Harness(
        InstalledModelDeploymentProfile Profile,
        FrozenModelDeploymentBinding Frozen,
        SessionOwnership Ownership,
        IInstalledModelDeploymentProfileRegistry Profiles,
        IModelDeploymentCredentialCatalog Catalog,
        IProviderCredentialSecretSource Secrets)
    {
        public DirectOpenAiModelExecutionAdapter Adapter(HttpMessageHandler handler) =>
            new(Profiles, Catalog, Secrets, handler);

        public ModelExecutionAttemptRequest ControlRequest(
            int maxControlUtf8Bytes = 65_536,
            string? participantText = null)
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
                new InvocationContext(
                    Ownership,
                    new string('a', 64),
                    new string('b', 64),
                    [],
                    [],
                    [],
                    [],
                    []),
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

    private sealed class ScriptedOpenAiHandler(
        string content,
        bool stream,
        string? secondDelta = null,
        string? returnedModel = "synthetic.model.pinned.2026-01-01") : HttpMessageHandler
    {
        public string CapturedRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.StartsWith("https://api.openai.com", request.RequestUri?.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal);
            if (request.Content is not null)
            {
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            if (stream)
            {
                var first = Chunk(content);
                var second = secondDelta is null ? string.Empty : Chunk(secondDelta);
                var body = first + second + "data: [DONE]\n\n";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "text/event-stream"),
                };
            }

            var encoded = System.Text.Json.JsonSerializer.Serialize(content);
            var json =
                "{\"id\":\"chatcmpl-test\",\"object\":\"chat.completion\",\"created\":0,\"model\":"
                + System.Text.Json.JsonSerializer.Serialize(returnedModel)
                + ",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":"
                + encoded
                + "},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":9,\"completion_tokens\":4,\"total_tokens\":13}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }

        private string Chunk(string text) =>
            $"data: {{\"id\":\"chatcmpl-test\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":{System.Text.Json.JsonSerializer.Serialize(returnedModel)},\"choices\":[{{\"index\":0,\"delta\":{{\"content\":{System.Text.Json.JsonSerializer.Serialize(text)}}},\"finish_reason\":null}}]}}\n\n";
    }

    private sealed class StatusOpenAiHandler(int status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent("""{"error":{"message":"limited"}}""", Encoding.UTF8, "application/json"),
            });
    }

    private sealed class ThrowingOpenAiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("synthetic origin outage");
    }

    private sealed class RedirectOpenAiHandler(string location) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri(location);
            return Task.FromResult(response);
        }
    }
}
