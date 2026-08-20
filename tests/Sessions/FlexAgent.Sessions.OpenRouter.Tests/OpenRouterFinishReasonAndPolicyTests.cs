using System.Text.Json;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterFinishReasonAndPolicyTests
{
    [Fact]
    public void Terminal_facts_require_an_explicit_finish_reason()
    {
        using var missing = JsonDocument.Parse(
            """
            {"id":"gen-test","model":"openai/gpt-oss-20b:free","choices":[{"index":0,"message":{"role":"assistant","content":"{}"}}],"usage":{"prompt_tokens":1,"completion_tokens":2},"openrouter_metadata":{"attempt":1,"endpoints":{"available":[{"provider":"Darkbloom","model":"openai/gpt-oss-20b:free","selected":true}]}}}
            """);
        Assert.False(
            OpenRouterResponseParser.TryReadTerminalFacts(
                missing.RootElement,
                "openai/gpt-oss-20b:free",
                "Darkbloom",
                out var facts,
                out _));
        Assert.Null(facts);
    }

    [Fact]
    public void Terminal_facts_record_a_length_finish_reason()
    {
        using var truncated = JsonDocument.Parse(
            """
            {"id":"gen-test","model":"openai/gpt-oss-20b:free","choices":[{"index":0,"message":{"role":"assistant","content":"{}"},"finish_reason":"length"}],"usage":{"prompt_tokens":1,"completion_tokens":100},"openrouter_metadata":{"attempt":1,"endpoints":{"available":[{"provider":"Darkbloom","model":"openai/gpt-oss-20b:free","selected":true}]}}}
            """);
        Assert.True(
            OpenRouterResponseParser.TryReadTerminalFacts(
                truncated.RootElement,
                "openai/gpt-oss-20b:free",
                "Darkbloom",
                out var facts,
                out _));
        Assert.Equal("length", facts!.FinishReason);
        Assert.Equal(100, facts.OutputTokens);
    }

    [Fact]
    public async Task Control_length_finish_reason_is_malformed_control()
    {
        var harness = OpenRouterAdapterContractTests.CreateHarness();
        var handler = new RecordingHandler(
            """{"id":"gen-test","model":"meta-llama/llama-3.1-8b-instruct:free","choices":[{"index":0,"message":{"role":"assistant","content":"{\"schema_version\":\"v2\",\"agent_decision_id\":\"adec.00000001\",\"agent_invocation_id\":\"ainv.00000001\",\"produced_at\":\"2026-08-14T00:00:00Z\",\"disposition\":\"no_action\",\"outputs\":[],\"requested_actions\":[],\"no_action\":{\"reason_category\":\"intentional_silence\"}}"},"finish_reason":"length"}],"usage":{"prompt_tokens":9,"completion_tokens":4},"openrouter_metadata":{"attempt":1,"endpoints":{"available":[{"provider":"Together","model":"meta-llama/llama-3.1-8b-instruct:free","selected":true}]}}}""");
        var result = await harness.Adapter(handler).ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelExecutionFailed>(result).ReasonCategory);
        Assert.Equal("length", result.Provenance?.TerminalFinishReason);
    }

    [Fact]
    public void Length_finish_reason_does_not_qualify_even_when_reported_tokens_are_below_the_ceiling()
    {
        var control = new ModelExecutionStructuredControl(Admission());
        var completed = new ModelContentCompleted { Provenance = Provenance(100) };
        Assert.False(
            OpenRouterLiveMatrixQualification.TryQualify(
                control,
                [new ModelContentTextDelta("Hi"), completed],
                OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens,
                "length",
                out var denial));
        Assert.Equal("length_truncated", denial);
    }

    [Fact]
    public void Default_policy_stays_at_256_tokens_and_phase21_keeps_4096()
    {
        Assert.Equal(256, OpenRouterAdapterContracts.MaxOutputTokens);
        Assert.Equal(4096, OpenRouterAdapterContracts.Phase21MaxOutputTokens);
        Assert.Equal(256, OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens);
        Assert.Equal("openrouter.request-policy.v2", OpenRouterAdapterContracts.RequestPolicyVersion);
        Assert.Equal("stop", OpenRouterAdapterContracts.ApprovedNonTruncationFinishReason);

        var example = OpenRouterInstalledConfiguration.Create(
            "openrouter.synthetic.example.do-not-enable",
            "1",
            "acme/example-instruct:free",
            "acme/example-instruct:free",
            "Together",
            "Together",
            ModelDeploymentCredentialModes.OrganizationByok,
            "openrouter.synthetic");
        Assert.Equal(256, example.Profile.MaxOutputTokens);
        Assert.Equal(TimeSpan.FromSeconds(30), example.Profile.ControlTimeout);
        Assert.Null(example.RequestPolicy.ReasoningEffort);
    }

    [Fact]
    public void Adapter_digest_binds_the_named_request_policy_version()
    {
        var withVersion = OpenRouterInstalledConfiguration.ComputeAdapterConfigurationDigest("Together", "Together");
        Assert.NotEqual("a240fe2db7acafcb39752cddf0e75066049124705dade1819efd295df6fbfa5a", withVersion);
    }

    [Fact]
    public async Task Control_system_prompt_binds_the_current_invocation_id()
    {
        var harness = OpenRouterAdapterContractTests.CreateHarness();
        var handler = new RecordingHandler();
        await harness.Adapter(handler).ExecuteAsync(harness.ControlRequest(), CancellationToken.None);
        using var body = JsonDocument.Parse(handler.Body);
        var system = body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString();
        Assert.Contains("ainv.00000001", system, StringComparison.Ordinal);
        Assert.DoesNotContain("ainv.synthetic.0002", system, StringComparison.Ordinal);
        Assert.Contains("exact current invocation ID", system, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitized_evidence_record_is_machine_readable_and_has_no_raw_bodies()
    {
        var record = new OpenRouterSanitizedQualificationRecord(
            SchemaVersion: OpenRouterSanitizedQualificationRecord.CurrentSchemaVersion,
            RequestPolicyVersion: OpenRouterAdapterContracts.RequestPolicyVersion,
            AdapterContractVersion: OpenRouterAdapterContracts.AdapterContractVersion,
            QualificationScope: OpenRouterAdapterContracts.QualificationScope,
            Model: OpenRouterLiveQualification.GptOssDarkbloomModel,
            ProviderIdentity: OpenRouterLiveQualification.GptOssDarkbloomProviderIdentity,
            ProfileDigest: "profile",
            AdapterConfigurationDigest: "adapter",
            ControlHttp: 200,
            ControlClass: "ok",
            ControlCache: "absent",
            ControlFinishReason: "stop",
            ControlTokensIn: 10,
            ControlTokensOut: 20,
            ContentHttp: 200,
            ContentClass: "ok",
            ContentCache: "absent",
            ContentFinishReason: "stop",
            ContentTokensIn: 4,
            ContentTokensOut: 1,
            QualificationOutcome: "qualified_for=synthetic_development",
            DenialReason: null);
        var json = record.ToSanitizedJson();
        Assert.Contains("\"control_finish_reason\"", json, StringComparison.Ordinal);
        Assert.Contains("\"content_finish_reason\"", json, StringComparison.Ordinal);
        Assert.Contains("openrouter.request-policy.v2", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer ", json, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-or-", json, StringComparison.Ordinal);
        Assert.DoesNotContain("choices", json, StringComparison.Ordinal);
    }

    private static ValidatedAgentDecisionEnvelope Admission()
    {
        var utf8 =
            """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"no_action","outputs":[],"requested_actions":[],"no_action":{"reason_category":"intentional_silence"}}
            """u8.ToArray();
        Assert.True(ValidatedAgentDecisionEnvelope.TryAdmit(utf8, out var admitted, out _) && admitted is not null);
        return admitted!;
    }

    private static ModelProviderAttemptProvenance Provenance(int outputTokens) =>
        new(
            ModelDeploymentAdapterKinds.OpenRouter,
            OpenRouterAdapterContracts.AdapterContractVersion,
            OpenRouterLiveQualification.GptOssDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GptOssDarkbloomProfileDigest,
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            ExecutionAttemptOutcomeCategories.ContentProduced,
            10,
            outputTokens,
            "pref.prat.phase21.content",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            ModelProviderRequestPhases.Content,
            "prat.phase21.content");

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _response;

        public RecordingHandler(string? response = null)
        {
            _response = response
                ?? """{"id":"gen-test","model":"meta-llama/llama-3.1-8b-instruct:free","choices":[{"index":0,"message":{"role":"assistant","content":"{\"schema_version\":\"v2\",\"agent_decision_id\":\"adec.00000001\",\"agent_invocation_id\":\"ainv.00000001\",\"produced_at\":\"2026-08-14T00:00:00Z\",\"disposition\":\"no_action\",\"outputs\":[],\"requested_actions\":[],\"no_action\":{\"reason_category\":\"intentional_silence\"}}"},"finish_reason":"stop"}],"usage":{"prompt_tokens":9,"completion_tokens":4},"openrouter_metadata":{"attempt":1,"endpoints":{"available":[{"provider":"Together","model":"meta-llama/llama-3.1-8b-instruct:free","selected":true}]}}}""";
        }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    _response,
                    System.Text.Encoding.UTF8,
                    "application/json"),
            };
        }
    }
}
