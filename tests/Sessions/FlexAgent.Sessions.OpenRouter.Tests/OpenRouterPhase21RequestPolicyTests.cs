using System.Net;
using System.Text;
using System.Text.Json;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Sessions.OpenRouter;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterPhase21RequestPolicyTests
{
    private const string CanarySecret = "sk-or-canary-secret-do-not-leak";
    private const string ReasoningCanary = "hidden.reasoning.canary.do-not-expose";

    [Fact]
    public void Phase21_create_pins_4096_tokens_low_excluded_reasoning_and_distinct_digests()
    {
        var created = OpenRouterInstalledConfiguration.Create(
            OpenRouterLiveQualification.GptOssDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomProviderSlug,
            OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity,
            ModelDeploymentCredentialModes.OrganizationByok,
            "openrouter.synthetic",
            requestPolicy: OpenRouterRequestPolicy.Phase21GptOss);

        Assert.Equal(OpenRouterAdapterContracts.Phase21MaxOutputTokens, created.Profile.MaxOutputTokens);
        Assert.Equal(TimeSpan.FromMinutes(2), created.Profile.ControlTimeout);
        Assert.Equal(TimeSpan.FromMinutes(2), created.Profile.ContentTimeout);
        Assert.Equal(256, OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens);
        Assert.Equal("low", created.RequestPolicy.ReasoningEffort);
        Assert.True(created.RequestPolicy.ReasoningExcluded);
        Assert.Equal(OpenRouterLiveQualification.GptOssDarkbloomAdapterDigest, created.AdapterConfigurationDigest);
        Assert.Equal(OpenRouterLiveQualification.GptOssDarkbloomProfileDigest, created.Profile.ProfileDigest);
        Assert.NotEqual(
            OpenRouterInstalledConfiguration.ComputeAdapterConfigurationDigest("Together", "Together"),
            created.AdapterConfigurationDigest);
        Assert.Equal(256, OpenRouterAdapterContracts.MaxOutputTokens);
        Assert.Equal(256, OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens);
    }

    [Fact]
    public void Default_create_keeps_256_tokens_no_reasoning_and_current_digests()
    {
        var example = OpenRouterInstalledConfiguration.Create(
            "openrouter.synthetic.example.do-not-enable",
            "1",
            "acme/example-instruct:free",
            "acme/example-instruct:free",
            "Together",
            "Together",
            ModelDeploymentCredentialModes.OrganizationByok,
            "openrouter.synthetic");

        Assert.Equal(OpenRouterAdapterContracts.MaxOutputTokens, example.Profile.MaxOutputTokens);
        Assert.Null(example.RequestPolicy.ReasoningEffort);
        Assert.False(example.RequestPolicy.ReasoningExcluded);
    }

    [Fact]
    public void Phase21_policy_rejects_identity_drift_and_non_gpt_oss_routes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OpenRouterInstalledConfiguration.Create(
                OpenRouterLiveQualification.GptOssDarkbloomProfileId,
                "1",
                OpenRouterLiveQualification.GptOssDarkbloomModel,
                OpenRouterLiveQualification.GptOssDarkbloomModel,
                "nvidia",
                "Nvidia",
                ModelDeploymentCredentialModes.OrganizationByok,
                "openrouter.synthetic",
                requestPolicy: OpenRouterRequestPolicy.Phase21GptOss));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OpenRouterInstalledConfiguration.Create(
                "openrouter.synthetic.example.do-not-enable",
                "1",
                "acme/example-instruct:free",
                "acme/example-instruct:free",
                "Together",
                "Together",
                ModelDeploymentCredentialModes.OrganizationByok,
                "openrouter.synthetic",
                requestPolicy: OpenRouterRequestPolicy.Phase21GptOss));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OpenRouterRequestPolicy.ForInstalledProfile(
                OpenRouterInstalledConfiguration.Create(
                    "openrouter.synthetic.example.do-not-enable",
                    "1",
                    "acme/example-instruct:free",
                    "acme/example-instruct:free",
                    "Together",
                    "Together",
                    ModelDeploymentCredentialModes.OrganizationByok,
                    "openrouter.synthetic").Profile with
                {
                    MaxOutputTokens = 512,
                }));
    }

    [Fact]
    public async Task Phase21_control_and_content_requests_send_4096_tokens_and_low_excluded_reasoning()
    {
        var harness = CreatePhase21Harness();
        var handler = new RecordingHandler(ControlBody(harness.InvocationJson()));
        var result = await harness.Adapter(handler).ExecuteAsync(harness.ControlRequest(), CancellationToken.None);

        Assert.IsType<ModelExecutionStructuredControl>(result);
        using var control = JsonDocument.Parse(handler.Body);
        Assert.Equal(OpenRouterLiveQualification.GptOssDarkbloomModel, control.RootElement.GetProperty("model").GetString());
        Assert.Equal(4096, control.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.Equal("json_schema", control.RootElement.GetProperty("response_format").GetProperty("type").GetString());
        Assert.True(control.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("strict").GetBoolean());
        var reasoning = control.RootElement.GetProperty("reasoning");
        Assert.Equal("low", reasoning.GetProperty("effort").GetString());
        Assert.True(reasoning.GetProperty("exclude").GetBoolean());
        Assert.False(control.RootElement.GetProperty("provider").GetProperty("allow_fallbacks").GetBoolean());
        Assert.Equal("darkbloom", Assert.Single(control.RootElement.GetProperty("provider").GetProperty("only").EnumerateArray()).GetString());

        var streamHandler = new RecordingHandler(StreamBody());
        await foreach (var _ in harness.Adapter(streamHandler).StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
        }

        using var content = JsonDocument.Parse(streamHandler.Body);
        Assert.Equal(4096, content.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.True(content.RootElement.GetProperty("stream").GetBoolean());
        Assert.Equal("low", content.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
        Assert.True(content.RootElement.GetProperty("reasoning").GetProperty("exclude").GetBoolean());
        Assert.False(content.RootElement.TryGetProperty("response_format", out _));
    }

    [Fact]
    public async Task Default_control_request_omits_reasoning()
    {
        var harness = OpenRouterAdapterContractTests.CreateHarness();
        var handler = new RecordingHandler(ControlBody(harness.InvocationJson(), model: "meta-llama/llama-3.1-8b-instruct:free", provider: "Together"));
        await harness.Adapter(handler).ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        using var body = JsonDocument.Parse(handler.Body);
        Assert.Equal(256, body.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("reasoning", out _));
    }

    [Fact]
    public async Task Hidden_reasoning_in_control_or_stream_fails_closed_without_exposing_the_trace()
    {
        var harness = CreatePhase21Harness();
        var control = await harness.Adapter(new RecordingHandler(ControlBody(
                harness.InvocationJson(),
                reasoning: ReasoningCanary)))
            .ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        var failed = Assert.IsType<ModelExecutionFailed>(control);
        Assert.Equal(ExecutionFailureReasons.ProviderUnavailable, failed.ReasonCategory);
        Assert.DoesNotContain(ReasoningCanary, failed.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(CanarySecret, failed.ToString(), StringComparison.Ordinal);

        var events = new List<ModelContentEvent>();
        await foreach (var item in harness.Adapter(new RecordingHandler(StreamBody(reasoning: ReasoningCanary)))
            .StreamParticipantVisibleContentAsync(harness.StreamRequest(), CancellationToken.None))
        {
            events.Add(item);
        }

        var contentFailed = Assert.Single(events.OfType<ModelContentFailed>());
        Assert.Equal(ExecutionFailureReasons.ProviderUnavailable, contentFailed.ReasonCategory);
        Assert.DoesNotContain(ReasoningCanary, string.Join('\n', events.Select(item => item.ToString())), StringComparison.Ordinal);
        Assert.Empty(events.OfType<ModelContentTextDelta>());
    }

    [Fact]
    public void Phase21_operator_files_round_trip_and_refuse_historical_identity()
    {
        var created = OpenRouterInstalledConfiguration.Create(
            OpenRouterLiveQualification.GptOssDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomProviderSlug,
            OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity,
            ModelDeploymentCredentialModes.OrganizationByok,
            "openrouter.synthetic",
            requestPolicy: OpenRouterRequestPolicy.Phase21GptOss);
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var profilesPath = Path.Combine(directory.FullName, "profile.json");
            var configurationsPath = Path.Combine(directory.FullName, "configuration.json");
            File.WriteAllText(
                profilesPath,
                $$"""
                [
                  {
                    "profileId": "{{created.Profile.ProfileId}}",
                    "profileVersion": "{{created.Profile.ProfileVersion}}",
                    "adapterKind": "{{created.Profile.AdapterKind}}",
                    "adapterContractVersion": "{{created.Profile.AdapterContractVersion}}",
                    "approvedHttpsOrigin": "https://openrouter.ai/",
                    "requestedModel": "{{created.Profile.RequestedModel}}",
                    "resolvedModelVersion": "{{created.Profile.ResolvedModelVersion}}",
                    "capabilityProfileId": "{{created.Profile.CapabilityProfileId}}",
                    "credentialMode": "{{created.Profile.CredentialMode}}",
                    "maxOutputTokens": {{created.Profile.MaxOutputTokens}},
                    "controlTimeoutMilliseconds": {{(int)created.Profile.ControlTimeout.TotalMilliseconds}},
                    "contentTimeoutMilliseconds": {{(int)created.Profile.ContentTimeout.TotalMilliseconds}},
                    "maxProviderRequestAttempts": {{created.Profile.MaxProviderRequestAttempts}},
                    "providerId": "{{created.Profile.ProviderId}}",
                    "adapterConfigurationDigest": "{{created.AdapterConfigurationDigest}}"
                  }
                ]
                """);
            File.WriteAllText(
                configurationsPath,
                $$"""
                [
                  {
                    "profileId": "{{created.Profile.ProfileId}}",
                    "profileVersion": "{{created.Profile.ProfileVersion}}",
                    "profileDigest": "{{created.Profile.ProfileDigest}}",
                    "adapterConfigurationDigest": "{{created.AdapterConfigurationDigest}}",
                    "providerSlug": "{{created.ProviderSlug}}",
                    "expectedReturnedProviderIdentity": "{{created.ExpectedReturnedProviderIdentity}}"
                  }
                ]
                """);

            var loadedProfiles = InstalledModelDeploymentProfileFile.Load(profilesPath);
            var loaded = Assert.Single(OpenRouterInstalledConfigurationFile.Load(configurationsPath, loadedProfiles));
            Assert.True(
                OpenRouterLivePinnedRouteAcceptance.TryAccept(
                    loaded,
                    OpenRouterLivePinnedRouteAcceptance.GptOssDarkbloom,
                    out var denial));
            Assert.Equal(string.Empty, denial);
            Assert.Equal(4096, loaded.Profile.MaxOutputTokens);
            Assert.Equal("low", loaded.RequestPolicy.ReasoningEffort);
            Assert.True(loaded.RequestPolicy.ReasoningExcluded);

            var example = OpenRouterInstalledConfiguration.Create(
                "openrouter.synthetic.example.do-not-enable",
                "1",
                "acme/example-instruct:free",
                "acme/example-instruct:free",
                "Together",
                "Together",
                ModelDeploymentCredentialModes.OrganizationByok,
                "openrouter.synthetic");
            Assert.False(
                OpenRouterLivePinnedRouteAcceptance.TryAccept(
                    example,
                    OpenRouterLivePinnedRouteAcceptance.GptOssDarkbloom,
                    out var exampleDenial));
            Assert.Equal("profile_id_mismatch", exampleDenial);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static OpenRouterAdapterContractTests.Harness CreatePhase21Harness()
    {
        var configuration = OpenRouterInstalledConfiguration.Create(
            OpenRouterLiveQualification.GptOssDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomProviderSlug,
            OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity,
            ModelDeploymentCredentialModes.OrganizationByok,
            "openrouter.synthetic",
            requestPolicy: OpenRouterRequestPolicy.Phase21GptOss);
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
        return new OpenRouterAdapterContractTests.Harness(
            profile,
            frozen,
            ownership,
            new InMemoryInstalledModelDeploymentProfileRegistry(profile),
            new InMemoryModelDeploymentCredentialCatalog(
                SessionRuntimeTestFixtures.CreateCatalogRecord(
                    ownership.OrganizationId,
                    providerId: "openrouter.synthetic")),
            new StaticSecretSource(CanarySecret),
            new InMemoryOpenRouterInstalledConfigurationRegistry(configuration));
    }

    private static string ControlBody(string content, string? reasoning = null, string? model = null, string? provider = null)
    {
        model ??= OpenRouterLiveQualification.GptOssDarkbloomModel;
        provider ??= OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity;
        var reasoningJson = reasoning is null ? string.Empty : ",\"reasoning\":" + JsonSerializer.Serialize(reasoning);
        return "{\"id\":\"gen-test\",\"model\":" + JsonSerializer.Serialize(model)
            + ",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":"
            + JsonSerializer.Serialize(content) + reasoningJson
            + "},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":9,\"completion_tokens\":4},"
            + "\"openrouter_metadata\":{\"attempt\":1,\"endpoints\":{\"available\":[{\"provider\":"
            + JsonSerializer.Serialize(provider) + ",\"model\":" + JsonSerializer.Serialize(model)
            + ",\"selected\":true}]}}}";
    }

    private static string StreamBody(string? reasoning = null)
    {
        var model = JsonSerializer.Serialize(OpenRouterLiveQualification.GptOssDarkbloomModel);
        var provider = JsonSerializer.Serialize(OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity);
        var reasoningJson = reasoning is null ? string.Empty : ",\"reasoning\":" + JsonSerializer.Serialize(reasoning);
        return ": keep-alive\n\n"
            + "data: {\"id\":\"gen-test\",\"model\":" + model
            + ",\"choices\":[{\"index\":0,\"delta\":{\"content\":\"Hi\"" + reasoningJson + "},\"finish_reason\":null}]}\n\n"
            + "data: {\"id\":\"gen-test\",\"model\":" + model
            + ",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":2,\"completion_tokens\":1},"
            + "\"openrouter_metadata\":{\"attempt\":1,\"endpoints\":{\"available\":[{\"provider\":" + provider
            + ",\"model\":" + model + ",\"selected\":true}]}}}\n\n"
            + "data: [DONE]\n\n";
    }

    private sealed class StaticSecretSource(string value) : IProviderCredentialSecretSource
    {
        public Task<ProviderSecret?> TryReadAsync(string secretName, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProviderSecret?>(new ProviderSecret(value));
    }

    private sealed class RecordingHandler(string body) : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var contentType = body.StartsWith(':') || body.StartsWith("data:", StringComparison.Ordinal)
                ? "text/event-stream"
                : "application/json";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, contentType),
            };
        }
    }
}
