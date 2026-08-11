namespace FlexAgent.Contracts.Transport;

public sealed record SafeErrorResponseV1(
    string SchemaVersion,
    string Outcome,
    string CorrelationId,
    string PermittedRecoveryAction,
    int? SessionVersion,
    string? SessionSequence);

public sealed record SseSessionEventV1(
    string SchemaVersion,
    string EventType,
    string SessionId,
    string SessionSequence,
    string OccurredAt,
    SseSessionEventPayloadV1 Payload);

public sealed record SseSessionEventPayloadV1(
    string Summary,
    int? FragmentSequence = null,
    string? AgentMessageId = null,
    string? TextDelta = null,
    string? TurnId = null,
    string? WorkState = null,
    string? ResolutionCategory = null,
    bool? ShowPersistentTurnStatus = null,
    string? AssembledContentDigest = null,
    int? FragmentCount = null);
