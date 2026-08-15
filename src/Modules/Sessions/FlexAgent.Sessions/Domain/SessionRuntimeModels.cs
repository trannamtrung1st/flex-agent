namespace FlexAgent.Sessions.Domain;

public enum SessionLifecycleState
{
    Ready,
    Active,
    Paused,
    Completing,
    Completed,
    Terminated,
    Aborted,
}

public static class InvocationPurposes
{
    public const string ParticipantTurnRespond = "participant_turn.respond";
    public const string AgentOpening = "workflow.agent_opening";
    public const string AgentClosing = "workflow.agent_closing";
    public const string TimerLaneCheck = "timer.lane_check";
}

public static class AgentInvocationStatuses
{
    public const string Admitted = "admitted";
    public const string Executing = "executing";
    public const string DecisionRecorded = "decision_recorded";
    public const string Decided = "decided";
    public const string ExecutionFailed = "execution_failed";
    public const string Cancelled = "cancelled";
}

public static class TurnKinds
{
    public const string Participant = "participant";
    public const string AgentOpening = "agent_opening";
    public const string AgentClosing = "agent_closing";
    public const string AgentTimer = "agent_timer";
}

public static class TurnStates
{
    public const string Accepted = "accepted";
    public const string WorkQueued = "work_queued";
    public const string Complete = "complete";
    public const string Cancelled = "cancelled";
}

public static class ResponseSlotStates
{
    public const string Open = "open";
    public const string ClaimedForPublication = "claimed_for_publication";
    public const string IntentionalNoAction = "intentional_no_action";
    public const string Cancelled = "cancelled";
}

public static class TranscriptAuthorTypes
{
    public const string Participant = "participant";
    public const string Agent = "agent";
}

public static class TriggerAdmissionOutcomeCodes
{
    public const string Succeeded = "trigger_admission.succeeded";
    public const string Reconciled = "trigger_admission.reconciled";
    public const string UnknownTrigger = "trigger_admission.unknown_trigger";
    public const string ProhibitedTrigger = "trigger_admission.prohibited_trigger";
    public const string LifecycleIneligible = "trigger_admission.lifecycle_ineligible";
    public const string BudgetExhausted = "trigger_admission.budget_exhausted";
    public const string CooldownActive = "trigger_admission.cooldown_active";
    public const string IdempotencyConflict = "trigger_admission.idempotency_conflict";
    public const string OwnershipMismatch = "trigger_admission.ownership_mismatch";
    public const string StaleVersion = "trigger_admission.stale_version";
    public const string NonUtcClock = "trigger_admission.non_utc_clock";
    public const string StaleClock = "trigger_admission.stale_clock";
    public const string MissingTurn = "trigger_admission.missing_turn";
    public const string Denied = "trigger_admission.denied";
}

public static class InvocationCompletionOutcomeCodes
{
    public const string Decided = "invocation_completion.decided";
    public const string ExecutionFailed = "invocation_completion.execution_failed";
    public const string AttemptsExhausted = "invocation_completion.attempts_exhausted";
    public const string LateResult = "invocation_completion.late_result";
    public const string AlreadyTerminal = "invocation_completion.already_terminal";
    public const string EffectFailed = "invocation_completion.effect_failed";
    public const string AttemptRecorded = "invocation_completion.attempt_recorded";
    public const string IdentityMismatch = "invocation_completion.identity_mismatch";
    public const string NonUtcClock = "invocation_completion.non_utc_clock";
    public const string StaleClock = "invocation_completion.stale_clock";
    public const string StaleVersion = "invocation_completion.stale_version";
    public const string Denied = "invocation_completion.denied";
    public const string OwnershipMismatch = "invocation_completion.ownership_mismatch";
}

public static class TimerLaneStates
{
    public const string Pending = "pending";
    public const string Claimed = "claimed";
    public const string Fired = "fired";
    public const string Cancelled = "cancelled";
    public const string Superseded = "superseded";
    public const string Expired = "expired";
}

public static class TimerRequestedByCategories
{
    public const string DefaultCadence = "default_cadence";
    public const string AgentRecommendation = "agent_recommendation";
    public const string SuccessorAfterFire = "successor_after_fire";
}

public static class TimerFireOutcomeCodes
{
    public const string Succeeded = "timer_fire.succeeded";
    public const string Reconciled = "timer_fire.reconciled";
    public const string Idle = "timer_fire.idle";
    public const string NotDue = "timer_fire.not_due";
    public const string LifecycleIneligible = "timer_fire.lifecycle_ineligible";
    public const string BudgetExhausted = "timer_fire.budget_exhausted";
    public const string NonUtcClock = "timer_fire.non_utc_clock";
    public const string StaleClock = "timer_fire.stale_clock";
    public const string StaleRevision = "timer_fire.stale_revision";
}

