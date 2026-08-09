namespace FlexAgent.Contracts.Session;

public sealed record SessionCommandEnvelopeV1(
    string SchemaVersion,
    string CommandType,
    string CommandId,
    string IdempotencyKey,
    SessionLocatorV1 SessionLocator,
    int ExpectedSessionVersion,
    int? ClientLastSeenSequence,
    object Payload);

public sealed record SessionLocatorV1(string SessionId);

public sealed record MessageSendPayloadV1(string MessageText);

public sealed record SessionStateEventEnvelopeV1(
    string SchemaVersion,
    string EventType,
    string SessionId,
    long SessionSequence,
    int SessionVersion,
    string OccurredAt,
    string? CorrelationId,
    SessionStateEventPayloadV1 Payload);

public sealed record SessionStateEventPayloadV1(
    string Summary,
    string? TurnId);
