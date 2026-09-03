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

public sealed record SessionAgentIdentityV1(string DisplayName);

public sealed record SessionTimingProjectionV1(
    string Policy,
    int? RemainingSeconds,
    string? WarningCode,
    string? PauseStartedAt,
    int? BudgetSeconds = null);

public sealed record SessionBoundSubmissionSummaryV1(
    string Summary,
    int AcceptedVersionCount);

public sealed record SessionSnapshotTranscriptItemV1(
    string ItemId,
    string Author,
    string Status,
    string SequenceStart,
    string SequenceEnd,
    string? Content,
    string? OccurredAt,
    string? TurnId);

public sealed record SessionTranscriptPageV1(
    IReadOnlyList<SessionSnapshotTranscriptItemV1> Items,
    bool OlderAvailable,
    string? OldestSequence,
    string? NewestSequence);

public sealed record SessionActivityProjectionV1(
    string WorkState,
    string? TurnId,
    string? ResolutionCategory);

public sealed record SessionCommandReconciliationV1(
    string LastOutcomeCode,
    string? LastCommandId);

public sealed record SessionSnapshotV1(
    string SchemaVersion,
    string ProjectionKind,
    Guid SessionId,
    string LifecycleState,
    int SessionVersion,
    string LastConfirmedSequence,
    string AuthoritativeObservedAt,
    IReadOnlyList<string> PermittedActions,
    string RecoveryCategory,
    string? CutoffSequence = null,
    SessionAgentIdentityV1? Agent = null,
    SessionTimingProjectionV1? Timing = null,
    SessionBoundSubmissionSummaryV1? BoundSubmission = null,
    SessionTranscriptPageV1? Transcript = null,
    SessionActivityProjectionV1? Activity = null,
    SessionCommandReconciliationV1? CommandReconciliation = null);

public sealed record SessionCommandOutcomeV1(
    string SchemaVersion,
    bool Succeeded,
    string OutcomeCategory,
    string OutcomeCode,
    string CommandId,
    string CommandType,
    Guid SessionId,
    string PermittedRecoveryAction,
    IReadOnlyList<string> PermittedActions,
    int? SessionVersion = null,
    string? SessionSequence = null,
    string? AcceptedMessageId = null);

public sealed record SessionHostedEventPayloadV1(
    string Summary,
    string? LifecycleState = null,
    int? RemainingSeconds = null,
    string? WarningCode = null,
    string? MessageId = null,
    string? TurnId = null,
    string? WorkState = null,
    string? ResolutionCategory = null,
    string? AgentMessageId = null,
    int? FragmentSequence = null,
    string? TextDelta = null,
    string? AssembledContentDigest = null,
    int? FragmentCount = null,
    string? CutoffSequence = null,
    string? AccessState = null,
    string? RecoveryCategory = null);

public sealed record SessionHostedEventEnvelopeV1(
    string SchemaVersion,
    string EventType,
    Guid SessionId,
    string SessionSequence,
    int SessionVersion,
    string OccurredAt,
    SessionHostedEventPayloadV1 Payload);