public sealed record TimerFireResult(
    bool Succeeded,
    string OutcomeCode,
    TimerScheduleRevision? Revision = null,
    TriggerAdmissionResult? Admission = null);

public static class SessionRuntimeAuditActions
{
    public const string FireDueTimer = "session.timer_lane.fire";
    public const string AdmitTrustedTrigger = "session.trusted_trigger.admit";
    public const string AcceptParticipantMessage = "session.participant_message.accept";
    public const string CompleteInvocation = "session.invocation.complete";
    public const string PublishAgentResponseFragment = "session.agent_response.publish";
    public const string SealAgentResponse = "session.agent_response.seal";
}

public static class SessionRuntimeResourceTypes
{
    public const string Session = "session";
}

public static class SessionRuntimeOutboxEventTypes
{
    public const string TimerLaneFired = "session.timer_lane.fired";
    public const string TrustedTriggerAdmitted = "session.trusted_trigger.admitted";
    public const string ParticipantMessageAccepted = "session.participant_message.accepted";
    public const string InvocationCompleted = "session.invocation.completed";
    public const string AgentFragmentCommitted = "session.agent.fragment.committed";
    public const string AgentMessageSealed = "session.agent.message.sealed";
}

public static class DurableSessionWorkTypes
{
    public const string ExecuteInvocation = "invocation.execute";
}

public static class DurableSessionWorkStates
{
    public const string Pending = "pending";
    public const string Claimed = "claimed";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}

public static class DurableInvocationWorkOutcomes
{
    public const string Idle = "idle";
    public const string Decided = "decided";
    public const string ExecutionFailed = "execution_failed";
    public const string Reconciled = "reconciled";
    public const string RetryLater = "retry_later";
    public const string Published = "published";
    public const string PublicationIncomplete = "publication_incomplete";
    public const string PublicationFailed = "publication_failed";
}

public static class AgentMessageCompletionStates
{
    public const string Open = "open";
    public const string Complete = "complete";
    public const string Incomplete = "incomplete";
    public const string Cancelled = "cancelled";
}

public static class FragmentCommitOutcomeCodes
{
    public const string Succeeded = "fragment_commit.succeeded";
    public const string Reconciled = "fragment_commit.reconciled";
    public const string Gap = "fragment_commit.gap";
    public const string DigestMismatch = "fragment_commit.digest_mismatch";
    public const string CompetingAttempt = "fragment_commit.competing_attempt";
    public const string Cutoff = "fragment_commit.cutoff";
    public const string PublicationNotClaimed = "fragment_commit.publication_not_claimed";
    public const string EmptyDelta = "fragment_commit.empty_delta";
    public const string AlreadyTerminal = "fragment_commit.already_terminal";
    public const string NonUtcClock = "fragment_commit.non_utc_clock";
    public const string StaleClock = "fragment_commit.stale_clock";
    public const string StaleVersion = "fragment_commit.stale_version";
    public const string Denied = "fragment_commit.denied";
    public const string OwnershipMismatch = "fragment_commit.ownership_mismatch";
    public const string FragmentTooLarge = "fragment_commit.fragment_too_large";
    public const string FragmentCountExceeded = "fragment_commit.fragment_count_exceeded";
    public const string AssembledSizeExceeded = "fragment_commit.assembled_size_exceeded";
    public const string InFlightExceeded = "fragment_commit.in_flight_exceeded";
    public const string RateExceeded = "fragment_commit.rate_exceeded";
    public const string ValidationFailed = "fragment_commit.validation_failed";
    public const string UnpublishedFailed = "fragment_commit.unpublished_failed";
}

public static class ExecutionFailureReasons
{
    public const string MalformedControl = "malformed_control";
    public const string IncompleteControl = "incomplete_control";
    public const string ProviderTimeout = "provider_timeout";
    public const string ProviderUnavailable = "provider_unavailable";
    public const string CredentialBindingFailed = "credential_binding_failed";
}

public static class ExecutionAttemptOutcomeCategories
{
    public const string DecisionProduced = "decision_produced";
    public const string ProviderTimeout = "provider_timeout";
    public const string ProviderUnavailable = "provider_unavailable";
    public const string MalformedControl = "malformed_control";
    public const string IncompleteControl = "incomplete_control";
    public const string Cancelled = "cancelled";
    public const string LateResult = "late_result";
}

