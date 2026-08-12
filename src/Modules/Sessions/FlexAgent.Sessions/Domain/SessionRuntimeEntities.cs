namespace FlexAgent.Sessions.Domain;

public sealed class ResponseSlot
{
    internal ResponseSlot(string responseSlotId)
    {
        ResponseSlotId = responseSlotId;
        State = ResponseSlotStates.Open;
    }

    public string ResponseSlotId { get; }

    public string State { get; private set; }

    public string? ClaimedByInvocationId { get; private set; }

    internal void ClaimForPublication(string invocationId)
    {
        State = ResponseSlotStates.ClaimedForPublication;
        ClaimedByInvocationId = invocationId;
    }

    internal void MarkIntentionalNoAction()
    {
        State = ResponseSlotStates.IntentionalNoAction;
        ClaimedByInvocationId = null;
    }

    internal void Cancel()
    {
        if (State == ResponseSlotStates.Open)
        {
            State = ResponseSlotStates.Cancelled;
        }
    }
}

public sealed class Turn
{
    internal Turn(string turnId, string kind, string? triggerInvocationId, ResponseSlot responseSlot)
    {
        TurnId = turnId;
        Kind = kind;
        TriggerInvocationId = triggerInvocationId;
        State = kind == TurnKinds.Participant ? TurnStates.Accepted : TurnStates.WorkQueued;
        ResponseSlot = responseSlot;
    }

    public string TurnId { get; }

    public string Kind { get; }

    public string? TriggerInvocationId { get; }

    public string State { get; private set; }

    public ResponseSlot ResponseSlot { get; }

    internal void MarkWorkQueued() => State = TurnStates.WorkQueued;

    internal void MarkComplete() => State = TurnStates.Complete;

    internal void Cancel()
    {
        if (State is TurnStates.Accepted or TurnStates.WorkQueued)
        {
            State = TurnStates.Cancelled;
            ResponseSlot.Cancel();
        }
    }
}

public sealed class InvocationExecutionAttempt
{
    internal InvocationExecutionAttempt(int attemptOrdinal, string outcomeCategory, string? agentDecisionId)
    {
        AttemptOrdinal = attemptOrdinal;
        OutcomeCategory = outcomeCategory;
        AgentDecisionId = agentDecisionId;
    }

    public int AttemptOrdinal { get; }

    public string OutcomeCategory { get; }

    public string? AgentDecisionId { get; }
}

public sealed class AgentDecisionRecord
{
    internal AgentDecisionRecord(DecisionRecommendation recommendation)
    {
        DecisionId = recommendation.DecisionId;
        DecisionType = recommendation.DecisionType;
        ProducedAt = recommendation.ProducedAt;
        NextTimer = recommendation.NextTimer;
        Recommendation = recommendation;
    }

    public string DecisionId { get; }

    public string DecisionType { get; }

    public DateTimeOffset ProducedAt { get; }

    public NextTimerRecommendation? NextTimer { get; }

    internal DecisionRecommendation Recommendation { get; }
}

public sealed class ExecutionOutcomeRecord
{
    internal ExecutionOutcomeRecord(string executionOutcomeId, string outcomeCategory, string reasonCategory)
    {
        ExecutionOutcomeId = executionOutcomeId;
        OutcomeCategory = outcomeCategory;
        ReasonCategory = reasonCategory;
    }

    public string ExecutionOutcomeId { get; }

    public string OutcomeCategory { get; }

    public string ReasonCategory { get; }
}

public sealed class DecisionValidationEffectRecord
{
    internal DecisionValidationEffectRecord(
        string validationOutcome,
        string effectOutcome,
        string timerValidationOutcome,
        string? rejectionReasonCategory)
    {
        ValidationOutcome = validationOutcome;
        EffectOutcome = effectOutcome;
        TimerValidationOutcome = timerValidationOutcome;
        RejectionReasonCategory = rejectionReasonCategory;
    }

    public string ValidationOutcome { get; private set; }

    public string EffectOutcome { get; private set; }

    public string TimerValidationOutcome { get; }

    public string? RejectionReasonCategory { get; }

    internal void SetEffectOutcome(string effectOutcome) => EffectOutcome = effectOutcome;
}

public sealed class AgentInvocation
{
    private readonly List<InvocationExecutionAttempt> _attempts = [];

    internal AgentInvocation(
        string agentInvocationId,
        SessionOwnership ownership,
        TrustedTrigger trigger,
        string idempotencyKey,
        string policyDigest,
        long sessionSequence)
    {
        AgentInvocationId = agentInvocationId;
        Ownership = ownership;
        Trigger = trigger;
        IdempotencyKey = idempotencyKey;
        PolicyDigest = policyDigest;
        SessionSequence = sessionSequence;
        Status = AgentInvocationStatuses.Admitted;
    }

    public string AgentInvocationId { get; }

    public SessionOwnership Ownership { get; }

    public TrustedTrigger Trigger { get; }

    public string IdempotencyKey { get; }

    public string PolicyDigest { get; }

    public long SessionSequence { get; private set; }

    public string Status { get; private set; }

    public string? AgentDecisionId { get; private set; }

    public string? ExecutionOutcomeId { get; private set; }

    public AgentDecisionRecord? Decision { get; private set; }

    public ExecutionOutcomeRecord? ExecutionOutcome { get; private set; }

    public DecisionValidationEffectRecord? ValidationEffect { get; private set; }

    public IReadOnlyList<InvocationExecutionAttempt> Attempts => _attempts;

    public bool IsTerminal =>
        Status is AgentInvocationStatuses.Decided
            or AgentInvocationStatuses.ExecutionFailed
            or AgentInvocationStatuses.Cancelled;

    internal string IdentityKey =>
        $"{Trigger.TriggerFamily}|{Trigger.TriggerType}|{Trigger.TriggerId}|{Trigger.Purpose}|{PolicyDigest}";

    internal void AttachDecision(AgentDecisionRecord decision, long sessionSequence)
    {
        Decision = decision;
        AgentDecisionId = decision.DecisionId;
        SessionSequence = sessionSequence;
        Status = AgentInvocationStatuses.Decided;
        _attempts.Add(new InvocationExecutionAttempt(
            _attempts.Count + 1,
            ExecutionAttemptOutcomeCategories.DecisionProduced,
            decision.DecisionId));
    }

    internal void AttachExecutionOutcome(ExecutionOutcomeRecord outcome, long sessionSequence, string status)
    {
        ExecutionOutcome = outcome;
        ExecutionOutcomeId = outcome.ExecutionOutcomeId;
        SessionSequence = sessionSequence;
        Status = status;
    }

    internal void AddFailedAttempt(string outcomeCategory)
    {
        _attempts.Add(new InvocationExecutionAttempt(_attempts.Count + 1, outcomeCategory, null));
    }

    internal void SetValidationEffect(DecisionValidationEffectRecord validationEffect) =>
        ValidationEffect = validationEffect;
}
