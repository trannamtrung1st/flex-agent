using System.Text;
using System.Text.Json;
using FlexAgent.Contracts.Session;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class AgentDecisionEnvelopeMapperTests
{
    private const string EnvelopeSchemaId =
        "https://flex-agent.local/contracts/schemas/v2/session/agent-decision.v2.schema.json";

    private const string HistoricalDecisionSchemaId =
        "https://flex-agent.local/contracts/schemas/v1/session/agent-decision.v1.schema.json";

    private static readonly string ContractsRoot = Path.Combine(AppContext.BaseDirectory, "contracts");

    private static readonly IReadOnlySet<string> AllowedKeywords =
        SchemaKeywordProfile.LoadAllowedKeywords(Path.Combine(ContractsRoot, "compatibility", "draft202012-keywords.profile.json"));

    private readonly Draft202012SchemaHarness _harness = new(AllowedKeywords);
    private readonly ContractCatalog _catalog = ContractCatalogLoader.Load(ContractsRoot);

    [Fact]
    public void Historical_emit_message_dual_reads_as_respond_plus_one_message()
    {
        var historical = new EmitMessageAgentDecisionV1(
            "v1",
            "adec.synthetic.0001",
            "ainv.synthetic.0001",
            "2026-08-11T00:00:05Z",
            new EmitMessageDecisionPayloadV1(
                "participant_turn_reply",
                "turn.synthetic.0001",
                "slot.synthetic.0001"),
            new NextTimerRequestV1("PT30S", "1"));

        var envelope = AgentDecisionEnvelopeMapper.FromV1(historical);

        Assert.Equal("v2", envelope.SchemaVersion);
        Assert.Equal(DecisionDispositionV2.Respond, envelope.Disposition);
        Assert.Null(envelope.NoAction);
        var message = Assert.Single(envelope.Outputs);
        Assert.Equal(AgentOutputKindV2.Message, message.Kind);
        Assert.Equal(AgentDecisionEnvelopeMapper.HistoricalMessageLocalRef, message.LocalRef);
        Assert.Equal("participant_turn_reply", message.CommunicationPurpose);
        var timer = Assert.Single(envelope.RequestedActions);
        Assert.Equal(AgentRequestedActionKindV2.NextTimerRequest, timer.Kind);
        AssertHistoricalAndEnvelopeBothValidate(historical, envelope);
    }

    [Fact]
    public void Historical_no_action_dual_reads_as_explicit_no_action_plus_zero_outputs()
    {
        var historical = new NoActionAgentDecisionV1(
            "v1",
            "adec.synthetic.0002",
            "ainv.synthetic.0002",
            "2026-08-11T00:00:06Z",
            new NoActionDecisionPayloadV1(NoActionReasonCategoryV1.IntentionalSilence));

        var envelope = AgentDecisionEnvelopeMapper.FromV1(historical);

        Assert.Equal(DecisionDispositionV2.NoAction, envelope.Disposition);
        Assert.Empty(envelope.Outputs);
        Assert.Empty(envelope.RequestedActions);
        Assert.Equal(NoActionReasonCategoryV1.IntentionalSilence, envelope.NoAction!.ReasonCategory);
        AssertHistoricalAndEnvelopeBothValidate(historical, envelope);
    }

    [Fact]
    public void Typed_voice_output_is_schema_valid_on_the_successor_envelope()
    {
        var envelope = new AgentDecisionEnvelopeV2(
            "v2",
            "adec.synthetic.0003",
            "ainv.synthetic.0003",
            "2026-08-14T00:00:07Z",
            DecisionDispositionV2.Respond,
            [
                new AgentOutputRecommendationV2(
                    AgentOutputKindV2.Message,
                    "out.message.primary",
                    "participant_turn_reply"),
                new AgentOutputRecommendationV2(
                    AgentOutputKindV2.Voice,
                    "out.voice.primary"),
            ],
            []);

        AssertEnvelopeValid(envelope);
    }

    [Fact]
    public void Respond_with_zero_outputs_remains_schema_valid()
    {
        var envelope = new AgentDecisionEnvelopeV2(
            "v2",
            "adec.synthetic.0004",
            "ainv.synthetic.0004",
            "2026-08-14T00:00:08Z",
            DecisionDispositionV2.Respond,
            [],
            []);

        AssertEnvelopeValid(envelope);
    }

    private void AssertHistoricalAndEnvelopeBothValidate(IAgentDecisionV1 historical, AgentDecisionEnvelopeV2 envelope)
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var historicalJson = SessionRuntimeContractJson.SerializeToUtf8Bytes(historical, historical.GetType());
        var envelopeJson = SessionRuntimeContractJson.SerializeToUtf8Bytes(envelope);
        var historicalResult = _harness.ValidateInstance(schemas[HistoricalDecisionSchemaId], historicalJson);
        var envelopeResult = _harness.ValidateInstance(schemas[EnvelopeSchemaId], envelopeJson);
        Assert.True(historicalResult.IsValid, JsonSerializer.Serialize(historicalResult));
        Assert.True(envelopeResult.IsValid, Encoding.UTF8.GetString(envelopeJson) + JsonSerializer.Serialize(envelopeResult));
    }

    private void AssertEnvelopeValid(AgentDecisionEnvelopeV2 envelope)
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var json = SessionRuntimeContractJson.SerializeToUtf8Bytes(envelope);
        var result = _harness.ValidateInstance(schemas[EnvelopeSchemaId], json);
        Assert.True(result.IsValid, Encoding.UTF8.GetString(json) + JsonSerializer.Serialize(result));
    }
}