public static class ExecutionOutcomeCategories
{
    public const string ExecutionFailed = "execution_failed";
    public const string Cancelled = "cancelled";
    public const string LateResult = "late_result";
    public const string AttemptsExhausted = "attempts_exhausted";
}

public static class NoActionReasonCategories
{
    public const string IntentionalSilence = "intentional_silence";
    public const string WorkflowComplete = "workflow_complete";
    public const string AwaitingInput = "awaiting_input";
}

public static class DecisionValidationOutcomes
{
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string Suppressed = "suppressed";
}

public static class DecisionEffectOutcomes
{
    public const string Applied = "applied";
    public const string NoDomainEffect = "no_domain_effect";
    public const string EffectFailed = "effect_failed";
    public const string NotAttempted = "not_attempted";
}

public static class RejectionReasonCategories
{
    public const string PolicyProhibited = "policy_prohibited";
    public const string CapabilityDisabled = "capability_disabled";
    public const string PayloadInvalid = "payload_invalid";
    public const string StateIneligible = "state_ineligible";
    public const string BudgetExhausted = "budget_exhausted";
    public const string CutoffExceeded = "cutoff_exceeded";
}

public static class TimerValidationOutcomes
{
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string Omitted = "omitted";
    public const string NotPresent = "not_present";
}

public static class DecisionPayloadDigest
{
    public const string FormatVersionV1 = "v1";
}

public static class InvocationContextFactCategories
{
    public const string SubmissionRef = "submission_ref";
    public const string KnowledgeRef = "knowledge_ref";
    public const string MemoryReadRef = "memory_read_ref";
    public const string TranscriptItem = "transcript_item";
    public const string ModelControl = "model_control";
    public const string Credential = "credential";
}

public static class InvocationContextOutcomeCodes
{
    public const string Succeeded = "invocation_context.succeeded";
    public const string DisallowedFact = "invocation_context.disallowed_fact";
    public const string OwnershipMismatch = "invocation_context.ownership_mismatch";
    public const string UnpermittedReference = "invocation_context.unpermitted_reference";
}

public sealed record SessionOwnership(
    Guid OrganizationId,
    Guid ActivityId,
    Guid ParticipantId,
    Guid AttemptId,
    Guid SessionId);

public sealed record TrustedRuntimeActor(Guid ActorId, string ActorType);

public sealed record ProtectedContentRef(string ProtectedRef, string ContentDigest)
{
    public static string DigestForReference(string protectedRef) => DigestUtf8(protectedRef);

