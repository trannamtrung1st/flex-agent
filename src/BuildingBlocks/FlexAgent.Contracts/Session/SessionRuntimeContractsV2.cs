using FlexAgent.Contracts.Manifest;

namespace FlexAgent.Contracts.Session;

public sealed record AgentOutputLocalReferenceV2(string Relation, string LocalRef);

public sealed record AgentOutputRecommendationV2(
    AgentOutputKindV2 Kind,
    string LocalRef,
    string? CommunicationPurpose = null,
    string? TurnId = null,
    string? ResponseSlotId = null,
    string? AgentOutputId = null,
    AgentOutputAudienceV2? Audience = null,
    IReadOnlyList<AgentOutputLocalReferenceV2>? References = null,
    ProtectedPayloadRefV1? PayloadRef = null);

public sealed record AgentRequestedActionV2(
    AgentRequestedActionKindV2 Kind,
    string LocalRef,
    string? RelativeDelay = null,
    string? ExpectedScheduleRevision = null);

public sealed record AgentDecisionEnvelopeV2(
    string SchemaVersion,
    string AgentDecisionId,
    string AgentInvocationId,
    string ProducedAt,
    DecisionDispositionV2 Disposition,
    IReadOnlyList<AgentOutputRecommendationV2> Outputs,
    IReadOnlyList<AgentRequestedActionV2> RequestedActions,
    NoActionDecisionPayloadV1? NoAction = null,
    ProtectedPayloadRefV1? PayloadRef = null);
