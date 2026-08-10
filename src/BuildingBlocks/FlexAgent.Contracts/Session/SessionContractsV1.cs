namespace FlexAgent.Contracts.Session;

public interface ISessionCommandEnvelopeV1
{
    string SchemaVersion { get; }

    string CommandType { get; }

    string CommandId { get; }

    string IdempotencyKey { get; }

    SessionLocatorV1 SessionLocator { get; }

    int ExpectedSessionVersion { get; }

    string? ClientLastSeenSequence { get; }
}

public sealed record SessionLocatorV1(string SessionId);

public sealed record EmptyCommandPayloadV1;

public sealed record MessageSendPayloadV1(string MessageText);

public sealed record TerminateCommandPayloadV1(string ReasonCode);

public sealed record SessionMessageSendCommandV1(
    string SchemaVersion,
    string CommandType,
    string CommandId,
    string IdempotencyKey,
    SessionLocatorV1 SessionLocator,
    int ExpectedSessionVersion,
    string? ClientLastSeenSequence,
    MessageSendPayloadV1 Payload) : ISessionCommandEnvelopeV1;

public sealed record SessionPauseCommandV1(
    string SchemaVersion,
    string CommandType,
    string CommandId,
    string IdempotencyKey,
    SessionLocatorV1 SessionLocator,
    int ExpectedSessionVersion,
    string? ClientLastSeenSequence,
    EmptyCommandPayloadV1 Payload) : ISessionCommandEnvelopeV1;

public sealed record SessionResumeCommandV1(
    string SchemaVersion,
    string CommandType,
    string CommandId,
    string IdempotencyKey,
    SessionLocatorV1 SessionLocator,
    int ExpectedSessionVersion,
    string? ClientLastSeenSequence,
    EmptyCommandPayloadV1 Payload) : ISessionCommandEnvelopeV1;

public sealed record SessionCompleteCommandV1(
    string SchemaVersion,
    string CommandType,
    string CommandId,
    string IdempotencyKey,
    SessionLocatorV1 SessionLocator,
    int ExpectedSessionVersion,
    string? ClientLastSeenSequence,
    EmptyCommandPayloadV1 Payload) : ISessionCommandEnvelopeV1;

public sealed record SessionTerminateCommandV1(
    string SchemaVersion,
    string CommandType,
    string CommandId,
    string IdempotencyKey,
    SessionLocatorV1 SessionLocator,
    int ExpectedSessionVersion,
    string? ClientLastSeenSequence,
    TerminateCommandPayloadV1 Payload) : ISessionCommandEnvelopeV1;

public sealed record SessionReconcileCommandV1(
    string SchemaVersion,
    string CommandType,
    string CommandId,
    string IdempotencyKey,
    SessionLocatorV1 SessionLocator,
    int ExpectedSessionVersion,
    string ClientLastSeenSequence,
    EmptyCommandPayloadV1 Payload) : ISessionCommandEnvelopeV1;

public sealed record SessionStateEventEnvelopeV1(
    string SchemaVersion,
    string EventType,
    string SessionId,
    string SessionSequence,
    int SessionVersion,
    string OccurredAt,
    string? CorrelationId,
    SessionStateEventPayloadV1 Payload);

public sealed record SessionStateEventPayloadV1(
    string Summary,
    string? TurnId);
