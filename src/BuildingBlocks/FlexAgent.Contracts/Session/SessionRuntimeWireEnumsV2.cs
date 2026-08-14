using System.Text.Json.Serialization;

namespace FlexAgent.Contracts.Session;

[JsonConverter(typeof(SessionRuntimeWireEnumConverter<DecisionDispositionV2>))]
public enum DecisionDispositionV2
{
    [JsonStringEnumMemberName("respond")]
    Respond,

    [JsonStringEnumMemberName("no_action")]
    NoAction,
}

[JsonConverter(typeof(SessionRuntimeWireEnumConverter<AgentOutputKindV2>))]
public enum AgentOutputKindV2
{
    [JsonStringEnumMemberName("message")]
    Message,

    [JsonStringEnumMemberName("voice")]
    Voice,
}

[JsonConverter(typeof(SessionRuntimeWireEnumConverter<AgentOutputAudienceV2>))]
public enum AgentOutputAudienceV2
{
    [JsonStringEnumMemberName("participant")]
    Participant,

    [JsonStringEnumMemberName("reviewer")]
    Reviewer,

    [JsonStringEnumMemberName("administrator")]
    Administrator,

    [JsonStringEnumMemberName("runtime_only")]
    RuntimeOnly,
}

[JsonConverter(typeof(SessionRuntimeWireEnumConverter<AgentRequestedActionKindV2>))]
public enum AgentRequestedActionKindV2
{
    [JsonStringEnumMemberName("next_timer_request")]
    NextTimerRequest,

    [JsonStringEnumMemberName("request_tool")]
    RequestTool,

    [JsonStringEnumMemberName("propose_transition")]
    ProposeTransition,

    [JsonStringEnumMemberName("escalate")]
    Escalate,
}