    public static string DigestUtf8(string text)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public sealed record VisibleTranscriptItemRef(
    string MessageId,
    string AuthorType,
    string? TurnId,
    ProtectedContentRef ContentRef);

public sealed record TrustedSessionBinding(
    SessionOwnership Ownership,
    string ConfigurationId,
    string ConfigurationDigest,
    string ManifestId,
    FrozenTextSessionRuntimePolicy Policy,
    IReadOnlyList<ProtectedContentRef> PermittedSubmissionRefs,
    IReadOnlyList<ProtectedContentRef> PermittedKnowledgeRefs,
    IReadOnlyList<ProtectedContentRef> PermittedMemoryReadRefs);

public sealed record TrustedTrigger(
    string TriggerFamily,
    string TriggerType,
    string TriggerId,
    string Purpose,
    string? TurnId,
    string? ResponseSlotId);

public sealed record NextTimerRecommendation(string RelativeDelay, string ExpectedScheduleRevision);

public abstract record DecisionRecommendation(
    string DecisionId,
    string InvocationId,
    DateTimeOffset ProducedAt,
    NextTimerRecommendation? NextTimer,
    string DecisionType);

public sealed record NoActionRecommendation(
    string DecisionId,
    string InvocationId,
    DateTimeOffset ProducedAt,
    string ReasonCategory,
    NextTimerRecommendation? NextTimer)
    : DecisionRecommendation(DecisionId, InvocationId, ProducedAt, NextTimer, RuntimeDecisionTypes.NoAction);

public sealed record EmitMessageRecommendation(
    string DecisionId,
    string InvocationId,
    DateTimeOffset ProducedAt,
    string CommunicationPurpose,
    string? TurnId,
    string? ResponseSlotId,
    NextTimerRecommendation? NextTimer)
    : DecisionRecommendation(DecisionId, InvocationId, ProducedAt, NextTimer, RuntimeDecisionTypes.EmitMessage);

public sealed record ProhibitedDecisionRecommendation(
    string DecisionId,
    string InvocationId,
    DateTimeOffset ProducedAt,
    string DecisionType,
    NextTimerRecommendation? NextTimer)
    : DecisionRecommendation(DecisionId, InvocationId, ProducedAt, NextTimer, DecisionType);

public sealed record ExecutionFailureCompletion(string ReasonCategory);

public sealed record InvocationContextFact(
    string Category,
    SessionOwnership? Ownership,
    string? Value);

public sealed record TriggerAdmissionResult(
    bool Succeeded,
    string OutcomeCode,
    AgentInvocation? Invocation,
    long? SessionSequence,
    long? SessionVersion = null);

public sealed record InvocationCompletionResult(
    bool Succeeded,
    string OutcomeCode,
    AgentInvocation? Invocation,
    AgentDecisionRecord? Decision = null,
    ExecutionOutcomeRecord? ExecutionOutcome = null,
    DecisionValidationEffectRecord? ValidationEffect = null,
    bool PublicationPathClaimed = false,
    bool AgentMessagePublished = false);

public sealed record DecisionValidationResult(
    bool Succeeded,
    string OutcomeCode,
    string ValidationOutcome,
    string? RejectionReasonCategory,
    string TimerValidationOutcome,
    IReadOnlyList<OutputItemValidation>? OutputValidations = null,
    IReadOnlyList<RequestedActionItemValidation>? RequestedActionValidations = null);

public sealed record DecisionEffectResult(
    bool Succeeded,
    string OutcomeCode,
    string EffectOutcome,
    bool PublicationPathClaimed = false,
    bool AgentMessagePublished = false);

public sealed record AgentResponseFragmentCommit(
    string AgentInvocationId,
    int FragmentOrdinal,
    string ExactUtf8Text,
    string GenerationAttemptId);

public sealed record AgentResponseFragmentCommitResult(
    bool Succeeded,
    string OutcomeCode,
    AgentResponseMessage? Message = null,
    AgentResponseFragment? Fragment = null,
    bool AgentMessagePublished = false);

public static class AuthorizedSessionEventTypes
{
    public const string AgentFragment = "session.agent.fragment.v1";
    public const string AgentComplete = "session.agent.complete.v1";
}

public static class SessionEventReplayOutcomeCodes
{
    public const string Succeeded = "session_event_replay.succeeded";
    public const string Reconcile = "session_event_replay.reconcile";
    public const string Denied = "session_event_replay.denied";
    public const string OwnershipMismatch = "session_event_replay.ownership_mismatch";
}

public sealed record AuthorizedSessionProjectionEvent(
    string EventType,
    string SessionId,
    string SessionSequence,
    string OccurredAt,
    string Summary,
    int? FragmentSequence = null,
    string? AgentMessageId = null,
    string? TextDelta = null,
    string? AssembledContentDigest = null,
    int? FragmentCount = null);

public sealed record AuthorizedSessionEventReplayResult(
    bool Succeeded,
    string OutcomeCode,
    IReadOnlyList<AuthorizedSessionProjectionEvent> Events,
    bool HasMore = false);

public sealed record InvocationContextAssembleResult(
    bool Succeeded,
    string OutcomeCode,
    InvocationContext? Context);

public sealed class InvocationContext
{
    public InvocationContext(
        SessionOwnership ownership,
        string configurationDigest,
        string policyDigest,
        IReadOnlyList<ProtectedContentRef> submissionRefs,
        IReadOnlyList<ProtectedContentRef> knowledgeRefs,
        IReadOnlyList<ProtectedContentRef> memoryReadRefs,
        IReadOnlyList<VisibleTranscriptItemRef> visibleTranscript,
        IReadOnlyList<string> factCategories)
    {
        Ownership = ownership;
        ConfigurationDigest = configurationDigest;
        PolicyDigest = policyDigest;
        SubmissionRefs = submissionRefs;
        KnowledgeRefs = knowledgeRefs;
        MemoryReadRefs = memoryReadRefs;
        VisibleTranscript = visibleTranscript;
        FactCategories = factCategories;
    }

    public SessionOwnership Ownership { get; }

    public string ConfigurationDigest { get; }

    public string PolicyDigest { get; }

    public IReadOnlyList<ProtectedContentRef> SubmissionRefs { get; }

    public IReadOnlyList<ProtectedContentRef> KnowledgeRefs { get; }

    public IReadOnlyList<ProtectedContentRef> MemoryReadRefs { get; }

    public IReadOnlyList<VisibleTranscriptItemRef> VisibleTranscript { get; }

    public IReadOnlyList<string> FactCategories { get; }
}
